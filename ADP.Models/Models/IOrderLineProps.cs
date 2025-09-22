using System;

namespace ShiftSoftware.ADP.Models;

/// <summary>
/// An Order Document contains lines that are either parts or labor or vehicles.
/// It can represent a job card, a showroom/bulk vehicle order, or a counter sales parts order.
/// An Order Document can either be a Sales Order or a Purchase Order.
/// </summary>
public interface IOrderLineProps
{
    public string LineID { get; set; }
    public decimal? OrderQuantity { get; set; }
    public decimal? SoldQuantity { get; set; }
    public decimal? ExtendedPrice { get; set; }

    /// <summary>
    /// The Physical Status of the Item. It could be a Vehicle or a Part. This will indicate statuses such as: Reserved, In Transit ...etc
    /// </summary>
    public string ItemStatus { get; set; }
    public string OrderStatus { get; set; }
    public DateTime? LoadDate { get; set; }
    public DateTime? PostDate { get; set; }
}