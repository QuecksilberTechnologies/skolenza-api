// ================================================================
// Purpose : Keeps the AiFace device's firmware-confirmed dropdown codes in
//           one place. UI labels are deliberately human-readable; the numeric
//           value is the value sent to the device through a typed command.
// ================================================================

namespace skolenza.application.Constants;

/// <summary>One selectable, firmware-confirmed device option.</summary>
public sealed record DeviceDdlOption(string Value, string Label);

/// <summary>One group of dropdown fields used by a device-settings UI section.</summary>
public sealed record DeviceDdlField(string Key, string Label, IReadOnlyList<DeviceDdlOption> Options);

/// <summary>
/// Static option catalog copied from the connected AiFace firmware UI and its
/// read-only <c>getdevinfo</c>/<c>getlangoption</c> responses. Keep vendor
/// codes here rather than duplicating them in Angular.
/// </summary>
public static class DeviceDdl
{
    public const string Time = "time";
    public const string Bell = "bell";
    public const string DeviceSetup = "device-setup";
    public const string Advanced = "advanced";
    public const string Lock = "lock";
    public const string Serial = "serial";
    public const string Ethernet = "ethernet";
    public const string Wifi = "wifi";
    public const string AppNotification = "app-notification";
    /// <summary>StaffMember credential actions are Skolenza UI choices, not vendor firmware settings.</summary>
    public const string StaffMemberDeviceCredentials = "staff-member-device-credentials";
    public const string StaffMemberDeviceAccessWindows = "staff-member-device-access-windows";
    public const string InstitutionCardInventory = "institution-card-inventory";

    private static readonly IReadOnlyList<DeviceDdlOption> YesNo =
    [
        new("0", "No"),
        new("1", "Yes")
    ];

    private static readonly IReadOnlyDictionary<string, IReadOnlyList<DeviceDdlField>> Sections =
        new Dictionary<string, IReadOnlyList<DeviceDdlField>>(StringComparer.OrdinalIgnoreCase)
        {
            [Time] =
            [
                Field("timeFormat", "Clock format", ("0", "24-hour"), ("1", "12-hour")),
                Field("dateFormat", "Date format", ("0", "Year / Month / Day"), ("1", "Month / Day / Year"), ("2", "Day / Month / Year")),
                Field("networkTimeEnabled", "Use network time (NTP)", YesNo),
                Field("timeZone", "Time zone", TimeZones())
            ],
            [Bell] =
            [
                Field("ringStyle", "Bell ring pattern", ("0", "Continuous tone"), ("1", "Interrupted tone")),
                Field("bellOutput", "Bell output", ("0", "Disabled"), ("1", "Bell"))
            ],
            [DeviceSetup] =
            [
                Field("language", "Device language", ("0", "English"), ("24", "Hindi")),
                Field("screenWakeUpMethod", "Wake screen with", ("0", "Face detection"), ("1", "Touch")),
                Field("resultDisplayStyle", "Result display style", ("0", "Default"), ("1", "Gym style"), ("2", "Message bar")),
                Field("faceRecognitionDistance", "Face recognition distance", ("0", "Short"), ("1", "Medium"), ("2", "Long"))
            ],
            [Advanced] =
            [
                Field("verificationMode", "Verification method", ("0", "Any available method"), ("10", "Card + Face"), ("9", "Face + PIN"), ("14", "Face + Card or PIN"), ("8", "Face only"), ("11", "Card + PIN"), ("3", "Card only"), ("2", "PIN only")),
                Field("qrCodeMode", "QR code mode", ("0", "Disabled"), ("1", "Visitor QR code"), ("2", "Card to QR code"), ("3", "Server QR code")),
                Field("fillLightMode", "Face fill light", ("0", "Automatic"), ("1", "Always off"), ("2", "Always on"))
            ],
            [Lock] =
            [
                Field("doorSensorMode", "Door sensor mode", ("0", "Normally grounded (NG)"), ("1", "Normally closed (NC)"), ("2", "Normally open (NO)")),
                Field("antiPassbackMode", "Anti-passback direction", ("0", "Disabled"), ("1", "Entry"), ("2", "Exit"), ("3", "Both directions")),
                Field("wiegandOutput", "Wiegand output content", ("0", "User ID"), ("1", "Card number"), ("2", "Management ID + user ID"), ("3", "Group ID"), ("4", "Group ID + user ID")),
                Field("wiegandFormat", "Wiegand bit format", ("0", "26-bit"), ("1", "34-bit"), ("2", "42-bit"), ("3", "50-bit"), ("4", "58-bit"), ("5", "64-bit")),
                Field("cardDisplayFormat", "Card display format", ("0", "Decimal"), ("1", "Wiegand"), ("2", "Hexadecimal"))
            ],
            [Serial] =
            [
                Field("baudRate", "Serial baud rate", ("0", "9,600"), ("1", "19,200"), ("2", "38,400"), ("3", "57,600"), ("4", "115,200")),
                Field("serialFunction", "Serial function", ("0", "Disabled"), ("1", "Printer"), ("2", "Send JSON"))
            ],
            [Ethernet] =
            [
                Field("dhcpEnabled", "Use DHCP", YesNo)
            ],
            [Wifi] =
            [
                Field("dhcpEnabled", "Use DHCP", YesNo)
            ],
            [AppNotification] =
            [
                Field("appNotificationEnabled", "Enable app notifications", YesNo),
                Field("notificationType", "Notification frequency", ("0", "All notifications"), ("1", "Each attendance punch"), ("2", "Summary"), ("3", "Disabled"))
            ],
            [StaffMemberDeviceCredentials] =
            [
                Field("credentialType", "Credential to remove", ("1", "Face biometric"), ("2", "Access card"), ("3", "Device PIN"))
            ],
            [StaffMemberDeviceAccessWindows] =
            [
                Field("dayOfWeek", "Working access day", ("1", "Monday"), ("2", "Tuesday"), ("3", "Wednesday"), ("4", "Thursday"), ("5", "Friday"), ("6", "Saturday"), ("7", "Sunday"))
            ],
            [InstitutionCardInventory] =
            [
                Field("taxTreatment", "Purchase tax treatment", ("1", "India — intra-state (CGST + SGST)"), ("2", "India — inter-state (IGST)"), ("3", "Import (customs duty / import tax)"), ("4", "Foreign local tax")),
                Field("cardStatus", "Inventory lifecycle filter", ("1", "Available"), ("2", "Assigned"), ("3", "Blocked"), ("4", "Lost"), ("5", "Returned"), ("6", "Retired"))
            ]
        };

    /// <summary>Returns the option groups for the requested UI section.</summary>
    public static IReadOnlyList<DeviceDdlField> GetSection(string section) =>
        !string.IsNullOrWhiteSpace(section) && Sections.TryGetValue(section.Trim(), out var fields)
            ? fields
            : throw new ArgumentOutOfRangeException(nameof(section), "The requested device dropdown section is not supported.");

    /// <summary>Returns all static UI section names.</summary>
    public static IReadOnlyCollection<string> GetSectionNames() => Sections.Keys.ToArray();

    private static DeviceDdlField Field(string key, string label, params (string Value, string Label)[] options) =>
        new(key, label, options.Select(option => new DeviceDdlOption(option.Value, option.Label)).ToArray());

    private static DeviceDdlField Field(string key, string label, IReadOnlyList<DeviceDdlOption> options) =>
        new(key, label, options);

    private static IReadOnlyList<DeviceDdlOption> TimeZones()
    {
        var options = new List<DeviceDdlOption>();
        for (var offset = 0; offset <= 12; offset++)
        {
            options.Add(new DeviceDdlOption(offset.ToString(), $"UTC+{offset}"));
        }

        for (var offset = 1; offset <= 12; offset++)
        {
            options.Add(new DeviceDdlOption((12 + offset).ToString(), $"UTC-{offset}"));
        }

        options.AddRange(
        [
            new("25", "UTC+3:30"),
            new("26", "UTC+4:30"),
            new("27", "UTC+5:30"),
            new("28", "UTC+5:45"),
            new("29", "UTC+6:30"),
            new("30", "UTC+9:30"),
            new("31", "UTC-3:30"),
            new("32", "UTC-4:30")
        ]);
        return options;
    }
}
