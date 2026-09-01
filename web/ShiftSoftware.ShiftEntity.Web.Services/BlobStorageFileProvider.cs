using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs.Specialized;
using Azure.Storage.Sas;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Options;
using ShiftSoftware.ShiftEntity.Core;
using ShiftSoftware.ShiftEntity.Core.Services;
using ShiftSoftware.ShiftEntity.Model;
using ShiftSoftware.ShiftEntity.Model.Enums;
using ShiftSoftware.ShiftEntity.Model.FileExplorer;
using ShiftSoftware.ShiftEntity.Model.FileExplorer.Dtos;
using ShiftSoftware.ShiftEntity.Web.Explorer;
using ShiftSoftware.TypeAuth.Core;

namespace ShiftSoftware.ShiftEntity.Web.Services;

public class BlobStorageFileProvider : IFileProvider
{
	private readonly AzureStorageService azureStorageService;

	private readonly AzureStorageOption storageOption;

	private readonly IdentityClaimProvider identityClaimProvider;

	private readonly Container? cosmosContainer;

	private readonly FileExplorerConfiguration config;

	private readonly IFileExplorerAccessControl? fileExplorerAccessControl;

	private readonly ITypeAuthService? typeAuthService;

	private const int MAX_CREATE_RETRY_ATTEMPTS = 25;

	private static readonly HashSet<string> ImageExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".jpg", ".jpeg", ".png", ".webp" };

	public char Delimiter => '/';

	public BlobStorageFileProvider(AzureStorageService azureStorageService, IOptions<FileExplorerConfiguration> config, IdentityClaimProvider identityClaimProvider, ITypeAuthService typeAuthService, IFileExplorerAccessControl? fileExplorerAccessControl = null, CosmosClient? cosmosClient = null)
	{
		this.azureStorageService = azureStorageService;
		storageOption = azureStorageService.GetStorageOption((string)null);
		this.identityClaimProvider = identityClaimProvider;
		this.fileExplorerAccessControl = fileExplorerAccessControl;
		this.config = config.Value;
		this.typeAuthService = typeAuthService;
		AzureStorageOption obj = storageOption;
		if (obj == null || !obj.SupportsFileExplorer)
		{
			AzureStorageOption obj2 = storageOption;
			throw new Exception("FileExplorer not supported for storage account (" + ((obj2 != null) ? obj2.AccountName : null) + ")");
		}
		try
		{
			if (cosmosClient != null && config.Value != null && !string.IsNullOrWhiteSpace(config.Value.DatabaseId) && !string.IsNullOrWhiteSpace(config.Value.ContainerId))
			{
				cosmosContainer = cosmosClient.GetContainer(config.Value.DatabaseId, config.Value.ContainerId);
			}
		}
		catch
		{
		}
	}

	private async Task<(List<string> list, BlobClient blob)> GetDeletedItems(string path, BlobContainerClient container)
	{
		string text = BlobHelper.Combine(new string[2] { path, "f7751140-2a59-4edf-955c-406eef7fa0d4" });
		BlobClient blob = container.GetBlobClient(text);
		List<string> list = new List<string>();
		try
		{
			if (((NullableResponse<BlobProperties>)(object)(await ((BlobBaseClient)blob).GetPropertiesAsync((BlobRequestConditions)null, default(CancellationToken)).ConfigureAwait(continueOnCapturedContext: false))).Value.ContentLength > 0)
			{
				string text2 = ((object)Response<BlobDownloadResult>.op_Implicit(await ((BlobBaseClient)blob).DownloadContentAsync().ConfigureAwait(continueOnCapturedContext: false)).Content).ToString();
				list = text2.Split(new char[2] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList() ?? new List<string>();
			}
		}
		catch (object obj) when (((Func<bool>)delegate
		{
			// Could not convert BlockContainer to single expression
			object obj2 = ((obj is RequestFailedException) ? obj : null);
			return obj2 != null && ((RequestFailedException)obj2).Status == 404;
		}).Invoke())
		{
		}
		return (list: list, blob: blob);
	}

	private async Task EnsurePathNotDeletedAsync(string path, BlobContainerClient container)
	{
		string[] array = path?.Split(Delimiter, StringSplitOptions.RemoveEmptyEntries);
		if (array == null || array.Length == 0)
		{
			return;
		}
		string currentPath = "";
		string fullPath = Delimiter + path;
		string[] array2 = array;
		foreach (string part in array2)
		{
			if ((await GetDeletedItems(currentPath, container)).Item1.Any((string p) => fullPath.StartsWith(p, StringComparison.Ordinal)))
			{
				throw new DirectoryNotFoundException();
			}
			currentPath = currentPath + part + Delimiter;
		}
	}

	public async Task<FileExplorerResponseDTO> GetFiles(FileExplorerReadDTO data)
	{
		string path = ((FileExplorerRequestDTOBase)data).Path ?? "";
		FileExplorerResponseDTO res = new FileExplorerResponseDTO(path);
		if (!BlobHelper.IsPathDirectory(path))
		{
			res.Message = new Message("Invalid path");
			return res;
		}
		BlobContainerClient container = azureStorageService.GetBlobContainerClient(((FileExplorerRequestDTOBase)data).AccountName, ((FileExplorerRequestDTOBase)data).ContainerName);
		IAsyncEnumerable<Page<BlobHierarchyItem>> asyncEnumerable = container.GetBlobsByHierarchyAsync(new GetBlobsByHierarchyOptions
		{
			Traits = (BlobTraits)2,
			Delimiter = Delimiter.ToString(),
			Prefix = path
		}, default(CancellationToken)).AsPages(data.ContinuationToken, (int?)config.PageSizeHint);
		Page<BlobHierarchyItem> workingPage = null;
		await using (IAsyncEnumerator<Page<BlobHierarchyItem>> asyncEnumerator = asyncEnumerable.GetAsyncEnumerator())
		{
			if (await asyncEnumerator.MoveNextAsync())
			{
				Page<BlobHierarchyItem> current = asyncEnumerator.Current;
				workingPage = current;
			}
		}
		if (workingPage == null || workingPage.Values.Count == 0)
		{
			res.Message = new Message("Directory not found");
			return res;
		}
		res.ContinuationToken = workingPage.ContinuationToken;
		ITypeAuthService? obj = typeAuthService;
		bool canViewDeletedFiles = obj == null || obj.CanAccess(AzureStorageActionTree.ViewDeletedFiles);
		try
		{
			if (!canViewDeletedFiles || !data.IncludeDeleted)
			{
				await EnsurePathNotDeletedAsync(path, container);
			}
		}
		catch (DirectoryNotFoundException)
		{
			res.Message = new Message("Directory not found");
			return res;
		}
		List<FileExplorerItemDTO> files = new List<FileExplorerItemDTO>();
		(List<string>, BlobClient) tuple = await GetDeletedItems(path, container);
		foreach (BlobHierarchyItem value6 in workingPage.Values)
		{
			FileExplorerItemDTO val = new FileExplorerItemDTO();
			string text = (value6.IsBlob ? value6.Blob.Name : value6.Prefix);
			if (tuple.Item1.Contains(Delimiter + text))
			{
				if (!canViewDeletedFiles || !data.IncludeDeleted)
				{
					continue;
				}
				val.IsDeleted = true;
			}
			if (value6.IsBlob)
			{
				bool num = value6.Blob.Name.EndsWith("f7751140-2a59-4edf-955c-406eef7fa0d4");
				string value;
				bool flag = value6.Blob.Metadata.TryGetValue("hidden", out value);
				if (num || flag)
				{
					continue;
				}
				value6.Blob.Metadata.TryGetValue("name", out var value2);
				value6.Blob.Metadata.TryGetValue("createdById", out var value3);
				val.Name = HttpUtility.UrlDecode(value2) ?? BlobHelper.GetName(value6.Blob.Name);
				val.Path = value6.Blob.Name;
				val.Type = Path.GetExtension(val.Name)?.ToLower();
				val.IsFile = true;
				val.Size = value6.Blob.Properties.ContentLength.GetValueOrDefault();
				val.CreatedDate = value6.Blob.Properties.CreatedOn?.UtcDateTime ?? default(DateTime);
				val.DateModified = value6.Blob.Properties.LastModified?.UtcDateTime ?? default(DateTime);
				val.CreatedBy = value3;
				val.Url = azureStorageService.GetSignedURL(value6.Blob.Name, (BlobSasPermissions)1, container.Name, (string)null, (int?)null);
				if (val.Type != null && ImageExtensions.Contains(val.Type))
				{
					value6.Blob.Metadata.TryGetValue("sizes", out var value4);
					string text2 = container.AccountName + "_" + container.Name;
					string value5 = value4?.Split("|").First() ?? "250x250";
					ValueTuple<string, string> valueTuple = BlobHelper.PathAndName(value6.Blob.Name);
					string item = valueTuple.Item1;
					string item2 = valueTuple.Item2;
					string text3 = $"{item}{item2}_{value5}.png";
					string text4 = BlobHelper.Combine(new string[2] { text2, text3 });
					val.ThumbnailUrl = azureStorageService.GetSignedURL(text4, (BlobSasPermissions)1, storageOption.ThumbnailContainerName, storageOption.AccountName, (int?)null);
				}
			}
			else if (value6.IsPrefix)
			{
				val.Name = BlobHelper.GetName(value6.Prefix);
				val.Type = "Directory";
				val.Path = value6.Prefix;
			}
			files.Add(val);
		}
		if (fileExplorerAccessControl != null)
		{
			IEnumerable<string> files2 = files.Select((FileExplorerItemDTO x) => x.Path);
			IEnumerable<string> accessList = await fileExplorerAccessControl.FilterWithReadAccessAsync(container, files2);
			files = files.Where((FileExplorerItemDTO x) => accessList.Contains(x.Path)).ToList();
		}
		res.Items = files;
		res.Success = true;
		return res;
	}

	public async Task<FileExplorerResponseDTO> Create(FileExplorerCreateDTO data)
	{
		FileExplorerResponseDTO res = new FileExplorerResponseDTO(((FileExplorerRequestDTOBase)data).Path);
		if (((FileExplorerRequestDTOBase)data).Path == null)
		{
			res.Message = new Message("Invalid path");
			return res;
		}
		string text = ((fileExplorerAccessControl == null) ? ((FileExplorerRequestDTOBase)data).Path : fileExplorerAccessControl.FilterWithWriteAccess(new global::_003C_003Ez__ReadOnlySingleElementList<string>(((FileExplorerRequestDTOBase)data).Path)).FirstOrDefault());
		string dir;
		string name;
		(dir, name) = BlobHelper.PathAndName(text);
		if (text == null || !BlobHelper.IsPathDirectory(text) || string.IsNullOrWhiteSpace(name))
		{
			res.Message = new Message("Invalid path");
			return res;
		}
		name = name.Trim();
		string newPath = dir + name + Delimiter;
		Unsafe.SkipInit(out object obj2);
		for (int i = 1; i <= 25; i++)
		{
			try
			{
				BlobContainerClient container = azureStorageService.GetBlobContainerClient(((FileExplorerRequestDTOBase)data).AccountName, ((FileExplorerRequestDTOBase)data).ContainerName);
				await container.GetBlobClient(newPath + "f7751140-2a59-4edf-955c-406eef7fa0d4").UploadAsync(BinaryData.FromBytes(Array.Empty<byte>()), false, default(CancellationToken));
				CreateLogItem(newPath, (FileExplorerAction)1, container);
				res.Success = true;
				res.Path = newPath;
				return res;
			}
			catch (object obj) when (((Func<bool>)delegate
			{
				// Could not convert BlockContainer to single expression
				obj2 = ((obj is RequestFailedException) ? obj : null);
				return obj2 != null && ((RequestFailedException)obj2).Status == 409;
			}).Invoke())
			{
				newPath = $"{dir}{name} ({i}){Delimiter}";
			}
		}
		res.Message = new Message("Could not create folder");
		return res;
	}

	public async Task<FileExplorerResponseDTO> Delete(FileExplorerDeleteDTO data)
	{
		FileExplorerResponseDTO res = new FileExplorerResponseDTO((string)null);
		IEnumerable<string> enumerable;
		if (fileExplorerAccessControl != null)
		{
			enumerable = fileExplorerAccessControl.FilterWithDeleteAccess(data.Paths);
		}
		else
		{
			IEnumerable<string> enumerable2 = data.Paths.ToList();
			enumerable = enumerable2;
		}
		IEnumerable<string> paths = enumerable;
		BlobContainerClient container = azureStorageService.GetBlobContainerClient(((FileExplorerRequestDTOBase)data).AccountName, ((FileExplorerRequestDTOBase)data).ContainerName);
		await QueryDeletedItems(paths.ToArray(), container, delegate(string path, List<string> list)
		{
			if (!list.Contains(path))
			{
				list.Add(path);
			}
			return ValueTask.CompletedTask;
		});
		foreach (string item in paths)
		{
			CreateLogItem(item, (FileExplorerAction)2, container);
		}
		res.Success = true;
		return res;
	}

	public async Task<FileExplorerResponseDTO> Restore(FileExplorerRestoreDTO data)
	{
		FileExplorerResponseDTO res = new FileExplorerResponseDTO((string)null);
		IEnumerable<string> enumerable;
		if (fileExplorerAccessControl != null)
		{
			enumerable = fileExplorerAccessControl.FilterWithDeleteAccess(data.Paths);
		}
		else
		{
			IEnumerable<string> enumerable2 = data.Paths.ToList();
			enumerable = enumerable2;
		}
		IEnumerable<string> paths = enumerable;
		BlobContainerClient container = azureStorageService.GetBlobContainerClient(((FileExplorerRequestDTOBase)data).AccountName, ((FileExplorerRequestDTOBase)data).ContainerName);
		await QueryDeletedItems(paths.ToArray(), container, delegate(string path, List<string> list)
		{
			list.RemoveAll((string x) => x == path);
			return ValueTask.CompletedTask;
		});
		foreach (string item in paths)
		{
			CreateLogItem(item, (FileExplorerAction)3, container);
		}
		res.Success = true;
		return res;
	}

	private async Task QueryDeletedItems(string[] paths, BlobContainerClient container, Func<string, List<string>, ValueTask> callback)
	{
		var enumerable = from x in paths.Select(delegate(string text2)
			{
				List<string> list = text2.Split('/', StringSplitOptions.RemoveEmptyEntries).ToList();
				string name = list.Last();
				list.RemoveAt(list.Count - 1);
				return new
				{
					name = name,
					IsFile = !text2.EndsWith('/'),
					path = string.Join('/', list)
				};
			})
			group x by x.path;
		foreach (var group in enumerable)
		{
			string path = group.Key;
			List<string> deletedList;
			BlobClient blobClient;
			(deletedList, blobClient) = await GetDeletedItems(path, container);
			foreach (var item in group)
			{
				string text = BlobHelper.AppendDelimiter(BlobHelper.Combine(new string[2] { path, item.name }), true);
				if (!item.IsFile)
				{
					text = BlobHelper.AppendDelimiter(text, false);
				}
				await callback(text, deletedList);
			}
			using Stream stream = await blobClient.OpenWriteAsync(true, (BlobOpenWriteOptions)null, default(CancellationToken));
			string s = string.Join("\n", deletedList) + "\n";
			byte[] bytes = Encoding.UTF8.GetBytes(s);
			await stream.WriteAsync(bytes, 0, bytes.Length);
		}
	}

	public async Task<FileExplorerResponseDTO> Detail(FileExplorerDetailDTO data)
	{
		string path = ((FileExplorerRequestDTOBase)data).Path ?? "";
		bool num = path.EndsWith(Delimiter) || path == string.Empty;
		FileExplorerResponseDTO res = new FileExplorerResponseDTO(path)
		{
			Items = new List<FileExplorerItemDTO>()
		};
		if (num)
		{
			BlobContainerClient container = azureStorageService.GetBlobContainerClient(((FileExplorerRequestDTOBase)data).AccountName, ((FileExplorerRequestDTOBase)data).ContainerName);
			IAsyncEnumerable<Page<BlobItem>> asyncEnumerable = container.GetBlobsAsync(new GetBlobsOptions
			{
				Prefix = path
			}, default(CancellationToken)).AsPages(((FileExplorerReadDTO)data).ContinuationToken, (int?)config.PageSizeHint);
			Page<BlobItem> workingPage = null;
			await using (IAsyncEnumerator<Page<BlobItem>> asyncEnumerator = asyncEnumerable.GetAsyncEnumerator())
			{
				if (await asyncEnumerator.MoveNextAsync())
				{
					Page<BlobItem> current = asyncEnumerator.Current;
					workingPage = current;
				}
			}
			if (workingPage == null || workingPage.Values.Count == 0)
			{
				res.Success = true;
				return res;
			}
			res.ContinuationToken = workingPage.ContinuationToken;
			ITypeAuthService? obj = typeAuthService;
			bool canViewDeletedFiles = obj == null || obj.CanAccess(AzureStorageActionTree.ViewDeletedFiles);
			try
			{
				if (!canViewDeletedFiles || !((FileExplorerReadDTO)data).IncludeDeleted)
				{
					await EnsurePathNotDeletedAsync(path, container);
				}
			}
			catch (DirectoryNotFoundException)
			{
				res.Success = true;
				return res;
			}
			IEnumerable<string> deletedItems = Array.Empty<string>();
			SemaphoreSlim semaphore = new SemaphoreSlim(10);
			deletedItems = (await Task.WhenAll(workingPage.Values.Where((BlobItem blob) => blob.Name.EndsWith(Delimiter + "f7751140-2a59-4edf-955c-406eef7fa0d4")).Select(async delegate(BlobItem blob)
			{
				await semaphore.WaitAsync();
				try
				{
					return ((List<string> list, BlobClient blob))(await GetDeletedItems(blob.Name, container));
				}
				finally
				{
					semaphore.Release();
				}
			}))).SelectMany(((List<string> list, BlobClient blob) x) => x.list);
			IEnumerable<BlobItem> source;
			if (!canViewDeletedFiles || !((FileExplorerReadDTO)data).IncludeDeleted)
			{
				source = workingPage.Values.Where((BlobItem blob) => !deletedItems.Contains(Delimiter + blob.Name));
			}
			else
			{
				IEnumerable<BlobItem> values = workingPage.Values;
				source = values;
			}
			int num2 = source.Count();
			long size = source.Sum((BlobItem blob) => blob.Properties.ContentLength.GetValueOrDefault());
			res.Items.Add(new FileExplorerItemDTO
			{
				Name = BlobHelper.GetName(path),
				Type = "Directory",
				Path = path,
				Size = size,
				Additional = num2
			});
		}
		res.Success = true;
		return res;
	}

	public Task<FileExplorerResponseDTO> Copy(FileExplorerCopyDTO data)
	{
		throw new NotImplementedException();
	}

	public Task<FileExplorerResponseDTO> Move(FileExplorerMoveDTO data)
	{
		throw new NotImplementedException();
	}

	public Task<FileExplorerResponseDTO> Rename(FileExplorerRenameDTO data)
	{
		throw new NotImplementedException();
	}

	public Task<FileExplorerResponseDTO> Search(FileExplorerSearchDTO data)
	{
		throw new NotImplementedException();
	}

	private unsafe void CreateLogItem(string path, FileExplorerAction action, BlobContainerClient container)
	{
		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
		//IL_003a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0041: Unknown result type (might be due to invalid IL or missing references)
		//IL_004c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0058: Unknown result type (might be due to invalid IL or missing references)
		//IL_0064: Unknown result type (might be due to invalid IL or missing references)
		//IL_0075: Unknown result type (might be due to invalid IL or missing references)
		//IL_0086: Unknown result type (might be due to invalid IL or missing references)
		//IL_0097: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cb: Expected O, but got Unknown
		//IL_00cb: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00eb: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f3: Unknown result type (might be due to invalid IL or missing references)
		if (cosmosContainer == null)
		{
			return;
		}
		LogItem val = new LogItem
		{
			Id = Guid.NewGuid().ToString(),
			Action = ((object)(*(FileExplorerAction*)(&action))/*cast due to .constrained prefix*/).ToString(),
			Path = path,
			Timestamp = DateTime.Now,
			AccountName = container.AccountName,
			Container = container.Name,
			CompanyID = identityClaimProvider.GetCompanyID(),
			CompanyHashedID = identityClaimProvider.GetHashedCompanyID(),
			CompanyBranchID = identityClaimProvider.GetCompanyBranchID(),
			CompanyBranchHashedID = identityClaimProvider.GetHashedCompanyBranchID(),
			UserID = identityClaimProvider.GetUserID(),
			UserHashedID = identityClaimProvider.GetHashedUserID()
		};
		PartitionKey value = new PartitionKeyBuilder().Add(val.Path).Add(val.Action).Build();
		try
		{
			cosmosContainer.CreateItemAsync<LogItem>(val, (PartitionKey?)value, (ItemRequestOptions)null, default(CancellationToken));
		}
		catch (Exception)
		{
		}
	}
}
