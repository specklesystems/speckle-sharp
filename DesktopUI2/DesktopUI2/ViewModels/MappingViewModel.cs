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
