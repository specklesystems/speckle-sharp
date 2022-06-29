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
  /// <see cref="Base.applicationId"/> cannot correspond with the <see cref="ApplicationObject.ApplicationGeneratedId"/> (ie, on receiving operations). 
  /// </summary>
  public class ApplicationObject : Base
  {
    public enum ConversionStatus
    {
      Converting, // Speckle object is in the process of being converted to an Application Object
      Created, // Speckle object is created from an ApplicationObject or ApplicationObject is created from Speckle Object
      Skipped, // Speckle or Application is not going to be sent or received
      Updated, // Application object is replacing an existing object in the application
      Failed, // Tried to convert & send or convert & bake but something went wrong
      Removed, //Removed object from application
      Unknown
    }

    public string Container { get; set; } // the document location of the object in the application

    public bool Convertible { get; set; } // is the object conversion supported by the converter

    [JsonIgnore]
    public List<ApplicationObject> Fallback { get; set; } // the fallback base if direct conversion is not supported, output corresponding base during flatten
    
    public string Id { get; set; } // the original object id (speckle or application)

    public List<string> CreatedIds { get; set; } // the created object ids (speckle or application)

    public ConversionStatus Status { get; set; } // status of the object, for report categorization

    public List<string> Log { get; set; } // conversion notes or other important info, exposed to user

    public bool Rollback { get; set; } // object creation or bake operation to be rolled back

    [JsonIgnore]
    public List<object> Converted { get; set; } // the converted objects

    public ApplicationObject(string id) 
    {
      Id = id;
    }

    public void Update(string createdId = null, ConversionStatus? status = null, List<string> log = null, string logItem = null)
    {
      if (createdId != null && !CreatedIds.Contains(createdId)) CreatedIds.Add(createdId);
      if (status.HasValue) Status = status.Value;
      if (log != null) log.Where(o => !string.IsNullOrEmpty(o))?.ToList().ForEach(o => Log.Add(o));
      if (!string.IsNullOrEmpty(logItem)) Log.Add(logItem);
    }
  }

  public class ProgressReport
  {
    #region Conversion
    private List<ApplicationObject> _reportObjects { get; set; } = new List<ApplicationObject>();

    public void Log(ApplicationObject obj)
    {
      var _reportObject = UpdateReportObject(obj); 
      if (_reportObject == null)
        _reportObjects.Add(obj);
    }
    public ApplicationObject UpdateReportObject(ApplicationObject obj)
    {
      if (GetReportObject(obj.Id, out int index))
      {
        _reportObjects[index].Update(status: obj.Status, log: obj.Log);
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

    public int GetConversionTotal(ApplicationObject.ConversionStatus action)
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
    }
    #endregion

    public void Merge(ProgressReport report)
    {
      lock(OperationErrorsLock)
        OperationErrors.AddRange(report.OperationErrors);
      // update report object notes
      foreach (var _reportObject in report._reportObjects)
        if (GetReportObject(_reportObject.Id, out int index))
          _reportObjects[index].Log.AddRange(_reportObject.Log);
    }
  }
}
