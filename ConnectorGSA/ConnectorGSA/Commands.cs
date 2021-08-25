using GsaProxy;
using Speckle.Core.Api;
using Speckle.Core.Kits;
using Speckle.Core.Models;
using Speckle.Core.Transports;
using Speckle.GSA.API;
using Speckle.GSA.API.GwaSchema;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ConnectorGSA
{
  public static class Commands
  {
    public static object Assert { get; private set; }

    public static Base Convert()
    {
      var kit = KitManager.GetDefaultKit();
      var converter = kit.LoadConverter(Applications.GSA);

      UpdateCache();

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
                  Instance.GsaModel.Cache.SetSpeckleObjects(nativeObj, speckleObjs.Cast<object>());
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

    public static async Task<StreamState> ReceiveStream(StreamState state)
    {
      var kit = KitManager.GetDefaultKit();
      var converter = kit.LoadConverter(Applications.GSA);
      var transport = new ServerTransport(state.Client.Account, state.Stream.id);

      var stream = await state.Client.StreamGet(state.Stream.id);

      return state;
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
  }
}
