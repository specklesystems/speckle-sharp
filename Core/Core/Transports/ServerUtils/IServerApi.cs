using System.Collections.Generic;
using System.Threading.Tasks;

namespace Speckle.Core.Transports.ServerUtils;

public delegate void CbObjectDownloaded(string id, string json);
public delegate void CbBlobdDownloaded();

internal interface IServerApi
{
  public Task<string> DownloadSingleObject(string streamId, string objectId);

  public Task DownloadObjects(string streamId, List<string> objectIds, CbObjectDownloaded onObjectCallback);

  public Task<Dictionary<string, bool>> HasObjects(string streamId, List<string> objectIds);

  public Task UploadObjects(string streamId, List<(string, string)> objects);

  public Task UploadBlobs(string streamId, List<(string, string)> objects);

  public Task DownloadBlobs(string streamId, List<string> blobIds, CbBlobdDownloaded onBlobCallback);
}
