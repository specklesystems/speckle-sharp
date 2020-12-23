using System;
using System.Linq;
using ConnectorGrasshopper.Extras;
using ConnectorGrasshopper.Objects;
using Grasshopper.Kernel;
using GrasshopperAsyncComponent;
using Speckle.Core.Api;

namespace ConnectorGrasshopper.Ops
{
  public class LocalReceiveComponent : SelectKitComponentBase
  {
    public LocalReceiveComponent() : base("Local Receive", "LR",
      "Receives data locally, without the need of a Speckle Server. NOTE: updates will not be automatically received.",
      "Speckle 2", "   Send/Receive")
    {
    }

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
      pManager.AddTextParameter("localDataId", "id", "ID of the data to receive", GH_ParamAccess.item);
    }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
      pManager.AddGenericParameter("Data", "D", "Data received.", GH_ParamAccess.tree);
    }

    protected override void SolveInstance(IGH_DataAccess DA)
    {
      var localDataId = "";
      if (!DA.GetData(0, ref localDataId)) return;

      var @base = Operations.Receive(localDataId).Result;
      if (Converter.CanConvertToNative(@base))
      {
        var data = Converter.ConvertToNative(@base);

        DA.SetData(0, Utilities.TryConvertItemToNative(data, Converter));
      }
      else if (@base.GetDynamicMembers().Count() == 1)
      {
        var treeBuilder = new TreeBuilder(Converter);
        var tree = treeBuilder.Build(@base[@base.GetDynamicMembers().ElementAt(0)]);

        DA.SetDataTree(0, tree);
      }
      else
      {
        DA.SetData(0, new GH_SpeckleBase(@base));
      }
    }

    public override Guid ComponentGuid => new Guid("D690C3C5-B18A-4A7B-B6FC-A4FFF9A55046");
  }
}
