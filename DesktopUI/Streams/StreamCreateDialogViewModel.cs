using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using MaterialDesignThemes.Wpf;
using Speckle.Core.Api;
using Speckle.Core.Credentials;
using Speckle.Core.Logging;
using Speckle.DesktopUI.Accounts;
using Speckle.DesktopUI.Utils;
using Stylet;

namespace Speckle.DesktopUI.Streams
{
  public class StreamCreateDialogViewModel : StreamDialogBase,
    IHandle<RetrievedFilteredObjectsEvent>, IHandle<UpdateSelectionEvent>, IHandle<ApplicationEvent>
  {
    private readonly IEventAggregator _events;
    private ISnackbarMessageQueue _notifications = new SnackbarMessageQueue(TimeSpan.FromSeconds(5));

    public StreamCreateDialogViewModel(
      IEventAggregator events,
      StreamsRepository streamsRepo,
      AccountsRepository acctsRepo,
      ConnectorBindings bindings)
    {
      DisplayName = "Create Stream";
      _events = events;
      Bindings = bindings;
      FilterTabs = new BindableCollection<FilterTab>(Bindings.GetSelectionFilters().Select(f => new FilterTab(f)));
      _streamsRepo = streamsRepo;
      _acctRepo = acctsRepo;

      SelectionCount = Bindings.GetSelectedObjects().Count;
      _events.Subscribe(this);
    }

    private readonly StreamsRepository _streamsRepo;
    private readonly AccountsRepository _acctRepo;

    public ISnackbarMessageQueue Notifications
    {
      get => _notifications;
      set => SetAndNotify(ref _notifications, value);
    }

    public List<string> StreamIds;

    private bool _createButtonLoading;

    public bool CreateButtonLoading
    {
      get => _createButtonLoading;
      set => SetAndNotify(ref _createButtonLoading, value);
    }

    private bool _addExistingButtonLoading;

    public bool AddExistingButtonLoading
    {
      get => _addExistingButtonLoading;
      set => SetAndNotify(ref _addExistingButtonLoading, value);
    }

    private Stream _streamToCreate = new Stream();

    public Stream StreamToCreate
    {
      get => _streamToCreate;
      set => SetAndNotify(ref _streamToCreate, value);
    }

    private StreamState _streamState = new StreamState();

    public StreamState StreamState
    {
      get => _streamState;
      set => SetAndNotify(ref _streamState, value);
    }

    public ObservableCollection<Account> Accounts
    {
      get => _acctRepo.LoadAccounts();
    }

    #region Searching Existing Streams

    private string _streamQuery;

    public string StreamQuery
    {
      get => _streamQuery;
      set
      {
        SetAndNotify(ref _streamQuery, value);

        if ( value == "" )
        {
          SelectedStream = null;
          StreamSearchResults?.Clear();
        }

        if ( SelectedStream != null && value == SelectedStream.name ) return;
        SearchForStreams();
      }
    }

    private BindableCollection<Stream> _streamSearchResults;

    public BindableCollection<Stream> StreamSearchResults
    {
      get => _streamSearchResults;
      set => SetAndNotify(ref _streamSearchResults, value);
    }

    private Stream _selectedStream;

    public Stream SelectedStream
    {
      get => _selectedStream;
      set
      {
        SetAndNotify(ref _selectedStream, value);
        NotifyOfPropertyChange(nameof(CanAddExistingStream));
        if ( SelectedStream == null )
          return;
        StreamQuery = SelectedStream.name;
      }
    }

    private async void SearchForStreams()
    {
      if ( StreamQuery == null || StreamQuery.Length <= 2 )
        return;

      try
      {
        var client = new Client(AccountToSendFrom);
        var streams = await client.StreamSearch(StreamQuery);
        StreamSearchResults = new BindableCollection<Stream>(streams);
      }
      catch ( Exception e )
      {
        // search prob returned no results
        StreamSearchResults?.Clear();
        Debug.WriteLine(e);
      }
    }

    #endregion

    public void ContinueStreamCreate(string slideIndex)
    {
      if ( StreamQuery == null || StreamQuery.Length < 2 )
      {
        Notifications.Enqueue("Please choose a name for your stream!");
        return;
      }

      StreamToCreate.name = StreamQuery;
      NotifyOfPropertyChange(nameof(StreamToCreate.name));

      SelectedStream = null;
      ChangeSlide(slideIndex);
    }

    public async void AddNewStream()
    {
      CreateButtonLoading = true;
      Tracker.TrackPageview(Tracker.STREAM_CREATE);
      var client = new Client(AccountToSendFrom);
      try
      {
        var streamId = await _streamsRepo.CreateStream(StreamToCreate, AccountToSendFrom);

        if ( Collaborators.Any() ) Tracker.TrackPageview("stream", "collaborators");
        foreach ( var user in Collaborators )
        {
          var res = await client.StreamGrantPermission(new StreamGrantPermissionInput()
          {
            streamId = streamId, userId = user.id, role = "stream:contributor"
          });
        }

        var filter = SelectedFilterTab.Filter;
        switch ( filter.Name )
        {
          case "View":
          case "Category":
          case "Selection" when SelectedFilterTab.ListItems.Any():
            filter.Selection = SelectedFilterTab.ListItems.ToList();
            break;
        }

        StreamToCreate = await _streamsRepo.GetStream(streamId, AccountToSendFrom);
        StreamState = new StreamState(client, StreamToCreate) {Filter = filter};
        Bindings.AddNewStream(StreamState);

        _events.Publish(new StreamAddedEvent() {NewStream = StreamState});
        StreamState = new StreamState();
        CloseDialog();
      }
      catch ( Exception e )
      {
        await client.StreamDelete(StreamToCreate.id);
        Notifications.Enqueue($"Error: {e.Message}");
      }

      CreateButtonLoading = false;
    }

    public bool CanAddExistingStream => SelectedStream != null;

    public async void AddExistingStream()
    {
      if ( StreamIds.Contains(SelectedStream.id) )
      {
        Notifications.Enqueue("This stream already exists in this file");
        return;
      }

      AddExistingButtonLoading = true;
      Tracker.TrackPageview(Tracker.STREAM_GET);

      var client = new Client(AccountToSendFrom);
      StreamToCreate = await client.StreamGet(SelectedStream.id);

      StreamState = new StreamState(client, StreamToCreate) {ServerUpdates = true};
      Bindings.AddNewStream(StreamState);
      _events.Publish(new StreamAddedEvent() {NewStream = StreamState});

      AddExistingButtonLoading = false;
      CloseDialog();
    }

    public void AddSimpleStream()
    {
      CreateButtonLoading = true;
      Tracker.TrackPageview("stream", "from-selection");
      SelectedFilterTab = FilterTabs.First(tab => tab.Filter.Name == "Selection");
      SelectedFilterTab.ListItems.Clear();
      SelectedFilterTab.Filter.Selection = Bindings.GetSelectedObjects();
      StreamToCreate.name = StreamQuery;
      SelectedStream = null;

      AddNewStream();
    }

    public void AddStreamFromView()
    {
      Tracker.TrackPageview("stream", "from-view");
      SelectedFilterTab = FilterTabs.First(tab => tab.Filter.Name == "Selection");
      SelectedFilterTab.Filter.Selection = ActiveViewObjects;

      AddNewStream();
    }

    public void ChangeSlide(string slideIndex)
    {
      SelectedSlide = int.Parse(slideIndex);
    }

    public void Handle(RetrievedFilteredObjectsEvent message)
    {
      StreamState.Placeholders = message.Objects.ToList();
    }

    public void Handle(UpdateSelectionEvent message)
    {
      var selectionFilter = FilterTabs.First(tab => tab.Filter.Name == "Selection");
      selectionFilter.Filter.Selection = message.ObjectIds;

      SelectionCount = message.ObjectIds.Count;
    }

    public void Handle(ApplicationEvent message)
    {
      switch ( message.Type )
      {
        case ApplicationEvent.EventType.ViewActivated:
        {
          NotifyOfPropertyChange(nameof(ActiveViewName));
          NotifyOfPropertyChange(nameof(ActiveViewObjects));
          return;
        }
        case ApplicationEvent.EventType.DocumentClosed:
        {
          CloseDialog();
          return;
        }
        default:
          return;
      }
    }
  }
}
