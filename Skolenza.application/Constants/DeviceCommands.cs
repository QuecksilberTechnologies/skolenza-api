namespace skolenza.application.Constants;

/// <summary>
/// Vendor command names confirmed from the checked-in MQTT Postman collection.
/// This is deliberately a name catalog only: vendor payload fields remain command-specific.
/// </summary>
public static class DeviceCommands
{
    public const string Register = "reg";
    public const string SendLog = "sendlog";
    public const string SendUser = "senduser";
    public const string AddUser = "adduser";
    public const string CancelAddUser = "canceladduser";
    public const string CheckLive = "checklive";
    public const string CheckRegistrationStatus = "checkregstatus";
    public const string CheckUserId = "checkuserid";
    public const string CleanAdmin = "cleanadmin";
    public const string CleanDatabase = "cleandatebase";
    public const string CleanInactiveUser = "cleaninactiveuser";
    public const string CleanLog = "cleanlog";
    public const string CleanLogPhoto = "cleanlogphoto";
    public const string CleanUser = "cleanuser";
    public const string CleanUserLock = "cleanuserlock";
    public const string DeleteUser = "deleteuser";
    public const string DeleteUserLock = "deleteuserlock";
    public const string DisableDevice = "disabledevice";
    public const string EnableDevice = "enabledevice";
    public const string EnableUser = "enableuser";
    public const string ForceOta = "forceota";
    public const string GetAllLog = "getalllog";
    public const string GetAllUsers = "getallusers";
    public const string GetBellTime = "getbelltime";
    public const string GetCompanyName = "getcompanyname";
    public const string GetDepartment = "getdepartment";
    public const string GetDeviceCapabilities = "getdevcap";
    public const string GetDeviceInfo = "getdevinfo";
    public const string GetDeviceLock = "getdevlock";
    public const string GetDirectory = "getdir";
    public const string GetDoorStatus = "getdoorstatus";
    public const string GetFile = "getfile";
    public const string GetHoliday = "getholiday";
    public const string GetNewLog = "getnewlog";
    public const string GetOtaServer = "getotaserver";
    public const string GetRegistration = "getreg";
    public const string GetShift = "getshift";
    public const string GetTime = "gettime";
    public const string GetUnregisteredUserId = "getunuserdid";
    public const string GetUserIds = "getuserids";
    public const string GetUserInfo = "getuserinfo";
    public const string GetUserList = "getuserlist";
    public const string GetUserLock = "getuserlock";
    public const string GetUserName = "getusername";
    public const string GetUserProfile = "getuserprofile";
    public const string InitializeMenu = "initmenu";
    public const string InitializeSystem = "initsys";
    public const string Keypad = "keypad";
    public const string LockControl = "lockctrl";
    public const string OpenDoor = "opendoor";
    public const string Reboot = "reboot";
    public const string SetBellTime = "setbelltime";
    public const string SetCompanyName = "setcompanyname";
    public const string SetDepartment = "setdepartment";
    public const string SetDeviceInfo = "setdevinfo";
    public const string SetDeviceLock = "setdevlock";
    public const string SetHoliday = "setholiday";
    public const string SetOtaServer = "setotaserver";
    public const string SetQuestionnaire = "setquestionnaire";
    public const string SetScreenSaver = "setscreensaver";
    public const string SetShift = "setshift";
    public const string SetTime = "settime";
    public const string SetUserInfo = "setuserinfo";
    public const string SetUserLock = "setuserlock";
    public const string SetUserName = "setusername";
    public const string SetUserProfile = "setuserprofile";
    public const string SetVoice = "setvoice";
    public const string Upgrade = "upgrade";
    public const string Verify = "verify";
    public const string WriteFile = "writefile";
}
