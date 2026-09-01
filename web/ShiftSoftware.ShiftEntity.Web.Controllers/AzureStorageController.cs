using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Azure.Storage.Sas;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Options;
using ShiftSoftware.ShiftEntity.Core;
using ShiftSoftware.ShiftEntity.Core.Extensions;
using ShiftSoftware.ShiftEntity.Core.Services;
using ShiftSoftware.ShiftEntity.Model;
using ShiftSoftware.ShiftEntity.Model.Dtos;
using ShiftSoftware.ShiftEntity.Model.Enums;
using ShiftSoftware.ShiftEntity.Model.FileExplorer;
using ShiftSoftware.ShiftEntity.Web.Services;
using ShiftSoftware.TypeAuth.AspNetCore;

namespace ShiftSoftware.ShiftEntity.Web.Controllers;

[Route("api/[controller]")]
[ApiController]
public class AzureStorageController : ControllerBase
{
	private AzureStorageService azureStorageService;

	private readonly IFileExplorerAccessControl? fileExplorerAccessControl;

	private Container? cosmosContainer;

	private IdentityClaimProvider identityClaimProvider;

	public AzureStorageController(AzureStorageService azureStorageService, IOptions<FileExplorerConfiguration> config, IdentityClaimProvider identityClaimProvider, IFileExplorerAccessControl? fileExplorerAccessControl = null, CosmosClient? cosmosClient = null)
	{
		this.azureStorageService = azureStorageService;
		this.fileExplorerAccessControl = fileExplorerAccessControl;
		this.identityClaimProvider = identityClaimProvider;
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

	[HttpPost("generate-file-upload-sas")]
	[TypeAuth(/*Could not decode attribute arguments.*/)]
	public async Task<ActionResult<ShiftEntityResponse<List<ShiftFileDTO>>>> GenerateFileUploadSAS([FromBody] List<ShiftFileDTO> files)
	{
		if (files.Any((ShiftFileDTO x) => string.IsNullOrWhiteSpace(x.Blob)))
		{
			AzureStorageController azureStorageController = this;
			ShiftEntityResponse<List<ShiftFileDTO>> obj = new ShiftEntityResponse<List<ShiftFileDTO>>();
			((ShiftEntityResponse)obj).Message = new Message("Bad Request", "Blob is required");
			return azureStorageController.BadRequest(obj);
		}
		ShiftEntityResponse<List<ShiftFileDTO>> val = new ShiftEntityResponse<List<ShiftFileDTO>>();
		foreach (ShiftFileDTO file in files)
		{
			string text = file.AccountName ?? azureStorageService.GetDefaultAccountName();
			string text2 = file.ContainerName ?? azureStorageService.GetDefaultContainerName(text);
			string extension = Path.GetExtension(file.Blob);
			string directoryName = Path.GetDirectoryName(file.Blob);
			file.Blob = StringExtension.AddUrlPath(directoryName, new string[1] { $"{Path.GetFileNameWithoutExtension(file.Blob)} ({Guid.NewGuid().ToString()}){extension}" });
			file.Url = azureStorageService.GetSignedURL(file.Blob, (BlobSasPermissions)9, text2, text, (int?)60);
			CreateLogItem(file.Blob, (FileExplorerAction)1, text, text2);
		}
		if (fileExplorerAccessControl != null)
		{
			IEnumerable<string> files2 = files.Select((ShiftFileDTO x) => x.Blob);
			IEnumerable<string> accessList = fileExplorerAccessControl.FilterWithWriteAccess(files2);
			files = files.Where((ShiftFileDTO x) => accessList.Contains(x.Blob)).ToList();
		}
		val.Entity = files;
		return new ContentResult
		{
			Content = JsonSerializer.Serialize<ShiftEntityResponse<List<ShiftFileDTO>>>(val, new JsonSerializerOptions()),
			ContentType = "application/json"
		};
	}

	private unsafe void CreateLogItem(string path, FileExplorerAction action, string accountName, string containerName)
	{
		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
		//IL_003a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0041: Unknown result type (might be due to invalid IL or missing references)
		//IL_004c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0053: Unknown result type (might be due to invalid IL or missing references)
		//IL_005b: Unknown result type (might be due to invalid IL or missing references)
		//IL_006c: Unknown result type (might be due to invalid IL or missing references)
		//IL_007d: Unknown result type (might be due to invalid IL or missing references)
		//IL_008e: Unknown result type (might be due to invalid IL or missing references)
		//IL_009f: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c2: Expected O, but got Unknown
		//IL_00c2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00dd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ea: Unknown result type (might be due to invalid IL or missing references)
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
			AccountName = accountName,
			Container = containerName,
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
