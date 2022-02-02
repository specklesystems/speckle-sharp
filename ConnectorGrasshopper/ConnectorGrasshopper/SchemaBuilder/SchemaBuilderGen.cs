

using System;
using Grasshopper.Kernel;
using ConnectorGrasshopperUtils;

namespace ConnectorGrasshopper {
// This is generated code:
public class DuctSchemaComponent: CreateSchemaObjectBase {
     
    public DuctSchemaComponent(): base("Duct", "Duct", "Creates a Speckle duct", "Speckle 2 BIM", "MEP") { }
    
    public override Guid ComponentGuid => new Guid("51d40791-43ea-a8e7-ef13-e9bfdf9cd893");
    public override bool Obsolete => true;
    public override GH_Exposure Exposure => GH_Exposure.hidden;
    
    public override void AddedToDocument(GH_Document document){
        SelectedConstructor = CSOUtils.FindConstructor("Objects.BuiltElements.Duct.ctor(Objects.Geometry.Line,System.Double,System.Double,System.Double,System.Double)","Objects.BuiltElements.Duct");
        base.AddedToDocument(document);
    }
}

// This is generated code:
public class FreeformElementSchemaComponent: CreateSchemaObjectBase {
     
    public FreeformElementSchemaComponent(): base("Freeform element", "Freeform element", "Creates a Revit Freeform element using a list of Brep or Meshes.", "Speckle 2 Revit", "Families") { }
    
    public override Guid ComponentGuid => new Guid("b24dc861-1c3c-a509-bc8b-560e9f7d503e");
    public override bool Obsolete => true;
    public override GH_Exposure Exposure => GH_Exposure.hidden;
    
    public override void AddedToDocument(GH_Document document){
        SelectedConstructor = CSOUtils.FindConstructor("Objects.BuiltElements.Revit.FreeformElement.ctor(Speckle.Core.Models.Base,System.Collections.Generic.List`1[Objects.BuiltElements.Revit.Parameter])","Objects.BuiltElements.Revit.FreeformElement");
        base.AddedToDocument(document);
    }
}

// This is generated code:
public class RevitDuctSchemaComponent: CreateSchemaObjectBase {
     
    public RevitDuctSchemaComponent(): base("RevitDuct", "RevitDuct", "Creates a Revit duct", "Speckle 2 Revit", "MEP") { }
    
    public override Guid ComponentGuid => new Guid("d7781536-e8a9-8aef-1b27-571584f8c4a3");
    public override bool Obsolete => true;
    public override GH_Exposure Exposure => GH_Exposure.hidden;
    
    public override void AddedToDocument(GH_Document document){
        SelectedConstructor = CSOUtils.FindConstructor("Objects.BuiltElements.Revit.RevitDuct.ctor(System.String,System.String,Objects.Geometry.Line,System.String,System.String,Objects.BuiltElements.Level,System.Double,System.Double,System.Double,System.Double,System.Collections.Generic.List`1[Objects.BuiltElements.Revit.Parameter])","Objects.BuiltElements.Revit.RevitDuct");
        base.AddedToDocument(document);
    }
}

// This is generated code:
public class RoofSchemaComponent: CreateSchemaObjectBase {
     
    public RoofSchemaComponent(): base("Roof", "Roof", "Creates a Speckle roof", "Speckle 2 BIM", "Architecture") { }
    
    public override Guid ComponentGuid => new Guid("c10c6dcd-e6a8-be88-32f3-45c935d0bae9");
    public override bool Obsolete => true;
    public override GH_Exposure Exposure => GH_Exposure.hidden;
    
    public override void AddedToDocument(GH_Document document){
        SelectedConstructor = CSOUtils.FindConstructor("Objects.BuiltElements.Roof.ctor(Objects.ICurve,System.Collections.Generic.List`1[Objects.ICurve],System.Collections.Generic.List`1[Speckle.Core.Models.Base])","Objects.BuiltElements.Roof");
        base.AddedToDocument(document);
    }
}


}
