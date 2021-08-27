using GsaProxy;
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

    public static bool LoadDataFromFile()
    {
      return UpdateCache();
    }

    public static bool ConvertToNative(ISpeckleConverter converter)
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

    public static Base ConvertToSpeckle(ISpeckleConverter converter)
    {
      //Get send native type dependencies
      var typeDependencyGenerations = Instance.GsaModel.Proxy.TxTypeDependencyGenerations;

      var nativeObjsByType = new Dictionary<Type, List<GsaRecord>>();
      var commit = new Base();

      foreach (var gen in typeDependencyGenerations)
      {
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
                  Instance.GsaModel.Cache.SetSpeckleObjects(nativeObj, speckleObjs.ToDictionary(so => so.applicationId, so => (object)so)); ;
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
    }

    public static async Task Send(Base commitObj, StreamState state, List<ITransport> transports)
    {
      var errors = new List<string>();

      var commitObjId = await Operations.Send(
        @object: commitObj,
        transports: transports,
        onErrorAction: (s, e) =>
        {
          errors.Add(e.Message);
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
          errors.Add(e.Message);
        }
      }

      return;
    }

    public static async Task Receive(string commitId, StreamState state, ITransport transport)
    {
      //Receive and write Speckle objects into cache
      var kit = KitManager.GetDefaultKit();
      var converter = kit.LoadConverter(Applications.GSA);
      var errors = new List<string>();

      var commitObject = await Operations.Receive(
          commitId,
          transport,
          onErrorAction: (s, e) =>
          {
            errors.Add(e.Message);
          },
          disposeTransports: true
          );


      var receivedObjects = FlattenCommitObject(commitObject, converter);

      Instance.GsaModel.Cache.Upsert(receivedObjects.ToDictionary(ro => string.IsNullOrEmpty(ro.applicationId) ? ro.id : ro.applicationId, ro => (object)ro));
    }

    private static bool UpdateCache()
    {
      var errored = new Dictionary<int, GsaRecord>();

      try
      {
        if (Instance.GsaModel.Proxy.GetGwaData(true, out var records))
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

    private static List<Base> FlattenCommitObject(object obj, ISpeckleConverter converter)
    {
      List<Base> objects = new List<Base>();

      if (obj is Base @base)
      {
        if (converter.CanConvertToNative(@base))
        {
          objects.Add(@base);

          return objects;
        }
        else
        {
          foreach (var prop in @base.GetDynamicMembers())
          {
            objects.AddRange(FlattenCommitObject(@base[prop], converter));
          }
          return objects;
        }
      }

      if (obj is List<object> list)
      {
        foreach (var listObj in list)
        {
          objects.AddRange(FlattenCommitObject(listObj, converter));
        }
        return objects;
      }

      if (obj is IDictionary dict)
      {
        foreach (DictionaryEntry kvp in dict)
        {
          objects.AddRange(FlattenCommitObject(kvp.Value, converter));
        }
        return objects;
      }

      return objects;
    }
  }
}
