using ReactiveUI;
using Speckle.Core.Api;

namespace DesktopUI2.ViewModels;

public class BranchViewModel : ReactiveObject
{
  private Branch _branch;

  private string _icon;

  public BranchViewModel(Branch branch, string icon = "SourceBranch")
  {
    Branch = branch;
    Icon = icon;
  }

  public Branch Branch
  {
    get => _branch;
    set => this.RaiseAndSetIfChanged(ref _branch, value);
  }

  public string Icon
  {
    get => _icon;
    set => this.RaiseAndSetIfChanged(ref _icon, value);
  }
}
