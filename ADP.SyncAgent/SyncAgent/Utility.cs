using Microsoft.Azure.Cosmos;
using ShiftSoftware.ADP.Models.Constants;
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

    public static string GetPropertyName<T>(Expression<Func<T, DateTimeOffset?>> expression)
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

    public static async Task CreateDatabasesAndContainersIfNotExistsAsync(
        CosmosClient client,
        int? companyDatabaseThroughput = 25_000,
        int? logsDatabaseThroughput = 25_000,
        int? serviceDatabaseThroughput = 25_000,
        int? customerDatabaseThroughput = 25_000,
        int? tbpDatabaseThroughput = 25_000)
    {
        if (companyDatabaseThroughput is not null)
        {
            var companyDatabaseResponse = await client.CreateDatabaseIfNotExistsAsync(
                NoSQLConstants.Databases.CompanyData,
                ThroughputProperties.CreateManualThroughput(companyDatabaseThroughput.Value)
            );
            var companyDatabase = companyDatabaseResponse.Database;

            await companyDatabase.CreateContainerIfNotExistsAsync(new ContainerProperties(
                NoSQLConstants.Containers.Brokers,
                "/id"
            ));

            await companyDatabase.CreateContainerIfNotExistsAsync(
                new ContainerProperties(
                NoSQLConstants.Containers.Vehicles,
                [
                    NoSQLConstants.PartitionKeys.Vehicles.Level1,
                    NoSQLConstants.PartitionKeys.Vehicles.Level2,
                    NoSQLConstants.PartitionKeys.Vehicles.Level3,
                ]
            ));

            await companyDatabase.CreateContainerIfNotExistsAsync(new ContainerProperties(
                NoSQLConstants.Containers.Parts,
                [NoSQLConstants.PartitionKeys.Parts.Level1, NoSQLConstants.PartitionKeys.Parts.Level2, NoSQLConstants.PartitionKeys.Parts.Level3]
            ));

            await companyDatabase.CreateContainerIfNotExistsAsync(new ContainerProperties(
                NoSQLConstants.Containers.ExteriorColors,
                [NoSQLConstants.PartitionKeys.ExteriorColors.Level1, NoSQLConstants.PartitionKeys.ExteriorColors.Level2]
            ));

            await companyDatabase.CreateContainerIfNotExistsAsync(new ContainerProperties(
                NoSQLConstants.Containers.InteriorColors,
                [NoSQLConstants.PartitionKeys.InteriorColors.Level1, NoSQLConstants.PartitionKeys.InteriorColors.Level2]
            ));

            await companyDatabase.CreateContainerIfNotExistsAsync(new ContainerProperties(
                NoSQLConstants.Containers.VehicleModels,
                [NoSQLConstants.PartitionKeys.VehicleModels.Level1, NoSQLConstants.PartitionKeys.VehicleModels.Level2]
            ));
        }

        if (logsDatabaseThroughput is not null)
        {
            var logsDatabaseResponse = await client.CreateDatabaseIfNotExistsAsync(
                NoSQLConstants.Databases.Logs,
                ThroughputProperties.CreateManualThroughput(logsDatabaseThroughput.Value)
            );

            var logsDatabase = logsDatabaseResponse.Database;

            await logsDatabase.CreateContainerIfNotExistsAsync(new ContainerProperties(
                NoSQLConstants.Containers.PartLookupLogs,
                [NoSQLConstants.PartitionKeys.PartLookupLogs.Level1]
            ));

            await logsDatabase.CreateContainerIfNotExistsAsync(new ContainerProperties(
                NoSQLConstants.Containers.ManufacturerPartLookupLogs,
                [NoSQLConstants.PartitionKeys.ManufacturerPartLookupLogs.Level1]
            ));

            await logsDatabase.CreateContainerIfNotExistsAsync(new ContainerProperties(
                NoSQLConstants.Containers.CSVUpload,
                "/id"
            ));

            await logsDatabase.CreateContainerIfNotExistsAsync(new ContainerProperties(
                NoSQLConstants.Containers.SSCLogs,
                [NoSQLConstants.PartitionKeys.SSCLogs.Level1]
            ));
        }

        if (serviceDatabaseThroughput is not null)
        {
            var serviceDatabaseResponse = await client.CreateDatabaseIfNotExistsAsync(
                NoSQLConstants.Databases.Services,
                ThroughputProperties.CreateManualThroughput(serviceDatabaseThroughput.Value)
            );

            var serviceDatabase = serviceDatabaseResponse.Database;

            await serviceDatabase.CreateContainerIfNotExistsAsync(new ContainerProperties(
                NoSQLConstants.Containers.FlatRate,
                [NoSQLConstants.PartitionKeys.FlatRate.Level1, NoSQLConstants.PartitionKeys.FlatRate.Level2]
            ));

            await serviceDatabase.CreateContainerIfNotExistsAsync(new ContainerProperties(
                NoSQLConstants.Containers.ServiceItems,
                "/id"
            ));

            await serviceDatabase.CreateContainerIfNotExistsAsync(new ContainerProperties(
                NoSQLConstants.Containers.ClaimableItemCampaigns,
                "/id"
            ));
        }

        if (customerDatabaseThroughput is not null)
        {
            var customerDatabaseResponse = await client.CreateDatabaseIfNotExistsAsync(
                NoSQLConstants.Databases.Customers,
                ThroughputProperties.CreateManualThroughput(customerDatabaseThroughput.Value)
            );

            var customerDatabase = customerDatabaseResponse.Database;

            await customerDatabase.CreateContainerIfNotExistsAsync(new ContainerProperties(
                NoSQLConstants.Containers.Customers_Customers,
                [NoSQLConstants.PartitionKeys.Customers.Level1, NoSQLConstants.PartitionKeys.Customers.Level2, NoSQLConstants.PartitionKeys.Customers.Level3]
            ));
        }

        if (tbpDatabaseThroughput is not null)
        {
            var tbpDatabaseResponse = await client.CreateDatabaseIfNotExistsAsync(
                NoSQLConstants.Databases.TBP,
                ThroughputProperties.CreateManualThroughput(tbpDatabaseThroughput.Value)
            );

            var tbpDatabase = tbpDatabaseResponse.Database;

            await tbpDatabase.CreateContainerIfNotExistsAsync(new ContainerProperties(
                NoSQLConstants.Containers.TBP_BrokerStock,
                [NoSQLConstants.PartitionKeys.TBPBrokerStock.Level1, NoSQLConstants.PartitionKeys.TBPBrokerStock.Level2, NoSQLConstants.PartitionKeys.TBPBrokerStock.Level3]
            ));
        }
    }
}