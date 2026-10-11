using skolenza.application.Common.Helpers;
using NUnit.Framework;

namespace skolenza.automationtests.Unit;

public class StaffMemberBankAccountTypeTests
{
    [TestCase("Saving", "saving")]
    [TestCase("SAVINGS", "saving")]
    [TestCase("Current", "current")]
    [TestCase("Salary", "salary")]
    public void Account_type_is_normalized_to_database_value(string input, string expected)
    {
        Assert.That(AccountTypeHelper.Normalize(input), Is.EqualTo(expected));
    }
}
