using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.Windows;
using Speckle.ConnectorAutocadCivil.UI;
using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Speckle.Core.Logging;
using Forms = System.Windows.Forms;

#if ADVANCESTEEL
using Autodesk.AdvanceSteel.Runtime;
#else
using Autodesk.AutoCAD.Runtime;
#endif

#if ADVANCESTEEL
[assembly: ExtensionApplication(typeof(Speckle.ConnectorAutocadCivil.Entry.App))]
#endif

namespace Speckle.ConnectorAutocadCivil.Entry;

public class App : IExtensionApplication
{
  public RibbonControl ribbon;

  #region Initializing and termination

  [SuppressMessage(
    "Design",
    "CA1031:Do not catch general exception types",
    Justification = "Is top level plugin catch"
  )]
  public void Initialize()
  {
    //Advance Steel addon is initialized after ribbon creation
    bool advanceSteel = false;
#if ADVANCESTEEL
    advanceSteel = true;
#endif
    ribbon = ComponentManager.Ribbon;
    try
    {
      if (ribbon != null && !advanceSteel) //the assembly was loaded using netload
      {
        Create();
      }
      else
      {
        // load the custom ribbon on startup, but wait for ribbon control to be created
        ComponentManager.ItemInitialized += new System.EventHandler<RibbonItemEventArgs>(
          ComponentManager_ItemInitialized
        );
        Application.SystemVariableChanged += TrapWSCurrentChange;
      }

      //Some dlls fail to load due to versions matching (0.10.7 vs 0.10.0)
      //the below should fix it! This affects Avalonia and Material
      AppDomain.CurrentDomain.AssemblyResolve += new ResolveEventHandler(OnAssemblyResolve);

      // DUI2
      var bindings = new ConnectorBindingsAutocad();
      Setup.Init(bindings.GetHostAppNameVersion(), bindings.GetHostAppName());
      SpeckleAutocadCommand.InitAvalonia();
      bindings.RegisterAppEvents();
      SpeckleAutocadCommand.Bindings = bindings;
    }
    catch (System.Exception ex)
    {
      SpeckleLog.Logger.Fatal(
        ex,
        "Add-in initialize context (true = application, false = doc): {isApplicationContext}",
        Application.DocumentManager.IsApplicationContext
      );
      Forms.MessageBox.Show(
        $"Add-in initialize context (true = application, false = doc): {Application.DocumentManager.IsApplicationContext.ToString()}. Error encountered: {ex}"
      );
    }
  }

  Assembly OnAssemblyResolve(object sender, ResolveEventArgs args)
  {
    Assembly a = null;
    var name = args.Name.Split(',')[0];
    string path = Path.GetDirectoryName(typeof(App).Assembly.Location);

    string assemblyFile = Path.Combine(path, name + ".dll");

    if (File.Exists(assemblyFile))
    {
      a = Assembly.LoadFrom(assemblyFile);
    }

    return a;
  }

  public void ComponentManager_ItemInitialized(object sender, RibbonItemEventArgs e)
  {
    // one Ribbon item is initialized, check for Ribbon control
    ribbon = ComponentManager.Ribbon;
    if (ribbon != null)
    {
      Create();
      // remove the event handler
      ComponentManager.ItemInitialized -= new System.EventHandler<RibbonItemEventArgs>(
        ComponentManager_ItemInitialized
      );
    }
  }

  // solving workspace changing
  public void TrapWSCurrentChange(object sender, SystemVariableChangedEventArgs e)
  {
    if (e.Name.Equals("WSCURRENT"))
    {
      Create();
    }
  }

  public void Create()
  {
    RibbonTab tab = FindOrMakeTab("Add-ins"); // add to Add-Ins tab
    if (tab == null)
    {
      return;
    }

    RibbonPanelSource panel = CreateButtonPanel("Speckle 2", tab);
    if (panel == null)
    {
      return;
    }

    RibbonToolTip speckleTip = CreateToolTip("Speckle", "Speckle Connector for " + Utils.AppName);
    RibbonToolTip oneClickTip = CreateToolTip(
      "Send",
      "Sends your selected objects to your account's document stream. If nothing is selected, sends everything in the document."
    );
    RibbonButton button = CreateButton("Connector " + Utils.AppName, "Speckle", panel, null, speckleTip, "logo");
    RibbonButton oneClickSendButton = CreateButton("Send", "SpeckleSend", panel, null, oneClickTip, "send");

    // help and resources buttons
    RibbonSplitButton helpButton =
      new()
      {
        Text = "Help & Resources",
        Image = LoadPngImgSource("help16.png"),
        LargeImage = LoadPngImgSource("help32.png"),
        ShowImage = true,
        ShowText = true,
        Size = RibbonItemSize.Large,
        Orientation = Orientation.Vertical
      };
    panel.Items.Add(helpButton);

    RibbonToolTip communityTip = CreateToolTip(
      "Community",
      "Check out our community forum! Opens a page in your web browser."
    );
    RibbonToolTip tutorialsTip = CreateToolTip(
      "Tutorials",
      "Check out our tutorials! Opens a page in your web browser"
    );
    RibbonToolTip docsTip = CreateToolTip("Docs", "Check out our documentation! Opens a page in your web browser");
    RibbonButton community = CreateButton("Community", "SpeckleCommunity", null, helpButton, communityTip, "forum");
    RibbonButton tutorials = CreateButton("Tutorials", "SpeckleTutorials", null, helpButton, tutorialsTip, "tutorials");
    RibbonButton docs = CreateButton("Docs", "SpeckleDocs", null, helpButton, docsTip, "docs");
  }

  public void Terminate() { }

  private RibbonTab FindOrMakeTab(string name)
  {
    // check to see if tab exists
    RibbonTab tab = ribbon.Tabs.FirstOrDefault(o => o.Title.Equals(name));

    // if not, create a new one
    if (tab == null)
    {
      tab = new RibbonTab { Title = name, Id = name };
      ribbon.Tabs.Add(tab);
    }

#if !ADVANCESTEEL
    tab.IsActive = true; // optional debug: set ribbon tab active
#endif
    return tab;
  }

  private RibbonPanelSource CreateButtonPanel(string name, RibbonTab tab)
  {
    var source = new RibbonPanelSource() { Title = name };
    var panel = new RibbonPanel() { Source = source };
    tab.Panels.Add(panel);
    return source;
  }

  private RibbonToolTip CreateToolTip(string title, string content)
  {
    RibbonToolTip toolTip =
      new()
      {
        //toolTip.Command = "";
        Title = title,
        Content = content,
        IsHelpEnabled = true // Without this "Press F1 for help" does not appear in the tooltip
      };

    return toolTip;
  }

  private RibbonButton CreateButton(
    string name,
    string CommandParameter,
    RibbonPanelSource sourcePanel = null,
    RibbonSplitButton sourceButton = null,
    RibbonToolTip tooltip = null,
    string imageName = ""
  )
  {
    var button = new RibbonButton
    {
      // ribbon panel source info assignment
      Text = name,
      Id = name,
      ShowImage = true,
      ShowText = true,
      ToolTip = tooltip,
      HelpSource = new System.Uri("https://speckle.guide/user/autocadcivil.html"),
      Size = RibbonItemSize.Large,
      Image = LoadPngImgSource(imageName + "16.png"),
      LargeImage = LoadPngImgSource(imageName + "32.png")
    };

    // add ribbon button pannel to the ribbon panel source
    if (sourcePanel != null)
    {
      button.Orientation = Orientation.Vertical;
      button.CommandParameter = CommandParameter;
      button.CommandHandler = new ButtonCommandHandler(CommandParameter);
      sourcePanel.Items.Add(button);
    }
    else if (sourceButton != null)
    {
      button.Orientation = Orientation.Horizontal;
      button.CommandParameter = CommandParameter;
      button.CommandHandler = new ButtonCommandHandler(CommandParameter);
      sourceButton.Items.Add(button);
    }
    return button;
  }

  /// <summary>
  /// Retrieve the png image source
  /// </summary>
  /// <param name="sourceName"></param>
  /// <returns></returns>
  private ImageSource LoadPngImgSource(string sourceName)
  {
    if (!string.IsNullOrEmpty(sourceName) && sourceName.ToLower().EndsWith(".png"))
    {
      Assembly assembly = Assembly.GetExecutingAssembly();
      string resource = GetType().Assembly
        .GetManifestResourceNames()
        .Where(o => o.EndsWith(sourceName))
        .FirstOrDefault();

      if (string.IsNullOrEmpty(resource))
      {
        return null;
      }

      Stream stream = null;
      try
      {
        stream = assembly.GetManifestResourceStream(resource);
      }
      catch (FileLoadException flEx)
      {
        SpeckleLog.Logger.Error(flEx, "Could not load app image source: {exceptionMessage}");
      }
      catch (FileNotFoundException fnfEx)
      {
        SpeckleLog.Logger.Error(fnfEx, "Could not find app image source: {exceptionMessage}");
      }
      catch (NotImplementedException niEx)
      {
        SpeckleLog.Logger.Error(niEx, "App image source could not be loaded: {exceptionMessage}");
      }

      if (stream is not null)
      {
        PngBitmapDecoder decoder = new(stream, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.Default);
        if (decoder.Frames.Count > 0)
        {
          ImageSource source = decoder.Frames[0];
          return source;
        }
      }
    }

    return null;
  }

  #endregion

  public class ButtonCommandHandler : System.Windows.Input.ICommand
  {
    private string commandParameter;

    public ButtonCommandHandler(string commandParameter)
    {
      this.commandParameter = commandParameter;
    }

    public event System.EventHandler CanExecuteChanged;

    public void Execute(object parameter)
    {
      if (parameter is RibbonButton)
      {
        switch (commandParameter)
        {
          case "Speckle":
            SpeckleAutocadCommand.SpeckleCommand();
            break;
          case "SpeckleCommunity":
            SpeckleAutocadCommand.SpeckleCommunity();
            break;
          case "SpeckleTutorials":
            SpeckleAutocadCommand.SpeckleTutorials();
            break;
          case "SpeckleDocs":
            SpeckleAutocadCommand.SpeckleDocs();
            break;
          default:
            break;
        }
      }
    }

    public bool CanExecute(object parameter) => true;
  }
}
