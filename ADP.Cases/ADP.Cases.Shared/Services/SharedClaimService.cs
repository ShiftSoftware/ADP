using ShiftSoftware.ADP.Cases.Shared.Enums;
using ShiftSoftware.ADP.Models.Enums;
using ShiftSoftware.ShiftEntity.Model;

namespace ShiftSoftware.ADP.Cases.Shared.Services;

/// <summary>
/// The claim status state machine shared by all claim-type cases (WarrantyClaim + ItemClaim).
/// Moved VERBATIM from the original host application's <c>Services.Data.Services.SharedClaimService</c> (Phase 2, D16) — the
/// method body is byte-faithful and protected by characterization tests in ADP.Cases.Shared.Tests.
/// </summary>
/// <remarks>
/// LOAD-BEARING SEMANTICS (do not "fix"):
/// <list type="bullet">
/// <item><b>Mutate-then-throw:</b> status mutations are applied in-memory to EVERY item BEFORE the
/// aggregated validation exception is thrown. Correctness depends on callers persisting only via
/// <c>SaveChangesAsync</c> AFTER this method returns — on exception the DbContext is discarded and
/// nothing is saved. Callers must never save per-item mid-loop.</item>
/// <item><b>Error aggregation:</b> per-claim errors accumulate as sub-messages, capped at 10 with a
/// "+N more claims" summary row, wrapped in a single <see cref="ShiftEntityException"/> titled
/// "Error" — deployed list pages render this exact dialog shape.</item>
/// <item>The new declarative <see cref="Workflow.CaseWorkflow{TStatus, TTrigger}"/> engine is
/// validate-first instead; converging this service onto it is a later, test-protected refactor.</item>
/// </list>
/// </remarks>
public class SharedClaimService
{
    public void UpdateClaimStatus(List<IClaim> items, UpdateStatusActionTypes actionType, string? inputText)
    {
        var subErrors = new List<Message>();

        foreach (var item in items)
        {
            var claimNo = item.GetClaimIdentifier();

            if (actionType == UpdateStatusActionTypes.SubmitToDistributor)
            {
                if (item.ClaimStatus != ClaimStatus.Draft && item.ClaimStatus != ClaimStatus.RejectedWithError)
                {
                    //for (int i = 0; i < 200; i++)
                    {
                        subErrors.Add(new Message($"Claim #{claimNo}", $"Only claims with [{ClaimStatus.Draft.Describe()}] & [{ClaimStatus.RejectedWithError.Describe()}] status can be submitted."));
                    }
                }

                item.ProcessDate = DateTime.UtcNow;
                item.ClaimStatus = ClaimStatus.PendingProcess;
            }

            if (actionType == UpdateStatusActionTypes.DistributorAccepted)
            {
                if (item.ClaimStatus == ClaimStatus.Draft)
                {
                    subErrors.Add(new Message($"Claim #{claimNo}", $"has [{ClaimStatus.Draft.Describe()}] status and can not be accepted."));
                }

                if (item.ClaimStatus == ClaimStatus.Certified)
                {
                    subErrors.Add(new Message($"Claim #{claimNo}", $"has [{ClaimStatus.Certified.Describe()}] status and can not be accepted."));
                }

                if (item.ClaimStatus == ClaimStatus.Invoiced)
                {
                    subErrors.Add(new Message($"Claim #{claimNo}", $"has [{ClaimStatus.Invoiced.Describe()}] status and can not be accepted."));
                }

                item.ClaimStatus = ClaimStatus.Accepted;
            }

            if (actionType == UpdateStatusActionTypes.DistributorError)
            {
                if (item.ClaimStatus == ClaimStatus.Draft)
                {
                    subErrors.Add(new Message($"Claim #{claimNo}", $"has [{ClaimStatus.Draft.Describe()}] status and can not be marked as error."));
                }

                if (item.ClaimStatus == ClaimStatus.Certified)
                {
                    subErrors.Add(new Message($"Claim #{claimNo}", $"has [{ClaimStatus.Certified.Describe()}] status and can not be marked as error."));
                }

                if (item.ClaimStatus == ClaimStatus.Invoiced)
                {
                    subErrors.Add(new Message($"Claim #{claimNo}", $"has [{ClaimStatus.Invoiced.Describe()}] status and can not be marked as error."));
                }

                item.ClaimStatus = ClaimStatus.RejectedWithError;

                //Reset Manufacturer status to Prevent exporting Invalid Claims
                item.ManufacturerStatus = WarrantyManufacturerClaimStatus.NA;
                item.DistributorErrorMessage = inputText;
            }

            if (actionType == UpdateStatusActionTypes.DistributorRejected)
            {
                if (item.ClaimStatus == ClaimStatus.Draft)
                {
                    subErrors.Add(new Message($"Claim #{claimNo}", $"has [{ClaimStatus.Draft.Describe()}] status and can not be rejected."));
                }

                if (item.ClaimStatus == ClaimStatus.Certified)
                {
                    subErrors.Add(new Message($"Claim #{claimNo}", $"has [{ClaimStatus.Certified.Describe()}] status and can not be rejected."));
                }

                if (item.ClaimStatus == ClaimStatus.Invoiced)
                {
                    subErrors.Add(new Message($"Claim #{claimNo}", $"has [{ClaimStatus.Invoiced.Describe()}] status and can not be rejected."));
                }

                item.ClaimStatus = ClaimStatus.RejectedPermanently;
            }

            if (actionType == UpdateStatusActionTypes.AssignInvoiceNo)
            {
                item.InvoiceNo = inputText;
            }

            if (actionType == UpdateStatusActionTypes.ManufacturerRejected)
            {
                if (item.ManufacturerStatus == WarrantyManufacturerClaimStatus.Paid)
                {
                    subErrors.Add(new Message($"Claim #{claimNo}", $"has [{WarrantyManufacturerClaimStatus.Paid.Describe()}] status and can not be rejected."));
                }

                item.ManufacturerStatus = WarrantyManufacturerClaimStatus.Rejected;
            }

            //if (actionType == UpdateStatusActionTypes.ManufacturerExportToggle)
            //{
            //    if (item.ManufacturerStatus == WarrantyManufacturerClaimStatus.Paid)
            //    {
            //        subErrors.Add(new Message($"Claim #{claimNo}", $"has [{WarrantyManufacturerClaimStatus.Paid.Describe()}] status and can not be exported."));
            //    }

            //    item.ManufacturerStatus = WarrantyManufacturerClaimStatus.Exportable;
            //}

            if (actionType == UpdateStatusActionTypes.ManufacturerPaid)
            {
                if (item.ManufacturerStatus == WarrantyManufacturerClaimStatus.Rejected)
                {
                    subErrors.Add(new Message($"Claim #{claimNo}", $"has [{WarrantyManufacturerClaimStatus.Rejected.Describe()}] status and can not be paid."));
                }

                item.ManufacturerStatus = WarrantyManufacturerClaimStatus.Paid;
            }

            if (actionType == UpdateStatusActionTypes.ManufacturerReset)
            {
                item.ManufacturerStatus = WarrantyManufacturerClaimStatus.NA;
            }
        }

        if (subErrors.Count > 0)
        {
            var errorLimit = 10;

            if (subErrors.Count > errorLimit)
            {
                var remainingErrorCountAfterTakingErrorLimit = subErrors.Count - errorLimit;

                subErrors = subErrors.Take(errorLimit).ToList();

                subErrors.Add(new Message($"+{remainingErrorCountAfterTakingErrorLimit} more claims".ToString(), $"omitted in this warning dialog"));
            }

            throw new ShiftEntityException(
                new Message(
                    "Error",
                    "",
                    subErrors
                ));
        }
    }
}
