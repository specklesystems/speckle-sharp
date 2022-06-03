using DesktopUI2.ViewModels.Share;
using DesktopUI2.Views.Pages;
using DesktopUI2.Views.Pages.ShareControls;
using DesktopUI2.Views.Windows.Dialogs;
using ReactiveUI;
using Speckle.Core.Logging;
using Splat;
using System;
using System.Reactive;

namespace DesktopUI2.ViewModels
{
  public class MainViewModel : ViewModelBase, IScreen
  {
    public string TitleFull => "Speckle for " + Bindings.GetHostAppNameVersion();
    public RoutingState Router { get; private set; }

    public ConnectorBindings Bindings { get; private set; } = new DummyBindings();

    public static RoutingState RouterInstance { get; private set; }

    public ReactiveCommand<Unit, Unit> GoBack => Router.NavigateBack;

    internal static MainViewModel Instance { get; private set; }

    public bool DialogVisible
    {
      get => _dialogBody!=null;
    }

    public double DialogOpacity
    {
      get => _dialogBody != null ? 1 : 0;
    }

    private DialogUserControl _dialogBody;
    public DialogUserControl DialogBody
    {
      get => _dialogBody;
      set  {

        this.RaiseAndSetIfChanged(ref _dialogBody, value);
        this.RaisePropertyChanged("DialogVisible");
        this.RaisePropertyChanged("DialogOpacity");
      }

    }


    public MainViewModel(ConnectorBindings _bindings)
    {
      Bindings = _bindings;
      Init();
    }
    public MainViewModel()
    {
     
      Init();
    }

    private void Init()
    {
      Instance = this;
      Setup.Init(Bindings.GetHostAppNameVersion(), Bindings.GetHostAppName());

      RxApp.DefaultExceptionHandler = Observer.Create<Exception>(CatchReactiveException);

      Router = new RoutingState();

      Locator.CurrentMutable.Register(() => new StreamEditView(), typeof(IViewFor<StreamViewModel>));
      Locator.CurrentMutable.Register(() => new HomeView(), typeof(IViewFor<HomeViewModel>));
      Locator.CurrentMutable.Register(() => new CollaboratorsView(), typeof(IViewFor<CollaboratorsViewModel>));
      Locator.CurrentMutable.Register(() => new SettingsView(), typeof(IViewFor<SettingsPageViewModel>));
      Locator.CurrentMutable.Register(() => Bindings, typeof(ConnectorBindings));

      RouterInstance = Router; // makes the router available app-wide
      Router.Navigate.Execute(new HomeViewModel(this));

      Bindings.UpdateSavedStreams = HomeViewModel.Instance.UpdateSavedStreams;
      Bindings.UpdateSelectedStream = HomeViewModel.Instance.UpdateSelectedStream;

      Router.PropertyChanged += Router_PropertyChanged;
    }

    //https://github.com/AvaloniaUI/Avalonia/issues/5290
    private void CatchReactiveException(Exception e)
    {
      //do nothing
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
