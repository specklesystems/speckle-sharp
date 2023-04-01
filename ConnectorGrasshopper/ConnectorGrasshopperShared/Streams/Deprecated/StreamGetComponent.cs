using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using ConnectorGrasshopper.Extras;
using Grasshopper.Kernel;
using Speckle.Core.Credentials;
using Speckle.Core.Models.Extensions;
using Logging = Speckle.Core.Logging;

namespace ConnectorGrasshopper.Streams
{
  [Obsolete]
  public class StreamGetComponent : GH_SpeckleComponent
  {
    public StreamGetComponent() : base("Stream Get", "sGet", "Gets a specific stream from your account",
      ComponentCategories.PRIMARY_RIBBON,
      ComponentCategories.STREAMS)
    {
    }

    public override Guid ComponentGuid => new Guid("D66AFB58-A1BA-487C-94BF-AF0FFFBA6CE5");

    protected override Bitmap Icon => Properties.Resources.StreamGet;

    public override GH_Exposure Exposure => GH_Exposure.hidden;

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
      pManager.AddParameter(new SpeckleStreamParam("Stream ID/URL", "ID/URL", "Speckle stream ID or URL",
        GH_ParamAccess.item));
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

    public override void SolveInstanceWithLogContext(IGH_DataAccess DA)
    {
      DA.DisableGapLogic();
      if (DA.Iteration != 0)
      {
        AddRuntimeMessage(GH_RuntimeMessageLevel.Warning,
          "Cannot fetch multiple streams at the same time. This is an explicit guard against possibly unintended behaviour. If you want to get the details of another stream, please use a new component.");
        return;
      }

      string userId = null;
      GH_SpeckleStream ghIdWrapper = null;
      DA.DisableGapLogic();
      if (!DA.GetData(0, ref ghIdWrapper)) return;
      DA.GetData(1, ref userId);
      var idWrapper = ghIdWrapper.Value;
      var account = string.IsNullOrEmpty(userId)
        ? AccountManager.GetAccounts().FirstOrDefault(a => a.serverInfo.url == idWrapper.ServerUrl) // If no user is passed in, get the first account for this server
        : AccountManager.GetAccounts().FirstOrDefault(a => a.userInfo.id == userId); // If user is passed in, get matching user in the db
      
      if (account == null || account.serverInfo.url != idWrapper.ServerUrl)
      {
        AddRuntimeMessage(GH_RuntimeMessageLevel.Error,
          $"Could not find an account for server ${idWrapper.ServerUrl}. Use the Speckle Manager to add an account.");
        return;
      }

      var newWrapper = new StreamWrapper(idWrapper.OriginalInput + $"?u={userId}");
      if (error != null)
      {
        Message = null;
        AddRuntimeMessage(GH_RuntimeMessageLevel.Error, error.ToFormattedString());
        error = null;
        stream = null;
      }
      else if (stream == null)
      {
        Message = "Fetching";
        // Validation
        string errorMessage = null;

        if (!ValidateInput(account, newWrapper.StreamId, ref errorMessage))
        {
          AddRuntimeMessage(GH_RuntimeMessageLevel.Error, errorMessage);
          return;
        }
        
        if(DA.Iteration == 0)                 
          Tracker.TrackNodeRun();

        // Run
        Task.Run(async () =>
        {
          try
          {
            var acc = idWrapper.GetAccount().Result;
            idWrapper.UserId = acc.userInfo.id;
            stream = idWrapper;
          }
          catch (Exception e)
          {
            stream = null;
            error = e;
          }
          finally
          {
            Rhino.RhinoApp.InvokeOnUiThread((Action)delegate { ExpireSolution(true); });
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
