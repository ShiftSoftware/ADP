namespace ShiftSoftware.ADP.SyncAgent.Extensions;

public static class IEnumerableExtensions
{
    public static async Task<IEnumerable<ISyncEngineLogger>?> LogInformation(this IEnumerable<ISyncEngineLogger>? loggers, string message, params object?[] args)
    {
        foreach (var logger in loggers ?? [])
            await logger.LogInformation(message, args);

        return loggers;
    }

    public static async Task<IEnumerable<ISyncEngineLogger>?> LogError(this IEnumerable<ISyncEngineLogger>? loggers, string message, params object?[] args)
    {
        foreach (var logger in loggers ?? [])
            await logger.LogError(message, args);
    
        return loggers;
    }

    public static async Task<IEnumerable<ISyncEngineLogger>?> LogError(this IEnumerable<ISyncEngineLogger>? loggers, Exception? exception, string? message, params object?[] args)
    {
        foreach (var logger in loggers ?? [])
            await logger.LogError(exception, message, args);

        return loggers;
    }

    public static async Task<IEnumerable<ISyncEngineLogger>?> LogWarning(this IEnumerable<ISyncEngineLogger>? loggers, string message, params object?[] args)
    {
        foreach (var logger in loggers ?? [])
            await logger.LogWarning(message,args);
        
        return loggers;
    }
}
