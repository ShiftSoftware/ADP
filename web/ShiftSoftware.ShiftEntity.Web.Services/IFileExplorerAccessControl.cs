using System.Collections.Generic;
using System.Threading.Tasks;
using Azure.Storage.Blobs;

namespace ShiftSoftware.ShiftEntity.Web.Services;

public interface IFileExplorerAccessControl
{
	Task<IEnumerable<string>> FilterWithReadAccessAsync(BlobContainerClient container, IEnumerable<string> files);

	IEnumerable<string> FilterWithWriteAccess(IEnumerable<string> files);

	IEnumerable<string> FilterWithDeleteAccess(IEnumerable<string> files);
}
