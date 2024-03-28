using System.Collections.Generic;
using System.Linq;

namespace Speckle.Converters.RevitShared.Helpers;

public class SendSelection
{
  private HashSet<string> _selectedItemIds;

  public SendSelection(IEnumerable<string> selectedItemIds)
  {
    _selectedItemIds = new HashSet<string>(selectedItemIds);
  }

  public bool Contains(string uniqueId) => _selectedItemIds.Contains(uniqueId);

  public IReadOnlyCollection<string> SelectedItems => _selectedItemIds.ToList().AsReadOnly();
}
