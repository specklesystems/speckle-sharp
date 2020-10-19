using GH_IO.Serialization;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using GrasshopperAsyncComponent;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Windows.Forms;

namespace ConnectorGrashopper.Ops
{
  public class ReceiveComponent : GH_AsyncComponent
  {
    public override Guid ComponentGuid => new Guid("{3D07C1AC-2D05-42DF-A297-F861CCEEFBC7}");

    public override GH_Exposure Exposure => GH_Exposure.primary;

    public bool AutoReceive { get; set; } = false;

    public string CurrentComponentState { get; set; } = "needs_input";

    public double OverallProgress { get; set; } = 0;

    public bool JustPastedIn { get; set; }

    public string BaseId { get; set; }

    public ReceiveComponent() : base("Receive", "Receive", "Receives Speckle data.", "Speckle 2", "   Send/Receive")
    {
      BaseWorker = new ReceiveComponentWorker(this);
      //Attributes = new SendComponentAttributes(this);
    }

    public override bool Write(GH_IWriter writer)
    {
      return base.Write(writer);
    }

    public override bool Read(GH_IReader reader)
    {
      return base.Read(reader);
    }

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
      pManager.AddGenericParameter("ID", "ID", "The Id of the data you want to receive.", GH_ParamAccess.tree);
    }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
      pManager.AddGenericParameter("Data", "D", "The data.", GH_ParamAccess.tree);
    }

    protected override void AppendAdditionalComponentMenuItems(ToolStripDropDown menu)
    {
      var autoSendMi = Menu_AppendItem(menu, $"Send automatically", (s, e) =>
      {
        AutoReceive = !AutoReceive;
        Rhino.RhinoApp.InvokeOnUiThread((Action)delegate
        {
          OnDisplayExpired(true);
        });
      }, true, AutoReceive);
      autoSendMi.ToolTipText = "Toggle automatic data sending. If set, any change in any of the input parameters of this component will start sending.\n Please be aware that if a new send starts before an old one is finished, the previous operation is cancelled.";

      base.AppendAdditionalComponentMenuItems(menu);
    }

    protected override void SolveInstance(IGH_DataAccess DA)
    {
      if ((AutoReceive || CurrentComponentState == "primed_to_send" || CurrentComponentState == "sending") && !JustPastedIn)
      {
        JustPastedIn = false;
        CurrentComponentState = "sending";
        // Delegate control to parent async component.
        base.SolveInstance(DA);
        return;
      }
      else if (!JustPastedIn)
      {
        CurrentComponentState = "expired";
        Message = "Expired";
        OnDisplayExpired(true);
      }

      // Set output data in a "first run" event. Note: we are not persisting the actual "sent" object as it can be very big.
      if (JustPastedIn)
      {
        DA.SetData(1, BaseId);
      }

      JustPastedIn = false;
    }

    public override void DisplayProgress(object sender, ElapsedEventArgs e)
    {
      if (Workers.Count == 0)
      {
        return;
      }

      Message = "";
      var total = 0.0;
      foreach (var kvp in ProgressReports)
      {
        Message += $"{kvp.Key}: {kvp.Value:0.00%}\n";
        total += kvp.Value;
      }

      OverallProgress = total / ProgressReports.Keys.Count();

      Rhino.RhinoApp.InvokeOnUiThread((Action)delegate
      {
        OnDisplayExpired(true);
      });
    }
  }

  public class ReceiveComponentWorker : WorkerInstance
  {

    GH_Structure<IGH_Goo> DataInput;

    public ReceiveComponentWorker(GH_Component p) : base(p) { }

    public override WorkerInstance Duplicate() => new ReceiveComponentWorker(Parent);

    public override void GetData(IGH_DataAccess DA, GH_ComponentParamServer Params)
    {
      DA.GetDataTree(0, out DataInput);
      var input = DataInput.get_DataItem(0).GetType().GetProperty("Value").GetValue(DataInput.get_DataItem(0));

      var xxx = input;
    }

    public override void DoWork(Action<string, double> ReportProgress, Action Done)
    {
      throw new NotImplementedException();
    }

    public override void SetData(IGH_DataAccess DA)
    {
      throw new NotImplementedException();
    }
  }


}
