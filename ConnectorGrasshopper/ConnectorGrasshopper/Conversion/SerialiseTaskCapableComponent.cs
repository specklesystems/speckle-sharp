using System;
using System.Threading;
using System.Threading.Tasks;
using ConnectorGrasshopper.Extras;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using Speckle.Core.Api;
using Speckle.Core.Models;

namespace ConnectorGrasshopper.Conversion
{
  public class SerializeTaskCapableComponent : GH_TaskCapableComponent<string>
  {
    private CancellationTokenSource source;
    public override Guid ComponentGuid => new Guid("6F6A5347-8DE1-44FA-8D26-C73FD21650A9");
    public override GH_Exposure Exposure => GH_Exposure.secondary;
    protected override System.Drawing.Bitmap Icon => Properties.Resources.Serialize;

    public SerializeTaskCapableComponent() : base(
      "Serialize",
      "SRL",
      "Serializes a Speckle Base object to JSON",
      ComponentCategories.SECONDARY_RIBBON,
      ComponentCategories.CONVERSION)
    {
    }

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
      pManager.AddParameter(
        new SpeckleBaseParam("Base", "B", "Speckle base objects to serialize.", GH_ParamAccess.item));
    }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
      pManager.AddTextParameter("Json", "J", "Serialized objects in JSON format.", GH_ParamAccess.item);
    }

    protected override void SolveInstance(IGH_DataAccess DA)
    {
      if (InPreSolve)
      {
        // You must place "RunCount == 1" here,
        // because RunCount is reset when "InPreSolve" becomes "false"
        if (RunCount == 1)
          source = new CancellationTokenSource();

        GH_SpeckleBase item = null;
        DA.GetData(0, ref item);
        var task = Task.Run(() => DoWork(item, DA), source.Token);
        TaskList.Add(task);
        return;
      }

      if (source.IsCancellationRequested || !GetSolveResults(DA, out var data))
      {
        DA.AbortComponentSolution(); // You must abort the `SolveInstance` iteration
        return;
      }

      DA.SetData(0, data);
    }

    private string DoWork(GH_SpeckleBase item, IGH_DataAccess DA)
    {
      if (item?.Value != null)
        try
        {
          return Operations.Serialize(item.Value);
        }
        catch (Exception e)
        {
          AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, e.Message);
          return null;
        }

      AddRuntimeMessage(GH_RuntimeMessageLevel.Warning,
        $"Item at path {{{DA.ParameterTargetPath(0)}}}[{DA.ParameterTargetIndex(0)}] is not a Base object.");
      return null;
    }
  }
}
