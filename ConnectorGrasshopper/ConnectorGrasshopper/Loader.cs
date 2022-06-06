using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Timers;
using System.Windows.Forms;
using Grasshopper.Kernel;
using Rhino;
using Speckle.Core.Kits;
using Speckle.Core.Logging;

namespace ConnectorGrasshopper
{
  public static class KeyWatcher
  {
    public static bool TabPressed;
  }

  public class Loader : GH_AssemblyPriority
  {
    public bool MenuHasBeenAdded;

    public IEnumerable<ISpeckleKit> loadedKits;
    public ISpeckleKit selectedKit;
    private ToolStripMenuItem speckleMenu;
    private IEnumerable<ToolStripItem> kitMenuItems;


    public override GH_LoadingInstruction PriorityLoad()
    {
      var version = VersionedHostApplications.Grasshopper6;
      if (RhinoApp.Version.Major == 7)
        version = VersionedHostApplications.Grasshopper7;
      
      try
      {
        typeof(Setup).InvokeMember(
          "Init",
          BindingFlags.Static | BindingFlags.InvokeMethod,
          null,
          null,
          new object[] { version, HostApplications.Grasshopper.Slug });
      }
      catch (MissingMethodException e)
      {
        Console.WriteLine(e);
      }

      Grasshopper.Instances.DocumentServer.DocumentAdded += CanvasCreatedEvent;
      Grasshopper.Instances.ComponentServer.AddCategoryIcon(ComponentCategories.PRIMARY_RIBBON,
        Properties.Resources.speckle_logo);
      Grasshopper.Instances.ComponentServer.AddCategorySymbolName(ComponentCategories.PRIMARY_RIBBON, 'S');
      Grasshopper.Instances.ComponentServer.AddCategoryIcon(ComponentCategories.SECONDARY_RIBBON,
        Properties.Resources.speckle_logo);
      Grasshopper.Instances.ComponentServer.AddCategorySymbolName(ComponentCategories.SECONDARY_RIBBON, 'S');
      return GH_LoadingInstruction.Proceed;
    }

    private static DialogResult ShowLoadErrorMessageBox()
    {
      return MessageBox.Show(
        "There was a problem setting up Speckle\n" +
        "This can be caused by \n\n" +
        "- A corrupted install\n" +
        "- Another Grasshopper plugin using an older version of Speckle\n" +
        "- Having an older version of the Rhino connector installed\n" +
        "Try reinstalling both Rhino and Grasshopper connectors.\n\n" +
        "If the problem persists, please reach out to our Community Forum (https://speckle.community)",
        "Speckle Error",
        MessageBoxButtons.OK);
    }

    private void CanvasCreatedEvent(GH_DocumentServer server, GH_Document doc)
    {
      try
      {
        AddSpeckleMenu(null, null);
      }
      catch (Exception e)
      {
        ShowLoadErrorMessageBox();
      }

      if (Grasshopper.Instances.ActiveCanvas != null)
      {
        Grasshopper.Instances.ActiveCanvas.KeyDown += (s, e) =>
        {
          if (e.KeyCode == Keys.Tab && !KeyWatcher.TabPressed)
            KeyWatcher.TabPressed = true;
        };

        Grasshopper.Instances.ActiveCanvas.KeyUp += (s, e) =>
        {
          if (KeyWatcher.TabPressed && e.KeyCode == Keys.Tab)
            KeyWatcher.TabPressed = false;
        };
      }
    }

    private void HandleKitSelectedEvent(object sender, EventArgs args)
    {
      var clickedItem = (ToolStripMenuItem)sender;

      // Update the selected kit
      selectedKit = loadedKits.First(kit => clickedItem.Text.Trim() == kit.Name);
      SpeckleGHSettings.SelectedKitName = selectedKit.Name;

      // Update the check status of all
      foreach (var item in kitMenuItems)
      {
        if (item is ToolStripMenuItem menuItem)
          menuItem.CheckState =
            clickedItem.Text.Trim() == selectedKit.Name
              ? CheckState.Checked
              : CheckState.Unchecked;
      }
    }

    private void AddSpeckleMenu(object sender, ElapsedEventArgs e)
    {
      if (Grasshopper.Instances.DocumentEditor == null || MenuHasBeenAdded) return;

      speckleMenu = new ToolStripMenuItem("Speckle 2");

      var kitHeader = speckleMenu.DropDown.Items.Add("Select the converter you want to use.");
      kitHeader.Enabled = false;

      try
      {
        loadedKits = KitManager.GetKitsWithConvertersForApp(Extras.Utilities.GetVersionedAppName());

        var kitItems = new List<ToolStripItem>();
        loadedKits.ToList().ForEach(kit =>
        {
          var item = speckleMenu.DropDown.Items.Add("  " + kit.Name);

          item.Click += HandleKitSelectedEvent;
          kitItems.Add(item);
        });
        kitMenuItems = kitItems;
      }
      catch (Exception exception)
      {
        Log.CaptureException(exception);
        var errItem = speckleMenu.DropDown.Items.Add("An error occurred while fetching Kits");
        errItem.Enabled = false;
      }

      speckleMenu.DropDown.Items.Add(new ToolStripSeparator());
      CreateSchemaConversionMenu();
      speckleMenu.DropDown.Items.Add(new ToolStripSeparator());
      CreateMeshingSettingsMenu();
      speckleMenu.DropDown.Items.Add(new ToolStripSeparator());
      CreateTabsMenu();
      speckleMenu.DropDown.Items.Add(new ToolStripSeparator());
  
      // Help items
      var helpHeader = speckleMenu.DropDown.Items.Add("Looking for help?");
      helpHeader.Enabled = false;
      speckleMenu.DropDown.Items.Add("Community Forum", Properties.Resources.forum16,
        (o, args) => Process.Start("https://speckle.community/tag/grasshopper"));
      speckleMenu.DropDown.Items.Add("Tutorials", Properties.Resources.tutorials16,
        (o, args) => Process.Start("https://speckle.systems/tag/grasshopper/"));
      speckleMenu.DropDown.Items.Add("Docs", Properties.Resources.docs16,
        (o, args) => Process.Start("https://speckle.guide/user/grasshopper.html"));

      speckleMenu.DropDown.Items.Add(new ToolStripSeparator());

      // Manager button
      speckleMenu.DropDown.Items.Add("Open Speckle Manager", Properties.Resources.speckle_logo,
        (o, args) => Process.Start("speckle://"));

      try
      {
        var mainMenu = Grasshopper.Instances.DocumentEditor.MainMenuStrip;
        Grasshopper.Instances.DocumentEditor.Invoke(new Action(() =>
        {
          if (!MenuHasBeenAdded)
          {
            mainMenu.Items.Add(speckleMenu);
            // Select the first kit by default.
            if (speckleMenu.DropDown.Items.Count > 0)
              HandleKitSelectedEvent(kitMenuItems.FirstOrDefault(k => k.Text.Trim() == "Objects"), null);
          }
        }));
      }
      catch (Exception err)
      {
        Log.CaptureException(err);
        var errItem = speckleMenu.DropDown.Items.Add("An error occurred while fetching Kits", null);
        errItem.Enabled = false;
      }

      MenuHasBeenAdded = true;
    }

    private void CreateTabsMenu()
    {
      var tabsMenu = speckleMenu.DropDown.Items.Add("Show/Hide Components") as ToolStripMenuItem;
      var warn = tabsMenu.DropDown.Items.Add("Changes require restarting Rhino to take effect.");
      warn.Enabled = false;
      new List<string>
      {
        "BIM",
        "Revit",
        "Structural",
        "ETABS",
        "GSA",
        "Tekla",
        "CSI"
      }.ForEach(s =>
      {
        var category = $"Speckle 2 {s}";
        var itemName = $"Show {s} components";
        var mi = tabsMenu.DropDown.Items.Add(itemName) as ToolStripMenuItem;
        mi.CheckOnClick = true;
        mi.Checked = SpeckleGHSettings.GetTabVisibility(category);
        mi.Click += (sender, args) =>
        {
          var tmi = sender as ToolStripMenuItem;
          SpeckleGHSettings.SetTabVisibility(category, tmi.Checked);
        };
      });
      
      tabsMenu.DropDown.Items.Add(new ToolStripSeparator());
      
      var showDevItem = new ToolStripMenuItem("Show Developer components", null, (o, args) =>
      {
        SpeckleGHSettings.ShowDevComponents = !SpeckleGHSettings.ShowDevComponents;
      });
      showDevItem.Checked = SpeckleGHSettings.ShowDevComponents;
      showDevItem.CheckOnClick = true;
      tabsMenu.DropDown.Items.Add(showDevItem);
      KeepOpenOnDropdownCheck(tabsMenu);
    }

    private void CreateMeshingSettingsMenu()
    {
      var defaultSetting = new ToolStripMenuItem(
        "Default") { Checked = SpeckleGHSettings.MeshSettings == SpeckleMeshSettings.Default, CheckOnClick = true };

      var currentDocSetting = new ToolStripMenuItem(
        "Current Rhino doc")
      {
        Checked = SpeckleGHSettings.MeshSettings == SpeckleMeshSettings.CurrentDoc, CheckOnClick = true
      };
      currentDocSetting.Click += (sender, args) =>
      {
        SpeckleGHSettings.MeshSettings = SpeckleMeshSettings.CurrentDoc;
        defaultSetting.Checked = false;
      };
      defaultSetting.Click += (sender, args) =>
      {
        SpeckleGHSettings.MeshSettings = SpeckleMeshSettings.Default;
        currentDocSetting.Checked = false;
      };
      var meshMenu = new ToolStripMenuItem("Select the default meshing parameters:");
      meshMenu.DropDown.Items.Add(defaultSetting);
      meshMenu.DropDown.Items.Add(currentDocSetting);
      
      KeepOpenOnDropdownCheck(meshMenu);
      speckleMenu.DropDown.Items.Add(meshMenu);
    }

    private void CreateSchemaConversionMenu()
    {
      var useSchemaTag = SpeckleGHSettings.UseSchemaTag;
      var schemaConversionHeader =
        speckleMenu.DropDown.Items.Add("Select the default Schema conversion option:") as ToolStripMenuItem;

      var objectItem = schemaConversionHeader.DropDown.Items.Add("Convert as Schema object.") as ToolStripMenuItem;
      objectItem.Checked = !useSchemaTag;

      var tagItem =
        schemaConversionHeader.DropDown.Items.Add($"Convert as geometry with 'Speckle Schema' attached") as
          ToolStripMenuItem;
      tagItem.Checked = useSchemaTag;
      tagItem.ToolTipText =
        "Enables Schema conversion while prioritizing the geometry over the schema.\n\nSchema information will e stored in a '@SpeckleSchema' property.";

      tagItem.Click += (s, args) =>
      {
        useSchemaTag = true;
        tagItem.Checked = useSchemaTag;
        objectItem.Checked = !useSchemaTag;
        SpeckleGHSettings.UseSchemaTag = useSchemaTag;
      };

      objectItem.Click += (s, args) =>
      {
        useSchemaTag = false;
        tagItem.Checked = useSchemaTag;
        objectItem.Checked = !useSchemaTag;
        SpeckleGHSettings.UseSchemaTag = useSchemaTag;
      };
      KeepOpenOnDropdownCheck(schemaConversionHeader);
    }
    
    public static void KeepOpenOnDropdownCheck (ToolStripMenuItem ctl)
    {
      foreach (var item in ctl.DropDownItems.OfType<ToolStripMenuItem>())
      {
        item.MouseEnter += (o, e) => ctl.DropDown.AutoClose = false;
        item.MouseLeave += (o, e) => ctl.DropDown.AutoClose = true;
      }

    }
  }
}
