using System;
using System.Linq;
using System.Collections.Generic;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using Objects.BuiltElements;
using Objects.BuiltElements.Revit;
using Objects.Structural.Geometry;
using Objects.Structural.Properties;
using Objects.Structural.Properties.Profiles;
using Objects.Structural.Materials;
using Objects.Geometry;
using Speckle.Core.Models;
using DB = Autodesk.Revit.DB;


namespace Objects.Converter.Revit
{
  public partial class ConverterRevit
  {
	public List<ApplicationPlaceholderObject> AnalyticalSurfaceToNative(Element2D speckleElement)
	{
	  throw new NotImplementedException();
	}

	private Element2D AnalyticalSurfaceToSpeckle(AnalyticalModelSurface revitSurface)
	{
	  if (!revitSurface.IsEnabled())
		  return new Element2D();

	  var speckleElement2D = new Element2D();
	  var structuralElement = Doc.GetElement(revitSurface.GetElementId());	 
	  var mark = GetParamValue<string>(structuralElement, BuiltInParameter.ALL_MODEL_MARK);
	  speckleElement2D.name = mark;
	 	 
	  var edgeNodes = new List<Node> { };
	  var loops = revitSurface.GetLoops(AnalyticalLoopType.External);

	  var displayLine = new Polycurve();
	  foreach (var loop in loops)
	  {
		var coor = new List<double>();
		foreach (var curve in loop)
		{
		  var points = curve.Tessellate();

		  foreach (var p in points.Skip(1))
		  {
			var vertex = PointToSpeckle(p);
			var edgeNode = new Node(vertex, null, null, null);
			edgeNodes.Add(edgeNode);
		  }

		  displayLine.segments.Add(CurveToSpeckle(curve));
		}		
	  }

	  speckleElement2D.topology = edgeNodes;
	  speckleElement2D["displayValue"] = displayLine;

	  var voidNodes = new List<List<Node>> { };
	  var voidLoops = revitSurface.GetLoops(AnalyticalLoopType.Void);
	  foreach (var loop in voidLoops)
	  {
		var loopNodes = new List<Node>();
		foreach (var curve in loop)
		{
		  var points = curve.Tessellate();

		  foreach (var p in points.Skip(1))
		  {
			var vertex = PointToSpeckle(p);
			var voidNode = new Node(vertex, null, null, null);
			loopNodes.Add(voidNode);
		  }
		}
		voidNodes.Add(loopNodes);
	  }
	  //speckleElement2D.voids = voidNodes;

	  //var mesh = new Geometry.Mesh();
	  //var solidGeom = GetElementSolids(structuralElement);
	  //(mesh.faces, mesh.vertices) = GetFaceVertexArrFromSolids(solidGeom);
	  //speckleElement2D.baseMesh = mesh;	  

	  var prop = new Property2D();

	  // Material
	  DB.Material structMaterial = null;
	  double thickness = 0;
	  var memberType = MemberType.Generic2D;

	  if (structuralElement is DB.Floor)
	  {
		var floor = structuralElement as DB.Floor;
		structMaterial = Doc.GetElement(floor.FloorType.StructuralMaterialId) as DB.Material;
		thickness = GetParamValue<double>(structuralElement, BuiltInParameter.STRUCTURAL_FLOOR_CORE_THICKNESS);
		memberType = MemberType.Slab;
	  }
	  else if (structuralElement is DB.Wall)
	  {
		var wall = structuralElement as DB.Wall;
		structMaterial = Doc.GetElement(wall.WallType.get_Parameter(BuiltInParameter.STRUCTURAL_MATERIAL_PARAM).AsElementId()) as DB.Material;
		thickness = ScaleToSpeckle(wall.WallType.Width);
		memberType = MemberType.Wall;
	  }

	  var materialAsset = ((PropertySetElement)Doc.GetElement(structMaterial.StructuralAssetId)).GetStructuralAsset();
	  var materialType = structMaterial.MaterialClass;

	  Structural.Materials.Material speckleMaterial = null;
	  switch (materialType)
	  {
		case "Concrete":
		  var concreteMaterial = new Concrete
		  {
			name = Doc.GetElement(structMaterial.StructuralAssetId).Name,
			materialType = Structural.MaterialType.Concrete,
			grade = null,
			designCode = null,
			codeYear = null,
			elasticModulus = materialAsset.YoungModulus.X,
			compressiveStrength = materialAsset.ConcreteCompression,
			tensileStrength = 0,
			flexuralStrength = 0,
			maxCompressiveStrain = 0,
			maxTensileStrain = 0,
			maxAggregateSize = 0,
			lightweight = materialAsset.Lightweight,
			poissonsRatio = materialAsset.PoissonRatio.X,
			shearModulus = materialAsset.ShearModulus.X,
			density = materialAsset.Density,
			thermalExpansivity = materialAsset.ThermalExpansionCoefficient.X,
			dampingRatio = 0
		  };
		  speckleMaterial = concreteMaterial;
		  break;
		case "Steel":
		  var steelMaterial = new Steel
		  {
			name = Doc.GetElement(structMaterial.StructuralAssetId).Name,
			materialType = Structural.MaterialType.Steel,
			grade = materialAsset.Name,
			designCode = null,
			codeYear = null,
			elasticModulus = materialAsset.YoungModulus.X, // Newtons per foot meter 
			yieldStrength = materialAsset.MinimumYieldStress, // Newtons per foot meter
			ultimateStrength = materialAsset.MinimumTensileStrength, // Newtons per foot meter
			maxStrain = 0,
			poissonsRatio = materialAsset.PoissonRatio.X,
			shearModulus = materialAsset.ShearModulus.X, // Newtons per foot meter
			density = materialAsset.Density, // kilograms per cubed feet 
			thermalExpansivity = materialAsset.ThermalExpansionCoefficient.X, // inverse Kelvin
			dampingRatio = 0
		  };
		  speckleMaterial = steelMaterial;
		  break;
		case "Wood":
		  var timberMaterial = new Timber
		  {
			name = Doc.GetElement(structMaterial.StructuralAssetId).Name,
			materialType = Structural.MaterialType.Timber,
			grade = materialAsset.WoodGrade,
			designCode = null,
			codeYear = null,
			elasticModulus = materialAsset.YoungModulus.X, // Newtons per foot meter 
			poissonsRatio = materialAsset.PoissonRatio.X,
			shearModulus = materialAsset.ShearModulus.X, // Newtons per foot meter
			density = materialAsset.Density, // kilograms per cubed feet 
			thermalExpansivity = materialAsset.ThermalExpansionCoefficient.X, // inverse Kelvin
			species = materialAsset.WoodSpecies,
			dampingRatio = 0
		  };
		  timberMaterial["bendingStrength"] = materialAsset.WoodBendingStrength;
		  timberMaterial["parallelCompressionStrength"] = materialAsset.WoodParallelCompressionStrength;
		  timberMaterial["parallelShearStrength"] = materialAsset.WoodParallelShearStrength;
		  timberMaterial["perpendicularCompressionStrength"] = materialAsset.WoodPerpendicularCompressionStrength;
		  timberMaterial["perpendicularShearStrength"] = materialAsset.WoodPerpendicularShearStrength;
		  speckleMaterial = timberMaterial;
		  break;
		default:
		  var defaultMaterial = new Structural.Materials.Material
		  {
			name = Doc.GetElement(structMaterial.StructuralAssetId).Name
		  };
		  speckleMaterial = defaultMaterial;
		  break;
	  }

	  prop.material = speckleMaterial;
	  prop.name = Doc.GetElement(revitSurface.GetElementId()).Name;
	  prop["memberType"] = memberType;
	  prop.type = Structural.PropertyType2D.Shell;
	  prop.thickness = thickness;

	  speckleElement2D.property = prop;

	  GetAllRevitParamsAndIds(speckleElement2D, revitSurface);
	  
	  //speckleElement2D.displayMesh = GetElementDisplayMesh(Doc.GetElement(revitSurface.GetElementId()),
		 // new Options() { DetailLevel = ViewDetailLevel.Fine, ComputeReferences = false });

	  return speckleElement2D;
	}
  }

}