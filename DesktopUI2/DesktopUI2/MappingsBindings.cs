using Speckle.Core.Models;
using System.Collections.Generic;

namespace DesktopUI2
{

  public abstract class MappingsBindings
  {
    /// <summary>
    /// Notifies that the selection has changed
    /// </summary>
    /// <param name="objects"></param>
    public delegate void UpdateSelection(List<object> objects);

    /// <summary>
    /// Gets the selected objects in the host application
    /// </summary>
    /// <returns></returns>
    public abstract List<Base> GetSelection();

    /// <summary>
    /// Sets the mappings on the current selection
    /// </summary>
    public abstract void SetMappings(List<object> objects, string schema);
  }
}
