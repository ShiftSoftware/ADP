using System;
using ShiftSoftware.ShiftEntity.Model;
using ShiftSoftware.ShiftEntity.Web.Explorer;
using ShiftSoftware.ShiftEntity.Web.Services;

namespace Microsoft.Extensions.DependencyInjection;

public static class FileExplorerBuilderExtensions
{
	public static IServiceCollection AddFileExplorer(this IServiceCollection builder, Action<FileExplorerConfiguration> action)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Expected O, but got Unknown
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		//IL_002c: Invalid comparison between Unknown and I4
		FileExplorerConfiguration val = new FileExplorerConfiguration();
		action(val);
		builder.AddOptions<FileExplorerConfiguration>().Configure(action.Invoke);
		if ((int)val.FileExplorerService == 1)
		{
			builder.AddScoped<IFileProvider, BlobStorageFileProvider>();
		}
		return builder;
	}
}
