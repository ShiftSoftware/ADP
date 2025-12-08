using System;
using System.Collections.Generic;
using System.Text;

namespace ShiftSoftware.ADP.SyncAgent;

public enum SyncOperationType
{
    Preparing,
    ActionStarted,
    SourceTotalItemCount,
    BatchStarted,
    GetSourceBatchItems,
    Mapping,
    StoreBatchData,
    BatchRetry,
    BatchCompleted,
    ActionCompleted,
    Failed,
    Succeeded,
    Finished
}
