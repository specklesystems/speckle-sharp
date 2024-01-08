#nullable disable
using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.Serialization;
using Speckle.Newtonsoft.Json;

namespace Speckle.Core.Models;

public class Blob : Base
{
  [JsonIgnore]
  public static int LocalHashPrefixLength => 20;

  private string _filePath;
  private string _hash;
  private bool _isHashExpired = true;

  public Blob() { }

  public Blob(string filePath)
  {
    this.filePath = filePath;
  }

  public string filePath
  {
    get => _filePath;
    set
    {
      originalPath ??= value;

      _filePath = value;
      _isHashExpired = true;
    }
  }

  public string originalPath { get; set; }

  /// <summary>
  /// For blobs, the id is the same as the file hash. Please note, when deserialising, the id will be set from the original hash generated on sending.
  /// </summary>
  public override string id
  {
    get => GetFileHash();
    set => base.id = value;
  }

  public string GetFileHash()
  {
    if ((_isHashExpired || _hash == null) && filePath != null)
    {
      _hash = Utilities.HashFile(filePath);
    }

    return _hash;
  }

  [OnDeserialized]
  internal void OnDeserialized(StreamingContext context)
  {
    _isHashExpired = false;
  }

  public string GetLocalDestinationPath(string blobStorageFolder)
  {
    var fileName = Path.GetFileName(filePath);
    return Path.Combine(blobStorageFolder, $"{id.Substring(0, 10)}-{fileName}");
  }

  [Obsolete("Renamed to " + nameof(GetLocalDestinationPath))]
  [SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "Obsolete")]
  public string getLocalDestinationPath(string blobStorageFolder) => GetLocalDestinationPath(blobStorageFolder);
}
