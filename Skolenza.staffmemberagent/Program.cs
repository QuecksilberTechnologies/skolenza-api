using System.Text.Json;

namespace Skolenza.StaffMemberAgent;

internal static class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
        ApplicationConfiguration.Initialize();
        if (TryBootstrap(args)) return;

        if (!LocalAgentStore.TryLoadBootstrap(out var settings, out var credential))
        {
            MessageBox.Show(
                "Agent setup is not complete. Register this computer from the Skolenza admin API, then run the agent once with the generated --api-url, --agent-instance-id and --credential values.",
                "Skolenza StaffMember Agent",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            return;
        }

        Application.Run(new AgentApplicationContext(settings!, credential!));
    }

    private static bool TryBootstrap(string[] args)
    {
        var apiIndex = Array.IndexOf(args, "--api-url");
        var credentialIndex = Array.IndexOf(args, "--credential");
        var instanceIndex = Array.IndexOf(args, "--agent-instance-id");
        if (apiIndex < 0 && credentialIndex < 0 && instanceIndex < 0) return false;
        if (apiIndex + 1 >= args.Length || credentialIndex + 1 >= args.Length || instanceIndex + 1 >= args.Length ||
            !Uri.TryCreate(args[apiIndex + 1], UriKind.Absolute, out var apiUri) ||
            (apiUri.Scheme != Uri.UriSchemeHttps && !apiUri.IsLoopback) ||
            !Guid.TryParse(args[instanceIndex + 1], out var agentInstanceId) ||
            string.IsNullOrWhiteSpace(args[credentialIndex + 1]))
        {
            MessageBox.Show("Agent setup parameters are invalid.", "Skolenza StaffMember Agent", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return true;
        }
        LocalAgentStore.SaveBootstrap(apiUri, agentInstanceId, args[credentialIndex + 1]);
        MessageBox.Show("Agent setup completed. Start the agent normally.", "Skolenza StaffMember Agent", MessageBoxButtons.OK, MessageBoxIcon.Information);
        return true;
    }
}

internal sealed class AgentApplicationContext : ApplicationContext
{
    private readonly NotifyIcon _trayIcon;
    private readonly CancellationTokenSource _cancellation = new();
    private readonly AgentRuntime _runtime;

    public AgentApplicationContext(AgentSettings settings, string credential)
    {
        _runtime = new AgentRuntime(settings, credential, new LocalAgentStore());
        _trayIcon = new NotifyIcon
        {
            Icon = SystemIcons.Application,
            Text = "Skolenza StaffMember Agent - starting",
            Visible = true,
            ContextMenuStrip = new ContextMenuStrip()
        };
        _trayIcon.ContextMenuStrip.Items.Add("Exit", null, (_, _) => ExitAgent());
        _runtime.StatusChanged += status =>
        {
            var text = "Skolenza StaffMember Agent - " + status;
            _trayIcon.Text = text.Length <= 63 ? text : text[..63];
        };
        _ = RunAsync();
    }

    private async Task RunAsync()
    {
        try
        {
            await _runtime.RunAsync(_cancellation.Token);
        }
        catch (OperationCanceledException) when (_cancellation.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            _trayIcon.Text = "Skolenza StaffMember Agent - error";
            MessageBox.Show(exception.Message, "Skolenza StaffMember Agent", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void ExitAgent()
    {
        _cancellation.Cancel();
        ExitThread();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _trayIcon.Visible = false;
            _trayIcon.Dispose();
            _runtime.Dispose();
            _cancellation.Dispose();
        }
        base.Dispose(disposing);
    }
}
