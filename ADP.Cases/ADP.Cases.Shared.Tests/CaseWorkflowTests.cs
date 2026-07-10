using ShiftSoftware.ADP.Cases.Shared;
using ShiftSoftware.ADP.Cases.Shared.Workflow;
using ShiftSoftware.ShiftEntity.Model;
using Xunit;

namespace ShiftSoftware.ADP.Cases.Shared.Tests;

/// <summary>
/// Behavior tests for the new declarative <see cref="CaseWorkflow{TStatus, TTrigger}"/> engine,
/// exercised with an SKR-shaped transition table (skr-dtr requirements §4.3.4).
/// Key contrast with the legacy claim engine: VALIDATE-FIRST — illegal triggers never mutate.
/// </summary>
public class CaseWorkflowTests
{
    public enum TestStatus { New, AmendRequest, DealerResponse, Approved, Denied }
    public enum TestTrigger { Approve, Deny, RequestAmendment, DealerSave }

    private const string Distributor = "Distributor";
    private const string Dealer = "Dealer";

    private class TestCase : ICase<TestStatus>
    {
        public TestStatus? Status { get; set; }
        public string GetCaseIdentifier() => "SKR-42";
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }

    private static readonly DateTimeOffset Now = new(2026, 7, 5, 12, 0, 0, TimeSpan.Zero);

    // The SKR state table (§4.3.4) + re-open rules, as data.
    private static CaseWorkflow<TestStatus, TestTrigger> CreateWorkflow() => new(
        [
            new(TestStatus.New, TestTrigger.Approve, TestStatus.Approved, Distributor),
            new(TestStatus.New, TestTrigger.Deny, TestStatus.Denied, Distributor),
            new(TestStatus.New, TestTrigger.RequestAmendment, TestStatus.AmendRequest, Distributor),
            new(TestStatus.AmendRequest, TestTrigger.DealerSave, TestStatus.DealerResponse, Dealer),
            new(TestStatus.DealerResponse, TestTrigger.Approve, TestStatus.Approved, Distributor),
            new(TestStatus.DealerResponse, TestTrigger.Deny, TestStatus.Denied, Distributor),
            new(TestStatus.Approved, TestTrigger.RequestAmendment, TestStatus.AmendRequest, Distributor),
            new(TestStatus.Denied, TestTrigger.RequestAmendment, TestStatus.AmendRequest, Distributor),
        ],
        new FixedTimeProvider(Now));

    [Fact]
    public void Apply_LegalTransition_MutatesStatus_AndReturnsHistoryEntry()
    {
        var workflow = CreateWorkflow();
        var @case = new TestCase { Status = TestStatus.New };

        var entry = workflow.Apply(@case, TestTrigger.RequestAmendment, actorUserId: "user-7", comment: "please add photos");

        Assert.Equal(TestStatus.AmendRequest, @case.Status);
        Assert.Equal(TestStatus.New, entry.FromStatus);
        Assert.Equal(TestStatus.AmendRequest, entry.ToStatus);
        Assert.Equal(Now, entry.Timestamp);
        Assert.Equal("user-7", entry.ActorUserId);
        Assert.Equal("please add photos", entry.Comment);
        Assert.False(entry.IsNote);
    }

    [Fact]
    public void Apply_IllegalTransition_Throws_AndDoesNotMutate()
    {
        var workflow = CreateWorkflow();
        var @case = new TestCase { Status = TestStatus.Approved };

        var ex = Assert.Throws<ShiftEntityException>(() => workflow.Apply(@case, TestTrigger.DealerSave));

        Assert.Equal(TestStatus.Approved, @case.Status); // validate-first: unchanged
        Assert.Equal("Error", ex.Message.Title);
        var sub = Assert.Single(ex.Message.SubMessages!);
        Assert.Equal("Case #SKR-42", sub.Title);
    }

    [Fact]
    public void Apply_WithNullStatus_Throws_AndDoesNotMutate()
    {
        var workflow = CreateWorkflow();
        var @case = new TestCase { Status = null };

        Assert.Throws<ShiftEntityException>(() => workflow.Apply(@case, TestTrigger.Approve));

        Assert.Null(@case.Status);
    }

    [Theory]
    [InlineData(TestStatus.New, TestTrigger.Approve, true)]
    [InlineData(TestStatus.New, TestTrigger.DealerSave, false)]
    [InlineData(TestStatus.AmendRequest, TestTrigger.DealerSave, true)]
    [InlineData(TestStatus.AmendRequest, TestTrigger.Approve, false)]
    [InlineData(TestStatus.Denied, TestTrigger.RequestAmendment, true)]
    [InlineData(TestStatus.Denied, TestTrigger.Deny, false)]
    public void CanApply_MatchesTheTransitionTable(TestStatus from, TestTrigger trigger, bool expected)
    {
        Assert.Equal(expected, CreateWorkflow().CanApply(from, trigger));
    }

    [Fact]
    public void AvailableFrom_ListsExactlyTheLegalTriggers()
    {
        var fromNew = CreateWorkflow().AvailableFrom(TestStatus.New).Select(t => t.Trigger).ToList();

        Assert.Equal([TestTrigger.Approve, TestTrigger.Deny, TestTrigger.RequestAmendment], fromNew);
        Assert.Empty(CreateWorkflow().AvailableFrom(null));
    }

    [Fact]
    public void Find_ExposesRequiredRole_AsData()
    {
        var transition = CreateWorkflow().Find(TestStatus.AmendRequest, TestTrigger.DealerSave);

        Assert.NotNull(transition);
        Assert.Equal(Dealer, transition!.RequiredRole);
    }

    [Fact]
    public void AppendNote_ReturnsFromEqualsToEntry_WithoutChangingStatus()
    {
        var workflow = CreateWorkflow();
        var @case = new TestCase { Status = TestStatus.DealerResponse };

        var entry = workflow.AppendNote(@case, "distributor-user", "checked with manufacturer");

        Assert.Equal(TestStatus.DealerResponse, @case.Status);
        Assert.Equal(TestStatus.DealerResponse, entry.FromStatus);
        Assert.Equal(TestStatus.DealerResponse, entry.ToStatus);
        Assert.True(entry.IsNote);
        Assert.Equal(Now, entry.Timestamp);
    }

    [Fact]
    public void AppendNote_OnStatuslessCase_Throws()
    {
        Assert.Throws<InvalidOperationException>(() =>
            CreateWorkflow().AppendNote(new TestCase { Status = null }, "u", "c"));
    }

    [Fact]
    public void CaseMessageAggregator_CapsErrors_AndUsesSharedDialogShape()
    {
        var errors = Enumerable.Range(1, 13).Select(i => new Message($"Case #{i}", "bad")).ToList();

        var ex = Assert.Throws<ShiftEntityException>(() => CaseMessageAggregator.ThrowIfAny(errors, "cases"));

        Assert.Equal("Error", ex.Message.Title);
        Assert.Equal(11, ex.Message.SubMessages!.Count);
        Assert.Equal("+3 more cases", ex.Message.SubMessages[10].Title);

        // Empty list is a no-op.
        CaseMessageAggregator.ThrowIfAny([], "cases");
    }
}
