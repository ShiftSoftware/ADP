using AutoMapper;
using Microsoft.Extensions.Logging;
using ShiftSoftware.ADP.SyncAgent.Services.Interfaces;

namespace ShiftSoftware.ADP.SyncAgent.Extensions;

public static class ISyncEngineExtensions
{
    public static ISyncEngine<TSource, TDestination> UseAutoMapper<TSource, TDestination>(
        this ISyncEngine<TSource, TDestination> syncService,
        IMapper mapper)
        where TSource : class, new()
        where TDestination : class, new()
    {
        syncService.SetupMapping((x, y) => new(mapper.Map<IEnumerable<TDestination>>(x)));

        return syncService;
    }


    /// <summary>
    /// Please add this after all other configurations, to avoid unexpected behaviors.
    /// </summary>
    /// <typeparam name="TSource"></typeparam>
    /// <typeparam name="TDestination"></typeparam>
    /// <param name="syncService"></param>
    /// <param name="logger"></param>
    /// <returns></returns>
    public static ISyncEngine<TSource, TDestination> AddLogger<TSource, TDestination>(
        this ISyncEngine<TSource, TDestination> syncService,
        ILogger logger)
        where TSource : class, new()
        where TDestination : class, new()
    {

        if(syncService.Preparing is not null)
        {
            var previousPreparing = syncService.Preparing;
            syncService.SetupPreparing ( async (x) =>
            {
                logger.LogInformation("Start preparing.");

                var result = await previousPreparing(x);

                if(result == SyncPreparingResponseAction.Succeeded)
                    logger.LogInformation("Preparing finished successfully.");
                else if(result == SyncPreparingResponseAction.Failed)
                    logger.LogError("Preparing failed.");
                else if(result == SyncPreparingResponseAction.Skiped)
                    logger.LogWarning("Preparing skipped.");

                return result;
            });
        }

        var previousActionStarted = syncService.ActionStarted;
        syncService.SetupActionStarted ( async (x) =>
        {
            logger.LogInformation($"Action {x.Input} is started.");

            if(previousActionStarted is not null)
                return await previousActionStarted(x);
            
            return true;
        });

        if (syncService.SourceTotalItemCount is not null)
        {
            var previousSourceTotalItemCount = syncService.SourceTotalItemCount;
            syncService.SetupSourceTotalItemCount ( async (x) =>
            {
                logger.LogInformation($"Start getting source total item count for {x.Input}");

                var result = await previousSourceTotalItemCount(x);

                logger.LogInformation($"Getting source total item count finished for {x.Input}, count is {result}");

                return result;
            });
        }

        var previousBatchStarted = syncService.BatchStarted;
        syncService.SetupBatchStarted ( async (x) =>
        {
            logger.LogInformation("");
            logger.LogInformation("----------------------------------------------------");

            logger.LogInformation($"Batch started for {x.Input.ActionType}, step {x.Input.CurrentStep + 1}" +
                    (x.Input.TotalSteps.HasValue ? $" of {x.Input.TotalSteps}" : ""));

            if (previousBatchStarted is not null)
                return await previousBatchStarted(x);

            return true;
        });

        if (syncService.GetSourceBatchItems is not null)
        {
            var previousGetSourceBatchItems = syncService.GetSourceBatchItems;
            syncService.SetupGetSourceBatchItems ( async (x) =>
            {
                logger.LogInformation($"Start getting source items for {x.Input.Status.ActionType}, step {x.Input.Status.CurrentStep + 1}" +
                    (x.Input.Status.TotalSteps.HasValue ? $" of {x.Input.Status.TotalSteps}" : ""));

                var result = await previousGetSourceBatchItems(x);

                logger.LogInformation($"Getting source batch items finished for {x.Input.Status.ActionType}, step {x.Input.Status.CurrentStep + 1}" +
                    (x.Input.Status.TotalSteps.HasValue ? $" of {x.Input.Status.TotalSteps}" : ""));

                return result;
            });
        }

        if(syncService.Mapping is not null)
        {
            var previousMapping = syncService.Mapping;
            syncService.SetupAdvancedMapping ( async (x) =>
            {
                logger.LogInformation($"Start mapping for {x.Input.Status.ActionType}, step {x.Input.Status.CurrentStep + 1}" +
                    (x.Input.Status.TotalSteps.HasValue ? $" of {x.Input.Status.TotalSteps}" : ""));

                var result = await previousMapping(x);

                logger.LogInformation($"Mapping finished for {x.Input.Status.ActionType}, step {x.Input.Status.CurrentStep + 1}" +
                    (x.Input.Status.TotalSteps.HasValue ? $" of {x.Input.Status.TotalSteps}" : ""));

                return result;
            });
        }

        if(syncService.StoreBatchData is not null)
        {
            var previousStoreBatchData = syncService.StoreBatchData;
            syncService.SetupStoreBatchData ( async (x) =>
            {
                logger.LogInformation($"Start storing batch data for {x.Input.Status.ActionType}, step {x.Input.Status.CurrentStep + 1}" +
                    (x.Input.Status.TotalSteps.HasValue ? $" of {x.Input.Status.TotalSteps}" : ""));

                var result = await previousStoreBatchData(x);

                logger.LogInformation($"Storing batch data finished for {x.Input.Status.ActionType}, step {x.Input.Status.CurrentStep + 1}" +
                    (x.Input.Status.TotalSteps.HasValue ? $" of {x.Input.Status.TotalSteps}" : ""));

                return result;
            });
        }

        var previousBatchRetry = syncService.BatchRetry;
        syncService.SetupBatchRetry ( async (x) =>
        {
            RetryAction result = syncService.Configurations!.DefaultRetryAction;

            if (previousBatchRetry is not null)
                result = await previousBatchRetry(x);

            if (result == RetryAction.RetryAndStopAfterLastRetry)
            {
                if (x.Input.Status.CurrentRetryCount + 1 <= x.Input.Status.MaxRetryCount)
                    logger.LogWarning($"Batch failed we do {x.Input.Status.CurrentRetryCount + 1} retry for {x.Input.Status.ActionType}, step {x.Input.Status.CurrentStep + 1}" + (x.Input.Status.TotalSteps.HasValue ? $" of {x.Input.Status.TotalSteps}" : ""));
                else
                    logger.LogError($"Batch failed we do stop for {x.Input.Status.ActionType}, step {x.Input.Status.CurrentStep + 1}" +
                    (x.Input.Status.TotalSteps.HasValue ? $" of {x.Input.Status.TotalSteps}" : ""));
            }
            else if(result == RetryAction.RetryAndContinueAfterLastRetry)
            {
                if (x.Input.Status.CurrentRetryCount + 1 <= x.Input.Status.MaxRetryCount)
                    logger.LogWarning($"Batch failed we do {x.Input.Status.CurrentRetryCount + 1} retry for {x.Input.Status.ActionType}, step {x.Input.Status.CurrentStep + 1}" + (x.Input.Status.TotalSteps.HasValue ? $" of {x.Input.Status.TotalSteps}" : ""));
            }
            else if (result == RetryAction.Skip)
                logger.LogWarning($"Batch failed we do skip for {x.Input.Status.ActionType}, step {x.Input.Status.CurrentStep + 1}" +
                    (x.Input.Status.TotalSteps.HasValue ? $" of {x.Input.Status.TotalSteps}" : ""));
            else if (result == RetryAction.Stop)
                logger.LogError($"Batch failed we do stop for {x.Input.Status.ActionType}, step {x.Input.Status.CurrentStep + 1}" +
                    (x.Input.Status.TotalSteps.HasValue ? $" of {x.Input.Status.TotalSteps}" : ""));

            if (x.Input?.Exception is not null)
                logger.LogError(x.Input.Exception, "The sync operation is failed with exception.");

            return result;
        });

        var previousBatchCompleted = syncService.BatchCompleted;
        syncService.SetupBatchCompleted ( async (x) =>
        {
            bool result = true;

            if(previousBatchCompleted is not null)
                result = await previousBatchCompleted(x);

            if(result)
                logger.LogInformation($"Batch completed succesfully for {x.Input.Status.ActionType}, step {x.Input.Status.CurrentStep + 1}" +
                    (x.Input.Status.TotalSteps.HasValue ? $" of {x.Input.Status.TotalSteps}" : ""));
            else
                logger.LogError($"Batch failed for {x.Input.Status.ActionType}, step {x.Input.Status.CurrentStep + 1}" +
                    (x.Input.Status.TotalSteps.HasValue ? $" of {x.Input.Status.TotalSteps}" : ""));

            logger.LogInformation("----------------------------------------------------");
            logger.LogInformation("");

            return result;
        });

        var previousActionCompleted = syncService.ActionCompleted;
        syncService.SetupActionCompleted ( async (x) =>
        {
            bool result = x.Input.Succeeded;
            if(previousActionCompleted is not null)
                result = await previousActionCompleted(x);

            if(result)
                logger.LogInformation($"Action {x.Input.ActionType} is completed successfully");
            else
                logger.LogError($"Action {x.Input.ActionType} is failed");

            return result;
        });

        var previousFailed = syncService.Failed;
        syncService.SetupFailed ( async (x) =>
        {
            if (previousFailed is not null)
                await previousFailed(x);

            logger.LogError("The sync operation is failed.");
            if (x.Input is not null)
                logger.LogError(x.Input, "The sync operation is failed with exception.");
        });

        var previousSucceeded = syncService.Succeeded;
        syncService.SetupSucceeded ( async (x) =>
        {
            if(previousSucceeded is not null)
                await previousSucceeded(x);

            logger.LogInformation("The sync operation is succeeded.");
        });

        var previousFinished = syncService.Finished;
        syncService.SetupFinished ( async (x) =>
        {
            if(previousFinished is not null)
                await previousFinished(x);

            logger.LogInformation("The sync operation is finished.");
        });

        return syncService;
    }

    public static ISyncEngine<TSource, TDestination> AddLogger<TSource, TDestination>(
        this ISyncEngine<TSource, TDestination> syncService,
        SyncEngineILogger logger)
        where TSource : class, new()
        where TDestination : class, new()
    {
        syncService.RegisterLogger(logger);

        if (syncService.Preparing is not null)
        {
            var previousPreparing = syncService.Preparing;
            syncService.SetupPreparing(async (x) =>
            {
                await logger.LogInformation("Start preparing.");

                var result = await previousPreparing(x);

                if (result == SyncPreparingResponseAction.Succeeded)
                    await logger.LogInformation("Preparing finished successfully.");
                else if (result == SyncPreparingResponseAction.Failed)
                    await logger.LogError("Preparing failed.");
                else if (result == SyncPreparingResponseAction.Skiped)
                    await logger.LogWarning("Preparing skipped.");

                return result;
            });
        }

        var previousActionStarted = syncService.ActionStarted;
        syncService.SetupActionStarted(async (x) =>
        {
            await logger.LogInformation($"Action {x.Input} is started.");

            if (previousActionStarted is not null)
                return await previousActionStarted(x);

            return true;
        });

        if (syncService.SourceTotalItemCount is not null)
        {
            var previousSourceTotalItemCount = syncService.SourceTotalItemCount;
            syncService.SetupSourceTotalItemCount(async (x) =>
            {
                await logger.LogInformation($"Start getting source total item count for {x.Input}");

                var result = await previousSourceTotalItemCount(x);

                await logger.LogInformation($"Getting source total item count finished for {x.Input}, count is {result}");

                return result;
            });
        }

        var previousBatchStarted = syncService.BatchStarted;
        syncService.SetupBatchStarted(async (x) =>
        {
            await logger.LogInformation("");
            await logger.LogInformation("----------------------------------------------------");

            await logger.LogInformation($"Batch started for {x.Input.ActionType}, step {x.Input.CurrentStep + 1}" +
                    (x.Input.TotalSteps.HasValue ? $" of {x.Input.TotalSteps}" : ""));

            if (previousBatchStarted is not null)
                return await previousBatchStarted(x);

            return true;
        });

        if (syncService.GetSourceBatchItems is not null)
        {
            var previousGetSourceBatchItems = syncService.GetSourceBatchItems;
            syncService.SetupGetSourceBatchItems(async (x) =>
            {
                await logger.LogInformation($"Start getting source items for {x.Input.Status.ActionType}, step {x.Input.Status.CurrentStep + 1}" +
                    (x.Input.Status.TotalSteps.HasValue ? $" of {x.Input.Status.TotalSteps}" : ""));

                var result = await previousGetSourceBatchItems(x);

                await logger.LogInformation($"Getting source batch items finished for {x.Input.Status.ActionType}, step {x.Input.Status.CurrentStep + 1}" +
                    (x.Input.Status.TotalSteps.HasValue ? $" of {x.Input.Status.TotalSteps}" : ""));

                return result;
            });
        }

        if (syncService.Mapping is not null)
        {
            var previousMapping = syncService.Mapping;
            syncService.SetupAdvancedMapping(async (x) =>
            {
                await logger.LogInformation($"Start mapping for {x.Input.Status.ActionType}, step {x.Input.Status.CurrentStep + 1}" +
                    (x.Input.Status.TotalSteps.HasValue ? $" of {x.Input.Status.TotalSteps}" : ""));

                var result = await previousMapping(x);

                await logger.LogInformation($"Mapping finished for {x.Input.Status.ActionType}, step {x.Input.Status.CurrentStep + 1}" +
                    (x.Input.Status.TotalSteps.HasValue ? $" of {x.Input.Status.TotalSteps}" : ""));

                return result;
            });
        }

        if (syncService.StoreBatchData is not null)
        {
            var previousStoreBatchData = syncService.StoreBatchData;
            syncService.SetupStoreBatchData(async (x) =>
            {
                await logger.LogInformation($"Start storing batch data for {x.Input.Status.ActionType}, step {x.Input.Status.CurrentStep + 1}" +
                    (x.Input.Status.TotalSteps.HasValue ? $" of {x.Input.Status.TotalSteps}" : ""));

                var result = await previousStoreBatchData(x);

                await logger.LogInformation($"Storing batch data finished for {x.Input.Status.ActionType}, step {x.Input.Status.CurrentStep + 1}" +
                    (x.Input.Status.TotalSteps.HasValue ? $" of {x.Input.Status.TotalSteps}" : ""));

                return result;
            });
        }

        var previousBatchRetry = syncService.BatchRetry;
        syncService.SetupBatchRetry(async (x) =>
        {
            RetryAction result = syncService.Configurations!.DefaultRetryAction;

            if (previousBatchRetry is not null)
                result = await previousBatchRetry(x);

            if (result == RetryAction.RetryAndStopAfterLastRetry)
            {
                if (x.Input.Status.CurrentRetryCount + 1 <= x.Input.Status.MaxRetryCount)
                    await logger.LogWarning($"Batch failed we do {x.Input.Status.CurrentRetryCount + 1} retry for {x.Input.Status.ActionType}, step {x.Input.Status.CurrentStep + 1}" + (x.Input.Status.TotalSteps.HasValue ? $" of {x.Input.Status.TotalSteps}" : ""));
                else
                    await logger.LogError($"Batch failed we do stop for {x.Input.Status.ActionType}, step {x.Input.Status.CurrentStep + 1}" +
                    (x.Input.Status.TotalSteps.HasValue ? $" of {x.Input.Status.TotalSteps}" : ""));
            }
            else if (result == RetryAction.RetryAndContinueAfterLastRetry)
            {
                if (x.Input.Status.CurrentRetryCount + 1 <= x.Input.Status.MaxRetryCount)
                    await logger.LogWarning($"Batch failed we do {x.Input.Status.CurrentRetryCount + 1} retry for {x.Input.Status.ActionType}, step {x.Input.Status.CurrentStep + 1}" + (x.Input.Status.TotalSteps.HasValue ? $" of {x.Input.Status.TotalSteps}" : ""));
            }
            else if (result == RetryAction.Skip)
                await logger.LogWarning($"Batch failed we do skip for {x.Input.Status.ActionType}, step {x.Input.Status.CurrentStep + 1}" +
                    (x.Input.Status.TotalSteps.HasValue ? $" of {x.Input.Status.TotalSteps}" : ""));
            else if (result == RetryAction.Stop)
                await logger.LogError($"Batch failed we do stop for {x.Input.Status.ActionType}, step {x.Input.Status.CurrentStep + 1}" +
                    (x.Input.Status.TotalSteps.HasValue ? $" of {x.Input.Status.TotalSteps}" : ""));

            if (x.Input?.Exception is not null)
                await logger.LogError(x.Input.Exception, "The sync operation is failed with exception.");

            return result;
        });

        var previousBatchCompleted = syncService.BatchCompleted;
        syncService.SetupBatchCompleted(async (x) =>
        {
            bool result = true;

            if (previousBatchCompleted is not null)
                result = await previousBatchCompleted(x);

            if (result)
                await logger.LogInformation($"Batch completed succesfully for {x.Input.Status.ActionType}, step {x.Input.Status.CurrentStep + 1}" +
                    (x.Input.Status.TotalSteps.HasValue ? $" of {x.Input.Status.TotalSteps}" : ""));
            else
                await logger.LogError($"Batch failed for {x.Input.Status.ActionType}, step {x.Input.Status.CurrentStep + 1}" +
                    (x.Input.Status.TotalSteps.HasValue ? $" of {x.Input.Status.TotalSteps}" : ""));

            await logger.LogInformation("----------------------------------------------------");
            await logger.LogInformation("");

            return result;
        });

        var previousActionCompleted = syncService.ActionCompleted;
        syncService.SetupActionCompleted(async (x) =>
        {
            bool result = x.Input.Succeeded;
            if (previousActionCompleted is not null)
                result = await previousActionCompleted(x);

            if (result)
                await logger.LogInformation($"Action {x.Input.ActionType} is completed successfully");
            else
                await logger.LogError($"Action {x.Input.ActionType} is failed");

            return result;
        });

        var previousFailed = syncService.Failed;
        syncService.SetupFailed(async (x) =>
        {
            if (previousFailed is not null)
                await previousFailed(x);

            await logger.LogError("The sync operation is failed.");
            if (x.Input is not null)
                await logger.LogError(x.Input, "The sync operation is failed with exception.");
        });

        var previousSucceeded = syncService.Succeeded;
        syncService.SetupSucceeded(async (x) =>
        {
            if (previousSucceeded is not null)
                await previousSucceeded(x);

            await logger.LogInformation("The sync operation is succeeded.");
        });

        var previousFinished = syncService.Finished;
        syncService.SetupFinished(async (x) =>
        {
            if (previousFinished is not null)
                await previousFinished(x);

            await logger.LogInformation("The sync operation is finished.");
        });

        return syncService;
    }

    public static ISyncEngine<TSource, TDestination> AddSyncProgressIndicator<TSource, TDestination>(
        this ISyncEngine<TSource, TDestination> syncService,
        SyncProgressIndicatorLogger logger)
        where TSource : class, new()
        where TDestination : class, new()
    {
        syncService.RegisterLogger(logger);

        if (syncService.Preparing is not null)
        {
            var previousPreparing = syncService.Preparing;
            syncService.SetupPreparing(async (x) =>
            {
                await logger.LogInformation("Start preparing.");

                var result = await previousPreparing(x);

                if (result == SyncPreparingResponseAction.Succeeded)
                    await logger.LogInformation("Preparing finished successfully.");
                else if (result == SyncPreparingResponseAction.Failed)
                    await logger.LogError("Preparing failed.");
                else if (result == SyncPreparingResponseAction.Skiped)
                    await logger.LogWarning("Preparing skipped.");

                return result;
            });
        }

        var previousActionStarted = syncService.ActionStarted;
        syncService.SetupActionStarted(async (x) =>
        {
            await logger.LogInformation($"Action {x.Input} is started.");

            if (previousActionStarted is not null)
                return await previousActionStarted(x);

            return true;
        });

        if (syncService.SourceTotalItemCount is not null)
        {
            var previousSourceTotalItemCount = syncService.SourceTotalItemCount;
            syncService.SetupSourceTotalItemCount(async (x) =>
            {
                await logger.LogInformation($"Start getting source total item count for {x.Input}");

                var result = await previousSourceTotalItemCount(x);

                await logger.LogInformation($"Getting source total item count finished for {x.Input}, count is {result}");

                return result;
            });
        }

        var previousBatchStarted = syncService.BatchStarted;
        syncService.SetupBatchStarted(async (x) =>
        {
            await logger.LogInformation("");
            await logger.LogInformation("----------------------------------------------------");

            await logger.LogInformation($"Batch started for {x.Input.ActionType}, step {x.Input.CurrentStep + 1}" +
                    (x.Input.TotalSteps.HasValue ? $" of {x.Input.TotalSteps}" : ""));

            if (previousBatchStarted is not null)
                return await previousBatchStarted(x);

            return true;
        });

        if (syncService.GetSourceBatchItems is not null)
        {
            var previousGetSourceBatchItems = syncService.GetSourceBatchItems;
            syncService.SetupGetSourceBatchItems(async (x) =>
            {
                await logger.LogInformation($"Start getting source items for {x.Input.Status.ActionType}, step {x.Input.Status.CurrentStep + 1}" +
                    (x.Input.Status.TotalSteps.HasValue ? $" of {x.Input.Status.TotalSteps}" : ""));

                var result = await previousGetSourceBatchItems(x);

                await logger.LogInformation($"Getting source batch items finished for {x.Input.Status.ActionType}, step {x.Input.Status.CurrentStep + 1}" +
                    (x.Input.Status.TotalSteps.HasValue ? $" of {x.Input.Status.TotalSteps}" : ""));

                return result;
            });
        }

        if (syncService.Mapping is not null)
        {
            var previousMapping = syncService.Mapping;
            syncService.SetupAdvancedMapping(async (x) =>
            {
                await logger.LogInformation($"Start mapping for {x.Input.Status.ActionType}, step {x.Input.Status.CurrentStep + 1}" +
                    (x.Input.Status.TotalSteps.HasValue ? $" of {x.Input.Status.TotalSteps}" : ""));

                var result = await previousMapping(x);

                await logger.LogInformation($"Mapping finished for {x.Input.Status.ActionType}, step {x.Input.Status.CurrentStep + 1}" +
                    (x.Input.Status.TotalSteps.HasValue ? $" of {x.Input.Status.TotalSteps}" : ""));

                return result;
            });
        }

        if (syncService.StoreBatchData is not null)
        {
            var previousStoreBatchData = syncService.StoreBatchData;
            syncService.SetupStoreBatchData(async (x) =>
            {
                await logger.LogInformation($"Start storing batch data for {x.Input.Status.ActionType}, step {x.Input.Status.CurrentStep + 1}" +
                    (x.Input.Status.TotalSteps.HasValue ? $" of {x.Input.Status.TotalSteps}" : ""));

                var result = await previousStoreBatchData(x);

                await logger.LogInformation($"Storing batch data finished for {x.Input.Status.ActionType}, step {x.Input.Status.CurrentStep + 1}" +
                    (x.Input.Status.TotalSteps.HasValue ? $" of {x.Input.Status.TotalSteps}" : ""));

                return result;
            });
        }

        var previousBatchRetry = syncService.BatchRetry;
        syncService.SetupBatchRetry(async (x) =>
        {
            RetryAction result = syncService.Configurations!.DefaultRetryAction;

            if (previousBatchRetry is not null)
                result = await previousBatchRetry(x);

            if (result == RetryAction.RetryAndStopAfterLastRetry)
            {
                if (x.Input.Status.CurrentRetryCount + 1 <= x.Input.Status.MaxRetryCount)
                    await logger.LogWarning($"Batch failed we do {x.Input.Status.CurrentRetryCount + 1} retry for {x.Input.Status.ActionType}, step {x.Input.Status.CurrentStep + 1}" + (x.Input.Status.TotalSteps.HasValue ? $" of {x.Input.Status.TotalSteps}" : ""));
                else
                    await logger.LogError($"Batch failed we do stop for {x.Input.Status.ActionType}, step {x.Input.Status.CurrentStep + 1}" +
                    (x.Input.Status.TotalSteps.HasValue ? $" of {x.Input.Status.TotalSteps}" : ""));
            }
            else if (result == RetryAction.RetryAndContinueAfterLastRetry)
            {
                if (x.Input.Status.CurrentRetryCount + 1 <= x.Input.Status.MaxRetryCount)
                    await logger.LogWarning($"Batch failed we do {x.Input.Status.CurrentRetryCount + 1} retry for {x.Input.Status.ActionType}, step {x.Input.Status.CurrentStep + 1}" + (x.Input.Status.TotalSteps.HasValue ? $" of {x.Input.Status.TotalSteps}" : ""));
            }
            else if (result == RetryAction.Skip)
                await logger.LogWarning($"Batch failed we do skip for {x.Input.Status.ActionType}, step {x.Input.Status.CurrentStep + 1}" +
                    (x.Input.Status.TotalSteps.HasValue ? $" of {x.Input.Status.TotalSteps}" : ""));
            else if (result == RetryAction.Stop)
                await logger.LogError($"Batch failed we do stop for {x.Input.Status.ActionType}, step {x.Input.Status.CurrentStep + 1}" +
                    (x.Input.Status.TotalSteps.HasValue ? $" of {x.Input.Status.TotalSteps}" : ""));

            if (x.Input?.Exception is not null)
                await logger.LogError(x.Input.Exception, "The sync operation is failed with exception.");

            return result;
        });

        var previousBatchCompleted = syncService.BatchCompleted;
        syncService.SetupBatchCompleted(async (x) =>
        {
            bool result = true;

            if (previousBatchCompleted is not null)
                result = await previousBatchCompleted(x);

            if (result)
                await logger.LogInformation($"Batch completed succesfully for {x.Input.Status.ActionType}, step {x.Input.Status.CurrentStep + 1}" +
                    (x.Input.Status.TotalSteps.HasValue ? $" of {x.Input.Status.TotalSteps}" : ""));
            else
                await logger.LogError($"Batch failed for {x.Input.Status.ActionType}, step {x.Input.Status.CurrentStep + 1}" +
                    (x.Input.Status.TotalSteps.HasValue ? $" of {x.Input.Status.TotalSteps}" : ""));

            await logger.LogInformation("----------------------------------------------------");
            await logger.LogInformation("");

            return result;
        });

        var previousActionCompleted = syncService.ActionCompleted;
        syncService.SetupActionCompleted(async (x) =>
        {
            bool result = x.Input.Succeeded;
            if (previousActionCompleted is not null)
                result = await previousActionCompleted(x);

            if (result)
                await logger.LogInformation($"Action {x.Input.ActionType} is completed successfully");
            else
                await logger.LogError($"Action {x.Input.ActionType} is failed");

            return result;
        });

        var previousFailed = syncService.Failed;
        syncService.SetupFailed(async (x) =>
        {
            if (previousFailed is not null)
                await previousFailed(x);

            await logger.LogError("The sync operation is failed.");
            if (x.Input is not null)
                await logger.LogError(x.Input, "The sync operation is failed with exception.");

            await logger.FailAllRunningTasks();
        });

        var previousSucceeded = syncService.Succeeded;
        syncService.SetupSucceeded(async (x) =>
        {
            if (previousSucceeded is not null)
                await previousSucceeded(x);

            await logger.LogInformation("The sync operation is succeeded.");
        });

        var previousFinished = syncService.Finished;
        syncService.SetupFinished(async (x) =>
        {
            if (previousFinished is not null)
                await previousFinished(x);

            await logger.LogInformation("The sync operation is finished.");

            await logger.CompleteAllRunningTasks();
        });

        return syncService;
    }
}
