using Objects.Other;

namespace Objects.BuiltElements.Revit.Interfaces
{
  public interface IRevitFamilyInstance : IHasRevitSymbolType
  {
    bool facingFlipped { get; set; }
    string placementType { get; }
    bool handFlipped { get; set; }
    bool mirrored { get; set; }
    Level level { get; set; }
    Transform transform { get; set; }
  }
}
