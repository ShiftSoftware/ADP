using System.Net;
using ShiftSoftware.ShiftEntity.Model;

namespace ShiftSoftware.ADP.WarrantyClaims.Data.Repositories;

internal static class FinancialLists
{
    public static ShiftEntityException ReadOnly() => new(
        new Message("Operation Prevented", "The financial lists are read-only."),
        httpStatusCode: (int)HttpStatusCode.MethodNotAllowed);
}
