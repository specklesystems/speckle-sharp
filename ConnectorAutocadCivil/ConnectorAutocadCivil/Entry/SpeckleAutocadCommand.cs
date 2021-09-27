using System.Collections.Generic;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.Runtime;
using Speckle.ConnectorAutocadCivil.UI;
using Speckle.DesktopUI;
using Application = Autodesk.AutoCAD.ApplicationServices.Core.Application;

namespace Speckle.ConnectorAutocadCivil.Entry
{
  public class SpeckleAutocadCommand
  {
    public static Bootstrapper Bootstrapper { get; set; }
    public static ConnectorBindingsAutocad Bindings { get; set; }

    /// <summary>
    /// Main command to initialize Speckle Connector
    /// </summary>
    [CommandMethod("Speckle", CommandFlags.Modal)]
    public static void SpeckleCommand()
    {
      try
      {
        if (Bootstrapper != null)
        {
          Bootstrapper.ShowRootView();
          return;
        }

        Bootstrapper = new Bootstrapper()
        {
          Bindings = Bindings
        };

        if (System.Windows.Application.Current != null)
          new StyletAppLoader() { Bootstrapper = Bootstrapper };
        else
          new DesktopUI.App(Bootstrapper);

        Bootstrapper.Start(System.Windows.Application.Current);
        Bootstrapper.SetParent(Application.MainWindow.Handle);
      }
      catch (System.Exception e)
      {

      }
    }
    
    [CommandMethod("SpeckleCommunity", CommandFlags.ActionMacro)]
    public static void SpeckleCommunity()
    {
      try
      {
        Application.DocumentManager.MdiActiveDocument.SendStringToExecute("_browser https://speckle.community ", false, false, true);
      }
      catch { }
    }

    [CommandMethod("SpeckleTutorials", CommandFlags.ActionMacro)]
    public static void SpeckleTutorials()
    {
      try
      {
        Application.DocumentManager.MdiActiveDocument.SendStringToExecute("_browser https://speckle.systems/tutorials ", false, false, true);
      }
      catch { }
    }

    [CommandMethod("SpeckleDocs", CommandFlags.ActionMacro)]
    public static void SpeckleDocs()
    {
      try
      {
        Application.DocumentManager.MdiActiveDocument.SendStringToExecute("_browser https://speckle.guide/user/autocadcivil.html ", false, false, true);
      }
      catch { }
    }

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

