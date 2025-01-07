using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Bentley.DgnPlatformNET;
using Bentley.DgnPlatformNET.DgnEC;
using Bentley.EC.Persistence.Query;
using Bentley.ECObjects;
using Bentley.ECObjects.Instance;
using Bentley.ECObjects.Schema;
using Bentley.MstnPlatformNET;
using DesktopUI2.Models;
using Speckle.Newtonsoft.Json;

namespace Speckle.ConnectorBentley.Storage;

public static class StreamStateManager
{
  static readonly string schemaName = "StreamStateWrapper";
  static readonly string className = "StreamState";
  static readonly string propertyName = "StreamData";

  /// <summary>
  /// Returns all the speckle stream states present in the file.
  /// </summary>
  /// <param name="schema"></param>
  /// <returns></returns>
  public static List<StreamState> ReadState(DgnFile file)
  {
    var states = new List<StreamState>();
    try
    {
      FindInstancesScope scope = FindInstancesScope.CreateScope(file, new FindInstancesScopeOption(DgnECHostType.All));
      var schema = (ECSchema)DgnECManager.Manager.LocateSchemaInScope(scope, schemaName, 1, 0, SchemaMatchType.Latest);

      if (schema == null)
      {
        return states;
      }

      ECQuery readWidget = new(schema.GetClass(className));
      readWidget.SelectClause.SelectAllProperties = true;

      using (DgnECInstanceCollection ecInstances = DgnECManager.Manager.FindInstances(scope, readWidget))
      {
        var streamStatesInstance = ecInstances.First();
        if (streamStatesInstance != null)
        {
          var str = streamStatesInstance[propertyName].StringValue;
          states = JsonConvert.DeserializeObject<List<StreamState>>(str);
        }
      }

      return states;
    }
    catch (Exception e)
    {
      return new List<StreamState>();
    }
  }

  /// <summary>
  /// Writes the stream states to the current schema.
  /// </summary>
  /// <param name="streamStates"></param>
  public static void WriteStreamStateList(DgnFile File, List<StreamState> streamStates)
  {
    DgnECManager Manager = DgnECManager.Manager;
    FindInstancesScope scope = FindInstancesScope.CreateScope(File, new FindInstancesScopeOption(DgnECHostType.All));

    IECSchema schema = RetrieveSchema(File, scope);
    IECClass ecClass = schema.GetClass(className);

    ECQuery readWidget = new(ecClass);
    readWidget.SelectClause.SelectAllProperties = true;

    using (DgnECInstanceCollection instances = Manager.FindInstances(scope, readWidget))
    {
      foreach (IDgnECInstance instance in instances)
      {
        instance.Delete();
      }
    }

    DgnECInstanceEnabler instanceEnabler = Manager.ObtainInstanceEnabler(File, ecClass);

    var data = JsonConvert.SerializeObject(streamStates) as string;
    StandaloneECDInstance _instance = instanceEnabler.SharedWipInstance;
    _instance.SetAsString(propertyName, data);
    instanceEnabler.CreateInstanceOnFile(File, _instance);
  }

  private static ECSchema CreateSchema(DgnFile File)
  {
    ECSchema newSchema = new(schemaName, 1, 0, schemaName);
    ECClass streamStateClass = new(className);
    ECProperty streamDataProp = new(propertyName, ECObjects.StringType);
    streamStateClass.Add(streamDataProp);
    newSchema.AddClass(streamStateClass);

    var status = DgnECManager.Manager.ImportSchema(newSchema, File, new ImportSchemaOptions());

    if (status != SchemaImportStatus.Success)
    {
      return null;
    }

    return newSchema;
  }

  private static ECSchema RetrieveSchema(DgnFile File, FindInstancesScope scope)
  {
    DgnECManager Manager = DgnECManager.Manager;
    DgnModel model = Session.Instance.GetActiveDgnModel();
    var schemas = (List<string>)Manager.DiscoverSchemasForModel(model, ReferencedModelScopeOption.All, false);
    var schemaString = schemas.Where(x => x.Contains(schemaName)).FirstOrDefault();

    if (schemaString != null)
    {
      try
      {
        IECSchema schema = Manager.LocateSchemaInScope(scope, schemaName, 1, 0, SchemaMatchType.Latest);
        return (ECSchema)schema;
      }
      catch (Exception e)
      {
        return null;
      }
    }
    else
    {
      return CreateSchema(File);
    }
  }
}
