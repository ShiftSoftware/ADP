using ShiftSoftware.ADP.Models.Service;
using System;
using System.Collections.Generic;

namespace ShiftSoftware.ADP.Lookup.Services.Milestones;

/// <summary>Opt-in historical visit counting for selected conditional rewards.</summary>
public sealed class ServiceRewardToleranceOptions
{
    /// <summary>Service-item integration IDs. Other items retain their catalog rules.</summary>
    public List<string> ServiceItemIDs { get; set; } = new();

    /// <summary>The calculated free-service start must be strictly before this date.</summary>
    public DateTime StartsBefore { get; set; }

    /// <summary>Programs that identify the terminal service at the eligible schedule cap.</summary>
    public List<string> TerminalPrograms { get; set; } = new();

    /// <summary>Default: a recognized package above the catalog ceiling immediately misses the reward.</summary>
    public RewardHighPackageBehavior HighPackageBehavior { get; set; } = RewardHighPackageBehavior.ImmediateMiss;

    /// <summary>
    /// Host recognition of additional maintenance such as oil replacement without a milestone.
    /// A positive line qualifies its job; unrelated lines in the same job do not veto it.
    /// </summary>
    public Func<OrderLaborLineModel, bool> IsAdditionalQualifyingWork { get; set; }

    /// <summary>
    /// Optional host confirmation that two complete job histories are duplicate source records.
    /// Normally jobs use VIN, company, branch and job number. Do not merge merely by date.
    /// </summary>
    public Func<IReadOnlyList<OrderLaborLineModel>, IReadOnlyList<OrderLaborLineModel>, bool> AreDuplicateJobs { get; set; }
}

public enum RewardHighPackageBehavior
{
    ImmediateMiss = 0,
    CountInSequence = 1,
}
