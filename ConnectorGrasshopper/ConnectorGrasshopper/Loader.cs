using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Timers;
using System.Windows.Forms;
using Grasshopper.Kernel;
using Speckle.Core.Kits;
using Speckle.Core.Logging;

namespace ConnectorGrasshopper
{
  public class Loader : GH_AssemblyPriority
  {
    public bool MenuHasBeenAdded;

    public IEnumerable<ISpeckleKit> loadedKits;
    public ISpeckleKit selectedKit;
    private ToolStripMenuItem speckleMenu;
    private IEnumerable<ToolStripItem> kitMenuItems;

    public override GH_LoadingInstruction PriorityLoad()
    {
      Setup.Init(Applications.Grasshopper);
      Grasshopper.Instances.DocumentServer.DocumentAdded += CanvasCreatedEvent;
      Grasshopper.Instances.ComponentServer.AddCategoryIcon(ComponentCategories.PRIMARY_RIBBON, Properties.Resources.speckle_logo);
      Grasshopper.Instances.ComponentServer.AddCategorySymbolName(ComponentCategories.PRIMARY_RIBBON, 'S');
      Grasshopper.Instances.ComponentServer.AddCategoryIcon(ComponentCategories.SECONDARY_RIBBON, Properties.Resources.speckle_logo);
      Grasshopper.Instances.ComponentServer.AddCategorySymbolName(ComponentCategories.SECONDARY_RIBBON, 'S');

      return GH_LoadingInstruction.Proceed;
    }

    private void CanvasCreatedEvent(GH_DocumentServer server, GH_Document doc)
    {
        AddSpeckleMenu(null, null);
    }
    
    private void HandleKitSelectedEvent(object sender, EventArgs args)
    {
      var clickedItem = (ToolStripMenuItem)sender;

      // Update the selected kit
      selectedKit = loadedKits.First(kit => clickedItem.Text.Trim() == kit.Name);

      var key = "Speckle2:kit.default.name";
      Grasshopper.Instances.Settings.SetValue(key, selectedKit.Name);
      Grasshopper.Instances.Settings.WritePersistentSettings();
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
        loadedKits = KitManager.GetKitsWithConvertersForApp(Applications.Rhino6);

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

      // Help items
      var helpHeader = speckleMenu.DropDown.Items.Add("Looking for help?");
      helpHeader.Enabled = false;
      speckleMenu.DropDown.Items.Add("Community Forum",Properties.Resources.forum16,(o, args) => Process.Start("https://speckle.community"));
      speckleMenu.DropDown.Items.Add("Tutorials", Properties.Resources.tutorials16, (o, args) => Process.Start("https://speckle.systems/tutorials"));
      speckleMenu.DropDown.Items.Add("Docs",Properties.Resources.docs16,(o, args) => Process.Start("https://speckle.guide"));
      
      speckleMenu.DropDown.Items.Add(new ToolStripSeparator());
      
      // Manager button
      speckleMenu.DropDown.Items.Add("Open Speckle Manager", Properties.Resources.speckle_logo, (o, args) => Process.Start("speckle://"));
      

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
  }
}
