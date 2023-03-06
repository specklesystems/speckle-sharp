﻿using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.DB.Structure.StructuralSections;
using Objects.BuiltElements.Revit;
using Objects.Structural.Geometry;
using Objects.Structural.Materials;
using Objects.Structural.Properties;
using Objects.Structural.Properties.Profiles;
using Speckle.Core.Models;
using System;
using System.Linq;
using DB = Autodesk.Revit.DB;

namespace Objects.Converter.Revit
{
  public partial class ConverterRevit
  {
    public ApplicationObject AnalyticalStickToNative(Element1D speckleStick)
    {
      ApplicationObject appObj = null;
      XYZ offset1 = VectorToNative(speckleStick.end1Offset ?? new Geometry.Vector(0, 0, 0));
      XYZ offset2 = VectorToNative(speckleStick.end2Offset ?? new Geometry.Vector(0, 0, 0));

#if REVIT2020 || REVIT2021 || REVIT2022
      appObj = CreatePhysicalMember(speckleStick);
      DB.FamilyInstance physicalMember = (DB.FamilyInstance)appObj.Converted.FirstOrDefault();
      SetAnalyticalProps(physicalMember, speckleStick, offset1, offset2);
#else
      var analyticalToPhysicalManager = AnalyticalToPhysicalAssociationManager.GetAnalyticalToPhysicalAssociationManager(Doc);

      // check for existing member
      var docObj = GetExistingElementByApplicationId(speckleStick.applicationId);
      appObj = new ApplicationObject(speckleStick.id, speckleStick.speckle_type) { applicationId = speckleStick.applicationId };

      // skip if element already exists in doc & receive mode is set to ignore
      if (IsIgnore(docObj, appObj, out appObj))
        return appObj;

      if (speckleStick.baseLine == null)
      {
        appObj.Update(status: ApplicationObject.State.Failed, logItem: "Only line based Analytical Members are currently supported.");
        return appObj;
      }

      var baseLine = CurveToNative(speckleStick.baseLine).get_Item(0);
      DB.Level level = null;

      level ??= ConvertLevelToRevit(LevelFromCurve(baseLine), out ApplicationObject.State levelState);
      var isUpdate = false;

      if (!GetElementType<FamilySymbol>(speckleStick, appObj, out DB.FamilySymbol familySymbol))
      {
        appObj.Update(status: ApplicationObject.State.Failed);
        return appObj;
      }

      AnalyticalMember revitMember = null;
      DB.FamilyInstance physicalMember = null;

      if (docObj != null && docObj is AnalyticalMember analyticalMember)
      {      
        // update location
        analyticalMember.SetCurve(baseLine);

        //update type
        analyticalMember.SectionTypeId = familySymbol.Id;
        isUpdate = true;
        revitMember = analyticalMember;

        if (analyticalToPhysicalManager.HasAssociation(revitMember.Id))
        {
          var physicalMemberId = analyticalToPhysicalManager.GetAssociatedElementId(revitMember.Id);
          physicalMember = (DB.FamilyInstance)Doc.GetElement(physicalMemberId);
          if (physicalMember.Symbol != familySymbol)
            physicalMember.Symbol = familySymbol;
        }
      }

      //create family instance
      if (revitMember == null)
      {
        revitMember = AnalyticalMember.Create(Doc, baseLine);
        //set type
        revitMember.SectionTypeId = familySymbol.Id;
      }

      // set or update analytical properties
      SetAnalyticalProps(revitMember, speckleStick, offset1, offset2);

      // if there isn't an associated physical element to the analytical element, create it
      if (!analyticalToPhysicalManager.HasAssociation(revitMember.Id))
      {
        var physicalMemberAppObj = CreatePhysicalMember(speckleStick);
        physicalMember = (DB.FamilyInstance)physicalMemberAppObj.Converted.FirstOrDefault();
        analyticalToPhysicalManager.AddAssociation(revitMember.Id, physicalMember.Id);

        appObj.Update(createdId: physicalMember.UniqueId, convertedItem: physicalMember);
      }

      var state = isUpdate ? ApplicationObject.State.Updated : ApplicationObject.State.Created;
      appObj.Update(status: state, createdId: revitMember.UniqueId, convertedItem: revitMember);
#endif
      return appObj;
    }

    private ApplicationObject CreatePhysicalMember(Element1D speckleStick)
    {
      ApplicationObject appObj = null;
      XYZ offset1 = VectorToNative(speckleStick.end1Offset ?? new Geometry.Vector(0, 0, 0));
      XYZ offset2 = VectorToNative(speckleStick.end2Offset ?? new Geometry.Vector(0, 0, 0));

      switch (speckleStick.type)
      {
        case ElementType1D.Beam:
          RevitBeam revitBeam = new RevitBeam();
          //This only works for CSIC sections now for sure. Need to test on other sections
          revitBeam.type = speckleStick.property.name.Replace('X', 'x');
          revitBeam.baseLine = speckleStick.baseLine;
#if REVIT2020 || REVIT2021 || REVIT2022
          revitBeam.applicationId = speckleStick.applicationId;
#endif
          appObj = BeamToNative(revitBeam);

          return appObj;

        case ElementType1D.Brace:
          RevitBrace revitBrace = new RevitBrace();
          revitBrace.type = speckleStick.property.name.Replace('X', 'x');
          revitBrace.baseLine = speckleStick.baseLine;
#if REVIT2020 || REVIT2021 || REVIT2022
          revitBrace.applicationId = speckleStick.applicationId;
#endif
          appObj = BraceToNative(revitBrace);

          return appObj;

        case ElementType1D.Column:
          RevitColumn revitColumn = new RevitColumn();
          revitColumn.type = speckleStick.property.name.Replace('X', 'x');
          revitColumn.baseLine = speckleStick.baseLine;
          revitColumn.units = speckleStick.units;
#if REVIT2020 || REVIT2021 || REVIT2022
          revitColumn.applicationId = speckleStick.applicationId;
#endif
          appObj = ColumnToNative(revitColumn);

          return appObj;
      }
      return appObj;
    }

    private void SetAnalyticalProps(Element element, Element1D element1d, XYZ offset1, XYZ offset2)
    {
      Func<char, bool> releaseConvert = rel => rel == 'R';

#if REVIT2020 || REVIT2021 || REVIT2022
      var analyticalModel = (AnalyticalModelStick)element.GetAnalyticalModel();
      analyticalModel.SetReleases(true, releaseConvert(element1d.end1Releases.code[0]), releaseConvert(element1d.end1Releases.code[1]), releaseConvert(element1d.end1Releases.code[2]), releaseConvert(element1d.end1Releases.code[3]), releaseConvert(element1d.end1Releases.code[4]), releaseConvert(element1d.end1Releases.code[5]));
      analyticalModel.SetReleases(false, releaseConvert(element1d.end2Releases.code[0]), releaseConvert(element1d.end2Releases.code[1]), releaseConvert(element1d.end2Releases.code[2]), releaseConvert(element1d.end2Releases.code[3]), releaseConvert(element1d.end2Releases.code[4]), releaseConvert(element1d.end2Releases.code[5]));
      analyticalModel.SetOffset(AnalyticalElementSelector.StartOrBase, offset1);
      analyticalModel.SetOffset(AnalyticalElementSelector.EndOrTop, offset2);
#else
      if (element is AnalyticalMember analyticalMember)
      {
        analyticalMember.SetReleaseConditions(new ReleaseConditions(true, releaseConvert(element1d.end1Releases.code[0]), releaseConvert(element1d.end1Releases.code[1]), releaseConvert(element1d.end1Releases.code[2]), releaseConvert(element1d.end1Releases.code[3]), releaseConvert(element1d.end1Releases.code[4]), releaseConvert(element1d.end1Releases.code[5])));
        analyticalMember.SetReleaseConditions(new ReleaseConditions(false, releaseConvert(element1d.end2Releases.code[0]), releaseConvert(element1d.end2Releases.code[1]), releaseConvert(element1d.end2Releases.code[2]), releaseConvert(element1d.end2Releases.code[3]), releaseConvert(element1d.end2Releases.code[4]), releaseConvert(element1d.end2Releases.code[5])));
      }
      //TODO Set offsets
#endif
    }
#if REVIT2020 || REVIT2021 || REVIT2022
    private Element1D AnalyticalStickToSpeckle(AnalyticalModelStick revitStick)
    {
      if (!revitStick.IsEnabled())
        return new Element1D();

      var speckleElement1D = new Element1D();
      switch (revitStick.Category.Name)
      {
        case "Analytical Columns":
          speckleElement1D.type = ElementType1D.Column;
          break;
        case "Analytical Beams":
          speckleElement1D.type = ElementType1D.Beam;
          break;
        case "Analytical Braces":
          speckleElement1D.type = ElementType1D.Brace;
          break;
        default:
          speckleElement1D.type = ElementType1D.Other;
          break;
      }

      var curves = revitStick.GetCurves(AnalyticalCurveType.RigidLinkHead).ToList();
      curves.AddRange(revitStick.GetCurves(AnalyticalCurveType.ActiveCurves));
      curves.AddRange(revitStick.GetCurves(AnalyticalCurveType.RigidLinkTail));

      if (curves.Count > 1)
        speckleElement1D.baseLine = null;
      else
        speckleElement1D.baseLine = (Objects.Geometry.Line)CurveToSpeckle(curves[0]);

      var coordinateSystem = revitStick.GetLocalCoordinateSystem();
      if (coordinateSystem != null)
        speckleElement1D.localAxis = new Geometry.Plane(PointToSpeckle(coordinateSystem.Origin), VectorToSpeckle(coordinateSystem.BasisZ), VectorToSpeckle(coordinateSystem.BasisX), VectorToSpeckle(coordinateSystem.BasisY));

      var startOffset = revitStick.GetOffset(AnalyticalElementSelector.StartOrBase);
      var endOffset = revitStick.GetOffset(AnalyticalElementSelector.EndOrTop);
      speckleElement1D.end1Offset = VectorToSpeckle(startOffset);
      speckleElement1D.end2Offset = VectorToSpeckle(endOffset);

      SetEndReleases(revitStick, ref speckleElement1D);

      var prop = new Property1D();

      var stickFamily = (Autodesk.Revit.DB.FamilyInstance)revitStick.Document.GetElement(revitStick.GetElementId());

      var speckleSection = GetSectionProfile(stickFamily.Symbol);

      var structMat = (DB.Material)stickFamily.Document.GetElement(stickFamily.StructuralMaterialId);
      if (structMat == null)
        structMat = (DB.Material)stickFamily.Document.GetElement(stickFamily.Symbol.get_Parameter(BuiltInParameter.STRUCTURAL_MATERIAL_PARAM).AsElementId());


      prop.profile = speckleSection;
      prop.material = GetStructuralMaterial(structMat);
      prop.name = revitStick.Document.GetElement(revitStick.GetElementId()).Name;

      var structuralElement = revitStick.Document.GetElement(revitStick.GetElementId());
      var mark = GetParamValue<string>(structuralElement, BuiltInParameter.ALL_MODEL_MARK);

      if (revitStick is AnalyticalModelColumn)
      {
        speckleElement1D.type = ElementType1D.Column;
        //prop.memberType = MemberType.Column;
        var locationMark = GetParamValue<string>(structuralElement, BuiltInParameter.COLUMN_LOCATION_MARK);
        if (locationMark == null)
          speckleElement1D.name = mark;
        else
          speckleElement1D.name = locationMark;
      }
      else
      {
        prop.memberType = MemberType.Beam;
        speckleElement1D.name = mark;
      }

      speckleElement1D.property = prop;

      GetAllRevitParamsAndIds(speckleElement1D, revitStick);
      speckleElement1D.displayValue = GetElementDisplayMesh(revitStick.Document.GetElement(revitStick.GetElementId()));
      return speckleElement1D;
    }

#else
    private Element1D AnalyticalStickToSpeckle(AnalyticalMember revitStick)
    {
      var speckleElement1D = new Element1D();
      switch (revitStick.StructuralRole)
      {
        case AnalyticalStructuralRole.StructuralRoleColumn :
          speckleElement1D.type = ElementType1D.Column;
          break;
        case AnalyticalStructuralRole.StructuralRoleBeam:
          speckleElement1D.type = ElementType1D.Beam;
          break;
        case AnalyticalStructuralRole.StructuralRoleMember:
          speckleElement1D.type = ElementType1D.Brace;
          break;
        default:
          speckleElement1D.type = ElementType1D.Other;
          break;
      }

      speckleElement1D.baseLine = (Objects.Geometry.Line)CurveToSpeckle(revitStick.GetCurve());

      SetEndReleases(revitStick, ref speckleElement1D);

      var prop = new Property1D();
  
      var stickFamily = (Autodesk.Revit.DB.FamilySymbol)revitStick.Document.GetElement(revitStick.SectionTypeId);

      var speckleSection = GetSectionProfile(stickFamily);

      var structMat = (DB.Material)stickFamily.Document.GetElement(revitStick.MaterialId);
      if (structMat == null)
        structMat = (DB.Material)stickFamily.Document.GetElement(stickFamily.get_Parameter(BuiltInParameter.STRUCTURAL_MATERIAL_PARAM).AsElementId());

      prop.profile = speckleSection;
      prop.material = GetStructuralMaterial(structMat);
      prop.name = stickFamily.Name;

      var mark = GetParamValue<string>(stickFamily, BuiltInParameter.ALL_MODEL_MARK);

      //TODO: how to differenciate between column and beam?

      //if (revitStick is AnalyticalModelColumn)
      //{
      //  speckleElement1D.type = ElementType1D.Column;
      //  //prop.memberType = MemberType.Column;
      //  var locationMark = GetParamValue<string>(stickFamily, BuiltInParameter.COLUMN_LOCATION_MARK);
      //  if (locationMark == null)
      //    speckleElement1D.name = mark;
      //  else
      //    speckleElement1D.name = locationMark;
      //}
      //else
      //{
      prop.memberType = MemberType.Beam;
      speckleElement1D.name = mark;
      //}

      speckleElement1D.property = prop;

      GetAllRevitParamsAndIds(speckleElement1D, revitStick);

      var analyticalToPhysicalManager = AnalyticalToPhysicalAssociationManager.GetAnalyticalToPhysicalAssociationManager(Doc);
      if (analyticalToPhysicalManager.HasAssociation(revitStick.Id))
      {
        var physicalElementId = analyticalToPhysicalManager.GetAssociatedElementId(revitStick.Id);
        var physicalElement = Doc.GetElement(physicalElementId);
        speckleElement1D.displayValue = GetElementDisplayMesh(physicalElement);
      }
      
      return speckleElement1D;
    }
#endif

    private void SetEndReleases(Element revitStick, ref Element1D speckleElement1D)
    {
      var startRelease = GetParamValue<int>(revitStick, BuiltInParameter.STRUCTURAL_START_RELEASE_TYPE);
      var endRelease = GetParamValue<int>(revitStick, BuiltInParameter.STRUCTURAL_END_RELEASE_TYPE);
      if (startRelease == 0)
        speckleElement1D.end1Releases = new Restraint(RestraintType.Fixed);
      else
      {
        var botReleaseX = GetParamValue<int>(revitStick, BuiltInParameter.STRUCTURAL_BOTTOM_RELEASE_FX) == 1 ? "R" : "F";
        var botReleaseY = GetParamValue<int>(revitStick, BuiltInParameter.STRUCTURAL_BOTTOM_RELEASE_FY) == 1 ? "R" : "F";
        var botReleaseZ = GetParamValue<int>(revitStick, BuiltInParameter.STRUCTURAL_BOTTOM_RELEASE_FZ) == 1 ? "R" : "F";
        var botReleaseXX = GetParamValue<int>(revitStick, BuiltInParameter.STRUCTURAL_BOTTOM_RELEASE_MX) == 1 ? "R" : "F";
        var botReleaseYY = GetParamValue<int>(revitStick, BuiltInParameter.STRUCTURAL_BOTTOM_RELEASE_MY) == 1 ? "R" : "F";
        var botReleaseZZ = GetParamValue<int>(revitStick, BuiltInParameter.STRUCTURAL_BOTTOM_RELEASE_MZ) == 1 ? "R" : "F";

        string botReleaseCode = botReleaseX + botReleaseY + botReleaseZ + botReleaseXX + botReleaseYY + botReleaseZZ;
        speckleElement1D.end1Releases = new Restraint(botReleaseCode);
      }

      if (endRelease == 0)
        speckleElement1D.end2Releases = new Restraint(RestraintType.Fixed);
      else
      {
        var topReleaseX = GetParamValue<int>(revitStick, BuiltInParameter.STRUCTURAL_TOP_RELEASE_FX) == 1 ? "R" : "F";
        var topReleaseY = GetParamValue<int>(revitStick, BuiltInParameter.STRUCTURAL_TOP_RELEASE_FY) == 1 ? "R" : "F";
        var topReleaseZ = GetParamValue<int>(revitStick, BuiltInParameter.STRUCTURAL_TOP_RELEASE_FZ) == 1 ? "R" : "F";
        var topReleaseXX = GetParamValue<int>(revitStick, BuiltInParameter.STRUCTURAL_TOP_RELEASE_MX) == 1 ? "R" : "F";
        var topReleaseYY = GetParamValue<int>(revitStick, BuiltInParameter.STRUCTURAL_TOP_RELEASE_MY) == 1 ? "R" : "F";
        var topReleaseZZ = GetParamValue<int>(revitStick, BuiltInParameter.STRUCTURAL_TOP_RELEASE_MZ) == 1 ? "R" : "F";

        string topReleaseCode = topReleaseX + topReleaseY + topReleaseZ + topReleaseXX + topReleaseYY + topReleaseZZ;
        speckleElement1D.end2Releases = new Restraint(topReleaseCode);
      }
    }

    private SectionProfile GetSectionProfile(FamilySymbol familySymbol)
    {
      var revitSection = familySymbol.GetStructuralSection();
      if (revitSection == null)
        return null;

      var speckleSection = new SectionProfile();

      switch (revitSection)
      {
        case StructuralSectionGeneralI o: // General Double T shape
          var ISection = new ISection();
          ISection.shapeType = Structural.ShapeType.I;
          ISection.depth = o.Height;
          ISection.width = o.Width;
          ISection.webThickness = o.WebThickness;
          ISection.flangeThickness = o.FlangeThickness;
         
          speckleSection = ISection;
          break;
        case StructuralSectionGeneralT o: // General Tee shape
          var teeSection = new Tee();
          teeSection.shapeType = Structural.ShapeType.Tee;
          teeSection.depth = o.Height;
          teeSection.width = o.Width;
          teeSection.webThickness = o.WebThickness;
          teeSection.flangeThickness = o.FlangeThickness;

          speckleSection = teeSection;
          break;
        case StructuralSectionGeneralH o: // Rectangular Pipe structural sections
          var rectSection = new Rectangular();
          rectSection.shapeType = Structural.ShapeType.I;
          rectSection.depth = o.Height;
          rectSection.width = o.Width;
          rectSection.webThickness = o.WallNominalThickness;
          rectSection.flangeThickness = o.WallNominalThickness;

          speckleSection = rectSection;
          break;
        case StructuralSectionGeneralR o: // Pipe structural sections
          var circSection = new Circular();
          circSection.shapeType = Structural.ShapeType.Circular;
          circSection.radius = o.Diameter / 2;
          circSection.wallThickness = o.WallNominalThickness;

          speckleSection = circSection;
          break;
        case StructuralSectionGeneralF o: // Flat Bar structural sections
          var flatRectSection = new Rectangular();
          flatRectSection.shapeType = Structural.ShapeType.I;
          flatRectSection.depth = o.Height;
          flatRectSection.width = o.Width;

          speckleSection = flatRectSection;
          break;
        case StructuralSectionGeneralS o: // Round Bar structural sections
          var flatCircSection = new Circular();
          flatCircSection.shapeType = Structural.ShapeType.Circular;
          flatCircSection.radius = o.Diameter / 2;

          speckleSection = flatCircSection;
          break;
        case StructuralSectionGeneralW o: // Angle structural sections
          var angleSection = new Angle();
          angleSection.shapeType = Structural.ShapeType.Angle;
          angleSection.depth = o.Height;
          angleSection.width = o.Width;
          angleSection.webThickness = o.WebThickness;
          angleSection.flangeThickness = o.FlangeThickness;

          speckleSection = angleSection;
          break;
        case StructuralSectionGeneralU o: // Channel  structural sections
          var channelSection = new Channel();
          channelSection.shapeType = Structural.ShapeType.Channel;
          channelSection.depth = o.Height;
          channelSection.width = o.Width;
          channelSection.webThickness = o.WebThickness;
          channelSection.flangeThickness = o.FlangeThickness;

          speckleSection = channelSection;
          break;
      }

      speckleSection.units = ModelUnits;
      speckleSection.name = familySymbol.Name;
      speckleSection.area = revitSection.SectionArea;
      speckleSection.weight = revitSection.NominalWeight;
      speckleSection.Izz = revitSection.MomentOfInertiaWeakAxis;
      speckleSection.Iyy = revitSection.MomentOfInertiaStrongAxis;
      speckleSection.J = revitSection.TorsionalMomentOfInertia;

      return speckleSection;
    }

    private StructuralMaterial GetStructuralMaterial(Material material)
    {
      if (material == null)
        return null;

      StructuralAsset materialAsset = null;
      string name = null;
      if (material.StructuralAssetId != ElementId.InvalidElementId)
      {
        materialAsset = ((PropertySetElement)material.Document.GetElement(material.StructuralAssetId)).GetStructuralAsset();

        name = material.Document.GetElement(material.StructuralAssetId)?.Name;
      }
      var materialName = material.MaterialClass;
      var materialType = GetMaterialType(materialName);
      return GetStructuralMaterial(materialType, materialAsset, name);
    }

    private StructuralMaterial GetStructuralMaterial(StructuralMaterialType materialType, StructuralAsset materialAsset, string name)
    {
      Structural.Materials.StructuralMaterial speckleMaterial = null;

      if (materialType == StructuralMaterialType.Undefined && materialAsset != null)
        materialType = GetMaterialType(materialAsset);

      name ??= materialType.ToString();
      switch (materialType)
      {
        case StructuralMaterialType.Concrete:
          var concreteMaterial = new Concrete
          {
            name = name,
            materialType = Structural.MaterialType.Concrete,
          };

          if (materialAsset != null)
          {
            concreteMaterial.compressiveStrength = materialAsset.ConcreteCompression; // Newtons per foot meter
            concreteMaterial.lightweight = materialAsset.Lightweight;
          }

          speckleMaterial = concreteMaterial;
          break;
        case StructuralMaterialType.Steel:
          var steelMaterial = new Steel
          {
            name = name,
            materialType = Structural.MaterialType.Steel,
            designCode = null,
            codeYear = null,
            maxStrain = 0,
            dampingRatio = 0,
          };

          if (materialAsset != null)
          {
            steelMaterial.grade = materialAsset.Name;
            steelMaterial.yieldStrength = materialAsset.MinimumYieldStress; // Newtons per foot meter
            steelMaterial.ultimateStrength = materialAsset.MinimumTensileStrength; // Newtons per foot meter
          }

          speckleMaterial = steelMaterial;
          break;
        case StructuralMaterialType.Wood:
          var timberMaterial = new Timber
          {
            name = name,
            materialType = Structural.MaterialType.Timber,
            designCode = null,
            codeYear = null,
            dampingRatio = 0
          };

          if (materialAsset != null)
          {
            timberMaterial.grade = materialAsset.WoodGrade;
            timberMaterial.species = materialAsset.WoodSpecies;
            timberMaterial["bendingStrength"] = materialAsset.WoodBendingStrength;
            timberMaterial["parallelCompressionStrength"] = materialAsset.WoodParallelCompressionStrength;
            timberMaterial["parallelShearStrength"] = materialAsset.WoodParallelShearStrength;
            timberMaterial["perpendicularCompressionStrength"] = materialAsset.WoodPerpendicularCompressionStrength;
            timberMaterial["perpendicularShearStrength"] = materialAsset.WoodPerpendicularShearStrength;
          }

          speckleMaterial = timberMaterial;
          break;
        default:
          var defaultMaterial = new Objects.Structural.Materials.StructuralMaterial
          {
            name = name,
          };
          speckleMaterial = defaultMaterial;
          break;
      }

      // TODO: support non-isotropic materials
      if (materialAsset != null)
      {
        // some of these are actually the dumbest units I've ever heard of
        speckleMaterial.elasticModulus = materialAsset.YoungModulus.X; // Newtons per foot meter
        speckleMaterial.poissonsRatio = materialAsset.PoissonRatio.X; // Unitless
        speckleMaterial.shearModulus = materialAsset.ShearModulus.X; // Newtons per foot meter
        speckleMaterial.density = materialAsset.Density; // kilograms per cubed feet 
        speckleMaterial.thermalExpansivity = materialAsset.ThermalExpansionCoefficient.X; // inverse Kelvin
      }

      return speckleMaterial;
    }

    private StructuralMaterialType GetMaterialType(string materialName)
    {
      StructuralMaterialType materialType = StructuralMaterialType.Undefined;
      switch (materialName.ToLower())
      {
        case "concrete":
          materialType = StructuralMaterialType.Concrete;
          break;
        case "steel":
          materialType = StructuralMaterialType.Steel;
          break;
        case "wood":
          materialType = StructuralMaterialType.Wood;
          break;
      }

      return materialType;
    }

    private StructuralMaterialType GetMaterialType(StructuralAsset materialAsset)
    {
      StructuralMaterialType materialType = StructuralMaterialType.Undefined;
      switch (materialAsset?.StructuralAssetClass)
      {
        case StructuralAssetClass.Metal:
          materialType = StructuralMaterialType.Steel;
          break;
        case StructuralAssetClass.Concrete:
          materialType = StructuralMaterialType.Concrete;
          break;
        case StructuralAssetClass.Wood:
          materialType = StructuralMaterialType.Wood;
          break;
      }

      return materialType;
    }
  }
}
