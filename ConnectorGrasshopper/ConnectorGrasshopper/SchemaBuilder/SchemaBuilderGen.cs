

using System;
using Grasshopper.Kernel;

namespace ConnectorGrasshopper {

// This is generated code:
public class AdaptiveComponentSchemaComponent: CreateSchemaObjectBase {
     
    public AdaptiveComponentSchemaComponent(): base("AdaptiveComponent", "AdaptiveComponent", "Creates a Revit adaptive component by points", "Speckle 2 Revit", "Families") { }
    
    public override Guid ComponentGuid => new Guid("71420d27-62d1-f158-edab-a89e54604d76");
    
    public override void AddedToDocument(GH_Document document){
        SelectedConstructor = CSOUtils.FindConstructor("Objects.BuiltElements.Revit.AdaptiveComponent.ctor(System.String,System.String,System.Collections.Generic.List`1[Objects.Geometry.Point],System.Boolean,System.Collections.Generic.List`1[Objects.BuiltElements.Revit.Parameter])","Objects.BuiltElements.Revit.AdaptiveComponent");
        base.AddedToDocument(document);
    }
}

// This is generated code:
public class AreaSchemaComponent: CreateSchemaObjectBase {
     
    public AreaSchemaComponent(): base("Area", "Area", "Creates a Speckle area", "Speckle 2 BIM", "Other") { }
    
    public override Guid ComponentGuid => new Guid("b98bd134-1ebd-b805-821c-465f1a25fb4e");
    
    public override void AddedToDocument(GH_Document document){
        SelectedConstructor = CSOUtils.FindConstructor("Objects.BuiltElements.Area.ctor(System.String,System.String,Objects.BuiltElements.Level,Objects.Geometry.Point)","Objects.BuiltElements.Area");
        base.AddedToDocument(document);
    }
}

// This is generated code:
public class BeamSchemaComponent: CreateSchemaObjectBase {
     
    public BeamSchemaComponent(): base("Beam", "Beam", "Creates a Speckle beam", "Speckle 2 BIM", "Structure") { }
    
    public override Guid ComponentGuid => new Guid("5c0a392e-bc1c-cf28-0048-a99ee090ffa1");
    
    public override void AddedToDocument(GH_Document document){
        SelectedConstructor = CSOUtils.FindConstructor("Objects.BuiltElements.Beam.ctor(Objects.ICurve)","Objects.BuiltElements.Beam");
        base.AddedToDocument(document);
    }
}

// This is generated code:
public class BraceSchemaComponent: CreateSchemaObjectBase {
     
    public BraceSchemaComponent(): base("Brace", "Brace", "Creates a Speckle brace", "Speckle 2 BIM", "Structure") { }
    
    public override Guid ComponentGuid => new Guid("cf5f1dad-80cd-d499-2ef7-6ae1f8d34a5c");
    
    public override void AddedToDocument(GH_Document document){
        SelectedConstructor = CSOUtils.FindConstructor("Objects.BuiltElements.Brace.ctor(Objects.ICurve)","Objects.BuiltElements.Brace");
        base.AddedToDocument(document);
    }
}

// This is generated code:
public class CeilingSchemaComponent: CreateSchemaObjectBase {
     
    public CeilingSchemaComponent(): base("Ceiling", "Ceiling", "Creates a Speckle ceiling", "Speckle 2 BIM", "Architecture") { }
    
    public override Guid ComponentGuid => new Guid("91b38d18-dd01-dfc7-f11d-e3d2c118ff0b");
    
    public override void AddedToDocument(GH_Document document){
        SelectedConstructor = CSOUtils.FindConstructor("Objects.BuiltElements.Ceiling.ctor(Objects.ICurve,System.Collections.Generic.List`1[Objects.ICurve],System.Collections.Generic.List`1[Speckle.Core.Models.Base])","Objects.BuiltElements.Ceiling");
        base.AddedToDocument(document);
    }
}

// This is generated code:
public class ColumnSchemaComponent: CreateSchemaObjectBase {
     
    public ColumnSchemaComponent(): base("Column", "Column", "Creates a Speckle column", "Speckle 2 BIM", "Structure") { }
    
    public override Guid ComponentGuid => new Guid("d92fc447-81b6-e595-1905-6239ea13a49b");
    
    public override void AddedToDocument(GH_Document document){
        SelectedConstructor = CSOUtils.FindConstructor("Objects.BuiltElements.Column.ctor(Objects.ICurve)","Objects.BuiltElements.Column");
        base.AddedToDocument(document);
    }
}

// This is generated code:
public class DetailCurveSchemaComponent: CreateSchemaObjectBase {
     
    public DetailCurveSchemaComponent(): base("DetailCurve", "DetailCurve", "Creates a Revit detail curve", "Speckle 2 Revit", "Curves") { }
    
    public override Guid ComponentGuid => new Guid("4752d321-22cc-2d9e-dc6d-e3cf8e70c612");
    
    public override void AddedToDocument(GH_Document document){
        SelectedConstructor = CSOUtils.FindConstructor("Objects.BuiltElements.Revit.Curve.DetailCurve.ctor(Objects.ICurve,System.String,System.Collections.Generic.List`1[Objects.BuiltElements.Revit.Parameter])","Objects.BuiltElements.Revit.Curve.DetailCurve");
        base.AddedToDocument(document);
    }
}

// This is generated code:
public class DirectShapeSchemaComponent: CreateSchemaObjectBase {
     
    public DirectShapeSchemaComponent(): base("DirectShape by base geometries", "DirectShape by base geometries", "Creates a Revit DirectShape using a list of base geometry objects.", "Speckle 2 Revit", "Families") { }
    
    public override Guid ComponentGuid => new Guid("870d9670-cbf5-06d2-f371-e1e49212b063");
    
    public override void AddedToDocument(GH_Document document){
        SelectedConstructor = CSOUtils.FindConstructor("Objects.BuiltElements.Revit.DirectShape.ctor(System.String,Objects.BuiltElements.Revit.RevitCategory,System.Collections.Generic.List`1[Speckle.Core.Models.Base],System.Collections.Generic.List`1[Objects.BuiltElements.Revit.Parameter])","Objects.BuiltElements.Revit.DirectShape");
        base.AddedToDocument(document);
    }
}

// This is generated code:
public class DuctSchemaComponent: CreateSchemaObjectBase {
     
    public DuctSchemaComponent(): base("Duct", "Duct", "Creates a Speckle duct", "Speckle 2 BIM", "MEP") { }
    
    public override Guid ComponentGuid => new Guid("51d40791-43ea-a8e7-ef13-e9bfdf9cd893");
    
    public override void AddedToDocument(GH_Document document){
        SelectedConstructor = CSOUtils.FindConstructor("Objects.BuiltElements.Duct.ctor(Objects.Geometry.Line,System.Double,System.Double,System.Double,System.Double)","Objects.BuiltElements.Duct");
        base.AddedToDocument(document);
    }
}

// This is generated code:
public class FamilyInstanceSchemaComponent: CreateSchemaObjectBase {
     
    public FamilyInstanceSchemaComponent(): base("FamilyInstance", "FamilyInstance", "Creates a Revit family instance", "Speckle 2 Revit", "Families") { }
    
    public override Guid ComponentGuid => new Guid("266c4d84-3f2a-9129-565b-0ddb1e5bdac4");
    
    public override void AddedToDocument(GH_Document document){
        SelectedConstructor = CSOUtils.FindConstructor("Objects.BuiltElements.Revit.FamilyInstance.ctor(Objects.Geometry.Point,System.String,System.String,Objects.BuiltElements.Level,System.Double,System.Boolean,System.Boolean,System.Collections.Generic.List`1[Objects.BuiltElements.Revit.Parameter])","Objects.BuiltElements.Revit.FamilyInstance");
        base.AddedToDocument(document);
    }
}

// This is generated code:
public class FloorSchemaComponent: CreateSchemaObjectBase {
     
    public FloorSchemaComponent(): base("Floor", "Floor", "Creates a Speckle floor", "Speckle 2 BIM", "Architecture") { }
    
    public override Guid ComponentGuid => new Guid("74c5b6bf-257e-8d4e-d9cb-7dc2c7ae3f22");
    
    public override void AddedToDocument(GH_Document document){
        SelectedConstructor = CSOUtils.FindConstructor("Objects.BuiltElements.Floor.ctor(Objects.ICurve,System.Collections.Generic.List`1[Objects.ICurve],System.Collections.Generic.List`1[Speckle.Core.Models.Base])","Objects.BuiltElements.Floor");
        base.AddedToDocument(document);
    }
}

// This is generated code:
public class FreeformElementSchemaComponent: CreateSchemaObjectBase {
     
    public FreeformElementSchemaComponent(): base("Freeform element", "Freeform element", "Creates a Revit Freeform element using a Brep or a Mesh.", "Speckle 2 Revit", "Families") { }
    
    public override Guid ComponentGuid => new Guid("b24dc861-1c3c-a509-bc8b-560e9f7d503e");
    
    public override void AddedToDocument(GH_Document document){
        SelectedConstructor = CSOUtils.FindConstructor("Objects.BuiltElements.Revit.FreeformElement.ctor(Speckle.Core.Models.Base,System.Collections.Generic.List`1[Objects.BuiltElements.Revit.Parameter])","Objects.BuiltElements.Revit.FreeformElement");
        base.AddedToDocument(document);
    }
}

// This is generated code:
public class GridLineSchemaComponent: CreateSchemaObjectBase {
     
    public GridLineSchemaComponent(): base("GridLine", "GridLine", "Creates a Speckle grid line", "Speckle 2 BIM", "Other") { }
    
    public override Guid ComponentGuid => new Guid("b2d4bd71-86a7-c142-7220-d9ed2ee7b02e");
    
    public override void AddedToDocument(GH_Document document){
        SelectedConstructor = CSOUtils.FindConstructor("Objects.BuiltElements.GridLine.ctor(Objects.ICurve)","Objects.BuiltElements.GridLine");
        base.AddedToDocument(document);
    }
}

// This is generated code:
public class LevelSchemaComponent: CreateSchemaObjectBase {
     
    public LevelSchemaComponent(): base("Level", "Level", "Creates a Speckle level", "Speckle 2 BIM", "Architecture") { }
    
    public override Guid ComponentGuid => new Guid("04fca79a-ae5b-6ac4-581d-79438351a4e8");
    
    public override void AddedToDocument(GH_Document document){
        SelectedConstructor = CSOUtils.FindConstructor("Objects.BuiltElements.Level.ctor(System.String,System.Double)","Objects.BuiltElements.Level");
        base.AddedToDocument(document);
    }
}

// This is generated code:
public class ModelCurveSchemaComponent: CreateSchemaObjectBase {
     
    public ModelCurveSchemaComponent(): base("ModelCurve", "ModelCurve", "Creates a Revit model curve", "Speckle 2 Revit", "Curves") { }
    
    public override Guid ComponentGuid => new Guid("7aa6e073-6783-8e7b-eec2-7b7bb0420db2");
    
    public override void AddedToDocument(GH_Document document){
        SelectedConstructor = CSOUtils.FindConstructor("Objects.BuiltElements.Revit.Curve.ModelCurve.ctor(Objects.ICurve,System.String,System.Collections.Generic.List`1[Objects.BuiltElements.Revit.Parameter])","Objects.BuiltElements.Revit.Curve.ModelCurve");
        base.AddedToDocument(document);
    }
}

// This is generated code:
public class ParameterSchemaComponent: CreateSchemaObjectBase {
     
    public ParameterSchemaComponent(): base("Parameter", "Parameter", "A Revit instance parameter to set on an element", "Speckle 2 Revit", "Families") { }
    
    public override Guid ComponentGuid => new Guid("706f3fe9-f499-b07f-b682-febedbe38c9c");
    
    public override void AddedToDocument(GH_Document document){
        SelectedConstructor = CSOUtils.FindConstructor("Objects.BuiltElements.Revit.Parameter.ctor(System.String,System.Object)","Objects.BuiltElements.Revit.Parameter");
        base.AddedToDocument(document);
    }
}

// This is generated code:
public class ParameterUpdaterSchemaComponent: CreateSchemaObjectBase {
     
    public ParameterUpdaterSchemaComponent(): base("ParameterUpdater", "ParameterUpdater", "Updates parameters on a Revit element by id", "Speckle 2 Revit", "Families") { }
    
    public override Guid ComponentGuid => new Guid("c4f5fc69-58e1-59f6-4dac-e31b738f7254");
    
    public override void AddedToDocument(GH_Document document){
        SelectedConstructor = CSOUtils.FindConstructor("Objects.BuiltElements.Revit.ParameterUpdater.ctor(System.String,System.Collections.Generic.List`1[Objects.BuiltElements.Revit.Parameter])","Objects.BuiltElements.Revit.ParameterUpdater");
        base.AddedToDocument(document);
    }
}

// This is generated code:
public class PipeSchemaComponent: CreateSchemaObjectBase {
     
    public PipeSchemaComponent(): base("Pipe", "Pipe", "Creates a Speckle pipe", "Speckle 2 BIM", "MEP") { }
    
    public override Guid ComponentGuid => new Guid("6892cf99-6913-7004-27ab-2cfb8435a644");
    
    public override void AddedToDocument(GH_Document document){
        SelectedConstructor = CSOUtils.FindConstructor("Objects.BuiltElements.Pipe.ctor(Objects.ICurve,System.Double,System.Double,System.Double,System.Double)","Objects.BuiltElements.Pipe");
        base.AddedToDocument(document);
    }
}

// This is generated code:
public class RenderMaterialSchemaComponent: CreateSchemaObjectBase {
     
    public RenderMaterialSchemaComponent(): base("RenderMaterial", "RenderMaterial", "Creates a render material.", "Speckle 2 BIM", "Other") { }
    
    public override Guid ComponentGuid => new Guid("03a49484-4eba-6e08-5e96-b3b78ed13f70");
    
    public override void AddedToDocument(GH_Document document){
        SelectedConstructor = CSOUtils.FindConstructor("Objects.Other.RenderMaterial.ctor(System.Double,System.Double,System.Double,System.Nullable`1[System.Drawing.Color],System.Nullable`1[System.Drawing.Color])","Objects.Other.RenderMaterial");
        base.AddedToDocument(document);
    }
}

// This is generated code:
public class RevitBeamSchemaComponent: CreateSchemaObjectBase {
     
    public RevitBeamSchemaComponent(): base("RevitBeam", "RevitBeam", "Creates a Revit beam by curve and base level.", "Speckle 2 Revit", "Structure") { }
    
    public override Guid ComponentGuid => new Guid("6aba19f5-1b1c-8e0c-f063-2a7c91816b1c");
    
    public override void AddedToDocument(GH_Document document){
        SelectedConstructor = CSOUtils.FindConstructor("Objects.BuiltElements.Revit.RevitBeam.ctor(System.String,System.String,Objects.ICurve,Objects.BuiltElements.Level,System.Collections.Generic.List`1[Objects.BuiltElements.Revit.Parameter])","Objects.BuiltElements.Revit.RevitBeam");
        base.AddedToDocument(document);
    }
}

// This is generated code:
public class RevitBraceSchemaComponent: CreateSchemaObjectBase {
     
    public RevitBraceSchemaComponent(): base("RevitBrace", "RevitBrace", "Creates a Revit brace by curve and base level.", "Speckle 2 Revit", "Structure") { }
    
    public override Guid ComponentGuid => new Guid("a3a689dc-2ca5-d5be-a225-99a144768e7e");
    
    public override void AddedToDocument(GH_Document document){
        SelectedConstructor = CSOUtils.FindConstructor("Objects.BuiltElements.Revit.RevitBrace.ctor(System.String,System.String,Objects.ICurve,Objects.BuiltElements.Level,System.Collections.Generic.List`1[Objects.BuiltElements.Revit.Parameter])","Objects.BuiltElements.Revit.RevitBrace");
        base.AddedToDocument(document);
    }
}

// This is generated code:
public class RevitColumnSchemaComponent: CreateSchemaObjectBase {
     
    public RevitColumnSchemaComponent(): base("RevitColumn Vertical", "RevitColumn Vertical", "Creates a vertical Revit Column by point and levels.", "Speckle 2 Revit", "Architecture") { }
    
    public override Guid ComponentGuid => new Guid("c243fe91-0103-bea1-34b7-3e8b39c8d0ec");
    
    public override void AddedToDocument(GH_Document document){
        SelectedConstructor = CSOUtils.FindConstructor("Objects.BuiltElements.Revit.RevitColumn.ctor(System.String,System.String,Objects.ICurve,Objects.BuiltElements.Level,Objects.BuiltElements.Level,System.Double,System.Double,System.Boolean,System.Double,System.Collections.Generic.List`1[Objects.BuiltElements.Revit.Parameter])","Objects.BuiltElements.Revit.RevitColumn");
        base.AddedToDocument(document);
    }
}

// This is generated code:
public class RevitColumn1SchemaComponent: CreateSchemaObjectBase {
     
    public RevitColumn1SchemaComponent(): base("RevitColumn Slanted", "RevitColumn Slanted", "Creates a slanted Revit Column by curve.", "Speckle 2 Revit", "Structure") { }
    
    public override Guid ComponentGuid => new Guid("bd9936e6-c75f-c0de-feb0-b801eff0e0ea");
    
    public override void AddedToDocument(GH_Document document){
        SelectedConstructor = CSOUtils.FindConstructor("Objects.BuiltElements.Revit.RevitColumn.ctor(System.String,System.String,Objects.ICurve,Objects.BuiltElements.Level,System.Boolean,System.Collections.Generic.List`1[Objects.BuiltElements.Revit.Parameter])","Objects.BuiltElements.Revit.RevitColumn");
        base.AddedToDocument(document);
    }
}

// This is generated code:
public class RevitDuctSchemaComponent: CreateSchemaObjectBase {
     
    public RevitDuctSchemaComponent(): base("RevitDuct", "RevitDuct", "Creates a Revit duct", "Speckle 2 Revit", "MEP") { }
    
    public override Guid ComponentGuid => new Guid("d7781536-e8a9-8aef-1b27-571584f8c4a3");
    
    public override void AddedToDocument(GH_Document document){
        SelectedConstructor = CSOUtils.FindConstructor("Objects.BuiltElements.Revit.RevitDuct.ctor(System.String,System.String,Objects.Geometry.Line,System.String,System.String,Objects.BuiltElements.Level,System.Double,System.Double,System.Double,System.Double,System.Collections.Generic.List`1[Objects.BuiltElements.Revit.Parameter])","Objects.BuiltElements.Revit.RevitDuct");
        base.AddedToDocument(document);
    }
}

// This is generated code:
public class RevitExtrusionRoofSchemaComponent: CreateSchemaObjectBase {
     
    public RevitExtrusionRoofSchemaComponent(): base("RevitExtrusionRoof", "RevitExtrusionRoof", "Creates a Revit roof by extruding a curve", "Speckle 2 Revit", "Architecture") { }
    
    public override Guid ComponentGuid => new Guid("707428ab-15b4-e7ec-a2cd-21154ff50c1b");
    
    public override void AddedToDocument(GH_Document document){
        SelectedConstructor = CSOUtils.FindConstructor("Objects.BuiltElements.Revit.RevitRoof.RevitExtrusionRoof.ctor(System.String,System.String,System.Double,System.Double,Objects.Geometry.Line,Objects.BuiltElements.Level,System.Collections.Generic.List`1[Speckle.Core.Models.Base],System.Collections.Generic.List`1[Objects.BuiltElements.Revit.Parameter])","Objects.BuiltElements.Revit.RevitRoof.RevitExtrusionRoof");
        base.AddedToDocument(document);
    }
}

// This is generated code:
public class RevitFaceWallSchemaComponent: CreateSchemaObjectBase {
     
    public RevitFaceWallSchemaComponent(): base("RevitWall by face", "RevitWall by face", "Creates a Revit wall from a surface.", "Speckle 2 Revit", "Architecture") { }
    
    public override Guid ComponentGuid => new Guid("fc3927a7-0877-8137-a34e-ecd19a6f688c");
    
    public override void AddedToDocument(GH_Document document){
        SelectedConstructor = CSOUtils.FindConstructor("Objects.BuiltElements.Revit.RevitFaceWall.ctor(System.String,System.String,Objects.Geometry.Brep,Objects.BuiltElements.Level,Objects.BuiltElements.Revit.LocationLine,System.Collections.Generic.List`1[Speckle.Core.Models.Base],System.Collections.Generic.List`1[Objects.BuiltElements.Revit.Parameter])","Objects.BuiltElements.Revit.RevitFaceWall");
        base.AddedToDocument(document);
    }
}

// This is generated code:
public class RevitFloorSchemaComponent: CreateSchemaObjectBase {
     
    public RevitFloorSchemaComponent(): base("RevitFloor", "RevitFloor", "Creates a Revit floor by outline and level", "Speckle 2 Revit", "Architecture") { }
    
    public override Guid ComponentGuid => new Guid("e6f17d4f-6c28-0d0f-2370-7b9c09a14fff");
    
    public override void AddedToDocument(GH_Document document){
        SelectedConstructor = CSOUtils.FindConstructor("Objects.BuiltElements.Revit.RevitFloor.ctor(System.String,System.String,Objects.ICurve,Objects.BuiltElements.Level,System.Boolean,System.Collections.Generic.List`1[Objects.ICurve],System.Collections.Generic.List`1[Speckle.Core.Models.Base],System.Collections.Generic.List`1[Objects.BuiltElements.Revit.Parameter])","Objects.BuiltElements.Revit.RevitFloor");
        base.AddedToDocument(document);
    }
}

// This is generated code:
public class RevitFootprintRoofSchemaComponent: CreateSchemaObjectBase {
     
    public RevitFootprintRoofSchemaComponent(): base("RevitFootprintRoof", "RevitFootprintRoof", "Creates a Revit roof by outline", "Speckle 2 Revit", "Architecture") { }
    
    public override Guid ComponentGuid => new Guid("b2f55d9f-7242-ee7e-c44a-24e34d5f6e3e");
    
    public override void AddedToDocument(GH_Document document){
        SelectedConstructor = CSOUtils.FindConstructor("Objects.BuiltElements.Revit.RevitRoof.RevitFootprintRoof.ctor(Objects.ICurve,System.String,System.String,Objects.BuiltElements.Level,Objects.BuiltElements.Revit.RevitLevel,System.Double,System.Collections.Generic.List`1[Objects.ICurve],System.Collections.Generic.List`1[Speckle.Core.Models.Base],System.Collections.Generic.List`1[Objects.BuiltElements.Revit.Parameter])","Objects.BuiltElements.Revit.RevitRoof.RevitFootprintRoof");
        base.AddedToDocument(document);
    }
}

// This is generated code:
public class RevitLevelSchemaComponent: CreateSchemaObjectBase {
     
    public RevitLevelSchemaComponent(): base("RevitLevel", "RevitLevel", "Creates a new Revit level unless one with the same elevation already exists", "Speckle 2 Revit", "Architecture") { }
    
    public override Guid ComponentGuid => new Guid("5ef6e45c-00bd-f3b9-1cbf-6e9a902da7ab");
    
    public override void AddedToDocument(GH_Document document){
        SelectedConstructor = CSOUtils.FindConstructor("Objects.BuiltElements.Revit.RevitLevel.ctor(System.String,System.Double,System.Boolean,System.Collections.Generic.List`1[Objects.BuiltElements.Revit.Parameter])","Objects.BuiltElements.Revit.RevitLevel");
        base.AddedToDocument(document);
    }
}

// This is generated code:
public class RevitLevel1SchemaComponent: CreateSchemaObjectBase {
     
    public RevitLevel1SchemaComponent(): base("RevitLevel by name", "RevitLevel by name", "Gets an existing Revit level by name", "Speckle 2 Revit", "Architecture") { }
    
    public override Guid ComponentGuid => new Guid("e075a88e-7867-3726-3bb7-15b73b2d17e6");
    
    public override void AddedToDocument(GH_Document document){
        SelectedConstructor = CSOUtils.FindConstructor("Objects.BuiltElements.Revit.RevitLevel.ctor(System.String)","Objects.BuiltElements.Revit.RevitLevel");
        base.AddedToDocument(document);
    }
}

// This is generated code:
public class RevitPipeSchemaComponent: CreateSchemaObjectBase {
     
    public RevitPipeSchemaComponent(): base("RevitPipe", "RevitPipe", "Creates a Revit pipe", "Speckle 2 Revit", "MEP") { }
    
    public override Guid ComponentGuid => new Guid("19700cd2-6310-c8b3-7ad5-954033702e52");
    
    public override void AddedToDocument(GH_Document document){
        SelectedConstructor = CSOUtils.FindConstructor("Objects.BuiltElements.Revit.RevitPipe.ctor(System.String,System.String,Objects.ICurve,System.Double,Objects.BuiltElements.Level,System.String,System.String,System.Collections.Generic.List`1[Objects.BuiltElements.Revit.Parameter])","Objects.BuiltElements.Revit.RevitPipe");
        base.AddedToDocument(document);
    }
}

// This is generated code:
public class RevitProfileWallSchemaComponent: CreateSchemaObjectBase {
     
    public RevitProfileWallSchemaComponent(): base("RevitWall by profile", "RevitWall by profile", "Creates a Revit wall from a profile.", "Speckle 2 Revit", "Architecture") { }
    
    public override Guid ComponentGuid => new Guid("b3962fc0-69b0-e766-22b4-b08404650c8a");
    
    public override void AddedToDocument(GH_Document document){
        SelectedConstructor = CSOUtils.FindConstructor("Objects.BuiltElements.Revit.RevitProfileWall.ctor(System.String,System.String,Objects.Geometry.Polycurve,Objects.BuiltElements.Level,Objects.BuiltElements.Revit.LocationLine,System.Boolean,System.Collections.Generic.List`1[Speckle.Core.Models.Base],System.Collections.Generic.List`1[Objects.BuiltElements.Revit.Parameter])","Objects.BuiltElements.Revit.RevitProfileWall");
        base.AddedToDocument(document);
    }
}

// This is generated code:
public class RevitRailingSchemaComponent: CreateSchemaObjectBase {
     
    public RevitRailingSchemaComponent(): base("Railing", "Railing", "Creates a Revit railing by base curve.", "Speckle 2 Revit", "Architecture") { }
    
    public override Guid ComponentGuid => new Guid("a6c3be7a-9e6b-663b-2bc0-dd9fa2ee6552");
    
    public override void AddedToDocument(GH_Document document){
        SelectedConstructor = CSOUtils.FindConstructor("Objects.BuiltElements.Revit.RevitRailing.ctor(System.String,Objects.Geometry.Polycurve,Objects.BuiltElements.Level,System.Boolean)","Objects.BuiltElements.Revit.RevitRailing");
        base.AddedToDocument(document);
    }
}

// This is generated code:
public class RevitShaftSchemaComponent: CreateSchemaObjectBase {
     
    public RevitShaftSchemaComponent(): base("RevitShaft", "RevitShaft", "Creates a Revit shaft from a bottom and top level", "Speckle 2 Revit", "Architecture") { }
    
    public override Guid ComponentGuid => new Guid("00cdb2fd-ef75-107e-822c-3490cd359380");
    
    public override void AddedToDocument(GH_Document document){
        SelectedConstructor = CSOUtils.FindConstructor("Objects.BuiltElements.Revit.RevitShaft.ctor(Objects.ICurve,Objects.BuiltElements.Level,Objects.BuiltElements.Level,System.Collections.Generic.List`1[Objects.BuiltElements.Revit.Parameter])","Objects.BuiltElements.Revit.RevitShaft");
        base.AddedToDocument(document);
    }
}

// This is generated code:
public class RevitTopographySchemaComponent: CreateSchemaObjectBase {
     
    public RevitTopographySchemaComponent(): base("RevitTopography", "RevitTopography", "Creates a Revit topography", "Speckle 2 Revit", "Architecture") { }
    
    public override Guid ComponentGuid => new Guid("f0840908-039e-b6d4-98de-8ed003dfd357");
    
    public override void AddedToDocument(GH_Document document){
        SelectedConstructor = CSOUtils.FindConstructor("Objects.BuiltElements.Revit.RevitTopography.ctor(Objects.Geometry.Mesh,System.Collections.Generic.List`1[Objects.BuiltElements.Revit.Parameter])","Objects.BuiltElements.Revit.RevitTopography");
        base.AddedToDocument(document);
    }
}

// This is generated code:
public class RevitWallSchemaComponent: CreateSchemaObjectBase {
     
    public RevitWallSchemaComponent(): base("RevitWall by curve and levels", "RevitWall by curve and levels", "Creates a Revit wall with a top and base level.", "Speckle 2 Revit", "Architecture") { }
    
    public override Guid ComponentGuid => new Guid("fa1aef22-ddd5-01f4-887e-145ce21da247");
    
    public override void AddedToDocument(GH_Document document){
        SelectedConstructor = CSOUtils.FindConstructor("Objects.BuiltElements.Revit.RevitWall.ctor(System.String,System.String,Objects.ICurve,Objects.BuiltElements.Level,Objects.BuiltElements.Level,System.Double,System.Double,System.Boolean,System.Boolean,System.Collections.Generic.List`1[Speckle.Core.Models.Base],System.Collections.Generic.List`1[Objects.BuiltElements.Revit.Parameter])","Objects.BuiltElements.Revit.RevitWall");
        base.AddedToDocument(document);
    }
}

// This is generated code:
public class RevitWall1SchemaComponent: CreateSchemaObjectBase {
     
    public RevitWall1SchemaComponent(): base("RevitWall by curve and height", "RevitWall by curve and height", "Creates an unconnected Revit wall.", "Speckle 2 Revit", "Architecture") { }
    
    public override Guid ComponentGuid => new Guid("bd2a0bb1-14f7-cd0a-76c4-2429412a5128");
    
    public override void AddedToDocument(GH_Document document){
        SelectedConstructor = CSOUtils.FindConstructor("Objects.BuiltElements.Revit.RevitWall.ctor(System.String,System.String,Objects.ICurve,Objects.BuiltElements.Level,System.Double,System.Double,System.Double,System.Boolean,System.Boolean,System.Collections.Generic.List`1[Speckle.Core.Models.Base],System.Collections.Generic.List`1[Objects.BuiltElements.Revit.Parameter])","Objects.BuiltElements.Revit.RevitWall");
        base.AddedToDocument(document);
    }
}

// This is generated code:
public class RevitWireSchemaComponent: CreateSchemaObjectBase {
     
    public RevitWireSchemaComponent(): base("RevitWire", "RevitWire", "Creates a Revit wire from points and level", "Speckle 2 Revit", "MEP") { }
    
    public override Guid ComponentGuid => new Guid("4045436b-0804-f0e1-a9f2-6217f4d8a45b");
    
    public override void AddedToDocument(GH_Document document){
        SelectedConstructor = CSOUtils.FindConstructor("Objects.BuiltElements.Revit.RevitWire.ctor(System.Collections.Generic.List`1[System.Double],System.String,System.String,Objects.BuiltElements.Level,System.String,System.Collections.Generic.List`1[Objects.BuiltElements.Revit.Parameter])","Objects.BuiltElements.Revit.RevitWire");
        base.AddedToDocument(document);
    }
}

// This is generated code:
public class RoofSchemaComponent: CreateSchemaObjectBase {
     
    public RoofSchemaComponent(): base("Roof", "Roof", "Creates a Speckle roof", "Speckle 2 BIM", "Architecture") { }
    
    public override Guid ComponentGuid => new Guid("c10c6dcd-e6a8-be88-32f3-45c935d0bae9");
    
    public override void AddedToDocument(GH_Document document){
        SelectedConstructor = CSOUtils.FindConstructor("Objects.BuiltElements.Roof.ctor(Objects.ICurve,System.Collections.Generic.List`1[Objects.ICurve],System.Collections.Generic.List`1[Speckle.Core.Models.Base])","Objects.BuiltElements.Roof");
        base.AddedToDocument(document);
    }
}

// This is generated code:
public class RoomSchemaComponent: CreateSchemaObjectBase {
     
    public RoomSchemaComponent(): base("Room", "Room", "Creates a Speckle room", "Speckle 2 BIM", "Architecture") { }
    
    public override Guid ComponentGuid => new Guid("c62087b7-2a9d-743d-336d-e8ea2ab72a29");
    
    public override void AddedToDocument(GH_Document document){
        SelectedConstructor = CSOUtils.FindConstructor("Objects.BuiltElements.Room.ctor(System.String,System.String,Objects.BuiltElements.Level,Objects.Geometry.Point)","Objects.BuiltElements.Room");
        base.AddedToDocument(document);
    }
}

// This is generated code:
public class RoomBoundaryLineSchemaComponent: CreateSchemaObjectBase {
     
    public RoomBoundaryLineSchemaComponent(): base("RoomBoundaryLine", "RoomBoundaryLine", "Creates a Revit room boundary line", "Speckle 2 Revit", "Curves") { }
    
    public override Guid ComponentGuid => new Guid("2edade8a-5139-09be-4273-551f3ac476e2");
    
    public override void AddedToDocument(GH_Document document){
        SelectedConstructor = CSOUtils.FindConstructor("Objects.BuiltElements.Revit.Curve.RoomBoundaryLine.ctor(Objects.ICurve,System.Collections.Generic.List`1[Objects.BuiltElements.Revit.Parameter])","Objects.BuiltElements.Revit.Curve.RoomBoundaryLine");
        base.AddedToDocument(document);
    }
}

// This is generated code:
public class TopographySchemaComponent: CreateSchemaObjectBase {
     
    public TopographySchemaComponent(): base("Topography", "Topography", "Creates a Speckle topography", "Speckle 2 BIM", "Architecture") { }
    
    public override Guid ComponentGuid => new Guid("b9207a45-eebc-72d6-a411-f496443d8b7f");
    
    public override void AddedToDocument(GH_Document document){
        SelectedConstructor = CSOUtils.FindConstructor("Objects.BuiltElements.Topography.ctor(Objects.Geometry.Mesh)","Objects.BuiltElements.Topography");
        base.AddedToDocument(document);
    }
}

// This is generated code:
public class WallSchemaComponent: CreateSchemaObjectBase {
     
    public WallSchemaComponent(): base("Wall", "Wall", "Creates a Speckle wall", "Speckle 2 BIM", "Architecture") { }
    
    public override Guid ComponentGuid => new Guid("6878cc65-2628-d00d-e8c0-b130e828a6c7");
    
    public override void AddedToDocument(GH_Document document){
        SelectedConstructor = CSOUtils.FindConstructor("Objects.BuiltElements.Wall.ctor(System.Double,Objects.ICurve,System.Collections.Generic.List`1[Speckle.Core.Models.Base])","Objects.BuiltElements.Wall");
        base.AddedToDocument(document);
    }
}

// This is generated code:
public class WireSchemaComponent: CreateSchemaObjectBase {
     
    public WireSchemaComponent(): base("Wire", "Wire", "Creates a Speckle wire from curve segments and points", "Speckle 2 BIM", "MEP") { }
    
    public override Guid ComponentGuid => new Guid("c10c9c87-b93d-4e98-d2fb-942d182008dc");
    
    public override void AddedToDocument(GH_Document document){
        SelectedConstructor = CSOUtils.FindConstructor("Objects.BuiltElements.Wire.ctor(System.Collections.Generic.List`1[Objects.ICurve])","Objects.BuiltElements.Wire");
        base.AddedToDocument(document);
    }
}


}
