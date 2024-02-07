using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using ConnectorGrasshopper.Extras;
using ConnectorGrasshopper.Properties;
using Grasshopper.Kernel;
using Speckle.Core.Api;
using Speckle.Core.Credentials;

namespace ConnectorGrasshopper.Streams;

public class StreamListComponentV2 : GH_SpeckleTaskCapableComponent<List<StreamWrapper>>
{
  public StreamListComponentV2()
    : base(
      "Stream List",
      "sList",
      "Lists all the streams for this account",
      ComponentCategories.PRIMARY_RIBBON,
      ComponentCategories.STREAMS
    ) { }

  public override Guid ComponentGuid => new("500F0509-C91F-47B5-AE09-8635275979EC");
  protected override Bitmap Icon => Resources.StreamList;
  public override GH_Exposure Exposure => GH_Exposure.primary;

  protected override void RegisterInputParams(GH_InputParamManager pManager)
  {
    var acc = pManager.AddParameter(new SpeckleAccountParam { Optional = true });
    pManager.AddIntegerParameter("Limit", "L", "Max number of streams to fetch", GH_ParamAccess.item, 10);
  }

  protected override void RegisterOutputParams(GH_OutputParamManager pManager)
  {
    pManager.AddParameter(
      new SpeckleStreamParam("Streams", "S", "List of streams for the provided account.", GH_ParamAccess.list)
    );
  }

  public override void SolveInstanceWithLogContext(IGH_DataAccess DA)
  {
    if (InPreSolve)
    {
      if (DA.Iteration == 0)
      {
        Tracker.TrackNodeRun();
      }

      Account account = null;
      var limit = 10;

      DA.GetData(0, ref account);
      DA.GetData(1, ref limit); // Has default value so will never be empty.

      if (account == null)
      {
        account = AccountManager.GetDefaultAccount();
        if (account == null)
        {
          AddRuntimeMessage(
            GH_RuntimeMessageLevel.Warning,
            "Could not find default account in this machine. Use the Speckle Manager to add an account."
          );
          return;
        }

        AddRuntimeMessage(GH_RuntimeMessageLevel.Remark, $"Using default account: {account}");
      }

      if (limit > 50)
      {
        limit = 50;
        AddRuntimeMessage(
          GH_RuntimeMessageLevel.Warning,
          "Max number of streams retrieved is 50. Limit has been capped."
        );
      }

      TaskList.Add(Task.Run(() => ListStreams(account, limit), CancelToken));
      return;
    }

    if (!GetSolveResults(DA, out var data))
    {
      return;
    }

    DA.SetDataList(0, data.Select(item => new GH_SpeckleStream(item)));
  }

  private Task<List<StreamWrapper>> ListStreams(Account account, int limit)
  {
    using var client = new Client(account);
    var res = client
      .StreamsGet(limit, CancelToken)
      .Result.Select(stream =>
      {
        var s = new StreamWrapper(stream.id, account.userInfo.id, account.serverInfo.url);
        s.SetAccount(account);
        return s;
      })
      .ToList();
    return Task.FromResult(res);
  }
}
