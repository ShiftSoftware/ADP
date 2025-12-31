using Microsoft.Azure.Cosmos;
using System.Collections;
using System.Linq.Expressions;
using System.Reflection;

namespace ShiftSoftware.ADP.SyncAgent;

internal class CosmosPatchOperationHelper
{
    /// <summary>
    /// Build patch operations from property expressions or all properties if none specified
    /// </summary>
    public static List<PatchOperation> BuildPatchOperations<T>(T? item, params Expression<Func<T, object?>>[]? propertiesToPatch)
        where T : class
    {
        if (item is null)
            return [];

        if (propertiesToPatch?.Any() != true)
        {
            // Patch all properties recursively
            return BuildAllPatchOperations(item, typeof(T), "");
        }
        else
        {
            // Patch only specified properties
            return BuildPatchOperationsFromExpressions(item, propertiesToPatch);
        }
    }

    /// <summary>
    /// Build patch operations for ALL properties recursively
    /// </summary>
    private static List<PatchOperation> BuildAllPatchOperations(object? obj, Type type, string currentPath)
    {
        var patchOperations = new List<PatchOperation>();

        if (obj == null)
        {
            return patchOperations;
        }

        var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);

        foreach (var property in properties)
        {
            var propertyName = property.Name;
            var jsonPath = string.IsNullOrEmpty(currentPath)
                ? $"/{propertyName}"
                : $"{currentPath}/{propertyName}";

            // Skip id and partition key properties at root level
            if (string.IsNullOrEmpty(currentPath) && (propertyName == "id" || propertyName == "P"))
            {
                continue;
            }

            var value = property.GetValue(obj);
            var propertyType = property.PropertyType;

            // Check if it's a complex type that should be recursively processed
            if (IsComplexType(propertyType) && value != null)
            {
                // Recursively process nested object
                var nestedOperations = BuildAllPatchOperations(value, propertyType, jsonPath);
                patchOperations.AddRange(nestedOperations);
            }
            else
            {
                // Simple type or collection - add patch operation directly
                patchOperations.Add(PatchOperation.Set(jsonPath, value));
            }
        }

        return patchOperations;
    }

    /// <summary>
    /// Check if a type is a complex type that should be recursively processed
    /// </summary>
    private static bool IsComplexType(Type type)
    {
        // Not a complex type if:
        // - Primitive types (int, bool, etc.)
        // - String
        // - Decimal
        // - DateTime/DateTimeOffset/TimeSpan
        // - Guid
        // - Enum
        // - Nullable of above types
        // - Collections (List, Array, etc.) - these are handled as a whole

        if (type.IsPrimitive)
            return false;

        if (type == typeof(string))
            return false;

        if (type == typeof(decimal))
            return false;

        if (type == typeof(DateTime) || type == typeof(DateTimeOffset) || type == typeof(TimeSpan))
            return false;

        if (type == typeof(Guid))
            return false;

        if (type.IsEnum)
            return false;

        // Check for nullable types
        var underlyingType = Nullable.GetUnderlyingType(type);
        if (underlyingType != null)
            return IsComplexType(underlyingType);

        // Collections are not complex types (patched as whole)
        if (typeof(IEnumerable).IsAssignableFrom(type) && type != typeof(string))
            return false;

        // It's a complex type (custom class/struct)
        return type.IsClass || type.IsValueType;
    }

    /// <summary>
    /// Build patch operations from property expressions
    /// </summary>
    private static List<PatchOperation> BuildPatchOperationsFromExpressions<T>(T item, Expression<Func<T, object?>>[] propertyExpressions)
        where T : class
    {
        var patchOperations = new List<PatchOperation>();

        foreach (var expression in propertyExpressions)
        {
            var (jsonPath, value) = GetPropertyPathAndValue(item, expression);

            if (string.IsNullOrEmpty(jsonPath))
            {
                Console.WriteLine($"Warning: Could not extract property path from expression");
                continue;
            }

            // Skip id and partition key properties
            if (jsonPath == "/id" || jsonPath == "/P")
            {
                continue;
            }

            patchOperations.Add(PatchOperation.Set(jsonPath, value));
        }

        return patchOperations;
    }

    /// <summary>
    /// Extract property path and value from expression (supports nested properties)
    /// </summary>
    private static (string jsonPath, object? value) GetPropertyPathAndValue<T>(T item, Expression<Func<T, object?>> expression)
    {
        var memberExpression = ExtractMemberExpression(expression.Body);

        if (memberExpression == null)
        {
            return (string.Empty, null);
        }

        // Build the property path by traversing the expression tree
        var pathSegments = new List<string>();
        var currentExpression = memberExpression;

        while (currentExpression != null)
        {
            pathSegments.Insert(0, currentExpression.Member.Name);

            if (currentExpression.Expression is MemberExpression parentMember)
            {
                currentExpression = parentMember;
            }
            else
            {
                break;
            }
        }

        // Build JSON path for Cosmos DB (e.g., /Child/Age)
        var jsonPath = "/" + string.Join("/", pathSegments);

        // Get the actual value by evaluating the expression
        var compiledExpression = expression.Compile();
        var value = compiledExpression(item);

        return (jsonPath, value);
    }

    /// <summary>
    /// Extract MemberExpression from expression body (handles boxing/unboxing)
    /// </summary>
    private static MemberExpression? ExtractMemberExpression(Expression expression)
    {
        if (expression is MemberExpression memberExpression)
        {
            return memberExpression;
        }
        else if (expression is UnaryExpression unaryExpression &&
                 unaryExpression.Operand is MemberExpression operandExpression)
        {
            // Handle boxing conversions (e.g., value types to object)
            return operandExpression;
        }

        return null;
    }
}
