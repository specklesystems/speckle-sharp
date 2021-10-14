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
using Objects.Structural.GSA.Loading;
using Objects.Structural.GSA.Analysis;
using Objects.Structural.GSA.Bridge;
using GwaAxisDirection6 = Speckle.GSA.API.GwaSchema.AxisDirection6;
using Restraint = Objects.Structural.Geometry.Restraint;
using Speckle.Core.Kits;
using Objects.Structural.Loading;
using Objects.Structural;

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
        { typeof(Element1D), Element1dToNative },
        { typeof(GSAElement2D), Element2dToNative },
        //Loading
        { typeof(GSALoadCase), LoadCaseToNative },
        { typeof(GSAAnalysisCase), AnalysisCaseToNative },
        { typeof(GSALoadCombination), LoadCombinationToNative },
        { typeof(GSALoadBeam), LoadBeamToNative },
        { typeof(GSALoadFace), LoadFaceToNative },
        { typeof(GSALoadNode), LoadNodeToNative },
        { typeof(GSALoadGravity), LoadGravityToNative },
        { typeof(GSALoadThermal2d), LoadThermal2dToNative },
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
        Index = speckleElement.ResolveElementIndex(),
        Name = speckleElement.name,
        Colour = speckleElement.colour?.ColourToNative() ?? Colour.NotSet,
        Type = speckleElement.type.ToNative(),
        //TaperOffsetPercentageEnd1 - currently not supported
        //TaperOffsetPercentageEnd2 - currently not supported
        NodeIndices = speckleElement.topology?.Select(n => GetIndexFromNode(n)).ToList() ?? new List<int>(),
        Dummy = speckleElement.isDummy,
      };
      if (speckleElement.property != null) gsaElement.PropertyIndex = speckleElement.property.ResolveIndex();
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
      if (speckleElement.end1Offset.x != 0)       gsaElement.End1OffsetX = speckleElement.end1Offset.x;
      if (speckleElement.orientationAngle != 0)   gsaElement.Angle = speckleElement.orientationAngle;
      if (speckleElement.orientationNode != null) gsaElement.OrientationNodeIndex = speckleElement.orientationNode.ResolveIndex();
      if (speckleElement.group > 0)               gsaElement.Group = speckleElement.group;
      if (speckleElement.parent != null) gsaElement.ParentIndex = speckleElement.parent.ResolveMemberIndex(); 
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
    private List<GsaRecord> LoadCaseToNative(Base speckleObject)
    {
      var speckleLoadCase = (GSALoadCase)speckleObject;
      var gsaLoadCase = new GsaLoadCase()
      {
        ApplicationId = speckleLoadCase.applicationId,
        Index = Instance.GsaModel.Cache.ResolveIndex<GsaLoadCase>(speckleLoadCase.applicationId),
        Title = speckleLoadCase.name,
        CaseType = speckleLoadCase.loadType.ToNative(),
        Category = speckleLoadCase.description.LoadCategoryToNative(),
        Direction = speckleLoadCase.direction.ToNative(),
        Include = speckleLoadCase.include.IncludeOptionToNative(),
        Source = int.Parse(speckleLoadCase.group),
      };
      if (speckleLoadCase.bridge) gsaLoadCase.Bridge = true;
      return new List<GsaRecord>() { gsaLoadCase };
    }

    private List<GsaRecord> AnalysisCaseToNative(Base speckleObject)
    {
      var speckleCase = (GSAAnalysisCase)speckleObject;
      var gsaCase = new GsaAnal()
      {
        ApplicationId = speckleCase.applicationId,
        Index = Instance.GsaModel.Cache.ResolveIndex<GsaAnal>(speckleCase.applicationId),
        Name = speckleCase.name,
        //TaskIndex = Instance.GsaModel.Cache.ResolveIndex<GsaTask>(speckleCase.task.applicationId), //TODO:
        Desc = GetAnalysisCaseDescription(speckleCase.loadCases, speckleCase.loadFactors),
      };
      return new List<GsaRecord>() { gsaCase };
    }

    private List<GsaRecord> LoadCombinationToNative(Base speckleObject)
    {
      var speckleLoadCombination = (GSALoadCombination)speckleObject;
      var gsaLoadCombination = new GsaCombination()
      {
        ApplicationId = speckleLoadCombination.applicationId,
        Index = speckleLoadCombination.ResolveIndex(),
        Name = speckleLoadCombination.name,
        Desc = GetLoadCombinationDescription(speckleLoadCombination.combinationType, speckleLoadCombination.loadCases, speckleLoadCombination.loadFactors),
      };

      //Add dynamic properties
      var members = speckleLoadCombination.GetMembers();
      if (members.ContainsKey("bridge") && speckleLoadCombination["bridge"] is bool && (bool)speckleLoadCombination["bridge"]) gsaLoadCombination.Bridge = true;
      if (members.ContainsKey("note") && speckleLoadCombination["note"] is string) gsaLoadCombination.Note = speckleLoadCombination["note"] as string;

      return new List<GsaRecord>() { gsaLoadCombination };
    }

    #region LoadBeam
    private List<GsaRecord> LoadBeamToNative(Base speckleObject)
    {
      var speckleLoad = (GSALoadBeam)speckleObject;
      GsaLoadBeam gsaLoad = null;

      var fns = new Dictionary<BeamLoadType, Func<GSALoadBeam, GsaLoadBeam>>
      { { BeamLoadType.Uniform, LoadBeamUniformToNative },
        { BeamLoadType.Linear, LoadBeamLinearToNative },
        { BeamLoadType.Point, LoadBeamPointToNative },
        { BeamLoadType.Patch, LoadBeamPatchToNative },
        { BeamLoadType.TriLinear, LoadBeamTriLinearToNative },
      };
      //Apply spring type specific properties
      if (fns.ContainsKey(speckleLoad.loadType)) gsaLoad = fns[speckleLoad.loadType](speckleLoad);
      else
      {
        ConversionErrors.Add(new Exception("LoadBeamToNative: beam load type (" + speckleLoad.loadType.ToString() + ") is not currently supported"));
      }

      return new List<GsaRecord>() { gsaLoad };
    }

    private GsaLoadBeam LoadBeamUniformToNative(GSALoadBeam speckleLoad)
    {
      var gsaLoad = LoadBeamBaseToNative<GsaLoadBeamUdl>(speckleLoad);
      if (speckleLoad.values != null) gsaLoad.Load = speckleLoad.values[0];
      return gsaLoad;
    }

    private GsaLoadBeam LoadBeamLinearToNative(GSALoadBeam speckleLoad)
    {
      var gsaLoad = LoadBeamBaseToNative<GsaLoadBeamLine>(speckleLoad);
      if (speckleLoad.values != null && speckleLoad.values.Count() >= 2)
      {
        gsaLoad.Load1 = speckleLoad.values[0];
        gsaLoad.Load2 = speckleLoad.values[1];
      }
      return gsaLoad;
    }

    private GsaLoadBeam LoadBeamPointToNative(GSALoadBeam speckleLoad)
    {
      var gsaLoad = LoadBeamBaseToNative<GsaLoadBeamPoint>(speckleLoad);
      if (speckleLoad.values != null) gsaLoad.Load = speckleLoad.values[0];
      if (speckleLoad.positions != null) gsaLoad.Position = speckleLoad.positions[0];
      return gsaLoad;
    }

    private GsaLoadBeam LoadBeamPatchToNative(GSALoadBeam speckleLoad)
    {
      var gsaLoad = LoadBeamBaseToNative<GsaLoadBeamPatch>(speckleLoad);
      if (speckleLoad.values != null && speckleLoad.values.Count() >= 2)
      {
        gsaLoad.Load1 = speckleLoad.values[0];
        gsaLoad.Load2 = speckleLoad.values[1];
      }
      if (speckleLoad.positions != null && speckleLoad.positions.Count() >= 2)
      {
        gsaLoad.Position1 = speckleLoad.positions[0];
        gsaLoad.Position2Percent = speckleLoad.positions[1];
      }
      return gsaLoad;
    }

    private GsaLoadBeam LoadBeamTriLinearToNative(GSALoadBeam speckleLoad)
    {
      var gsaLoad = LoadBeamBaseToNative<GsaLoadBeamTrilin>(speckleLoad);
      if (speckleLoad.values != null && speckleLoad.values.Count() >= 2)
      {
        gsaLoad.Load1 = speckleLoad.values[0];
        gsaLoad.Load2 = speckleLoad.values[1];
      }
      if (speckleLoad.positions != null && speckleLoad.positions.Count() >= 2)
      {
        gsaLoad.Position1 = speckleLoad.positions[0];
        gsaLoad.Position2Percent = speckleLoad.positions[1];
      }
      return gsaLoad;
    }

    private T LoadBeamBaseToNative<T>(GSALoadBeam speckleLoad) where T : GsaLoadBeam
    {
      var gsaLoad = (T)Activator.CreateInstance(typeof(T), new object());
      gsaLoad.ApplicationId = speckleLoad.applicationId;
      gsaLoad.Index = Instance.GsaModel.Cache.ResolveIndex<T>(speckleLoad.applicationId);
      gsaLoad.Name = speckleLoad.name;
      gsaLoad.LoadCaseIndex = speckleLoad.loadCase.ResolveIndex();
      gsaLoad.Projected = speckleLoad.isProjected;
      gsaLoad.LoadDirection = speckleLoad.direction.ToNative();
      if (speckleLoad.elements != null)
      {
        gsaLoad.ElementIndices = speckleLoad.elements.GetElementIndicies();
        gsaLoad.MemberIndices = speckleLoad.elements.GetMemberIndicies();
      }
      if (GetLoadBeamAxis(speckleLoad.loadAxis, out var gsaAxisRefType, out var gsaAxisIndex))
      {
        gsaLoad.AxisRefType = gsaAxisRefType;
        gsaLoad.AxisIndex = gsaAxisIndex;
      }
      return gsaLoad;
    }
    #endregion

    private List<GsaRecord> LoadFaceToNative(Base speckleObject)
    {
      var speckleLoad = (GSALoadFace)speckleObject;
      var gsaLoad = new GsaLoad2dFace()
      {
        ApplicationId = speckleLoad.applicationId,
        Index = speckleLoad.ResolveIndex(),
        Name = speckleLoad.name,
        Type = speckleLoad.loadType.ToNative(),
        LoadCaseIndex = speckleLoad.loadCase.ResolveIndex(),
        Values = speckleLoad.values,
        LoadDirection = speckleLoad.direction.ToNative(),
        Projected = speckleLoad.isProjected,
      };
      if (speckleLoad.elements != null)
      {
        gsaLoad.ElementIndices = speckleLoad.elements.GetElementIndicies();
        gsaLoad.MemberIndices = speckleLoad.elements.GetMemberIndicies();
      }
      if (GetLoadFaceAxis(speckleLoad.loadAxis, speckleLoad.loadAxisType, out var gsaAxisRefType, out var gsaAxisIndex))
      {
        gsaLoad.AxisRefType = gsaAxisRefType;
        gsaLoad.AxisIndex = gsaAxisIndex;
      }
      if (speckleLoad.positions != null && speckleLoad.positions.Count() >= 2)
      {
        gsaLoad.R = speckleLoad.positions[0];
        gsaLoad.S = speckleLoad.positions[1];
      }

      return new List<GsaRecord>() { gsaLoad };
    }

    private List<GsaRecord> LoadNodeToNative(Base speckleObject)
    {
      var speckleLoad = (GSALoadNode)speckleObject;
      var gsaLoad = new GsaLoadNode()
      {
        ApplicationId = speckleLoad.applicationId,
        Index = speckleLoad.ResolveIndex(),
        Name = speckleLoad.name,
        LoadDirection = speckleLoad.direction.ToNative(),
        LoadCaseIndex = speckleLoad.loadCase.ResolveIndex(),
      };
      if (speckleLoad.value != 0) gsaLoad.Value = speckleLoad.value;
      if (speckleLoad.nodes != null) gsaLoad.NodeIndices = speckleLoad.nodes.GetIndicies();
      if (speckleLoad.loadAxis.definition.IsGlobal())
      {
        gsaLoad.GlobalAxis = true;
      }
      else
      {
        gsaLoad.GlobalAxis = false;
        gsaLoad.AxisIndex = speckleLoad.loadAxis.ResolveIndex();
      }
      return new List<GsaRecord>() { gsaLoad };
    }

    private List<GsaRecord> LoadGravityToNative(Base speckleObject)
    {
      var speckleLoad = (GSALoadGravity)speckleObject;
      var gsaLoad = new GsaLoadGravity()
      {
        ApplicationId = speckleLoad.applicationId,
        Index = speckleLoad.ResolveIndex(),
        Name = speckleLoad.name,
        LoadCaseIndex = speckleLoad.loadCase.ResolveIndex(),
      };
      if (speckleLoad.nodes != null) gsaLoad.Nodes = speckleLoad.nodes.Select(o => (Node)o).ToList().GetIndicies();
      if (speckleLoad.elements != null)
      {
        gsaLoad.ElementIndices = speckleLoad.elements.GetElementIndicies();
        gsaLoad.MemberIndices = speckleLoad.elements.GetMemberIndicies();
      }
      if (speckleLoad.gravityFactors.x != 0) gsaLoad.X = speckleLoad.gravityFactors.x;
      if (speckleLoad.gravityFactors.y != 0) gsaLoad.Y = speckleLoad.gravityFactors.y;
      if (speckleLoad.gravityFactors.z != 0) gsaLoad.Z = speckleLoad.gravityFactors.z;

      return new List<GsaRecord>() { gsaLoad };
    }

    private List<GsaRecord> LoadThermal2dToNative(Base speckleObject)
    {
      var speckleLoad = (GSALoadThermal2d)speckleObject;
      var gsaLoad = new GsaLoad2dThermal()
      {
        ApplicationId = speckleLoad.applicationId,
        Index = speckleLoad.ResolveIndex(),
        Name = speckleLoad.name,
        LoadCaseIndex = speckleLoad.loadCase.ResolveIndex(),
        Type = speckleLoad.type.ToNative(),
        Values = speckleLoad.values,
      };
      if (speckleLoad.elements != null)
      {
        var speckleElements = speckleLoad.elements.Select(o => (Base)o).ToList();
        gsaLoad.ElementIndices = speckleElements.GetElementIndicies();
        gsaLoad.MemberIndices = speckleElements.GetMemberIndicies();
      }
      return new List<GsaRecord>() { gsaLoad };
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
    #region Loading
    private bool GetLoadBeamAxis(Axis speckleAxis, out LoadBeamAxisRefType gsaAxisRefType, out int? gsaAxisIndex)
    {
      gsaAxisIndex = null;
      if (speckleAxis.definition.IsGlobal())
      {
        gsaAxisRefType = LoadBeamAxisRefType.Global;
      }
      else
      {
        gsaAxisIndex = Instance.GsaModel.Cache.ResolveIndex<GsaAxis>(speckleAxis.applicationId);
        if (gsaAxisIndex == null)
        {
          //TODO: handle local, and natural cases
          gsaAxisRefType = LoadBeamAxisRefType.NotSet;
          return false;
        }
        else
        {
          gsaAxisRefType = LoadBeamAxisRefType.Reference;
        }
      }

      return true;
    }

    private bool GetLoadFaceAxis(Axis speckleAxis, LoadAxisType speckleAxisType, out AxisRefType gsaAxisRefType, out int? gsaAxisIndex)
    {
      gsaAxisIndex = null;
      if (speckleAxis == null)
      {
        gsaAxisRefType = speckleAxisType.ToNative();
      }
      else if (speckleAxis.definition.IsGlobal())
      {
        gsaAxisRefType = AxisRefType.Global;
      }
      else
      {
        gsaAxisIndex = Instance.GsaModel.Cache.LookupIndex<GsaAxis>(speckleAxis.applicationId);
        if (gsaAxisIndex == null)
        {
          //TODO: handle local, and natural cases
          gsaAxisRefType = AxisRefType.NotSet;
          return false;
        }
        else
        {
          gsaAxisRefType = AxisRefType.Reference;
        }
      }

      return true;
    }

    private string GetAnalysisCaseDescription(List<LoadCase> speckleLoadCases, List<double> speckleLoadFactors)
    {
      var gsaDescription = "";
      for (var i = 0; i < speckleLoadCases.Count(); i++)
      {
        if (i > 0 && speckleLoadFactors[i] > 0) gsaDescription += "+";
        if (speckleLoadFactors[i] == 1)
        {
          //Do nothing
        }
        else if (speckleLoadFactors[i] == -1)
        {
          gsaDescription += "-";
        }
        else
        {
          gsaDescription += speckleLoadFactors[i].ToString();
        }
        gsaDescription += "L" + speckleLoadCases[i].ResolveIndex();
      }
      return gsaDescription;
    }

    private string GetLoadCombinationDescription(CombinationType type, List<Base> loadCases, List<double> loadFactors)
    {
      if (type != CombinationType.LinearAdd) return null; //TODO - handle other cases
      var desc = "";

      for (var i = 0; i < loadCases.Count(); i++)
      {
        if (i > 0 && loadFactors[i] > 0) desc += "+";
        if (loadFactors[i] == 1)
        {
          //Do nothing
        }
        else if (loadFactors[i] == -1)
        {
          desc += "-";
        }
        else
        {
          desc += loadFactors[i].ToString();
        }
        if (loadCases[i].GetType() == typeof(GSALoadCombination))
        {
          desc += "C" + Instance.GsaModel.Cache.ResolveIndex<GsaCombination>(loadCases[i].applicationId);
        }
        else if (loadCases[i].GetType() == typeof(GSAAnalysisCase))
        {
          desc += "A" + Instance.GsaModel.Cache.ResolveIndex<GsaAnal>(loadCases[i].applicationId);
        }
        else
        {
          return null;
        }
        
      }
      return desc;
    }
    #endregion
    #endregion
  }
}
