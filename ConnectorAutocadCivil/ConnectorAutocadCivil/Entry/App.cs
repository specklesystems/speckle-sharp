﻿using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.Windows;
using Speckle.ConnectorAutocadCivil.UI;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Forms = System.Windows.Forms;

#if ADVANCESTEEL2023
using Autodesk.AdvanceSteel.Runtime;
#else
using Autodesk.AutoCAD.Runtime;
#endif

namespace Speckle.ConnectorAutocadCivil.Entry
{
  public class App : IExtensionApplication
  {
    public RibbonControl ribbon;

    #region Initializing and termination
    public void Initialize()
    {
      try
      {
        //Advance Steel addon is initialized after ribbon creation
        bool advanceSteel = false;
#if ADVANCESTEEL2023
        advanceSteel = true;
#endif

        ribbon = ComponentManager.Ribbon;
        if (ribbon != null && !advanceSteel) //the assembly was loaded using netload
        {
          Create();
        }
        else
        {
          // load the custom ribbon on startup, but wait for ribbon control to be created
          ComponentManager.ItemInitialized += new System.EventHandler<RibbonItemEventArgs>(ComponentManager_ItemInitialized);
          Application.SystemVariableChanged += TrapWSCurrentChange;
        }

        //Some dlls fail to load due to versions matching (0.10.7 vs 0.10.0)
        //the below should fix it! This affects Avalonia and Material 
        AppDomain.CurrentDomain.AssemblyResolve += new ResolveEventHandler(OnAssemblyResolve);

        // DUI2
        SpeckleAutocadCommand.InitAvalonia();
        var bindings = new ConnectorBindingsAutocad();
        bindings.RegisterAppEvents();
        SpeckleAutocadCommand.Bindings = bindings;
      }
      catch (System.Exception e)
      {
        Forms.MessageBox.Show($"Add-in initialize context (true = application, false = doc): {Application.DocumentManager.IsApplicationContext.ToString()}. Error encountered: {e.ToString()}");
      }
    }

    Assembly OnAssemblyResolve(object sender, ResolveEventArgs args)
    {
      Assembly a = null;
      var name = args.Name.Split(',')[0];
      string path = Path.GetDirectoryName(typeof(App).Assembly.Location);

      string assemblyFile = Path.Combine(path, name + ".dll");

      if (File.Exists(assemblyFile))
        a = Assembly.LoadFrom(assemblyFile);

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
      RibbonToolTip speckleTip = CreateToolTip("Speckle", "Speckle Connector for " + Utils.AppName);
      RibbonToolTip oneClickTip = CreateToolTip("Send", "Sends your selected objects to your account's document stream. If nothing is selected, sends everything in the document.");
      RibbonButton button = CreateButton("Connector " + Utils.AppName, "Speckle", panel, null, speckleTip, "logo");
      RibbonButton oneClickSendButton = CreateButton("Send", "SpeckleSend", panel, null, oneClickTip, "send");

      // help and resources buttons
      RibbonSplitButton helpButton = new RibbonSplitButton();
      helpButton.Text = "Help & Resources";
      helpButton.Image = LoadPngImgSource("help16.png");
      helpButton.LargeImage = LoadPngImgSource("help32.png");
      helpButton.ShowImage = true;
      helpButton.ShowText = true;
      helpButton.Size = RibbonItemSize.Large;
      helpButton.Orientation = Orientation.Vertical;
      panel.Items.Add(helpButton);

      RibbonToolTip communityTip = CreateToolTip("Community", "Check out our community forum! Opens a page in your web browser.");
      RibbonToolTip tutorialsTip = CreateToolTip("Tutorials", "Check out our tutorials! Opens a page in your web browser");
      RibbonToolTip docsTip = CreateToolTip("Docs", "Check out our documentation! Opens a page in your web browser");
      RibbonButton community = CreateButton("Community", "SpeckleCommunity", null, helpButton, communityTip, "forum");
      RibbonButton tutorials = CreateButton("Tutorials", "SpeckleTutorials", null, helpButton, tutorialsTip, "tutorials");
      RibbonButton docs = CreateButton("Docs", "SpeckleDocs", null, helpButton, docsTip, "docs");
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

#if !ADVANCESTEEL2023
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
      RibbonToolTip toolTip = new RibbonToolTip();

      //toolTip.Command = "";
      toolTip.Title = title;
      toolTip.Content = content;
      toolTip.IsHelpEnabled = true; // Without this "Press F1 for help" does not appear in the tooltip

      return toolTip;
    }

    private RibbonButton CreateButton(string name, string CommandParameter, RibbonPanelSource sourcePanel = null, RibbonSplitButton sourceButton = null, RibbonToolTip tooltip = null, string imageName = "")
    {
      var button = new RibbonButton();

      // ribbon panel source info assignment
      button.Text = name;
      button.Id = name;
      button.ShowImage = true;
      button.ShowText = true;
      button.ToolTip = tooltip;
      button.HelpSource = new System.Uri("https://speckle.guide/user/autocadcivil.html");
      button.Size = RibbonItemSize.Large;
      button.Image = LoadPngImgSource(imageName + "16.png");
      button.LargeImage = LoadPngImgSource(imageName + "32.png");

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

    private ImageSource LoadPngImgSource(string sourceName)
    {
      try
      {
        string resource = this.GetType().Assembly.GetManifestResourceNames().Where(o => o.EndsWith(sourceName)).FirstOrDefault();
        Assembly assembly = Assembly.GetExecutingAssembly();
        Stream stream = assembly.GetManifestResourceStream(resource);
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
      private string commandParameter;

      public ButtonCommandHandler(string commandParameter)
      {
        this.commandParameter = commandParameter;
      }

      public event System.EventHandler CanExecuteChanged;

      public void Execute(object parameter)
      {
        RibbonButton btn = parameter as RibbonButton;
        if (btn != null)
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
          }
      }

      public bool CanExecute(object parameter) => true;
    }
  }
}
