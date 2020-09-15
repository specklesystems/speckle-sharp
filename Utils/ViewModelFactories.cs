using System;
using System.Threading.Tasks;
using Speckle.DesktopUI.Feed;
using Speckle.DesktopUI.Inbox;
using Speckle.DesktopUI.Settings;
using Speckle.DesktopUI.Streams;

namespace Speckle.DesktopUI.Utils
{
  // View Model Factory to create the page view models
  // via the Bootstrapper 
  public interface IViewModelFactory
  {
    // view models for main pages
    StreamsHomeViewModel CreateStreamsHomeViewModel();
    InboxViewModel CreateInboxViewModel();
    FeedViewModel CreateFeedViewModel();
    SettingsViewModel CreateSettingsViewModel();

    // view models for main stream page
    AllStreamsViewModel CreateAllStreamsViewModel();
  }

  // Factory to create view models for individual stream pages
  public interface IStreamViewModelFactory
  {
    StreamViewModel CreateStreamViewModel();
  }

  // Factory to create dialogs for stream creation and editing 
  public interface IDialogFactory
  {
    StreamCreateDialogViewModel CreateStreamCreateDialog();
  }
}
