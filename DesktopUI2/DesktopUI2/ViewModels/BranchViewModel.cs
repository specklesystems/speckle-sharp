using ReactiveUI;
using Speckle.Core.Api;

namespace DesktopUI2.ViewModels
{
  public class BranchViewModel : ReactiveObject
  {

    private Branch _branch;
    public Branch Branch
    {
      get => _branch;
      set => this.RaiseAndSetIfChanged(ref _branch, value);

    }

    private string _icon;
    public string Icon
    {
      get => _icon;
      set => this.RaiseAndSetIfChanged(ref _icon, value);

    }


    public BranchViewModel(Branch branch, string icon = "SourceBranch")
    {
      Branch = branch;
      Icon = icon;
    }
  }
}
