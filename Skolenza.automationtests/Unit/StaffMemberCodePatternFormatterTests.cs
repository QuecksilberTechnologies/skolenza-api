using skolenza.application.Common.Helpers;
using skolenza.domain.Entity;
using NUnit.Framework;

namespace skolenza.automationtests.Unit;

[TestFixture]
[Category("StaffMemberBulkCode")]
public sealed class StaffMemberCodePatternFormatterTests
{
    #region Joining Date And Existing Pattern

    [Test]
    public void Original_joining_date_supplies_year_and_month()
    {
        var pattern = Pattern();
        pattern.IncludeYear = true;
        pattern.IncludeMonth = true;

        var code = StaffMemberCodePatternFormatter.Format(pattern, new DateTime(2022, 3, 15), null, 145);

        Assert.That(code, Is.EqualTo("QT/2022/MAR/0145"));
    }

    [TestCase("/", "QT/2022/MAR/0145")]
    [TestCase("", "QT2022MAR0145")]
    public void Matching_code_preserves_running_number_with_or_without_separator(string separator, string code)
    {
        var pattern = Pattern();
        pattern.Separator = separator;
        pattern.IncludeYear = true;
        pattern.IncludeMonth = true;

        var matches = StaffMemberCodePatternFormatter.TryGetRunningNumber(
            pattern, new DateTime(2022, 3, 15), null, code, out var number);

        Assert.Multiple(() =>
        {
            Assert.That(matches, Is.True);
            Assert.That(number, Is.EqualTo(145));
        });
    }

    [TestCase("QT/0000")]
    [TestCase("QT/145")]
    [TestCase("QT/00145")]
    [TestCase("OTHER/0145")]
    [TestCase("QT/+145")]
    [TestCase("QT/0145 ")]
    public void Nonmatching_codes_are_not_silently_normalized(string code)
    {
        Assert.That(StaffMemberCodePatternFormatter.TryGetRunningNumber(
            Pattern(), default, null, code, out _), Is.False);
    }

    [Test]
    public void Numeric_prefix_is_not_part_of_the_sequence()
    {
        var pattern = Pattern();
        pattern.Prefix = "QT99";
        pattern.Separator = "";

        Assert.That(StaffMemberCodePatternFormatter.TryGetRunningNumber(
            pattern, default, null, "QT990145", out var number), Is.True);
        Assert.That(number, Is.EqualTo(145));
    }

    [Test]
    public void Highest_preserved_code_determines_next_number()
    {
        Assert.That(StaffMemberCodePatternFormatter.NextRunningNumber(new[] { 200, 145 }), Is.EqualTo(201));
    }

    [Test]
    public void Sequence_overflow_is_rejected()
    {
        Assert.Throws<OverflowException>(() =>
            StaffMemberCodePatternFormatter.NextRunningNumber(new[] { int.MaxValue }));
    }

    [Test]
    public void Date_dependent_pattern_rejects_missing_joining_date()
    {
        var pattern = Pattern();
        pattern.IncludeYear = true;
        Assert.Throws<ArgumentException>(() => StaffMemberCodePatternFormatter.Format(pattern, default, null, 1));
    }

    [Test]
    public void Department_dependent_pattern_rejects_missing_department()
    {
        var pattern = Pattern();
        pattern.IncludeDepartment = true;
        Assert.Throws<ArgumentException>(() => StaffMemberCodePatternFormatter.Format(pattern, default, null, 1));
    }

    private static StaffMemberCodePattern Pattern()
    {
        return new StaffMemberCodePattern
        {
            Prefix = "QT",
            Separator = "/",
            RunningNumberLength = 4
        };
    }

    [Test]
    public void Pattern_preview_recodes_admin_and_other_staffMember_without_mutating_them()
    {
        var current = Pattern();
        var proposed = Pattern();
        proposed.Prefix = "EMP";
        proposed.IncludeYear = true;
        var staffMembers = new[] { StaffMember(1, "QT/0145"), StaffMember(2, "QT/0200") };

        var preview = StaffMemberCodePatternFormatter.PreviewChanges(8, current, proposed, staffMembers);

        Assert.Multiple(() =>
        {
            Assert.That(preview.CanCommit, Is.True);
            Assert.That(preview.StaffMembers.Select(row => row.ProposedCode), Is.EqualTo(new[] { "EMP/2022/0145", "EMP/2022/0200" }));
            Assert.That(preview.LastUsedNumber, Is.EqualTo(200));
            Assert.That(staffMembers[0].EmployementCode, Is.EqualTo("QT/0145"));
            Assert.That(preview.Applied, Is.False);
        });
    }

    [Test]
    public void Changed_staffMember_code_invalidates_preview_fingerprint()
    {
        var staffMember = StaffMember(1, "QT/0145");
        var first = StaffMemberCodePatternFormatter.PreviewChanges(8, Pattern(), Pattern(), new[] { staffMember });
        staffMember.EmployementCode = "QT/0146";
        var second = StaffMemberCodePatternFormatter.PreviewChanges(8, Pattern(), Pattern(), new[] { staffMember });
        Assert.That(first.PreviewHash, Is.Not.EqualTo(second.PreviewHash));
    }

    [Test]
    public void Duplicate_proposed_codes_block_the_entire_preview()
    {
        var preview = StaffMemberCodePatternFormatter.PreviewChanges(8, Pattern(), Pattern(),
            new[] { StaffMember(1, "QT/0145"), StaffMember(2, "QT/0145") });
        Assert.That(preview.CanCommit, Is.False);
        Assert.That(preview.StaffMembers.All(row => row.Errors.Count > 0), Is.True);
    }

    [Test]
    public void Foreign_staffMember_blocks_preview()
    {
        var staffMember = StaffMember(1, "QT/0145");
        staffMember.InstitutionId = 9;
        var preview = StaffMemberCodePatternFormatter.PreviewChanges(8, Pattern(), Pattern(), new[] { staffMember });
        Assert.That(preview.CanCommit, Is.False);
    }

    [Test]
    public void Missing_original_date_is_not_replaced_with_today()
    {
        var staffMember = StaffMember(1, "QT/0145");
        staffMember.DateOfOnBoarding = null;
        var proposed = Pattern();
        proposed.IncludeYear = true;
        var preview = StaffMemberCodePatternFormatter.PreviewChanges(8, Pattern(), proposed, new[] { staffMember });
        Assert.That(preview.CanCommit, Is.False);
    }

    private static StaffMember StaffMember(long id, string code)
    {
        return new StaffMember
        {
            Id = id,
            InstitutionId = 8,
            FirstName = "Sample",
            EmployementCode = code,
            DateOfOnBoarding = new DateTime(2022, 3, 15),
            DepartmentId = 6
        };
    }

    [TestCase(2026, true)]
    [TestCase(2025, false)]
    public void Legacy_creation_year_requires_exact_persisted_date(int creationYear, bool allowed)
    {
        var current = Pattern();
        current.IncludeYear = true;
        var staffMember = StaffMember(1, "QT/2026/0001");
        staffMember.DateOfOnBoarding = new DateTime(2018, 7, 24);
        staffMember.AddedDateTime = new DateTime(creationYear, 9, 9);

        var preview = StaffMemberCodePatternFormatter.PreviewChanges(8, current, current, new[] { staffMember });

        Assert.That(preview.CanCommit, Is.EqualTo(allowed));
        if (allowed)
        {
            Assert.That(preview.StaffMembers[0].ProposedCode, Is.EqualTo("QT/2018/0001"));
            staffMember.AddedDateTime = staffMember.AddedDateTime.AddDays(1);
            var changed = StaffMemberCodePatternFormatter.PreviewChanges(8, current, current, new[] { staffMember });
            Assert.That(changed.PreviewHash, Is.Not.EqualTo(preview.PreviewHash));
        }
    }

    #endregion
}
