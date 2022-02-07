﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using GH_IO.Serialization;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Special;
using Rhino;
using Speckle.Core.Credentials;
using Logging = Speckle.Core.Logging;

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
      //SetAccountList();
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
      ListItems.Add(new GH_ValueListItem("No account selected", ""));
      var accounts = AccountManager.GetAccounts().ToList();
      var defaultAccount = AccountManager.GetDefaultAccount();
      int index = 0, defaultAccountIndex = 0;

      if (accounts.Count == 0)
      {

        SelectItem(0);
        AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "No accounts found. Please use the Speckle Manager to manage your accounts on this computer.");
        return;
      }

      foreach (var account in accounts)
      {
        if (defaultAccount != null && account.userInfo.id == defaultAccount.userInfo.id)
        {
          defaultAccountIndex = index + 1;
        }

        ListItems.Add(new GH_ValueListItem(account.ToString(), $"\"{account.userInfo.id}\""));
        index++;
      }

      Logging.Analytics.TrackEvent(Logging.Analytics.Events.NodeRun, new Dictionary<string, object>() { { "name", "Account List" } });

      if (string.IsNullOrEmpty(selectedServerUrl) && string.IsNullOrEmpty(selectedUserId))
      {
        // This is a new component, use default account
        SelectItem(defaultAccountIndex);

        return;
      }

      // Not a new component, should have account set.
      var selectedIndex = GetSelectedAccountIndex(accounts);
      SelectItem(selectedIndex != -1 ? selectedIndex : 0);
    }

    private int GetSelectedAccountIndex(List<Account> accounts)
    {
      //TODO: Refactor this into a method
      // Check for the specific user ID that was selected before.
      var acc = accounts.Find(a => a.userInfo.id == selectedUserId);
      if (acc != null)
      {
        var accIndex = accounts.IndexOf(acc);
        return accIndex + 1;
      }

      // If the selected account doesn't work, try with another account in the same server
      acc = accounts.FirstOrDefault(a => a.serverInfo.url == selectedServerUrl);
      if (acc != null)
      {
        var accIndex = accounts.IndexOf(acc);
        AddRuntimeMessage(GH_RuntimeMessageLevel.Remark, "Account mismatch. Using a different account for the same server.");

        return accIndex + 1;
      }
      // If no accounts exist on the selected server, throw error in node.
      return -1;
    }

    private string selectedUserId;
    private string selectedServerUrl;

    public override bool Read(GH_IReader reader)
    {
      try
      {
        selectedUserId = reader.GetString("selectedId");
        selectedServerUrl = reader.GetString("selectedServer");
      }
      catch (Exception e)
      {
        Console.WriteLine(e);
      }
      return base.Read(reader);
    }

    public override bool Write(GH_IWriter writer)
    {
      try
      {
        var selectedUserId = FirstSelectedItem.Expression?.Trim(new char[] { '"' });
        var selectedAccount = AccountManager.GetAccounts().FirstOrDefault(a => a.userInfo.id == selectedUserId);
        if (selectedAccount != null)
        {
          writer.SetString("selectedId", selectedUserId);
          writer.SetString("selectedServer", selectedAccount.serverInfo.url);
        }
        else
        {
          writer.SetString("selectedId", "-");
          writer.SetString("selectedServer", "-");
        }
      }
      catch (Exception e)
      {
        Console.WriteLine(e);
      }
      return base.Write(writer);
    }

    protected override void CollectVolatileData_Custom()
    {
      m_data.ClearData();

      if (FirstSelectedItem.Value != null)
        m_data.Append(this.FirstSelectedItem.Value, new GH_Path(0));
    }

    public override void AddedToDocument(GH_Document document)
    {
      Logging.Tracker.TrackPageview(Logging.Tracker.ACCOUNT_LIST);
      base.AddedToDocument(document);
      SetAccountList();
    }
  }

}
