using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using ConnectorGrasshopper.Extras;
using ConnectorGrasshopper.Properties;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Rhino;
using Speckle.Core.Api;
using Speckle.Core.Credentials;
using Speckle.Core.Helpers;
using Speckle.Core.Models.Extensions;

namespace ConnectorGrasshopper.Streams;

[Obsolete]
[SuppressMessage(
  "Design",
  "CA1031:Do not catch general exception types",
  Justification = "Class is used by obsolete component"
)]
public class StreamListComponent : GH_SpeckleComponent
{
  private Exception error;

  private List<StreamWrapper> streams;

  public StreamListComponent()
    : base(
      "Stream List",
      "sList",
      "Lists all the streams for this account",
      ComponentCategories.PRIMARY_RIBBON,
      ComponentCategories.STREAMS
    ) { }

  public override Guid ComponentGuid => new("BE790AF4-1834-495B-BE68-922B42FD53C7");
  protected override Bitmap Icon => Resources.StreamList;

  public override GH_Exposure Exposure => GH_Exposure.hidden;

  protected override void RegisterInputParams(GH_InputParamManager pManager)
  {
    var acc = pManager.AddTextParameter("Account", "A", "Account to get streams from", GH_ParamAccess.item);
    pManager.AddIntegerParameter("Limit", "L", "Max number of streams to fetch", GH_ParamAccess.item, 10);

    Params.Input[acc].Optional = true;
  }

  protected override void RegisterOutputParams(GH_OutputParamManager pManager)
  {
    pManager.AddParameter(
      new SpeckleStreamParam("Streams", "S", "List of streams for the provided account.", GH_ParamAccess.list)
    );
  }

  public override void SolveInstanceWithLogContext(IGH_DataAccess DA)
  {
    if (error != null)
    {
      Message = null;
      AddRuntimeMessage(GH_RuntimeMessageLevel.Error, error.ToFormattedString());
      error = null;
      streams = null;
    }
    else if (streams == null)
    {
      Message = "Fetching";
      string userId = null;
      var limit = 10;

      DA.GetData(0, ref userId);
      DA.GetData(1, ref limit); // Has default value so will never be empty.

      if (limit > 50)
      {
        limit = 50;
        AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Max number of streams retrieved is 50.");
      }

      var account = string.IsNullOrEmpty(userId)
        ? AccountManager.GetDefaultAccount()
        : AccountManager.GetAccounts().FirstOrDefault(a => a.userInfo.id == userId);

      if (userId == null)
      {
        AddRuntimeMessage(GH_RuntimeMessageLevel.Remark, "No account was provided, using default.");
      }

      if (account == null)
      {
        AddRuntimeMessage(
          GH_RuntimeMessageLevel.Error,
          "Could not find default account in this machine. Use the Speckle Manager to add an account."
        );
        return;
      }

      Params.Input[0].AddVolatileData(new GH_Path(0), 0, account.userInfo.id);

      Tracker.TrackNodeRun();

      Task.Run(async () =>
      {
        if (!await Http.UserHasInternet().ConfigureAwait(false))
        {
          AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "You are not connected to the internet");
          Message = "Error";
          return;
        }
        try
        {
          var client = new Client(account);
          // Save the result
          var result = await client.StreamsGet(limit).ConfigureAwait(false);
          streams = result
            .Select(stream => new StreamWrapper(stream.id, account.userInfo.id, account.serverInfo.url))
            .ToList();
        }
        catch (Exception ex)
        {
          error = ex;
        }
        finally
        {
          RhinoApp.InvokeOnUiThread(
            (Action)
              delegate
              {
                ExpireSolution(true);
              }
          );
        }
      });
    }
    else
    {
      Message = "Done";
      int limit = 10;
      DA.GetData(1, ref limit); // Has default value so will never be empty.

      if (limit > 50)
      {
        AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Max number of streams retrieved is 50.");
      }

      if (streams != null)
      {
        DA.SetDataList(0, streams.Select(item => new GH_SpeckleStream(item)));
      }

      streams = null;
    }
  }
}
