using Speckle.Core.Models;

namespace Objects.BuiltElements.Archicad.Model
{
  public interface IArchicadElementBaseData
  {
    string elementId { get; set; }
    int? floorIndex { get; set; }
  }
}
