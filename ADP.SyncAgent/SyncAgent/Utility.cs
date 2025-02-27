using ADP.SyncAgent.Entities;
using ADP.SyncAgent.Extensions;
using Microsoft.Azure.Cosmos;
using ShiftSoftware.ShiftEntity.Model.Enums;
using System.Linq.Expressions;

namespace ADP.SyncAgent;

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
}