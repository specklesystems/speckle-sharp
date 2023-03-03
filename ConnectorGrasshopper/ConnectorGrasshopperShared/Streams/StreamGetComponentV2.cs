﻿using System;
using System.Drawing;
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
    protected override Bitmap Icon => Properties.Resources.StreamGet;
    public override GH_Exposure Exposure => GH_Exposure.primary;
    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
      pManager.AddParameter(new SpeckleStreamParam("Stream ID/URL", "ID/URL", "Speckle stream ID or URL",
        GH_ParamAccess.item));
      var acc = pManager.AddParameter(new SpeckleAccountParam());
    }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
      pManager.AddParameter(new SpeckleStreamParam("Stream", "S", "Speckle Stream",
        GH_ParamAccess.item));
    }

    protected override void SolveInstance(IGH_DataAccess DA)
    {
      DA.DisableGapLogic();
      if (DA.Iteration != 0)
      {
        AddRuntimeMessage(GH_RuntimeMessageLevel.Warning,
          "Cannot fetch multiple streams at the same time. This is an explicit guard against possibly unintended behaviour. If you want to get the details of another stream, please use a new component.");
        return;
      }

      if (InPreSolve)
      {
        Account account = null;
        GH_SpeckleStream ghIdWrapper = null;
        if (!DA.GetData(0, ref ghIdWrapper)) return;
        if (!DA.GetData(1, ref account)) return;
        var idWrapper = ghIdWrapper.Value;
        
        if (DA.Iteration == 0)
          Tracker.TrackNodeRun();
        
        TaskList.Add(AssignAccountToStream(idWrapper, account));
      }

      if (!GetSolveResults(DA, out var data))
        return;
      if(data != null)
        DA.SetData(0, new GH_SpeckleStream(data));
    }

    private async Task<StreamWrapper> AssignAccountToStream(StreamWrapper idWrapper, Account account)
    {
      var newWrapper = new StreamWrapper(idWrapper.OriginalInput);
      try
      {
        await newWrapper.ValidateWithAccount(account).ConfigureAwait(false); // Validates the stream
      }
      catch (Exception e)
      {
        AddRuntimeMessage(GH_RuntimeMessageLevel.Error, e.Message);
        return null;
      }
      newWrapper.SetAccount(account);
      return newWrapper;
    }
  }
}
