
using Speckle.Rhino7.Interfaces;

namespace Speckle.Connectors.Rhino7.Operations.Receive;

/// <summary>
/// Helper class to disable <see cref="IRhinoViewTable.RedrawEnabled"/> within a scope
/// </summary>
public sealed class DisableRedrawScope : IDisposable
{
  private readonly IRhinoViewTable _viewTable;
  private readonly bool _returnToStatus;

  public DisableRedrawScope(IRhinoViewTable viewTable, bool returnToStatus = true)
  {
    _viewTable = viewTable;
    _returnToStatus = returnToStatus;

    _viewTable.RedrawEnabled = false;
  }

  public void Dispose()
  {
    _viewTable.RedrawEnabled = _returnToStatus;
  }
}
