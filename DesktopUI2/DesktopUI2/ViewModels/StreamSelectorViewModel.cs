using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls.Selection;
using Avalonia.Threading;
using DesktopUI2.Models;
using DesktopUI2.ViewModels.MappingTool;
using ReactiveUI;
using Serilog.Events;
using Speckle.Core.Api;
using Speckle.Core.Credentials;
using Speckle.Core.Logging;

namespace DesktopUI2.ViewModels;

public class StreamSelectorViewModel : ReactiveObject
{
  public StreamSelectorViewModel()
  {
    streamSearchDebouncer = Utils.Debounce(SearchStreams, 500);
    Accounts = AccountManager.GetAccounts().Select(x => new AccountViewModel(x)).ToList();
    GetStreams().ConfigureAwait(true);
  }

  //private void SelectionModel_SelectionChanged(object sender, SelectionModelSelectionChangedEventArgs<AccountViewModel> e)
  //{
  //  this.RaisePropertyChanged("HasSelectedUsers");
  //}

  private void SearchStreams()
  {
    GetStreams().ConfigureAwait(true);
  }

  private async Task GetStreams()
  {
    if (!Accounts.Any())
    {
      return;
    }

    ShowProgress = true;

    //needed for the search feature
    StreamGetCancelTokenSource?.Cancel();
    StreamGetCancelTokenSource = new CancellationTokenSource();

    var streams = new List<StreamAccountWrapper>();

    foreach (var account in Accounts)
    {
      if (StreamGetCancelTokenSource.IsCancellationRequested)
      {
        return;
      }

      try
      {
        var result = new List<Stream>();

        //NO SEARCH
        if (string.IsNullOrEmpty(SearchQuery))
        {
          result = await account.Client.StreamsGet(25, StreamGetCancelTokenSource.Token).ConfigureAwait(true);
        }
        //SEARCH
        else
        {
          result = await account.Client
            .StreamSearch(SearchQuery, 25, StreamGetCancelTokenSource.Token)
            .ConfigureAwait(true);
        }

        if (StreamGetCancelTokenSource.IsCancellationRequested)
        {
          return;
        }

        streams.AddRange(result.Select(x => new StreamAccountWrapper(x, account.Account)));
      }
      catch (Exception ex)
      {
        bool isError = !(ex is TaskCanceledException || ex.InnerException is TaskCanceledException);
        var logLevel = isError ? LogEventLevel.Error : LogEventLevel.Debug;
        SpeckleLog.Logger.Write(logLevel, ex, "Failed to fetch streams {exceptionMessage} for account", ex.Message);
      }
    }
    if (StreamGetCancelTokenSource.IsCancellationRequested)
    {
      return;
    }

    try
    {
      Streams = streams.OrderByDescending(x => x.Stream.updatedAt).ToList();
    }
    catch (Exception ex)
    {
      SpeckleLog.Logger.Fatal(ex, "Failed to fetch streams  {exceptionMessage}", ex.Message);
    }
    finally
    {
      ShowProgress = false;
    }
  }

  private async void GetBranches()
  {
    using var client = new Client(SelectedStream.Account);

    Branches = (await client.StreamGetBranchesWithLimitRetry(SelectedStream.Stream.id, 1).ConfigureAwait(true))
      .Where(x => x.commits.totalCount > 0)
      .ToList();

    if (Branches.Any())
    {
      SelectedBranch = Branches.FirstOrDefault();
    }
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

    Dispatcher.UIThread.InvokeAsync(() => Task.Run(() => MappingsViewModel.Instance.OnBranchSelected()));
  }

  private void CancelCommand()
  {
    IsVisible = false;
  }

  #region bindings

  private Action streamSearchDebouncer;

  private bool _noSearch;

  private bool _isVisible;

  public bool IsVisible
  {
    get => _isVisible;
    set => this.RaiseAndSetIfChanged(ref _isVisible, value);
  }

  public SelectionModel<AccountViewModel> SelectionModel { get; private set; }

  private List<AccountViewModel> _accounts = new();

  public List<AccountViewModel> Accounts
  {
    get => _accounts;
    private set => this.RaiseAndSetIfChanged(ref _accounts, value);
  }

  private string _searchQuery = "";

  public string SearchQuery
  {
    get => _searchQuery;
    set
    {
      this.RaiseAndSetIfChanged(ref _searchQuery, value);
      if (_noSearch)
      {
        return;
      }

      if (!string.IsNullOrEmpty(SearchQuery) && SearchQuery.Length <= 2)
      {
        return;
      }

      streamSearchDebouncer();
    }
  }

  private List<StreamAccountWrapper> _streams;

  public List<StreamAccountWrapper> Streams
  {
    get => _streams;
    private set => this.RaiseAndSetIfChanged(ref _streams, value);
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
    private set => this.RaiseAndSetIfChanged(ref _branches, value);
  }

  private Branch _selectedBranch;

  public Branch SelectedBranch
  {
    get => _selectedBranch;
    private set => this.RaiseAndSetIfChanged(ref _selectedBranch, value);
  }

  private bool _showProgress;

  public bool ShowProgress
  {
    get => _showProgress;
    private set => this.RaiseAndSetIfChanged(ref _showProgress, value);
  }

  private CancellationTokenSource StreamGetCancelTokenSource;

  #endregion
}
