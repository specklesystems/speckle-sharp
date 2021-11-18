using ReactiveUI;
using Speckle.Core.Api;
using Speckle.Core.Credentials;
using System;
using System.Collections.Generic;
using System.Text;

namespace DesktopUI2.ViewModels
{
  /// <summary>
  /// Common base for all Stream View Models
  /// </summary>
  public class StreamViewModelBase : ReactiveObject
  {
    // private Account _account;
    // public Account Account
    // {
    //   get => _account;
    //   internal set => this.RaiseAndSetIfChanged(ref _account, value);
    // }

    private Stream _stream;
    public Stream Stream
    {
      get => _stream;
      internal set => this.RaiseAndSetIfChanged(ref _stream, value);
    }

    private ProgressViewModel _progress = new ProgressViewModel();
    public ProgressViewModel Progress
    {
      get => _progress;
      set => this.RaiseAndSetIfChanged(ref _progress, value);
    }
  }
}
