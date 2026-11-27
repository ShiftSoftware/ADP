using ShiftSoftware.ADP.Cases.Data.Flow;
using ShiftSoftware.ADP.Cases.Shared.Flow;
using ShiftSoftware.ShiftEntity.Model;
using Xunit;
using static ShiftSoftware.ADP.Cases.Shared.Tests.TestReports;

namespace ShiftSoftware.ADP.Cases.Shared.Tests;

public class CaseFlowTests
{
    private class Line : CaseStatusHistoryBase<Line, Status>;

    private class Report : ICase<Status>, ICaseWithHistory<Line>
    {
        public Status? Status { get; set; }
        public ICollection<Line> StatusHistory { get; } = new List<Line>();
        public string GetCaseIdentifier() => "7";
    }

    private static readonly CaseActor Dealer = new(1, "Dana", IsDistributor: false);
    private static readonly CaseActor Distributor = new(2, "Omar", IsDistributor: true);

    private static readonly CaseFlow<Report, Line, Status, Trigger> Flow = new(Definition);

    private static Report In(Status status) => new() { Status = status };

    [Theory]
    [InlineData(Status.Draft, Status.Draft)]
    [InlineData(null, Status.New)]
    public void Raising_a_case_opens_its_history(Status? requested, Status opened)
    {
        var report = new Report();

        Flow.Raise(report, requested, Dealer);

        Assert.Equal(opened, report.Status);
        AssertLine(report, null, opened, null, Dealer);
    }

    [Fact]
    public void Saving_moves_a_case_only_when_it_sends_a_draft_in()
    {
        var report = In(Status.Draft);
        Flow.Save(report, Flow.Resolve(report, Status.Draft, Dealer), Dealer);
        Assert.Empty(report.StatusHistory);

        Flow.Save(report, Flow.Resolve(report, Status.New, Dealer), Dealer);

        Assert.Equal(Status.New, report.Status);
        AssertLine(report, Status.Draft, Status.New, null, Dealer);
        Assert.Throws<ShiftEntityException>(() => Flow.Resolve(report, Status.Approved, Distributor));
    }

    [Fact]
    public void An_action_moves_the_case_and_its_comment_goes_on_the_line()
    {
        var report = In(Status.New);

        Flow.Act(report, Trigger.Return, "  add the photos ", Distributor);

        Assert.Equal(Status.Returned, report.Status);
        AssertLine(report, Status.New, Status.Returned, "add the photos", Distributor);
    }

    [Fact]
    public void The_other_sides_action_and_an_illegal_move_are_refused_and_change_nothing()
    {
        var report = In(Status.Approved);

        Assert.Equal(403, Assert.Throws<ShiftEntityException>(() => Flow.Act(report, Trigger.Approve, null, Dealer)).HttpStatusCode);
        Assert.Throws<ShiftEntityException>(() => Flow.Act(report, Trigger.Approve, null, Distributor));

        Assert.Equal(Status.Approved, report.Status);
        Assert.Empty(report.StatusHistory);
    }

    [Fact]
    public void An_action_on_several_cases_is_all_or_none()
    {
        Report[] mixed = [In(Status.New), In(Status.Approved)];

        var refused = Assert.Throws<ShiftEntityException>(() => Flow.ActOnAll(mixed, Trigger.Approve, null, Distributor));
        Assert.Single(refused.Message.SubMessages!);
        Assert.Equal([Status.New, Status.Approved], mixed.Select(x => x.Status!.Value));
        Assert.All(mixed, x => Assert.Empty(x.StatusHistory));

        Report[] legal = [In(Status.New), In(Status.New)];
        Flow.ActOnAll(legal, Trigger.Approve, "ok", Distributor);
        Assert.All(legal, x => AssertLine(x, Status.New, Status.Approved, "ok", Distributor));

        Assert.Throws<ShiftEntityException>(() => Flow.ActOnAll([], Trigger.Approve, null, Distributor));
    }

    [Fact]
    public void A_note_is_a_line_that_moves_nothing()
    {
        var report = In(Status.New);

        Flow.AddNote(report, " waiting for the part ", Dealer);

        Assert.Equal(Status.New, report.Status);
        AssertLine(report, Status.New, Status.New, "waiting for the part", Dealer);
        Assert.True(report.StatusHistory.Single().IsNote);

        Assert.Throws<ShiftEntityException>(() => Flow.AddNote(report, "  ", Dealer));
        Assert.Equal(403, Assert.Throws<ShiftEntityException>(() => Flow.AddNote(In(Status.Approved), "late", Dealer)).HttpStatusCode);
    }

    private static void AssertLine(Report report, Status? from, Status to, string? comment, CaseActor actor)
    {
        var line = Assert.Single(report.StatusHistory);

        Assert.Equal((from, to, comment), (line.FromStatus, line.ToStatus, line.Comment));
        Assert.Equal((actor.UserID, actor.UserName, actor.Role), (line.ActorUserID, line.ActorUserName, line.ActorRole));
        Assert.NotEqual(default, line.Timestamp);
    }
}
