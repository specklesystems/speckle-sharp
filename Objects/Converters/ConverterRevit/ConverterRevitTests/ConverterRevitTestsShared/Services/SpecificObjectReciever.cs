using System.Threading.Tasks;
using ConnectorRevit.Services;
using DesktopUI2.Models;
using DesktopUI2.ViewModels;
using Speckle.Core.Models;

namespace ConverterRevitTestsShared.Services
{
  internal class SpecificObjectReciever : ISpeckleObjectReceiver
  {
    private Base @base;

    public SpecificObjectReciever(Base @base)
    {
      this.@base = @base;
    }

    public async Task<Base> ReceiveCommitObject(StreamState state, ProgressViewModel progress)
    {
      return @base;
    }
  }
}
