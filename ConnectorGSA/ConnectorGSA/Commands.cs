using ConnectorGSA.Models;
using Newtonsoft.Json;
using Speckle.Core.Api;
using Speckle.Core.Kits;
using Speckle.Core.Models;
using Speckle.Core.Transports;
using Speckle.GSA.API;
using Speckle.GSA.API.GwaSchema;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ConnectorGSA
{
  public static class Commands
  {
    public static object Assert { get; private set; }

    public static bool OpenFile(string filePath, bool visible)
    {
      Instance.GsaModel.Proxy = new Speckle.ConnectorGSA.Proxy.GsaProxy(); //Use a real proxy
      var opened = Instance.GsaModel.Proxy.OpenFile(filePath, visible);
      if (!opened)
      {
        
        return false;
      }
      return true;
    }

    public static bool ExtractSavedReceptionStreamInfo(string userId, string restApi, bool? receive, bool? send, out List<StreamState> streamStates)
    {
      var sid = Instance.GsaModel.Proxy.GetTopLevelSid();
      var allSaved = JsonConvert.DeserializeObject<List<StreamState>>(sid);

      //So currently it assumes that a new user for this file will have a new stream created for them, even if other users saved this file with their stream info
      streamStates = allSaved.Where(ss => ((ss.UserId == userId) && ss.ServerUrl.Equals(restApi, StringComparison.InvariantCultureIgnoreCase))).ToList();
      if (receive.HasValue)
      {
        streamStates = streamStates.Where(ss => ss.IsReceiving == receive.Value).ToList();
      }
      if (send.HasValue)
      {
        streamStates = streamStates.Where(ss => ss.IsSending == send.Value).ToList();
      }
      return (streamStates != null && streamStates.Count > 0);
    }

    public static bool UpsertSavedReceptionStreamInfo(bool? receive, bool? send, List<StreamState> streamStates)
    {
      var sid = Instance.GsaModel.Proxy.GetTopLevelSid();
      List<StreamState> allSs = null;
      try
      {
        allSs = JsonConvert.DeserializeObject<List<StreamState>>(sid);
      }
      catch (JsonException ex)
      {
        //Could not deserialise, probably because it has a v1-format of stream information.  In this case, ignore the info

        //TO DO: write technical long line here
      }

      if (allSs == null || allSs.Count() == 0)
      {
        allSs = streamStates;
      }
      else
      {
        var merged = new List<StreamState>();
        foreach (var ss in streamStates)
        {
          var matching = allSs.FirstOrDefault(s => s.Equals(ss));
          if (matching != null)
          {
            if (matching.IsReceiving != ss.IsReceiving)
            {
              matching.IsReceiving = true;  //This is merging of two booleans, where a true value is to be set if any are true
            }
            if (matching.IsSending != ss.IsSending)
            {
              matching.IsSending = true;  //This is merging of two booleans, where a true value is to be set if any are true
            }
            merged.Add(ss);
          }
        }

        allSs = allSs.Union(streamStates.Except(merged)).ToList();
      }

      var newSid = JsonConvert.SerializeObject(allSs);
      return Instance.GsaModel.Proxy.SetTopLevelSid(newSid);
    }

    public static bool CloseFile(string filePath, bool visible)
    {
      Instance.GsaModel.Proxy.Close();
      return Instance.GsaModel.Proxy.Clear();
    }

    public static bool LoadDataFromFile(IEnumerable<ResultGroup> resultGroups = null, IEnumerable<ResultType> resultTypes = null)
    {
      var loadedCache = UpdateCache();
      int cumulativeErrorRows = 0;

      if (resultGroups != null && resultGroups.Any() && resultTypes != null && resultTypes.Any())
      {
        if (!Instance.GsaModel.Proxy.PrepareResults(resultTypes, Instance.GsaModel.Result1DNumPosition + 2))
        {
          return false;
        }
        foreach (var g in resultGroups)
        {
          if (!Instance.GsaModel.Proxy.LoadResults(g, out int numErrorRows) || numErrorRows > 0)
          {
            return false;
          }
          cumulativeErrorRows += numErrorRows;
        }
      }

      return (loadedCache && (cumulativeErrorRows == 0));
    }

    public static bool ConvertToNative(ISpeckleConverter converter) //Includes writing to Cache
    {
      //With the attached objects in speckle objects, there is no type dependency needed on the receive side, so just convert each object

      if (Instance.GsaModel.Cache.GetSpeckleObjects(out var speckleObjects))
      {
        foreach (var so in speckleObjects.Cast<Base>())
        {
          try
          {
            if (converter.CanConvertToNative(so))
            {
              var nativeObjects = converter.ConvertToNative(new List<Base> { so }).Cast<GsaRecord>().ToList();
              var appId = string.IsNullOrEmpty(so.applicationId) ? so.id : so.applicationId;
              Instance.GsaModel.Cache.SetNatives(so.GetType(), appId, nativeObjects);
            }
          }
          catch (Exception ex)
          {

          }
        } 
      }

      return true;
    }

    public static List<Base> ConvertToSpeckle(ISpeckleConverter converter)
    {
      if (!Instance.GsaModel.Cache.GetNatives(out List<GsaRecord> gsaRecords))
      {
        return null;
      }

      return converter.ConvertToSpeckle(gsaRecords.Cast<object>().ToList());

      /*
      var convertedObjectsByType = convertedObjects.GroupBy(o => o.GetType()).ToDictionary(g => g.Key, g => g.ToList());

      var commit = new Base();

      foreach(var t in convertedObjectsByType.Keys)
      {
        commit[$"{t.Name}"] = convertedObjectsByType[t];
      }

      return commit;
      /*

      /*
      //Get send native type dependencies
      var typeDependencyGenerations = Instance.GsaModel.Proxy.GetTxTypeDependencyGenerations(Instance.GsaModel.StreamLayer);

      foreach (var gen in typeDependencyGenerations)
      {
        var nativeObjsByType = new Dictionary<Type, List<GsaRecord>>();
        foreach (var t in gen)
        {
          if (Instance.GsaModel.Cache.GetNative(t, out var gsaRecords))
          {
            nativeObjsByType.Add(t, gsaRecords);
          }
        }

        foreach (var t in nativeObjsByType.Keys)
        {
          var speckleObjsBucket = new List<Base>();
          foreach (var nativeObj in nativeObjsByType[t])
          {
            try
            {
              if (converter.CanConvertToSpeckle(nativeObj))
              {
                var speckleObjs = converter.ConvertToSpeckle(new List<object> { nativeObj });
                if (speckleObjs != null && speckleObjs.Count > 0)
                {
                  speckleObjsBucket.AddRange(speckleObjs);
                  Instance.GsaModel.Cache.SetSpeckleObjects(nativeObj, speckleObjs.ToDictionary(so => so.applicationId, so => (object)so));
                }
              }
            }
            catch (Exception ex)
            {

            }
          }
          if (speckleObjsBucket.Count > 0)
          {
            commit[$"{t.Name}"] = speckleObjsBucket;
          }
        }
      }
      return commit;
      */
    }

    public static async Task<bool> Send(Base commitObj, StreamState state, params ITransport[] transports)
    {
      var commitObjId = await Operations.Send(
        @object: commitObj,
        transports: transports.ToList(),
        onErrorAction: (s, e) =>
        {
          state.Errors.Add(e);
        },
        disposeTransports: true
        );

      if (transports.Any(t => t is ServerTransport))
      {
        var actualCommit = new CommitCreateInput
        {
          streamId = state.Stream.id,
          objectId = commitObjId,
          branchName = "main",
          message = "Pushed it real good",
          sourceApplication = Applications.GSA
        };

        //if (state.PreviousCommitId != null) { actualCommit.parents = new List<string>() { state.PreviousCommitId }; }

        try
        {
          var commitId = await state.Client.CommitCreate(actualCommit);
        }
        catch (Exception e)
        {
          state.Errors.Add(e);
        }
      }

      return (state.Errors.Count == 0);
    }

    public static async Task<bool> Receive(string commitId, StreamState state, ITransport transport, Func<Base, bool> IsSingleObjectFn)
    {
      var commitObject = await Operations.Receive(
          commitId,
          transport,
          onErrorAction: (s, e) =>
          {
            state.Errors.Add(e);
          },
          disposeTransports: true
          );

      if (commitObject != null)
      {
        var receivedObjects = FlattenCommitObject(commitObject, IsSingleObjectFn);

        return (Instance.GsaModel.Cache.Upsert(receivedObjects.ToDictionary(
            ro => string.IsNullOrEmpty(ro.applicationId) ? ro.id : ro.applicationId, 
            ro => (object)ro))
          && receivedObjects != null && receivedObjects.Any() && state.Errors.Count == 0);
      }
      return false;
    }

    private static bool UpdateCache(bool onlyNodesWithApplicationIds = true)
    {
      var errored = new Dictionary<int, GsaRecord>();

      try
      {
        if (Instance.GsaModel.Proxy.GetGwaData(out var records))
        {
          for (int i = 0; i < records.Count(); i++)
          {
            if (!Instance.GsaModel.Cache.Upsert(records[i]))
            {
              errored.Add(i, records[i]);
            }
          }
        }
        return true;
      }
      catch
      {
        return false;
      }
    }

    private static List<Base> FlattenCommitObject(object obj, Func<Base, bool> IsSingleObjectFn)
    {
      //This is needed because with GSA models, there could be a design and analysis layer with objects appearing in both, so only include the first
      //occurrence of each object (distinguished by the ID returned by the Base.GetId() method) in the list returned
      var uniques = new Dictionary<Type, HashSet<string>>();
      return FlattenCommitObject(obj, IsSingleObjectFn, uniques);
    }


    private static List<Base> FlattenCommitObject(object obj, Func<Base, bool> IsSingleObjectFn, Dictionary<Type, HashSet<string>> uniques)
    {
      List<Base> objects = new List<Base>();

      if (obj is Base @base)
      {
        if (IsSingleObjectFn(@base))
        {
          var t = obj.GetType();
          var id = @base.GetId();
          if (!uniques.ContainsKey(t))
          {
            uniques.Add(t, new HashSet<string>() { @base.GetId() });
          }
          if (!uniques[t].Contains(id))
          {
            objects.Add(@base);
            uniques[t].Add(id);
          }

          return objects;
        }
        else
        {
          foreach (var prop in @base.GetDynamicMembers())
          {
            objects.AddRange(FlattenCommitObject(@base[prop], IsSingleObjectFn, uniques));
          }
          foreach (var kvp in @base.GetMembers())
          {
            var prop = kvp.Key;
            objects.AddRange(FlattenCommitObject(@base[prop], IsSingleObjectFn, uniques));
          }
          return objects;
        }
      }

      if (obj is List<object> list)
      {
        foreach (var listObj in list)
        {
          objects.AddRange(FlattenCommitObject(listObj, IsSingleObjectFn, uniques));
        }
        return objects;
      }
      else if (obj is List<Base> baseObjList)
      {
        foreach (var baseObj in baseObjList)
        {
          objects.AddRange(FlattenCommitObject(baseObj, IsSingleObjectFn, uniques));
        }
        return objects;
      }
      else if (obj is IDictionary dict)
      {
        foreach (DictionaryEntry kvp in dict)
        {
          objects.AddRange(FlattenCommitObject(kvp.Value, IsSingleObjectFn, uniques));
        }
        return objects;
      }

      return objects;
    }

    internal static async Task<List<StreamState>> GetStreamList(TabCoordinator coordinator, SpeckleAccountForUI account, Progress<MessageEventArgs> loggingProgress)
    {
      return new List<StreamState>();
    }

    internal static async Task<bool> NewFile(TabCoordinator coordinator, Progress<MessageEventArgs> loggingProgress)
    {
      return true;
    }

    internal static async Task<bool> ReadSavedStreamInfo(TabCoordinator coordinator, Progress<MessageEventArgs> loggingProgress)
    {
      return true;
    }

    internal static async Task<bool> SaveFile(TabCoordinator coordinator)
    {
      return true;
    }

    internal static async Task<bool> RenameStream(TabCoordinator coordinator, string streamId, string newStreamName, Progress<MessageEventArgs> loggingProgress)
    {
      return true;
    }

    internal static async Task<bool> CloneStream(TabCoordinator coordinator, string streamId, Progress<MessageEventArgs> loggingProgress)
    {
      return true;
    }

    internal static async Task<bool> SendTriggered(object gsaSenderCoordinator)
    {
      return true;
    }

    internal static async Task<bool> SendInitial(TabCoordinator coordinator, object gsaSenderCoordinator, Progress<StreamState> streamCreationProgress, Progress<StreamState> streamDeletionProgress, Progress<MessageEventArgs> loggingProgress, Progress<string> statusProgress, Progress<double> percentageProgress)
    {
      throw new NotImplementedException();
    }

    internal static async Task<bool> Receive(TabCoordinator coordinator, object continuousReceiverCoordinator, Progress<StreamState> streamCreationProgress, Progress<MessageEventArgs> loggingProgress, Progress<string> statusProgress, Progress<double> percentageProgress)
    {
      return true;
    }
  }
}
