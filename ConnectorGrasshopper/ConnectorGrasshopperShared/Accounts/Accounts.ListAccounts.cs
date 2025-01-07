#nullable enable
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using ConnectorGrasshopper.Extras;
using ConnectorGrasshopper.Properties;
using GH_IO.Serialization;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Special;
using Grasshopper.Kernel.Types;
using Speckle.Core.Credentials;
using Speckle.Core.Helpers;

namespace ConnectorGrasshopper.Accounts;

public class AccountListComponent : GH_ValueList, ISpeckleTrackingDocumentObject
{
  private Uri? _selectedAccountUrl;

  public AccountListComponent()
  {
    MutableNickName = false;
    //SetAccountList();
    Tracker = new ComponentTracker(null);
    _selectedAccountUrl = null;
  }

  protected override Bitmap Icon => Resources.Accounts;
  public override Guid ComponentGuid => new("734C6CB6-2430-40B3-BE2F-255B27302131");
  public override string Category => ComponentCategories.SECONDARY_RIBBON;
  public override string SubCategory => ComponentCategories.STREAMS;
  public override string Description => "A dropdown list of your existing Speckle accounts.";
  public override string Name => "Accounts";
  public override GH_Exposure Exposure => GH_Exposure.primary;
  public ComponentTracker Tracker { get; set; }
  public bool IsNew { get; set; } = true;

  public override bool AppendMenuItems(ToolStripDropDown menu)
  {
    Menu_AppendItem(menu, "Refresh list (will reset your selection!)", (s, e) => SetAccountList());
    Menu_AppendSeparator(menu);
    Menu_AppendItem(
      menu,
      "Launch the Speckle Manager to add new accounts, or remove old ones.",
      (s, e) =>
      {
        Open.Url("speckle://goto=accounts");
      }
    );
    return true;
  }

  private void SetAccountList()
  {
    ListItems.Clear();
    ListItems.Add(new GH_ValueListItem("No account selected", ""));
    var accounts = AccountManager.GetAccounts().ToList();

    if (accounts.Count == 0)
    {
      SelectItem(0);
      AddRuntimeMessage(
        GH_RuntimeMessageLevel.Warning,
        "No accounts found. Please use the Speckle Manager to manage your accounts on this computer."
      );
      return;
    }

    Tracker.TrackNodeRun("Accounts list");

    var valueItems = accounts.Select(account => new GH_ValueListItem(
      account.ToString(),
      $"\"{AccountManager.GetLocalIdentifierForAccount(account)}\""
    ));
    ListItems.AddRange(valueItems);

    if (_selectedAccountUrl == null)
    {
      // This is a new component, use default account + 1 (first item is "No account selected")
      var defaultAccountIndex = accounts.FindIndex(acc => acc.isDefault);
      SelectItem(defaultAccountIndex + 1);
      return;
    }

    // Not a new component, should have account set.
    var selectedIndex = GetSelectedAccountIndex(accounts);
    SelectItem(selectedIndex != -1 ? selectedIndex : 0);
  }

  private int GetSelectedAccountIndex(List<Account> accounts)
  {
    if (_selectedAccountUrl == null)
    {
      return -1;
    }

    var acc = AccountManager.GetAccountForLocalIdentifier(_selectedAccountUrl);
    if (acc != null)
    {
      var accIndex = accounts.IndexOf(acc);
      return accIndex + 1;
    }
    else
    {
      // If the selected account doesn't work, try with another account in the same server
      acc = accounts.FirstOrDefault(a => a.serverInfo.url == _selectedAccountUrl.GetLeftPart(UriPartial.Authority));
      if (acc != null)
      {
        var accIndex = accounts.IndexOf(acc);
        AddRuntimeMessage(
          GH_RuntimeMessageLevel.Remark,
          "Account mismatch. Using a different account for the same server."
        );

        return accIndex + 1;
      }
    }

    // If no accounts exist on the selected server, throw error in node.
    return -1;
  }

  public override bool Read(GH_IReader reader)
  {
    // Set isNew to false, indicating this node already existed in some way. This prevents the `NodeCreate` event from being raised.
    IsNew = false;

    string? userId = null;
    string? serverUrl = null;
    var idSuccess = reader.TryGetString("selectedId", ref userId);
    var serverSuccess = reader.TryGetString("selectedServer", ref serverUrl);

    var isIdValid = idSuccess && userId != "-";
    var isServerValid = serverSuccess && serverUrl != "-";

    string? accountUrl = null;
    var accountUrlSuccess = reader.TryGetString(nameof(FirstSelectedItem), ref accountUrl);
    var isAccountUrlValid = accountUrlSuccess && !string.IsNullOrEmpty(accountUrl);

    if (isAccountUrlValid)
    {
      _selectedAccountUrl = new(accountUrl);
    }
    else if (isIdValid && isServerValid)
    {
      _selectedAccountUrl = new(serverUrl + "?id=" + userId);
    }

    return base.Read(reader);
  }

  public override bool Write(GH_IWriter writer)
  {
    writer.SetString(nameof(FirstSelectedItem), FirstSelectedItem.Expression.Trim('"'));
    return base.Write(writer);
  }

  /// <summary>
  /// Custom method for collecting volatile data.
  /// </summary>
  protected override void CollectVolatileData_Custom()
  {
    m_data.ClearData();
    if (FirstSelectedItem.Value is GH_String strGoo)
    {
      var x = AccountManager.GetAccountForLocalIdentifier(new Uri(strGoo.Value));
      m_data.Append(new GH_SpeckleAccountGoo { Value = x }, new GH_Path(0));
    }
    AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "hello");
  }

  public override void AddedToDocument(GH_Document document)
  {
    base.AddedToDocument(document);
    SetAccountList();
    // If the node is new (i.e. GH has not called Read(...) ) we log the node creation event.
    if (IsNew)
    {
      Tracker.TrackNodeCreation("Accounts list");
    }
  }
}
