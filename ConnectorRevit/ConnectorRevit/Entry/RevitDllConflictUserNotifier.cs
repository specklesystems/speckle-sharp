using Autodesk.Revit.UI;
using Speckle.DllConflictManagement;
using Speckle.DllConflictManagement.EventEmitter;

namespace ConnectorRevit.Entry;

internal sealed class RevitDllConflictUserNotifier : DllConflictUserNotifier
{
  public RevitDllConflictUserNotifier(DllConflictManager dllConflictManager, DllConflictEventEmitter eventEmitter)
    : base(dllConflictManager, eventEmitter) { }

  protected override void ShowDialog(string title, string heading, string body)
  {
    using var dialog = new TaskDialog(title);
    dialog.MainInstruction = heading;
    dialog.MainContent = body;
    dialog.CommonButtons = TaskDialogCommonButtons.Ok;
    _ = dialog.Show();
  }

  protected override void ShowDialogWithDoNotWarnAgainOption(
    string title,
    string heading,
    string body,
    out bool doNotShowAgain
  )
  {
    using var dialog = new TaskDialog(title);
    dialog.MainInstruction = heading;
    dialog.MainContent = body;

    dialog.ExtraCheckBoxText = "Do not warn about these conflicts again";
    dialog.CommonButtons = TaskDialogCommonButtons.Ok;

    _ = dialog.Show();

    doNotShowAgain = dialog.WasExtraCheckBoxChecked();
  }
}
