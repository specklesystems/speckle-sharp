using DesktopUI2.Models;
using DesktopUI2.ViewModels;
using Speckle.Core.Models;
using System.Threading.Tasks;

namespace ConnectorRevit.Services
{
  /// <summary>
  /// Responsible for receiving the desired commit's referenced object
  /// </summary>
  public interface ISpeckleObjectReceiver
  {
    Task<Base> ReceiveCommitObject(StreamState state, ProgressViewModel progress);
  }
}
