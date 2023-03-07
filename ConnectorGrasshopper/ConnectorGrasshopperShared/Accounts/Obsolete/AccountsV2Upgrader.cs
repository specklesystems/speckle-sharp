using System;
using ConnectorGrasshopper.Extras;
using ConnectorGrasshopper.UpgradeUtilities;
using Grasshopper.Kernel;
#pragma warning disable CS0612

namespace ConnectorGrasshopper.Streams
{
  public class AccountDetailsV2Upgrader: SpeckleUpgradeObject<AccountDetailsComponent, AccountDetailsComponentV2>
  {
    public override void CustomUpgrade(AccountDetailsComponent oldComponent, AccountDetailsComponentV2 newComponent, GH_Document document)
    {
      UpgradeUtils.SwapParameter(newComponent, 0, new SpeckleAccountParam());
    }

    public override DateTime Version => new DateTime(2023, 2, 24);
  }
}
