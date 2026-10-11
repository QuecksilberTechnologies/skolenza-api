// ================================================================
// Purpose : Defines command-specific device protocol policy. Internal command
//           tracking is never added to device JSON payloads.
// ================================================================

using System.Text.Json;
using skolenza.application.Exceptions;
using skolenza.domain.Entity;

namespace skolenza.application.Constants;

/// <summary>Defines the protocol and authorization policy for one vendor command.</summary>
public sealed record DeviceProtocolCommandDefinition(
    string Name,
    DeviceCommandAccessLevel AccessLevel,
    DeviceCommandResponseMode ResponseMode,
    IReadOnlyList<string> ResponseMatchFields);

/// <summary>
/// Catalogues vendor commands confirmed from the vendor protocol material. Unknown
/// commands are rejected rather than being forwarded through any physical-device transport.
/// </summary>
public static class DeviceProtocolCommandCatalog
{
    #region Catalog Lookup and Protocol Validation

    private static readonly IReadOnlyDictionary<string, DeviceProtocolCommandDefinition> Definitions =
        BuildDefinitions();

    /// <summary>Gets a known command policy or rejects an unconfirmed command.</summary>
    public static DeviceProtocolCommandDefinition GetRequired(string commandName)
    {
        var normalized = Normalize(commandName);
        return Definitions.TryGetValue(normalized, out var definition)
            ? definition
            : throw new ValidationErrorException("The requested device command is not part of the supported device protocol catalog.");
    }

    /// <summary>Normalizes and validates a command payload without mutating vendor JSON.</summary>
    public static string ValidatePayload(string commandName, string payload)
    {
        if (string.IsNullOrWhiteSpace(payload))
        {
            throw new ValidationErrorException("A device command payload is required.");
        }

        using var document = JsonDocument.Parse(payload);
        if (document.RootElement.ValueKind != JsonValueKind.Object ||
            !document.RootElement.TryGetProperty("cmd", out var commandProperty) ||
            commandProperty.ValueKind != JsonValueKind.String ||
            !string.Equals(Normalize(commandProperty.GetString()), Normalize(commandName), StringComparison.Ordinal))
        {
            throw new ValidationErrorException("The payload cmd must match the requested supported device command.");
        }

        foreach (var forbiddenField in new[] { "correlationid", "requestid", "commandid", "internaltrackingid" })
        {
            if (document.RootElement.EnumerateObject().Any(property =>
                    string.Equals(property.Name, forbiddenField, StringComparison.OrdinalIgnoreCase)))
            {
                throw new ValidationErrorException("Internal Skolenza tracking fields must not be sent in a device payload.");
            }
        }

        return document.RootElement.GetRawText();
    }

    /// <summary>Builds persisted protocol matching criteria from documented request/response fields only.</summary>
    public static string BuildMatchCriteria(DeviceProtocolCommandDefinition definition, string payload)
    {
        using var document = JsonDocument.Parse(payload);
        var criteria = new Dictionary<string, JsonElement>(StringComparer.OrdinalIgnoreCase);
        foreach (var field in definition.ResponseMatchFields)
        {
            if (document.RootElement.TryGetProperty(field, out var value))
            {
                criteria[field] = value.Clone();
            }
        }

        return JsonSerializer.Serialize(criteria);
    }

    /// <summary>Tests a response against the documented fields retained on a command.</summary>
    public static bool ResponseMatches(DeviceCommand command, JsonElement response)
    {
        if (!response.TryGetProperty("ret", out var ret) || ret.ValueKind != JsonValueKind.String ||
            !string.Equals(Normalize(ret.GetString()), command.CommandName, StringComparison.Ordinal))
        {
            return false;
        }

        using var criteriaDocument = JsonDocument.Parse(command.MatchCriteria);
        foreach (var property in criteriaDocument.RootElement.EnumerateObject())
        {
            if (!response.TryGetProperty(property.Name, out var responseValue) ||
                !JsonElement.DeepEquals(property.Value, responseValue))
            {
                return false;
            }
        }

        return true;
    }

    #endregion

    #region Command Definition Construction

    private static IReadOnlyDictionary<string, DeviceProtocolCommandDefinition> BuildDefinitions()
    {
        var allVendorCommands = typeof(DeviceCommands)
            .GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
            .Where(field => field.FieldType == typeof(string))
            .Select(field => (string?)field.GetRawConstantValue())
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => Normalize(value))
            .Where(value => value is not "reg" and not "sendlog" and not "senduser")
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                value => value,
                value => new DeviceProtocolCommandDefinition(
                    value,
                    DeviceCommandAccessLevel.HostOnly,
                    DeviceCommandResponseMode.Required,
                    Array.Empty<string>()),
                StringComparer.OrdinalIgnoreCase);

        ConfigureInstitution(allVendorCommands, DeviceCommands.SetUserInfo, "enrollid");
        ConfigureInstitution(allVendorCommands, DeviceCommands.AddUser, "enrollid");
        ConfigureInstitution(allVendorCommands, DeviceCommands.DeleteUser);
        ConfigureInstitution(allVendorCommands, DeviceCommands.EnableUser);
        ConfigureInstitution(allVendorCommands, DeviceCommands.GetDeviceCapabilities);
        ConfigureInstitution(allVendorCommands, DeviceCommands.GetDeviceInfo);
        ConfigureInstitution(allVendorCommands, DeviceCommands.GetTime);
        ConfigureInstitution(allVendorCommands, DeviceCommands.SetTime);
        // setdevinfo is dispatched only by the typed Institution runtime
        // configuration handler; the generic raw-command endpoint rejects it.
        ConfigureInstitution(allVendorCommands, DeviceCommands.SetDeviceInfo);
        ConfigureInstitution(allVendorCommands, DeviceCommands.GetUserInfo, "enrollid", "backupnum");
        ConfigureInstitution(
            allVendorCommands,
            DeviceCommands.Reboot,
            DeviceCommandAccessLevel.InstitutionPermission,
            DeviceCommandResponseMode.PublishOnly);
        ConfigureInstitution(allVendorCommands, DeviceCommands.GetDoorStatus, accessLevel: DeviceCommandAccessLevel.InstitutionAccessControlPermission);
        ConfigureInstitution(allVendorCommands, DeviceCommands.OpenDoor, accessLevel: DeviceCommandAccessLevel.InstitutionAccessControlPermission);
        ConfigureInstitution(allVendorCommands, DeviceCommands.LockControl, accessLevel: DeviceCommandAccessLevel.InstitutionAccessControlPermission);

        ConfigureStream(allVendorCommands, DeviceCommands.GetAllLog);
        ConfigureStream(allVendorCommands, DeviceCommands.GetAllUsers);
        ConfigureStream(allVendorCommands, DeviceCommands.GetNewLog);
        ConfigureStream(allVendorCommands, DeviceCommands.GetUserList);

        return allVendorCommands;
    }

    private static void ConfigureInstitution(
        IDictionary<string, DeviceProtocolCommandDefinition> definitions,
        string command,
        params string[] responseMatchFields) =>
        ConfigureInstitution(definitions, command, DeviceCommandAccessLevel.InstitutionPermission, DeviceCommandResponseMode.Required, responseMatchFields);

    private static void ConfigureInstitution(
        IDictionary<string, DeviceProtocolCommandDefinition> definitions,
        string command,
        DeviceCommandAccessLevel accessLevel,
        DeviceCommandResponseMode responseMode = DeviceCommandResponseMode.Required,
        params string[] responseMatchFields)
    {
        var normalized = Normalize(command);
        definitions[normalized] = new DeviceProtocolCommandDefinition(normalized, accessLevel, responseMode, responseMatchFields);
    }

    private static void ConfigureStream(IDictionary<string, DeviceProtocolCommandDefinition> definitions, string command)
    {
        var normalized = Normalize(command);
        definitions[normalized] = new DeviceProtocolCommandDefinition(
            normalized,
            DeviceCommandAccessLevel.HostOnly,
            DeviceCommandResponseMode.Streamed,
            Array.Empty<string>());
    }

    private static string Normalize(string? value) => value?.Trim().ToLowerInvariant() ?? string.Empty;

    #endregion
}
