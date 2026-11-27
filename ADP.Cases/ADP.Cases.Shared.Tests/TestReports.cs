using ShiftSoftware.ADP.Cases.Shared.Flow;

namespace ShiftSoftware.ADP.Cases.Shared.Tests;

/// <summary>A tiny case type for the tests of the shared case code.</summary>
public static class TestReports
{
    public enum Status { Draft, New, Returned, Approved }
    public enum Trigger { Submit, Approve, Return, Resend }

    public static readonly CaseDefinition<Status, Trigger> Definition = new()
    {
        Transitions =
        [
            new(Status.Draft, Trigger.Submit, Status.New, CaseRoles.Dealer),
            new(Status.New, Trigger.Approve, Status.Approved, CaseRoles.Distributor),
            new(Status.New, Trigger.Return, Status.Returned, CaseRoles.Distributor),
            new(Status.Returned, Trigger.Resend, Status.New, CaseRoles.Dealer),
            new(Status.Approved, Trigger.Return, Status.Returned, CaseRoles.Distributor),
        ],
        Initial = Status.New,
        Draft = Status.Draft,
        Submit = Trigger.Submit,
        ClosedToDealer = new HashSet<Status> { Status.Approved },
        Noun = "report",
    };
}
