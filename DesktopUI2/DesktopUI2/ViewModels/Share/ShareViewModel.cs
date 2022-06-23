using DesktopUI2.Views.Pages.ShareControls;
using ReactiveUI;
using Splat;
using System.Reactive;

namespace DesktopUI2.ViewModels.Share
{
  public class ShareViewModel : ViewModelBase, IScreen
  {
    public string Title => "for " + Bindings.GetHostAppNameVersion();
    public string TitleFull => "Quick Share";

    public RoutingState Router { get; private set; }
    public static RoutingState RouterInstance { get; private set; }

    public ReactiveCommand<Unit, Unit> GoBack => Router.NavigateBack;

    public ConnectorBindings Bindings { get; private set; } = new DummyBindings();



    public ShareViewModel()
    {
      Init();
    }

    public ShareViewModel(ConnectorBindings _bindings)
    {
      Init();
      Bindings = _bindings;
    }

    private void Init()
    {
      Router = new RoutingState();

      Locator.CurrentMutable.Register(() => new AddCollaborators(), typeof(IViewFor<AddCollaboratorsViewModel>));
      Locator.CurrentMutable.Register(() => new Sending(), typeof(IViewFor<SendingViewModel>));
      Locator.CurrentMutable.Register(() => Bindings, typeof(ConnectorBindings));

      RouterInstance = Router; // makes the router available app-wide
      Router.Navigate.Execute(new AddCollaboratorsViewModel(this));

    }


  }
}
