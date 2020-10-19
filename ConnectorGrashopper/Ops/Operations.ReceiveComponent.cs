using GH_IO.Serialization;
using Grasshopper.Kernel;
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

    public bool UseDefaultCache { get; set; } = true;

    public double OverallProgress { get; set; } = 0;

    public bool JustPastedIn { get; set; }

    public string BaseId { get; set; }

    public ReceiveComponent() : base("Send", "Send", "Sends data to the provided transports/streams.", "Speckle 2", "   Send/Receive")
    {
      //BaseWorker = new SendComponentWorker(this);
      //Attributes = new SendComponentAttributes(this);
    }

    public override bool Write(GH_IWriter writer)
    {
      writer.SetBoolean("UseDefaultCache", UseDefaultCache);
      writer.SetBoolean("AutoReceive", AutoReceive);
      writer.SetString("CurrentComponentState", CurrentComponentState);
      writer.SetString("BaseId", BaseId);

      return base.Write(writer);
    }

    public override bool Read(GH_IReader reader)
    {
      UseDefaultCache = reader.GetBoolean("UseDefaultCache");
      AutoReceive = reader.GetBoolean("AutoReceive");
      CurrentComponentState = reader.GetString("CurrentComponentState");
      BaseId = reader.GetString("BaseId");

      return base.Read(reader);
    }

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
      pManager.AddGenericParameter("ID", "ID", "The Id of the data you want to receive.", GH_ParamAccess.tree);
      pManager.AddGenericParameter("Stream", "S", "Stream(s) and/or transports to send to.", GH_ParamAccess.tree);
      pManager.AddTextParameter("Branch", "B", "The branch you want your commit associated with.", GH_ParamAccess.tree, "main");
      pManager.AddTextParameter("Message", "M", "Commit message. If left blank, one will be generated for you.", GH_ParamAccess.tree, "");

      Params.Input[2].Optional = true;
      Params.Input[3].Optional = true;
    }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
      pManager.AddGenericParameter("Commits", "C", "The created commits. Commits are created automatically for any streams.", GH_ParamAccess.list);
      pManager.AddTextParameter("Object Id", "O", "The object id (hash) of the sent data.", GH_ParamAccess.list);
      pManager.AddGenericParameter("Data", "D", "The actual sent object.", GH_ParamAccess.list);
    }

    protected override void AppendAdditionalComponentMenuItems(ToolStripDropDown menu)
    {
      var cacheMi = Menu_AppendItem(menu, $"Use default cache", (s, e) => UseDefaultCache = !UseDefaultCache, true, UseDefaultCache);
      cacheMi.ToolTipText = "It's advised you always use the default cache, unless you are providing a list of custom transports and you understand the consequences.";

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


}
