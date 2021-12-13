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

namespace ConnectorGrasshopper.Conversion
{
  public class ToSpeckleTaskCapableComponent : SelectKitTaskCapableComponentBase<IGH_Goo>
  {
    public ToSpeckleTaskCapableComponent() : base(
      "To Speckle",
      "To Speckle",
      "Convert data from Rhino to their Speckle Base equivalent.",
      ComponentCategories.SECONDARY_RIBBON,
      ComponentCategories.CONVERSION)
    {
    }

    private CancellationTokenSource source;

    public override Guid ComponentGuid => new Guid("FB88150A-1885-4A77-92EA-9B1378310FDD");
    protected override Bitmap Icon => Properties.Resources.ToNative;

    public override bool CanDisableConversion => false;

    public override GH_Exposure Exposure => GH_Exposure.primary;

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
      pManager.AddGenericParameter("Data", "D", "Data to convert to Speckle Base objects.", GH_ParamAccess.item);
    }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
      pManager.AddGenericParameter("Base", "B", "Converted Base Speckle objects.", GH_ParamAccess.item);
      //pManager.AddParameter(new SpeckleBaseParam("Base", "B", "Converted Base Speckle objects.", GH_ParamAccess.item));
    }

    protected override void SolveInstance(IGH_DataAccess DA)
    {
      if (InPreSolve)
      {
        // You must place "RunCount == 1" here,
        // because RunCount is reset when "InPreSolve" becomes "false"
        if (RunCount == 1)
          source = new CancellationTokenSource();

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
        {
          DA.AbortComponentSolution();
          return null;
        }
        var converted = Extras.Utilities.TryConvertItemToSpeckle(item, Converter, true);

        if (source.Token.IsCancellationRequested)
        {
          DA.AbortComponentSolution();
          return null;
        }

        if (converted == null)
        {
          AddRuntimeMessage(GH_RuntimeMessageLevel.Warning,
            $"Cannot convert item {DA.ParameterTargetPath(0)}[{DA.ParameterTargetIndex(0)}] to Speckle.");
          return new GH_SpeckleBase();
        }

        if (converted.GetType().IsSimpleType())
          return new GH_ObjectWrapper(converted);

        return new GH_SpeckleBase { Value = converted as Base };
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
