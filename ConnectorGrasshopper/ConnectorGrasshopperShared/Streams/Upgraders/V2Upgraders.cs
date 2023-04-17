using System;
using ConnectorGrasshopper.Extras;
using ConnectorGrasshopper.UpgradeUtilities;
using Grasshopper.Kernel;

#pragma warning disable CS0612

namespace ConnectorGrasshopper.Streams.Upgrade;

public class StreamCreateV2UpgradeObject : SpeckleUpgradeObject<StreamCreateComponent, StreamCreateComponentV2>
{
  public override DateTime Version => new(2023, 2, 14);

  public override void CustomUpgrade(
    StreamCreateComponent oldComponent,
    StreamCreateComponentV2 newComponent,
    GH_Document document
  )
  {
    UpgradeUtils.SwapParameter(newComponent, 0, new SpeckleAccountParam());
    newComponent.stream = oldComponent.stream;
  }
}

public class StreamDetailsV2UpgradeObject : SpeckleUpgradeObject<StreamDetailsComponent, StreamDetailsComponentV2>
{
  public override DateTime Version => new(2023, 2, 14);

  public override void CustomUpgrade(
    StreamDetailsComponent oldComponent,
    StreamDetailsComponentV2 newComponent,
    GH_Document document
  )
  {
    newComponent.Params.Input.ForEach(p => p.Access = GH_ParamAccess.item);
    newComponent.Params.Output.ForEach(p => p.Access = GH_ParamAccess.item);
  }
}

public class StreamGetV2UpgradeObject : SpeckleUpgradeObject<StreamGetComponent, StreamGetComponentV2>
{
  public override DateTime Version => new(2023, 2, 14);

  public override void CustomUpgrade(
    StreamGetComponent oldComponent,
    StreamGetComponentV2 newComponent,
    GH_Document document
  )
  {
    UpgradeUtils.SwapParameter(newComponent, 1, new SpeckleAccountParam());
  }
}

public class StreamListV2UpgradeObject : SpeckleUpgradeObject<StreamListComponent, StreamListComponentV2>
{
  public override DateTime Version => new(2023, 2, 14);

  public override void CustomUpgrade(
    StreamListComponent oldComponent,
    StreamListComponentV2 newComponent,
    GH_Document document
  )
  {
    UpgradeUtils.SwapParameter(newComponent, 0, new SpeckleAccountParam());
  }
}

public class StreamUpdateV2UpgradeObject : SpeckleUpgradeObject<StreamUpdateComponent, StreamUpdateComponentV2>
{
  public override DateTime Version => new(2023, 2, 14);

  public override void CustomUpgrade(
    StreamUpdateComponent oldComponent,
    StreamUpdateComponentV2 newComponent,
    GH_Document document
  ) { }
}
