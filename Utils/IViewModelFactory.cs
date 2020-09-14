using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
    StreamsHomeViewModel CreateStreamsHomeViewModel();
    InboxViewModel CreateInboxViewModel();
    FeedViewModel CreateFeedViewModel();
    SettingsViewModel CreateSettingsViewModel();
    AllStreamsViewModel CreateAllStreamsViewModel();
  }
}