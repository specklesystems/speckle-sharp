using System;
using Speckle.DesktopUI.Utils;
using Stylet;

namespace Speckle.DesktopUI.Streams
{
  public class StreamsHomeViewModel : Conductor<IScreen>.StackNavigation
  {
    private IViewModelFactory _viewModelFactory;
    public StreamsHomeViewModel(IViewModelFactory viewModelFactory)
    {
      DisplayName = "Home";
      _viewModelFactory = viewModelFactory;

      var item = _viewModelFactory.CreateAllStreamsViewModel();

      ActivateItem(item);
    }
  }
}