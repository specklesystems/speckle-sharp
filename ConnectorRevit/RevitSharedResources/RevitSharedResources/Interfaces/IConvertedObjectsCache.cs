using System;
using System.Collections.Generic;
using Speckle.Core.Models;

namespace RevitSharedResources.Interfaces
{
  public interface IConvertedObjectsCache
  {
    public void AddReceivedElements(List<object> elements, Base @base);
    public IEnumerable<Base> GetConvertedBaseObjects();
    public IList<object> GetConvertedObjectsFromApplicationId(string applicationId);
    public IEnumerable<object> GetConvertedObjects();
    public bool ContainsApplicationId(string applicationId);
  }
}
