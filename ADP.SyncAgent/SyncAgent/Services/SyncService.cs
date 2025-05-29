using Microsoft.Extensions.DependencyInjection;
using ShiftSoftware.ADP.SyncAgent.Configurations;
using ShiftSoftware.ADP.SyncAgent.Services.Interfaces;

namespace ShiftSoftware.ADP.SyncAgent.Services;

public class SyncService<T> : SyncService<T, T>
    where T : class
{
    public SyncService() : base()
    {
    }

    public SyncService(IServiceProvider services): base(services)
    {
    }
}

public class SyncService<TSource, TDestination> : ISyncService<TSource, TDestination>
    where TSource : class
    where TDestination : class
{
    public virtual SyncConfigurations? Configurations { get; private set; }

    public virtual Func<SyncFunctionInput, ValueTask<SyncPreparingResponseAction>>? Preparing { get; private set; }

    public virtual Func<SyncFunctionInput<SyncActionType>, ValueTask<bool>>? ActionStarted { get; private set; }

    public virtual Func<SyncFunctionInput<SyncActionType>, ValueTask<long?>>? SourceTotalItemCount { get; private set; }

    public virtual Func<SyncFunctionInput<SyncActionStatus>, ValueTask<bool>>? BatchStarted { get; private set; }

    public virtual Func<SyncFunctionInput<SyncGetBatchDataInput<TSource>>, ValueTask<IEnumerable<TSource?>?>>? GetSourceBatchItems { get; private set; }

    public virtual Func<SyncFunctionInput<SyncStoreDataInput<TDestination>>, ValueTask<SyncStoreDataResult<TDestination>>>? StoreBatchData { get; private set; }

    public virtual Func<SyncFunctionInput<SyncBatchCompleteRetryInput<TSource, TDestination>>, ValueTask<RetryAction>>? BatchRetry { get; private set; }

    public virtual Func<SyncFunctionInput<SyncBatchCompleteRetryInput<TSource, TDestination>>, ValueTask<bool>>? BatchCompleted { get; private set; }

    public virtual Func<SyncFunctionInput<SyncActionCompletedInput>, ValueTask<bool>>? ActionCompleted { get; private set; }

    public virtual Func<SyncFunctionInput<SyncMappingInput<TSource, TDestination>>, ValueTask<IEnumerable<TDestination?>?>>? Mapping { get; private set; }

    public virtual Func<SyncFunctionInput, ValueTask>? Failed { get; private set; }

    public virtual Func<SyncFunctionInput, ValueTask>? Succeeded { get; private set; }

    public virtual Func<SyncFunctionInput, ValueTask>? Finished { get; private set; }

    private CancellationTokenSource? cancellationTokenSource;

    private readonly IServiceProvider services;

    public SyncService()
    {
        this.Configure();
    }

    public SyncService(IServiceProvider services) : this()
    {
        this.services = services;
    }

    public virtual ISyncService<TSource, TDestination> Configure(long? batchSize = null, long maxRetryCount = 0, long operationTimeoutInSeconds = 300, RetryAction defaultRetryAction = RetryAction.RetryAndStopAfterLastRetry)
    {
        this.Configurations = new SyncConfigurations(batchSize, maxRetryCount, operationTimeoutInSeconds,defaultRetryAction);
        return this;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="actionExecutionAndOrder">
    /// This is decide which action to be run and in which order,
    /// the default is [SyncActionType.Delete, SyncActionType.Update, SyncActionType.Add]
    /// </param>
    /// <param name="batchSize"></param>
    /// <param name="maxRetryCount"></param>
    /// <param name="operationTimeoutInSeconds"></param>
    /// <returns></returns>
    public virtual ISyncService<TSource, TDestination> Configure(IEnumerable<SyncActionType> actionExecutionAndOrder, long? batchSize = null, long maxRetryCount = 0, long operationTimeoutInSeconds = 300, RetryAction defaultRetryAction = RetryAction.RetryAndStopAfterLastRetry)
    {
        this.Configurations = new SyncConfigurations(batchSize, maxRetryCount, operationTimeoutInSeconds, defaultRetryAction, actionExecutionAndOrder);
        return this;
    }

    public virtual ISyncService<TSource, TDestination> SetupPreparing(Func<SyncFunctionInput, ValueTask<SyncPreparingResponseAction>> preparingFunc)
    {
        this.Preparing = preparingFunc;
        return this;
    }

    public virtual ISyncService<TSource, TDestination> SetupActionStarted(Func<SyncFunctionInput<SyncActionType>, ValueTask<bool>>? actionStartedFunc)
    {
        this.ActionStarted = actionStartedFunc;
        return this;
    }

    public virtual ISyncService<TSource, TDestination> SetupSourceTotalItemCount(Func<SyncFunctionInput<SyncActionType>, ValueTask<long?>>? sourceTotalItemCountFunc)
    {
        this.SourceTotalItemCount = sourceTotalItemCountFunc;
        return this;
    }

    public virtual ISyncService<TSource, TDestination> SetupBatchStarted(Func<SyncFunctionInput<SyncActionStatus>, ValueTask<bool>> batchStartedFunc)
    {
        this.BatchStarted = batchStartedFunc;
        return this;
    }

    public virtual ISyncService<TSource, TDestination> SetupGetSourceBatchItems(Func<SyncFunctionInput<SyncGetBatchDataInput<TSource>>, ValueTask<IEnumerable<TSource?>?>> getSourceBatchItemsFunc)
    {
        this.GetSourceBatchItems = getSourceBatchItemsFunc;
        return this;
    }

    public virtual ISyncService<TSource, TDestination> SetupAdvancedMapping(Func<SyncFunctionInput<SyncMappingInput<TSource, TDestination>>, ValueTask<IEnumerable<TDestination?>?>> mappingFunc)
    {
        this.Mapping = mappingFunc;
        return this;
    }

    /// <summary>
    /// This is using the advanced mapping too, but for simplify,if the operation is not retyr and previous mapped items not null, it return the previous mapped items., otherwise it will mapp it with this function.
    /// </summary>
    /// <param name="mappingFunc"></param>
    /// <returns></returns>
    public virtual ISyncService<TSource, TDestination> SetupMapping(Func<IEnumerable<TSource?>?, SyncActionType, ValueTask<IEnumerable<TDestination?>?>> mappingFunc)
    {
        this.Mapping = x =>
        {
            if(x.Input.Status.CurrentRetryCount > 0 && x.Input.PreviousMappedItem is not null)
                return new(x.Input.PreviousMappedItem);

            return mappingFunc(x.Input.SourceItems, x.Input.Status.ActionType);
        };

        return this;
    }

    public virtual ISyncService<TSource, TDestination> SetupStoreBatchData(Func<SyncFunctionInput<SyncStoreDataInput<TDestination>>, ValueTask<SyncStoreDataResult<TDestination>>> storeBatchDataFunc)
    {
        this.StoreBatchData = storeBatchDataFunc;
        return this;
    }

    public virtual ISyncService<TSource, TDestination> SetupBatchRetry(Func<SyncFunctionInput<SyncBatchCompleteRetryInput<TSource, TDestination>>, ValueTask<RetryAction>> batchRetryFunc)
    {
        this.BatchRetry = batchRetryFunc;
        return this;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="batchCompletedFunc">
    /// Return true to continue the sync process,
    /// Return false to retry.
    /// </param>
    /// <returns></returns>
    public virtual ISyncService<TSource, TDestination> SetupBatchCompleted(Func<SyncFunctionInput<SyncBatchCompleteRetryInput<TSource, TDestination>>, ValueTask<bool>> batchCompletedFunc)
    {
        this.BatchCompleted = batchCompletedFunc;
        return this;
    }

    public virtual ISyncService<TSource, TDestination> SetupActionCompleted(Func<SyncFunctionInput<SyncActionCompletedInput>, ValueTask<bool>> actionCompletedFunc)
    {
        this.ActionCompleted = actionCompletedFunc;
        return this;
    }

    public virtual ISyncService<TSource, TDestination> SetupFailed(Func<SyncFunctionInput, ValueTask> failedFunc)
    {
        this.Failed = failedFunc;
        return this;
    }

    public virtual ISyncService<TSource, TDestination> SetupSucceeded(Func<SyncFunctionInput, ValueTask> succeededFunc)
    {
        this.Succeeded = succeededFunc;
        return this;
    }

    public virtual async Task<bool> RunAsync()
    {
        CheckSetups();

        bool result = false;

        // Setup cancellation token
        this.cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(this.Configurations!.OperationTimeoutInSeconds));

        try
        {
            // Preparing
            var preparingResult = SyncPreparingResponseAction.Succeeded;
            if (this.Preparing is not null)
                preparingResult = await this.Preparing!(new SyncFunctionInput(this.GetCancellationToken()));

            if (preparingResult == SyncPreparingResponseAction.Succeeded)
            {
                bool actionResult = true;

                foreach (var actionType in this.Configurations.ActionExecutionAndOrder)
                {
                    if (!actionResult)
                        break;

                    actionResult =  await RunAction(actionType);
                }

                result = actionResult;
            }

            if ((result || preparingResult == SyncPreparingResponseAction.Skiped) && this.Succeeded is not null)
                await this.Succeeded!(new(GetCancellationToken()));
            else if (!result && this.Failed is not null)
                await this.Failed!(new(GetCancellationToken()));
        }
        catch (Exception ex)
        {
            if(this.Failed is not null)
                await this.Failed!(new(GetCancellationToken()));

            result = false;
        }

        if(this.Finished is not null)
            await this.Finished!(new(GetCancellationToken()));

        return result;
    }

    private async Task<bool> RunAction(SyncActionType actionType)
    {
        bool result = false;

        bool actionStartedResult = true;

        if (this.ActionStarted is not null)
            actionStartedResult = await this.ActionStarted!(new(this.GetCancellationToken(), actionType));

        if (actionStartedResult)
        {
            // Get total item count
            long? totalItemCount = null;
            if (this.SourceTotalItemCount is not null)
                totalItemCount = await this.SourceTotalItemCount!(new SyncFunctionInput<SyncActionType>(this.GetCancellationToken(), actionType));

            // Prepare batch informations
            var batchSize = (this.Configurations!.BatchSize ?? totalItemCount) ?? 0;
            var currentStep = 0;
            var totalSteps = totalItemCount == null ? null : (long?)Math.Ceiling((double)totalItemCount.Value / (double)batchSize);
            long retryCount = 0;
            var maxRetryCount = this.Configurations.MaxRetryCount;

            if (totalSteps > 0  || totalItemCount is null)
            {
                IEnumerable<TSource?>? sourceItems = null;
                IEnumerable<TDestination?>? destinationItems = null;
                SyncStoreDataResult<TDestination>? storeResult = null;

                while (true)
                {
                    await CheckForCancellation();

                    // If provide with total item count and the last step is completed, then stop the operation
                    if (totalSteps != null && currentStep >= totalSteps)
                    {
                        result = true;
                        break;
                    }

                    // If this is not retry and BatchStarted is not null, the run it
                    if (this.BatchStarted is not null && retryCount == 0)
                    {
                        var batchStartedResult = await this.BatchStarted!(new SyncFunctionInput<SyncActionStatus>(this.GetCancellationToken(), new SyncActionStatus(currentStep, totalSteps, batchSize, totalItemCount, maxRetryCount, retryCount, actionType)));

                        if (!batchStartedResult)
                            break;
                    }

                    try
                    {
                        // Get current batch data from the source
                        sourceItems = await this.GetSourceBatchItems!(new SyncFunctionInput<SyncGetBatchDataInput<TSource>>(this.GetCancellationToken(), new SyncGetBatchDataInput<TSource>(sourceItems, new SyncActionStatus(currentStep, totalSteps, batchSize, totalItemCount, maxRetryCount, retryCount, actionType))));

                        // If no data is returned and total item count not provided, then stop the operation
                        if (!(sourceItems?.Any() ?? false) && totalItemCount is null)
                        {
                            result = true;
                            break;
                        }

                        // Map the source data to destination data
                        destinationItems = await this.Mapping!(new SyncFunctionInput<SyncMappingInput<TSource, TDestination>>(this.GetCancellationToken(), new SyncMappingInput<TSource, TDestination>(sourceItems, destinationItems, new SyncActionStatus(currentStep, totalSteps, batchSize, totalItemCount, maxRetryCount, retryCount, actionType))));

                        // Store the mapped data to the destination
                        storeResult = await this.StoreBatchData!(new SyncFunctionInput<SyncStoreDataInput<TDestination>>(this.GetCancellationToken(), new SyncStoreDataInput<TDestination>(destinationItems, storeResult, new SyncActionStatus(currentStep, totalSteps, batchSize, totalItemCount, maxRetryCount, retryCount, actionType))));

                        // Check if retry required
                        if (storeResult.NeedRetry)
                            throw new Exception("Retry is required.");

                        // Run batch completed function
                        if (this.BatchCompleted is not null)
                        {
                            var batchCompletedResult = await this.BatchCompleted!(new SyncFunctionInput<SyncBatchCompleteRetryInput<TSource, TDestination>>(this.GetCancellationToken(), new SyncBatchCompleteRetryInput<TSource, TDestination>(sourceItems, storeResult, new SyncActionStatus(currentStep, totalSteps, batchSize, totalItemCount, maxRetryCount, retryCount, actionType))));

                            if (!batchCompletedResult)
                                throw new Exception("Retry is required.");
                        }

                        currentStep++;
                        retryCount = 0;
                        sourceItems = null;
                        destinationItems = null;
                        storeResult = null;
                    }
                    catch (Exception)
                    {
                        RetryAction retryResult = this.Configurations.DefaultRetryAction;
                        if (this.BatchRetry is not null)
                            retryResult = await this.BatchRetry!(new SyncFunctionInput<SyncBatchCompleteRetryInput<TSource, TDestination>>(this.GetCancellationToken(), new SyncBatchCompleteRetryInput<TSource, TDestination>(sourceItems, storeResult, new SyncActionStatus(currentStep, totalSteps, batchSize, totalItemCount, maxRetryCount, retryCount, actionType))));

                        // Do action based on the retry result
                        if (retryResult == RetryAction.RetryAndContinueAfterLastRetry)
                        {
                            retryCount++;

                            if (retryCount > maxRetryCount)
                            {
                                // Run batch completed function
                                if (this.BatchCompleted is not null)
                                {
                                    var batchCompletedResult = await this.BatchCompleted!(new SyncFunctionInput<SyncBatchCompleteRetryInput<TSource, TDestination>>(this.GetCancellationToken(), new SyncBatchCompleteRetryInput<TSource, TDestination>(sourceItems, storeResult, new SyncActionStatus(currentStep, totalSteps, batchSize, totalItemCount, maxRetryCount, retryCount, actionType))));

                                    if (!batchCompletedResult)
                                    {
                                        result = false;
                                        break;
                                    }
                                }

                                currentStep++;
                                retryCount = 0;
                                sourceItems = null;
                                destinationItems = null;
                                storeResult = null;
                            }
                        }
                        else if (retryResult == RetryAction.Skip)
                        {
                            // Run batch completed function
                            if (this.BatchCompleted is not null)
                            {
                                var batchCompletedResult = await this.BatchCompleted!(new SyncFunctionInput<SyncBatchCompleteRetryInput<TSource, TDestination>>(this.GetCancellationToken(), new SyncBatchCompleteRetryInput<TSource, TDestination>(sourceItems, storeResult, new SyncActionStatus(currentStep, totalSteps, batchSize, totalItemCount, maxRetryCount, retryCount, actionType))));

                                if (!batchCompletedResult)
                                {
                                    result = false;
                                    break;
                                }
                            }

                            currentStep++;
                            retryCount = 0;
                            sourceItems = null;
                            destinationItems = null;
                            storeResult = null;
                        }
                        else if (retryResult == RetryAction.Stop)
                        {
                            result = false;
                            break;
                        }
                        else if (retryResult == RetryAction.RetryAndStopAfterLastRetry)
                        {
                            retryCount++;

                            if (retryCount > maxRetryCount)
                            {
                                result = false;
                                break;
                            }
                        }
                    }
                }
            }
            else
            {
                result = true;
            }
        }
        
        // Run operation completed function
        if (this.ActionCompleted is not null)
            result = await this.ActionCompleted!(new SyncFunctionInput<SyncActionCompletedInput>(this.GetCancellationToken(), new(actionType, result)));

        return result;
    }

    private void CheckSetups()
    {
        if (this.Configurations == null)
            throw new InvalidOperationException("SyncConfigurations is not set.");
        if (this.GetSourceBatchItems == null)
            throw new InvalidOperationException("GetSourceBatchItems function is not set.");
        if (this.StoreBatchData == null)
            throw new InvalidOperationException("StoreBatchData function is not set.");
        if (this.Mapping == null)
            throw new InvalidOperationException("Mapping function is not set.");
    }

    public virtual CancellationToken GetCancellationToken()
    {
        return this.cancellationTokenSource!.Token;
    }

    private async Task CheckForCancellation()
    {
        if (GetCancellationToken().IsCancellationRequested)
        {
            await this.Failed!(new(GetCancellationToken()));
            throw new OperationCanceledException();
        }
    }

    public virtual ValueTask Reset()
    {
        this.Configurations = null;
        this.Preparing = null;
        this.SourceTotalItemCount = null;
        this.GetSourceBatchItems = null;
        this.StoreBatchData = null;
        this.BatchRetry = null;
        this.BatchCompleted = null;
        this.ActionCompleted = null;
        this.cancellationTokenSource = null;
        this.Mapping = null;
        this.Failed = null;
        this.Succeeded = null;
        this.Finished = null;
        return new ValueTask();
    }

    public virtual ValueTask DisposeAsync()
    {
        Reset();
        return new ValueTask();
    }

    public virtual ISyncService<TSource, TDestination> SetupFinished(Func<SyncFunctionInput, ValueTask> finishedFunc)
    {
        this.Finished = finishedFunc;
        return this;
    }

    public virtual ISyncService<TSource, TDestination> SetDataAddapter<TConfigurations, TImplementer>(ISyncDataAdapter<TSource, TDestination, TConfigurations, TImplementer> dataAdapter, TConfigurations configurations) 
        where TImplementer : ISyncDataAdapter<TSource, TDestination, TConfigurations, TImplementer>
    {

        dataAdapter.SetSyncService(this).Configure(configurations);

        return this;
    }

    public virtual TDataAdapter SetDataAddapter<TDataAdapter>(IServiceProvider services) where TDataAdapter : ISyncDataAdapter<TSource, TDestination, TDataAdapter>
    {
        var dataAdapter = services.GetRequiredService<TDataAdapter>();
        dataAdapter.SetSyncService(this);

        return dataAdapter;
    }

    public virtual TDataAdapter SetDataAddapter<TDataAdapter>() where TDataAdapter : ISyncDataAdapter<TSource, TDestination, TDataAdapter>
    {
        var dataAdapter = this.services.GetRequiredService<TDataAdapter>();
        dataAdapter.SetSyncService(this);

        return dataAdapter;
    }
}