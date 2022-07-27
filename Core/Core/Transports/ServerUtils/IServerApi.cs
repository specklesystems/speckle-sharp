using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Speckle.Core.Transports.ServerUtils
{
  public delegate void CbObjectDownloaded(string id, string json);

  internal interface IServerApi
  {
    public Task<string> DownloadSingleObject(string streamId, string objectId);
    
    public Task DownloadObjects(string streamId, List<string> objectIds, CbObjectDownloaded onObjectCallback);

    public Task<Dictionary<string, bool>> HasObjects(string streamId, List<string> objectIds);

    public Task UploadObjects(string streamId, List<(string, string)> objects);

    public Task<List<string>> UploadBlobs(string streamId, List<(string, string)> objects);
  }
}
