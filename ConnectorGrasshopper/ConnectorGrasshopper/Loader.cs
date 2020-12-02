using System;
using System.Diagnostics;
using System.Linq;
using System.Timers;
using System.Windows.Forms;
using Grasshopper.GUI;
using Grasshopper.GUI.Canvas;
using Grasshopper.Kernel;
using Speckle.Core.Kits;
using Speckle.Core.Logging;

namespace ConnectorGrasshopper
{
  public class Loader : GH_AssemblyPriority
  {
    System.Timers.Timer loadTimer;
    static bool MenuHasBeenAdded = false;

    public Loader( ) { }

    public override GH_LoadingInstruction PriorityLoad( )
    {
      Setup.Init(Applications.Grasshopper);
      Grasshopper.Instances.DocumentServer.DocumentAdded += CanvasCreatedEvent;
      return GH_LoadingInstruction.Proceed;
      
    }

    private void CanvasCreatedEvent(GH_DocumentServer server, GH_Document doc)
    {
      Console.WriteLine("CAnvas created");
      AddSpeckleMenu(null,null);
    }
    
    private void AddSpeckleMenu( object sender, ElapsedEventArgs e )
    {
      if ( Grasshopper.Instances.DocumentEditor == null ) return;
      if ( MenuHasBeenAdded )
      {
        return;
      }

      var speckleMenu = new ToolStripMenuItem( "Speckle 2" );

      var kitHeader = speckleMenu.DropDown.Items.Add( "Select the converter you want to use.");
      kitHeader.Enabled = false;

      var kits = KitManager.GetKitsWithConvertersForApp(Applications.Rhino);
      
      kits.ToList().ForEach(kit =>
      {
        speckleMenu.DropDown.Items.Add("  " + kit.Name);
        speckleMenu.CheckState = CheckState.Checked;
        speckleMenu.CheckOnClick = true;
        
      });
      
      speckleMenu.DropDown.Items.Add( new ToolStripSeparator() );

      speckleMenu.DropDown.Items.Add( "Open Speckle Manager", Properties.Resources.speckle_logo);

      try
      {
        var mainMenu = Grasshopper.Instances.DocumentEditor.MainMenuStrip;
        Grasshopper.Instances.DocumentEditor.Invoke( new Action( ( ) =>
        {
          if(!MenuHasBeenAdded)
            mainMenu.Items.Add( speckleMenu );
        } ) );
        MenuHasBeenAdded = true;
      }
      catch ( Exception err )
      {
        Debug.WriteLine( err.Message );
      }
    }
  }
}
