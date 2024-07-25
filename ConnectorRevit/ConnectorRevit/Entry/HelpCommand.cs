using System;
using System.IO;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Speckle.Core.Helpers;

namespace Speckle.ConnectorRevit.Entry;

[Transaction(TransactionMode.Manual)]
public class ForumCommand : IExternalCommand
{
  public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
  {
    Open.Url("https://speckle.community/");
    return Result.Succeeded;
  }
}

[Transaction(TransactionMode.Manual)]
public class DocsCommand : IExternalCommand
{
  public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
  {
    Open.Url("https://speckle.guide/user/revit.html");
    return Result.Succeeded;
  }
}

[Transaction(TransactionMode.Manual)]
public class TutorialsCommand : IExternalCommand
{
  public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
  {
    Open.Url("https://speckle.systems/tutorials/");
    return Result.Succeeded;
  }
}

[Transaction(TransactionMode.Manual)]
public class ManagerCommand : IExternalCommand
{
  public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
  {
    var path = Path.Combine(
      Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
      "Speckle",
      "Manager",
      "Manager.exe"
    );
    if (File.Exists(path))
    {
      Open.File(path);
    }
    else
    {
      TaskDialog.Show("No Manager found", "Seems like Manager is not installed on this pc.");
    }

    return Result.Succeeded;
  }
}

[Transaction(TransactionMode.Manual)]
public class NewRibbonCommand : IExternalCommand
{
  public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
  {
    TaskDialog mainDialog = new("Speckle has moved!");
    mainDialog.MainInstruction = "Speckle has moved!";
    mainDialog.MainContent = "The Speckle Connector for Revit has moved to its own Tab named 'Speckle' 👉";
    mainDialog.FooterText = "<a href=\"https://speckle.community/\">" + "Feedback?</a>";

    mainDialog.Show();
    return Result.Succeeded;
  }
}
