using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using ShiftSoftware.ShiftEntity.Model;
using ShiftSoftware.ShiftEntity.Model.Dtos;
using ShiftSoftware.ShiftEntity.Model.FileExplorer.Dtos;
using ShiftSoftware.ShiftEntity.Web.Explorer;
using ShiftSoftware.TypeAuth.AspNetCore;

namespace ShiftSoftware.ShiftEntity.Web.Controllers;

[Route("api/[controller]")]
[ApiController]
public class FileExplorerController : ControllerBase
{
	private HttpClient httpClient;

	private string? AzureFunctionsEndpoint;

	private IFileProvider fileProvider;

	public FileExplorerController(HttpClient httpClient, IFileProvider fileProvider, IOptions<FileExplorerConfiguration> config)
	{
		this.httpClient = httpClient;
		FileExplorerConfiguration value = config.Value;
		AzureFunctionsEndpoint = ((value != null) ? value.FunctionsEndpoint : null);
		this.fileProvider = fileProvider;
	}

	[HttpGet]
	[Route("list")]
	[TypeAuth(/*Could not decode attribute arguments.*/)]
	public async Task<FileExplorerResponseDTO> List([FromQuery] FileExplorerReadDTO data)
	{
		return await fileProvider.GetFiles(data);
	}

	[HttpPost]
	[Route("create")]
	[TypeAuth(/*Could not decode attribute arguments.*/)]
	public async Task<FileExplorerResponseDTO> Create([FromBody] FileExplorerCreateDTO data)
	{
		return await fileProvider.Create(data);
	}

	[HttpPost]
	[Route("delete")]
	[TypeAuth(/*Could not decode attribute arguments.*/)]
	public async Task<FileExplorerResponseDTO> Delete([FromBody] FileExplorerDeleteDTO data)
	{
		return await fileProvider.Delete(data);
	}

	[HttpPost]
	[Route("restore")]
	[TypeAuth(/*Could not decode attribute arguments.*/)]
	public async Task<FileExplorerResponseDTO> Restore([FromBody] FileExplorerRestoreDTO data)
	{
		return await fileProvider.Restore(data);
	}

	[HttpGet]
	[Route("detail")]
	[TypeAuth(/*Could not decode attribute arguments.*/)]
	public async Task<FileExplorerResponseDTO> Detail([FromQuery] FileExplorerDetailDTO data)
	{
		return await fileProvider.Detail(data);
	}

	[HttpPost("ZipFiles")]
	public async Task<ActionResult> ZipFiles(ZipOptionsDTO zipOptions)
	{
		if (string.IsNullOrWhiteSpace(AzureFunctionsEndpoint))
		{
			throw new ArgumentNullException("AzureFunctions:Endpoint not found in appsettings.json");
		}
		return StatusCode((int)(await httpClient.PostAsJsonAsync<ZipOptionsDTO>(AzureFunctionsEndpoint + "/api/zip", zipOptions)).StatusCode);
	}

	[HttpPost("UnzipFiles")]
	public async Task<ActionResult> UnzipFiles(ZipOptionsDTO zipOptions)
	{
		if (string.IsNullOrWhiteSpace(AzureFunctionsEndpoint))
		{
			throw new ArgumentNullException("AzureFunctions:Endpoint not found in appsettings.json");
		}
		return StatusCode((int)(await httpClient.PostAsJsonAsync<ZipOptionsDTO>(AzureFunctionsEndpoint + "/api/unzip", zipOptions)).StatusCode);
	}
}
