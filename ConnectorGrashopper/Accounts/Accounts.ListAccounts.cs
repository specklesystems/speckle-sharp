using Grasshopper.Kernel;
using Grasshopper.Kernel.Special;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ConnectorGrashopper.Accounts
{
  public class AccountListComponent : GH_ValueList
  {
    public override Guid ComponentGuid => new Guid("734C6CB6-2430-40B3-BE2F-255B27302131");

    public override string Category { get => "Speckle 2"; }

    public override string SubCategory { get => "Accounts"; }

    public override string Description { get => "A dropdown list of your existing Speckle accounts."; }

    public override string Name { get => "Accounts"; }

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
        System.Diagnostics.Process.Start("speckle://goto=accounts");
      });
      return true;
    }

    private void SetAccountList()
    {
      ListItems.Clear();
      var accounts = Speckle.Core.Credentials.AccountManager.GetAccounts();
      var defaultAccount = Speckle.Core.Credentials.AccountManager.GetDefaultAccount();
      int index = 0, defaultAccountIndex = 0;
      foreach (var account in accounts)
      {
        if (defaultAccount != null && account.id == defaultAccount.id)
          defaultAccountIndex = index;

        ListItems.Add(new GH_ValueListItem(account.ToString(), $"\"{account.id}\""));
        index++;
      }

      SelectItem(defaultAccountIndex);
    }

    protected override void ValuesChanged()
    {
      base.ValuesChanged();
    }
  }


}
