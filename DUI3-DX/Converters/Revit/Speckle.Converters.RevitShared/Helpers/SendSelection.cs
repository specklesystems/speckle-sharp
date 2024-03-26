using System.Collections.Generic;

namespace Speckle.Converters.RevitShared.Helpers;

public class SendSelection
{
  private HashSet<string> _selectedItemIds;

  public bool Contains(string uniqueId) => _selectedItemIds.Contains(uniqueId);

  public void SetSelection(IEnumerable<string> itemIds)
  {
    if (_selectedItemIds != null)
    {
      throw new System.Exception("POC : make more specific exception");
    }
    _selectedItemIds = new(itemIds);
  }
}
