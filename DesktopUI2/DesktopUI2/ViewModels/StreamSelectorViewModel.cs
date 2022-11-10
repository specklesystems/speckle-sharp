using Avalonia.Controls.Selection;
using DesktopUI2.Models;
using DesktopUI2.ViewModels.MappingTool;
using ReactiveUI;
using Speckle.Core.Api;
using Speckle.Core.Credentials;
using Speckle.Core.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DesktopUI2.ViewModels
{
  public class StreamSelectorViewModel : ReactiveObject
  {
    #region bindings

    private Action streamSearchDebouncer = null;

    private bool _noSearch = false;

    private bool _isVisible;
    public bool IsVisible
    {
      get => _isVisible;
      set
      {
        this.RaiseAndSetIfChanged(ref _isVisible, value);
      }
    }
    public SelectionModel<AccountViewModel> SelectionModel { get; private set; }

    private List<AccountViewModel> _accounts = new List<AccountViewModel>();
    public List<AccountViewModel> Accounts
    {
      get => _accounts;
      private set
      {
        this.RaiseAndSetIfChanged(ref _accounts, value);
      }
    }

    private string _searchQuery = "";

    public string SearchQuery
    {
      get => _searchQuery;
      set
      {
        this.RaiseAndSetIfChanged(ref _searchQuery, value);
        if (_noSearch)
          return;
        if (!string.IsNullOrEmpty(SearchQuery) && SearchQuery.Length <= 2)
          return;

        streamSearchDebouncer();
      }
    }
    private List<StreamAccountWrapper> _streams;
    public List<StreamAccountWrapper> Streams
    {
      get => _streams;
      private set
      {
        this.RaiseAndSetIfChanged(ref _streams, value);
      }
    }

    private StreamAccountWrapper _selectedStream;
    public StreamAccountWrapper SelectedStream
    {
      get => _selectedStream;
      private set
      {
        this.RaiseAndSetIfChanged(ref _selectedStream, value);

        if (value != null)
        {
          _noSearch = true;
          SearchQuery = _selectedStream.Stream.name;
          _noSearch = false;
          GetBranches();
        }

      }
    }

    private List<Branch> _branches;
    public List<Branch> Branches
    {
      get => _branches;
      private set
      {
        this.RaiseAndSetIfChanged(ref _branches, value);
      }
    }

    private Branch _selectedBranch;
    public Branch SelectedBranch
    {
      get => _selectedBranch;
      private set
      {
        this.RaiseAndSetIfChanged(ref _selectedBranch, value);
      }
    }

    private bool _showProgress;
    public bool ShowProgress
    {
      get => _showProgress;
      private set
      {
        this.RaiseAndSetIfChanged(ref _showProgress, value);
      }
    }


    private CancellationTokenSource StreamGetCancelTokenSource = null;
    #endregion

    public StreamSelectorViewModel()
    {
      streamSearchDebouncer = Utils.Debounce(SearchStreams, 500);
      Accounts = AccountManager.GetAccounts().Select(x => new AccountViewModel(x)).ToList();
      GetStreams().ConfigureAwait(false);
    }


    //private void SelectionModel_SelectionChanged(object sender, SelectionModelSelectionChangedEventArgs<AccountViewModel> e)
    //{
    //  this.RaisePropertyChanged("HasSelectedUsers");
    //}

    private void SearchStreams()
    {
      GetStreams().ConfigureAwait(false);
    }

    private async Task GetStreams()
    {

      if (!Accounts.Any())
        return;

      ShowProgress = true;

      try
      {
        //needed for the search feature
        StreamGetCancelTokenSource?.Cancel();
        StreamGetCancelTokenSource = new CancellationTokenSource();

        var streams = new List<StreamAccountWrapper>();

        foreach (var account in Accounts)
        {
          if (StreamGetCancelTokenSource.IsCancellationRequested)
            return;

          try
          {
            var result = new List<Stream>();

            //NO SEARCH
            if (SearchQuery == "")
              result = await account.Client.StreamsGet(StreamGetCancelTokenSource.Token, 25);
            //SEARCH
            else
              result = await account.Client.StreamSearch(StreamGetCancelTokenSource.Token, SearchQuery, 25);

            if (StreamGetCancelTokenSource.IsCancellationRequested)
              return;

            streams.AddRange(result.Select(x => new StreamAccountWrapper(x, account.Account)));

          }
          catch (Exception e)
          {
            if (e.InnerException is System.Threading.Tasks.TaskCanceledException)
              return;
            Log.CaptureException(new Exception("Could not fetch streams", e), Sentry.SentryLevel.Error);
          }
        }
        if (StreamGetCancelTokenSource.IsCancellationRequested)
          return;

        Streams = streams.OrderByDescending(x => DateTime.Parse(x.Stream.updatedAt)).ToList();

      }
      catch (Exception e)
      {
        Log.CaptureException(new Exception("Could not fetch streams", e), Sentry.SentryLevel.Error);
      }
      finally
      {
        ShowProgress = false;
      }
    }

    private async void GetBranches()
    {
      var client = new Client(SelectedStream.Account);

      Branches = await client.StreamGetBranches(SelectedStream.Stream.id, 100, 0);

      if (Branches.Any())
        SelectedBranch = Branches.FirstOrDefault();
    }

    private void ClearSearchCommand()
    {
      SearchQuery = "";
      SelectedStream = null;
      SelectedBranch = null;
      Streams = new List<StreamAccountWrapper>();
    }

    private void OkCommand()
    {
      IsVisible = false;

      Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() => Task.Run(() => MappingsViewModel.Instance.OnBranchSelected()));
    }

    private void CancelCommand()
    {
      IsVisible = false;
    }
  }
}
