using System;

namespace ShiftSoftware.ADP.Models;

/// <summary>
/// An Order Document contains lines that are either parts or labor or vehicles.
/// It can represent a job card, a showroom/bulk vehicle order, or a counter sales parts order.
/// An Order Document can either be a Sales Order or a Purchase Order.
/// </summary>
public interface IOrderLineProps
{
    /// <summary>
    /// The unique identifier of the line item within the order.
    /// </summary>
    public string LineID { get; set; }

    /// <summary>
    /// The quantity ordered.
    /// </summary>
    public decimal? OrderQuantity { get; set; }

    /// <summary>
    /// The quantity sold.
    /// </summary>
    public decimal? SoldQuantity { get; set; }

    /// <summary>
    /// The extended price (unit price multiplied by quantity).
    /// </summary>
    public decimal? ExtendedPrice { get; set; }

    /// <summary>
    /// The Physical Status of the Item. It could be a Vehicle or a Part. This will indicate statuses such as: Reserved, In Transit ...etc
    /// </summary>
    public string ItemStatus { get; set; }

    /// <summary>
    /// The status of the parent order (e.g., Open, Completed).
    /// </summary>
    public string OrderStatus { get; set; }

    /// <summary>
    /// The date the line was loaded into the order document.
    /// </summary>
    public DateTime? LoadDate { get; set; }

    /// <summary>
    /// The date the line was posted/finalized.
    /// </summary>
    public DateTime? PostDate { get; set; }
}