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
using GwaAxisDirection6 = Speckle.GSA.API.GwaSchema.AxisDirection6;
using Restraint = Objects.Structural.Geometry.Restraint;

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
        //Material
        { typeof(GSASteel), SteelToNative },
        //Property
        { typeof(Property1D), Property1dToNative },
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
        Colour = speckleElement.colour.ColourToNative(),
        Type = speckleElement.type.ToNative(),
        //TaperOffsetPercentageEnd1 - currently not supported
        //TaperOffsetPercentageEnd2 - currently not supported
        NodeIndices = speckleElement.topology.Select(n => GetIndexFromNode(n)).ToList(),
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
    private List<GsaRecord> Property1dToNative(Base speckleObject)
    {
      var speckleProperty = (GSAProperty1D)speckleObject;
      var gsaProperty = new GsaSection()
      {
        Index = Instance.GsaModel.Cache.ResolveIndex<GsaSection>(speckleProperty.applicationId),
        Name = "",
        ApplicationId = speckleProperty.applicationId,
        Colour = Colour.NO_RGB,
        Type = Section1dType.Generic,
        //PoolIndex = 0,
        ReferencePoint = ReferencePoint.Centroid,
        RefY = 0,
        RefZ = 0,
        Mass = 0,
        Fraction = 1,
        Cost = 0,
        Left = 0,
        Right = 0,
        Slab = 0,
        Components = new List<GsaSectionComponentBase>()
        {
          new SectionComp()
          {
            Name = "",
            //MatAnalIndex = 0,
            MaterialType = Section1dMaterialType.STEEL,
            MaterialIndex = 1,
            OffsetY = 0,
            OffsetZ = 0,
            Rotation = 0,
            Reflect = ComponentReflection.NONE,
            //Pool = 0,
            TaperType = Section1dTaperType.NONE,
            //TaperPos = 0
            ProfileGroup = Section1dProfileGroup.Catalogue,
            ProfileDetails = new ProfileDetailsCatalogue()
            {
              Group = Section1dProfileGroup.Catalogue,
              Profile = "CAT A-UB 610UB125 19981201"
            }
          },
          new SectionSteel()
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
        },
        Environ = false
      };
      return new List<GsaRecord>() { gsaProperty };
    }
    #endregion
    #endregion

    #region Helper
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
