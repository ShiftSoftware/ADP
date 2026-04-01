namespace ShiftSoftware.ADP.Models;

/// <summary>
/// Defines the item type discriminator used for partitioned storage in Cosmos DB.
/// Models implementing this interface can be stored in the same container and distinguished by their ItemType.
/// </summary>
public interface IPartitionedItem
{
    /// <summary>
    /// The type discriminator string that identifies the model type within a partitioned container.
    /// </summary>
    public string ItemType { get; }
}