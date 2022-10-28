using System.Collections.Generic;
using Speckle.Core.Models;

namespace Objects.Organization
{

  /// <summary>
  /// A simple organization object that links multiple speckle objects into one bundle.
  /// All linked objects must be situated on the same server
  /// </summary>
  public class Bundle : Base
  {
    /// <summary>
    /// Name of the bundle
    /// </summary>
    public string name { get; set; }

    /// <summary>
    /// A list of reference object Ids to link together
    /// Collection key is the streamId associated with the list of objects to use 
    /// </summary>
    [DetachProperty] public List<BundleItem> items { get; set; }

    /// <summary>
    /// 
    /// </summary>
    public Bundle()
    {
    }

    /// <summary>
    /// Basic constructor 
    /// </summary>
    /// <param name="name"></param>
    /// <param name="items"></param>
    public Bundle(string name, List<BundleItem> items)
    {
      this.name = name;
      this.items = items;
    }



    /// <summary>
    /// Checks if <see cref="name"/> and <see cref="items"/> are similar
    /// </summary>
    /// <param name="obj"></param>
    /// <returns></returns>
    public bool Equals(Bundle obj)
    {
      // check if name are similar along with collection of objects
      if (!name.Equals(obj.name) || obj.items?.Count != items?.Count)
        return false;

      foreach (var item in items)
      {
        BundleItem itemToFind = null;

        foreach (var o in obj.items)
        {
          if (!o.streamId.Equals(item.streamId))
            continue;

          itemToFind = o;
          break;
        }

        if (itemToFind == null || itemToFind.objectIds?.Count != item.objectIds?.Count)
          return false;

        // check all ids
        for (var i = 0; i < item.objectIds.Count; i++)
        {
          // TODO: Might get reordered somehow
          if (!item.objectIds[i].Equals(itemToFind.objectIds[i]))
            return false;
        }
      }

      return true;
    }
  }
}
