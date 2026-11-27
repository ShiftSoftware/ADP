using ShiftSoftware.ADP.Cases.Shared.Enums;
using ShiftSoftware.ADP.Cases.Shared.Services;
using ShiftSoftware.ADP.Models.Enums;
using ShiftSoftware.ShiftEntity.Model;
using Xunit;

namespace ShiftSoftware.ADP.Cases.Shared.Tests;

public class ClaimActionAccessTests
{
    [Fact]
    public void Submitting_is_the_only_action_open_to_a_dealer()
    {
        var open = Enum.GetValues<UpdateStatusActionTypes>().Where(x => !ClaimActionAccess.IsDistributorOnly(x));

        Assert.Equal([UpdateStatusActionTypes.SubmitToDistributor], open);
    }

    [Fact]
    public void A_dealer_is_refused_the_distributors_actions()
    {
        foreach (var action in Enum.GetValues<UpdateStatusActionTypes>())
        {
            ClaimActionAccess.RequireAllowed(action, isDistributor: true);

            if (ClaimActionAccess.IsDistributorOnly(action))
                AssertRefused(action, isDistributor: false);
            else
                ClaimActionAccess.RequireAllowed(action, isDistributor: false);
        }
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(5)]
    [InlineData(99)]
    public void A_value_outside_the_enum_is_refused_to_both_sides(int action)
    {
        AssertRefused((UpdateStatusActionTypes)action, isDistributor: false);
        AssertRefused((UpdateStatusActionTypes)action, isDistributor: true);
    }

    /// <summary>The buttons offer exactly what the server takes.</summary>
    [Fact]
    public void The_form_actions_are_the_ones_the_role_check_and_the_service_accept()
    {
        ClaimStatus?[] statuses = [null, .. Enum.GetValues<ClaimStatus>().Select(x => (ClaimStatus?)x)];

        foreach (var status in statuses)
        {
            foreach (var isDistributor in new[] { false, true })
            {
                var accepted = Enum.GetValues<UpdateStatusActionTypes>()
                    .Where(x => ClaimActionAccess.IsFormAction(x) && IsAccepted(x, status, isDistributor));

                Assert.Equal(accepted, ClaimActionAccess.ActionsFrom(status, isDistributor));
            }
        }

        Assert.DoesNotContain(UpdateStatusActionTypes.AssignInvoiceNo, Enum.GetValues<UpdateStatusActionTypes>().Where(ClaimActionAccess.IsFormAction));
        Assert.DoesNotContain(UpdateStatusActionTypes.ManufacturerPaid, Enum.GetValues<UpdateStatusActionTypes>().Where(ClaimActionAccess.IsFormAction));
    }

    private static bool IsAccepted(UpdateStatusActionTypes action, ClaimStatus? status, bool isDistributor)
    {
        try
        {
            ClaimActionAccess.RequireAllowed(action, isDistributor);
            new SharedClaimService().UpdateClaimStatus([new Claim { ClaimStatus = status }], action, null);
            return true;
        }
        catch (ShiftEntityException)
        {
            return false;
        }
    }

    private static void AssertRefused(UpdateStatusActionTypes action, bool isDistributor)
    {
        var refused = Assert.Throws<ShiftEntityException>(() => ClaimActionAccess.RequireAllowed(action, isDistributor));

        Assert.Equal(403, refused.HttpStatusCode);
        Assert.Equal("Operation Prevented", refused.Message.Title);
    }

    private class Claim : IClaim
    {
        public ClaimStatus? ClaimStatus { get; set; }
        public string? DistributorErrorMessage { get; set; }
        public DateTime? ProcessDate { get; set; }
        public string? InvoiceNo { get; set; }
        public WarrantyManufacturerClaimStatus? ManufacturerStatus { get; set; }
        public string GetClaimIdentifier() => "1";
    }
}
