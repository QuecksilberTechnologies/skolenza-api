using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.Data.Sqlite;

namespace Skolenza.StaffMemberAgent;

internal sealed class LocalAgentStore
{
    private readonly string _rootPath;
    private readonly string _queuePath;
    private readonly string _databasePath;
    private readonly byte[] _queueKey;

    public LocalAgentStore()
    {
        _rootPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), AgentConstants.ProductFolder);
        _queuePath = Path.Combine(_rootPath, AgentConstants.QueueFolderName);
        _databasePath = Path.Combine(_rootPath, AgentConstants.QueueDatabaseFileName);
        Directory.CreateDirectory(_queuePath);
        _queueKey = LoadOrCreateQueueKey();
    }

    public string RootPath => _rootPath;

    public async Task InitializeAsync(CancellationToken cancellationToken)
    {
        await using var connection = OpenConnection();
        await connection.OpenAsync(cancellationToken);
        var command = connection.CreateCommand();
        command.CommandText = """
            CREATE TABLE IF NOT EXISTS CaptureQueue (
                CaptureId TEXT NOT NULL PRIMARY KEY,
                CapturedAtUtc TEXT NOT NULL,
                MonitorNumber INTEGER NOT NULL,
                EncryptedFilePath TEXT NOT NULL,
                ContentType TEXT NOT NULL,
                ContentLength INTEGER NOT NULL,
                ChecksumSha256 TEXT NOT NULL,
                Status INTEGER NOT NULL,
                RetryCount INTEGER NOT NULL,
                NextRetryUtc TEXT NULL,
                LastError TEXT NULL
            );
            CREATE INDEX IF NOT EXISTS IX_CaptureQueue_Status_NextRetry
                ON CaptureQueue (Status, NextRetryUtc, CapturedAtUtc);
            """;
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<QueueCapture> EnqueueAsync(byte[] screenshot, int monitorNumber, DateTime capturedAtUtc, CancellationToken cancellationToken)
    {
        var captureId = Guid.CreateVersion7();
        var encrypted = Encrypt(screenshot);
        var filePath = Path.Combine(_queuePath, captureId.ToString("N") + ".enc");
        await File.WriteAllBytesAsync(filePath, encrypted, cancellationToken);
        var checksum = Convert.ToHexString(SHA256.HashData(screenshot));

        try
        {
            await using var connection = OpenConnection();
            await connection.OpenAsync(cancellationToken);
            var command = connection.CreateCommand();
            command.CommandText = """
                INSERT INTO CaptureQueue
                    (CaptureId, CapturedAtUtc, MonitorNumber, EncryptedFilePath, ContentType, ContentLength, ChecksumSha256, Status, RetryCount)
                VALUES
                    ($captureId, $capturedAtUtc, $monitorNumber, $filePath, $contentType, $contentLength, $checksum, 0, 0);
                """;
            command.Parameters.AddWithValue("$captureId", captureId.ToString("D"));
            command.Parameters.AddWithValue("$capturedAtUtc", capturedAtUtc.ToString("O"));
            command.Parameters.AddWithValue("$monitorNumber", monitorNumber);
            command.Parameters.AddWithValue("$filePath", filePath);
            command.Parameters.AddWithValue("$contentType", "image/jpeg");
            command.Parameters.AddWithValue("$contentLength", screenshot.LongLength);
            command.Parameters.AddWithValue("$checksum", checksum);
            await command.ExecuteNonQueryAsync(cancellationToken);
        }
        catch
        {
            File.Delete(filePath);
            throw;
        }

        return new QueueCapture(captureId, capturedAtUtc, monitorNumber, filePath, "image/jpeg", screenshot.LongLength, checksum, 0);
    }

    public async Task<List<QueueCapture>> GetPendingAsync(int limit, CancellationToken cancellationToken)
    {
        var result = new List<QueueCapture>();
        await using var connection = OpenConnection();
        await connection.OpenAsync(cancellationToken);
        var command = connection.CreateCommand();
        command.CommandText = """
            SELECT CaptureId, CapturedAtUtc, MonitorNumber, EncryptedFilePath, ContentType, ContentLength, ChecksumSha256, RetryCount
            FROM CaptureQueue
            WHERE Status = 0 AND (NextRetryUtc IS NULL OR NextRetryUtc <= $now)
            ORDER BY CapturedAtUtc
            LIMIT $limit;
            """;
        command.Parameters.AddWithValue("$now", DateTime.UtcNow.ToString("O"));
        command.Parameters.AddWithValue("$limit", limit);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            result.Add(new QueueCapture(
                Guid.Parse(reader.GetString(0)),
                DateTime.Parse(reader.GetString(1), null, System.Globalization.DateTimeStyles.RoundtripKind),
                reader.GetInt32(2),
                reader.GetString(3),
                reader.GetString(4),
                reader.GetInt64(5),
                reader.GetString(6),
                reader.GetInt32(7)));
        }
        return result;
    }

    public byte[] ReadDecrypted(QueueCapture capture) => Decrypt(File.ReadAllBytes(capture.EncryptedFilePath));

    public async Task MarkSynchronizedAsync(QueueCapture capture, CancellationToken cancellationToken)
    {
        await ExecuteAsync("DELETE FROM CaptureQueue WHERE CaptureId = $captureId;", capture.CaptureId, null, cancellationToken);
        if (File.Exists(capture.EncryptedFilePath)) File.Delete(capture.EncryptedFilePath);
    }

    public Task MarkFailedAsync(QueueCapture capture, string error, DateTime nextRetryUtc, CancellationToken cancellationToken) =>
        ExecuteAsync(
            "UPDATE CaptureQueue SET RetryCount = RetryCount + 1, NextRetryUtc = $nextRetryUtc, LastError = $error WHERE CaptureId = $captureId;",
            capture.CaptureId,
            (nextRetryUtc, error.Length > 500 ? error[..500] : error),
            cancellationToken);

    public async Task<int> CountPendingAsync(CancellationToken cancellationToken)
    {
        await using var connection = OpenConnection();
        await connection.OpenAsync(cancellationToken);
        var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM CaptureQueue WHERE Status = 0;";
        return Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken));
    }

    public long GetQueueBytes() => Directory.EnumerateFiles(_queuePath, "*.enc").Sum(path => new FileInfo(path).Length);

    public void RemoveExpired(DateTime oldestAcceptedUtc)
    {
        using var connection = OpenConnection();
        connection.Open();
        using var select = connection.CreateCommand();
        select.CommandText = "SELECT EncryptedFilePath FROM CaptureQueue WHERE CapturedAtUtc < $cutoff;";
        select.Parameters.AddWithValue("$cutoff", oldestAcceptedUtc.ToString("O"));
        using var reader = select.ExecuteReader();
        var paths = new List<string>();
        while (reader.Read()) paths.Add(reader.GetString(0));
        reader.Close();
        using var delete = connection.CreateCommand();
        delete.CommandText = "DELETE FROM CaptureQueue WHERE CapturedAtUtc < $cutoff;";
        delete.Parameters.AddWithValue("$cutoff", oldestAcceptedUtc.ToString("O"));
        delete.ExecuteNonQuery();
        foreach (var path in paths.Where(File.Exists)) File.Delete(path);
    }

    public static void SaveBootstrap(Uri apiBaseUri, Guid agentInstanceId, string credential)
    {
        var root = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), AgentConstants.ProductFolder);
        Directory.CreateDirectory(root);
        File.WriteAllText(Path.Combine(root, AgentConstants.SettingsFileName), JsonSerializer.Serialize(new AgentSettings(apiBaseUri, agentInstanceId)));
        var protectedCredential = ProtectedData.Protect(System.Text.Encoding.UTF8.GetBytes(credential), null, DataProtectionScope.CurrentUser);
        File.WriteAllBytes(Path.Combine(root, AgentConstants.CredentialFileName), protectedCredential);
    }

    public static bool TryLoadBootstrap(out AgentSettings? settings, out string? credential)
    {
        settings = null;
        credential = null;
        var root = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), AgentConstants.ProductFolder);
        var settingsPath = Path.Combine(root, AgentConstants.SettingsFileName);
        var credentialPath = Path.Combine(root, AgentConstants.CredentialFileName);

        if (!File.Exists(settingsPath) || !File.Exists(credentialPath)) return false;

        try
        {
            settings = JsonSerializer.Deserialize<AgentSettings>(File.ReadAllText(settingsPath));
            credential = System.Text.Encoding.UTF8.GetString(ProtectedData.Unprotect(
                File.ReadAllBytes(credentialPath), null, DataProtectionScope.CurrentUser));

            return settings is not null &&
                   settings.AgentInstanceId != Guid.Empty &&
                   settings.ApiBaseUri.IsAbsoluteUri &&
                   (settings.ApiBaseUri.Scheme == Uri.UriSchemeHttps || settings.ApiBaseUri.IsLoopback) &&
                   !string.IsNullOrWhiteSpace(credential);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or JsonException or CryptographicException)
        {
            settings = null;
            credential = null;
            return false;
        }
    }

    private byte[] LoadOrCreateQueueKey()
    {
        var path = Path.Combine(_rootPath, AgentConstants.EncryptionKeyFileName);
        if (File.Exists(path)) return ProtectedData.Unprotect(File.ReadAllBytes(path), null, DataProtectionScope.CurrentUser);
        var key = RandomNumberGenerator.GetBytes(32);
        File.WriteAllBytes(path, ProtectedData.Protect(key, null, DataProtectionScope.CurrentUser));
        return key;
    }

    private byte[] Encrypt(byte[] content)
    {
        var nonce = RandomNumberGenerator.GetBytes(12);
        var tag = new byte[16];
        var ciphertext = new byte[content.Length];
        using var aes = new AesGcm(_queueKey, tag.Length);
        aes.Encrypt(nonce, content, ciphertext, tag);
        return nonce.Concat(tag).Concat(ciphertext).ToArray();
    }

    private byte[] Decrypt(byte[] content)
    {
        var plaintext = new byte[content.Length - 28];
        using var aes = new AesGcm(_queueKey, 16);
        aes.Decrypt(content.AsSpan(0, 12), content.AsSpan(28), content.AsSpan(12, 16), plaintext);
        return plaintext;
    }

    private SqliteConnection OpenConnection() => new($"Data Source={_databasePath};Mode=ReadWriteCreate;Cache=Shared");

    private async Task ExecuteAsync(string sql, Guid captureId, (DateTime NextRetryUtc, string Error)? failure, CancellationToken cancellationToken)
    {
        await using var connection = OpenConnection();
        await connection.OpenAsync(cancellationToken);
        var command = connection.CreateCommand();
        command.CommandText = sql;
        command.Parameters.AddWithValue("$captureId", captureId.ToString("D"));
        if (failure.HasValue)
        {
            command.Parameters.AddWithValue("$nextRetryUtc", failure.Value.NextRetryUtc.ToString("O"));
            command.Parameters.AddWithValue("$error", failure.Value.Error);
        }
        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
