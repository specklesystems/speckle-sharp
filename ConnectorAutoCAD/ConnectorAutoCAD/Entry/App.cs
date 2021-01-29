using Autodesk.AutoCAD.Runtime;
using Autodesk.Windows;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.ApplicationServices;
using System.Windows.Controls;
using System.Windows.Media;
using System.Reflection;
using System.IO;
using System.Windows.Media.Imaging;
using Speckle.ConnectorAutoCAD.UI;

namespace Speckle.ConnectorAutoCAD.Entry
{
  public class App : IExtensionApplication
  {
    public RibbonControl ribbon;

    #region Initializing and termination
    public void Initialize()
    {
      ribbon = ComponentManager.Ribbon;
      //throw new notimplementedexception(); 
      if (ribbon != null)
      {
        //the assembly was loaded using netload, so the ribbon
        //is available and we just create the ribbon
        Create();
      }
      else
      {
        //load the custom ribbon on startup, but at this point
        //the ribbon control is not available, so register for
        //an event and wait
        ComponentManager.ItemInitialized +=
            new System.EventHandler<RibbonItemEventArgs>
              (ComponentManager_ItemInitialized);

        Create();
        Application.SystemVariableChanged += TrapWSCurrentChange;
      }

      // set up bindings here? possible to subscribe to document events?
      SpeckleAutoCADCommand.Bindings = new ConnectorBindingsAutoCAD();
    }

    public void ComponentManager_ItemInitialized(object sender, RibbonItemEventArgs e)
    {
      // now one Ribbon item is initialized, but the Ribbon control
      // may not be available yet, so check if before
      ribbon = ComponentManager.Ribbon;
      if (ribbon != null)
      {
        // create Ribbon
        Create();
        // and remove the event handler
        ComponentManager.ItemInitialized -=
            new System.EventHandler<RibbonItemEventArgs>
              (ComponentManager_ItemInitialized);
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
      RibbonTab tab = FindOrMakeTab("Speckle Systems");
      if (tab == null)
        return;
      RibbonPanelSource panel = CreateButtonPannel("Speckle", tab);
      if (panel == null)
        return;
      RibbonButton button = CreateButton("2.0", "Speckle", "Speckle.ConnectorAutoCAD.Assets.logo32.png", panel);
    }


    public void Terminate()
    {
      //throw new NotImplementedException();
    }

    private RibbonTab FindOrMakeTab(string str)
    {
      // check to see if tab exists
      RibbonTab tab = ribbon.FindTab(str);

      // if not, create a new one
      if (tab == null)
      {
        tab = new RibbonTab();
        tab.Title = str;
        tab.Id = str;
        ribbon.Tabs.Add(tab);
        tab.IsActive = true; // optional: set ribbon tab active
      }
      return tab;
    }

    private RibbonPanelSource CreateButtonPannel(string name, RibbonTab tab)
    {
      RibbonPanelSource source = new RibbonPanelSource();
      source.Title = name;
      RibbonPanel panel = new RibbonPanel();
      panel.Source = source;
      tab.Panels.Add(panel);
      return source;
    }

    private RibbonButton CreateButton(string name, string CommandParameter, string pngFile, RibbonPanelSource source)
    {
      RibbonButton button = new RibbonButton();
      string path = typeof(App).Assembly.Location;

      // ribbon panel source info assignment
      button.Text = name;
      button.Id = name;
      button.CommandParameter = CommandParameter; // name of the command
      button.ShowImage = true;
      button.ShowText = true;
      button.Size = RibbonItemSize.Large;
      button.Orientation = Orientation.Vertical;
      button.Image = LoadPngImgSource("Speckle.ConnectorAutoCAD.Resources.logo16.png", path);
      button.LargeImage = LoadPngImgSource("Speckle.ConnectorAutoCAD.Resources.logo32.png", path);

      // add command to the button
      button.CommandHandler = new ButtonCommandHandler();

      // add ribbon button pannel to the ribbon pannel source
      source.Items.Add(button);
      return button;
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

    private ImageSource LoadPngImgSource(string sourceName, string path)
    {
      try
      {
        Assembly assembly = Assembly.GetExecutingAssembly();
        Stream stream = assembly.GetManifestResourceStream(sourceName);
        PngBitmapDecoder decoder = new PngBitmapDecoder(stream, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.Default);
        ImageSource source = decoder.Frames[0];
        return (source);
      }
      catch { }

      return null;
    }
  }
}