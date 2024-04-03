using System.Collections.Generic;
using System.Linq;

namespace Speckle.Converters.RevitShared.Helpers;

// POC: why do we need this send selection?
// why does conversion need to know about selection in this way?
public class SendSelection
{
  private readonly HashSet<string> _selectedItemIds;

  public SendSelection(IEnumerable<string> selectedItemIds)
  {
    _selectedItemIds = new HashSet<string>(selectedItemIds);
  }

  public bool Contains(string uniqueId) => _selectedItemIds.Contains(uniqueId);

  public IReadOnlyCollection<string> SelectedItems => _selectedItemIds.ToList().AsReadOnly();
}
