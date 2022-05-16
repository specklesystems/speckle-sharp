using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;
using ConnectorGrasshopper.Extras;
using Grasshopper.Kernel;
using Speckle.Core.Api;
using Speckle.Core.Models;
using Speckle.Core.Transports;
using Logging = Speckle.Core.Logging;

namespace ConnectorGrasshopper.Transports
{
  public class SendReceiveTransport : GH_Component
  {
    public override Guid ComponentGuid { get => new Guid("4229B8DC-9F81-49A3-9EF9-DF3DE0B8E4B6"); }

    protected override Bitmap Icon => Properties.Resources.sendToTransport;

    public override GH_Exposure Exposure => SpeckleGHSettings.ShowDevComponents ? GH_Exposure.primary : GH_Exposure.hidden;

    public SendReceiveTransport() : base("Send To Transports", "ST", "Sends an object to a list of given transports: the object will be stored in each of them. Please use this component with caution: it can freeze your defintion. It also does not perform any conversions, so ensure that the object input already has converted speckle objects inside.", ComponentCategories.SECONDARY_RIBBON, ComponentCategories.TRANSPORTS) { }

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
      pManager.AddGenericParameter("transports", "T", "The transports to send to.", GH_ParamAccess.list);
      pManager.AddParameter(new SpeckleBaseParam("Object", "O", "The speckle object you want to send. It needs to be a Speckle Object in which everything is already converted to Speckle already. ", GH_ParamAccess.item));
    }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
      pManager.AddTextParameter("id", "ID", "The sent object's id.", GH_ParamAccess.item);
    }

    protected override void SolveInstance(IGH_DataAccess DA)
    {
      if (DA.Iteration != 0)
      {
        AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "This component does not work with multiple iterations. Please ensure that you have as inputs a flat list of transports and a single speckle object (combine multiple objects into a root 'parent' speckle object).");
        return;
      }

      Logging.Analytics.TrackEvent(Logging.Analytics.Events.NodeRun, new Dictionary<string, object>() { { "name", "Send To Transports" } });

      List<ITransport> transports = new List<ITransport>();
      DA.GetDataList(0, transports);

      GH_SpeckleBase obj = null;
      DA.GetData(1, ref obj);

      if (obj == null || obj.Value == null || transports.Count == 0)
      {
        AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Invalid inputs.");
        return;
      }

      var freshTransports = new List<ITransport>();
      foreach (var tr in transports)
      {
        if (tr is ICloneable cloneable) freshTransports.Add(cloneable.Clone() as ITransport);
        else freshTransports.Add(tr);
      }
      transports = freshTransports;

      var res = Task.Run(async () => await Speckle.Core.Api.Operations.Send(obj.Value, transports, false, disposeTransports: true)).Result;
      DA.SetData(0, res);
    }
  }

  public class ReceiveFromTransport : GH_Component
  {
    public override Guid ComponentGuid { get => new Guid("8C7C6CA5-1557-4216-810B-F64E710526D0"); }

    protected override Bitmap Icon => Properties.Resources.receiveFromTransport;

    public override GH_Exposure Exposure => SpeckleGHSettings.ShowDevComponents ? GH_Exposure.primary : GH_Exposure.hidden;

    public ReceiveFromTransport() : base("Receive From Transport", "RT", "Receives a list of objects from a given transport. Please use this component with caution: it can freeze your defintion. It also does not perform any conversions on the output.", ComponentCategories.SECONDARY_RIBBON, ComponentCategories.TRANSPORTS) { }

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
      pManager.AddGenericParameter("transport", "T", "The transport to receive from.", GH_ParamAccess.item);
      pManager.AddTextParameter("object ids", "IDs", "The ids of the objects you want to receive.", GH_ParamAccess.list);
    }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
      pManager.AddGenericParameter("objects", "O", "The objects you requested.", GH_ParamAccess.list);
    }

    protected override void SolveInstance(IGH_DataAccess DA)
    {
      if (DA.Iteration != 0)
      {
        AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "This component does not work with multiple iterations. Please ensure you input only one transport and a flat list of object ids.");
        return;
      }

      Logging.Analytics.TrackEvent(Logging.Analytics.Events.NodeRun, new Dictionary<string, object>() { { "name", "Receive From Transports" } });
      
      List<string> ids = new List<string>();
      DA.GetDataList(1, ids);

      object transportGoo = null;
      DA.GetData(0, ref transportGoo);

      var transport = transportGoo.GetType().GetProperty("Value").GetValue(transportGoo) as ITransport;

      if (transport == null)
      {
        AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Transport is null.");
      }

      if (transport is ICloneable disposedTwin)
      {
        transport = disposedTwin.Clone() as ITransport;
      }

      List<Base> results = new List<Base>();
      foreach (var id in ids)
      {
        var res = Task.Run(async () => await Operations.Receive(id, null, transport, disposeTransports: true)).Result;
        results.Add(res);
      }

      DA.SetDataList(0, results);
    }
  }
}
