using System;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using ConnectorGrasshopper.Extras;
using ConnectorGrasshopper.Objects;
using ConnectorGrasshopper.Properties;
using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using Speckle.Core.Logging;
using Speckle.Core.Models.Extensions;

namespace ConnectorGrasshopper.Conversion;

public class ToNativeTaskCapableComponent : SelectKitTaskCapableComponentBase<IGH_Goo>
{
  static ToNativeTaskCapableComponent()
  {
    SpeckleGHSettings.SettingsChanged += (_, args) =>
    {
      if (args.Key != SpeckleGHSettings.SHOW_DEV_COMPONENTS)
      {
        return;
      }

      var proxy = Instances.ComponentServer.ObjectProxies.FirstOrDefault(p => p.Guid == internalGuid);
      if (proxy == null)
      {
        return;
      }

      proxy.Exposure = SpeckleGHSettings.ShowDevComponents ? GH_Exposure.primary : GH_Exposure.hidden;
    };
  }

  public ToNativeTaskCapableComponent()
    : base(
      "To Native",
      "To Native",
      "Convert data from Speckle's Base object to its Rhino equivalent.",
      ComponentCategories.SECONDARY_RIBBON,
      ComponentCategories.CONVERSION
    ) { }

  public override GH_Exposure Exposure =>
    SpeckleGHSettings.ShowDevComponents ? GH_Exposure.primary : GH_Exposure.hidden;

  protected override Bitmap Icon => Resources.ToNative;

  internal static Guid internalGuid => new("7F4BDA01-F9C8-42ED-ABC1-DA0443283219");

  public override Guid ComponentGuid => internalGuid;

  public override bool CanDisableConversion => false;

  protected override void RegisterInputParams(GH_InputParamManager pManager)
  {
    pManager.AddGenericParameter("Base", "B", "Speckle Base object to convert to Grasshopper.", GH_ParamAccess.item);
  }

  protected override void RegisterOutputParams(GH_OutputParamManager pManager)
  {
    pManager.AddGenericParameter("Data", "D", "Converted data in GH native format.", GH_ParamAccess.item);
  }

  public override void SolveInstanceWithLogContext(IGH_DataAccess DA)
  {
    if (InPreSolve)
    {
      object item = null;
      DA.GetData(0, ref item);
      if (DA.Iteration == 0)
      {
        Tracker.TrackNodeRun();
      }

      var task = Task.Run(() => DoWork(item, DA), CancelToken);
      TaskList.Add(task);
      return;
    }

    var solveResults = GetSolveResults(DA, out var data);
    if (solveResults == false)
    {
      AddRuntimeMessage(
        GH_RuntimeMessageLevel.Error,
        $@"The conversion operation failed for {DA.ParameterTargetPath(0)}[{DA.ParameterTargetIndex(0)}]"
      );
      //DA.AbortComponentSolution(); // You must abort the `SolveInstance` iteration
      return;
    }

    DA.SetData(0, data);
  }

  private IGH_Goo DoWork(object item, IGH_DataAccess DA)
  {
    try
    {
      return Utilities.TryConvertItemToNative(item, Converter, true);
    }
    catch (Exception ex) when (!ex.IsFatal())
    {
      // If we reach this, something happened that we weren't expecting...
      if (ex is AggregateException aggregateException)
      {
        ex = aggregateException.Flatten();
      }

      SpeckleLog.Logger.Error(ex, "Failed during execution of {componentName}", this.GetType());
      AddRuntimeMessage(GH_RuntimeMessageLevel.Error, ex.ToFormattedString());
      return new GH_SpeckleBase();
    }
  }
}
