using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Bentley.DgnPlatformNET.DgnEC;
using Bentley.MstnPlatformNET;
using Bentley.DgnPlatformNET;
using Bentley.ECObjects.Schema;
using Bentley.ECObjects;
using Bentley.ECObjects.Instance;
using Bentley.EC.Persistence.Query;

using Speckle.Newtonsoft.Json;
using DesktopUI2.Models;

namespace Speckle.ConnectorMicroStationOpenRoads.Storage
{
  public static class StreamStateManager2
  {
    static readonly string schemaName = "StreamStateWrapper";
    static readonly string className = "StreamStates";
    static readonly string propertyName = "StreamData";

    /// <summary>
    /// Returns all the speckle stream states present in the custom schema (schema is attached to file).
    /// </summary>
    /// <param name="schema"></param>
    /// <returns></returns>
    public static List<StreamState> ReadState(ECSchema schema)
    {
      DgnFile File = Session.Instance.GetActiveDgnFile();
      DgnECManager Manager = DgnECManager.Manager;

      try
      {
        ECQuery readWidget = new ECQuery(schema.GetClass(className));
        readWidget.SelectClause.SelectAllProperties = true;

        FindInstancesScope scope = FindInstancesScope.CreateScope(File, new FindInstancesScopeOption(DgnECHostType.All));

        var states = new List<StreamState>();
        using (DgnECInstanceCollection ecInstances = Manager.FindInstances(scope, readWidget))
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
      IECSchema schema = Manager.LocateSchemaInScope(scope, schemaName, 1, 0, SchemaMatchType.Latest);

      if (schema == null)
      {
        schema = CreateSchema(File);
      }

      IECClass ecClass = schema.GetClass(className);

      ECQuery readWidget = new ECQuery(ecClass);
      readWidget.SelectClause.SelectAllProperties = true;

      using (DgnECInstanceCollection instances = Manager.FindInstances(scope, readWidget))
      {
        foreach (IDgnECInstance instance in instances)
          instance.Delete();
      }

      DgnECInstanceEnabler instanceEnabler = Manager.ObtainInstanceEnabler(File, ecClass);

      var data = JsonConvert.SerializeObject(streamStates) as string;
      StandaloneECDInstance _instance = instanceEnabler.SharedWipInstance;
      _instance.SetAsString(propertyName, data);
      instanceEnabler.CreateInstanceOnFile(File, _instance);
    }

    private static ECSchema CreateSchema(DgnFile File)
    {
      ECSchema newSchema = new ECSchema(schemaName, 1, 0, schemaName);
      ECClass streamStateClass = new ECClass(className);
      ECProperty streamDataProp = new ECProperty(propertyName, ECObjects.StringType);
      streamStateClass.Add(streamDataProp);
      newSchema.AddClass(streamStateClass);

      var status = DgnECManager.Manager.ImportSchema(newSchema, File, new ImportSchemaOptions());

      if (status != SchemaImportStatus.Success)
        return null;

      return newSchema;
    }
  }
}
