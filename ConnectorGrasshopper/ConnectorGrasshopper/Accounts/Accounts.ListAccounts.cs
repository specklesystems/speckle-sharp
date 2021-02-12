using GH_IO.Serialization;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Special;
using Speckle.Core.Credentials;
using Speckle.Core.Logging;
using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace ConnectorGrasshopper.Accounts
{
  public class AccountListComponent : GH_ValueList
  {
    protected override Bitmap Icon => Properties.Resources.Accounts;

    public override Guid ComponentGuid => new Guid("734C6CB6-2430-40B3-BE2F-255B27302131");

    public override string Category { get => ComponentCategories.SECONDARY_RIBBON; }

    public override string SubCategory { get => ComponentCategories.STREAMS; }

    public override string Description { get => "A dropdown list of your existing Speckle accounts."; }

    public override string Name { get => "Accounts"; }

    public override GH_Exposure Exposure => GH_Exposure.primary;

    public AccountListComponent() : base()
    {
      MutableNickName = false;
      SetAccountList();
    }

    public override bool AppendMenuItems(ToolStripDropDown menu)
    {
      Menu_AppendItem(menu, "Refresh list (will reset your selection!)", (s, e) => SetAccountList());
      Menu_AppendSeparator(menu);
      Menu_AppendItem(menu, "Launch the Speckle Manager to add new accounts, or remove old ones.", (s, e) =>
      {
        // TODO: path to actual exe
        System.Diagnostics.Process.Start("speckle://goto=accounts");
      });
      return true;
    }

    private void SetAccountList()
    {
      ListItems.Clear();
      var accounts = AccountManager.GetAccounts();
      var defaultAccount = AccountManager.GetDefaultAccount();
      int index = 0, defaultAccountIndex = 0;

      if(accounts.ToList().Count == 0)
      {
        ListItems.Add(new GH_ValueListItem("No accounts where found on this machine. Right-click for more options.",""));
        AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "No accounts found. Please use the Speckle Manager to manage your accounts on this computer.");
        SelectItem(0);
        return;
      }

      foreach (var account in accounts)
      {
        if (defaultAccount != null && account.userInfo.id == defaultAccount.userInfo.id)
        {
          defaultAccountIndex = index;
        }

        ListItems.Add(new GH_ValueListItem(account.ToString(), $"\"{account.userInfo.id}\""));
        index++;
      }

      SelectItem(defaultAccountIndex);
    }

    public override bool Read(GH_IReader reader)
    {
      var accounts = AccountManager.GetAccounts().ToList();
      var selectedAccountId = reader.GetString("selectedId");

      var account = accounts.FirstOrDefault(a => a.id == selectedAccountId);
      if (account != null)
      {
        var index = accounts.IndexOf(account);
        SelectItem(index);
        return base.Read(reader);
      }

      var selectedServerUrl = reader.GetString("selectedServer");
      account = accounts.FirstOrDefault(a => a.serverInfo.url == selectedServerUrl);
      if (account != null)
      {
        var index = accounts.IndexOf(account);
        SelectItem(index);
        AddRuntimeMessage(Grasshopper.Kernel.GH_RuntimeMessageLevel.Remark, "Account mismatch. Using a different account for the same server.");
        return base.Read(reader);
      }

      AddRuntimeMessage(Grasshopper.Kernel.GH_RuntimeMessageLevel.Remark, "Account mismatch. Using the default one.");
      return base.Read(reader);
    }

    public override bool Write(GH_IWriter writer)
    {
      var selectedAccountId = SelectedItems[0].Expression.Trim(new char[] { '"' });
      var selectedAccount = AccountManager.GetAccounts().FirstOrDefault(a => a.userInfo.id == selectedAccountId);
      if (selectedAccount != null)
      {
        writer.SetString("selectedId", selectedAccountId);
        writer.SetString("selectedServer", selectedAccount.serverInfo.url);
      }

      return base.Write(writer);
    }

    public override void AddedToDocument(GH_Document document)
    {
      Tracker.TrackPageview("accounts", "added");
      base.AddedToDocument(document);
    }
  }


}
