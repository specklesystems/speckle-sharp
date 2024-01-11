#nullable disable
using System.Collections.Generic;
using System.Linq;
using Speckle.Newtonsoft.Json;

namespace Speckle.Core.Models;

/// <summary>
/// A simple wrapper to keep track of the relationship between speckle objects and their host-application siblings in cases where the
/// <see cref="Base.applicationId"/> cannot correspond with the <see cref="ApplicationObject.CreatedIds"/> (ie, on receiving operations).
/// </summary>
public class ApplicationObject
{
  public enum State
  {
    Unknown = default,
    Created, // Speckle object created on send, or native objects created on receive
    Skipped, // Speckle or Application object is not going to be sent or received
    Updated, // Application object is replacing an existing object in the application
    Failed, // Tried to convert & send or convert & bake but something went wrong
    Removed, //Removed object from application
  }

  public ApplicationObject(string id, string type)
  {
    OriginalId = id;
    Descriptor = type;
    Status = State.Unknown;
  }

  /// <summary>
  /// ID of the object from host application that generated it.
  /// </summary>
  public string applicationId { get; set; }

  /// <summary>
  /// The container for the object in the native application
  /// </summary>
  public string Container { get; set; }

  /// <summary>
  /// Indicates if conversion is supported by the converter
  /// </summary>
  public bool Convertible { get; set; }

  /// <summary>
  /// The fallback values if direct conversion is not available, typically displayValue
  /// </summary>
  [JsonIgnore]
  public List<ApplicationObject> Fallback { get; set; } = new();

  /// <summary>
  /// The Speckle id (on receive) or native id (on send)
  /// </summary>
  /// <remarks>
  /// Used to retrieve this object in <code>ProgressReport.GetReportObject()</code>, typically to pass between connectors and converters
  /// </remarks>
  public string OriginalId { get; set; }

  /// <summary>
  /// A descriptive string to describe the object. Use the object type as default.
  /// </summary>
  public string Descriptor { get; set; }

  /// <summary>
  /// The created object ids associated with this object
  /// </summary>
  /// <remarks>
  /// On send, this is currently left empty as generating Speckle ids would be performance expensive
  /// </remarks>
  public List<string> CreatedIds { get; set; } = new();

  /// <summary>
  /// Conversion status of object
  /// </summary>
  public State Status { get; set; }

  /// <summary>
  /// Conversion notes or other important information to expose to the user
  /// </summary>
  public List<string> Log { get; set; } = new();

  /// <summary>
  /// Converted objects corresponding to this object
  /// </summary>
  /// <remarks>
  /// Used during receive for convenience, corresponds to CreatedIds
  /// </remarks>
  [JsonIgnore]
  public List<object> Converted { get; set; } = new();

  public void Update(
    string createdId = null,
    List<string> createdIds = null,
    State? status = null,
    string container = null,
    List<string> log = null,
    string logItem = null,
    List<object> converted = null,
    object convertedItem = null,
    string descriptor = null
  )
  {
    createdIds?.Where(o => !string.IsNullOrEmpty(o) && !CreatedIds.Contains(o))?.ToList().ForEach(CreatedIds.Add);

    if (createdId != null && !CreatedIds.Contains(createdId))
    {
      CreatedIds.Add(createdId);
    }

    if (status.HasValue)
    {
      Status = status.Value;
    }

    log?.Where(o => !string.IsNullOrEmpty(o) && !Log.Contains(o))?.ToList().ForEach(Log.Add);

    if (!string.IsNullOrEmpty(logItem) && !Log.Contains(logItem))
    {
      Log.Add(logItem);
    }

    if (convertedItem != null && !Converted.Contains(convertedItem))
    {
      Converted.Add(convertedItem);
    }

    converted?.Where(o => o != null && !Converted.Contains(o))?.ToList().ForEach(Converted.Add);

    if (!string.IsNullOrEmpty(container))
    {
      Container = container;
    }

    if (!string.IsNullOrEmpty(descriptor))
    {
      Descriptor = descriptor;
    }
  }
}
