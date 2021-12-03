//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;
//using Bentley.DgnPlatformNET.DgnEC;
//using Bentley.MstnPlatformNET;
//using Bentley.DgnPlatformNET;
//using Bentley.ECObjects.Schema;
//using Bentley.ECObjects;
//using Bentley.ECObjects.Instance;
//using Bentley.EC.Persistence.Query;

//using Speckle.Newtonsoft.Json;
//using Speckle.DesktopUI.Utils;

//using DesktopUI2.Models;

//namespace Speckle.ConnectorMicroStationOpenRoads.Storage
//{
//  public static class StreamStateManager2
//  {
//    static readonly string schemaName = "StreamStateWrapper";
//    static readonly string className = "StreamState";

//    /// <summary>
//    /// Returns all the speckle stream states present in the custom schema (schema is attached to file).
//    /// </summary>
//    /// <param name="schema"></param>
//    /// <returns></returns>
//    public static List<DesktopUI2.Models.StreamState> ReadState(ECSchema schema)
//    {
//      DgnFile File = Session.Instance.GetActiveDgnFile();
//      DgnECManager Manager = DgnECManager.Manager;

//      try
//      {
//        ECQuery readWidget = new ECQuery(schema.GetClass(className));
//        readWidget.SelectClause.SelectAllProperties = true;

//        FindInstancesScope scope = FindInstancesScope.CreateScope(File, new FindInstancesScopeOption(DgnECHostType.All));

//        var states = new List<DesktopUI2.Models.StreamState>();
//        using (DgnECInstanceCollection ecInstances = Manager.FindInstances(scope, readWidget))
//        {
//          foreach (IDgnECInstance instance in ecInstances)
//          {
//            var id = instance["Id"].StringValue;
//            var streamStateData = instance["StreamData"].StringValue;
//            var state = JsonConvert.DeserializeObject<DesktopUI2.Models.StreamState>(streamStateData);
//            states.Add(state);
//          }
//        }

//        return states;
//      }
//      catch (Exception e)
//      {
//        return new List<DesktopUI2.Models.StreamState>();
//      }
//    }

//    /// <summary>
//    /// Writes the stream states to the current schema.
//    /// </summary>
//    /// <param name="streamStates"></param>
//    public static void WriteStreamStateList(DgnFile File, List<DesktopUI2.Models.StreamState> streamStates)
//    {
//      DgnECManager Manager = DgnECManager.Manager;

//      FindInstancesScope scope = FindInstancesScope.CreateScope(File, new FindInstancesScopeOption(DgnECHostType.All));
//      IECSchema schema = Manager.LocateSchemaInScope(scope, schemaName, 1, 0, SchemaMatchType.Latest);

//      if (schema == null)
//      {
//        schema = StreamStateListSchema.GetSchema();
//      }

//      IECClass ecClass = schema.GetClass(className);

//      ECQuery readWidget = new ECQuery(ecClass);
//      readWidget.SelectClause.SelectAllProperties = true;

//      using (DgnECInstanceCollection instances = Manager.FindInstances(scope, readWidget))
//      {
//        foreach (IDgnECInstance instance in instances)
//          instance.Delete();
//      }

//      DgnECInstanceEnabler instanceEnabler = Manager.ObtainInstanceEnabler(File, ecClass);

//      foreach (var streamState in streamStates)
//      {
//        var data = JsonConvert.SerializeObject(streamState) as string;
//        StandaloneECDInstance instance = instanceEnabler.SharedWipInstance;

//        instance.SetAsString("Id", streamState.StreamId);
//        instance.SetAsString("StreamData", data);

//        instanceEnabler.CreateInstanceOnFile(File, instance);
//      }
//    }
//  }
//}
