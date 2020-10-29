using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using ConnectorGrasshopper.Extras;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Speckle.Core.Api;
using Speckle.Core.Credentials;
using Speckle.Core.Logging;

namespace ConnectorGrasshopper.Streams
{
  public class StreamGetComponent : GH_Component
  {
    public StreamGetComponent() : base("Stream Get", "sGet", "Gets a specific stream from your account", "Speckle 2",
      "Streams")
    {
    }

    public override Guid ComponentGuid => new Guid("D66AFB58-A1BA-487C-94BF-AF0FFFBA6CE5");

    protected override Bitmap Icon => Properties.Resources.StreamGet;

    public override GH_Exposure Exposure => GH_Exposure.primary;

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
      pManager.AddTextParameter("Stream ID", "ID", "Stream ID to fetch stream from the server", GH_ParamAccess.item);
      var acc = pManager.AddTextParameter("Account", "A", "Account to get stream with.", GH_ParamAccess.item);

      Params.Input[acc].Optional = true;
    }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
      pManager.AddParameter(new SpeckleStreamParam("Stream", "S", "Speckle Stream",
        GH_ParamAccess.item));
    }

    private StreamWrapper stream;
    private Exception error;

    protected override void SolveInstance(IGH_DataAccess DA)
    {
      DA.DisableGapLogic();
      if (DA.Iteration != 0)
      {
        AddRuntimeMessage(GH_RuntimeMessageLevel.Warning,
          "Cannot fetch multiple streams at the same time. This is an explicit guard against possibly unintended behaviour. If you want to get the details of another stream, please use a new component.");
        return;
      }
      
      string accountId = null;
      string id = null;
      DA.DisableGapLogic();
      DA.GetData(0, ref id);
      var account = !DA.GetData(1, ref accountId)
        ? AccountManager.GetDefaultAccount()
        : AccountManager.GetAccounts().FirstOrDefault(a => a.id == accountId);

      Params.Input[1].AddVolatileData(new GH_Path(0), 0, account.id);

      if (error != null)
      {
        Message = null;
        AddRuntimeMessage(GH_RuntimeMessageLevel.Error, error.Message);
        error = null;
        stream = null;
      }
      else if (stream == null)
      {
        Message = "Fetching";
        // Validation
        string errorMessage = null;
        if (!ValidateInput(account, id, ref errorMessage))
        {
          AddRuntimeMessage(GH_RuntimeMessageLevel.Error, errorMessage);
          return;
        }

        // Run
        Task.Run(async () =>
        {
          try
          {
            Tracker.TrackEvent(Tracker.STREAM_LIST);

            //Exists?
            var client = new Client(account);
            var result = await client.StreamGet(id);
            stream = new StreamWrapper(result.id, account.id, account.serverInfo.url);
          }
          catch (Exception e)
          {
            stream = null;
            error = e;
          }
          finally
          {
            Rhino.RhinoApp.InvokeOnUiThread((Action) delegate { ExpireSolution(true); });
          }
        });
      }
      else
      {
        Message = "Done";
        DA.SetData(0, new GH_SpeckleStream(stream));
        stream = null;
      }
    }

    private bool ValidateInput(Account account, string id, ref string s)
    {
      string message = null;
      if (account == null)
      {
        s = "No account was found.";
        return false;
      }

      if (id == null)
      {
        s = "ID cannot be null";
        return false;
      }

      if (!IsValidId(id, ref message))
      {
        s = message;
        return false;
      }

      s = null;
      return true;
    }

    private bool IsValidId(string id, ref string s)
    {
      // TODO: Add validation!
      return true;
    }
  }
}
