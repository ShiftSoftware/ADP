﻿using Microsoft.EntityFrameworkCore;
using ShiftSoftware.ShiftEntity.Model.Enums;

namespace ShiftSoftware.ADP.SyncAgent.Entities;

[Index(nameof(ContainerName), nameof(LastReplicationDate))]
public class DeletedRowLog
{
    public long ID { get; set; }
    public string RowID { get; set; }
    public string? PartitionKeyLevelOneValue { get; set; } = default!;
    public PartitionKeyTypes PartitionKeyLevelOneType { get; set; }
    public string? PartitionKeyLevelTwoValue { get; set; } = default!;
    public PartitionKeyTypes PartitionKeyLevelTwoType { get; set; }
    public string? PartitionKeyLevelThreeValue { get; set; } = default!;
    public PartitionKeyTypes PartitionKeyLevelThreeType { get; set; }
    public string ContainerName { get; set; } = default!;
    public DateTime? LastReplicationDate { get; set; }
}