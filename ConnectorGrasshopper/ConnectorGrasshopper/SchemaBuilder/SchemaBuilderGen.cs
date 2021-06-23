

using System;
using Grasshopper.Kernel;

namespace ConnectorGrasshopper {

// This is generated code:
public class AdaptiveComponentSchemaComponent: CreateSchemaObjectBase {
     
    public AdaptiveComponentSchemaComponent(): base("AdaptiveComponent", "AdaptiveComponent", "Creates a Revit adaptive component by points", "Speckle 2 Revit", "Families") { }
    
    public override Guid ComponentGuid => new Guid("66b41bbe-c848-bd70-76b5-8fe4eecfb0fe");
    
    public override void AddedToDocument(GH_Document document){
        SelectedConstructor = CSOUtils.FindConstructor("Objects.BuiltElements.Revit.AdaptiveComponent.ctor(System.String,System.String,System.Collections.Generic.List`1[Objects.Geometry.Point],System.Boolean,System.Collections.Generic.List`1[Objects.BuiltElements.Revit.Parameter])","Objects.BuiltElements.Revit.AdaptiveComponent");
        base.AddedToDocument(document);
    }
}

// This is generated code:
public class AreaSchemaComponent: CreateSchemaObjectBase {
     
    public AreaSchemaComponent(): base("Area", "Area", "Creates a Speckle area", "Speckle 2 BIM", "Other") { }
    
    public override Guid ComponentGuid => new Guid("0a54d2aa-c137-9b5c-cf72-f2e9c6b39468");
    
    public override void AddedToDocument(GH_Document document){
        SelectedConstructor = CSOUtils.FindConstructor("Objects.BuiltElements.Area.ctor(System.String,System.String,Objects.BuiltElements.Level,Objects.Geometry.Point)","Objects.BuiltElements.Area");
        base.AddedToDocument(document);
    }
}

// This is generated code:
public class BeamSchemaComponent: CreateSchemaObjectBase {
     
    public BeamSchemaComponent(): base("Beam", "Beam", "Creates a Speckle beam", "Speckle 2 BIM", "Structure") { }
    
    public override Guid ComponentGuid => new Guid("d3efd475-31ec-685a-834c-a3dd5a39bd26");
    
    public override void AddedToDocument(GH_Document document){
        SelectedConstructor = CSOUtils.FindConstructor("Objects.BuiltElements.Beam.ctor(Objects.ICurve)","Objects.BuiltElements.Beam");
        base.AddedToDocument(document);
    }
}

// This is generated code:
public class BraceSchemaComponent: CreateSchemaObjectBase {
     
    public BraceSchemaComponent(): base("Brace", "Brace", "Creates a Speckle brace", "Speckle 2 BIM", "Structure") { }
    
    public override Guid ComponentGuid => new Guid("49d9db93-7293-978c-2adc-8048894ee6cc");
    
    public override void AddedToDocument(GH_Document document){
        SelectedConstructor = CSOUtils.FindConstructor("Objects.BuiltElements.Brace.ctor(Objects.ICurve)","Objects.BuiltElements.Brace");
        base.AddedToDocument(document);
    }
}

// This is generated code:
public class CeilingSchemaComponent: CreateSchemaObjectBase {
     
    public CeilingSchemaComponent(): base("Ceiling", "Ceiling", "Creates a Speckle ceiling", "Speckle 2 BIM", "Architecture") { }
    
    public override Guid ComponentGuid => new Guid("4ce489f1-0c17-6835-90aa-d8ddf8fe8ac9");
    
    public override void AddedToDocument(GH_Document document){
        SelectedConstructor = CSOUtils.FindConstructor("Objects.BuiltElements.Ceiling.ctor(Objects.ICurve,System.Collections.Generic.List`1[Objects.ICurve],System.Collections.Generic.List`1[Speckle.Core.Models.Base])","Objects.BuiltElements.Ceiling");
        base.AddedToDocument(document);
    }
}

// This is generated code:
public class ColumnSchemaComponent: CreateSchemaObjectBase {
     
    public ColumnSchemaComponent(): base("Column", "Column", "Creates a Speckle column", "Speckle 2 BIM", "Structure") { }
    
    public override Guid ComponentGuid => new Guid("03be61d3-61e5-a27d-85b8-3e0989306736");
    
    public override void AddedToDocument(GH_Document document){
        SelectedConstructor = CSOUtils.FindConstructor("Objects.BuiltElements.Column.ctor(Objects.ICurve)","Objects.BuiltElements.Column");
        base.AddedToDocument(document);
    }
}

// This is generated code:
public class DetailCurveSchemaComponent: CreateSchemaObjectBase {
     
    public DetailCurveSchemaComponent(): base("DetailCurve", "DetailCurve", "Creates a Revit detail curve", "Speckle 2 Revit", "Curves") { }
    
    public override Guid ComponentGuid => new Guid("ebf4ee5c-edb2-5648-02fc-170ae0fb1279");
    
    public override void AddedToDocument(GH_Document document){
        SelectedConstructor = CSOUtils.FindConstructor("Objects.BuiltElements.Revit.Curve.DetailCurve.ctor(Objects.ICurve,System.String,System.Collections.Generic.List`1[Objects.BuiltElements.Revit.Parameter])","Objects.BuiltElements.Revit.Curve.DetailCurve");
        base.AddedToDocument(document);
    }
}

// This is generated code:
public class DirectShapeSchemaComponent: CreateSchemaObjectBase {
     
    public DirectShapeSchemaComponent(): base("DirectShape by base geometries", "DirectShape by base geometries", "Creates a Revit DirectShape using a list of base geometry objects.", "Speckle 2 Revit", "Families") { }
    
    public override Guid ComponentGuid => new Guid("45d270ff-5730-a7fc-b2a9-04acda8e243d");
    
    public override void AddedToDocument(GH_Document document){
        SelectedConstructor = CSOUtils.FindConstructor("Objects.BuiltElements.Revit.DirectShape.ctor(System.String,Objects.BuiltElements.Revit.RevitCategory,System.Collections.Generic.List`1[Speckle.Core.Models.Base],System.Collections.Generic.List`1[Objects.BuiltElements.Revit.Parameter])","Objects.BuiltElements.Revit.DirectShape");
        base.AddedToDocument(document);
    }
}

// This is generated code:
public class DuctSchemaComponent: CreateSchemaObjectBase {
     
    public DuctSchemaComponent(): base("Duct", "Duct", "Creates a Speckle duct", "Speckle 2 BIM", "MEP") { }
    
    public override Guid ComponentGuid => new Guid("db524fe6-10ff-13ec-2af5-4b900951ed14");
    
    public override void AddedToDocument(GH_Document document){
        SelectedConstructor = CSOUtils.FindConstructor("Objects.BuiltElements.Duct.ctor(Objects.Geometry.Line,System.Double,System.Double,System.Double,System.Double)","Objects.BuiltElements.Duct");
        base.AddedToDocument(document);
    }
}

// This is generated code:
public class FamilyInstanceSchemaComponent: CreateSchemaObjectBase {
     
    public FamilyInstanceSchemaComponent(): base("FamilyInstance", "FamilyInstance", "Creates a Revit family instance", "Speckle 2 Revit", "Families") { }
    
    public override Guid ComponentGuid => new Guid("35871f67-164c-9b4e-5fd4-67ed5b463eaa");
    
    public override void AddedToDocument(GH_Document document){
        SelectedConstructor = CSOUtils.FindConstructor("Objects.BuiltElements.Revit.FamilyInstance.ctor(Objects.Geometry.Point,System.String,System.String,Objects.BuiltElements.Level,System.Double,System.Boolean,System.Boolean,System.Collections.Generic.List`1[Objects.BuiltElements.Revit.Parameter])","Objects.BuiltElements.Revit.FamilyInstance");
        base.AddedToDocument(document);
    }
}

// This is generated code:
public class FloorSchemaComponent: CreateSchemaObjectBase {
     
    public FloorSchemaComponent(): base("Floor", "Floor", "Creates a Speckle floor", "Speckle 2 BIM", "Architecture") { }
    
    public override Guid ComponentGuid => new Guid("d12fb55d-e105-6dcf-a8f9-2e8cd89d3677");
    
    public override void AddedToDocument(GH_Document document){
        SelectedConstructor = CSOUtils.FindConstructor("Objects.BuiltElements.Floor.ctor(Objects.ICurve,System.Collections.Generic.List`1[Objects.ICurve],System.Collections.Generic.List`1[Speckle.Core.Models.Base])","Objects.BuiltElements.Floor");
        base.AddedToDocument(document);
    }
}

// This is generated code:
public class FreeformElementSchemaComponent: CreateSchemaObjectBase {
     
    public FreeformElementSchemaComponent(): base("Freeform element", "Freeform element", "Creates a Revit Freeform element using a Brep or a Mesh.", "Speckle 2 Revit", "Families") { }
    
    public override Guid ComponentGuid => new Guid("d452bb1d-1264-d6d9-5054-fa7c0c96f789");
    
    public override void AddedToDocument(GH_Document document){
        SelectedConstructor = CSOUtils.FindConstructor("Objects.BuiltElements.Revit.FreeformElement.ctor(Speckle.Core.Models.Base,System.Collections.Generic.List`1[Objects.BuiltElements.Revit.Parameter])","Objects.BuiltElements.Revit.FreeformElement");
        base.AddedToDocument(document);
    }
}

// This is generated code:
public class GridLineSchemaComponent: CreateSchemaObjectBase {
     
    public GridLineSchemaComponent(): base("GridLine", "GridLine", "Creates a Speckle grid line", "Speckle 2 BIM", "Other") { }
    
    public override Guid ComponentGuid => new Guid("a7b6381f-a03c-0fc1-51e3-9e71260a7a7f");
    
    public override void AddedToDocument(GH_Document document){
        SelectedConstructor = CSOUtils.FindConstructor("Objects.BuiltElements.GridLine.ctor(Objects.ICurve)","Objects.BuiltElements.GridLine");
        base.AddedToDocument(document);
    }
}

// This is generated code:
public class LevelSchemaComponent: CreateSchemaObjectBase {
     
    public LevelSchemaComponent(): base("Level", "Level", "Creates a Speckle level", "Speckle 2 BIM", "Architecture") { }
    
    public override Guid ComponentGuid => new Guid("6931f911-a3e4-60cc-5d67-7df6761934b7");
    
    public override void AddedToDocument(GH_Document document){
        SelectedConstructor = CSOUtils.FindConstructor("Objects.BuiltElements.Level.ctor(System.String,System.Double)","Objects.BuiltElements.Level");
        base.AddedToDocument(document);
    }
}

// This is generated code:
public class ModelCurveSchemaComponent: CreateSchemaObjectBase {
     
    public ModelCurveSchemaComponent(): base("ModelCurve", "ModelCurve", "Creates a Revit model curve", "Speckle 2 Revit", "Curves") { }
    
    public override Guid ComponentGuid => new Guid("9d8117ef-d131-7c7f-c2d8-7fa144da0cfe");
    
    public override void AddedToDocument(GH_Document document){
        SelectedConstructor = CSOUtils.FindConstructor("Objects.BuiltElements.Revit.Curve.ModelCurve.ctor(Objects.ICurve,System.String,System.Collections.Generic.List`1[Objects.BuiltElements.Revit.Parameter])","Objects.BuiltElements.Revit.Curve.ModelCurve");
        base.AddedToDocument(document);
    }
}

// This is generated code:
public class ParameterSchemaComponent: CreateSchemaObjectBase {
     
    public ParameterSchemaComponent(): base("Parameter", "Parameter", "A Revit instance parameter to set on an element", "Speckle 2 Revit", "Families") { }
    
    public override Guid ComponentGuid => new Guid("fd9a30a7-a337-1d3f-d7d7-947d5f0c8421");
    
    public override void AddedToDocument(GH_Document document){
        SelectedConstructor = CSOUtils.FindConstructor("Objects.BuiltElements.Revit.Parameter.ctor(System.String,System.Object,System.String)","Objects.BuiltElements.Revit.Parameter");
        base.AddedToDocument(document);
    }
}

// This is generated code:
public class ParameterUpdaterSchemaComponent: CreateSchemaObjectBase {
     
    public ParameterUpdaterSchemaComponent(): base("ParameterUpdater", "ParameterUpdater", "Updates parameters on a Revit element by id", "Speckle 2 Revit", "Families") { }
    
    public override Guid ComponentGuid => new Guid("206e574f-749f-6ec4-0f83-470e33ac2d26");
    
    public override void AddedToDocument(GH_Document document){
        SelectedConstructor = CSOUtils.FindConstructor("Objects.BuiltElements.Revit.ParameterUpdater.ctor(System.String,System.Collections.Generic.List`1[Objects.BuiltElements.Revit.Parameter])","Objects.BuiltElements.Revit.ParameterUpdater");
        base.AddedToDocument(document);
    }
}

// This is generated code:
public class PipeSchemaComponent: CreateSchemaObjectBase {
     
    public PipeSchemaComponent(): base("Pipe", "Pipe", "Creates a Speckle pipe", "Speckle 2 BIM", "MEP") { }
    
    public override Guid ComponentGuid => new Guid("fb5f2554-9846-3779-417c-2d61b084e899");
    
    public override void AddedToDocument(GH_Document document){
        SelectedConstructor = CSOUtils.FindConstructor("Objects.BuiltElements.Pipe.ctor(Objects.Geometry.Line,System.Double,System.Double,System.Double,System.Double)","Objects.BuiltElements.Pipe");
        base.AddedToDocument(document);
    }
}

// This is generated code:
public class RenderMaterialSchemaComponent: CreateSchemaObjectBase {
     
    public RenderMaterialSchemaComponent(): base("RenderMaterial", "RenderMaterial", "Creates a render material.", "Speckle 2 BIM", "Other") { }
    
    public override Guid ComponentGuid => new Guid("e473fcba-4e52-8a23-43e1-3402d6b6d6d4");
    
    public override void AddedToDocument(GH_Document document){
        SelectedConstructor = CSOUtils.FindConstructor("Objects.Other.RenderMaterial.ctor(System.Double,System.Double,System.Double,System.Nullable`1[System.Drawing.Color],System.Nullable`1[System.Drawing.Color])","Objects.Other.RenderMaterial");
        base.AddedToDocument(document);
    }
}

// This is generated code:
public class RevitBeamSchemaComponent: CreateSchemaObjectBase {
     
    public RevitBeamSchemaComponent(): base("RevitBeam", "RevitBeam", "Creates a Revit beam by curve and base level.", "Speckle 2 Revit", "Structure") { }
    
    public override Guid ComponentGuid => new Guid("f24a39c8-c9e5-2495-1874-f4ab446e2ec1");
    
    public override void AddedToDocument(GH_Document document){
        SelectedConstructor = CSOUtils.FindConstructor("Objects.BuiltElements.Revit.RevitBeam.ctor(System.String,System.String,Objects.ICurve,Objects.BuiltElements.Level,System.Collections.Generic.List`1[Objects.BuiltElements.Revit.Parameter])","Objects.BuiltElements.Revit.RevitBeam");
        base.AddedToDocument(document);
    }
}

// This is generated code:
public class RevitBraceSchemaComponent: CreateSchemaObjectBase {
     
    public RevitBraceSchemaComponent(): base("RevitBrace", "RevitBrace", "Creates a Revit brace by curve and base level.", "Speckle 2 Revit", "Structure") { }
    
    public override Guid ComponentGuid => new Guid("5b25d90c-638c-c68d-0553-966732b0f32e");
    
    public override void AddedToDocument(GH_Document document){
        SelectedConstructor = CSOUtils.FindConstructor("Objects.BuiltElements.Revit.RevitBrace.ctor(System.String,System.String,Objects.ICurve,Objects.BuiltElements.Level,System.Collections.Generic.List`1[Objects.BuiltElements.Revit.Parameter])","Objects.BuiltElements.Revit.RevitBrace");
        base.AddedToDocument(document);
    }
}

// This is generated code:
public class RevitColumnSchemaComponent: CreateSchemaObjectBase {
     
    public RevitColumnSchemaComponent(): base("RevitColumn Vertical", "RevitColumn Vertical", "Creates a vertical Revit Column by point and levels.", "Speckle 2 Revit", "Architecture") { }
    
    public override Guid ComponentGuid => new Guid("2fc41752-da0b-8f1f-5bd4-c17e8b08f0d9");
    
    public override void AddedToDocument(GH_Document document){
        SelectedConstructor = CSOUtils.FindConstructor("Objects.BuiltElements.Revit.RevitColumn.ctor(System.String,System.String,Objects.ICurve,Objects.BuiltElements.Level,Objects.BuiltElements.Level,System.Double,System.Double,System.Boolean,System.Double,System.Collections.Generic.List`1[Objects.BuiltElements.Revit.Parameter])","Objects.BuiltElements.Revit.RevitColumn");
        base.AddedToDocument(document);
    }
}

// This is generated code:
public class RevitDuctSchemaComponent: CreateSchemaObjectBase {
     
    public RevitDuctSchemaComponent(): base("RevitDuct", "RevitDuct", "Creates a Revit duct", "Speckle 2 Revit", "MEP") { }
    
    public override Guid ComponentGuid => new Guid("78a7a7c8-8cae-2f35-78b5-5e2e96d037bd");
    
    public override void AddedToDocument(GH_Document document){
        SelectedConstructor = CSOUtils.FindConstructor("Objects.BuiltElements.Revit.RevitDuct.ctor(System.String,System.String,Objects.Geometry.Line,System.String,System.String,Objects.BuiltElements.Level,System.Double,System.Double,System.Double,System.Double,System.Collections.Generic.List`1[Objects.BuiltElements.Revit.Parameter])","Objects.BuiltElements.Revit.RevitDuct");
        base.AddedToDocument(document);
    }
}

// This is generated code:
public class RevitExtrusionRoofSchemaComponent: CreateSchemaObjectBase {
     
    public RevitExtrusionRoofSchemaComponent(): base("RevitExtrusionRoof", "RevitExtrusionRoof", "Creates a Revit roof by extruding a curve", "Speckle 2 Revit", "Architecture") { }
    
    public override Guid ComponentGuid => new Guid("3de83e73-de31-9005-35e5-1e465ed44f14");
    
    public override void AddedToDocument(GH_Document document){
        SelectedConstructor = CSOUtils.FindConstructor("Objects.BuiltElements.Revit.RevitRoof.RevitExtrusionRoof.ctor(System.String,System.String,System.Double,System.Double,Objects.Geometry.Line,Objects.BuiltElements.Level,System.Collections.Generic.List`1[Speckle.Core.Models.Base],System.Collections.Generic.List`1[Objects.BuiltElements.Revit.Parameter])","Objects.BuiltElements.Revit.RevitRoof.RevitExtrusionRoof");
        base.AddedToDocument(document);
    }
}

// This is generated code:
public class RevitFaceWallSchemaComponent: CreateSchemaObjectBase {
     
    public RevitFaceWallSchemaComponent(): base("RevitWall by face", "RevitWall by face", "Creates a Revit wall from a surface.", "Speckle 2 Revit", "Architecture") { }
    
    public override Guid ComponentGuid => new Guid("71bf5594-3716-8a74-122d-5539ab277ba0");
    
    public override void AddedToDocument(GH_Document document){
        SelectedConstructor = CSOUtils.FindConstructor("Objects.BuiltElements.Revit.RevitFaceWall.ctor(System.String,System.String,Objects.Geometry.Brep,Objects.BuiltElements.Level,Objects.BuiltElements.Revit.LocationLine,System.Collections.Generic.List`1[Speckle.Core.Models.Base],System.Collections.Generic.List`1[Objects.BuiltElements.Revit.Parameter])","Objects.BuiltElements.Revit.RevitFaceWall");
        base.AddedToDocument(document);
    }
}

// This is generated code:
public class RevitFloorSchemaComponent: CreateSchemaObjectBase {
     
    public RevitFloorSchemaComponent(): base("RevitFloor", "RevitFloor", "Creates a Revit floor by outline and level", "Speckle 2 Revit", "Architecture") { }
    
    public override Guid ComponentGuid => new Guid("9c935e8c-557f-202e-eceb-c385cb932b29");
    
    public override void AddedToDocument(GH_Document document){
        SelectedConstructor = CSOUtils.FindConstructor("Objects.BuiltElements.Revit.RevitFloor.ctor(System.String,System.String,Objects.ICurve,Objects.BuiltElements.Level,System.Boolean,System.Collections.Generic.List`1[Objects.ICurve],System.Collections.Generic.List`1[Speckle.Core.Models.Base],System.Collections.Generic.List`1[Objects.BuiltElements.Revit.Parameter])","Objects.BuiltElements.Revit.RevitFloor");
        base.AddedToDocument(document);
    }
}

// This is generated code:
public class RevitFootprintRoofSchemaComponent: CreateSchemaObjectBase {
     
    public RevitFootprintRoofSchemaComponent(): base("RevitFootprintRoof", "RevitFootprintRoof", "Creates a Revit roof by outline", "Speckle 2 Revit", "Architecture") { }
    
    public override Guid ComponentGuid => new Guid("a5147f8f-143b-b662-62cb-c8c2832b8039");
    
    public override void AddedToDocument(GH_Document document){
        SelectedConstructor = CSOUtils.FindConstructor("Objects.BuiltElements.Revit.RevitRoof.RevitFootprintRoof.ctor(Objects.ICurve,System.String,System.String,Objects.BuiltElements.Level,Objects.BuiltElements.Revit.RevitLevel,System.Double,System.Collections.Generic.List`1[Objects.ICurve],System.Collections.Generic.List`1[Speckle.Core.Models.Base],System.Collections.Generic.List`1[Objects.BuiltElements.Revit.Parameter])","Objects.BuiltElements.Revit.RevitRoof.RevitFootprintRoof");
        base.AddedToDocument(document);
    }
}

// This is generated code:
public class RevitLevelSchemaComponent: CreateSchemaObjectBase {
     
    public RevitLevelSchemaComponent(): base("RevitLevel", "RevitLevel", "Creates a new Revit level unless one with the same elevation already exists", "Speckle 2 Revit", "Architecture") { }
    
    public override Guid ComponentGuid => new Guid("3058b055-29c8-0142-8ca3-d759903e1077");
    
    public override void AddedToDocument(GH_Document document){
        SelectedConstructor = CSOUtils.FindConstructor("Objects.BuiltElements.Revit.RevitLevel.ctor(System.String,System.Double,System.Boolean,System.Collections.Generic.List`1[Objects.BuiltElements.Revit.Parameter])","Objects.BuiltElements.Revit.RevitLevel");
        base.AddedToDocument(document);
    }
}

// This is generated code:
public class RevitPipeSchemaComponent: CreateSchemaObjectBase {
     
    public RevitPipeSchemaComponent(): base("RevitPipe", "RevitPipe", "Creates a Revit pipe", "Speckle 2 Revit", "MEP") { }
    
    public override Guid ComponentGuid => new Guid("068d2e01-a492-ad86-49ab-ec26a4fe4784");
    
    public override void AddedToDocument(GH_Document document){
        SelectedConstructor = CSOUtils.FindConstructor("Objects.BuiltElements.Revit.RevitPipe.ctor(System.String,System.String,Objects.Geometry.Line,System.Double,Objects.BuiltElements.Level,System.String,System.String,System.Collections.Generic.List`1[Objects.BuiltElements.Revit.Parameter])","Objects.BuiltElements.Revit.RevitPipe");
        base.AddedToDocument(document);
    }
}

// This is generated code:
public class RevitRailingSchemaComponent: CreateSchemaObjectBase {
     
    public RevitRailingSchemaComponent(): base("Railing", "Railing", "Creates a Revit railing by base curve.", "Speckle 2 Revit", "Architecture") { }
    
    public override Guid ComponentGuid => new Guid("ae3086f0-fa31-ded0-c1bd-bdb8d87bf5f9");
    
    public override void AddedToDocument(GH_Document document){
        SelectedConstructor = CSOUtils.FindConstructor("Objects.BuiltElements.Revit.RevitRailing.ctor(System.String,Objects.Geometry.Polycurve,Objects.BuiltElements.Level,System.Boolean)","Objects.BuiltElements.Revit.RevitRailing");
        base.AddedToDocument(document);
    }
}

// This is generated code:
public class RevitShaftSchemaComponent: CreateSchemaObjectBase {
     
    public RevitShaftSchemaComponent(): base("RevitShaft", "RevitShaft", "Creates a Revit shaft from a bottom and top level", "Speckle 2 Revit", "Architecture") { }
    
    public override Guid ComponentGuid => new Guid("08d18d6d-2ece-25e2-a6f2-ddd706b5ebd9");
    
    public override void AddedToDocument(GH_Document document){
        SelectedConstructor = CSOUtils.FindConstructor("Objects.BuiltElements.Revit.RevitShaft.ctor(Objects.ICurve,Objects.BuiltElements.Level,Objects.BuiltElements.Level,System.Collections.Generic.List`1[Objects.BuiltElements.Revit.Parameter])","Objects.BuiltElements.Revit.RevitShaft");
        base.AddedToDocument(document);
    }
}

// This is generated code:
public class RevitTopographySchemaComponent: CreateSchemaObjectBase {
     
    public RevitTopographySchemaComponent(): base("RevitTopography", "RevitTopography", "Creates a Revit topography", "Speckle 2 Revit", "Architecture") { }
    
    public override Guid ComponentGuid => new Guid("d6f62ecb-0f54-c1d1-6c69-7346ec552417");
    
    public override void AddedToDocument(GH_Document document){
        SelectedConstructor = CSOUtils.FindConstructor("Objects.BuiltElements.Revit.RevitTopography.ctor(Objects.Geometry.Mesh,System.Collections.Generic.List`1[Objects.BuiltElements.Revit.Parameter])","Objects.BuiltElements.Revit.RevitTopography");
        base.AddedToDocument(document);
    }
}

// This is generated code:
public class RevitWallSchemaComponent: CreateSchemaObjectBase {
     
    public RevitWallSchemaComponent(): base("RevitWall by curve and levels", "RevitWall by curve and levels", "Creates a Revit wall with a top and base level.", "Speckle 2 Revit", "Architecture") { }
    
    public override Guid ComponentGuid => new Guid("d7afa102-5541-85c6-cf4c-4ac6a0b22820");
    
    public override void AddedToDocument(GH_Document document){
        SelectedConstructor = CSOUtils.FindConstructor("Objects.BuiltElements.Revit.RevitWall.ctor(System.String,System.String,Objects.ICurve,Objects.BuiltElements.Level,Objects.BuiltElements.Level,System.Double,System.Double,System.Boolean,System.Boolean,System.Collections.Generic.List`1[Speckle.Core.Models.Base],System.Collections.Generic.List`1[Objects.BuiltElements.Revit.Parameter])","Objects.BuiltElements.Revit.RevitWall");
        base.AddedToDocument(document);
    }
}

// This is generated code:
public class RevitWireSchemaComponent: CreateSchemaObjectBase {
     
    public RevitWireSchemaComponent(): base("RevitWire", "RevitWire", "Creates a Revit wire from points and level", "Speckle 2 Revit", "MEP") { }
    
    public override Guid ComponentGuid => new Guid("ab51eaa9-50b7-bb99-4ea9-8650311c22a6");
    
    public override void AddedToDocument(GH_Document document){
        SelectedConstructor = CSOUtils.FindConstructor("Objects.BuiltElements.Revit.RevitWire.ctor(System.Collections.Generic.List`1[System.Double],System.String,System.String,Objects.BuiltElements.Level,System.String,System.Collections.Generic.List`1[Objects.BuiltElements.Revit.Parameter])","Objects.BuiltElements.Revit.RevitWire");
        base.AddedToDocument(document);
    }
}

// This is generated code:
public class RoofSchemaComponent: CreateSchemaObjectBase {
     
    public RoofSchemaComponent(): base("Roof", "Roof", "Creates a Speckle roof", "Speckle 2 BIM", "Architecture") { }
    
    public override Guid ComponentGuid => new Guid("49ec7f71-9688-6843-c2e5-92a53ede6fef");
    
    public override void AddedToDocument(GH_Document document){
        SelectedConstructor = CSOUtils.FindConstructor("Objects.BuiltElements.Roof.ctor(Objects.ICurve,System.Collections.Generic.List`1[Objects.ICurve],System.Collections.Generic.List`1[Speckle.Core.Models.Base])","Objects.BuiltElements.Roof");
        base.AddedToDocument(document);
    }
}

// This is generated code:
public class RoomSchemaComponent: CreateSchemaObjectBase {
     
    public RoomSchemaComponent(): base("Room", "Room", "Creates a Speckle room", "Speckle 2 BIM", "Architecture") { }
    
    public override Guid ComponentGuid => new Guid("26fe91d9-8ad7-7f15-fded-c90f6a07537c");
    
    public override void AddedToDocument(GH_Document document){
        SelectedConstructor = CSOUtils.FindConstructor("Objects.BuiltElements.Room.ctor(System.String,System.String,Objects.BuiltElements.Level,Objects.Geometry.Point)","Objects.BuiltElements.Room");
        base.AddedToDocument(document);
    }
}

// This is generated code:
public class RoomBoundaryLineSchemaComponent: CreateSchemaObjectBase {
     
    public RoomBoundaryLineSchemaComponent(): base("RoomBoundaryLine", "RoomBoundaryLine", "Creates a Revit room boundary line", "Speckle 2 Revit", "Curves") { }
    
    public override Guid ComponentGuid => new Guid("5dab27e5-60dc-9e36-4b56-2b23f3195ee0");
    
    public override void AddedToDocument(GH_Document document){
        SelectedConstructor = CSOUtils.FindConstructor("Objects.BuiltElements.Revit.Curve.RoomBoundaryLine.ctor(Objects.ICurve,System.Collections.Generic.List`1[Objects.BuiltElements.Revit.Parameter])","Objects.BuiltElements.Revit.Curve.RoomBoundaryLine");
        base.AddedToDocument(document);
    }
}

// This is generated code:
public class TopographySchemaComponent: CreateSchemaObjectBase {
     
    public TopographySchemaComponent(): base("Topography", "Topography", "Creates a Speckle topography", "Speckle 2 BIM", "Architecture") { }
    
    public override Guid ComponentGuid => new Guid("ac008c63-8c7b-3472-2e92-da065d1665a2");
    
    public override void AddedToDocument(GH_Document document){
        SelectedConstructor = CSOUtils.FindConstructor("Objects.BuiltElements.Topography.ctor(Objects.Geometry.Mesh)","Objects.BuiltElements.Topography");
        base.AddedToDocument(document);
    }
}

// This is generated code:
public class WallSchemaComponent: CreateSchemaObjectBase {
     
    public WallSchemaComponent(): base("Wall", "Wall", "Creates a Speckle wall", "Speckle 2 BIM", "Architecture") { }
    
    public override Guid ComponentGuid => new Guid("d5303a81-bf0b-90d3-dabd-0580b638b4a5");
    
    public override void AddedToDocument(GH_Document document){
        SelectedConstructor = CSOUtils.FindConstructor("Objects.BuiltElements.Wall.ctor(System.Double,Objects.ICurve,System.Collections.Generic.List`1[Speckle.Core.Models.Base])","Objects.BuiltElements.Wall");
        base.AddedToDocument(document);
    }
}

// This is generated code:
public class WireSchemaComponent: CreateSchemaObjectBase {
     
    public WireSchemaComponent(): base("Wire", "Wire", "Creates a Speckle wire from curve segments and points", "Speckle 2 BIM", "MEP") { }
    
    public override Guid ComponentGuid => new Guid("584fe689-34ea-eda4-d42e-cf7094f443d9");
    
    public override void AddedToDocument(GH_Document document){
        SelectedConstructor = CSOUtils.FindConstructor("Objects.BuiltElements.Wire.ctor(System.Collections.Generic.List`1[Objects.ICurve])","Objects.BuiltElements.Wire");
        base.AddedToDocument(document);
    }
}


}
