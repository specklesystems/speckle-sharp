﻿using Grasshopper.Kernel;
using Logging = Speckle.Core.Logging;
using System;
using System.Collections.Generic;
using System.Drawing;

namespace ConnectorGrasshopper.Transports
{
  public class DiskTransportComponent : GH_Component
  {
    public override Guid ComponentGuid { get => new Guid("BA068B11-2BC0-4669-BC73-09CF16820659"); }

    protected override Bitmap Icon => Properties.Resources.DiskTransport;

    public override GH_Exposure Exposure => GH_Exposure.secondary;

    public DiskTransportComponent() : base("Disk Transport", "Disk", "Creates a Disk Transport. This transport will store objects in files in a folder that you can specify (including one on a network drive!). It's useful for understanding how Speckle's decomposition api works. It's not meant to be performant - it's useful for debugging purposes - e.g., when developing a new class/object model you can understand easily the relative sizes of the resulting objects.", ComponentCategories.SECONDARY_RIBBON, ComponentCategories.TRANSPORTS) { }

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
      pManager.AddTextParameter("base path", "P", "The root folder where you want the data to be stored. Defaults to `%appdata%/Speckle/DiskTransportFiles`.", GH_ParamAccess.item);

      Params.Input.ForEach(p => p.Optional = true);
    }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
      pManager.AddGenericParameter("disk transport", "T", "The Disk Transport you have created.", GH_ParamAccess.item);
    }

    protected override void SolveInstance(IGH_DataAccess DA)
    {
      if (DA.Iteration != 0)
      {
        AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Cannot create multiple transports at the same time. This is an explicit guard against possibly unintended behaviour. If you want to create another transport, please use a new component.");
        return;
      }

      if (DA.Iteration == 0)
      {
        Logging.Tracker.TrackPageview("transports", "disk");
        Logging.Analytics.TrackEvent(Logging.Analytics.Events.NodeRun, new Dictionary<string, object>() { { "name", "Disk Transport" } });
      }

      string basePath = null;
      DA.GetData(0, ref basePath);

      var myTransport = new DiskTransport.DiskTransport(basePath);

      DA.SetData(0, myTransport);
    }
  }
}
