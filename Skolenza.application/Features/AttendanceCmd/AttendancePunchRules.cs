using skolenza.domain.Entity;

namespace skolenza.application.Features.AttendanceCmd;

/// <summary>Pure attendance state rules shared by runtime validation and automated tests.</summary>
public static class AttendancePunchRules
{
    public static bool IsAllowedTransition(AttendancePunchAction? lastAction,
        AttendancePunchAction requestedAction)
    {
        return requestedAction switch
        {
            AttendancePunchAction.CheckIn => lastAction != AttendancePunchAction.CheckIn,
            AttendancePunchAction.CheckOut => lastAction == AttendancePunchAction.CheckIn,
            _ => false
        };
    }
}
