using ShiftSoftware.ADP.Cases.Shared.Flow;
using ShiftSoftware.ADP.Cases.Shared.Validation;
using Xunit;
using static ShiftSoftware.ADP.Cases.Shared.Tests.TestReports;

namespace ShiftSoftware.ADP.Cases.Shared.Tests;

public class CaseDefinitionTests
{
    private static readonly CaseActorContext Dealer = new("1", IsElevated: false, CaseRoles.Dealer);
    private static readonly CaseActorContext Distributor = new("2", IsElevated: true, CaseRoles.Distributor);

    [Theory]
    [InlineData(Status.New, CaseRoles.Distributor, new[] { Trigger.Approve, Trigger.Return })]
    [InlineData(Status.Approved, CaseRoles.Distributor, new[] { Trigger.Return })]
    [InlineData(Status.Returned, CaseRoles.Distributor, new Trigger[0])]
    [InlineData(Status.Returned, CaseRoles.Dealer, new[] { Trigger.Resend })]
    [InlineData(Status.New, CaseRoles.Dealer, new Trigger[0])]
    [InlineData(Status.Draft, CaseRoles.Dealer, new Trigger[0])]
    [InlineData(null, CaseRoles.Distributor, new Trigger[0])]
    [InlineData(Status.New, null, new Trigger[0])]
    public void A_status_offers_each_side_its_moves_in_the_table(Status? status, string? role, Trigger[] expected) =>
        Assert.Equal(expected, Definition.ActionsFrom(status, role));

    [Theory]
    [InlineData(Trigger.Approve, CaseRoles.Distributor, true)]
    [InlineData(Trigger.Approve, CaseRoles.Dealer, false)]
    [InlineData(Trigger.Resend, CaseRoles.Dealer, true)]
    [InlineData(Trigger.Resend, CaseRoles.Distributor, false)]
    [InlineData(Trigger.Submit, CaseRoles.Dealer, false)]
    [InlineData(Trigger.Approve, null, false)]
    public void An_action_belongs_to_one_side_and_submitting_is_never_one(Trigger trigger, string? role, bool expected) =>
        Assert.Equal(expected, Definition.IsActionOf(trigger, role));

    [Theory]
    [InlineData(Status.New, CaseRoles.Dealer, true)]
    [InlineData(Status.Returned, CaseRoles.Dealer, true)]
    [InlineData(Status.Approved, CaseRoles.Dealer, false)]
    [InlineData(Status.Approved, CaseRoles.Distributor, true)]
    [InlineData(Status.Draft, CaseRoles.Dealer, false)]
    [InlineData(Status.Draft, CaseRoles.Distributor, false)]
    [InlineData(Status.New, null, false)]
    [InlineData(null, CaseRoles.Distributor, false)]
    public void Who_may_add_a_note(Status? status, string? role, bool expected) =>
        Assert.Equal(expected, Definition.CanAddNote(status, role));

    [Fact]
    public void A_new_case_opens_as_a_draft_only_for_a_dealer()
    {
        Assert.Same(CaseSaveResolution<Trigger>.NoStatusChange, Definition.ResolveSave(null, Status.Draft, Dealer));
        Assert.Same(CaseSaveResolution<Trigger>.NoStatusChange, Definition.ResolveSave(null, null, Distributor));
        Refused("Only a dealer can save", null, Status.Draft, Distributor);
    }

    [Fact]
    public void A_save_that_does_not_ask_for_a_move_does_not_make_one()
    {
        Assert.Same(CaseSaveResolution<Trigger>.NoStatusChange, Definition.ResolveSave(Status.New, Status.New, Dealer));
        Assert.Same(CaseSaveResolution<Trigger>.NoStatusChange, Definition.ResolveSave(Status.New, null, Dealer));
        Assert.Same(CaseSaveResolution<Trigger>.NoStatusChange, Definition.ResolveSave(Status.Approved, Status.Approved, Distributor));
    }

    [Fact]
    public void A_dealer_sends_a_draft_in_with_a_save() =>
        Assert.Equal(Trigger.Submit, Definition.ResolveSave(Status.Draft, Status.New, Dealer).Trigger);

    [Fact]
    public void A_save_that_asks_for_any_other_move_is_refused()
    {
        Refused("no longer be edited by the dealer", Status.Approved, Status.Approved, Dealer);
        Refused("cannot be put back into draft", Status.New, Status.Draft, Dealer);
        Refused("changed with the buttons", Status.New, Status.Approved, Dealer);
        Refused("changed with the buttons", Status.New, Status.Approved, Distributor);
        Refused("changed with the buttons", Status.Draft, Status.New, Distributor);
    }

    private static void Refused(string message, Status? current, Status? requested, CaseActorContext actor)
    {
        var resolution = Definition.ResolveSave(current, requested, actor);

        Assert.True(resolution.IsRejected);
        Assert.Null(resolution.Trigger);
        Assert.Contains(message, resolution.Rejection!.Body);
    }
}
