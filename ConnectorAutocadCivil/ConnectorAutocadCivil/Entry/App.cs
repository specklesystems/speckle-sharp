using System.Windows.Controls;
using System.Windows.Media;
using System.Reflection;
using System.IO;
using System.Windows.Media.Imaging;
using System.Linq;

using Speckle.ConnectorAutocadCivil.UI;

using Autodesk.AutoCAD.Runtime;
using Autodesk.Windows;
using Autodesk.AutoCAD.ApplicationServices;

namespace Speckle.ConnectorAutocadCivil.Entry
{
  public class App : IExtensionApplication
  {
    public RibbonControl ribbon;

    #region Initializing and termination
    public void Initialize()
    {
      ribbon = ComponentManager.Ribbon;
      if (ribbon != null) //the assembly was loaded using netload
      {
        Create();
      }
      else
      {
        // load the custom ribbon on startup, but wait for ribbon control to be created
        ComponentManager.ItemInitialized += new System.EventHandler<RibbonItemEventArgs>(ComponentManager_ItemInitialized);
        Create();
        Application.SystemVariableChanged += TrapWSCurrentChange;
      }

      // set up bindings here? possible to subscribe to document events?
      SpeckleAutocadCommand.Bindings = new ConnectorBindingsAutocad();
      SpeckleAutocadCommand.Bindings.SetExecutorAndInit();
    }

    public void ComponentManager_ItemInitialized(object sender, RibbonItemEventArgs e)
    {
      // one Ribbon item is initialized, check for Ribbon control
      ribbon = ComponentManager.Ribbon;
      if (ribbon != null)
      {
        Create();
        // remove the event handler
        ComponentManager.ItemInitialized -= new System.EventHandler<RibbonItemEventArgs>(ComponentManager_ItemInitialized);
      }
    }

    // solving workspace changing
    public void TrapWSCurrentChange(object sender, SystemVariableChangedEventArgs e)
    {
      if (e.Name.Equals("WSCURRENT"))
        Create();
    }

    public void Create()
    {
      RibbonTab tab = FindOrMakeTab("Add-ins"); // add to Add-Ins tab
      if (tab == null)
        return;
      RibbonPanelSource panel = CreateButtonPanel("Speckle 2", tab);
      if (panel == null)
        return;
      RibbonButton button = CreateButton("Connector", "Speckle", panel);
    }

    public void Terminate()
    {
    }

    private RibbonTab FindOrMakeTab(string name)
    {
      // check to see if tab exists
      RibbonTab tab = ribbon.Tabs.Where(o => o.Title.Equals(name)).FirstOrDefault();

      // if not, create a new one
      if (tab == null)
      {
        tab = new RibbonTab();
        tab.Title = name;
        tab.Id = name;
        ribbon.Tabs.Add(tab);
      }

      tab.IsActive = true; // optional debug: set ribbon tab active
      return tab;
    }

    private RibbonPanelSource CreateButtonPanel(string name, RibbonTab tab)
    {
      var source = new RibbonPanelSource() { Title = name };
      var panel = new RibbonPanel() { Source = source };
      tab.Panels.Add(panel);
      return source;
    }

    private RibbonButton CreateButton(string name, string CommandParameter, RibbonPanelSource source)
    {
      var button = new RibbonButton();

      // ribbon panel source info assignment
      button.Text = name;
      button.Id = name;
      button.CommandParameter = CommandParameter;
      button.ShowImage = true;
      button.ShowText = true;
      button.ToolTip = "Speckle Connector for AutoCAD Civil3D";
      button.Size = RibbonItemSize.Large;
      button.Orientation = Orientation.Vertical;
      button.Image = LoadPngImgSource("Speckle.ConnectorAutoCAD.Resources.logo16.png");
      button.LargeImage = LoadPngImgSource("Speckle.ConnectorAutoCAD.Resources.logo32.png");

      // add command to the button
      button.CommandHandler = new ButtonCommandHandler();

      // add ribbon button pannel to the ribbon panel source
      source.Items.Add(button);
      return button;
    }

    private ImageSource LoadPngImgSource(string sourceName)
    {
      try
      {
        Assembly assembly = Assembly.GetExecutingAssembly();
        Stream stream = assembly.GetManifestResourceStream(sourceName);
        PngBitmapDecoder decoder = new PngBitmapDecoder(stream, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.Default);
        ImageSource source = decoder.Frames[0];
        return source;
      }
      catch { }
      return null;
    }

    #endregion 

    public class ButtonCommandHandler : System.Windows.Input.ICommand
    {
      public event System.EventHandler CanExecuteChanged;

      // the command parameter includes an extra space at the end to simulate pressing "enter"
      public void Execute(object parameter)
      {
        RibbonButton btn = parameter as RibbonButton;
        if (btn != null)
          Application.DocumentManager.MdiActiveDocument.SendStringToExecute(
            (string)btn.CommandParameter + " ", true, false, true);
      }

      public bool CanExecute(object parameter) => true;
    }
  }
}