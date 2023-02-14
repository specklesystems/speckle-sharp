using System;
using System.Linq;
using System.Threading.Tasks;
using ConnectorGrasshopper.Extras;
using Grasshopper.Kernel;
using Speckle.Core.Credentials;

namespace ConnectorGrasshopper.Streams
{
  public class StreamGetComponentV2 : GH_SpeckleTaskCapableComponent<StreamWrapper>
  {
    public StreamGetComponentV2() : base("Stream Get", "sGet", "Gets a specific stream from your account",
      ComponentCategories.PRIMARY_RIBBON,
      ComponentCategories.STREAMS)
    {
    }

    public override Guid ComponentGuid => new Guid("16558783-8A26-4B87-8023-245E312E0CE9");

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

    protected override void SolveInstance(IGH_DataAccess DA)
    {
      if (DA.Iteration != 0)
      {
        AddRuntimeMessage(GH_RuntimeMessageLevel.Warning,
          "Cannot fetch multiple streams at the same time. This is an explicit guard against possibly unintended behaviour. If you want to get the details of another stream, please use a new component.");
        return;
      }

      if (InPreSolve)
      {
        string userId = null;
        GH_SpeckleStream ghIdWrapper = null;
        if (!DA.GetData(0, ref ghIdWrapper)) return;
        DA.GetData(1, ref userId);
        var idWrapper = ghIdWrapper.Value;
        
        if (DA.Iteration == 0)
          Tracker.TrackNodeRun();
        
        TaskList.Add(AssignAccountToStream(idWrapper, userId));
      }

      if (!GetSolveResults(DA, out var data))
        return;

      DA.SetData(0, new GH_SpeckleStream(data));
    }

    private async Task<StreamWrapper> AssignAccountToStream(StreamWrapper idWrapper, string userId)
    {
      var account = string.IsNullOrEmpty(userId)
        ? AccountManager.GetAccounts()
          .FirstOrDefault(a =>
            a.serverInfo.url == idWrapper.ServerUrl) // If no user is passed in, get the first account for this server
        : AccountManager.GetAccounts()
          .FirstOrDefault(a => a.userInfo.id == userId); // If user is passed in, get matching user in the db

      if (account == null || account.serverInfo.url != idWrapper.ServerUrl)
      {
        AddRuntimeMessage(GH_RuntimeMessageLevel.Error,
          $"Could not find an account for server ${idWrapper.ServerUrl}. Use the Speckle Manager to add an account.");
        return null;
      }

      var newWrapper = new StreamWrapper(idWrapper.OriginalInput);
      newWrapper.SetAccount(account);
      await newWrapper.GetAccount();
      return newWrapper;
    }
  }
}
