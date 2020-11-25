using Speckle.Core.Kits;
using Speckle.Core.Models;
using System.Collections.Generic;

namespace Objects.Revit
{
  public interface IBaseRevitElement
  {
    string elementId { get; set; }
  }

  public interface IRevitElement : IBaseRevitElement
  {
    string family { get; set; }

    string type { get; set; }

    Dictionary<string, object> parameters { get; set; }

    Dictionary<string, object> typeParameters { get; set; }
  }
}
