using System;
using System.Threading.Tasks;
using ConnectorRevit.Services;
using DesktopUI2.Models;
using DesktopUI2.ViewModels;
using Speckle.Core.Models;

namespace ConverterRevitTestsShared.Services
{
  internal class SpeckleObjectLocalReceiver : ISpeckleObjectReceiver
  {
    public Task<Base> ReceiveCommitObject(StreamState state, ProgressViewModel progress)
    {
      throw new NotImplementedException();
    }
  }
}
