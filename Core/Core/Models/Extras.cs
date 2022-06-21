using Speckle.Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Speckle.Core.Models
{
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
    public string assemblyQualifiedName { get; set; }

    private object _base;

    /// <summary>
    /// The original object.
    /// </summary>
    public object @base
    {
      get => _base; set
      {
        _base = value;
        assemblyQualifiedName = value.GetType().AssemblyQualifiedName;
      }
    }

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

  }

  /// <summary>
  /// <para>In short, this helps you chunk big things into smaller things.</para>
  /// See the following <see href="https://pics.me.me/chunky-boi-57848570.png">reference.</see>
  /// </summary>
  /// <typeparam name="T"></typeparam>
  public class DataChunk : Base
  {
    public List<object> data { get; set; } = new List<object>();
    public DataChunk() { }
  }

  public class ObjectReference
  {
    public string referencedId { get; set; }
    public string speckle_type = "reference";

    public ObjectReference() { }
  }

  public class ProgressEventArgs : EventArgs
  {
    public int current { get; set; }
    public int total { get; set; }
    public string scope { get; set; }
    public ProgressEventArgs(int current, int total, string scope)
    {
      this.current = current; this.total = total; this.scope = scope;
    }
  }

  /// <summary>
  /// A simple wrapper to keep track of the relationship between speckle objects and their host-application siblings in cases where the
  /// <see cref="Base.applicationId"/> cannot correspond with the <see cref="ApplicationPlaceholderObject.ApplicationGeneratedId"/> (ie, on receiving operations). 
  /// </summary>
  public class ApplicationPlaceholderObject : Base
  {
    public ApplicationPlaceholderObject() { }

    public string ApplicationGeneratedId { get; set; }

    [JsonIgnore]
    public object NativeObject;
  }

  public static class Helpers
  {
    public static void Update(this ProgressReport.ReportObject obj, string createdId = null, ProgressReport.ConversionStatus? status = null, string message = null, List<string> notes = null, string note = null)
    {
      if (createdId != null && !obj.CreatedIds.Contains(createdId)) obj.CreatedIds.Add(createdId);
      if (status.HasValue) obj.Status = status.Value;
      if (message != null) obj.Message = message;
      if (notes != null) notes.Where(o => !string.IsNullOrEmpty(o))?.ToList().ForEach(o => obj.Notes.Add(o));
      if (!string.IsNullOrEmpty(note)) obj.Notes.Add(note);
    }
  }

  public class ProgressReport
  {
    #region Conversion
    public enum ConversionStatus { Converting, Created, Skipped, Failed, Updated, Unknown };

    /// <summary>
    /// Class for tracking objects during conversion
    /// </summary>
    public class ReportObject
    {
      public string Id { get; set; }
      public List<string> CreatedIds { get; set; } = new List<string>();
      public ConversionStatus Status { get; set; } = ConversionStatus.Unknown;
      public string Message { get; set; } = string.Empty;
      public List<string> Notes { get; set; } = new List<string>();

      public ReportObject(string id)
      {
        Id = id;
      }
    }

    private List<ReportObject> _reportObjects { get; set; } = new List<ReportObject>();

    /// <summary>
    /// Keeps track of the conversion process
    /// </summary>
    public List<string> ConversionLog { get; } = new List<string>();

    private readonly object ConversionLogLock = new object();
    public void Log(string text)
    {
      var time = DateTime.Now.ToLocalTime().ToString("dd/MM/yy HH:mm:ss");
      lock (ConversionLogLock)
        ConversionLog.Add(time + " " + text);
    }
    public void Log(ReportObject obj)
    {
      var time = DateTime.Now.ToLocalTime().ToString("dd/MM/yy HH:mm:ss");
      var _reportObject = UpdateReportObject(obj); 
      if (_reportObject == null)
        _reportObjects.Add(obj);
      
      var logItem = time + " " + obj.Status + $" {obj.Id}: {obj.Message}";
      lock (ConversionLogLock)
        ConversionLog.Add(logItem);
    }
    public ReportObject UpdateReportObject(ReportObject obj)
    {
      if (GetReportObject(obj.Id, out int index))
      {
        _reportObjects[index].Update(status: obj.Status, message: obj.Message, notes: obj.Notes);
        return _reportObjects[index];
      }
      else return null;
    }
    public bool GetReportObject(string id, out int index)
    {
      var _reportObject = _reportObjects.Where(o => o.Id == id)?.FirstOrDefault();
      index = _reportObject != null ? _reportObjects.IndexOf(_reportObject) : -1;
      return index == -1 ? false : true;
    }

    public int GetConversionTotal(ConversionStatus action)
    {
      var actionObjects = _reportObjects.Where(o => o.Status == action);
      return actionObjects == null ? 0 : actionObjects.Count();
    }

    /// <summary>
    /// Keeps track of items selected in the Report
    /// </summary>
    public List<string> Selection { get; set; } = new List<string>();

    #endregion

    #region Operation
    /// <summary>
    /// Keeps track of errors in the operations of send/receive.
    /// </summary>
    public List<Exception> OperationErrors { get; } = new List<Exception>();
    private readonly object OperationErrorsLock = new object();
    public string OperationErrorsString
    {
      get
      {
        lock (OperationErrorsLock)
          return string.Join("\n", OperationErrors.Select(x => x.Message).Distinct());
      }
    }

    public int OperationErrorsCount => OperationErrors.Count;

    public void LogOperationError(Exception exception)
    {
      lock (OperationErrorsLock)
        OperationErrors.Add(exception);
      Log(exception.Message);
    }
    #endregion

    public void Merge(ProgressReport report)
    {
      lock(OperationErrorsLock)
        OperationErrors.AddRange(report.OperationErrors);
      lock (ConversionLogLock)
        ConversionLog.AddRange(report.ConversionLog);
      // update report objects with notes
      foreach (var _reportObject in report._reportObjects)
        if (GetReportObject(_reportObject.Id, out int index))
          _reportObjects[index].Notes.AddRange(_reportObject.Notes);
    }
  }
}
