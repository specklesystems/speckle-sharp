using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Timers;
using System.Windows.Forms;
using ConnectorGrasshopper.Extras;
using Grasshopper;
using Grasshopper.GUI.Canvas;
using Grasshopper.Kernel;
using Rhino;
using Serilog;
using Speckle.Core.Api;
using Speckle.Core.Helpers;
using Speckle.Core.Kits;
using Speckle.Core.Logging;
using Speckle.Core.Models.Extensions;

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
      var version = HostApplications.Grasshopper.GetVersion(HostAppVersion.v6);
      if (RhinoApp.Version.Major == 7)
        version = HostApplications.Grasshopper.GetVersion(HostAppVersion.v7);

      var logConfig = new SpeckleLogConfiguration(logToSentry: false);
#if MAC
      logConfig.enhancedLogContext = false;
#endif
      SpeckleLog.Initialize(HostApplications.Grasshopper.Name, version, logConfig);
      try
      {
        // Using reflection instead of calling `Setup.Init` to prevent loader from exploding. See comment on Catch clause.
        typeof(Setup).GetMethod("Init", BindingFlags.Public | BindingFlags.Static)
          .Invoke(null, new object[] { version, HostApplications.Grasshopper.Slug });
      }
      catch (Exception e)
      {
        // This is here to ensure that other older versions of core (which did not have the Setup class) don't bork our connector initialisation.
        // The only way this can happen right now is if a 3rd party plugin includes the Core dll in their distribution (which they shouldn't ever do).
        // Recommended practice is to assume that our connector would be installed alongside theirs.
        SpeckleLog.Logger.Error(e, e.Message);
      }

      Grasshopper.Instances.CanvasCreated += OnCanvasCreated;
#if RHINO7
      if (Grasshopper.Instances.RunningHeadless)
      {
        // If GH is running headless, we listen for document added/removed events.
        Grasshopper.Instances.DocumentServer.DocumentAdded += OnDocumentAdded;
        Grasshopper.Instances.DocumentServer.DocumentRemoved += OnDocumentRemoved;
      }
#endif


      Grasshopper.Instances.ComponentServer.AddCategoryIcon(ComponentCategories.PRIMARY_RIBBON,
        Properties.Resources.speckle_logo);
      Grasshopper.Instances.ComponentServer.AddCategorySymbolName(ComponentCategories.PRIMARY_RIBBON, 'S');
      Grasshopper.Instances.ComponentServer.AddCategoryIcon(ComponentCategories.SECONDARY_RIBBON,
        Properties.Resources.speckle_logo);
      Grasshopper.Instances.ComponentServer.AddCategorySymbolName(ComponentCategories.SECONDARY_RIBBON, 'S');
      return GH_LoadingInstruction.Proceed;
    }

    private void OnDocumentAdded(GH_DocumentServer sender, GH_Document doc)
    {
      // Add events for solution start and end
      doc.SolutionStart += DocumentOnSolutionStart;
      doc.SolutionEnd += DocumentOnSolutionEnd;
    }

    private void OnDocumentRemoved(GH_DocumentServer sender, GH_Document doc)
    {
      // Remove events for solution start and end
      doc.SolutionStart -= DocumentOnSolutionStart;
      doc.SolutionEnd -= DocumentOnSolutionEnd;
    }

    private void DocumentOnSolutionStart(object sender, GH_SolutionEventArgs e)
    {
      SetupHeadlessDoc();
    }

    private void DocumentOnSolutionEnd(object sender, GH_SolutionEventArgs e)
    {
      DisposeHeadlessDoc();
    }

    private void OnCanvasCreated(GH_Canvas canvas)
    {
      Grasshopper.Instances.DocumentEditor.Load += OnDocumentEditorLoad;

      if (canvas == null) return;

      canvas.KeyDown += (s, e) =>
      {
        if (e.KeyCode == Keys.Tab && !KeyWatcher.TabPressed)
          KeyWatcher.TabPressed = true;
      };

      canvas.KeyUp += (s, e) =>
      {
        if (KeyWatcher.TabPressed && e.KeyCode == Keys.Tab)
          KeyWatcher.TabPressed = false;
      };
    }

    private void OnDocumentEditorLoad(object sender, EventArgs e)
    {
      try
      {
        var mainMenu = Grasshopper.Instances.DocumentEditor.MainMenuStrip;
        AddSpeckleMenu(mainMenu);
      }
      catch (Exception ex)
      {
        ShowLoadErrorMessageBox();
      }
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

    private void AddSpeckleMenu(MenuStrip mainMenu)
    {
      if (MenuHasBeenAdded) return;
      // Double check that the menu does not exist.

      var menuName = "Speckle 2";
      if (mainMenu.Items.ContainsKey(menuName))
        mainMenu.Items.RemoveByKey(menuName);

      speckleMenu = new ToolStripMenuItem(menuName);

      CreateKitSelectionMenu(speckleMenu);
      speckleMenu.DropDown.Items.Add(new ToolStripSeparator());
      CreateSchemaConversionMenu();
      speckleMenu.DropDown.Items.Add(new ToolStripSeparator());
      CreateMeshingSettingsMenu();
      // speckleMenu.DropDown.Items.Add(new ToolStripSeparator());
      // CreateHeadlessTemplateMenu();
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
        (o, args) =>
        {
          try
          {
            string path = "";

            Speckle.Core.Logging.Analytics.TrackEvent(Speckle.Core.Logging.Analytics.Events.DUIAction,
              new Dictionary<string, object>() { { "name", "Launch Manager" } });

#if MAC
            path = @"/Applications/Manager for Speckle.app";

#else
            path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Speckle",
              "Manager", "Manager.exe");
#endif

            if (File.Exists(path) || Directory.Exists(path))
              Process.Start(path);
            else
            {
              Process.Start(new ProcessStartInfo($"https://speckle.systems/download") { UseShellExecute = true });
            }
          }
          catch (Exception e)
          {
            SpeckleLog.Logger.Error(e, e.Message);
          }
        });


      if (!MenuHasBeenAdded)
        mainMenu.Items.Add(speckleMenu);

      MenuHasBeenAdded = true;
    }

    private DebounceDispatcher headlessTemplateNameDebounceDispatcher = new DebounceDispatcher();

    private void CreateHeadlessTemplateMenu()
    {
      var main = speckleMenu.DropDown.Items.Add("Rhino.Compute") as ToolStripMenuItem;
      var head = main.DropDown.Items.Add("Default 3dm filename");
      head.Enabled = false;
      head.ToolTipText =
        @"The file name of the default file to be used when running on Rhino.Compute. They should be placed in `%appdata%/Speckle/Templates/. If no file is found with that name, an empty file will be opened";
      var textbox = new ToolStripTextBox("Default file name");
      main.DropDown.Items.Add(textbox);
      textbox.Text = SpeckleGHSettings.HeadlessTemplateFilename;
      textbox.AutoSize = false;
      textbox.Width = Global_Proc.UiAdjust(200);
      textbox.TextChanged += (sender, text) =>
        headlessTemplateNameDebounceDispatcher.Debounce(400,
          o => SpeckleGHSettings.HeadlessTemplateFilename = textbox.Text);
      main.DropDown.Items.Add(new ToolStripSeparator());
      main.DropDown.Items.Add("Open Templates folder", null, (sender, args) =>
      {
        var path = Path.Combine(SpecklePathProvider.InstallSpeckleFolderPath, "Templates");

        if (!Directory.Exists(path))
          Directory.CreateDirectory(path);
#if MAC
          Process.Start("file://" + path);
#else
        Process.Start("explorer.exe", "/select, " + path);
#endif
      });
    }

    private void CreateKitSelectionMenu(ToolStripMenuItem menu)
    {
      var header = new ToolStripMenuItem("Select the converter you want to use.") { Enabled = false };
      var loading = new ToolStripMenuItem("   Loading Kits...") { Enabled = false };

      menu.DropDown.Items.Add(header);
      menu.DropDown.Items.Add(loading);

      Task.Run(() =>
      {
        loadedKits = KitManager.GetKitsWithConvertersForApp(Extras.Utilities.GetVersionedAppName());

        var kitItems = new List<ToolStripItem>();

        loadedKits.ToList().ForEach(kit =>
        {
          var item = new ToolStripMenuItem("  " + kit.Name);
          item.Click += HandleKitSelectedEvent;
          kitItems.Add(item);
        });
        kitMenuItems = kitItems;
      }).ContinueWith(task =>
      {
        if (task.Exception != null)
        {
          SpeckleLog.Logger.Error(task.Exception, task.Exception.Message);
          var errItem = new ToolStripMenuItem("An error occurred while fetching Kits");
          errItem.DropDown.Items.Add(task.Exception.ToFormattedString());

          RhinoApp.InvokeOnUiThread((Action)delegate
          {
            menu.DropDown.Items.Remove(loading);
            menu.DropDown.Items.Insert(1, errItem);
            Grasshopper.Instances.DocumentEditor.Refresh();
          });
          return;
        }

        RhinoApp.InvokeOnUiThread((Action)delegate
        {
          var current = 1;
          menu.DropDown.Items.Remove(loading);
          foreach (var item in kitMenuItems)
            menu.DropDown.Items.Insert(current++, item);
          HandleKitSelectedEvent(kitMenuItems.FirstOrDefault(k => k.Text.Trim() == "Objects"), null);
          Grasshopper.Instances.DocumentEditor.Refresh();
        });
      });
    }

    private void CreateTabsMenu()
    {
      var tabsMenu = speckleMenu.DropDown.Items.Add("Show/Hide Components") as ToolStripMenuItem;
      new List<string>
      {
        "BIM",
        "Revit",
        "Structural",
        "GSA",
        "Tekla",
        "CSI",
        "Archicad",
        "Advance Steel"
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

      var showDevItem = new ToolStripMenuItem("Show Developer components", null,
        (o, args) => { SpeckleGHSettings.ShowDevComponents = !SpeckleGHSettings.ShowDevComponents; });
      showDevItem.Checked = SpeckleGHSettings.ShowDevComponents;
      showDevItem.CheckOnClick = true;
      tabsMenu.DropDown.Items.Add(showDevItem);
      KeepOpenOnDropdownCheck(tabsMenu);
    }

    private void CreateMeshingSettingsMenu()
    {
      var defaultSetting = new ToolStripMenuItem(
        "Default")
      { Checked = SpeckleGHSettings.MeshSettings == SpeckleMeshSettings.Default, CheckOnClick = true };

      var currentDocSetting = new ToolStripMenuItem(
        "Current Rhino doc")
      {
        Checked = SpeckleGHSettings.MeshSettings == SpeckleMeshSettings.CurrentDoc,
        CheckOnClick = true
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

    public static void KeepOpenOnDropdownCheck(ToolStripMenuItem ctl)
    {
      foreach (var item in ctl.DropDownItems.OfType<ToolStripMenuItem>())
      {
        item.MouseEnter += (o, e) => ctl.DropDown.AutoClose = false;
        item.MouseLeave += (o, e) => ctl.DropDown.AutoClose = true;
      }
    }

    private static RhinoDoc _headlessDoc;

    public static void DisposeHeadlessDoc()
    {
#if RHINO7
      _headlessDoc?.Dispose();
#endif
      _headlessDoc = null;
    }

    public static void SetupHeadlessDoc()
    {
#if RHINO7
      // var templatePath = Path.Combine(Helpers.UserApplicationDataPath, "Speckle", "Templates",
      //   SpeckleGHSettings.HeadlessTemplateFilename);
      // Console.WriteLine($"Setting up doc. Looking for '{templatePath}'");
      // _headlessDoc = File.Exists(templatePath)
      //   ? RhinoDoc.CreateHeadless(templatePath)
      //   : RhinoDoc.CreateHeadless(null);

      _headlessDoc = RhinoDoc.CreateHeadless(null);
      Console.WriteLine(
        $"Headless run with doc '{_headlessDoc.Name ?? "Untitled"}'\n    with template: '{_headlessDoc.TemplateFileUsed ?? "No template"}'\n    with units: {_headlessDoc.ModelUnitSystem}");
#endif
    }

    /// <summary>
    /// Get the current document for this Grasshopper instance.
    /// This will correspond to the `ActiveDoc` on normal Rhino usage, while in headless mode it will try to load
    /// </summary>
    /// <returns></returns>
    public static RhinoDoc GetCurrentDocument()
    {
#if RHINO7
      if (Instances.RunningHeadless && RhinoDoc.ActiveDoc == null)
      {
        Console.WriteLine(
          $"Fetching headless doc '{_headlessDoc.Name ?? "Untitled"}'\n    with template: '{_headlessDoc.TemplateFileUsed ?? "No template"}'");
        Console.WriteLine("    Model units:" + _headlessDoc.ModelUnitSystem);
        return _headlessDoc;
      }
      return RhinoDoc.ActiveDoc;
#else
      return RhinoDoc.ActiveDoc;
#endif
    }
  }
}
