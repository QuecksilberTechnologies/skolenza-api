using System.Text.Json;

namespace Skolenza.AutomationTests.Infrastructure;

/// <summary>
/// Loads non-secret automation test settings. Environment variables take precedence over the JSON file.
/// </summary>
internal static class TestSettings
{
    private const string ApiBaseUrlEnvironmentVariable = "SKOLENZA_TEST_API_BASE_URL";
    private const string WebBaseUrlEnvironmentVariable = "SKOLENZA_TEST_WEB_BASE_URL";
    private const string HeadlessEnvironmentVariable = "SKOLENZA_TEST_HEADLESS";

    private static readonly Lazy<AutomationSettings> Settings = new(LoadSettings);

    /// <summary>
    /// Gets the API root URL, such as http://localhost:5170.
    /// </summary>
    public static string ApiBaseUrl => GetRequiredUrl(
        Environment.GetEnvironmentVariable(ApiBaseUrlEnvironmentVariable),
        Settings.Value.ApiBaseUrl,
        ApiBaseUrlEnvironmentVariable,
        nameof(AutomationSettings.ApiBaseUrl));

    /// <summary>
    /// Gets the optional web application root URL. A blank value intentionally skips UI tests.
    /// </summary>
    public static string? WebBaseUrl => GetOptionalUrl(
        Environment.GetEnvironmentVariable(WebBaseUrlEnvironmentVariable),
        Settings.Value.WebBaseUrl);

    /// <summary>
    /// Gets whether Chromium runs without a visible window.
    /// </summary>
    public static bool Headless => GetBoolean(
        Environment.GetEnvironmentVariable(HeadlessEnvironmentVariable),
        Settings.Value.Headless);

    private static AutomationSettings LoadSettings()
    {
        var configurationPath = Path.Combine(AppContext.BaseDirectory, "automationsettings.json");

        if (!File.Exists(configurationPath))
        {
            throw new InvalidOperationException(
                $"Automation settings file was not found at '{configurationPath}'. Build the test project and try again.");
        }

        var json = File.ReadAllText(configurationPath);
        return JsonSerializer.Deserialize<AutomationSettings>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        }) ?? new AutomationSettings();
    }

    private static string GetRequiredUrl(string? environmentValue, string? configuredValue, string environmentVariable, string settingName)
    {
        var value = GetOptionalUrl(environmentValue, configuredValue);

        if (value is null)
        {
            throw new InvalidOperationException(
                $"Set '{environmentVariable}' or '{settingName}' in automationsettings.json to a valid absolute URL.");
        }

        return value;
    }

    private static string? GetOptionalUrl(string? environmentValue, string? configuredValue)
    {
        var value = string.IsNullOrWhiteSpace(environmentValue) ? configuredValue : environmentValue;

        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            throw new InvalidOperationException($"'{value}' is not a valid HTTP(S) URL.");
        }

        return uri.ToString().TrimEnd('/');
    }

    private static bool GetBoolean(string? environmentValue, bool configuredValue) =>
        bool.TryParse(environmentValue, out var value) ? value : configuredValue;
}

internal sealed class AutomationSettings
{
    public string ApiBaseUrl { get; init; } = "http://localhost:5170";

    public string WebBaseUrl { get; init; } = string.Empty;

    public bool Headless { get; init; } = true;
}
