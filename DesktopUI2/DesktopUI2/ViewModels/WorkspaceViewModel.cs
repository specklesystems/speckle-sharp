#nullable enable
using ReactiveUI;
using Speckle.Core.Api.GraphQL.Models;

namespace DesktopUI2.ViewModels;

public sealed class WorkspaceViewModel : ReactiveObject
{
  public Workspace? Workspace { get; }

  public string Name => Workspace is not null ? Workspace.name : "Personal projects";
  public string Description => Workspace?.description ?? "";

  public WorkspaceViewModel(Workspace workspace)
  {
    Workspace = workspace;
  }

  private WorkspaceViewModel() { }

  public static WorkspaceViewModel PersonalProjects { get; } = new();
}
