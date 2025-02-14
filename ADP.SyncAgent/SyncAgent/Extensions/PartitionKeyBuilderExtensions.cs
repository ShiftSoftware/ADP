using Microsoft.Azure.Cosmos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ADP.SyncAgent.Extensions;

public static class PartitionKeyBuilderExtensions
{
    public static PartitionKeyBuilder Add(this PartitionKeyBuilder builder,
        (object? value, Type type, string? propertyName)? partitionKeyDetail)
    {
        if (partitionKeyDetail == null)
            return builder;

        if (partitionKeyDetail.Value.type == typeof(string))
            builder.Add(Convert.ToString(partitionKeyDetail.Value.value));
        else if (partitionKeyDetail.Value.type.IsNumericType() || partitionKeyDetail.Value.type.IsEnum)
            builder.Add(Convert.ToDouble(partitionKeyDetail.Value.value));
        else if (partitionKeyDetail.Value.type == typeof(bool) || partitionKeyDetail.Value.type == typeof(bool?))
            builder.Add(Convert.ToBoolean(partitionKeyDetail.Value.value));
        else
            throw new ArgumentException($"The type or value of '{partitionKeyDetail.Value.propertyName}' partition key is incorrect");

        return builder;
    }
}
