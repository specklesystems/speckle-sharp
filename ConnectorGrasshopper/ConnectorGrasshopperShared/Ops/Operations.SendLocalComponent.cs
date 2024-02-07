using System;
using System.Collections.Generic;
using System.Drawing;
using ConnectorGrasshopper.Objects;
using ConnectorGrasshopper.Properties;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using GrasshopperAsyncComponent;
using Speckle.Core.Api;
using Speckle.Core.Kits;
using Speckle.Core.Logging;
using Speckle.Core.Models;
using Speckle.Core.Models.Extensions;
using Utilities = ConnectorGrasshopper.Extras.Utilities;

namespace ConnectorGrasshopper.Ops;

public class SendLocalComponent : SelectKitAsyncComponentBase
{
  public ISpeckleConverter Converter;

  public ISpeckleKit Kit;

  public SendLocalComponent()
    : base(
      "Local sender",
      "LS",
      "Sends data locally, without the need of a Speckle Server.",
      ComponentCategories.SECONDARY_RIBBON,
      ComponentCategories.SEND_RECEIVE
    )
  {
    BaseWorker = new SendLocalWorker(this);
  }

  public override GH_Exposure Exposure => GH_Exposure.tertiary | GH_Exposure.obscure;

  protected override Bitmap Icon => Resources.LocalSender;

  public override Guid ComponentGuid => new("80AC1649-FF36-4B8B-A5B4-320E9D88F8BF");

  protected override void RegisterInputParams(GH_InputParamManager pManager)
  {
    pManager.AddGenericParameter("Data", "D", "Data to send.", GH_ParamAccess.tree);
  }

  protected override void RegisterOutputParams(GH_OutputParamManager pManager)
  {
    pManager.AddGenericParameter("localDataId", "id", "ID of the local data sent.", GH_ParamAccess.item);
  }

  protected override void SolveInstance(IGH_DataAccess DA)
  {
    base.SolveInstance(DA);
    if (DA.Iteration == 0)
    {
      Tracker.TrackNodeRun();
    }
  }
}

public class SendLocalWorker : WorkerInstance
{
  private GH_Structure<IGH_Goo> data;

  private string sentObjectId;

  public SendLocalWorker(GH_Component _parent)
    : base(_parent) { }

  private List<(GH_RuntimeMessageLevel, string)> RuntimeMessages { get; set; } = new();

  public override WorkerInstance Duplicate()
  {
    return new SendLocalWorker(Parent);
  }

  public override void DoWork(Action<string, double> ReportProgress, Action Done)
  {
    try
    {
      Parent.Message = "Sending...";
      var converter = (Parent as SendLocalComponent)?.Converter;
      converter?.SetContextDocument(Loader.GetCurrentDocument());
      var converted = Utilities.DataTreeToNestedLists(data, converter);
      var ObjectToSend = new Base();
      ObjectToSend["@data"] = converted;
      sentObjectId = Operations.Send(ObjectToSend).Result;
    }
    catch (Exception ex) when (!ex.IsFatal())
    {
      SpeckleLog.Logger.Error(ex, "Local send failed");
      RuntimeMessages.Add((GH_RuntimeMessageLevel.Warning, ex.ToFormattedString()));
    }

    Done();
  }

  public override void SetData(IGH_DataAccess DA)
  {
    DA.SetData(0, sentObjectId);
    foreach (var (level, message) in RuntimeMessages)
    {
      Parent.AddRuntimeMessage(level, message);
    }

    data = null;
  }

  public override void GetData(IGH_DataAccess DA, GH_ComponentParamServer Params)
  {
    DA.GetDataTree(0, out data);
    sentObjectId = null;
  }
}
