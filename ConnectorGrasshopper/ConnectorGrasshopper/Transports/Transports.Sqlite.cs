﻿using Grasshopper.Kernel;
using Logging = Speckle.Core.Logging;
using Speckle.Core.Transports;
using System;
using System.Collections.Generic;
using System.Drawing;

namespace ConnectorGrasshopper.Transports
{
  public class SqliteTransportComponent : GH_Component
  {
    public override Guid ComponentGuid { get => new Guid("DFFAF45E-06A8-4458-85D8-74FDA8DF3268"); }

    protected override Bitmap Icon => Properties.Resources.SQLiteTransport;

    public override GH_Exposure Exposure => GH_Exposure.secondary;

    public SqliteTransportComponent() : base("Sqlite Transport", "Sqlite", "Creates a Sqlite Transport. This transport will store its objects in a sqlite database at the location you will specify (including a network drive!).", ComponentCategories.SECONDARY_RIBBON, ComponentCategories.TRANSPORTS) { }

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
      pManager.AddTextParameter("Base Path", "P", "The root folder where you want the sqlite db to be stored. Defaults to `%appdata%`.", GH_ParamAccess.item, Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData));
      pManager.AddTextParameter("Application Name", "N", "The subfolder you want the sqlite db to be stored. Defaults to `Speckle`.", GH_ParamAccess.item, "Speckle");
      pManager.AddTextParameter("Database Name", "D", "The name of the actual database file. Defaults to `UserLocalDefaultDb`.", GH_ParamAccess.item, "UserLocalDefaultDb");
    }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
      pManager.AddGenericParameter("sqlite transport", "T", "The Sqlite transport you have created.", GH_ParamAccess.item);
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
        Logging.Tracker.TrackPageview("transports", "sqlite");
        Logging.Analytics.TrackEvent(Logging.Analytics.Events.NodeRun, new Dictionary<string, object>() { { "name", "SQLite Transport" } });
      }

      string basePath = null, applicationName = null, scope = null;

      DA.GetData(0, ref basePath);
      DA.GetData(1, ref applicationName);
      DA.GetData(2, ref scope);

      var myTransport = new SQLiteTransport(basePath, applicationName, scope);

      DA.SetData(0, myTransport);
    }

    protected override void BeforeSolveInstance()
    {
      base.BeforeSolveInstance();
    }

  }
}
