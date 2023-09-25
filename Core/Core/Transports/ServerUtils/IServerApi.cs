using System.Collections.Generic;
using System.Threading.Tasks;

namespace Speckle.Core.Transports.ServerUtils;

public delegate void CbObjectDownloaded(string id, string json);
public delegate void CbBlobdDownloaded();

internal interface IServerApi
{
  public Task<string> DownloadSingleObject(string streamId, string objectId);

  public Task DownloadObjects(string streamId, IReadOnlyList<string> objectIds, CbObjectDownloaded onObjectCallback);

  public Task<Dictionary<string, bool>> HasObjects(string streamId, IReadOnlyList<string> objectIds);

  public Task UploadObjects(string streamId, IReadOnlyList<(string id, string data)> objects);

  public Task UploadBlobs(string streamId, IReadOnlyList<(string id, string data)> objects);

  public Task DownloadBlobs(string streamId, IReadOnlyList<string> blobIds, CbBlobdDownloaded onBlobCallback);
}
