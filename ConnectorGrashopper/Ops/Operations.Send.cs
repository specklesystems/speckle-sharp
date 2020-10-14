using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using GrasshopperAsyncComponent;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConnectorGrashopper.Ops
{
  public class SendComponent : GH_AsyncComponent
  {
    public override Guid ComponentGuid => new Guid("{5E6A5A78-9E6F-4893-8DED-7EEAB63738A5}");

    public override GH_Exposure Exposure => GH_Exposure.primary;

    public SendComponent() : base("Send", "Send", "Sends data to a stream and creates a commit.", "Speckle 2", "Send/Receive")
    {
      
    }

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
      pManager.AddGenericParameter("Data", "D", "A Speckle object containing the data you want to send.", GH_ParamAccess.tree);
      pManager.AddGenericParameter("Stream", "S", "Stream(s) to send to.", GH_ParamAccess.tree);
      pManager.AddTextParameter("Branch", "B", "The branch you want your commit associated with.", GH_ParamAccess.tree);
      pManager.AddTextParameter("Message", "M", "Commit message.", GH_ParamAccess.tree);
    }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
      pManager.AddTextParameter("Id", "Id", "Commit id", GH_ParamAccess.list);
    }

    protected override void SolveInstance(IGH_DataAccess DA)
    {
      base.SolveInstance(DA);
    }
  }

  public class SendComponentWorker : WorkerInstance
  {

    GH_Structure<IGH_Goo> DataInput;
    GH_Structure<IGH_Goo> StreamsInput;
    GH_Structure<IGH_Goo> BranchNameInput;
    GH_Structure<IGH_Goo> MessageInput;

    public SendComponentWorker() { }
    
    public override WorkerInstance Duplicate() => new SendComponentWorker();
    
    public override void GetData(IGH_DataAccess DA, GH_ComponentParamServer Params)
    {
      DA.GetDataTree(0, out DataInput);
      DA.GetDataTree(0, out StreamsInput);
      DA.GetDataTree(0, out BranchNameInput);
      DA.GetDataTree(0, out MessageInput);
    }

    public override void DoWork(Action<string, double> ReportProgress, Action<string, GH_RuntimeMessageLevel> ReportError, Action Done)
    {
      throw new NotImplementedException();

    }




    public override void SetData(IGH_DataAccess DA)
    {
      throw new NotImplementedException();
    }
  }
}
