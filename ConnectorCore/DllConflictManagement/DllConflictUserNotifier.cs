using System.Text;

namespace Speckle.DllConflictManagement;

public abstract class DllConflictUserNotifier
{
  private readonly DllConflictManager _dllConflictManager;

  protected DllConflictUserNotifier(DllConflictManager dllConflictManager)
  {
    _dllConflictManager = dllConflictManager;
  }

  public void NotifyUserOfMissingMethodException(MissingMethodException ex)
  {
    NotifyUserOfException(_dllConflictManager.GetConflictThatCausedException(ex), ex);
  }

  private void NotifyUserOfException(AssemblyConflictInfo? identifiedConflictingAssemblyInfo, Exception ex)
  {
    StringBuilder sb = new();
    if (identifiedConflictingAssemblyInfo is not null)
    {
      AddSingleDependencyErrorToStringBuilder(sb, identifiedConflictingAssemblyInfo);
    }
    else if (
      _dllConflictManager.AllConflictInfo.ToList() is List<AssemblyConflictInfo> conflictInfos
      && conflictInfos.Count > 0
    )
    {
      sb.AppendLine("This is likely due to one of the following dependency conflicts");
      AddMultipleDependencyErrorsToStringBuilder(sb, conflictInfos);
    }
    else
    {
      _dllConflictManager.LogError?.Invoke(
        $"A type load exception was thrown, but the conflict manager did not detect any conflicts. Exception message: {ex.Message}"
      );
    }

    ShowDialog("Conflict Report 🔥", "Speckle failed to load due to a dependency mismatch error.", sb.ToString());
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

  protected abstract void ShowDialog(string title, string heading, string body);

  public void NotifyUserOfTypeLoadException(TypeLoadException ex)
  {
    NotifyUserOfException(_dllConflictManager.GetConflictThatCausedException(ex), ex);
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
    string title,
    string heading,
    string body,
    out bool doNotWarnAgain
  );
}
