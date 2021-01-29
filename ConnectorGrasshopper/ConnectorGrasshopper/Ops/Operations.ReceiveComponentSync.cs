using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Grasshopper.Kernel;
using GrasshopperAsyncComponent;
using Rhino.Geometry;

namespace ConnectorGrasshopper.Ops
{
  public class ReceiveSync : GH_TaskCapableComponent<string>
  {
    /// <summary>
    /// Initializes a new instance of the TaskCapableMultiThread class.
    /// </summary>
    /// 
    CancellationTokenSource source;
    CancellationTokenSource tokenSource;
    const int delay = 1000;
    public WorkerInstance BaseWorker { get; set; }
    public WorkerInstance currentWorker { get; set; }
    public TaskCreationOptions? TaskCreationOptions { get; set; } = null;

    /// <summary>
    /// Initializes a new instance of the Operations class.
    /// </summary>
    public ReceiveSync() : base("Receive", "Receive", "Receive data from a Speckle server",
      "Speckle 2", "   Receive Sync")
    {
      BaseWorker = new ReceiveComponentWorker(this);
    }

    /// <summary>
    /// Registers all the input parameters for this component.
    /// </summary>
    protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
    {
      var streamInputIndex = pManager.AddGenericParameter("Stream", "S", "The Speckle Stream to receive data from. You can also input the Stream ID or it's URL as text.", GH_ParamAccess.tree);
      pManager[streamInputIndex].Optional = true;
    }

    /// <summary>
    /// Registers all the output parameters for this component.
    /// </summary>
    protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
    {
      pManager.AddGenericParameter("Data", "D", "Data received.", GH_ParamAccess.tree);
      pManager.AddTextParameter("Info", "I", "Commit information.", GH_ParamAccess.item);
    }

    /// <summary>
    /// This is the method that actually does the work.
    /// </summary>
    /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
    protected override void SolveInstance(IGH_DataAccess DA)
    {
      if (RunCount == 1)
      {
        source = new CancellationTokenSource(delay);
        tokenSource = new CancellationTokenSource();
        currentWorker = BaseWorker.Duplicate();
      }

      if (InPreSolve)
      {

        if (currentWorker == null)
        {
          AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Could not get a worker instance.");
          return;
        }

        currentWorker.GetData(DA, Params);
        currentWorker.CancellationToken = tokenSource.Token;
        
        var currentRun = TaskCreationOptions != null
        ? new Task(() => currentWorker.DoWork(null, null), tokenSource.Token, (TaskCreationOptions)TaskCreationOptions)
        : new Task(() => currentWorker.DoWork(null, null), tokenSource.Token);
        currentRun.Start();

        var task = Task.Run(() =>
        {
          currentRun.Start();
          currentRun.Wait();
          return "Done";
        }, source.Token);


        TaskList.Add(task);
        return;
      }

      if (source.IsCancellationRequested)
      {
        AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Run out of time!");
      }
      else if (!GetSolveResults(DA, out string data))
      {
        AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Not running multithread");
      }
      else
      {
        currentWorker.SetData(DA);
        DA.SetData(0, data);
      }
    }

    /// <summary>
    /// Provides an Icon for the component.
    /// </summary>
    protected override System.Drawing.Bitmap Icon
    {
      get
      {
        //You can add image files to your project resources and access them like this:
        // return Resources.IconForThisComponent;
        return null;
      }
    }

    /// <summary>
    /// Gets the unique ID for this component. Do not change this ID after release.
    /// </summary>
    public override Guid ComponentGuid
    {
      get { return new Guid("a1e073a0-5203-438b-9310-e212fc81675d"); }
    }
  }
}
