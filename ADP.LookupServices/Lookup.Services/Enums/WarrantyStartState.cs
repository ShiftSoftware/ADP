namespace ShiftSoftware.ADP.Lookup.Services.Enums;

/// <summary>
/// Why a vehicle's standard warranty has or has not started yet.
///
/// <para>Possession is not a sale. While the vehicle is still moving through the supply chain — held by
/// the distributor, an intermediary, or a broker that has not invoiced it — the warranty has deliberately
/// not started, and the holder's own invoice date must never stand in for the start: the end customer
/// would silently lose that whole possession period off the front of their coverage. The evaluator
/// enforces this by leaving the start date null; this states the reason, so a consumer can say so instead
/// of rendering an unexplained empty coverage.</para>
///
/// <para>Distinct from <see cref="WarrantyActivationStatus"/>, which answers a different question: whether
/// to offer the <i>requesting dealer</i> the activation affordance.</para>
/// </summary>
public enum WarrantyStartState
{
    /// <summary>A start date resolved and the coverage period is established.</summary>
    Started = 0,

    /// <summary>
    /// A broker holds the vehicle and has not invoiced it. The dealer's invoice only moved the car to the
    /// broker, so it cannot anchor the warranty; the broker's invoice will.
    /// </summary>
    AwaitingBrokerInvoice = 1,

    /// <summary>
    /// Only supply-chain entries exist for this vehicle — the distributor or an intermediary — and none is
    /// marked as a direct sale to an end customer. The vehicle has not reached a customer yet, or the
    /// dealer's own entry has not synced.
    /// </summary>
    AwaitingEndCustomerSale = 2,

    /// <summary>
    /// An end-customer sale exists but nothing has dated the warranty: no service activation, no sale
    /// warranty-activation date, and invoice-date defaulting is off for this deployment.
    /// </summary>
    AwaitingActivation = 3,
}
