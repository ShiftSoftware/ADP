namespace ShiftSoftware.ADP.Models;

public readonly struct PartitionedItemType
{
    public string Value { get; }

    public PartitionedItemType(string value) => Value = value;

    public override string ToString() => Value;

    public static implicit operator string(PartitionedItemType modelType) => modelType.Value;
    public static implicit operator PartitionedItemType(string value) => new PartitionedItemType(value);
}