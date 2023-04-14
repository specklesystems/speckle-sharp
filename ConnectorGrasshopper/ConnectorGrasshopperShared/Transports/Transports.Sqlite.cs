using System;
using System.Drawing;
using System.Linq;
using ConnectorGrasshopper.Properties;
using Grasshopper;
using Grasshopper.Kernel;
using Speckle.Core.Helpers;
using Speckle.Core.Transports;

namespace ConnectorGrasshopper.Transports;

public class SqliteTransportComponent : GH_SpeckleComponent
{
  public SqliteTransportComponent()
    : base(
      "Sqlite Transport",
      "Sqlite",
      "Creates a Sqlite Transport. This transport will store its objects in a sqlite database at the location you will specify (including a network drive!).",
      ComponentCategories.SECONDARY_RIBBON,
      ComponentCategories.TRANSPORTS
    )
  {
    SpeckleGHSettings.SettingsChanged += (_, args) =>
    {
      if (args.Key != SpeckleGHSettings.SHOW_DEV_COMPONENTS)
        return;

      var proxy = Instances.ComponentServer.ObjectProxies.FirstOrDefault(p => p.Guid == ComponentGuid);
      if (proxy == null)
        return;
      proxy.Exposure = Exposure;
    };
  }

  internal static Guid internalGuid => new("DFFAF45E-06A8-4458-85D8-74FDA8DF3268");
  internal static GH_Exposure internalExposure =>
    SpeckleGHSettings.ShowDevComponents ? GH_Exposure.primary : GH_Exposure.hidden;

  public override Guid ComponentGuid => internalGuid;

  protected override Bitmap Icon => Resources.SQLiteTransport;

  public override GH_Exposure Exposure =>
    SpeckleGHSettings.ShowDevComponents ? GH_Exposure.secondary : GH_Exposure.hidden;

  protected override void RegisterInputParams(GH_InputParamManager pManager)
  {
    pManager.AddTextParameter(
      "Base Path",
      "P",
      "The root folder where you want the sqlite db to be stored. Defaults to `%appdata%`.",
      GH_ParamAccess.item,
      SpecklePathProvider.UserApplicationDataPath()
    );
    pManager.AddTextParameter(
      "Application Name",
      "N",
      "The subfolder you want the sqlite db to be stored. Defaults to `Speckle`.",
      GH_ParamAccess.item,
      "Speckle"
    );
    pManager.AddTextParameter(
      "Database Name",
      "D",
      "The name of the actual database file. Defaults to `UserLocalDefaultDb`.",
      GH_ParamAccess.item,
      "UserLocalDefaultDb"
    );
  }

  protected override void RegisterOutputParams(GH_OutputParamManager pManager)
  {
    pManager.AddGenericParameter(
      "sqlite transport",
      "T",
      "The Sqlite transport you have created.",
      GH_ParamAccess.item
    );
  }

  public override void SolveInstanceWithLogContext(IGH_DataAccess DA)
  {
    if (DA.Iteration != 0)
    {
      AddRuntimeMessage(
        GH_RuntimeMessageLevel.Warning,
        "Cannot create multiple transports at the same time. This is an explicit guard against possibly unintended behaviour. If you want to create another transport, please use a new component."
      );
      return;
    }

    if (DA.Iteration == 0)
      Tracker.TrackNodeRun();

    string basePath = null,
      applicationName = null,
      scope = null;

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
