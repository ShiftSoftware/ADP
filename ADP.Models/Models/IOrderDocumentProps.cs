using System;

namespace ShiftSoftware.ADP.Models;

/// <summary>
/// An Order Document contains lines that are either parts or labor or vehicles.
/// It can represent a job card, a showroom/bulk vehicle order, or a counter sales parts order.
/// An Order Document can either be a Sales Order or a Purchase Order.
/// </summary>
internal interface IOrderDocumentProps
{
    public string OrderDocumentNumber { get; set; }
}