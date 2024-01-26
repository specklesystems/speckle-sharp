using System.Threading.Tasks;

namespace Archicad.Communication.Commands;

internal interface ICommand<TResult>
{
  Task<TResult> Execute();
}
