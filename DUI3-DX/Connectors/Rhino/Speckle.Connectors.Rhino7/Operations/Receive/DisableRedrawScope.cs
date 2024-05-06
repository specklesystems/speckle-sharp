using System;
using Rhino.DocObjects.Tables;

namespace Speckle.Connectors.Rhino7.Operations.Receive;

/// <summary>
/// Helper class to disable <see cref="ViewTable.RedrawEnabled"/> within a scope
/// </summary>
public sealed class DisableRedrawScope : IDisposable
{
  private readonly ViewTable _viewTable;
  private readonly bool _returnToStatus;

  public DisableRedrawScope(ViewTable viewTable, bool returnToStatus = true)
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
