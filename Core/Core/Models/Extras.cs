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
    public static void Update(this ProgressReport.ReportObject obj, string speckleId = null, string applicationId = null, ProgressReport.ConversionStatus? status = null, string message = null, List<string> notes = null, bool? hasError = null)
    {
      if (speckleId != null) obj.SpeckleId = speckleId;
      if (applicationId != null) obj.ApplicationId = applicationId;
      if (status.HasValue) obj.Status = status.Value;
      if (message != null) obj.Message = message;
      if (notes != null) notes.Where(o => !string.IsNullOrEmpty(o))?.ToList().ForEach(o => obj.Notes.Add(o));
      if (hasError.HasValue) obj.HasError = hasError.Value;
    }
  }

  public class ProgressReport
  {
    #region Conversion
    public enum ConversionStatus { Converted, Created, Skipped, Failed, Updated, Unknown };

    /// <summary>
    /// Class for tracking objects during conversion
    /// </summary>
    public class ReportObject
    {
      public string SpeckleId { get; set; }
      public string ApplicationId { get; set; }
      public ConversionStatus Status { get; set; } = ConversionStatus.Unknown;
      public string Message { get; set; } = string.Empty;
      public List<string> Notes { get; set; } = new List<string>();
      public bool HasError { get; set; } = false;

      public ReportObject()
      {
      }
    }

    private List<ReportObject> ReportObjects { get; set; } = new List<ReportObject>();

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
      ReportObjects.Add(obj);
      var logItem = time + " " + obj.Status + (obj.SpeckleId != null ? $"(Speckle){obj.SpeckleId}" : "") + (obj.ApplicationId != null ? $"(Application){obj.ApplicationId}" : "") + $": {obj.Message}";
      lock (ConversionLogLock)
        ConversionLog.Add(logItem);
    }

    public int GetConversionTotal(ConversionStatus action)
    {
      var actionObjects = ReportObjects.Where(o => o.Status == action);
      return actionObjects == null ? 0 : actionObjects.Count();
    }

    /// <summary>
    /// Keeps track of items selected in the Report
    /// </summary>
    public List<string> Selection { get; set; } = new List<string>();

    #endregion

    #region Operation
    #endregion

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
      lock(OperationErrorsLock)
        OperationErrors.Add(exception);
      Log(exception.Message);
    }

    public void Merge(ProgressReport report)
    {
      lock(ConversionErrorsLock)
        ConversionErrors.AddRange(report.ConversionErrors);
      lock(OperationErrorsLock)
        OperationErrors.AddRange(report.OperationErrors);
      lock (ConversionLogLock)
        ConversionLog.AddRange(report.ConversionLog);
    }
  }
}
