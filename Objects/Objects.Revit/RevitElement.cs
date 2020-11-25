using Speckle.Core.Kits;
using Speckle.Core.Models;
using System.Collections.Generic;

namespace Objects.Revit
{
  public interface IBaseRevitElement
  {
    string elementId { get; set; }
  }

  //public interface IRevitElement : IBaseRevitElement
  //{
  //  string family { get; set; }

  //  string type { get; set; }

  //  Dictionary<string, object> parameters { get; set; }

  //  Dictionary<string, object> typeParameters { get; set; }
  //}

  public interface IRevitHasFamilyAndType : IBaseRevitElement
  {
    string family { get; set; }

    string type { get; set; }
  }

  public interface IRevitHasParameters : IBaseRevitElement
  {
    Dictionary<string, object> parameters { get; set; }

  }

  public interface IRevitHasTypeParameters : IBaseRevitElement
  {
    Dictionary<string, object> typeParameters { get; set; }
  }

}
