using Objects.Geometry;
using Objects.Structural.Geometry;
using Objects.Structural.GSA.Geometry;
using Objects.Structural.GSA.Materials;
using Objects.Structural.GSA.Properties;
using Objects.Structural.Properties;
using Speckle.Core.Models;
using Speckle.GSA.API;
using Speckle.GSA.API.GwaSchema;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Objects.Structural.GSA.Analysis;
using Objects.Structural.GSA.Bridge;
using GwaAxisDirection6 = Speckle.GSA.API.GwaSchema.AxisDirection6;
using Restraint = Objects.Structural.Geometry.Restraint;
using Speckle.Core.Kits;
using Objects.Structural.Properties.Profiles;
using Objects.Structural;
using Objects.Structural.Materials;
using Objects;

namespace ConverterGSA
{
  //Container just for ToNative methods, and their helper methods
  public partial class ConverterGSA
  {
    private Dictionary<Type, Func<Base, List<GsaRecord>>> ToNativeFns;

    void SetupToNativeFns()
    {
      ToNativeFns = new Dictionary<Type, Func<Base, List<GsaRecord>>>()
      {
        //Geometry
        { typeof(Axis), AxisToNative },
        { typeof(GSANode), NodeToNative },
        { typeof(GSAElement1D), Element1dToNative },
        { typeof(GSAElement2D), Element2dToNative },
        // Bridge
        { typeof(GSAInfluenceNode), InfNodeToNative},
        { typeof(GSAInfluenceBeam), InfBeamToNative},
        {typeof(GSAAlignment), AlignToNative},
        {typeof(GSAPath), PathToNative},
        // Analysis
        {typeof(GSAStage), AnalStageToNative},
        //Material
        { typeof(GSASteel), SteelToNative },
        //Property
        { typeof(Property1D), Property1dToNative },
        { typeof(GSAProperty1D), GsaProperty1dToNative },
      };
    }

    #region ToNative
    //TO DO: implement conversion code for ToNative

    #region Geometry
    private List<GsaRecord> AxisToNative(Base speckleObject)
    {
      var speckleAxis = (Axis)speckleObject;
      var gsaAxis = new GsaAxis()
      {
        ApplicationId = speckleAxis.applicationId,
        Index = Instance.GsaModel.Cache.ResolveIndex<GsaAxis>(speckleAxis.applicationId),
        Name = speckleAxis.name,
        OriginX = speckleAxis.definition.origin.x,
        OriginY = speckleAxis.definition.origin.y,
        OriginZ = speckleAxis.definition.origin.z,
      };
      if (speckleAxis.definition.xdir.Norm() != 0)
      {
        gsaAxis.XDirX = speckleAxis.definition.xdir.x;
        gsaAxis.XDirY = speckleAxis.definition.xdir.y;
        gsaAxis.XDirZ = speckleAxis.definition.xdir.z;
      }
      if (speckleAxis.definition.ydir.Norm() != 0)
      {
        gsaAxis.XYDirX = speckleAxis.definition.ydir.x;
        gsaAxis.XYDirY = speckleAxis.definition.ydir.y;
        gsaAxis.XYDirZ = speckleAxis.definition.ydir.z;
      }
      return new List<GsaRecord>{ gsaAxis };
    }

    private List<GsaRecord> NodeToNative(Base speckleObject)
    {
      var speckleNode = (GSANode)speckleObject;
      var gsaNode = new GsaNode()
      {
        ApplicationId = speckleNode.applicationId,
        Index = Instance.GsaModel.Cache.ResolveIndex<GsaNode>(speckleNode.applicationId),
        Name = speckleNode.name,
        Colour = speckleNode.colour.ColourToNative(),
        X = speckleNode.basePoint.x,
        Y = speckleNode.basePoint.y,
        Z = speckleNode.basePoint.z,
      };

      if (speckleNode.springProperty != null) gsaNode.SpringPropertyIndex = Instance.GsaModel.Cache.ResolveIndex<GsaPropSpr>(speckleNode.springProperty.applicationId);
      if (speckleNode.massProperty != null) gsaNode.MassPropertyIndex = Instance.GsaModel.Cache.ResolveIndex<GsaPropMass>(speckleNode.massProperty.applicationId);
      if (GetRestraint(speckleNode.restraint, out var gsaNodeRestraint, out var gsaRestraint))
      {
        gsaNode.NodeRestraint = gsaNodeRestraint;
        gsaNode.Restraints = gsaRestraint;
      }
      if (GetAxis(speckleNode.constraintAxis, out var gsaAxisRefType, out var gsaAxisIndex))
      {
        gsaNode.AxisRefType = gsaAxisRefType;
        gsaNode.AxisIndex = gsaAxisIndex;
      }
      if (speckleNode.localElementSize > 0) gsaNode.MeshSize = speckleNode.localElementSize;

      return new List<GsaRecord>() { gsaNode };
    }

    private List<GsaRecord> Element1dToNative(Base speckleObject)
    {
      var speckleElement = (GSAElement1D)speckleObject;
      var gsaElement = new GsaEl()
      {
        ApplicationId = speckleElement.applicationId,
        Index = Instance.GsaModel.Cache.ResolveIndex<GsaEl>(speckleElement.applicationId),
        Name = speckleElement.name,
        Colour = speckleElement.colour?.ColourToNative() ?? Colour.NotSet,
        Type = speckleElement.type.ToNative(),
        //TaperOffsetPercentageEnd1 - currently not supported
        //TaperOffsetPercentageEnd2 - currently not supported
        NodeIndices = speckleElement.topology?.Select(n => GetIndexFromNode(n)).ToList() ?? new List<int>(),
        Dummy = speckleElement.isDummy,
      };
      if (speckleElement.property != null) gsaElement.PropertyIndex = Instance.GsaModel.Cache.ResolveIndex<GsaSection>(speckleElement.property.applicationId);
      if (GetReleases(speckleElement.end1Releases, out var gsaRelease1, out var gsaStiffnesses1, out var gsaReleaseInclusion1))
      {
        gsaElement.Releases1 = gsaRelease1;
        gsaElement.Stiffnesses1 = gsaStiffnesses1;
        gsaElement.ReleaseInclusion = gsaReleaseInclusion1;
      }
      if (GetReleases(speckleElement.end2Releases, out var gsaRelease2, out var gsaStiffnesses2, out var gsaReleaseInclusion2))
      {
        gsaElement.Releases2 = gsaRelease2;
        gsaElement.Stiffnesses2 = gsaStiffnesses2;
        gsaElement.ReleaseInclusion = gsaReleaseInclusion2;
      }
      if (speckleElement.end1Offset.x != 0) gsaElement.End1OffsetX = speckleElement.end1Offset.x;
      if (speckleElement.end2Offset.x != 0) gsaElement.End2OffsetX = speckleElement.end2Offset.x;
      if (speckleElement.end1Offset.y == speckleElement.end2Offset.y)
      {
        if (speckleElement.end1Offset.y != 0) gsaElement.OffsetY = speckleElement.end1Offset.y;
      }
      else
      {
        gsaElement.OffsetY = speckleElement.end1Offset.y;
        ConversionErrors.Add(new Exception("Element1dToNative: " 
          + "Error converting element1d with application id (" + speckleElement.applicationId + "). "
          + "Different y offsets were assigned at either end."
          + "end 1 y offset of " + gsaElement.OffsetY.ToString() + " has been applied"));
      }
      if (speckleElement.end1Offset.z == speckleElement.end2Offset.z)
      {
        if (speckleElement.end1Offset.z != 0) gsaElement.OffsetZ = speckleElement.end1Offset.z;
      }
      else
      {
        gsaElement.OffsetZ = speckleElement.end1Offset.z;
        ConversionErrors.Add(new Exception("Element1dToNative: "
          + "Error converting element1d with application id (" + speckleElement.applicationId + "). "
          + "Different z offsets were assigned at either end."
          + "end 1 z offset of " + gsaElement.OffsetY.ToString() + " has been applied"));
      }
      if (speckleElement.end1Offset.x != 0) gsaElement.End1OffsetX = speckleElement.end1Offset.x;
      if (speckleElement.orientationAngle != 0) gsaElement.Angle = speckleElement.orientationAngle;
      if (speckleElement.orientationNode != null) gsaElement.OrientationNodeIndex = Instance.GsaModel.Cache.ResolveIndex<GsaNode>(speckleElement.orientationNode.applicationId);
      if (speckleElement.group > 0) gsaElement.Group = speckleElement.group;
      if (speckleElement.parent != null) gsaElement.ParentIndex = Instance.GsaModel.Cache.ResolveIndex<GsaMemb>(speckleElement.parent.applicationId);
      return new List<GsaRecord>() { gsaElement };
    }

    private List<GsaRecord> Element2dToNative(Base speckleObject)
    {
      var speckleElement = (GSAElement2D)speckleObject;
      var gsaElement = new GsaEl()
      {
        ApplicationId = speckleElement.applicationId,
        Index = Instance.GsaModel.Cache.ResolveIndex<GsaEl>(speckleElement.applicationId),
        Name = speckleElement.name,
        Colour = speckleElement.colour.ColourToNative(),
        Type = speckleElement.type.ToNative(),
        NodeIndices = speckleElement.topology.Select(n => GetIndexFromNode(n)).ToList(),
        Dummy = speckleElement.isDummy,
        ReleaseInclusion = ReleaseInclusion.NotIncluded,
      };
      if (speckleElement.property != null) gsaElement.PropertyIndex = Instance.GsaModel.Cache.ResolveIndex<GsaProp2d>(speckleElement.property.applicationId);
      if (speckleElement.orientationAngle != 0) gsaElement.Angle = speckleElement.orientationAngle;
      if (speckleElement.group > 0) gsaElement.Group = speckleElement.group;
      if (speckleElement.offset != 0) gsaElement.OffsetZ = speckleElement.offset;
      if (speckleElement.parent != null) gsaElement.ParentIndex = Instance.GsaModel.Cache.ResolveIndex<GsaMemb>(speckleElement.parent.applicationId);
      return new List<GsaRecord>() { gsaElement };
    }
    #endregion

    #region Loading
    #endregion

    #region Materials
    private List<GsaRecord> SteelToNative(Base speckleObject)
    {
      var speckleSteel = (GSASteel)speckleObject;
      var gsaSteel = new GsaMatSteel()
      {
        Index = Instance.GsaModel.Cache.ResolveIndex<GsaMatSteel>(speckleSteel.applicationId),
        ApplicationId = speckleSteel.applicationId,
        Name = "",
        Mat = new GsaMat()
        {
          Name = "",
          E = 2e11,
          F = 360000000,
          Nu = 0.3,
          G = 7.692307692e+10,
          Rho = 7850,
          Alpha = 1.2e-5,
          Prop = new GsaMatAnal()
          {
            Type = MatAnalType.MAT_ELAS_ISO,
            NumParams = 6,
            E = 2e11,
            Nu = 0.3,
            Rho = 7850,
            Alpha = 1.2e-5,
            G = 7.692307692e+10,
            Damp = 0
          },
          NumUC = 0,
          AbsUC = Dimension.NotSet,
          OrdUC = Dimension.NotSet,
          PtsUC = new double[0],
          NumSC = 0,
          AbsSC = Dimension.NotSet,
          OrdSC = Dimension.NotSet,
          PtsSC = new double[0],
          NumUT = 0,
          AbsUT = Dimension.NotSet,
          OrdUT = Dimension.NotSet,
          PtsUT = new double[0],
          NumST = 0,
          AbsST = Dimension.NotSet,
          OrdST = Dimension.NotSet,
          PtsST = new double[0],
          Eps = 0.05,
          Uls = new GsaMatCurveParam()
          {
            Model = new List<MatCurveParamType>() { MatCurveParamType.UNDEF },
            StrainElasticCompression = 0.0018,
            StrainElasticTension = 0.0018,
            StrainPlasticCompression = 0.0018,
            StrainPlasticTension = 0.0018,
            StrainFailureCompression = 0.05,
            StrainFailureTension = 0.05,
            GammaF = 1,
            GammaE = 1
          },
          Sls = new GsaMatCurveParam()
          {
            Model = new List<MatCurveParamType>() { MatCurveParamType.ELAS_PLAS },
            StrainElasticCompression = 0.0018,
            StrainElasticTension = 0.0018,
            StrainPlasticCompression = 0.0018,
            StrainPlasticTension = 0.0018,
            StrainFailureCompression = 0.05,
            StrainFailureTension = 0.05,
            GammaF = 1,
            GammaE = 1
          },
          Cost = 0,
          Type = MatType.STEEL
        },
        Fy = 360000000,
        Fu = 450000000,
        EpsP = 0,
        Eh = 0,
      };
      return new List<GsaRecord>() { gsaSteel };
    }
    #endregion

    #region Properties
    private List<GsaRecord> GsaProperty1dToNative(Base speckleObject)
    {
      var speckleProperty = (GSAProperty1D)speckleObject;
      var natives = Property1dToNative(speckleObject);
      var gsaSection = (GsaSection)natives.FirstOrDefault(n => n is GsaSection);
      if (gsaSection != null)
      {
        gsaSection.Colour = (Enum.TryParse(speckleProperty.colour, true, out Colour gsaColour) ? gsaColour : Colour.NO_RGB);
        gsaSection.Mass = (speckleProperty.additionalMass == 0) ? null : (double?)speckleProperty.additionalMass;
        gsaSection.Cost = (speckleProperty.cost == 0) ? null : (double?)speckleProperty.cost;
        if (speckleProperty.designMaterial != null && gsaSection.Components != null && gsaSection.Components.Count > 0)
        {
          var sectionComp = (SectionComp)gsaSection.Components.First();
          if (speckleProperty.designMaterial.type == MaterialType.Steel)
          {
            sectionComp.MaterialType = Section1dMaterialType.STEEL;
            sectionComp.MaterialIndex = Instance.GsaModel.Cache.LookupIndex<GsaMatSteel>(speckleProperty.designMaterial.applicationId);

            var steelMaterial = (Steel)speckleProperty.designMaterial;
            var gsaSectionSteel = new SectionSteel()
            {
              //GradeIndex = 0,
              //Defaults
              PlasElas = 1,
              NetGross = 1,
              Exposed = 1,
              Beta = 0.4,
              Type = SectionSteelSectionType.Undefined,
              Plate = SectionSteelPlateType.Undefined,
              Locked = false
            };
            gsaSection.Components.Add(gsaSectionSteel);
          }
          else if (speckleProperty.material.type == MaterialType.Concrete)
          {
            sectionComp.MaterialType = Section1dMaterialType.CONCRETE;
            sectionComp.MaterialIndex = Instance.GsaModel.Cache.LookupIndex<GsaMatConcrete>(speckleProperty.material.applicationId);

            var gsaSectionConc = new SectionConc();
            var gsaSectionCover = new SectionCover();
            var gsaSectionTmpl = new SectionTmpl();
            gsaSection.Components.Add(gsaSectionConc);
            gsaSection.Components.Add(gsaSectionCover);
            gsaSection.Components.Add(gsaSectionTmpl);
          }
          else
          {
            //Not supported yet
          }
        }
      }
      return natives;
    }

    //Note: there should be no ToNative for SectionProfile because it's not a type that will create a first-class citizen in the GSA model
    //      so there is basically a ToNative of that class here in this method too
    private List<GsaRecord> Property1dToNative(Base speckleObject)
    {
      var speckleProperty = (Property1D)speckleObject;
      
      var gsaSection = new GsaSection()
      {
        Index = Instance.GsaModel.Cache.ResolveIndex<GsaSection>(speckleProperty.applicationId),
        Name = speckleProperty.name,
        ApplicationId = speckleProperty.applicationId,
        Type = speckleProperty.memberType.ToNative(),
        //PoolIndex = 0,
        ReferencePoint = speckleProperty.referencePoint.ToNative(),
        RefY = (speckleProperty.offsetY == 0) ? null : (double?)speckleProperty.offsetY,
        RefZ = (speckleProperty.offsetZ == 0) ? null : (double?)speckleProperty.offsetZ,
        Fraction = 1,
        //Left = 0,
        //Right = 0,
        //Slab = 0,
        Components = new List<GsaSectionComponentBase>()
      };

      var sectionComp = new SectionComp()
      {
        Name = string.IsNullOrEmpty(speckleProperty.profile.name) ? null : speckleProperty.profile.name
      };
      
      Property1dProfileToSpeckle(speckleProperty.profile, out sectionComp.ProfileDetails, out sectionComp.ProfileGroup);
      gsaSection.Components.Add(sectionComp);

        /*
         * new SectionSteel()
            {
              //GradeIndex = 0,
              PlasElas = 1,
              NetGross = 1,
              Exposed = 1,
              Beta = 0.4,
              Type = SectionSteelSectionType.HotRolled,
              Plate = SectionSteelPlateType.Undefined,
              Locked = false
            }
        */
        return new List<GsaRecord>() { gsaSection };
    }

    private bool CurveToGsaOutline(ICurve outline, ref List<double?> Y, ref List<double?> Z, ref List<string> actions)
    {
      if (!(outline is Curve))
      {
        return false;
      }
      var pointCoords = ((Curve)outline).points.GroupBy(3).Select(g => g.ToList()).ToList();
      foreach (var coords in pointCoords)
      {
        Y.Add(coords[1]);
        Z.Add(coords[2]);
      }
      actions.Add("M");
      actions.AddRange(Enumerable.Repeat("L", (pointCoords.Count() - 1)));
      return true;
    }

    private bool Property1dProfileToSpeckle(SectionProfile sectionProfile, out ProfileDetails gsaProfileDetails, out Section1dProfileGroup gsaProfileGroup)
    {
      if (sectionProfile.shapeType == ShapeType.Catalogue)
      {
        var p = (Catalogue)sectionProfile;
        gsaProfileDetails = new ProfileDetailsCatalogue()
        {
          Profile = p.description
        };
        gsaProfileGroup = Section1dProfileGroup.Catalogue;
      }
      else if (sectionProfile.shapeType == ShapeType.Explicit)
      {
        var p = (Explicit)sectionProfile;
        gsaProfileDetails = new ProfileDetailsExplicit() { Area = p.area, Iyy = p.Iyy, Izz = p.Izz, J = p.J, Ky = p.Ky, Kz = p.Kz };
        gsaProfileGroup = Section1dProfileGroup.Explicit;
      }
      else if (sectionProfile.shapeType == ShapeType.Perimeter)
      {
        var p = (Perimeter)sectionProfile;
        var hollow = (p.voids != null && p.voids.Count > 0);
        gsaProfileDetails = new ProfileDetailsPerimeter()
        {
          Type = "P"
        };
        if (p.outline is Curve && (p.voids == null || (p.voids.All(v => v is Curve))))
        {
          ((ProfileDetailsPerimeter)gsaProfileDetails).Actions = new List<string>();
          ((ProfileDetailsPerimeter)gsaProfileDetails).Y = new List<double?>();
          ((ProfileDetailsPerimeter)gsaProfileDetails).Z = new List<double?>();

          CurveToGsaOutline(p.outline, ref ((ProfileDetailsPerimeter)gsaProfileDetails).Y, 
            ref ((ProfileDetailsPerimeter)gsaProfileDetails).Z, ref ((ProfileDetailsPerimeter)gsaProfileDetails).Actions);

          if (hollow)
          {
            foreach (var v in p.voids)
            {
              CurveToGsaOutline(v, ref ((ProfileDetailsPerimeter)gsaProfileDetails).Y, 
                ref ((ProfileDetailsPerimeter)gsaProfileDetails).Z, ref ((ProfileDetailsPerimeter)gsaProfileDetails).Actions);
            }
          }
        }
        gsaProfileGroup = Section1dProfileGroup.Perimeter;
      }
      else
      {
        gsaProfileGroup = Section1dProfileGroup.Standard;
        if (sectionProfile.shapeType == ShapeType.Rectangular)
        {
          var p = (Rectangular)sectionProfile;
          var hollow = (p.flangeThickness > 0 || p.webThickness > 0);
          if (hollow)
          {
            gsaProfileDetails = new ProfileDetailsTwoThickness() { ProfileType = Section1dStandardProfileType.RectangularHollow };
            ((ProfileDetailsStandard)gsaProfileDetails).SetValues(p.depth, p.width, p.webThickness, p.flangeThickness);
          }
          else
          {
            gsaProfileDetails = new ProfileDetailsRectangular() { ProfileType = Section1dStandardProfileType.Rectangular };
            ((ProfileDetailsStandard)gsaProfileDetails).SetValues(p.depth, p.width);
          }
        }
        else if (sectionProfile.shapeType == ShapeType.Circular)
        {
          var p = (Circular)sectionProfile;
          var hollow = (p.wallThickness > 0);
          if (hollow)
          {
            gsaProfileDetails = new ProfileDetailsCircularHollow() { ProfileType = Section1dStandardProfileType.CircularHollow };
            ((ProfileDetailsStandard)gsaProfileDetails).SetValues(p.radius * 2, p.wallThickness);
          }
          else
          {
            gsaProfileDetails = new ProfileDetailsCircular() { ProfileType = Section1dStandardProfileType.Circular };
            ((ProfileDetailsStandard)gsaProfileDetails).SetValues(p.radius * 2);
          }
        }
        else if (sectionProfile.shapeType == ShapeType.Angle)
        {
          var p = (Angle)sectionProfile;
          gsaProfileDetails = new ProfileDetailsTwoThickness() { ProfileType = Section1dStandardProfileType.Angle };
          ((ProfileDetailsStandard)gsaProfileDetails).SetValues(p.depth, p.width, p.webThickness, p.flangeThickness);
        }
        else if (sectionProfile.shapeType == ShapeType.Channel)
        {
          var p = (Channel)sectionProfile;
          gsaProfileDetails = new ProfileDetailsTwoThickness() { ProfileType = Section1dStandardProfileType.Channel };
          ((ProfileDetailsStandard)gsaProfileDetails).SetValues(p.depth, p.width, p.webThickness, p.flangeThickness);
        }
        else if (sectionProfile.shapeType == ShapeType.I)
        {
          var p = (ISection)sectionProfile;
          gsaProfileDetails = new ProfileDetailsTwoThickness() { ProfileType = Section1dStandardProfileType.ISection };
          ((ProfileDetailsStandard)gsaProfileDetails).SetValues(p.depth, p.width, p.webThickness, p.flangeThickness);
        }
        else if (sectionProfile.shapeType == ShapeType.Tee)
        {
          var p = (Tee)sectionProfile;
          gsaProfileDetails = new ProfileDetailsTwoThickness() { ProfileType = Section1dStandardProfileType.Tee };
          ((ProfileDetailsStandard)gsaProfileDetails).SetValues(p.depth, p.width, p.webThickness, p.flangeThickness);
        }
        else
        {
          gsaProfileDetails = null;
        }
      }
      return true;
    }

    /*
    private ProfileDetailsStandard GetProfileStandardRectangular(Rectangular sectionProfile)
    {
      var p = (Rectangular)sectionProfile;
      var profileDetails = new ProfileDetailsStandard()
      {
        ProfileType = Section1dStandardProfileType.Rectangular,

      }
      var speckleProfile = new Rectangular()
      {
        name = "",
        shapeType = ShapeType.Rectangular,
      };
      if (p.b.HasValue) speckleProfile.width = p.b.Value;
      if (p.d.HasValue) speckleProfile.depth = p.d.Value;
      return speckleProfile;
    }
    private ProfileDetailsTwoThickness GetProfileStandardRHS(Rectangular sectionProfile)
    {
      var speckleProfile = new Rectangular()
      {
        name = "",
        shapeType = ShapeType.Rectangular,
      };
      if (p.b.HasValue) speckleProfile.width = p.b.Value;
      if (p.d.HasValue) speckleProfile.depth = p.d.Value;
      if (p.tw.HasValue) speckleProfile.webThickness = p.tw.Value;
      if (p.tf.HasValue) speckleProfile.flangeThickness = p.tf.Value;
      return speckleProfile;
    }
    private ProfileDetailsCircular GetProfileStandardCircular(SectionProfile sectionProfile)
    {
      var speckleProfile = new Circular()
      {
        name = "",
        shapeType = ShapeType.Circular,
      };
      if (p.d.HasValue) speckleProfile.radius = p.d.Value / 2;
      return speckleProfile;
    }
    private ProfileDetailsCircularHollow GetProfileStandardCHS(SectionProfile sectionProfile)
    {
      var speckleProfile = new Circular()
      {
        name = "",
        shapeType = ShapeType.Circular,
      };
      if (p.d.HasValue) speckleProfile.radius = p.d.Value / 2;
      if (p.t.HasValue) speckleProfile.wallThickness = p.t.Value;
      return speckleProfile;
    }
    private ProfileDetailsTwoThickness GetProfileStandardISection(SectionProfile sectionProfile)
    {
      var speckleProfile = new ISection()
      {
        name = "",
        shapeType = ShapeType.I,
      };
      if (p.b.HasValue) speckleProfile.width = p.b.Value;
      if (p.d.HasValue) speckleProfile.depth = p.d.Value;
      if (p.tw.HasValue) speckleProfile.webThickness = p.tw.Value;
      if (p.tf.HasValue) speckleProfile.flangeThickness = p.tf.Value;
      return speckleProfile;
    }
    private ProfileDetailsTwoThickness GetProfileStandardTee(SectionProfile sectionProfile)
    {
      var speckleProfile = new Tee()
      {
        name = "",
        shapeType = ShapeType.Tee,
      };
      if (p.b.HasValue) speckleProfile.width = p.b.Value;
      if (p.d.HasValue) speckleProfile.depth = p.d.Value;
      if (p.tw.HasValue) speckleProfile.webThickness = p.tw.Value;
      if (p.tf.HasValue) speckleProfile.flangeThickness = p.tf.Value;
      return speckleProfile;
    }
    private ProfileDetailsTwoThickness GetProfileStandardAngle(SectionProfile sectionProfile)
    {
      var speckleProfile = new Angle()
      {
        name = "",
        shapeType = ShapeType.Angle,
      };
      if (p.b.HasValue) speckleProfile.width = p.b.Value;
      if (p.d.HasValue) speckleProfile.depth = p.d.Value;
      if (p.tw.HasValue) speckleProfile.webThickness = p.tw.Value;
      if (p.tf.HasValue) speckleProfile.flangeThickness = p.tf.Value;
      return speckleProfile;
    }
    private ProfileDetailsTwoThickness GetProfileStandardChannel(SectionProfile sectionProfile)
    {
      var speckleProfile = new Channel()
      {
        name = "",
        shapeType = ShapeType.Channel,
      };
      if (p.b.HasValue) speckleProfile.width = p.b.Value;
      if (p.d.HasValue) speckleProfile.depth = p.d.Value;
      if (p.tw.HasValue) speckleProfile.webThickness = p.tw.Value;
      if (p.tf.HasValue) speckleProfile.flangeThickness = p.tf.Value;
      return speckleProfile;
    }
    */

    #endregion

    #region Bridge

    private List<GsaRecord> AlignToNative(Base speckleObject)
    {
      var speckleAlign = (GSAAlignment)speckleObject;
      var gsaAlign = new GsaAlign()
      {
        ApplicationId = speckleAlign.applicationId,
        Index = Instance.GsaModel.Cache.ResolveIndex<GsaAlign>(speckleAlign.applicationId),
        Chain = speckleAlign.chainage,
        Curv = speckleAlign.curvature,
        Name = speckleAlign.name,
        Sid = speckleAlign.id,
        GridSurfaceIndex = speckleAlign.gridSurface.nativeId,
        NumAlignmentPoints = speckleAlign.GetNumAlignmentPoints(),
      };
      return new List<GsaRecord>() { gsaAlign };
    }

    private List<GsaRecord> InfBeamToNative(Base speckleObject)
    {
      var speckleInfBeam = (GSAInfluenceBeam)speckleObject;
      var elementIndex = ((GsaEl)Element1dToNative(speckleInfBeam.element).First()).Index;
      var gsaInfBeam = new GsaInfBeam
      {
        Name = speckleInfBeam.name,
        Index = Instance.GsaModel.Cache.ResolveIndex<GsaInfBeam>(speckleInfBeam.applicationId),
        Direction = speckleInfBeam.direction.ToNative(),
        Element = elementIndex,
        Factor = speckleInfBeam.factor,
        Position = speckleInfBeam.position,
        Sid = speckleObject.id,
        Type = speckleInfBeam.type.ToNative(),
      };
      return new List<GsaRecord>() { gsaInfBeam };
    }
    
    private List<GsaRecord> InfNodeToNative(Base speckleObject)
    {
      var speckleInfNode = (GSAInfluenceNode)speckleObject;
      GetAxis(speckleInfNode.axis, out var gsaRefType, out var axisIndex);
      var nodeIndex = ((GsaNode)(NodeToNative(speckleInfNode.node).First())).Index;
      var gsaInfBeam = new GsaInfNode()
      {
        ApplicationId = speckleObject.applicationId,
        Index = Instance.GsaModel.Cache.ResolveIndex<GsaInfNode>(speckleInfNode.applicationId),
        Name = speckleInfNode.name,
        Direction = speckleInfNode.direction.ToNative(),
        Factor = speckleInfNode.factor,
        Sid = speckleObject.id,
        Type = speckleInfNode.type.ToNative(),
        AxisIndex = axisIndex,
        Node = nodeIndex
      };
      return new List<GsaRecord>() { gsaInfBeam };
    }
    
    private List<GsaRecord> PathToNative(Base speckleObject)
    {
      var specklePath = (GSAPath)speckleObject;
      var alignmentIndex = ((GsaAlign)(AlignToNative(specklePath.alignment)).First()).Index;
      var gsaPath = new GsaPath()
      {
        ApplicationId = speckleObject.applicationId,
        Index = Instance.GsaModel.Cache.ResolveIndex<GsaPath>(specklePath.applicationId),
        Name = specklePath.name,
        Sid = speckleObject.id,
        Factor = specklePath.factor,
        Alignment = alignmentIndex,
        Group = specklePath.group,
        Left = specklePath.left,
        Right = specklePath.right,
        NumMarkedLanes = specklePath.numMarkedLanes,
        Type = specklePath.type.ToNative(),
      };
      return new List<GsaRecord>() { gsaPath };
    }
    
    #endregion

    #region Analysis Stage
    
    public List<GsaRecord> AnalStageToNative(Base speckleObject)
    {
      var analStage = (GSAStage)speckleObject;
      var gsaAnalStage = new GsaAnalStage()
      {
        Name = analStage.name,
        Days = analStage.stageTime,
        Colour = analStage.colour.ColourToNative(),
        ElementIndices = analStage.elements.Select(x => GetElementIndex(x)).ToList(),
        LockElementIndices = analStage.lockedElements.Select(x => ((GSAElement1D)x).nativeId).ToList(),
        Phi = analStage.creepFactor,
      };
      return new List<GsaRecord>() { gsaAnalStage };
    }

    #endregion

    #endregion

    #region Helper

    private int GetElementIndex(object obj)
    {
      if (obj is GSAElement1D element1D)
        return element1D.nativeId;
      else if (obj is GSAElement2D element2D)
        return element2D.nativeId;
      else
        return -1;
    }
    
    #region ToNative
    #region Geometry
    #region Axis
    private bool GetAxis(Axis speckleAxis, out NodeAxisRefType gsaAxisRefType, out int? gsaAxisIndex)
    {
      gsaAxisRefType = NodeAxisRefType.NotSet;
      gsaAxisIndex = null;
      if (speckleAxis.definition.IsGlobal())
      {
        gsaAxisRefType = NodeAxisRefType.Global;
      }
      else if (speckleAxis.definition.IsXElevation())
      {
        gsaAxisRefType = NodeAxisRefType.XElevation;
      }
      else if (speckleAxis.definition.IsYElevation())
      {
        gsaAxisRefType = NodeAxisRefType.YElevation;
      }
      else if (speckleAxis.definition.IsVertical())
      {
        gsaAxisRefType = NodeAxisRefType.Vertical;
      }
      else
      {
        gsaAxisRefType = NodeAxisRefType.Reference;
        gsaAxisIndex = Instance.GsaModel.Cache.ResolveIndex<GsaAxis>(speckleAxis.applicationId);
      }
      
      return true;
    }
    #endregion

    #region Node
    private bool GetRestraint(Restraint speckleRestraint, out NodeRestraint gsaNodeRestraint, out List<GwaAxisDirection6> gsaRestraint)
    {
      gsaRestraint = null; //default

      switch(speckleRestraint.code)
      {
        case "RRRRRR":
          gsaNodeRestraint = NodeRestraint.Free;
          break;
        case "FFFRRR":
          gsaNodeRestraint = NodeRestraint.Pin;
          break;
        case "FFFFFF":
          gsaNodeRestraint = NodeRestraint.Fix;
          break;
        default:
          gsaNodeRestraint = NodeRestraint.Custom;
          gsaRestraint = new List<GwaAxisDirection6>();
          int i = 0;
          foreach(char c in speckleRestraint.code)
          {
            if (c == 'F')
            {
              if (i == 0) gsaRestraint.Add(GwaAxisDirection6.X);
              else if (i == 1) gsaRestraint.Add(GwaAxisDirection6.Y);
              else if (i == 2) gsaRestraint.Add(GwaAxisDirection6.Z);
              else if (i == 3) gsaRestraint.Add(GwaAxisDirection6.XX);
              else if (i == 4) gsaRestraint.Add(GwaAxisDirection6.YY);
              else if (i == 5) gsaRestraint.Add(GwaAxisDirection6.ZZ);
            }
            i++;
          }
          break;
      }
      return true;
    }
    private int GetIndexFromNode(Node speckleNode)
    {
      return Instance.GsaModel.Cache.ResolveIndex<GsaNode>(speckleNode.applicationId);
    }
    #endregion

    #region Element
    private bool GetReleases(Restraint speckleRelease, out Dictionary<GwaAxisDirection6,ReleaseCode> gsaRelease, out List<double> gsaStiffnesses, out ReleaseInclusion gsaReleaseInclusion)
    {
      if (speckleRelease.code == "FFFFFF")
      {
        gsaReleaseInclusion = ReleaseInclusion.NotIncluded;
        gsaRelease = null;
        gsaStiffnesses = null;
      }
      else if (speckleRelease.code.ToUpperInvariant().IndexOf('K') > 0)
      {
        gsaReleaseInclusion = ReleaseInclusion.Stiff;
        gsaRelease = speckleRelease.code.ReleasesToNative();
        gsaStiffnesses = new List<double>();
        if (speckleRelease.stiffnessX > 0) gsaStiffnesses.Add(speckleRelease.stiffnessX);
        if (speckleRelease.stiffnessY > 0) gsaStiffnesses.Add(speckleRelease.stiffnessY);
        if (speckleRelease.stiffnessZ > 0) gsaStiffnesses.Add(speckleRelease.stiffnessZ);
        if (speckleRelease.stiffnessXX > 0) gsaStiffnesses.Add(speckleRelease.stiffnessXX);
        if (speckleRelease.stiffnessYY > 0) gsaStiffnesses.Add(speckleRelease.stiffnessYY);
        if (speckleRelease.stiffnessZZ > 0) gsaStiffnesses.Add(speckleRelease.stiffnessZZ);
      }
      else
      {
        gsaReleaseInclusion = ReleaseInclusion.Included;
        gsaRelease = speckleRelease.code.ReleasesToNative();
        gsaStiffnesses = null;
      }
      return true;
    }
    #endregion
    #endregion
    #endregion
    #endregion
  }
}
