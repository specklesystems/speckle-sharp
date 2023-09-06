#if ADVANCESTEEL
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using Autodesk.AdvanceSteel.ConstructionTypes;
using Autodesk.AdvanceSteel.Geometry;
using Autodesk.AdvanceSteel.Modelling;
using ASPoint3d = Autodesk.AdvanceSteel.Geometry.Point3d;
using System.Linq;
using static Autodesk.AdvanceSteel.DotNetRoots.Units.Unit;

namespace Objects.Converter.AutocadCivil;

public class ScrewBoltPatternProperties : ASBaseProperties<ScrewBoltPattern>, IASProperties
{
  public override Dictionary<string, ASProperty> BuildedPropertyList()
  {
    Dictionary<string, ASProperty> dictionary = new Dictionary<string, ASProperty>();

    InsertProperty(dictionary, "top tool diameter", nameof(ScrewBoltPattern.TopToolDiameter), eUnitType.kDistance);
    InsertProperty(dictionary, "bottom tool diameter", nameof(ScrewBoltPattern.BottomToolDiameter), eUnitType.kDistance);
    InsertProperty(dictionary, "bottom tool height", nameof(ScrewBoltPattern.BottomToolHeight), eUnitType.kDistance);
    InsertProperty(dictionary, "head number of edges", nameof(ScrewBoltPattern.BoltHeadNumEdges));
    InsertProperty(dictionary, "head diameter", nameof(ScrewBoltPattern.BoltHeadDiameter), eUnitType.kDistance);
    InsertProperty(dictionary, "head height", nameof(ScrewBoltPattern.BoltHeadHeight), eUnitType.kDistance);
    InsertProperty(dictionary, "top tool height", nameof(ScrewBoltPattern.TopToolHeight), eUnitType.kDistance);
    InsertProperty(dictionary, "bolt assembly", nameof(ScrewBoltPattern.BoltAssembly));
    InsertProperty(dictionary, "grade", nameof(ScrewBoltPattern.Grade));
    InsertProperty(dictionary, "standard", nameof(ScrewBoltPattern.Standard));
    InsertProperty(dictionary, "hole tolerance", nameof(ScrewBoltPattern.HoleTolerance), eUnitType.kDistance);
    InsertProperty(dictionary, "binding length addition", nameof(ScrewBoltPattern.BindingLengthAddition), eUnitType.kDistance);
    InsertProperty(dictionary, "annotation", nameof(ScrewBoltPattern.Annotation));
    InsertProperty(dictionary, "screw length", nameof(ScrewBoltPattern.ScrewLength), eUnitType.kDistance);
    InsertProperty(dictionary, "sum top height", nameof(ScrewBoltPattern.SumTopHeight), eUnitType.kDistance);
    InsertProperty(dictionary, "sum top set height", nameof(ScrewBoltPattern.SumTopSetHeight), eUnitType.kDistance);
    InsertProperty(dictionary, "sum bottom set height", nameof(ScrewBoltPattern.SumBottomSetHeight), eUnitType.kDistance);
    InsertProperty(dictionary, "sum bottom height", nameof(ScrewBoltPattern.SumBottomHeight), eUnitType.kDistance);
    InsertProperty(dictionary, "max top diameter", nameof(ScrewBoltPattern.MaxTopDiameter), eUnitType.kDistance);
    InsertProperty(dictionary, "max bottom diameter", nameof(ScrewBoltPattern.MaxBottomDiameter), eUnitType.kDistance);
    InsertProperty(dictionary, "nut height", nameof(ScrewBoltPattern.NutHeight), eUnitType.kDistance);
    InsertProperty(dictionary, "nut diameter", nameof(ScrewBoltPattern.NutDiameter), eUnitType.kDistance);
    InsertProperty(dictionary, "screw diameter", nameof(ScrewBoltPattern.ScrewDiameter), eUnitType.kDistance);
    InsertProperty(dictionary, "binding length", nameof(ScrewBoltPattern.BindingLength), eUnitType.kDistance);
    InsertProperty(dictionary, "ignore max gap", nameof(ScrewBoltPattern.IgnoreMaxGap));
    InsertProperty(dictionary, "weight", nameof(ScrewBoltPattern.GetWeight), eUnitType.kWeight);
    InsertProperty(dictionary, "screw bolt type", nameof(ScrewBoltPattern.ScrewBoltType));
    InsertProperty(dictionary, "assembly location", nameof(ScrewBoltPattern.AssemblyLocation));

    return dictionary;
  }
}
#endif
