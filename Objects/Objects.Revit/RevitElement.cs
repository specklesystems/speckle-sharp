using Speckle.Core.Kits;
using Speckle.Core.Models;
using System.Collections.Generic;

namespace Objects.Revit
{
  /// <summary>
  /// Something, anyting in Revit, has an element id.
  /// <para>Note: the uniqueId is captured in the Base's object applicationId. This elementId is captured for selection purposes.</para>
  /// </summary>
  public interface IBaseRevitElement
  {
    string elementId { get; set; }
  }

  /// <summary>
  /// Describes family-like objects.
  /// </summary>
  public interface IRevitHasFamilyAndType : IBaseRevitElement
  {
    string family { get; set; }

    string type { get; set; }
  }

  /// <summary>
  /// Objects having a parameters property.
  /// </summary>
  public interface IRevitHasParameters : IBaseRevitElement
  {
    Dictionary<string, object> parameters { get; set; }

  }

  public interface IRevitHasTypeParameters : IBaseRevitElement
  {
    Dictionary<string, object> typeParameters { get; set; }
  }

}
