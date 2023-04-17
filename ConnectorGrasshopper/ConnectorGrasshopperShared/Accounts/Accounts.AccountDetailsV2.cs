using System;
using System.Drawing;
using ConnectorGrasshopper.Extras;
using ConnectorGrasshopper.Properties;
using Grasshopper.Kernel;
using Speckle.Core.Credentials;

namespace ConnectorGrasshopper.Streams;

public class AccountDetailsComponentV2 : GH_SpeckleComponent
{
  public AccountDetailsComponentV2()
    : base(
      "Account Details",
      "AccDet",
      "Gets the details from a specific account",
      ComponentCategories.PRIMARY_RIBBON,
      ComponentCategories.STREAMS
    ) { }

  public override Guid ComponentGuid => new("075AE5E1-F345-4678-B3E9-11B2BF29DF7A");

  protected override Bitmap Icon => Resources.AccountDetails;

  public override GH_Exposure Exposure => GH_Exposure.secondary;

  protected override void RegisterInputParams(GH_InputParamManager pManager)
  {
    pManager.AddParameter(new SpeckleAccountParam { Optional = true });
  }

  protected override void RegisterOutputParams(GH_OutputParamManager pManager)
  {
    pManager.AddBooleanParameter(
      "isDefault",
      "D",
      "Determines if the account is the default of this machine.",
      GH_ParamAccess.item
    );
    pManager.AddTextParameter("Server name", "SN", "Name of the server.", GH_ParamAccess.item);
    pManager.AddTextParameter("Server Company", "SC", "Name of the company running this server.", GH_ParamAccess.item);
    pManager.AddTextParameter("Server URL", "SU", "URL of the server.", GH_ParamAccess.item);
    pManager.AddTextParameter("User ID", "UID", "Unique ID of this account's user.", GH_ParamAccess.item);
    pManager.AddTextParameter("User Name", "UN", "Name of this account's user", GH_ParamAccess.item);
    pManager.AddTextParameter("User Company", "UC", "Name of the company this user belongs to", GH_ParamAccess.item);
    pManager.AddTextParameter("User Email", "UE", "Email of this account's user", GH_ParamAccess.item);
  }

  public override void SolveInstanceWithLogContext(IGH_DataAccess DA)
  {
    Account account = null;
    DA.GetData(0, ref account);

    if (account == null)
    {
      AddRuntimeMessage(GH_RuntimeMessageLevel.Remark, "No account provided. Trying with default account.");
      account = AccountManager.GetDefaultAccount();
    }

    if (account == null)
    {
      AddRuntimeMessage(
        GH_RuntimeMessageLevel.Error,
        "Could not find default account in this machine. Use the Speckle Manager to add an account."
      );
      return;
    }

    if (DA.Iteration == 0) // Only report on first iteration of the component.
      Tracker.TrackNodeRun();

    //Params.Input[0].AddVolatileData(new GH_Path(0), 0, account);

    DA.SetData(0, account.isDefault);
    DA.SetData(1, account.serverInfo.name);
    DA.SetData(2, account.serverInfo.company);
    DA.SetData(3, account.serverInfo.url);
    DA.SetData(4, account.userInfo.id);
    DA.SetData(5, account.userInfo.name);
    DA.SetData(6, account.userInfo.company);
    DA.SetData(7, account.userInfo.email);
  }
}
