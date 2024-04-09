using System.Text;

namespace DllConflictManagment;

public abstract class DllConflictUserNotifier
{
  private readonly DllConflictManager _dllConflictManager;

  protected DllConflictUserNotifier(DllConflictManager dllConflictManager)
  {
    _dllConflictManager = dllConflictManager;
  }

  public void NotifyUserOfMissingMethodException(MissingMethodException ex)
  {
    StringBuilder sb = new();
    sb.AppendLine($"Speckle failed to load due to a dependency mismatch error.");
    if (_dllConflictManager.GetConflictThatCausedException(ex) is AssemblyConflictInfo assemblyConflictInfo)
    {
      AddSingleDependencyErrorToStringBuilder(sb, assemblyConflictInfo);
    }
    else if (
      _dllConflictManager.AllConflictInfo.ToList() is List<AssemblyConflictInfo> conflictInfos
      && conflictInfos.Count > 0
    )
    {
      sb.AppendLine("This is likely due to one of the following dependency conflicts");
      AddMultipleDependencyErrorsToStringBuilder(sb, conflictInfos);
    }

    ShowDialog("Conflict Report 🔥", sb.ToString());
  }

  private static void AddSingleDependencyErrorToStringBuilder(
    StringBuilder sb,
    AssemblyConflictInfo assemblyConflictInfo
  )
  {
    sb.AppendLine($"Dependency Name:  {assemblyConflictInfo.SpeckleDependencyAssemblyName.Name}");
    sb.AppendLine($"Expected Version: {assemblyConflictInfo.SpeckleDependencyAssemblyName.Version}");
    sb.AppendLine($"Actual Version:   {assemblyConflictInfo.ConflictingAssembly.GetName().Version}");
    sb.AppendLine($"Conflicting version folder: {assemblyConflictInfo.GetConflictingExternalAppName()}");
  }

  private static void AddMultipleDependencyErrorsToStringBuilder(
    StringBuilder sb,
    IEnumerable<AssemblyConflictInfo> assemblyConflictInfos
  )
  {
    foreach (var assemblyConflictInfo in assemblyConflictInfos)
    {
      sb.AppendLine();
      AddSingleDependencyErrorToStringBuilder(sb, assemblyConflictInfo);
    }
  }

  protected abstract void ShowDialog(string dialogTitle, string body);

  public void NotifyUserOfTypeLoadException(TypeLoadException ex)
  {
    StringBuilder sb = new();
    sb.AppendLine($"Speckle failed to load due to a dependency mismatch error.");
    if (_dllConflictManager.GetConflictThatCausedException(ex) is AssemblyConflictInfo assemblyConflictInfo)
    {
      AddSingleDependencyErrorToStringBuilder(sb, assemblyConflictInfo);
    }
    else if (
      _dllConflictManager.AllConflictInfo.ToList() is List<AssemblyConflictInfo> conflictInfos
      && conflictInfos.Count > 0
    )
    {
      sb.AppendLine("This is likely due to one of the following dependency conflicts");
      AddMultipleDependencyErrorsToStringBuilder(sb, conflictInfos);
    }

    ShowDialog("Conflict Report 🔥", sb.ToString());
  }

  public void WarnUserOfPossibleConflicts()
  {
    List<AssemblyConflictInfo> conflictInfoNotIgnored = _dllConflictManager
      .GetConflictInfoThatUserHasntSuppressedWarningsFor()
      .ToList();

    if (conflictInfoNotIgnored.Count == 0)
    {
      return;
    }

    StringBuilder sb = new();
    AddMultipleDependencyErrorsToStringBuilder(sb, conflictInfoNotIgnored);

    ShowDialogWithDoNotWarnAgainOption(
      "Conflict Report 🔥",
      "Speckle could not load the correct version of the following dependencies. This is likely due to a different addin that uses a different version of the same dependency. This may cause the Speckle Connector to not work properly",
      sb.ToString(),
      out bool doNotWarnAgain
    );

    if (doNotWarnAgain)
    {
      _dllConflictManager.SupressFutureAssemblyConflictWarnings(conflictInfoNotIgnored);
    }
  }

  protected abstract void ShowDialogWithDoNotWarnAgainOption(
    string dialogTitle,
    string dialogHeading,
    string body,
    out bool doNotWarnAgain
  );
}
