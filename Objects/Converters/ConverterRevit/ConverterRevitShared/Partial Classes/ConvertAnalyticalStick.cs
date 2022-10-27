using Autodesk.Revit.DB;
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
      XYZ offset1 = VectorToNative(speckleStick.end1Offset ?? new Geometry.Vector(0,0,0));
      XYZ offset2 = VectorToNative(speckleStick.end2Offset ?? new Geometry.Vector(0,0,0));

#if REVIT2019 || REVIT2020 || REVIT2021 || REVIT2022
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
      if (docObj != null && docObj is AnalyticalMember analyticalMember)
      {      
        // update location
        analyticalMember.SetCurve(baseLine);

        //update type
        analyticalMember.SectionTypeId = familySymbol.Id;
        isUpdate = true;
        revitMember = analyticalMember;
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

      DB.FamilyInstance physicalMember = null;
      // if there isn't an associated physical element to the analytical element, create it
      if (!analyticalToPhysicalManager.HasAssociation(revitMember.Id))
      {
        var physicalMemberAppObj = CreatePhysicalMember(speckleStick);
        physicalMember = (DB.FamilyInstance)physicalMemberAppObj.Converted.FirstOrDefault();

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
#if REVIT2019 || REVIT2020 || REVIT2021 || REVIT2022
          revitBeam.applicationId = speckleStick.applicationId;
#endif
          appObj = BeamToNative(revitBeam);

          return appObj;

        case ElementType1D.Brace:
          RevitBrace revitBrace = new RevitBrace();
          revitBrace.type = speckleStick.property.name.Replace('X', 'x');
          revitBrace.baseLine = speckleStick.baseLine;
#if REVIT2019 || REVIT2020 || REVIT2021 || REVIT2022
          revitBrace.applicationId = speckleStick.applicationId;
#endif
          appObj = BraceToNative(revitBrace);

          return appObj;

        case ElementType1D.Column:
          RevitColumn revitColumn = new RevitColumn();
          revitColumn.type = speckleStick.property.name.Replace('X', 'x');
          revitColumn.baseLine = speckleStick.baseLine;
          revitColumn.units = speckleStick.units;
#if REVIT2019 || REVIT2020 || REVIT2021 || REVIT2022
          revitColumn.applicationId = speckleStick.applicationId;
#endif
          appObj = ColumnToNative(revitColumn);

          return appObj;
      }
      return appObj;
    }

    private void SetAnalyticalProps(Element element, Element1D element1d, XYZ offset1, XYZ offset2)
    {
#if !REVIT2023
      var analyticalModel = (AnalyticalModelStick)element.GetAnalyticalModel();
      analyticalModel.SetReleases(true, Convert.ToBoolean(element1d.end1Releases.stiffnessX), Convert.ToBoolean(element1d.end1Releases.stiffnessY), Convert.ToBoolean(element1d.end1Releases.stiffnessZ), Convert.ToBoolean(element1d.end1Releases.stiffnessXX), Convert.ToBoolean(element1d.end1Releases.stiffnessYY), Convert.ToBoolean(element1d.end1Releases.stiffnessZZ));
      analyticalModel.SetReleases(false, Convert.ToBoolean(element1d.end2Releases.stiffnessX), Convert.ToBoolean(element1d.end2Releases.stiffnessY), Convert.ToBoolean(element1d.end2Releases.stiffnessZ), Convert.ToBoolean(element1d.end2Releases.stiffnessXX), Convert.ToBoolean(element1d.end2Releases.stiffnessYY), Convert.ToBoolean(element1d.end2Releases.stiffnessZZ));
      analyticalModel.SetOffset(AnalyticalElementSelector.StartOrBase, offset1);
      analyticalModel.SetOffset(AnalyticalElementSelector.EndOrTop, offset2);
#else
      if (element is AnalyticalMember analyticalMember)
      {
        analyticalMember.SetReleaseConditions(new ReleaseConditions(true, Convert.ToBoolean(element1d.end1Releases.stiffnessX), Convert.ToBoolean(element1d.end1Releases.stiffnessY), Convert.ToBoolean(element1d.end1Releases.stiffnessZ), Convert.ToBoolean(element1d.end1Releases.stiffnessXX), Convert.ToBoolean(element1d.end1Releases.stiffnessYY), Convert.ToBoolean(element1d.end1Releases.stiffnessZZ)));
        analyticalMember.SetReleaseConditions(new ReleaseConditions(false, Convert.ToBoolean(element1d.end2Releases.stiffnessX), Convert.ToBoolean(element1d.end2Releases.stiffnessY), Convert.ToBoolean(element1d.end2Releases.stiffnessZ), Convert.ToBoolean(element1d.end2Releases.stiffnessXX), Convert.ToBoolean(element1d.end2Releases.stiffnessYY), Convert.ToBoolean(element1d.end2Releases.stiffnessZZ)));
      }
      //TODO Set offsets
#endif
    }
#if !REVIT2023
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
      var section = stickFamily.Symbol.GetStructuralSection();

      var speckleSection = GetSectionProfile(section);

      var materialType = stickFamily.StructuralMaterialType;
      var structMat = (DB.Material)stickFamily.Document.GetElement(stickFamily.StructuralMaterialId);
      if (structMat == null)
        structMat = (DB.Material)stickFamily.Document.GetElement(stickFamily.Symbol.get_Parameter(BuiltInParameter.STRUCTURAL_MATERIAL_PARAM).AsElementId());
      var materialAsset = ((PropertySetElement)structMat.Document.GetElement(structMat.StructuralAssetId)).GetStructuralAsset();

      var name = stickFamily.Document.GetElement(stickFamily.StructuralMaterialId).Name;
      Structural.Materials.StructuralMaterial speckleMaterial = GetStructuralMaterial(materialType, materialAsset, name);

      prop.profile = speckleSection;
      prop.material = speckleMaterial;
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

      var speckleSection = GetSectionProfile(stickFamily.GetStructuralSection());

      var materialType = stickFamily.StructuralMaterialType;
      var structMat = (DB.Material)stickFamily.Document.GetElement(revitStick.MaterialId);
      if (structMat == null)
        structMat = (DB.Material)stickFamily.Document.GetElement(stickFamily.get_Parameter(BuiltInParameter.STRUCTURAL_MATERIAL_PARAM).AsElementId());
      var materialAsset = ((PropertySetElement)structMat.Document.GetElement(structMat.StructuralAssetId)).GetStructuralAsset();

      var name = structMat.Document.GetElement(structMat.StructuralAssetId).Name;
      var speckleMaterial = GetStructuralMaterial(materialType, materialAsset, name);

      prop.profile = speckleSection;
      prop.material = speckleMaterial;
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

    private SectionProfile GetSectionProfile(StructuralSection section)
    {
      var speckleSection = new SectionProfile();
      speckleSection.name = section.StructuralSectionShapeName;

      switch (section.StructuralSectionGeneralShape)
      {
        case DB.Structure.StructuralSections.StructuralSectionGeneralShape.GeneralI: // Double T structural sections
          var ISection = new ISection();
          ISection.name = section.StructuralSectionShapeName;
          ISection.shapeType = Structural.ShapeType.I;
          ISection.depth = (double)typeof(DB.Structure.StructuralSections.StructuralSectionGeneralI).GetProperty("Height").GetValue(section);
          ISection.width = (double)typeof(DB.Structure.StructuralSections.StructuralSectionGeneralI).GetProperty("Width").GetValue(section);
          ISection.webThickness = (double)typeof(DB.Structure.StructuralSections.StructuralSectionGeneralI).GetProperty("WebThickness").GetValue(section);
          ISection.flangeThickness = (double)typeof(DB.Structure.StructuralSections.StructuralSectionGeneralI).GetProperty("FlangeThickness").GetValue(section);
          ISection.area = (double)typeof(DB.Structure.StructuralSections.StructuralSectionGeneralI).GetProperty("SectionArea").GetValue(section);
          ISection.weight = (double)typeof(DB.Structure.StructuralSections.StructuralSectionGeneralI).GetProperty("NominalWeight").GetValue(section);
          ISection.Iyy = (double)typeof(DB.Structure.StructuralSections.StructuralSectionGeneralI).GetProperty("MomentOfInertiaStrongAxis").GetValue(section);
          ISection.Izz = (double)typeof(DB.Structure.StructuralSections.StructuralSectionGeneralI).GetProperty("MomentOfInertiaWeakAxis").GetValue(section);
          ISection.J = (double)typeof(DB.Structure.StructuralSections.StructuralSectionGeneralI).GetProperty("TorsionalMomentOfInertia").GetValue(section);
          speckleSection = ISection;
          break;
        case DB.Structure.StructuralSections.StructuralSectionGeneralShape.GeneralT: // Tees structural sections
          var teeSection = new Tee();
          teeSection.name = section.StructuralSectionShapeName;
          teeSection.shapeType = Structural.ShapeType.I;
          teeSection.depth = (double)typeof(DB.Structure.StructuralSections.StructuralSectionGeneralT).GetProperty("Height").GetValue(section);
          teeSection.width = (double)typeof(DB.Structure.StructuralSections.StructuralSectionGeneralT).GetProperty("Width").GetValue(section);
          teeSection.webThickness = (double)typeof(DB.Structure.StructuralSections.StructuralSectionGeneralT).GetProperty("WebThickness").GetValue(section);
          teeSection.flangeThickness = (double)typeof(DB.Structure.StructuralSections.StructuralSectionGeneralT).GetProperty("FlangeThickness").GetValue(section);
          teeSection.area = (double)typeof(DB.Structure.StructuralSections.StructuralSectionGeneralT).GetProperty("SectionArea").GetValue(section);
          teeSection.weight = (double)typeof(DB.Structure.StructuralSections.StructuralSectionGeneralT).GetProperty("NominalWeight").GetValue(section);
          teeSection.Iyy = (double)typeof(DB.Structure.StructuralSections.StructuralSectionGeneralT).GetProperty("MomentOfInertiaStrongAxis").GetValue(section);
          teeSection.Izz = (double)typeof(DB.Structure.StructuralSections.StructuralSectionGeneralT).GetProperty("MomentOfInertiaWeakAxis").GetValue(section);
          teeSection.J = (double)typeof(DB.Structure.StructuralSections.StructuralSectionGeneralT).GetProperty("TorsionalMomentOfInertia").GetValue(section);
          speckleSection = teeSection;
          break;
        case DB.Structure.StructuralSections.StructuralSectionGeneralShape.GeneralH: // Rectangular Pipe structural sections
          var rectSection = new Rectangular();
          rectSection.name = section.StructuralSectionShapeName;
          rectSection.shapeType = Structural.ShapeType.I;
          rectSection.depth = (double)typeof(DB.Structure.StructuralSections.StructuralSectionGeneralH).GetProperty("Height").GetValue(section);
          rectSection.width = (double)typeof(DB.Structure.StructuralSections.StructuralSectionGeneralH).GetProperty("Width").GetValue(section);
          var wallThickness = (double)typeof(DB.Structure.StructuralSections.StructuralSectionGeneralH).GetProperty("WallNominalThickness").GetValue(section);
          rectSection.webThickness = wallThickness;
          rectSection.flangeThickness = wallThickness;
          rectSection.area = (double)typeof(DB.Structure.StructuralSections.StructuralSectionGeneralH).GetProperty("SectionArea").GetValue(section);
          rectSection.weight = (double)typeof(DB.Structure.StructuralSections.StructuralSectionGeneralH).GetProperty("NominalWeight").GetValue(section);
          rectSection.Iyy = (double)typeof(DB.Structure.StructuralSections.StructuralSectionGeneralH).GetProperty("MomentOfInertiaStrongAxis").GetValue(section);
          rectSection.Izz = (double)typeof(DB.Structure.StructuralSections.StructuralSectionGeneralH).GetProperty("MomentOfInertiaWeakAxis").GetValue(section);
          rectSection.J = (double)typeof(DB.Structure.StructuralSections.StructuralSectionGeneralH).GetProperty("TorsionalMomentOfInertia").GetValue(section);
          speckleSection = rectSection;
          break;
        case DB.Structure.StructuralSections.StructuralSectionGeneralShape.GeneralR: // Pipe structural sections
          var circSection = new Circular();
          circSection.name = section.StructuralSectionShapeName;
          circSection.shapeType = Structural.ShapeType.Circular;
          circSection.radius = (double)typeof(DB.Structure.StructuralSections.StructuralSectionGeneralR).GetProperty("Diameter").GetValue(section) / 2;
          circSection.wallThickness = (double)typeof(DB.Structure.StructuralSections.StructuralSectionGeneralR).GetProperty("WallNominalThickness").GetValue(section);
          circSection.area = (double)typeof(DB.Structure.StructuralSections.StructuralSectionGeneralR).GetProperty("SectionArea").GetValue(section);
          circSection.weight = (double)typeof(DB.Structure.StructuralSections.StructuralSectionGeneralR).GetProperty("NominalWeight").GetValue(section);
          circSection.Iyy = (double)typeof(DB.Structure.StructuralSections.StructuralSectionGeneralR).GetProperty("MomentOfInertiaStrongAxis").GetValue(section);
          circSection.Izz = (double)typeof(DB.Structure.StructuralSections.StructuralSectionGeneralR).GetProperty("MomentOfInertiaWeakAxis").GetValue(section);
          circSection.J = (double)typeof(DB.Structure.StructuralSections.StructuralSectionGeneralR).GetProperty("TorsionalMomentOfInertia").GetValue(section);
          speckleSection = circSection;
          break;
        case DB.Structure.StructuralSections.StructuralSectionGeneralShape.GeneralF: // Flat Bar structural sections
          var flatRectSection = new Rectangular();
          flatRectSection.name = section.StructuralSectionShapeName;
          flatRectSection.shapeType = Structural.ShapeType.I;
          flatRectSection.depth = (double)typeof(DB.Structure.StructuralSections.StructuralSectionGeneralF).GetProperty("Height").GetValue(section);
          flatRectSection.width = (double)typeof(DB.Structure.StructuralSections.StructuralSectionGeneralF).GetProperty("Width").GetValue(section);
          flatRectSection.area = (double)typeof(DB.Structure.StructuralSections.StructuralSectionGeneralF).GetProperty("SectionArea").GetValue(section);
          flatRectSection.weight = (double)typeof(DB.Structure.StructuralSections.StructuralSectionGeneralF).GetProperty("NominalWeight").GetValue(section);
          flatRectSection.Iyy = (double)typeof(DB.Structure.StructuralSections.StructuralSectionGeneralF).GetProperty("MomentOfInertiaStrongAxis").GetValue(section);
          flatRectSection.Izz = (double)typeof(DB.Structure.StructuralSections.StructuralSectionGeneralF).GetProperty("MomentOfInertiaWeakAxis").GetValue(section);
          flatRectSection.J = (double)typeof(DB.Structure.StructuralSections.StructuralSectionGeneralF).GetProperty("TorsionalMomentOfInertia").GetValue(section);
          speckleSection = flatRectSection;
          break;
        case DB.Structure.StructuralSections.StructuralSectionGeneralShape.GeneralS: // Round Bar structural sections
          var flatCircSection = new Circular();
          flatCircSection.name = section.StructuralSectionShapeName;
          flatCircSection.shapeType = Structural.ShapeType.Circular;
          flatCircSection.radius = (double)typeof(DB.Structure.StructuralSections.StructuralSectionGeneralS).GetProperty("Diameter").GetValue(section) / 2;
          flatCircSection.area = (double)typeof(DB.Structure.StructuralSections.StructuralSectionGeneralS).GetProperty("SectionArea").GetValue(section);
          flatCircSection.weight = (double)typeof(DB.Structure.StructuralSections.StructuralSectionGeneralS).GetProperty("NominalWeight").GetValue(section);
          flatCircSection.Iyy = (double)typeof(DB.Structure.StructuralSections.StructuralSectionGeneralS).GetProperty("MomentOfInertiaStrongAxis").GetValue(section);
          flatCircSection.Izz = (double)typeof(DB.Structure.StructuralSections.StructuralSectionGeneralS).GetProperty("MomentOfInertiaWeakAxis").GetValue(section);
          flatCircSection.J = (double)typeof(DB.Structure.StructuralSections.StructuralSectionGeneralS).GetProperty("TorsionalMomentOfInertia").GetValue(section);
          speckleSection = flatCircSection;
          break;
        case DB.Structure.StructuralSections.StructuralSectionGeneralShape.GeneralW: // Angle structural sections
          var angleSection = new Angle();
          angleSection.name = section.StructuralSectionShapeName;
          angleSection.shapeType = Structural.ShapeType.Angle;
          angleSection.depth = (double)typeof(DB.Structure.StructuralSections.StructuralSectionGeneralW).GetProperty("Height").GetValue(section);
          angleSection.width = (double)typeof(DB.Structure.StructuralSections.StructuralSectionGeneralW).GetProperty("Width").GetValue(section);
          angleSection.webThickness = (double)typeof(DB.Structure.StructuralSections.StructuralSectionGeneralW).GetProperty("WebThickness").GetValue(section);
          angleSection.flangeThickness = (double)typeof(DB.Structure.StructuralSections.StructuralSectionGeneralW).GetProperty("FlangeThickness").GetValue(section);
          angleSection.area = (double)typeof(DB.Structure.StructuralSections.StructuralSectionGeneralW).GetProperty("SectionArea").GetValue(section);
          angleSection.weight = (double)typeof(DB.Structure.StructuralSections.StructuralSectionGeneralW).GetProperty("NominalWeight").GetValue(section);
          angleSection.Iyy = (double)typeof(DB.Structure.StructuralSections.StructuralSectionGeneralW).GetProperty("MomentOfInertiaStrongAxis").GetValue(section);
          angleSection.Izz = (double)typeof(DB.Structure.StructuralSections.StructuralSectionGeneralW).GetProperty("MomentOfInertiaWeakAxis").GetValue(section);
          angleSection.J = (double)typeof(DB.Structure.StructuralSections.StructuralSectionGeneralW).GetProperty("TorsionalMomentOfInertia").GetValue(section);
          speckleSection = angleSection;
          break;
        case DB.Structure.StructuralSections.StructuralSectionGeneralShape.GeneralU: // Channel  structural sections
          var channelSection = new Channel();
          channelSection.name = section.StructuralSectionShapeName;
          channelSection.shapeType = Structural.ShapeType.Channel;
          channelSection.depth = (double)typeof(DB.Structure.StructuralSections.StructuralSectionGeneralU).GetProperty("Height").GetValue(section);
          channelSection.width = (double)typeof(DB.Structure.StructuralSections.StructuralSectionGeneralU).GetProperty("Width").GetValue(section);
          channelSection.webThickness = (double)typeof(DB.Structure.StructuralSections.StructuralSectionGeneralU).GetProperty("WebThickness").GetValue(section);
          channelSection.flangeThickness = (double)typeof(DB.Structure.StructuralSections.StructuralSectionGeneralU).GetProperty("FlangeThickness").GetValue(section);
          channelSection.area = (double)typeof(DB.Structure.StructuralSections.StructuralSectionGeneralU).GetProperty("SectionArea").GetValue(section);
          channelSection.weight = (double)typeof(DB.Structure.StructuralSections.StructuralSectionGeneralU).GetProperty("NominalWeight").GetValue(section);
          channelSection.Iyy = (double)typeof(DB.Structure.StructuralSections.StructuralSectionGeneralU).GetProperty("MomentOfInertiaStrongAxis").GetValue(section);
          channelSection.Izz = (double)typeof(DB.Structure.StructuralSections.StructuralSectionGeneralU).GetProperty("MomentOfInertiaWeakAxis").GetValue(section);
          channelSection.J = (double)typeof(DB.Structure.StructuralSections.StructuralSectionGeneralU).GetProperty("TorsionalMomentOfInertia").GetValue(section);
          speckleSection = channelSection;
          break;
        default:
          speckleSection.name = section.StructuralSectionShapeName;
          break;
      }

      return speckleSection;
    }

    private StructuralMaterial GetStructuralMaterial(StructuralMaterialType materialType, StructuralAsset materialAsset, string name)
    {
      Structural.Materials.StructuralMaterial speckleMaterial = null;

      if (materialType == StructuralMaterialType.Undefined)
      {
        switch (materialAsset.StructuralAssetClass)
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
      }

      switch (materialType)
      {
        case StructuralMaterialType.Concrete:

          var concreteMaterial = new Concrete
          {
            name = name,
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
        case StructuralMaterialType.Steel:
          var steelMaterial = new Steel
          {
            name = name,
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
        case StructuralMaterialType.Wood:
          var timberMaterial = new Timber
          {
            name = name,
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
          var defaultMaterial = new Objects.Structural.Materials.StructuralMaterial
          {
            name = name,
          };
          speckleMaterial = defaultMaterial;
          break;
      }

      return speckleMaterial;
    }
  }
}