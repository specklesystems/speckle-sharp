#nullable enable
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace Speckle.Core.Transports.ServerUtils;

/// <summary>
/// https://cymbeline.ch/2014/03/16/gzip-encoding-an-http-post-request-body/
/// </summary>
internal sealed class GzipContent : HttpContent
{
  private readonly HttpContent? _content;

  public GzipContent(HttpContent? content)
  {
    _content = content;

    // Keep the original content's headers ...
    if (content is not null)
    {
      foreach (KeyValuePair<string, IEnumerable<string>> header in content.Headers)
        Headers.TryAddWithoutValidation(header.Key, header.Value);
    }

    // ... and let the server know we've Gzip-compressed the body of this request.
    Headers.ContentEncoding.Add("gzip");
  }

  protected override async Task SerializeToStreamAsync(Stream stream, TransportContext context)
  {
    // Open a GZipStream that writes to the specified output stream.
    using GZipStream gzip = new(stream, CompressionMode.Compress, true);
    // Copy all the input content to the GZip stream.
    if (_content != null)
      await _content.CopyToAsync(gzip).ConfigureAwait(false);
    else
    {
      using var emptyContent = new StringContent(string.Empty);
      await emptyContent.CopyToAsync(gzip).ConfigureAwait(false);
    }
    await gzip.FlushAsync().ConfigureAwait(false);
  }

  protected override bool TryComputeLength(out long length)
  {
    length = -1;
    return false;
  }
}
