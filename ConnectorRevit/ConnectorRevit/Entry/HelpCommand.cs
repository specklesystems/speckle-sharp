using System;
using System.Diagnostics;
using System.IO;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace Speckle.ConnectorRevit.Entry
{
  [Transaction(TransactionMode.Manual)]
  public class ForumCommand : IExternalCommand
  {
    public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
    {
      Process.Start("https://speckle.community/");
      return Result.Succeeded;
    }
  }
  [Transaction(TransactionMode.Manual)]
  public class DocsCommand : IExternalCommand
  {
    public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
    {
      Process.Start("https://speckle.guide/user/revit.html");
      return Result.Succeeded;
    }
  }
  [Transaction(TransactionMode.Manual)]
  public class TutorialsCommand : IExternalCommand
  {
    public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
    {
      Process.Start("https://speckle.systems/tutorials/");
      return Result.Succeeded;
    }
  }

  [Transaction(TransactionMode.Manual)]
  public class ManagerCommand : IExternalCommand
  {
    public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
    {
      Process.Start(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs", "speckle-manager", "SpeckleManager.exe"));
      return Result.Succeeded;
    }
  }

  [Transaction(TransactionMode.Manual)]
  public class NewRibbonCommand : IExternalCommand
  {
    public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
    {
      TaskDialog mainDialog = new TaskDialog("Speckle has moved!");
      mainDialog.MainInstruction = "Speckle has moved!";
      mainDialog.MainContent =
          "The Speckle Connector for Revit has moved to its own Tab named 'Speckle' 👉";
      mainDialog.FooterText =
          "<a href=\"https://speckle.community/\">"
          + "Feedback?</a>";

      mainDialog.Show();
      return Result.Succeeded;
    }
  }

}
