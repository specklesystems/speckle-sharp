using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DesktopUI2.Models;
using DesktopUI2.ViewModels;
using Speckle.Core.Kits;

namespace Speckle.ConnectorNavisworks.Bindings;

public partial class ConnectorBindingsNavisworks
{
  // There is no receive mode relevant for the Navisworks Connector at this time.


  public override bool CanPreviewReceive => false;

  public override bool CanReceive => false;

  public override List<ReceiveMode> GetReceiveModes() => null;

  public override async Task<StreamState> PreviewReceive(StreamState state, ProgressViewModel progress)
  {
    await Task.Delay(0).ConfigureAwait(false);
    return null;
  }

  public override async Task<StreamState> ReceiveStream(StreamState state, ProgressViewModel progress)
  {
    await Task.Delay(TimeSpan.FromMilliseconds(500)).ConfigureAwait(false);
    return state;
  }
}
