using Avalonia;
using Avalonia.Controls;
using Avalonia.Metadata;
using DesktopUI2.Models;
using DesktopUI2.Models.Filters;
using DesktopUI2.Models.Settings;
using DesktopUI2.ViewModels.Share;
using DesktopUI2.Views.Pages;
using DesktopUI2.Views.Windows;
using DynamicData;
using Material.Icons;
using Material.Icons.Avalonia;
using ReactiveUI;
using Speckle.Core.Api;
using Speckle.Core.Kits;
using Speckle.Core.Logging;
using Speckle.Core.Models;
using Splat;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Reactive;
using System.Threading.Tasks;
using System.Windows.Input;
using Stream = Speckle.Core.Api.Stream;

namespace DesktopUI2.ViewModels
{
  public class MappingViewModel : ReactiveObject, IRoutableViewModel
  {
    public string UrlPathSegment => throw new NotImplementedException();

    public IScreen HostScreen => throw new NotImplementedException();

    public ReactiveCommand<Unit, Unit> GoBack => MainViewModel.RouterInstance.NavigateBack;

    public List<string> SearchResults { get; set; }
    public Dictionary<string, string> mapping { get; set; }
    public List<string> tabs { get; set; }

    public event EventHandler OnRequestClose;

    public MappingViewModel()
    {
      mapping = new Dictionary<string, string>
      {
        { "W12x19", "W12x19" },
        { "Type1", "type123" },
        { "anotherType", "anotherType" },
        { "yetAnotherType", "differentType" },
        { "short", "short" },
        { "a very very very long type name. Oh no", "a very very very long type name. Oh no" }
      };
    }

    //public void hey()
    //{
    //  if (receiveMappings == true)
    //  {
    //    var listProperties = GetListProperties(objects);
    //    var listHostProperties = GetHostDocumentPropeties(CurrentDoc.Document);
    //    var mappings = returnFirstPassMap(listProperties, listHostProperties);
    //    //User to update logic from computer here;

    //    //var vm = new MappingViewModel(mappings);
    //    //var mappingView = new MappingView
    //    //{
    //    //  DataContext = vm
    //    //};

    //    //mappingView.ShowDialog(MainWindow.Instance);
    //    //vm.OnRequestClose += (s, e) => mappingView.Close();
    //    //var newMappings = await mappingView.ShowDialog<Dictionary<string, string>?>(MainWindow.Instance);
    //    //System.Diagnostics.Debug.WriteLine($"new mappings {newMappings}");

    //    updateRecieveObject(mappings, objects);

    //  }
    //}

    //public Base GetLatestCommit()
    //{
    //  var transport = new ServerTransport(state.Client.Account, state.StreamId);

    //  var stream = await state.Client.StreamGet(state.StreamId);

    //  if (progress.CancellationTokenSource.Token.IsCancellationRequested)
    //  {
    //    return null;
    //  }

    //  Commit myCommit = null;
    //  //if "latest", always make sure we get the latest commit when the user clicks "receive"
    //  if (state.CommitId == "latest")
    //  {
    //    var res = await state.Client.BranchGet(progress.CancellationTokenSource.Token, state.StreamId, state.BranchName, 1);
    //    myCommit = res.commits.items.FirstOrDefault();
    //  }
    //}
    public MappingViewModel(Dictionary<string, string> firstPassMapping)
    {
      mapping = firstPassMapping;
    }

    public void Close_Click()
    {
      OnRequestClose(this, new EventArgs());
    }
  }
}
