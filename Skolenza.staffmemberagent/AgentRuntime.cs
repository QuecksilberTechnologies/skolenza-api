using System.Drawing.Imaging;
using System.Net.NetworkInformation;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.Win32;

namespace Skolenza.StaffMemberAgent;

internal sealed class AgentRuntime : IDisposable
{
    private readonly AgentSettings _settings;
    private readonly string _credential;
    private readonly LocalAgentStore _store;
    private readonly HttpClient _httpClient;
    private readonly SemaphoreSlim _wakeSignal = new(0, 1);
    private MonitoringPolicy? _policy;
    private DateTime _nextCaptureUtc;
    private DateTime _nextHeartbeatUtc;
    private DateTime _nextPolicyRefreshUtc;
    private DateTime? _lastCaptureUtc;
    private DateTime? _lastSuccessfulUploadUtc;
    private string? _lastErrorCode;

    public AgentRuntime(AgentSettings settings, string credential, LocalAgentStore store)
    {
        _settings = settings;
        _credential = credential;
        _store = store;
        _httpClient = new HttpClient { BaseAddress = settings.ApiBaseUri, Timeout = TimeSpan.FromSeconds(30) };
        _httpClient.DefaultRequestHeaders.Add(AgentConstants.AgentCredentialHeader, credential);
        NetworkChange.NetworkAvailabilityChanged += OnNetworkAvailabilityChanged;
    }

    public event Action<string>? StatusChanged;

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        await _store.InitializeAsync(cancellationToken);
        RegisterAutoStart();
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await RefreshPolicyIfRequiredAsync(cancellationToken);
                if (_policy is not null)
                {
                    _store.RemoveExpired(DateTime.UtcNow.AddDays(-_policy.OfflineRetentionDays));
                    await CaptureIfRequiredAsync(cancellationToken);
                    await SynchronizeAsync(cancellationToken);
                    await SendHeartbeatIfRequiredAsync(cancellationToken);
                }
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                _lastErrorCode = exception.GetType().Name;
                StatusChanged?.Invoke("Offline - waiting to retry");
            }

            await WaitForWakeOrDelayAsync(cancellationToken);
        }
    }

    private async Task RefreshPolicyIfRequiredAsync(CancellationToken cancellationToken)
    {
        if (_policy is not null && DateTime.UtcNow < _nextPolicyRefreshUtc) return;
        using var response = await _httpClient.GetAsync(AgentConstants.ConfigurationRoute, cancellationToken);
        response.EnsureSuccessStatusCode();
        var envelope = await response.Content.ReadFromJsonAsync<ApiEnvelope<MonitoringPolicy>>(cancellationToken: cancellationToken);
        _policy = envelope?.Data ?? throw new InvalidOperationException("The server did not return an agent policy.");
        _nextPolicyRefreshUtc = DateTime.UtcNow.AddMinutes(15);
        if (_nextCaptureUtc == default) ScheduleNextCapture();
        if (_nextHeartbeatUtc == default) _nextHeartbeatUtc = DateTime.UtcNow;
    }

    private async Task CaptureIfRequiredAsync(CancellationToken cancellationToken)
    {
        if (_policy is null || DateTime.UtcNow < _nextCaptureUtc) return;
        if (_store.GetQueueBytes() >= _policy.MaximumOfflineBytes)
        {
            _lastErrorCode = AgentConstants.LocalQueueLimitErrorCode;
            ScheduleNextCapture();
            return;
        }

        var screens = _policy.CaptureAllMonitors ? Screen.AllScreens : [Screen.PrimaryScreen ?? Screen.AllScreens[0]];
        for (var index = 0; index < screens.Length; index++)
        {
            var bytes = CaptureScreen(screens[index], _policy.ImageQuality);
            if (_store.GetQueueBytes() + bytes.LongLength > _policy.MaximumOfflineBytes)
            {
                _lastErrorCode = AgentConstants.LocalQueueLimitErrorCode;
                break;
            }
            await _store.EnqueueAsync(bytes, index, DateTime.UtcNow, cancellationToken);
        }
        _lastCaptureUtc = DateTime.UtcNow;
        ScheduleNextCapture();
        StatusChanged?.Invoke("Captured - synchronization pending");
    }

    private async Task SynchronizeAsync(CancellationToken cancellationToken)
    {
        foreach (var capture in await _store.GetPendingAsync(AgentConstants.UploadBatchSize, cancellationToken))
        {
            try
            {
                var bytes = _store.ReadDecrypted(capture);
                using var form = new MultipartFormDataContent
                {
                    { new StringContent(capture.CaptureId.ToString("D")), "captureId" },
                    { new StringContent(capture.CapturedAtUtc.ToString("O")), "capturedAtUtc" },
                    { new StringContent(capture.MonitorNumber.ToString(System.Globalization.CultureInfo.InvariantCulture)), "monitorNumber" },
                    { new StringContent(capture.ChecksumSha256), "checksumSha256" }
                };
                var file = new ByteArrayContent(bytes);
                file.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(capture.ContentType);
                form.Add(file, "screenshot", capture.CaptureId.ToString("N") + ".jpg");
                using var response = await _httpClient.PostAsync(AgentConstants.CaptureRoute, form, cancellationToken);
                response.EnsureSuccessStatusCode();
                await _store.MarkSynchronizedAsync(capture, cancellationToken);
                _lastSuccessfulUploadUtc = DateTime.UtcNow;
                _lastErrorCode = null;
                StatusChanged?.Invoke("Online - synchronized");
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                var exponent = Math.Min(capture.RetryCount, 5);
                var retryDelay = TimeSpan.FromSeconds(Math.Min(300, 15 * Math.Pow(2, exponent)));
                await _store.MarkFailedAsync(capture, exception.GetType().Name, DateTime.UtcNow.Add(retryDelay), cancellationToken);
                _lastErrorCode = AgentConstants.UploadFailedErrorCode;
                break;
            }
        }
    }

    private async Task SendHeartbeatIfRequiredAsync(CancellationToken cancellationToken)
    {
        if (_policy is null || DateTime.UtcNow < _nextHeartbeatUtc) return;
        var heartbeat = new
        {
            AgentVersion = Application.ProductVersion,
            PendingCaptureCount = await _store.CountPendingAsync(cancellationToken),
            LastCaptureDateTime = _lastCaptureUtc,
            LastSuccessfulUploadDateTime = _lastSuccessfulUploadUtc,
            LastErrorCode = _lastErrorCode
        };
        using var response = await _httpClient.PostAsJsonAsync(AgentConstants.HeartbeatRoute, heartbeat, cancellationToken);
        response.EnsureSuccessStatusCode();
        _nextHeartbeatUtc = DateTime.UtcNow.AddSeconds(_policy.HeartbeatIntervalSeconds);
    }

    private void ScheduleNextCapture()
    {
        if (_policy is null) return;
        _nextCaptureUtc = DateTime.UtcNow.AddSeconds(RandomNumberGenerator.GetInt32(
            _policy.MinimumCaptureIntervalSeconds,
            checked(_policy.MaximumCaptureIntervalSeconds + 1)));
    }

    private static byte[] CaptureScreen(Screen screen, int quality)
    {
        using var bitmap = new Bitmap(screen.Bounds.Width, screen.Bounds.Height, PixelFormat.Format24bppRgb);
        using (var graphics = Graphics.FromImage(bitmap))
        {
            graphics.CopyFromScreen(screen.Bounds.Location, Point.Empty, screen.Bounds.Size, CopyPixelOperation.SourceCopy);
        }
        using var output = new MemoryStream();
        var encoder = ImageCodecInfo.GetImageEncoders().Single(x => x.FormatID == ImageFormat.Jpeg.Guid);
        using var parameters = new EncoderParameters(1);
        parameters.Param[0] = new EncoderParameter(Encoder.Quality, quality);
        bitmap.Save(output, encoder, parameters);
        return output.ToArray();
    }

    private void RegisterAutoStart()
    {
        var executablePath = Environment.ProcessPath ?? throw new InvalidOperationException("The agent executable path is unavailable.");
        using var key = Registry.CurrentUser.OpenSubKey(AgentConstants.AutoStartRegistryPath, writable: true)
            ?? Registry.CurrentUser.CreateSubKey(AgentConstants.AutoStartRegistryPath, writable: true);
        key.SetValue(AgentConstants.AutoStartValueName, $"\"{executablePath}\"");
    }

    private async Task WaitForWakeOrDelayAsync(CancellationToken cancellationToken)
    {
        _ = await _wakeSignal.WaitAsync(
            TimeSpan.FromSeconds(AgentConstants.SyncProbeSeconds),
            cancellationToken);
    }

    private void OnNetworkAvailabilityChanged(object? sender, NetworkAvailabilityEventArgs eventArgs)
    {
        if (eventArgs.IsAvailable && _wakeSignal.CurrentCount == 0) _wakeSignal.Release();
    }

    public void Dispose()
    {
        NetworkChange.NetworkAvailabilityChanged -= OnNetworkAvailabilityChanged;
        _httpClient.Dispose();
        _wakeSignal.Dispose();
    }
}
