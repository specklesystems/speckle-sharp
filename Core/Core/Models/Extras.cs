using System;
using System.Collections.Generic;
using System.Linq;
using Speckle.Core.Models.Extensions;
using Speckle.Newtonsoft.Json;

namespace Speckle.Core.Models;

/// <summary>
/// Wrapper around other, third party, classes that are not coming from a speckle kit.
/// <para>Serialization and deserialization of the base object happens through default Newtonsoft converters. If your object does not de/serialize correctly, this class will not prevent that from happening.</para>
/// <para><b>Limitations:</b></para>
/// <para>- Base object needs to be serializable.</para>
/// <para>- Inline collection declarations with values do not behave correctly.</para>
/// <para>- Your class needs to have a void constructor.</para>
/// <para>- Probably more. File a bug!</para>
/// </summary>
public class Abstract : Base
{
  private object _base;

  /// <summary>
  /// See <see cref="Abstract"/> for limitations of this approach.
  /// </summary>
  public Abstract() { }

  /// <summary>
  /// See <see cref="Abstract"/> for limitations of this approach.
  /// </summary>
  /// <param name="_original"></param>
  public Abstract(object _original)
  {
    @base = _original;
    assemblyQualifiedName = @base.GetType().AssemblyQualifiedName;
  }

  public string assemblyQualifiedName { get; set; }

  /// <summary>
  /// The original object.
  /// </summary>
  public object @base
  {
    get => _base;
    set
    {
      _base = value;
      assemblyQualifiedName = value.GetType().AssemblyQualifiedName;
    }
  }
}

/// <summary>
/// <para>In short, this helps you chunk big things into smaller things.</para>
/// See the following <see href="https://pics.me.me/chunky-boi-57848570.png">reference.</see>
/// </summary>
/// <typeparam name="T"></typeparam>
public class DataChunk : Base
{
  public List<object> data { get; set; } = new();
}

public class ObjectReference
{
  public string speckle_type = "reference";

  public string referencedId { get; set; }
}

public class ProgressEventArgs : EventArgs
{
  public ProgressEventArgs(int current, int total, string scope)
  {
    this.current = current;
    this.total = total;
    this.scope = scope;
  }

  public int current { get; set; }
  public int total { get; set; }
  public string scope { get; set; }
}

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
    if (createdIds != null)
      createdIds
        .Where(o => !string.IsNullOrEmpty(o) && !CreatedIds.Contains(o))
        ?.ToList()
        .ForEach(o => CreatedIds.Add(o));
    if (createdId != null && !CreatedIds.Contains(createdId))
      CreatedIds.Add(createdId);
    if (status.HasValue)
      Status = status.Value;
    if (log != null)
      log.Where(o => !string.IsNullOrEmpty(o) && !Log.Contains(o))?.ToList().ForEach(o => Log.Add(o));
    if (!string.IsNullOrEmpty(logItem) && !Log.Contains(logItem))
      Log.Add(logItem);
    if (convertedItem != null && !Converted.Contains(convertedItem))
      Converted.Add(convertedItem);
    if (converted != null)
      converted.Where(o => o != null && !Converted.Contains(o))?.ToList().ForEach(o => Converted.Add(o));
    if (!string.IsNullOrEmpty(container))
      Container = container;
    if (!string.IsNullOrEmpty(descriptor))
      Descriptor = descriptor;
  }
}

public class ProgressReport
{
  public Dictionary<string, ApplicationObject> ReportObjects { get; set; } = new();

  public List<string> SelectedReportObjects { get; set; } = new();

  public void Log(ApplicationObject obj)
  {
    var _reportObject = UpdateReportObject(obj);
    if (_reportObject == null)
      ReportObjects.Add(obj.OriginalId, obj);
  }

  public ApplicationObject UpdateReportObject(ApplicationObject obj)
  {
    if (ReportObjects.TryGetValue(obj.OriginalId, out ApplicationObject reportObject))
    {
      reportObject.Update(
        createdIds: obj.CreatedIds,
        container: obj.Container,
        converted: obj.Converted,
        log: obj.Log,
        descriptor: obj.Descriptor
      );

      if (obj.Status != ApplicationObject.State.Unknown)
        reportObject.Update(status: obj.Status);
      return reportObject;
    }

    return null;
  }

  [Obsolete("Use TryGetValue or Dictionary indexing", true)]
  public bool GetReportObject(string id, out int index)
  {
    throw new NotImplementedException();
    // var _reportObject = ReportObjects.Where(o => o.OriginalId == id)?.FirstOrDefault();
    // index = _reportObject != null ? ReportObjects.IndexOf(_reportObject) : -1;
    // return index == -1 ? false : true;
  }

  public void Merge(ProgressReport report)
  {
    lock (OperationErrorsLock)
      OperationErrors.AddRange(report.OperationErrors);

    lock (ConversionLogLock)
      ConversionLog.AddRange(report.ConversionLog);

    // update report object notes
    foreach (var item in ReportObjects.Values)
    {
      var ids = new List<string> { item.OriginalId };
      if (item.Fallback.Count > 0)
        ids.AddRange(item.Fallback.Select(o => o.OriginalId));

      if (item.Status == ApplicationObject.State.Unknown)
        if (report.ReportObjects.TryGetValue(item.OriginalId, out var reportObject))
          item.Status = reportObject.Status;

      foreach (var id in ids)
        //if (report.GetReportObject(id, out int index))
        if (report.ReportObjects.TryGetValue(id, out var reportObject))
        {
          foreach (var logItem in reportObject.Log)
            if (!item.Log.Contains(logItem))
              item.Log.Add(logItem);
          foreach (var createdId in reportObject.CreatedIds)
            if (!item.CreatedIds.Contains(createdId))
              item.CreatedIds.Add(createdId);
          foreach (var convertedItem in reportObject.Converted)
            if (!item.Converted.Contains(convertedItem))
              item.Converted.Add(convertedItem);
        }
    }
  }

  #region Conversion

  /// <summary>
  /// Keeps track of the conversion process
  /// </summary>
  public List<string> ConversionLog { get; } = new();

  private readonly object ConversionLogLock = new();

  public string ConversionLogString
  {
    get
    {
      var summary = "";
      lock (ConversionLogLock)
      {
        var converted = ConversionLog.Count(x => x.ToLowerInvariant().Contains("converted"));
        var created = ConversionLog.Count(x => x.ToLowerInvariant().Contains("created"));
        var skipped = ConversionLog.Count(x => x.ToLowerInvariant().Contains("skipped"));
        var failed = ConversionLog.Count(x => x.ToLowerInvariant().Contains("failed"));
        var updated = ConversionLog.Count(x => x.ToLowerInvariant().Contains("updated"));

        summary += converted > 0 ? $"CONVERTED: {converted}\n" : "";
        summary += created > 0 ? $"CREATED: {created}\n" : "";
        summary += updated > 0 ? $"UPDATED: {updated}\n" : "";
        summary += skipped > 0 ? $"SKIPPED: {skipped}\n" : "";
        summary += failed > 0 ? $"FAILED: {failed}\n" : "";
        summary = !string.IsNullOrEmpty(summary) ? $"SUMMARY\n\n{summary}\n\n" : "";

        return summary + string.Join("\n", ConversionLog);
      }
    }
  }

  public void Log(string text)
  {
    var time = DateTime.Now.ToLocalTime().ToString("dd/MM/yy HH:mm:ss");
    lock (ConversionLogLock)
      ConversionLog.Add(time + " " + text);
  }

  /// <summary>
  /// Keeps track of errors in the conversions.
  /// </summary>
  public List<Exception> ConversionErrors { get; } = new();

  private readonly object ConversionErrorsLock = new();

  public string ConversionErrorsString
  {
    get
    {
      lock (ConversionErrorsLock)
        return string.Join("\n", ConversionErrors.Select(x => x.Message).Distinct());
    }
  }

  public int ConversionErrorsCount => ConversionErrors.Count;

  public void LogConversionError(Exception exception)
  {
    lock (ConversionErrorsLock)
      ConversionErrors.Add(exception);
    Log(exception.Message);
  }

  #endregion

  #region Operation

  /// <summary>
  /// Keeps track of HANDLED errors that occur during send/recieve commands.
  /// </summary>
  /// <remarks>
  /// Handled errors specific to the conversion, should be added to ConversionErrors
  /// Unhandleable errors (i.e. that lead to the entire send/receive failing) should be Thrown instead.
  /// </remarks>
  public List<Exception> OperationErrors { get; } = new();

  private readonly object OperationErrorsLock = new();

  public string OperationErrorsString
  {
    get
    {
      lock (OperationErrorsLock)
        return string.Join("\n", OperationErrors.Select(x => x.ToFormattedString()).Distinct());
    }
  }

  public int OperationErrorsCount => OperationErrors.Count;

  public void LogOperationError(Exception exception)
  {
    lock (OperationErrorsLock)
      OperationErrors.Add(exception);
  }

  #endregion
}
