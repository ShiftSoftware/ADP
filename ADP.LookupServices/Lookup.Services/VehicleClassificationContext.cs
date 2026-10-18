using System;
using System.Collections.Generic;
using ShiftSoftware.ADP.Lookup.Services.Evaluators;
using ShiftSoftware.ADP.Lookup.Services.Aggregate;
using ShiftSoftware.ADP.Models.Vehicle;

namespace ShiftSoftware.ADP.Lookup.Services;

/// <summary>Server-loaded evidence for one evaluation. Resolvers must reuse it without fetching more data.</summary>
public sealed class VehicleClassificationContext
{
    public CompanyDataAggregateModel Aggregate { get; }
    public DateTimeOffset EvaluatedAt { get; }
    public string LanguageCode { get; }
    public bool IsAuthorized { get; }
    /// <summary>The same resolved invoice groups used by this request's service-history response.</summary>
    public IReadOnlyList<VehicleServiceHistoryEvaluator.VehicleServiceHistoryInvoice> ServiceHistory { get; }
    public Enums.ConsistencyLevels ServiceHistoryConsistencyLevel { get; }

    /// <summary>Unfiltered catalog evidence, including campaigns whose benefits have not activated.</summary>
    public IReadOnlyList<ServiceItemModel> ServiceItemCatalog { get; }

    public VehicleClassificationContext(CompanyDataAggregateModel aggregate, DateTimeOffset evaluatedAt, string languageCode, bool isAuthorized,
        IReadOnlyList<VehicleServiceHistoryEvaluator.VehicleServiceHistoryInvoice> serviceHistory,
        Enums.ConsistencyLevels serviceHistoryConsistencyLevel,
        IReadOnlyList<ServiceItemModel> serviceItemCatalog = null)
    {
        ServiceItemCatalog = serviceItemCatalog ?? Array.Empty<ServiceItemModel>();
        Aggregate = aggregate;
        EvaluatedAt = evaluatedAt;
        LanguageCode = languageCode;
        IsAuthorized = isAuthorized;
        ServiceHistory = serviceHistory;
        ServiceHistoryConsistencyLevel = serviceHistoryConsistencyLevel;
    }
}
