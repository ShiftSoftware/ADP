using System.Text.Json.Serialization;
using ShiftSoftware.ShiftEntity.Core.Services;

namespace Microsoft.AspNetCore.Mvc;

public static class MvcJsonOptionsExtensions
{
	public static JsonOptions RegisterAzureStorageServiceConverters(this JsonOptions options, AzureStorageService? azureStorageService)
	{
		//IL_0011: Unknown result type (might be due to invalid IL or missing references)
		//IL_001b: Expected O, but got Unknown
		if (azureStorageService == null)
		{
			return options;
		}
		options.JsonSerializerOptions.Converters.Add((JsonConverter)new JsonShiftFileDTOConverter(azureStorageService));
		return options;
	}
}
