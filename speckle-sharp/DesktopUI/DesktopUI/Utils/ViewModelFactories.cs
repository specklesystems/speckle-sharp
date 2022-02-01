using Speckle.DesktopUI.Settings;
using Speckle.DesktopUI.Streams;
using Speckle.DesktopUI.Streams.Dialogs;

namespace Speckle.DesktopUI.Utils
{
  // View Model Factory to create the page view models
  // via the Bootstrapper 
  public interface IViewModelFactory
  {
    // view models for main pages
    SettingsViewModel CreateSettingsViewModel();

    // view models for main stream page
    AllStreamsViewModel CreateAllStreamsViewModel();
  }

  // Factory to create view models for individual stream pages
  public interface IStreamViewModelFactory
  {
    StreamViewModel CreateStreamViewModel();
  }

  // TODO move this into IViewModelFactory (they're not really dialogs)
  // Factory to create dialogs for stream creation and editing 
  public interface IDialogFactory
  {
    StreamCreateDialogViewModel CreateStreamCreateDialog();
    StreamUpdateObjectsDialogViewModel StreamUpdateObjectsDialogView();
    ShareStreamDialogViewModel CreateShareStreamDialogViewModel();
    BranchCreateDialogViewModel CreateBranchCreateDialogViewModel();
  }
}
