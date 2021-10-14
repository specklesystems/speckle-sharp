using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Text;
using DesktopUI2.Models;
using DesktopUI2.Views.Pages;
using Material.Colors;
using Material.Styles.Themes;
using ReactiveUI;
using Speckle.Core.Api;
using Splat;

namespace DesktopUI2.ViewModels
{
  public class MainWindowViewModel : ViewModelBase, IScreen
  {
    public RoutingState Router { get; private set; }

    public ConnectorBindings Bindings { get; private set; } = new DummyBindings();

    public static RoutingState RouterInstance { get; private set; }

    public ReactiveCommand<Unit, Unit> GoBack => Router.NavigateBack;

    public string Title => "for " + Bindings.GetHostAppName();
    public string TitleFull => "Speckle for " + Bindings.GetHostAppName();
    public string Version => "v" + Bindings.ConnectorVersion;
    public MainWindowViewModel(ConnectorBindings _bindings)
    {
      Bindings = _bindings;
      Init();
    }
    public MainWindowViewModel()
    {
      Init();

    }

    private void Init()
    {
      Router = new RoutingState();

      Locator.CurrentMutable.Register(() => new StreamEditView(), typeof(IViewFor<StreamEditViewModel>));
      Locator.CurrentMutable.Register(() => new HomeView(), typeof(IViewFor<HomeViewModel>));
      Locator.CurrentMutable.Register(() => Bindings, typeof(ConnectorBindings));

      RouterInstance = Router; // makes the router available app-wide
      Router.Navigate.Execute(new HomeViewModel(this));

      Bindings.UpdateSavedStreams = HomeViewModel.Instance.UpdateSavedStreams;
    }


  }
}
