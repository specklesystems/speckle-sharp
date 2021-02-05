using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.Civil.ApplicationServices;
using Speckle.DesktopUI;
using Speckle.ConnectorAutoCAD;
using Speckle.ConnectorAutoCAD.UI;
using Autodesk.AutoCAD.EditorInput;

[assembly: CommandClass(typeof(Speckle.ConnectorAutoCAD.Entry.SpeckleAutoCADCommand))]
namespace Speckle.ConnectorAutoCAD.Entry
{
  public class SpeckleAutoCADCommand
  {
    public static Bootstrapper Bootstrapper { get; set; }
    public static ConnectorBindingsAutoCAD Bindings { get; set; }
    public static Document Doc => Application.DocumentManager.MdiActiveDocument;
    public static CivilDocument DocC3D => CivilApplication.ActiveDocument;


    [CommandMethod("Speckle")]
    public static void SpeckleCommand()
    {
      try
      {
        if (Bootstrapper != null)
        {
          Bootstrapper.Application.MainWindow.Show();
          return;
        }

        Bootstrapper = new Bootstrapper()
        {
          Bindings = Bindings
        };

        Bootstrapper.Setup(System.Windows.Application.Current != null ? System.Windows.Application.Current : new System.Windows.Application());

        Bootstrapper.Application.Startup += (o, e) =>
        {
          var helper = new System.Windows.Interop.WindowInteropHelper(Bootstrapper.Application.MainWindow);
          helper.Owner = Application.MainWindow.Handle;
        };
      }
      catch (System.Exception e)
      {

      }
    }

    [CommandMethod("SpeckleSelection", CommandFlags.UsePickSet | CommandFlags.Transparent)]
    public static void GetSelection()
    {
      List<string> objs = new List<string>();
      PromptSelectionResult selection = Doc.Editor.SelectImplied(); // don't use get selection as this will prompt user to select if nothing is selected already
      if (selection.Status == PromptStatus.OK)
        objs = selection.Value.GetHandles();
      UserData.UpdateSpeckleSelection(objs);
    }

    /*
    [CommandMethod("SpeckleSchema", CommandFlags.UsePickSet | CommandFlags.Transparent)]
    public static void SetSchema()
    {
      var ids = new List<ObjectId>();
      PromptSelectionResult selection = Doc.Editor.GetSelection();
      if (selection.Status == PromptStatus.OK)
        ids = selection.Value.GetObjectIds().ToList();
      foreach (var id in ids)
      {
        // decide schema here, assumption or user input.
        string schema = "";
        switch (id.ObjectClass.DxfName)
        {
          case "LINE":
            schema = "Column";
            break;
        }

        // add schema to object XData
        using (Transaction tr = Doc.TransactionManager.StartTransaction())
        {
          DBObject obj = tr.GetObject(id, OpenMode.ForWrite);
          if (obj.XData == null)
            obj.XData = new ResultBuffer(new TypedValue(Convert.ToInt32(DxfCode.Text), schema));
          else
            obj.XData.Add(new TypedValue(Convert.ToInt32(DxfCode.Text), schema));
        }
      }
    }
    */
  }
}