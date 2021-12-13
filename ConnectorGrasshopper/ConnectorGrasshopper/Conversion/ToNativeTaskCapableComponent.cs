using System;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using ConnectorGrasshopper.Extras;
using ConnectorGrasshopper.Objects;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using Speckle.Core.Logging;
using Speckle.Core.Models;
using Utilities = Speckle.Core.Models.Utilities;

namespace ConnectorGrasshopper.Conversion
{
  public class ToNativeTaskCapableComponent : SelectKitTaskCapableComponentBase<IGH_Goo>
  {
    public ToNativeTaskCapableComponent() : base(
      "To Native", 
      "To Native",
      "Convert data from Speckle's Base object to its Rhino equivalent.",
      ComponentCategories.SECONDARY_RIBBON,
      ComponentCategories.CONVERSION)
    {
    }

    public override GH_Exposure Exposure => GH_Exposure.primary;

    protected override Bitmap Icon => Properties.Resources.ToNative;

    public override Guid ComponentGuid => new Guid("7F4BDA01-F9C8-42ED-ABC1-DA0443283219");

    public override bool CanDisableConversion => false;

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
      pManager.AddGenericParameter("Base", "B",
        "Speckle Base object to convert to Grasshopper.", GH_ParamAccess.item);
    }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
      pManager.AddGenericParameter("Data", "D", "Converted data in GH native format.", GH_ParamAccess.item);
    }

    private CancellationTokenSource source;

    protected override void SolveInstance(IGH_DataAccess DA)
    {
      if (RunCount == 1)
      {
        source = new CancellationTokenSource(1000);
      }

      if (InPreSolve)
      {
        // You must place "RunCount == 1" here,
        // because RunCount is reset when "InPreSolve" becomes "false"
        if (RunCount == 1)
          source = new CancellationTokenSource(100);

        object item = null;
        DA.GetData(0, ref item);
        var task = Task.Run(() => DoWork(item, DA), source.Token);
        TaskList.Add(task);
        return;
      }

      if (source.IsCancellationRequested || !GetSolveResults(DA, out var data))
      {
        AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Couldn't do the request");
        DA.AbortComponentSolution(); // You must abort the `SolveInstance` iteration
        return;
      }

      DA.SetData(0, data);
    }

    private IGH_Goo DoWork(object item, IGH_DataAccess DA)
    {
      try
      {
        if (source.Token.IsCancellationRequested)
          DA.AbortComponentSolution();

        return ConnectorGrasshopper.Extras.Utilities.TryConvertItemToNative(item, Converter, true);

      }
      catch (Exception e)
      {
        // If we reach this, something happened that we weren't expecting...
        Log.CaptureException(e);
        AddRuntimeMessage(GH_RuntimeMessageLevel.Error, e.InnerException?.Message ?? e.Message);
        return new GH_SpeckleBase();
      }
    }
  }
}
