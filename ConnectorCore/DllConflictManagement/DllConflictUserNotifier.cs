using System.Text;
using Speckle.DllConflictManagement.Analytics;
using Speckle.DllConflictManagement.EventEmitter;

namespace Speckle.DllConflictManagement;

public abstract class DllConflictUserNotifier
{
  private readonly DllConflictManager _dllConflictManager;
  private readonly DllConflictEventEmitter _eventEmitter;

  protected DllConflictUserNotifier(DllConflictManager dllConflictManager, DllConflictEventEmitter eventEmitter)
  {
    _dllConflictManager = dllConflictManager;
    _eventEmitter = eventEmitter;
  }

  public void NotifyUserOfMissingMethodException(MemberAccessException ex)
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
    else if (_dllConflictManager.AllConflictInfo.Count > 0)
    {
      sb.AppendLine("This is likely due to one of the following dependency conflicts");
      AddMultipleDependencyErrorsToStringBuilder(sb, _dllConflictManager.AllConflictInfo);
    }
    else
    {
      _eventEmitter.EmitError(
        new LoggingEventArgs(
          "A type load exception was thrown, but the conflict manager did not detect any conflicts",
          ex
        )
      );
    }

    Dictionary<string, object?> eventParameters =
      new()
      {
        { "name", "DllConflictExceptionDialogShown" },
        { "conflictExceptionMessage", ex.Message },
        { "conflictExceptionType", ex.GetType().FullName },
        { "identifiedConflictingAssemblyName", identifiedConflictingAssemblyInfo?.SpeckleDependencyAssemblyName.Name },
        { "speckleVersion", identifiedConflictingAssemblyInfo?.SpeckleDependencyAssemblyName.Version },
        { "loadedVersion", identifiedConflictingAssemblyInfo?.ConflictingAssembly.GetName().Version },
        { "loadedDependencyLocation", identifiedConflictingAssemblyInfo?.GetConflictingExternalAppName() },
      };

    if (identifiedConflictingAssemblyInfo is null)
    {
      eventParameters["numDetectedConflicts"] = _dllConflictManager.AllConflictInfo.Count;
      eventParameters["allDetectedConflicts"] = _dllConflictManager.AllConflictInfoAsDtos.ToList();
    }

    _eventEmitter.EmitAction(new ActionEventArgs(nameof(Events.DUIAction), eventParameters));

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
      .Where(conflict => !conflict.SpeckleDependencyAssemblyName.Name.StartsWith("System.")) // ignore system dlls for now
      .ToList();

    if (conflictInfoNotIgnored.Count == 0)
    {
      return;
    }

    _eventEmitter.EmitAction(
      new ActionEventArgs(
        nameof(Events.DUIAction),
        new()
        {
          { "name", "DllConflictsDetected" },
          { "dialogShown", true },
          { "numConflicts", conflictInfoNotIgnored.Count },
          { "conflicts", conflictInfoNotIgnored.ToDtos().ToList() },
        }
      )
    );

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
      _eventEmitter.EmitAction(
        new ActionEventArgs(nameof(Events.DUIAction), new() { { "name", "Do not warn about dll conflicts checked" } })
      );
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
