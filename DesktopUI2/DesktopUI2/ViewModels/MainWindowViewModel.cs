﻿using DesktopUI2.Views.Pages;
using ReactiveUI;
using Speckle.Core.Logging;
using Splat;
using System.Reactive;

namespace DesktopUI2.ViewModels
{
  public class MainWindowViewModel : ViewModelBase, IScreen
  {
    public string TitleFull => "Speckle for " + Bindings.GetHostAppNameVersion();
    public RoutingState Router { get; private set; }

    public ConnectorBindings Bindings { get; private set; } = new DummyBindings();

    public static RoutingState RouterInstance { get; private set; }

    public ReactiveCommand<Unit, Unit> GoBack => Router.NavigateBack;


    public MainWindowViewModel(ConnectorBindings _bindings)
    {
      Bindings = _bindings;
      Setup.Init(Bindings.GetHostAppNameVersion(), Bindings.GetHostAppName());
      Init();
    }
    public MainWindowViewModel()
    {
      Init();
    }

    private void Init()
    {
      Router = new RoutingState();

      Locator.CurrentMutable.Register(() => new StreamEditView(), typeof(IViewFor<StreamViewModel>));
      Locator.CurrentMutable.Register(() => new HomeView(), typeof(IViewFor<HomeViewModel>));
      Locator.CurrentMutable.Register(() => Bindings, typeof(ConnectorBindings));

      RouterInstance = Router; // makes the router available app-wide
      Router.Navigate.Execute(new HomeViewModel(this));

      Bindings.UpdateSavedStreams = HomeViewModel.Instance.UpdateSavedStreams;
      Bindings.UpdateSelectedStream = HomeViewModel.Instance.UpdateSelectedStream;

      Router.PropertyChanged += Router_PropertyChanged;
      //var theme = PaletteHelper.GetTheme();
      //theme.SetPrimaryColor(SwatchHelper.Lookup[MaterialColor.Blue600]);
      //PaletteHelper.SetTheme(theme);
    }

    private void Router_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
      throw new System.NotImplementedException();
    }

    public static void GoHome()
    {
      if (RouterInstance != null && HomeViewModel.Instance != null)
        RouterInstance.Navigate.Execute(HomeViewModel.Instance);
    }

  }
}
