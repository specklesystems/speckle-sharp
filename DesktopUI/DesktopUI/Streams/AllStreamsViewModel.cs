using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using MaterialDesignThemes.Wpf;
using Speckle.Core.Api;
using Speckle.DesktopUI.Utils;
using Stylet;

namespace Speckle.DesktopUI.Streams
{
  public class AllStreamsViewModel : Screen, IHandle<StreamAddedEvent>, IHandle<StreamUpdatedEvent>, IHandle<StreamRemovedEvent>, IHandle<ApplicationEvent>, IHandle<ReloadRequestedEvent>, IHandle<UpdateSelectionCountEvent>
  {
    private readonly IViewManager _viewManager;
    private readonly IStreamViewModelFactory _streamViewModelFactory;
    private readonly IDialogFactory _dialogFactory;
    private readonly IEventAggregator _events;
    private readonly ConnectorBindings _bindings;

    private StreamsRepository _repo;
    private BindableCollection<StreamState> _streamList = new BindableCollection<StreamState>();
    private Stream _selectedStream;
    private Branch _selectedBranch;
    private CancellationTokenSource _cancellationToken = new CancellationTokenSource();

    public BindableCollection<StreamState> StreamList
    {
      get => _streamList;
      set
      {
        SetAndNotify(ref _streamList, value);
        NotifyOfPropertyChange(nameof(EmptyState));
      }
    }

    public bool EmptyState { get => StreamList.Count == 0; }

    public Stream SelectedStream
    {
      get => _selectedStream;
      set => SetAndNotify(ref _selectedStream, value);
    }

    public Branch SelectedBranch
    {
      get => _selectedBranch;
      set => SetAndNotify(ref _selectedBranch, value);
    }

    private int _selectionCount = 0;

    public int SelectionCount
    {
      get => _selectionCount;
      set => SetAndNotify(ref _selectionCount, value);
    }

    public AllStreamsViewModel(IViewManager viewManager, IStreamViewModelFactory streamViewModelFactory, IDialogFactory dialogFactory, IEventAggregator events, StreamsRepository streamsRepo, ConnectorBindings bindings)
    {
      _repo = streamsRepo;
      _events = events;
      DisplayName = bindings.GetHostAppName() + " streams";
      _viewManager = viewManager;
      _streamViewModelFactory = streamViewModelFactory;
      _dialogFactory = dialogFactory;
      _bindings = bindings;

      StreamList = LoadStreams();

      _events.Subscribe(this);

      Globals.Repo = _repo;

      var ass = AppDomain.CurrentDomain.GetAssemblies().Where(x => x.FullName.ToLowerInvariant().Contains("xaml")).ToList();
    }

    public void RefreshPage()
    {
      NotifyOfPropertyChange(string.Empty);
      StreamList = LoadStreams();
    }

    private BindableCollection<StreamState> LoadStreams()
    {
      var streams = new BindableCollection<StreamState>(_bindings.GetStreamsInFile());

      return streams;
    }

    public void ShowStreamInfo(StreamState state)
    {
      var item = _streamViewModelFactory.CreateStreamViewModel();
      item.StreamState = state;
      ((RootViewModel)Parent).GoToStreamViewPage(item);
    }

    public async void SwitchBranch(BranchContextMenuItem.BranchSwitchCommandArgument args)
    {
      if (args.Branch != null)
      {
        args.RootStreamState.SwitchBranch(args.Branch);
        return;
      }

      var viewmodel = _dialogFactory.CreateBranchCreateDialogViewModel();
      viewmodel.StreamState = args.RootStreamState;

      var view = _viewManager.CreateAndBindViewForModelIfNecessary(viewmodel);
      var res = await DialogHost.Show(view, "RootDialogHost");
      if (res == null) return;
      args.RootStreamState.SwitchBranch((Branch)res);
    }

    public void SwitchCommit(CommitContextMenuItem.CommitSwitchCommandArgument args) => args.RootStreamState.SwitchCommit(args.Commit);

    public void SwapState(StreamState state)
    {
      state.SwapState();
      state.Refresh();
    }

    public async void ShowStreamUpdateObjectsDialog(StreamState state)
    {
      var viewmodel = _dialogFactory.StreamUpdateObjectsDialogView();
      viewmodel.SetState(state);
      var view = _viewManager.CreateAndBindViewForModelIfNecessary(viewmodel);

      await DialogHost.Show(view, "RootDialogHost");
    }

    public async void Send(StreamState state)
    {
      if (state.CommitExpanderChecked)
        state.Send();
      else
        state.CommitExpanderChecked = true;
    }

    public async void Receive(StreamState state) => state.Receive();

    #region Selection events

    public void SetObjectSelection(StreamState state) => state.SetObjectSelection();

    public void AddObjectSelection(StreamState state) => state.AddObjectSelection();

    public void RemoveObjectSelection(StreamState state) => state.RemoveObjectSelection();

    public void ClearObjectSelection(StreamState state) => state.ClearObjectSelection();

    #endregion

    public async void ShowStreamCreateDialog()
    {
      var viewmodel = _dialogFactory.CreateStreamCreateDialog();
      viewmodel.StreamIds = StreamList.Select(s => s.Stream.id).ToList();
      var view = _viewManager.CreateAndBindViewForModelIfNecessary(viewmodel);

      var result = await DialogHost.Show(view, "RootDialogHost");
    }

    public void RemoveDisabledStream(StreamState state)
    {
      RemoveStream(state.Stream.id);
    }

    private void RemoveStream(string streamId)
    {
      var state = StreamList.First(s => s.Stream.id == streamId);
      StreamList.Remove(state);
      NotifyOfPropertyChange(nameof(EmptyState));
    }

    public void OpenStreamInWeb(StreamState state)
    {
      Link.OpenInBrowser($"{state.ServerUrl}/streams/{state.Stream.id}");
    }

    public void CopyStreamUrl(StreamState state)
    {
      Clipboard.SetDataObject($"{state.ServerUrl}/streams/{state.Stream.id}");
      // notification might actually be annoying? idk commenting out for now
      // _bindings.RaiseNotification($"Copied URL for {state.Stream.name} to clipboard");
    }

    #region Application events

    public void Handle(StreamAddedEvent message)
    {
      StreamList.Insert(0, message.NewStream);
      NotifyOfPropertyChange(nameof(EmptyState));
    }

    public void Handle(StreamUpdatedEvent message)
    {
      StreamList.Refresh();
    }

    public void Handle(StreamRemovedEvent message)
    {
      RemoveStream(message.StreamId);
    }

    public void Handle(ApplicationEvent message)
    {
      switch (message.Type)
      {
        case ApplicationEvent.EventType.DocumentClosed:
          {
            StreamList.Clear();

            break;
          }
        case ApplicationEvent.EventType.DocumentModified:
          {
            // warn that stream data may be expired
            break;
          }
        case ApplicationEvent.EventType.DocumentOpened:
        case ApplicationEvent.EventType.ViewActivated:
          StreamList.Clear();
          StreamList = new BindableCollection<StreamState>(message.DynamicInfo);
          StreamList.Refresh();
          //foreach (var state in StreamList)
          //{
          //  state.RefreshStream();
          //}
          break;
        case ApplicationEvent.EventType.ApplicationIdling:
          break;
        default:
          return;
      }
    }

    public void Handle(ReloadRequestedEvent message)
    {
      StreamList = LoadStreams();
    }

    public void Handle(UpdateSelectionCountEvent message)
    {
      SelectionCount = message.SelectionCount;
    }

    #endregion
  }

  /// <summary>
  /// Opens context menus on left click and positions them directly under the button.
  /// </summary>
  public static class ContextMenuLeftClickBehavior
  {
    public static bool GetIsLeftClickEnabled(DependencyObject obj)
    {
      return (bool)obj.GetValue(IsLeftClickEnabledProperty);
    }

    public static void SetIsLeftClickEnabled(DependencyObject obj, bool value)
    {
      obj.SetValue(IsLeftClickEnabledProperty, value);
    }

    public static readonly DependencyProperty IsLeftClickEnabledProperty = DependencyProperty.RegisterAttached(
      "IsLeftClickEnabled",
      typeof(bool),
      typeof(ContextMenuLeftClickBehavior),
      new UIPropertyMetadata(false, OnIsLeftClickEnabledChanged));

    private static void OnIsLeftClickEnabledChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
      var uiElement = sender as UIElement;

      if (uiElement == null) return;
      bool IsEnabled = e.NewValue is bool value && value;

      if (IsEnabled)
      {
        if (uiElement is ButtonBase btn)
        {
          btn.Click += OnMouseLeftButtonUp;
        }
        else
        {
          uiElement.MouseLeftButtonUp += OnMouseLeftButtonUp;
        }
      }
      else
      {
        if (uiElement is ButtonBase btn)
        {
          btn.Click -= OnMouseLeftButtonUp;
        }
        else
        {
          uiElement.MouseLeftButtonUp -= OnMouseLeftButtonUp;
        }
      }
    }

    private static void OnMouseLeftButtonUp(object sender, RoutedEventArgs e)
    {
      Debug.Print("OnMouseLeftButtonUp");
      var fe = sender as FrameworkElement;
      if (fe == null) return;
      // if we use binding in our context menu, then it's DataContext won't be set when we show the menu on left click
      // (it seems setting DataContext for ContextMenu is hardcoded in WPF when user right clicks on a control, although I'm not sure)
      // so we have to set up ContextMenu.DataContext manually here
      if (fe.ContextMenu.DataContext == null)
      {
        fe.ContextMenu.SetBinding(FrameworkElement.DataContextProperty, new Binding { Source = fe.DataContext });
      }
      fe.ContextMenu.PlacementTarget = fe;
      //fe.ContextMenu.
      fe.ContextMenu.Placement = PlacementMode.Bottom;
      //fe.ContextMenu.HorizontalOffset = 0;
      //fe.ContextMenu.VerticalOffset = 0;
      if (fe.ContextMenu.IsOpen)
      {
        Debug.WriteLine("WASD OPEN");
      }
      fe.ContextMenu.IsOpen = true;
    }
  }

  public class BindingProxy : Freezable
  {
    protected override Freezable CreateInstanceCore()
    {
      return new BindingProxy();
    }

    public object Data
    {
      get { return GetValue(DataProperty); }
      set { SetValue(DataProperty, value); }
    }

    public static readonly DependencyProperty DataProperty =
      DependencyProperty.Register("Data", typeof(object), typeof(BindingProxy), new UIPropertyMetadata(null));
  }

}
