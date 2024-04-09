using Autodesk.Revit.UI;
using DllConflictManagment;

namespace ConnectorRevit.Entry;

internal sealed class RevitDllConflictUserNotifier : DllConflictUserNotifier
{
  public RevitDllConflictUserNotifier(DllConflictManager dllConflictManager)
    : base(dllConflictManager) { }

  protected override void ShowDialog(string dialogTitle, string body) => TaskDialog.Show(dialogTitle, body);

  protected override void ShowDialogWithDoNotWarnAgainOption(
    string dialogTitle,
    string dialogHeading,
    string body,
    out bool doNotShowAgain
  )
  {
    using var dialog = new TaskDialog(dialogTitle);
    dialog.MainInstruction = dialogHeading;
    dialog.MainContent = body;

    dialog.ExtraCheckBoxText = "Do not warn about this conflict again";
    dialog.CommonButtons = TaskDialogCommonButtons.Ok;

    _ = dialog.Show();

    doNotShowAgain = dialog.WasExtraCheckBoxChecked();
  }
}
