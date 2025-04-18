using Microsoft.Azure.Cosmos;
using ShiftSoftware.ADP.SyncAgent.Entities;
using ShiftSoftware.ADP.SyncAgent.Extensions;
using ShiftSoftware.ShiftEntity.Model.Enums;
using System.Linq.Expressions;

namespace ShiftSoftware.ADP.SyncAgent;

public class Utility
{
    public static string GetPropertyName<T>(Expression<Func<T, object?>> expression)
    {
        if (expression.Body is MemberExpression memberExpression)
        {
            return memberExpression.Member.Name;
        }

        if (expression.Body is UnaryExpression unaryExpression && unaryExpression.Operand is MemberExpression operand)
        {
            return operand.Member.Name;
        }

        throw new ArgumentException("Invalid expression");
    }

    public static string GetPropertyName<T>(Expression<Func<T, bool?>> expression)
    {
        if (expression.Body is MemberExpression memberExpression)
        {
            return memberExpression.Member.Name;
        }

        if (expression.Body is UnaryExpression unaryExpression && unaryExpression.Operand is MemberExpression operand)
        {
            return operand.Member.Name;
        }

        throw new ArgumentException("Invalid expression");
    }

    public static PartitionKey GetPartitionKey<T>(T item,
        Expression<Func<T, object>> partitionKeyLevel1Expression,
        Expression<Func<T, object>>? partitionKeyLevel2Expression,
        Expression<Func<T, object>>? partitionKeyLevel3Expression)
    {
        var partitonKeyBuilder = new PartitionKeyBuilder();

        if (partitionKeyLevel1Expression != null)
        {
            var detail = GetPartitionKeyDetail(item, partitionKeyLevel1Expression);
            partitonKeyBuilder.Add(detail);
        }

        if (partitionKeyLevel2Expression != null)
        {
            var detail = GetPartitionKeyDetail(item, partitionKeyLevel2Expression);
            partitonKeyBuilder.Add(detail);
        }

        if (partitionKeyLevel3Expression != null)
        {
            var detail = GetPartitionKeyDetail(item, partitionKeyLevel3Expression);
            partitonKeyBuilder.Add(detail);
        }

        return partitonKeyBuilder.Build();
    }

    public static (object? value, Type type, string? propertyName)? GetPartitionKeyDetail<T>(object data,
        Expression<Func<T, object>> partitionKeyExpression)
    {
        var value = partitionKeyExpression.Compile().Invoke((T)data);
        var type = partitionKeyExpression.Body.Type;
        var propertyName = null as string;

        // Check if the body of the expression is a UnaryExpression
        if (partitionKeyExpression.Body is UnaryExpression unaryExpression)
        {
            // If it is, use the Operand property to get the underlying expression
            type = unaryExpression.Operand.Type;
        }

        // Check if the body of the expression is a MemberExpression
        if (partitionKeyExpression.Body is MemberExpression memberExpression)
            propertyName = memberExpression.Member.Name;

        return (value, type, propertyName);
    }

    internal static PartitionKey GetPartitionKey(DeletedRowLog row)
    {
        var builder = new PartitionKeyBuilder();

        AddPrtitionKey(builder, row.PartitionKeyLevelOneValue, row.PartitionKeyLevelOneType);
        AddPrtitionKey(builder, row.PartitionKeyLevelTwoValue, row.PartitionKeyLevelTwoType);
        AddPrtitionKey(builder, row.PartitionKeyLevelThreeValue, row.PartitionKeyLevelThreeType);


        return builder.Build();
    }

    internal static void AddPrtitionKey(PartitionKeyBuilder builder, string? value, PartitionKeyTypes type)
    {
        if (type == PartitionKeyTypes.String)
            builder.Add(value);
        else if (type == PartitionKeyTypes.Numeric)
            builder.Add(double.Parse(value));
        else if (type == PartitionKeyTypes.Boolean)
            builder.Add(bool.Parse(value));
    }

    public static async Task CreateDatabasesAndContainersIfNotExistsAsync(CosmosClient client)
    {
        var companyDatabaseResponse = await client.CreateDatabaseIfNotExistsAsync(
        Models.Constants.NoSQLConstants.Databases.CompanyData,
            ThroughputProperties.CreateManualThroughput(100_000)
        );

        var logsDatabaseResponse = await client.CreateDatabaseIfNotExistsAsync(
            Models.Constants.NoSQLConstants.Databases.Logs,
            ThroughputProperties.CreateManualThroughput(100_000)
        );

        var serviceDatabaseResponse = await client.CreateDatabaseIfNotExistsAsync(
            Models.Constants.NoSQLConstants.Databases.Services,
            ThroughputProperties.CreateManualThroughput(100_000)
        );

        var companyDatabase = companyDatabaseResponse.Database;

        var logsDatabase = logsDatabaseResponse.Database;

        var serviceDatabase = serviceDatabaseResponse.Database;

        await companyDatabase.CreateContainerIfNotExistsAsync(new ContainerProperties(
            Models.Constants.NoSQLConstants.Containers.Brokers,
            "/id"
        ));

        await companyDatabase.CreateContainerIfNotExistsAsync(new ContainerProperties(
            Models.Constants.NoSQLConstants.Containers.Vehicles,
            [Models.Constants.NoSQLConstants.PartitionKeys.Vehicles.Level1, Models.Constants.NoSQLConstants.PartitionKeys.Vehicles.Level2]
        ));

        await companyDatabase.CreateContainerIfNotExistsAsync(new ContainerProperties(
            Models.Constants.NoSQLConstants.Containers.Parts,
            [Models.Constants.NoSQLConstants.PartitionKeys.Parts.Level1, Models.Constants.NoSQLConstants.PartitionKeys.Parts.Level2, Models.Constants.NoSQLConstants.PartitionKeys.Parts.Level3]
        ));

        await companyDatabase.CreateContainerIfNotExistsAsync(new ContainerProperties(
            Models.Constants.NoSQLConstants.Containers.ExteriorColors,
            [Models.Constants.NoSQLConstants.PartitionKeys.ExteriorColors.Level1, Models.Constants.NoSQLConstants.PartitionKeys.ExteriorColors.Level2]
        ));

        await companyDatabase.CreateContainerIfNotExistsAsync(new ContainerProperties(
            Models.Constants.NoSQLConstants.Containers.InteriorColors,
            [Models.Constants.NoSQLConstants.PartitionKeys.InteriorColors.Level1, Models.Constants.NoSQLConstants.PartitionKeys.InteriorColors.Level2]
        ));

        await companyDatabase.CreateContainerIfNotExistsAsync(new ContainerProperties(
            Models.Constants.NoSQLConstants.Containers.VehicleModels,
            [Models.Constants.NoSQLConstants.PartitionKeys.VehicleModels.Level1, Models.Constants.NoSQLConstants.PartitionKeys.VehicleModels.Level2]
        ));

        await logsDatabase.CreateContainerIfNotExistsAsync(new ContainerProperties(
            Models.Constants.NoSQLConstants.Containers.PartLookupLogs,
            [Models.Constants.NoSQLConstants.PartitionKeys.PartLookupLogs.Level1]
        ));

        await logsDatabase.CreateContainerIfNotExistsAsync(new ContainerProperties(
            Models.Constants.NoSQLConstants.Containers.CSVUpload,
            "/id"
        ));

        await logsDatabase.CreateContainerIfNotExistsAsync(new ContainerProperties(
            Models.Constants.NoSQLConstants.Containers.SSCLogs,
            [Models.Constants.NoSQLConstants.PartitionKeys.SSCLogs.Level1]
        ));

        await serviceDatabase.CreateContainerIfNotExistsAsync(new ContainerProperties(
            Models.Constants.NoSQLConstants.Containers.FlatRate,
            [Models.Constants.NoSQLConstants.PartitionKeys.FlatRate.Level1, Models.Constants.NoSQLConstants.PartitionKeys.FlatRate.Level2]
        ));

        await serviceDatabase.CreateContainerIfNotExistsAsync(new ContainerProperties(
            Models.Constants.NoSQLConstants.Containers.ServiceItems,
            "/id"
        ));

        await serviceDatabase.CreateContainerIfNotExistsAsync(new ContainerProperties(
            Models.Constants.NoSQLConstants.Containers.ClaimableItemCampaigns,
            "/id"
        ));
    }
}