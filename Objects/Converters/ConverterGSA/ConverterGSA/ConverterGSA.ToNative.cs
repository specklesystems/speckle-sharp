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
        //Material
        { typeof(GSASteel), SteelToNative },
        //Property
        { typeof(Property1D), Property1dToNative },
      };
    }

    #region ToNative
    //TO DO: implement conversion code for ToNative

    #region Geometry
    private List<GsaRecord> AxisToNative(Base @object)
    {
      var axis = (Axis)@object;

      var index = Instance.GsaModel.Cache.ResolveIndex<GsaAxis>(axis.applicationId);

      return new List<GsaRecord>
      {
        new GsaAxis()
        {
          ApplicationId = axis.applicationId,
          Name = axis.name,
          Index = index,
          OriginX = axis.definition.origin.x,
          OriginY = axis.definition.origin.y,
          OriginZ = axis.definition.origin.z
        }
      };
    }

    private List<GsaRecord> NodeToNative(Base speckleObject)
    {
      //TO DO: update conversion code for colour, restraints, axis
      var speckleNode = (GSANode)speckleObject;
      var gsaNode = new GsaNode()
      {
        ApplicationId = speckleNode.applicationId,
        Name = speckleNode.name,
        Colour = Colour.NO_RGB,
        X = speckleNode.basePoint.x,
        Y = speckleNode.basePoint.y,
        Z = speckleNode.basePoint.z,
        NodeRestraint = NodeRestraint.Free,
        Restraints = null,
        AxisRefType = NodeAxisRefType.Global,
        AxisIndex = null,
        MeshSize = speckleNode.localElementSize,
        
        //NodeRestraint = GetRestraint(speckleNode.restraint);
      };
      var nodeIndex = Instance.GsaModel.Cache.ResolveIndex<GsaNode>(speckleNode.applicationId);
      if (nodeIndex > 0) gsaNode.Index = nodeIndex;
      if (speckleNode.springProperty != null)
      {
        var springIndex = Instance.GsaModel.Cache.ResolveIndex<GsaPropSpr>(speckleNode.springProperty.applicationId);
        if (springIndex > 0) gsaNode.SpringPropertyIndex = springIndex;
      }
      if (speckleNode.massProperty != null)
      {
        var massIndex = Instance.GsaModel.Cache.ResolveIndex<GsaPropMass>(speckleNode.massProperty.applicationId);
        if (massIndex > 0) gsaNode.MassPropertyIndex = massIndex;
      }

      return new List<GsaRecord>() { gsaNode };
    }

    private List<GsaRecord> Element1dToNative(Base speckleObject)
    {
      var speckleElement = (GSAElement1D)speckleObject;
      var gsaElement = new GsaEl()
      {
        ApplicationId = speckleElement.applicationId,
        Name = speckleElement.name,
        Colour = Colour.NO_RGB,
        Type = ElementType.Beam,
        TaperOffsetPercentageEnd1 = null, //double?
        TaperOffsetPercentageEnd2 = null, //double?
        Group = null, //int?
        NodeIndices = new List<int> { 1, 2 },
        OrientationNodeIndex = null, //int?
        Angle = 0, //double?
        ReleaseInclusion = ReleaseInclusion.NotIncluded,
        Releases1 = null, //Dictionary<AxisDirection6, ReleaseCode>
        Stiffnesses1 = null, //List<double>
        Releases2 = null, //Dictionary<AxisDirection6, ReleaseCode>
        Stiffnesses2 = null, //List<double>
        End1OffsetX = null, //double?
        End2OffsetX = null, //double?
        OffsetY = null, //double?
        OffsetZ = null, //double?
        Dummy = false,
        ParentIndex = null, //int?
      };
      var elementIndex = Instance.GsaModel.Cache.ResolveIndex<GsaEl>(speckleElement.applicationId);
      var propertyIndex = Instance.GsaModel.Cache.ResolveIndex<GsaSection>(speckleElement.property.applicationId);
      if (elementIndex > 0) gsaElement.Index = elementIndex;
      if (propertyIndex > 0) gsaElement.PropertyIndex = propertyIndex;
      return new List<GsaRecord>() { gsaElement };
    }

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
    #endregion

    #endregion
  }
}
