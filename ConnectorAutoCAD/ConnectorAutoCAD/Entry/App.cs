using Autodesk.AutoCAD.Runtime;
using Autodesk.Windows;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.ApplicationServices;
using System.Windows.Controls;
using System.Windows.Media;
using System.Reflection;
using System.IO;
using System.Windows.Media.Imaging;

namespace Speckle.ConnectorAutoCAD.Entry
{
  public class App : IExtensionApplication
  {

    #region Initializing and termination
    public void Initialize()
    {
      //throw new notimplementedexception(); 
      if (ComponentManager.Ribbon == null)
      {
        //load the custom ribbon on startup, but at this point
        //the ribbon control is not available, so register for
        //an event and wait
        ComponentManager.ItemInitialized +=
            new System.EventHandler<RibbonItemEventArgs>
              (ComponentManager_ItemInitialized);
      }
      else
      {
        //the assembly was loaded using netload, so the ribbon
        //is available and we just create the ribbon
        Create();
      }
      Create();
      Application.SystemVariableChanged += TrapWSCurrentChange;
    }

    private void ComponentManager_ItemInitialized(object sender, RibbonItemEventArgs e)
    {
      //now one Ribbon item is initialized, but the Ribbon control
      //may not be available yet, so check if before
      if (ComponentManager.Ribbon != null)
      {
        //create Ribbon
        Create();
        //and remove the event handler
        ComponentManager.ItemInitialized -=
            new System.EventHandler<RibbonItemEventArgs>
              (ComponentManager_ItemInitialized);
      }
    }

    //solving workspace changing
    public void TrapWSCurrentChange(object sender, SystemVariableChangedEventArgs e)
    {
      if (e.Name.Equals("WSCURRENT"))
        Create();
    }

    public void Create()
    {
      RibbonTab tab = FindOrMakeTab("Add-ins");
      if (tab == null)
        return;
      RibbonPanelSource panel = CreateButtonPannel("Speckle", tab);
      if (panel == null)
        return;
      RibbonButton button = CreateButton("Speckle", "Speckle", "Speckle.ConnectorAutoCAD.Assets.logo32.png", panel);
    }


    public void Terminate()
    {
      //throw new NotImplementedException();
    }

    private RibbonTab FindOrMakeTab(string str)
    {
      // Create ribbon control
      RibbonControl rc = ComponentManager.Ribbon;

      // check to see if tab exists
      RibbonTab rt = rc.FindTab(str);

      // if not, create a new one
      if (rt == null)
      {
        rt = new RibbonTab();
        rt.Title = str;
        rt.Id = str;
        rc.Tabs.Add(rt);
        rt.IsActive = true; // optional: set ribbon tab active
      }
      return rt;
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
      button.LargeImage = LoadPngImgSource("Speckle.ConnectorAutoCAD.Assets.logo32.png", path); ;

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

      public void Execute(object parameter)
      {
        RibbonButton btn = parameter as RibbonButton;
        if (btn != null)
          Application.DocumentManager.MdiActiveDocument.SendStringToExecute(
            (string)btn.CommandParameter, true, false, true);
      }

      public bool CanExecute(object parameter)
      {
        return true;
      }
    }

    private ImageSource LoadPngImgSource(string sourceName, string path)
    {
      try
      {
        var assembly = Assembly.LoadFrom(Path.Combine(path));
        var icon = assembly.GetManifestResourceStream(sourceName);
        PngBitmapDecoder m_decoder = new PngBitmapDecoder(icon, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.Default);
        ImageSource m_source = m_decoder.Frames[0];
        return (m_source);
      }
      catch { }

      return null;
    }
  }
}