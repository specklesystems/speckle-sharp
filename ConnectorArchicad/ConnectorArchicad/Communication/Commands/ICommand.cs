using System.Threading.Tasks;
using Speckle.Core.Logging;

namespace Archicad.Communication.Commands
{
  internal interface ICommand<TResult>
  {
    Task<TResult> Execute();
  }
}
