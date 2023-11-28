using System.Collections.Generic;

namespace RevitSharedResources.Interfaces;

/// <summary>
/// Objects that implement the IConvertedObjectsCache interface are responsible for
/// querying and mutating a cache of objects that have been converted during the current converion operation.
/// This object will then get passed into an IReceivedObjectsCache to be saved
/// </summary>
public interface IConvertedObjectsCache<TFrom, TTo>
{
  #region Add
  public void AddConvertedObjects(TFrom converted, IList<TTo> created);
  #endregion

  #region Query

  #region GetConverted
  public IEnumerable<TFrom> GetConvertedObjects();
  public IEnumerable<TFrom> GetConvertedObjectsFromCreatedId(string id);
  public bool HasConvertedObjectWithId(string id);
  #endregion

  #region GetCreated
  public IEnumerable<TTo> GetCreatedObjects();
  public IEnumerable<TTo> GetCreatedObjectsFromConvertedId(string id);
  public bool HasCreatedObjectWithId(string id);
  #endregion

  #endregion
}
