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
using Speckle.DesktopUI.Utils;

namespace Speckle.ConnectorBentley.Storage
{
  public static class StreamStateManager
  {
    static readonly string schemaName = "StreamStateWrapper";
    static readonly string className = "StreamState";

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
          foreach (IDgnECInstance instance in ecInstances)
          {
            var id = instance["Id"].StringValue;
            var streamStateData = instance["StreamData"].StringValue;
            var state = JsonConvert.DeserializeObject<StreamState>(streamStateData);
            states.Add(state);
          }
        }

        if (states != null)
        {
          states.ForEach(x => x.Initialise(true));
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
    public static void WriteStreamStateList(List<StreamState> streamStates)
    {
      DgnFile File = Session.Instance.GetActiveDgnFile();
      DgnECManager Manager = DgnECManager.Manager;

      FindInstancesScope scope = FindInstancesScope.CreateScope(File, new FindInstancesScopeOption(DgnECHostType.All));
      IECSchema schema = Manager.LocateSchemaInScope(scope, schemaName, 1, 0, SchemaMatchType.Latest);

      if (schema == null)
      {
        schema = StreamStateListSchema.GetSchema();
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

      foreach (var streamState in streamStates)
      {
        var data = JsonConvert.SerializeObject(streamState) as string;
        StandaloneECDInstance instance = instanceEnabler.SharedWipInstance;

        instance.SetAsString("Id", streamState.Stream.id);
        instance.SetAsString("StreamData", data);

        instanceEnabler.CreateInstanceOnFile(File, instance);
      }
    }


    public static class StreamStateListSchema
    {
      public static ECSchema GetSchema()
      {
        var schema = RetrieveSchemas().Where(x => x.Contains(schemaName)).FirstOrDefault();
        if (schema != null)
          return RetrieveSchema();

        return AddSchema();
      }

      public static ECSchema AddSchema()
      {
        DgnFile File = Session.Instance.GetActiveDgnFile();
        DgnECManager Manager = DgnECManager.Manager;

        ECSchema newSchema = new ECSchema(schemaName, 1, 0, schemaName);
        ECClass streamStateClass = new ECClass(className);
        ECProperty streamIdProp = new ECProperty("Id", ECObjects.StringType);
        ECProperty streamDataProp = new ECProperty("StreamData", ECObjects.StringType);
        streamStateClass.Add(streamIdProp);
        streamStateClass.Add(streamDataProp);
        newSchema.AddClass(streamStateClass);

        var status = Manager.ImportSchema(newSchema, File, new ImportSchemaOptions());

        if (status != SchemaImportStatus.Success)
          return null;

        return newSchema;
      }

      public static List<string> RetrieveSchemas()
      {
        DgnECManager Manager = DgnECManager.Manager;
        DgnModel model = Session.Instance.GetActiveDgnModel();

        return (List<string>)Manager.DiscoverSchemasForModel(model, ReferencedModelScopeOption.All, false);
      }

      public static ECSchema RetrieveSchema()
      {
        DgnFile File = Session.Instance.GetActiveDgnFile();
        DgnECManager Manager = DgnECManager.Manager;

        FindInstancesScope scope = FindInstancesScope.CreateScope(File, new FindInstancesScopeOption(DgnECHostType.All));
        IECSchema schema = Manager.LocateSchemaInScope(scope, schemaName, 1, 0, SchemaMatchType.Latest);
        return (ECSchema)schema;
      }

      public static DgnECInstanceCollection RetrieveInstances()
      {
        DgnFile File = Session.Instance.GetActiveDgnFile();
        DgnECManager Manager = DgnECManager.Manager;

        FindInstancesScope scope = FindInstancesScope.CreateScope(File, new FindInstancesScopeOption(DgnECHostType.All));
        IECSchema schema = Manager.LocateSchemaInScope(scope, schemaName, 1, 0, SchemaMatchType.Latest);
        IECClass ecClass = schema.GetClass(className);

        ECQuery readWidget = new ECQuery(ecClass);
        readWidget.SelectClause.SelectAllProperties = true;
        DgnECInstanceCollection instances = Manager.FindInstances(scope, readWidget);
        return instances;
      }

      public static IDgnECInstance RetrieveInstance(StreamState streamState)
      {
        DgnFile File = Session.Instance.GetActiveDgnFile();
        DgnECManager Manager = DgnECManager.Manager;

        FindInstancesScope scope = FindInstancesScope.CreateScope(File, new FindInstancesScopeOption(DgnECHostType.All));
        IECSchema schema = Manager.LocateSchemaInScope(scope, schemaName, 1, 0, SchemaMatchType.Latest);
        IECClass ecClass = schema.GetClass(className);

        ECQuery readWidget = new ECQuery(ecClass);
        readWidget.SelectClause.SelectAllProperties = true;
        DgnECInstanceCollection instances = Manager.FindInstances(scope, readWidget);

        var instance = instances.Where(x => x["Id"].StringValue == streamState.Stream.id).FirstOrDefault();
        return instance;
      }
    }
  }
}