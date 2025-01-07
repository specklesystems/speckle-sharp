using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using ConnectorGrasshopper.Extras;
using ConnectorGrasshopper.Properties;
using Grasshopper;
using Grasshopper.GUI.Canvas;
using Grasshopper.Kernel;
using Rhino;
using Speckle.Core.Helpers;
using Speckle.Core.Kits;
using Speckle.Core.Logging;
using Speckle.Core.Models.Extensions;

namespace ConnectorGrasshopper;

public static class KeyWatcher
{
  public static bool TabPressed;
}

public class Loader : GH_AssemblyPriority
{
  private static RhinoDoc _headlessDoc;

  private DebounceDispatcher headlessTemplateNameDebounceDispatcher = new();
  private IEnumerable<ToolStripItem> kitMenuItems;

  public IEnumerable<ISpeckleKit> loadedKits;
  public bool MenuHasBeenAdded;
  public ISpeckleKit selectedKit;
  private ToolStripMenuItem speckleMenu;

  public override GH_LoadingInstruction PriorityLoad()
  {
    const bool ENHANCED_LOG_CONTEXT =
#if MAC
      false;
#else
      true;
#endif
    var logConfig = new SpeckleLogConfiguration(logToSentry: false, enhancedLogContext: ENHANCED_LOG_CONTEXT);

    // We initialise with Rhino values. Grasshopper will use it's own tracking class that will override said values in all calls.
    Setup.Init(GetRhinoHostAppVersion(), HostApplications.Rhino.Slug, logConfig);

    Instances.CanvasCreated += OnCanvasCreated;

#if RHINO7_OR_GREATER
    if (Instances.RunningHeadless)
    {
      // If GH is running headless, we listen for document added/removed events.
      Instances.DocumentServer.DocumentAdded += OnDocumentAdded;
      Instances.DocumentServer.DocumentRemoved += OnDocumentRemoved;
    }
#endif

    Instances.ComponentServer.AddCategoryIcon(ComponentCategories.PRIMARY_RIBBON, Resources.speckle_logo);
    Instances.ComponentServer.AddCategorySymbolName(ComponentCategories.PRIMARY_RIBBON, 'S');
    Instances.ComponentServer.AddCategoryIcon(ComponentCategories.SECONDARY_RIBBON, Resources.speckle_logo);
    Instances.ComponentServer.AddCategorySymbolName(ComponentCategories.SECONDARY_RIBBON, 'S');
    return GH_LoadingInstruction.Proceed;
  }

  public static string GetRhinoHostAppVersion() =>
    RhinoApp.Version.Major switch
    {
      6 => HostApplications.Rhino.GetVersion(HostAppVersion.v6),
      7 => HostApplications.Rhino.GetVersion(HostAppVersion.v7),
      8 => HostApplications.Rhino.GetVersion(HostAppVersion.v8),
      _ => throw new NotSupportedException($"Version {RhinoApp.Version.Major} of Rhino is not supported"),
    };

  public static string GetGrasshopperHostAppVersion() =>
    RhinoApp.Version.Major switch
    {
      6 => HostApplications.Grasshopper.GetVersion(HostAppVersion.v6),
      7 => HostApplications.Grasshopper.GetVersion(HostAppVersion.v7),
      8 => HostApplications.Grasshopper.GetVersion(HostAppVersion.v8),
      _ => throw new NotSupportedException($"Version {RhinoApp.Version.Major} of Rhino is not supported"),
    };

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
    Instances.DocumentEditor.Load += OnDocumentEditorLoad;

    if (canvas == null)
    {
      return;
    }

    canvas.KeyDown += (s, e) =>
    {
      if (e.KeyCode == Keys.Tab && !KeyWatcher.TabPressed)
      {
        KeyWatcher.TabPressed = true;
      }
    };

    canvas.KeyUp += (s, e) =>
    {
      if (KeyWatcher.TabPressed && e.KeyCode == Keys.Tab)
      {
        KeyWatcher.TabPressed = false;
      }
    };
  }

  private void OnDocumentEditorLoad(object sender, EventArgs e)
  {
    try
    {
      var mainMenu = Instances.DocumentEditor.MainMenuStrip;
      AddSpeckleMenu(mainMenu);
    }
    catch (Exception ex) when (!ex.IsFatal())
    {
      ShowLoadErrorMessageBox();
    }
  }

  private static DialogResult ShowLoadErrorMessageBox()
  {
    return MessageBox.Show(
      "There was a problem setting up Speckle\n"
        + "This can be caused by \n\n"
        + "- A corrupted install\n"
        + "- Another Grasshopper plugin using an older version of Speckle\n"
        + "- Having an older version of the Rhino connector installed\n"
        + "Try reinstalling both Rhino and Grasshopper connectors.\n\n"
        + "If the problem persists, please reach out to our Community Forum (https://speckle.community)",
      "Speckle Error",
      MessageBoxButtons.OK
    );
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
      {
        menuItem.CheckState = clickedItem.Text.Trim() == selectedKit.Name ? CheckState.Checked : CheckState.Unchecked;
      }
    }
  }

  private void AddSpeckleMenu(MenuStrip mainMenu)
  {
    if (MenuHasBeenAdded)
    {
      return;
    }
    // Double check that the menu does not exist.

    var menuName = "Speckle 2";
    if (mainMenu.Items.ContainsKey(menuName))
    {
      mainMenu.Items.RemoveByKey(menuName);
    }

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
    speckleMenu.DropDown.Items.Add(
      "Community Forum",
      Resources.forum16,
      (o, args) => Open.Url("https://speckle.community/tag/grasshopper")
    );
    speckleMenu.DropDown.Items.Add(
      "Tutorials",
      Resources.tutorials16,
      (o, args) => Open.Url("https://v1.speckle.systems/tag/grasshopper/")
    );
    speckleMenu.DropDown.Items.Add(
      "Docs",
      Resources.docs16,
      (o, args) => Open.Url("https://speckle.guide/user/grasshopper.html")
    );

    speckleMenu.DropDown.Items.Add(new ToolStripSeparator());

    // Manager button
    speckleMenu.DropDown.Items.Add(
      "Open Speckle Manager",
      Resources.speckle_logo,
      (o, args) =>
      {
        try
        {
          string path = "";

          Speckle.Core.Logging.Analytics.TrackEvent(
            Speckle.Core.Logging.Analytics.Events.DUIAction,
            new Dictionary<string, object> { { "name", "Launch Manager" } }
          );

#if MAC
          path = @"/Applications/Manager for Speckle.app";

#else
          path = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Speckle",
            "Manager",
            "Manager.exe"
          );
#endif

          if (File.Exists(path) || Directory.Exists(path))
          {
            Open.File(path);
          }
          else
          {
            Open.Url("https://speckle.systems/download");
          }
        }
        catch (Exception ex) when (!ex.IsFatal())
        {
          SpeckleLog.Logger.Fatal(
            ex,
            "Swallowing exception in {methodName}: {exceptionMessage}",
            nameof(AddSpeckleMenu),
            ex.Message
          );
        }
      }
    );

    if (!MenuHasBeenAdded)
    {
      mainMenu.Items.Add(speckleMenu);
    }

    MenuHasBeenAdded = true;
  }

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
      headlessTemplateNameDebounceDispatcher.Debounce(
        400,
        o => SpeckleGHSettings.HeadlessTemplateFilename = textbox.Text
      );
    main.DropDown.Items.Add(new ToolStripSeparator());
    main.DropDown.Items.Add(
      "Open Templates folder",
      null,
      (sender, args) =>
      {
        var path = Path.Combine(SpecklePathProvider.InstallSpeckleFolderPath, "Templates");

        if (!Directory.Exists(path))
        {
          Directory.CreateDirectory(path);
        }
#if MAC
        Open.File("file://" + path);
#else
        Open.File("explorer.exe", "/select, " + path);
#endif
      }
    );
  }

  private void CreateKitSelectionMenu(ToolStripMenuItem menu)
  {
    var header = new ToolStripMenuItem("Select the converter you want to use.") { Enabled = false };
    var loading = new ToolStripMenuItem("   Loading Kits...") { Enabled = false };

    menu.DropDown.Items.Add(header);
    menu.DropDown.Items.Add(loading);

    Task.Run(() =>
      {
        loadedKits = KitManager.GetKitsWithConvertersForApp(Utilities.GetVersionedAppName());

        var kitItems = new List<ToolStripItem>();

        loadedKits
          .ToList()
          .ForEach(kit =>
          {
            var item = new ToolStripMenuItem("  " + kit.Name);
            item.Click += HandleKitSelectedEvent;
            kitItems.Add(item);
          });
        kitMenuItems = kitItems;
      })
      .ContinueWith(task =>
      {
        if (task.Exception != null)
        {
          SpeckleLog.Logger.Error(task.Exception, "An exception occurred while fetching Kits");
          var errItem = new ToolStripMenuItem("An error occurred while fetching Kits");
          errItem.DropDown.Items.Add(task.Exception.ToFormattedString());

          RhinoApp.InvokeOnUiThread(
            (Action)
              delegate
              {
                menu.DropDown.Items.Remove(loading);
                menu.DropDown.Items.Insert(1, errItem);
                Instances.DocumentEditor.Refresh();
              }
          );
          return;
        }

        RhinoApp.InvokeOnUiThread(
          (Action)
            delegate
            {
              var current = 1;
              menu.DropDown.Items.Remove(loading);
              foreach (var item in kitMenuItems)
              {
                menu.DropDown.Items.Insert(current++, item);
              }

              HandleKitSelectedEvent(kitMenuItems.FirstOrDefault(k => k.Text.Trim() == "Objects"), null);
              Instances.DocumentEditor.Refresh();
            }
        );
      });
  }

  private void CreateTabsMenu()
  {
    var tabsMenu = speckleMenu.DropDown.Items.Add("Show/Hide Components") as ToolStripMenuItem;
    new List<string> { "BIM", "Revit", "Structural", "GSA", "Tekla", "CSI", "Archicad", "Advance Steel" }.ForEach(s =>
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

    var showDevItem = new ToolStripMenuItem(
      "Show Developer components",
      null,
      (o, args) =>
      {
        SpeckleGHSettings.ShowDevComponents = !SpeckleGHSettings.ShowDevComponents;
      }
    );
    showDevItem.Checked = SpeckleGHSettings.ShowDevComponents;
    showDevItem.CheckOnClick = true;
    tabsMenu.DropDown.Items.Add(showDevItem);
    KeepOpenOnDropdownCheck(tabsMenu);
  }

  private void CreateMeshingSettingsMenu()
  {
    var defaultSetting = new ToolStripMenuItem("Default")
    {
      Checked = SpeckleGHSettings.MeshSettings == SpeckleMeshSettings.Default,
      CheckOnClick = true
    };

    var currentDocSetting = new ToolStripMenuItem("Current Rhino doc")
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
      schemaConversionHeader.DropDown.Items.Add("Convert as geometry with 'Speckle Schema' attached")
      as ToolStripMenuItem;
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

  public static void DisposeHeadlessDoc()
  {
#if RHINO7_OR_GREATER
    _headlessDoc?.Dispose();
#endif
    _headlessDoc = null;
  }

  public static void SetupHeadlessDoc()
  {
#if RHINO7_OR_GREATER
    // var templatePath = Path.Combine(Helpers.UserApplicationDataPath, "Speckle", "Templates",
    //   SpeckleGHSettings.HeadlessTemplateFilename);
    // Console.WriteLine($"Setting up doc. Looking for '{templatePath}'");
    // _headlessDoc = File.Exists(templatePath)
    //   ? RhinoDoc.CreateHeadless(templatePath)
    //   : RhinoDoc.CreateHeadless(null);

    _headlessDoc = RhinoDoc.CreateHeadless(null);
    Console.WriteLine(
      $"Speckle - Backup headless doc is ready: '{_headlessDoc.Name ?? "Untitled"}'\n    with template: '{_headlessDoc.TemplateFileUsed ?? "No template"}'\n    with units: {_headlessDoc.ModelUnitSystem}"
    );
    Console.WriteLine(
      "Speckle - To modify the units in a headless run, you can override the 'RhinoDoc.ActiveDoc' in the '.gh' file using a c#/python script."
    );
#endif
  }

  /// <summary>
  /// Get the current document for this Grasshopper instance.
  /// This will correspond to the `ActiveDoc` on normal Rhino usage, while in headless mode it will try to load
  /// </summary>
  /// <returns></returns>
  public static RhinoDoc GetCurrentDocument()
  {
#if RHINO7_OR_GREATER
    if (Instances.RunningHeadless && RhinoDoc.ActiveDoc == null && _headlessDoc != null)
    {
      // Running headless, with no ActiveDoc override and _headlessDoc was correctly initialised.
      // Only time the _headlessDoc is not set is upon document opening, where the components will
      // check for this as their normal initialisation routine, but the document will be refreshed on every solution run.
      Console.WriteLine(
        $"Speckle - Fetching headless doc '{_headlessDoc?.Name ?? "Untitled"}'\n    with template: '{_headlessDoc.TemplateFileUsed ?? "No template"}'"
      );
      Console.WriteLine("    Model units:" + _headlessDoc.ModelUnitSystem);
      return _headlessDoc;
    }

    return RhinoDoc.ActiveDoc;
#else
    return RhinoDoc.ActiveDoc;
#endif
  }
}
