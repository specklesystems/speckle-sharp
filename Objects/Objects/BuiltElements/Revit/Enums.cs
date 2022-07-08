using System.Collections.Generic;

namespace Objects.BuiltElements.Revit
{
  //This is an enum so that we can easily create a dropdown in GH for schema builder
  //NOTE: if edited the list in Objects.Converter.Revit.Categories should be updated too
  public enum RevitCategory
  {
    Casework,
    Ceilings,
    Columns,
    CurtainPanels,
    CurtainSystems,
    CurtainWallMullions,
    Doors,
    Entourage,
    Fascias,
    Floors,
    Furniture,
    FurnitureSystems,
    GenericModels,
    Gutters,
    Landings,
    Mass,
    Materials,
    Railings,
    Ramps,
    Rebar,
    Roads,
    RoofSoffits,
    Roofs,
    Runs,
    Site,
    SpecialtyEquipment,
    Stairs,
    StructuralAreaReinforcement,
    StructuralBeamSystems,
    StructuralColumns,
    StructuralConnections,
    StructuralFoundations,
    StructuralFraming,
    StructuralRebar,
    StructuralStiffeners,
    StructuralTrusses,
    Supports,
    Walls,
    Windows
  }

  public enum LocationLine
  {
    Centerline,
    Exterior,
    Interior,
  }
}
