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
using Objects.Structural.Materials;
using Objects;
using Objects.Structural.Properties.Profiles;

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
        { typeof(GSALoadCase), GSALoadCaseToNative },
        { typeof(LoadCase), LoadCaseToNative },
        { typeof(GSAAnalysisCase), GSAAnalysisCaseToNative },
        { typeof(GSALoadCombination), GSALoadCombinationToNative },
        { typeof(LoadCombination), LoadCombinationToNative },
        { typeof(GSALoadBeam), GSALoadBeamToNative },
        { typeof(LoadBeam), LoadBeamToNative },
        { typeof(GSALoadFace), GSALoadFaceToNative },
        { typeof(LoadFace), LoadFaceToNative },
        { typeof(GSALoadNode), GSALoadNodeToNative },
        { typeof(LoadNode), LoadNodeToNative },
        { typeof(GSALoadGravity), GSALoadGravityToNative },
        { typeof(LoadGravity), LoadGravityToNative },
        { typeof(GSALoadThermal2d), GSALoadThermal2dToNative },
        //Materials
        { typeof(GSASteel), GSASteelToNative },
        { typeof(Steel), SteelToNative },
        { typeof(GSAConcrete), GSAConcreteToNative },
        { typeof(Concrete), ConcreteToNative },
        //Properties
        { typeof(Property1D), Property1dToNative },
        { typeof(GSAProperty1D), GsaProperty1dToNative },
        // Bridge
        { typeof(GSAInfluenceNode), InfNodeToNative},
        { typeof(GSAInfluenceBeam), InfBeamToNative},
        {typeof(GSAAlignment), AlignToNative},
        {typeof(GSAPath), PathToNative},
        // Analysis
        {typeof(GSAStage), AnalStageToNative},
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
    private List<GsaRecord> GSALoadCaseToNative(Base speckleObject)
    {
      var gsaLoadCase = (GsaLoadCase)LoadCaseToNative(speckleObject).First(o => o is GsaLoadCase);
      var speckleLoadCase = (GSALoadCase)speckleObject;
      gsaLoadCase.Direction = speckleLoadCase.direction.ToNative();
      gsaLoadCase.Include = speckleLoadCase.include.IncludeOptionToNative();
      if (speckleLoadCase.bridge) gsaLoadCase.Bridge = true;
      return new List<GsaRecord>() { gsaLoadCase };
    }

    private List<GsaRecord> LoadCaseToNative(Base speckleObject)
    {
      var speckleLoadCase = (LoadCase)speckleObject;
      var gsaLoadCase = new GsaLoadCase()
      {
        ApplicationId = speckleLoadCase.applicationId,
        Index = Instance.GsaModel.Cache.ResolveIndex<GsaLoadCase>(speckleLoadCase.applicationId),
        Title = speckleLoadCase.name,
        CaseType = speckleLoadCase.loadType.ToNative(),
        Category = speckleLoadCase.description.LoadCategoryToNative(),
        Source = int.Parse(speckleLoadCase.group),
      };
      return new List<GsaRecord>() { gsaLoadCase };
    }

    private List<GsaRecord> GSAAnalysisCaseToNative(Base speckleObject)
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

    private List<GsaRecord> GSALoadCombinationToNative(Base speckleObject)
    {
      var gsaLoadCombination = (GsaCombination)LoadCombinationToNative(speckleObject).First(o => o is GsaCombination);
      var speckleLoadCombination = (GSALoadCombination)speckleObject;
      var members = speckleLoadCombination.GetMembers();
      if (members.ContainsKey("bridge") && speckleLoadCombination["bridge"] is bool && (bool)speckleLoadCombination["bridge"]) gsaLoadCombination.Bridge = true;
      if (members.ContainsKey("note") && speckleLoadCombination["note"] is string) gsaLoadCombination.Note = speckleLoadCombination["note"] as string;
      return new List<GsaRecord>() { gsaLoadCombination };
    }

    private List<GsaRecord> LoadCombinationToNative(Base speckleObject)
    {
      var speckleLoadCombination = (LoadCombination)speckleObject;
      var gsaLoadCombination = new GsaCombination()
      {
        ApplicationId = speckleLoadCombination.applicationId,
        Index = speckleLoadCombination.ResolveIndex(),
        Name = speckleLoadCombination.name,
        Desc = GetLoadCombinationDescription(speckleLoadCombination.combinationType, speckleLoadCombination.loadCases, speckleLoadCombination.loadFactors),
      };
      return new List<GsaRecord>() { gsaLoadCombination };
    }

    #region LoadBeam
    private List<GsaRecord> GSALoadBeamToNative(Base speckleObject)
    {
      var gsaLoad = (GsaLoadBeam)LoadBeamToNative(speckleObject).First(o => o is GsaLoadBeam);
      var speckleLoad = (GSALoadBeam)speckleObject;
      //Add any app specific conversions here
      return new List<GsaRecord>() { gsaLoad };
    }

    private List<GsaRecord> LoadBeamToNative(Base speckleObject)
    {
      var speckleLoad = (LoadBeam)speckleObject;
      GsaLoadBeam gsaLoad = null;

      var fns = new Dictionary<BeamLoadType, Func<LoadBeam, GsaLoadBeam>>
      { { BeamLoadType.Uniform, LoadBeamUniformToNative },
        { BeamLoadType.Linear, LoadBeamLinearToNative },
        { BeamLoadType.Point, LoadBeamPointToNative },
        { BeamLoadType.Patch, LoadBeamPatchToNative },
        { BeamLoadType.TriLinear, LoadBeamTriLinearToNative },
      };

      //Apply spring type specific properties
      if (fns.ContainsKey(speckleLoad.loadType))
      {
        gsaLoad = fns[speckleLoad.loadType](speckleLoad);
      }
      else
      {
        ConversionErrors.Add(new Exception("LoadBeamToNative: beam load type (" + speckleLoad.loadType.ToString() + ") is not currently supported"));
      }

      return new List<GsaRecord>() { gsaLoad };
    }

    private GsaLoadBeam LoadBeamUniformToNative(LoadBeam speckleLoad)
    {
      var gsaLoad = LoadBeamBaseToNative<GsaLoadBeamUdl>(speckleLoad);
      if (speckleLoad.values != null) gsaLoad.Load = speckleLoad.values[0];
      return gsaLoad;
    }

    private GsaLoadBeam LoadBeamLinearToNative(LoadBeam speckleLoad)
    {
      var gsaLoad = LoadBeamBaseToNative<GsaLoadBeamLine>(speckleLoad);
      if (speckleLoad.values != null && speckleLoad.values.Count() >= 2)
      {
        gsaLoad.Load1 = speckleLoad.values[0];
        gsaLoad.Load2 = speckleLoad.values[1];
      }
      return gsaLoad;
    }

    private GsaLoadBeam LoadBeamPointToNative(LoadBeam speckleLoad)
    {
      var gsaLoad = LoadBeamBaseToNative<GsaLoadBeamPoint>(speckleLoad);
      if (speckleLoad.values != null) gsaLoad.Load = speckleLoad.values[0];
      if (speckleLoad.positions != null) gsaLoad.Position = speckleLoad.positions[0];
      return gsaLoad;
    }

    private GsaLoadBeam LoadBeamPatchToNative(LoadBeam speckleLoad)
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

    private GsaLoadBeam LoadBeamTriLinearToNative(LoadBeam speckleLoad)
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

    private T LoadBeamBaseToNative<T>(LoadBeam speckleLoad) where T : GsaLoadBeam
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

    private List<GsaRecord> GSALoadFaceToNative(Base speckleObject)
    {
      var gsaLoad = (GsaLoad2dFace)LoadFaceToNative(speckleObject).First(o => o is GsaLoad2dFace);
      var speckleLoad = (GSALoadFace)speckleObject;
      //Add any app specific conversions here
      return new List<GsaRecord>() { gsaLoad };
    }

    private List<GsaRecord> LoadFaceToNative(Base speckleObject)
    {
      var speckleLoad = (LoadFace)speckleObject;
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

    private List<GsaRecord> GSALoadNodeToNative(Base speckleObject)
    {
      var gsaLoad = (GsaLoadNode)LoadNodeToNative(speckleObject).First(o => o is GsaLoadNode);
      var speckleLoad = (GSALoadNode)speckleObject;
      //Add any app specific conversions here
      return new List<GsaRecord>() { gsaLoad };
    }

    private List<GsaRecord> LoadNodeToNative(Base speckleObject)
    {
      var speckleLoad = (LoadNode)speckleObject;
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

    private List<GsaRecord> GSALoadGravityToNative(Base speckleObject)
    {
      var gsaLoad = (GsaLoadGravity)LoadGravityToNative(speckleObject).First(o => o is GsaLoadGravity);
      var speckleLoad = (GSALoadGravity)speckleObject;
      //Add any app specific conversions here
      return new List<GsaRecord>() { gsaLoad };
    }

    private List<GsaRecord> LoadGravityToNative(Base speckleObject)
    {
      var speckleLoad = (LoadGravity)speckleObject;
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

    private List<GsaRecord> GSALoadThermal2dToNative(Base speckleObject)
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
    private List<GsaRecord> GSASteelToNative(Base speckleObject)
    {
      var gsaSteel = (GsaMatSteel)SteelToNative(speckleObject).First(o => o is GsaMatSteel);
      var speckleSteel = (GSASteel)speckleObject;
      var members = speckleSteel.GetMembers();
      if (members.ContainsKey("Mat") && speckleSteel["Mat"] is Base) gsaSteel.Mat = GetMat(speckleSteel["Mat"] as Base);
      return new List<GsaRecord>() { gsaSteel };
    }

    private List<GsaRecord> SteelToNative(Base speckleObject)
    {
      //Values based on GSA10.1 with design code AS4100-1998, material grade 200-450 from AS3678
      var speckleSteel = (Steel)speckleObject;
      var gsaSteel = new GsaMatSteel()
      {
        ApplicationId = speckleSteel.applicationId,
        Index = speckleSteel.ResolveIndex(),
        Name = speckleSteel.name,
        Mat = new GsaMat()
        {
          E = speckleSteel.elasticModulus,
          F = speckleSteel.yieldStrength,
          Nu = speckleSteel.poissonsRatio,
          G = speckleSteel.shearModulus,
          Rho = speckleSteel.density,
          Alpha = speckleSteel.thermalExpansivity,
          Prop = new GsaMatAnal()
          {
            E = speckleSteel.elasticModulus,
            Nu = speckleSteel.poissonsRatio,
            Rho = speckleSteel.density,
            Alpha = speckleSteel.thermalExpansivity,
            G = speckleSteel.shearModulus,
            Damp = speckleSteel.dampingRatio,
          },
          NumUC = 0,
          AbsUC = Dimension.NotSet,
          OrdUC = Dimension.NotSet,
          PtsUC = null,
          NumSC = 0,
          AbsSC = Dimension.NotSet,
          OrdSC = Dimension.NotSet,
          PtsSC = null,
          NumUT = 0,
          AbsUT = Dimension.NotSet,
          OrdUT = Dimension.NotSet,
          PtsUT = null,
          NumST = 0,
          AbsST = Dimension.NotSet,
          OrdST = Dimension.NotSet,
          PtsST = null,
          Eps = speckleSteel.maxStrain,
          Uls = new GsaMatCurveParam()
          {
            Model = new List<MatCurveParamType>() { MatCurveParamType.ELAS_PLAS },
            StrainElasticCompression = GetSteelStrain(speckleSteel.yieldStrength),
            StrainElasticTension = GetSteelStrain(speckleSteel.yieldStrength),
            StrainPlasticCompression = GetSteelStrain(speckleSteel.yieldStrength),
            StrainPlasticTension = GetSteelStrain(speckleSteel.yieldStrength),
            StrainFailureCompression = speckleSteel.maxStrain,
            StrainFailureTension = speckleSteel.maxStrain,
            GammaF = 1,
            GammaE = 1,
          },
          Sls = new GsaMatCurveParam()
          {
            Model = new List<MatCurveParamType>() { MatCurveParamType.ELAS_PLAS },
            StrainElasticCompression = GetSteelStrain(speckleSteel.yieldStrength),
            StrainElasticTension = GetSteelStrain(speckleSteel.yieldStrength),
            StrainPlasticCompression = GetSteelStrain(speckleSteel.yieldStrength),
            StrainPlasticTension = GetSteelStrain(speckleSteel.yieldStrength),
            StrainFailureCompression = speckleSteel.maxStrain,
            StrainFailureTension = speckleSteel.maxStrain,
            GammaF = 1,
            GammaE = 1,
          },
          Cost = speckleSteel.cost,
          Type = MatType.STEEL,
        },
        Fy = speckleSteel.yieldStrength,
        Fu = speckleSteel.ultimateStrength,
        EpsP = 0,
        Eh = speckleSteel.strainHardeningModulus,
      };

      //TODO:
      //SpeckleObject:
      //  string grade
      //  string designCode
      //  string codeYear
      //  double strength
      //  double materialSafetyFactor
      return new List<GsaRecord>() { gsaSteel };
    }

    private List<GsaRecord> GSAConcreteToNative(Base speckleObject)
    {
      var gsaConcrete = (GsaMatConcrete)ConcreteToNative(speckleObject).First(o => o is GsaMatConcrete);
      var speckleConcrete = (GSAConcrete)speckleObject;
      var members = speckleConcrete.GetMembers();
      if (members.ContainsKey("Mat") && speckleConcrete["Mat"] is Base)              gsaConcrete.Mat = GetMat(speckleConcrete["Mat"] as Base);
      if (members.ContainsKey("Type") && speckleConcrete["Type"] is string)          Enum.TryParse(speckleObject["Type"] as string, true, out gsaConcrete.Type);
      if (members.ContainsKey("Cement") && speckleConcrete["Cement"] is string)      Enum.TryParse(speckleObject["Cement"] as string, true, out gsaConcrete.Cement);
      if (members.ContainsKey("Fcd") && speckleConcrete["Fcd"] is double?)           gsaConcrete.Fcd = speckleObject["Fcd"] as double?;
      if (members.ContainsKey("Fcdc") && speckleConcrete["Fcdc"] is double?)         gsaConcrete.Fcdc = speckleObject["Fcdc"] as double?;
      if (members.ContainsKey("Fcfib") && speckleConcrete["Fcfib"] is double?)       gsaConcrete.Fcfib = speckleObject["Fcfib"] as double?;
      if (members.ContainsKey("EmEs") && speckleConcrete["EmEs"] is double?)         gsaConcrete.EmEs = speckleObject["EmEs"] as double?;
      if (members.ContainsKey("N") && speckleConcrete["N"] is double?)               gsaConcrete.N = speckleObject["N"] as double?;
      if (members.ContainsKey("Emod") && speckleConcrete["Emod"] is double?)         gsaConcrete.Emod = speckleObject["Emod"] as double?;
      if (members.ContainsKey("EpsPeak") && speckleConcrete["EpsPeak"] is double?)   gsaConcrete.EpsPeak = speckleObject["EpsPeak"] as double?;
      if (members.ContainsKey("EpsMax") && speckleConcrete["EpsMax"] is double?)     gsaConcrete.EpsMax = speckleObject["EpsMax"] as double?;
      if (members.ContainsKey("EpsAx") && speckleConcrete["EpsAx"] is double?)       gsaConcrete.EpsAx = speckleObject["EpsAx"] as double?;
      if (members.ContainsKey("EpsTran") && speckleConcrete["EpsTran"] is double?)   gsaConcrete.EpsTran = speckleObject["EpsTran"] as double?;
      if (members.ContainsKey("EpsAxs") && speckleConcrete["EpsAxs"] is double?)     gsaConcrete.EpsAxs = speckleObject["EpsAxs"] as double?;
      if (members.ContainsKey("XdMin") && speckleConcrete["XdMin"] is double?)       gsaConcrete.XdMin = speckleObject["XdMin"] as double?;
      if (members.ContainsKey("XdMax") && speckleConcrete["XdMax"] is double?)       gsaConcrete.XdMax = speckleObject["XdMax"] as double?;
      if (members.ContainsKey("Beta") && speckleConcrete["Beta"] is double?)         gsaConcrete.Beta = speckleObject["Beta"] as double?;
      if (members.ContainsKey("Shrink") && speckleConcrete["Shrink"] is double?)     gsaConcrete.Shrink = speckleObject["Shrink"] as double?;
      if (members.ContainsKey("Confine") && speckleConcrete["Confine"] is double?)   gsaConcrete.Confine = speckleObject["Confine"] as double?;
      if (members.ContainsKey("Fcc") && speckleConcrete["Fcc"] is double?)           gsaConcrete.Fcc = speckleObject["Fcc"] as double?;
      if (members.ContainsKey("EpsPlasC") && speckleConcrete["EpsPlasC"] is double?) gsaConcrete.EpsPlasC = speckleObject["EpsPlasC"] as double?;
      if (members.ContainsKey("EpsUC") && speckleConcrete["EpsUC"] is double?)       gsaConcrete.EpsUC = speckleObject["EpsUC"] as double?;
      return new List<GsaRecord>() { gsaConcrete };
    }

    private List<GsaRecord> ConcreteToNative(Base speckleObject)
    {
      //Values based on GSA10.1 with design code AS3600-2018
      var speckleConcrete = (Concrete)speckleObject;
      var gsaConcrete = new GsaMatConcrete()
      {
        ApplicationId = speckleConcrete.applicationId,
        Index = speckleConcrete.ResolveIndex(),
        Name = speckleConcrete.name,
        Mat = new GsaMat()
        {
          E = speckleConcrete.elasticModulus,
          F = null,
          Nu = speckleConcrete.poissonsRatio,
          G = speckleConcrete.shearModulus,
          Rho = speckleConcrete.density,
          Alpha = speckleConcrete.thermalExpansivity,
          Prop = new GsaMatAnal()
          {
            Name = "",
            Colour = Colour.NO_RGB,
            E = speckleConcrete.elasticModulus,
            Nu = speckleConcrete.poissonsRatio,
            Rho = speckleConcrete.density,
            Alpha = speckleConcrete.thermalExpansivity,
            G = speckleConcrete.shearModulus,
            Damp = speckleConcrete.dampingRatio,
          },
          NumUC = 0,
          AbsUC = Dimension.NotSet,
          OrdUC = Dimension.NotSet,
          PtsUC = null,
          NumSC = 0,
          AbsSC = Dimension.NotSet,
          OrdSC = Dimension.NotSet,
          PtsSC = null,
          NumUT = 0,
          AbsUT = Dimension.NotSet,
          OrdUT = Dimension.NotSet,
          PtsUT = null,
          NumST = 0,
          AbsST = Dimension.NotSet,
          OrdST = Dimension.NotSet,
          PtsST = null,
          Eps = 0,
          Uls = new GsaMatCurveParam()
          {
            Model = new List<MatCurveParamType>() { MatCurveParamType.RECTANGLE, MatCurveParamType.NO_TENSION },
            StrainElasticCompression = GetEpsMax(speckleConcrete.maxCompressiveStrain),
            StrainElasticTension = 0,
            StrainPlasticCompression = GetEpsMax(speckleConcrete.maxCompressiveStrain),
            StrainPlasticTension = 0,
            StrainFailureCompression = 0.003,
            StrainFailureTension = 1,
            GammaF = 1,
            GammaE = 1,
          },
          Sls = new GsaMatCurveParam()
          {
            Model = new List<MatCurveParamType>() { MatCurveParamType.LINEAR, MatCurveParamType.INTERPOLATED },
            StrainElasticCompression = 0.003,
            StrainElasticTension = 0,
            StrainPlasticCompression = 0.003,
            StrainPlasticTension = 0,
            StrainFailureCompression = 0.003,
            StrainFailureTension = speckleConcrete.maxTensileStrain,
            GammaF = 1,
            GammaE = 1,
          },
          Cost = speckleConcrete.cost,
          Type = MatType.CONCRETE,
        },
        Type = MatConcreteType.CYLINDER, //strength type
        Cement = MatConcreteCement.N, //cement class
        Fc = speckleConcrete.compressiveStrength, //concrete strength
        Fcd = 0.85 * speckleConcrete.compressiveStrength, //design strength
        Fcdc = 0.4 * speckleConcrete.compressiveStrength, //cracked strength
        Fcdt = speckleConcrete.tensileStrength, //tensile strength
        Fcfib = 0.6 * speckleConcrete.tensileStrength, //peak strength for FIB/Popovics curves
        EmEs = 0, //ratio of initial elastic modulus to secant modulus
        N = 2, //parabolic coefficient (normally 2)
        Emod = 1, //modifier on elastic stiffness typically in range (0.8:1.2)
        EpsPeak = 0.003, //concrete strain at peak SLS stress
        EpsMax = GetEpsMax(speckleConcrete.maxCompressiveStrain), //maximum conrete SLS strain
        EpsU = speckleConcrete.maxCompressiveStrain, //concrete ULS failure strain
        EpsAx = 0.0025, //concrete max compressive ULS strain
        EpsTran = 0.002, //slab transition strain
        EpsAxs = 0.0025, //slab axial strain limit
        Light = speckleConcrete.lightweight, //lightweight flag
        Agg = speckleConcrete.maxAggregateSize, //maximum aggregate size
        XdMin = 0, //minimum x/d in flexure
        XdMax = 1, //maximum x/d in flexure
        Beta = GetBeta(speckleConcrete.compressiveStrength), //depth of rectangular stress block
        Shrink = null, //shrinkage strain
        Confine = null, //confining stress
        Fcc = null, //concrete strength [confined]
        EpsPlasC = null, //plastic strain (ULS) [confined]
        EpsUC = null, //concrete failure strain [confined]
      };
      //TODO:
      //SpeckleObject:
      //  string grade
      //  string designCode
      //  string codeYear
      //  double strength
      //  double materialSafetyFactor
      //  double flexuralStrength
      //  double maxTensileStrain
      return new List<GsaRecord>() { gsaConcrete };
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
    #region ToNative
    #region Geometry
    private int GetElementIndex(object obj)
    {
      if (obj is GSAElement1D element1D)
        return element1D.nativeId;
      else if (obj is GSAElement2D element2D)
        return element2D.nativeId;
      else
        return -1;
    }

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
    #region Materials
    private GsaMatSteel GsaMatSteelExample(Steel speckleSteel)
    {
      return new GsaMatSteel()
      {
        Index = speckleSteel.ResolveIndex(),
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
    }

    private GsaMat GetMat(Base speckleObject)
    {
      var members = speckleObject.GetMembers();
      var gsaMat = new GsaMat();
      if (members.ContainsKey("Name") && speckleObject["Name"] is string)    gsaMat.Name = speckleObject["Name"] as string;
      if (members.ContainsKey("E") && speckleObject["E"] is double?)         gsaMat.E = speckleObject["E"] as double?;
      if (members.ContainsKey("F") && speckleObject["F"] is double?)         gsaMat.F = speckleObject["F"] as double?;
      if (members.ContainsKey("Nu") && speckleObject["Nu"] is double?)       gsaMat.Nu = speckleObject["Nu"] as double?;
      if (members.ContainsKey("G") && speckleObject["G"] is double?)         gsaMat.G = speckleObject["G"] as double?;
      if (members.ContainsKey("Rho") && speckleObject["Rho"] is double?)     gsaMat.Rho = speckleObject["Rho"] as double?;
      if (members.ContainsKey("Alpha") && speckleObject["Alpha"] is double?) gsaMat.Alpha = speckleObject["Alpha"] as double?;
      if (members.ContainsKey("Prop") && speckleObject["Prop"] is Base)      gsaMat.Prop = GetMatAnal(speckleObject["Prop"] as Base);
      if (members.ContainsKey("Uls") && speckleObject["Uls"] is Base)        gsaMat.Uls = GetMatCurveParam(speckleObject["Uls"] as Base);
      if (members.ContainsKey("Sls") && speckleObject["Sls"] is Base)        gsaMat.Sls = GetMatCurveParam(speckleObject["Sls"] as Base);
      if (members.ContainsKey("Eps") && speckleObject["Eps"] is double?)     gsaMat.Eps = speckleObject["Eps"] as double?;
      if (members.ContainsKey("Cost") && speckleObject["Cost"] is double?)   gsaMat.Cost = speckleObject["Cost"] as double?;
      if (members.ContainsKey("Type") && speckleObject["Type"] is string)    gsaMat.Type = Enum.TryParse(speckleObject["Type"] as string, true, out MatType v) ? v : MatType.GENERIC;
      if (members.ContainsKey("PtsUC") && speckleObject["PtsUC"] is double[])
      {
        gsaMat.PtsUC = speckleObject["PtsUC"] as double[];
        gsaMat.NumUC = gsaMat.PtsUC.Length;
        if (members.ContainsKey("AbsUC") && speckleObject["AbsUC"] is string) gsaMat.AbsUC = Enum.TryParse(speckleObject["AbsUC"] as string, true, out Dimension v) ? v : Dimension.NotSet;
        if (members.ContainsKey("OrdUC") && speckleObject["OrdUC"] is string) gsaMat.OrdUC = Enum.TryParse(speckleObject["OrdUC"] as string, true, out Dimension v) ? v : Dimension.NotSet;
      }
      if (members.ContainsKey("PtsSC") && speckleObject["PtsSC"] is double[])
      {
        gsaMat.PtsSC = speckleObject["PtsSC"] as double[];
        gsaMat.NumSC = gsaMat.PtsSC.Length;
        if (members.ContainsKey("AbsSC") && speckleObject["AbsSC"] is string) gsaMat.AbsSC = Enum.TryParse(speckleObject["AbsSC"] as string, true, out Dimension v) ? v : Dimension.NotSet;
        if (members.ContainsKey("OrdSC") && speckleObject["OrdSC"] is string) gsaMat.OrdSC = Enum.TryParse(speckleObject["OrdSC"] as string, true, out Dimension v) ? v : Dimension.NotSet;
      }
      if (members.ContainsKey("PtsUT") && speckleObject["PtsUT"] is double[])
      {
        gsaMat.PtsUT = speckleObject["PtsUT"] as double[];
        gsaMat.NumUT = gsaMat.PtsUT.Length;
        if (members.ContainsKey("AbsUT") && speckleObject["AbsUT"] is string) gsaMat.AbsUT = Enum.TryParse(speckleObject["AbsUT"] as string, true, out Dimension v) ? v : Dimension.NotSet;
        if (members.ContainsKey("OrdUT") && speckleObject["OrdUT"] is string) gsaMat.OrdUT = Enum.TryParse(speckleObject["OrdUT"] as string, true, out Dimension v) ? v : Dimension.NotSet;
      }
      if (members.ContainsKey("PtsST") && speckleObject["PtsST"] is double[])
      {
        gsaMat.PtsST = speckleObject["PtsST"] as double[];
        gsaMat.NumST = gsaMat.PtsST.Length;
        if (members.ContainsKey("AbsST") && speckleObject["AbsST"] is string) gsaMat.AbsST = Enum.TryParse(speckleObject["AbsST"] as string, true, out Dimension v) ? v : Dimension.NotSet;
        if (members.ContainsKey("OrdST") && speckleObject["OrdST"] is string) gsaMat.OrdST = Enum.TryParse(speckleObject["OrdST"] as string, true, out Dimension v) ? v : Dimension.NotSet;
      }
      return gsaMat;
    }

    private GsaMatAnal GetMatAnal(Base speckleObject)
    {
      var members = speckleObject.GetMembers();
      var gsaMatAnal = new GsaMatAnal();
      if (members.ContainsKey("Name") && speckleObject["Name"] is string)          gsaMatAnal.Name = speckleObject["Name"] as string;
      if (members.ContainsKey("Colour") && speckleObject["Colour"] is string)      gsaMatAnal.Colour = Enum.TryParse(speckleObject["Colour"] as string, true, out Colour v) ? v : Colour.NotSet;
      if (members.ContainsKey("Type") && speckleObject["Type"] is string)          gsaMatAnal.Type = Enum.TryParse(speckleObject["Type"] as string, true, out MatAnalType v) ? v : MatAnalType.MAT_ELAS_ISO;
      if (members.ContainsKey("NumParams") && speckleObject["NumParams"] is int?)  gsaMatAnal.NumParams = speckleObject["NumParams"] as int?;
      if (members.ContainsKey("E") && speckleObject["E"] is double?)               gsaMatAnal.E = speckleObject["E"] as double?;
      if (members.ContainsKey("Nu") && speckleObject["Nu"] is double?)             gsaMatAnal.Nu = speckleObject["Nu"] as double?;
      if (members.ContainsKey("Rho") && speckleObject["Rho"] is double?)           gsaMatAnal.Rho = speckleObject["Rho"] as double?;
      if (members.ContainsKey("Alpha") && speckleObject["Alpha"] is double?)       gsaMatAnal.Alpha = speckleObject["Alpha"] as double?;
      if (members.ContainsKey("G") && speckleObject["G"] is double?)               gsaMatAnal.G = speckleObject["G"] as double?;
      if (members.ContainsKey("Damp") && speckleObject["Damp"] is double?)         gsaMatAnal.Damp = speckleObject["Damp"] as double?;
      if (members.ContainsKey("Yield") && speckleObject["Yield"] is double?)       gsaMatAnal.Yield = speckleObject["Yield"] as double?;
      if (members.ContainsKey("Ultimate") && speckleObject["Ultimate"] is double?) gsaMatAnal.Ultimate = speckleObject["Ultimate"] as double?;
      if (members.ContainsKey("Eh") && speckleObject["Eh"] is double?)             gsaMatAnal.Eh = speckleObject["Eh"] as double?;
      if (members.ContainsKey("Beta") && speckleObject["Beta"] is double?)         gsaMatAnal.Beta = speckleObject["Beta"] as double?;
      if (members.ContainsKey("Cohesion") && speckleObject["Cohesion"] is double?) gsaMatAnal.Cohesion = speckleObject["Cohesion"] as double?;
      if (members.ContainsKey("Phi") && speckleObject["Phi"] is double?)           gsaMatAnal.Phi = speckleObject["Phi"] as double?;
      if (members.ContainsKey("Psi") && speckleObject["Psi"] is double?)           gsaMatAnal.Psi = speckleObject["Psi"] as double?;
      if (members.ContainsKey("Scribe") && speckleObject["Scribe"] is double?)     gsaMatAnal.Scribe = speckleObject["Scribe"] as double?;
      if (members.ContainsKey("Ex") && speckleObject["Ex"] is double?)             gsaMatAnal.Ex = speckleObject["Ex"] as double?;
      if (members.ContainsKey("Ey") && speckleObject["Ey"] is double?)             gsaMatAnal.Ey = speckleObject["Ey"] as double?;
      if (members.ContainsKey("Ez") && speckleObject["Ez"] is double?)             gsaMatAnal.Ez = speckleObject["Ez"] as double?;
      if (members.ContainsKey("Nuxy") && speckleObject["Nuxy"] is double?)         gsaMatAnal.Nuxy = speckleObject["Nuxy"] as double?;
      if (members.ContainsKey("Nuyz") && speckleObject["Nuyz"] is double?)         gsaMatAnal.Nuyz = speckleObject["Nuyz"] as double?;
      if (members.ContainsKey("Nuzx") && speckleObject["Nuzx"] is double?)         gsaMatAnal.Nuzx = speckleObject["Nuzx"] as double?;
      if (members.ContainsKey("Alphax") && speckleObject["Alphax"] is double?)     gsaMatAnal.Alphax = speckleObject["Alphax"] as double?;
      if (members.ContainsKey("Alphay") && speckleObject["Alphay"] is double?)     gsaMatAnal.Alphay = speckleObject["Alphay"] as double?;
      if (members.ContainsKey("Alphaz") && speckleObject["Alphaz"] is double?)     gsaMatAnal.Alphaz = speckleObject["Alphaz"] as double?;
      if (members.ContainsKey("Gxy") && speckleObject["Gxy"] is double?)           gsaMatAnal.Gxy = speckleObject["Gxy"] as double?;
      if (members.ContainsKey("Gyz") && speckleObject["Gyz"] is double?)           gsaMatAnal.Gyz = speckleObject["Gyz"] as double?;
      if (members.ContainsKey("Gzx") && speckleObject["Gzx"] is double?)           gsaMatAnal.Gzx = speckleObject["Gzx"] as double?;
      if (members.ContainsKey("Comp") && speckleObject["Comp"] is double?)         gsaMatAnal.Comp = speckleObject["Comp"] as double?;
      return gsaMatAnal;
    }

    private GsaMatCurveParam GetMatCurveParam(Base speckleObject)
    {
      var members = speckleObject.GetMembers();
      var gsaMatCurveParam = new GsaMatCurveParam();
      if (members.ContainsKey("Name") && speckleObject["Name"] is string) gsaMatCurveParam.Name = speckleObject["Name"] as string;
      if (members.ContainsKey("Model") && speckleObject["Model"] is List<string>)
      {
        var model = speckleObject["Model"] as List<string>;
        gsaMatCurveParam.Model = model.Select(s => Enum.TryParse(s, true, out MatCurveParamType v) ? v : MatCurveParamType.UNDEF).ToList();
      }

      if (members.ContainsKey("StrainElasticCompression") && speckleObject["StrainElasticCompression"] is double?)
      {
        gsaMatCurveParam.StrainElasticCompression = speckleObject["StrainElasticCompression"] as double?;
      }
      if (members.ContainsKey("StrainElasticTension") && speckleObject["StrainElasticTension"] is double?)
      {
        gsaMatCurveParam.StrainElasticTension = speckleObject["StrainElasticTension"] as double?;
      }
      if (members.ContainsKey("StrainPlasticCompression") && speckleObject["StrainPlasticCompression"] is double?)
      {
        gsaMatCurveParam.StrainPlasticCompression = speckleObject["StrainPlasticCompression"] as double?;
      }
      if (members.ContainsKey("StrainPlasticTension") && speckleObject["StrainPlasticTension"] is double?)
      {
        gsaMatCurveParam.StrainPlasticTension = speckleObject["StrainPlasticTension"] as double?;
      }
      if (members.ContainsKey("StrainFailureCompression") && speckleObject["StrainFailureCompression"] is double?)
      {
        gsaMatCurveParam.StrainFailureCompression = speckleObject["StrainFailureCompression"] as double?;
      }
      if (members.ContainsKey("StrainFailureTension") && speckleObject["StrainFailureTension"] is double?)
      {
        gsaMatCurveParam.StrainFailureTension = speckleObject["StrainFailureTension"] as double?;
      }
      if (members.ContainsKey("GammaF") && speckleObject["GammaF"] is double?)
      {
        gsaMatCurveParam.GammaF = speckleObject["GammaF"] as double?;
      }
      if (members.ContainsKey("GammaE") && speckleObject["GammaE"] is double?)
      {
        gsaMatCurveParam.GammaE = speckleObject["GammaE"] as double?;
      }
      return gsaMatCurveParam;
    }

    private double GetBeta(double fc) => LinearInterp(20e6, 100e6, 0.92, 0.72, fc); //TODO: - units

    private double GetEpsMax(double fc) => LinearInterp(20e6, 100e6, 0.00024, 0.00084, fc); //TODO: - units

    private double GetSteelStrain(double fy) => LinearInterp(200e6, 450e6, 0.001, 0.00225, fy); //TODO - units
    #endregion
    #region Properties
    private GsaSection GsaSectionExample(GSAProperty1D speckleProperty)
    {
      return new GsaSection()
      {
        Index = speckleProperty.ResolveIndex(),
        Name = speckleProperty.name,
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
    }
    #endregion
    #region Other
    private double LinearInterp(double x1, double x2, double y1, double y2, double x) => (y2 - y1) / (x2 - x1) * (x - x1) + y1;
    #endregion
    #endregion
    #endregion
  }
}
