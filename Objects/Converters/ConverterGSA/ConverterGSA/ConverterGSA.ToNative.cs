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
        { typeof(GSANode), GSANodeToNative },
        { typeof(Node), NodeToNative },
        { typeof(GSAElement1D), GSAElement1dToNative },
        { typeof(Element1D), Element1dToNative },
        { typeof(GSAElement2D), GSAElement2dToNative },
        { typeof(Element2D), Element2dToNative },
        { typeof(GSAMember1D), GSAMember1dToNative },
        { typeof(GSAMember2D), GSAMember2dToNative },
        { typeof(GSAAssembly), GSAAssemblyToNative },
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
        { typeof(Property2D), Property2dToNative },
        { typeof(GSAProperty2D), GsaProperty2dToNative },
        { typeof(PropertySpring), PropertySpringToNative },
        { typeof(PropertyMass), PropertyMassToNative },
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
        Index = speckleAxis.GetIndex<GsaAxis>(),
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

    private List<GsaRecord> GSANodeToNative(Base speckleObject)
    {
      var gsaNode = (GsaNode)NodeToNative(speckleObject).First(o => o is GsaNode);
      var speckleNode = (GSANode)speckleObject;
      gsaNode.Colour = speckleNode.colour?.ColourToNative() ?? Colour.NotSet;
      if (speckleNode.localElementSize > 0) gsaNode.MeshSize = speckleNode.localElementSize;
      return new List<GsaRecord>() { gsaNode };
    }

    private List<GsaRecord> NodeToNative(Base speckleObject)
    {
      var speckleNode = (Node)speckleObject;
      var gsaNode = new GsaNode()
      {
        ApplicationId = speckleNode.applicationId,
        Index = speckleNode.GetIndex<GsaNode>(),
        Name = speckleNode.name,
        
        X = speckleNode.basePoint.x,
        Y = speckleNode.basePoint.y,
        Z = speckleNode.basePoint.z,
        SpringPropertyIndex = speckleNode.springProperty.GetIndex<GsaPropSpr>(),
        MassPropertyIndex = speckleNode.massProperty.GetIndex<GsaPropMass>(),
      };

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
      

      return new List<GsaRecord>() { gsaNode };
    }

    private List<GsaRecord> GSAElement1dToNative(Base speckleObject)
    {
      var gsaElement = (GsaEl)Element1dToNative(speckleObject).First(o => o is GsaEl);
      var speckleElement = (GSAElement1D)speckleObject;
      gsaElement.Colour = speckleElement.colour?.ColourToNative() ?? Colour.NotSet;
      gsaElement.Dummy = speckleElement.isDummy;
      if (speckleElement.group > 0) gsaElement.Group = speckleElement.group;
      return new List<GsaRecord>() { gsaElement };

      //TODO:
      //SpeckleObject:
      //  string action
    }

    private List<GsaRecord> Element1dToNative(Base speckleObject)
    {
      var speckleElement = (Element1D)speckleObject;
      var gsaElement = new GsaEl()
      {
        ApplicationId = speckleElement.applicationId,
        Index = speckleElement.GetIndex<GsaEl>(),
        Name = speckleElement.name,
        Type = speckleElement.type.ToNative(),
        //TaperOffsetPercentageEnd1 - currently not supported
        //TaperOffsetPercentageEnd2 - currently not supported
        NodeIndices = speckleElement.topology?.Select(n=>(Base)n).ToList().GetIndicies<GsaNode>() ?? new List<int>(),
        PropertyIndex = speckleElement.property.GetIndex<GsaSection>(),
        OrientationNodeIndex = speckleElement.orientationNode.GetIndex<GsaNode>(),
        ParentIndex = speckleElement.parent.GetIndex<GsaMemb>(),
      };

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
      
      return new List<GsaRecord>() { gsaElement };
    }

    private List<GsaRecord> GSAElement2dToNative(Base speckleObject)
    {
      var gsaElement = (GsaEl)Element2dToNative(speckleObject).First(o => o is GsaEl);
      var speckleElement = (GSAElement2D)speckleObject;
      gsaElement.Colour = speckleElement.colour?.ColourToNative() ?? Colour.NotSet;
      gsaElement.Dummy = speckleElement.isDummy;
      if (speckleElement.group > 0) gsaElement.Group = speckleElement.group;
      return new List<GsaRecord>() { gsaElement };
    }

    private List<GsaRecord> Element2dToNative(Base speckleObject)
    {
      var speckleElement = (Element2D)speckleObject;
      var gsaElement = new GsaEl()
      {
        ApplicationId = speckleElement.applicationId,
        Index = speckleElement.GetIndex<GsaEl>(),
        Name = speckleElement.name,
        Type = speckleElement.type.ToNative(),
        NodeIndices = speckleElement.topology.Select(n => (Base)n).ToList().GetIndicies<GsaNode>(),
        PropertyIndex = speckleElement.property.GetIndex<GsaProp2d>(),
        ReleaseInclusion = ReleaseInclusion.NotIncluded,
        ParentIndex = speckleElement.parent.GetIndex<GsaMemb>(),
      };

      if (speckleElement.orientationAngle != 0) gsaElement.Angle = speckleElement.orientationAngle;
      if (speckleElement.offset != 0) gsaElement.OffsetZ = speckleElement.offset;

      return new List<GsaRecord>() { gsaElement };
    }

    private List<GsaRecord> GSAMember1dToNative(Base speckleObject)
    {
      var speckleMember = (GSAMember1D)speckleObject;
      var gsaMember = new GsaMemb()
      {
        ApplicationId = speckleMember.applicationId,
        Index = speckleMember.GetIndex<GsaMemb>(),
        Name = speckleMember.name,
        Type = speckleMember.type.ToNativeMember(),
        Colour = speckleMember.colour?.ColourToNative() ?? Colour.NotSet,
        //TaperOffsetPercentageEnd1 - currently not supported
        //TaperOffsetPercentageEnd2 - currently not supported
        NodeIndices = speckleMember.topology.Select(n => (Base)n).ToList().GetIndicies<GsaNode>(),
        PropertyIndex = speckleMember.property.GetIndex<GsaSection>(),
        OrientationNodeIndex = speckleMember.orientationNode.GetIndex<GsaNode>(),
        Dummy = speckleMember.isDummy,
        IsIntersector = speckleMember.intersectsWithOthers,

        //Dynamic properties
        Exposure = speckleMember.GetDynamicEnum<ExposedSurfaces>("Exposure"),
        AnalysisType = speckleMember.GetDynamicEnum<AnalysisType>("AnalysisType"),
        Fire = speckleMember.GetDynamicEnum<FireResistance>("Fire"),
        RestraintEnd1 = speckleMember.GetDynamicEnum<Speckle.GSA.API.GwaSchema.Restraint>("RestraintEnd1"),
        RestraintEnd2 = speckleMember.GetDynamicEnum<Speckle.GSA.API.GwaSchema.Restraint>("RestraintEnd2"),
        EffectiveLengthType = speckleMember.GetDynamicEnum<EffectiveLengthType>("EffectiveLengthType"),
        LoadHeightReferencePoint = speckleMember.GetDynamicEnum<LoadHeightReferencePoint>("LoadHeightReferencePoint"),
        CreationFromStartDays = speckleMember.GetDynamicValue<int>("CreationFromStartDays"),
        StartOfDryingDays = speckleMember.GetDynamicValue<int>("StartOfDryingDays"),
        AgeAtLoadingDays = speckleMember.GetDynamicValue<int>("AgeAtLoadingDays"),
        RemovedAtDays = speckleMember.GetDynamicValue<int>("RemovedAtDays"),
        MemberHasOffsets = speckleMember.GetDynamicValue<bool>("MemberHasOffsets"),
        End1AutomaticOffset = speckleMember.GetDynamicValue<bool>("End1AutomaticOffset"),
        End2AutomaticOffset = speckleMember.GetDynamicValue<bool>("End2AutomaticOffset"),
        LimitingTemperature = speckleMember.GetDynamicValue<double?>("LimitingTemperature"),
        LoadHeight = speckleMember.GetDynamicValue<double?>("LoadHeight"),
        EffectiveLengthYY = speckleMember.GetDynamicValue<double?>("EffectiveLengthYY"),
        PercentageYY = speckleMember.GetDynamicValue<double?>("PercentageYY"),
        EffectiveLengthZZ = speckleMember.GetDynamicValue<double?>("EffectiveLengthZZ"),
        PercentageZZ = speckleMember.GetDynamicValue<double?>("PercentageZZ"),
        EffectiveLengthLateralTorsional = speckleMember.GetDynamicValue<double?>("EffectiveLengthLateralTorsional"),
        FractionLateralTorsional = speckleMember.GetDynamicValue<double?>("FractionLateralTorsional"),
      };

      if (GetReleases(speckleMember.end1Releases, out var gsaRelease1, out var gsaStiffnesses1))
      {
        gsaMember.Releases1 = gsaRelease1;
        gsaMember.Stiffnesses1 = gsaStiffnesses1;
      }
      if (GetReleases(speckleMember.end2Releases, out var gsaRelease2, out var gsaStiffnesses2))
      {
        gsaMember.Releases2 = gsaRelease2;
        gsaMember.Stiffnesses2 = gsaStiffnesses2;
      }
      if (speckleMember.end1Offset.x != 0) gsaMember.End1OffsetX = speckleMember.end1Offset.x;
      if (speckleMember.end2Offset.x != 0) gsaMember.End2OffsetX = speckleMember.end2Offset.x;
      if (speckleMember.end1Offset.y == speckleMember.end2Offset.y)
      {
        if (speckleMember.end1Offset.y != 0) gsaMember.OffsetY = speckleMember.end1Offset.y;
      }
      else
      {
        gsaMember.OffsetY = speckleMember.end1Offset.y;
        ConversionErrors.Add(new Exception("GSAMember1dToNative: "
          + "Error converting element1d with application id (" + speckleMember.applicationId + "). "
          + "Different y offsets were assigned at either end."
          + "end 1 y offset of " + gsaMember.OffsetY.ToString() + " has been applied"));
      }
      if (speckleMember.end1Offset.z == speckleMember.end2Offset.z)
      {
        if (speckleMember.end1Offset.z != 0) gsaMember.OffsetZ = speckleMember.end1Offset.z;
      }
      else
      {
        gsaMember.OffsetZ = speckleMember.end1Offset.z;
        ConversionErrors.Add(new Exception("GSAMember1dToNative: "
          + "Error converting element1d with application id (" + speckleMember.applicationId + "). "
          + "Different z offsets were assigned at either end."
          + "end 1 z offset of " + gsaMember.OffsetY.ToString() + " has been applied"));
      }
      if (speckleMember.end1Offset.x != 0) gsaMember.End1OffsetX = speckleMember.end1Offset.x;
      if (speckleMember.orientationAngle != 0) gsaMember.Angle = speckleMember.orientationAngle;
      if (speckleMember.group > 0) gsaMember.Group = speckleMember.group;
      if (speckleMember.targetMeshSize > 0) gsaMember.MeshSize = speckleMember.targetMeshSize;

      //Dynamic properties
      var members = speckleMember.GetMembers();
      if (members.ContainsKey("Voids") && speckleMember["Voids"] is List<List<Node>>)
      {
        var speckleVoids = speckleObject["Voids"] as List<List<Base>>;
        gsaMember.Voids = speckleVoids.Select(v => v.GetIndicies<GsaNode>()).ToList();
      }
      if (members.ContainsKey("Points") && speckleMember["Points"] is List<Node>)
      {
        var specklePoints = speckleObject["Points"] as List<Base>;
        gsaMember.PointNodeIndices = specklePoints.GetIndicies<GsaNode>();
      }
      if (members.ContainsKey("Lines") && speckleMember["Lines"] is List<List<Node>>)
      {
        var speckleLines = speckleObject["Lines"] as List<List<Base>>;
        gsaMember.Polylines = speckleLines.Select(v => v.GetIndicies<GsaNode>()).ToList();
      }
      if (members.ContainsKey("Areas") && speckleMember["Areas"] is List<List<Node>>)
      {
        var speckleAreas = speckleObject["Areas"] as List<List<Base>>;
        gsaMember.AdditionalAreas = speckleAreas.Select(v => v.GetIndicies<GsaNode>()).ToList();
      }
      if (members.ContainsKey("SpanRestraints") && speckleMember["SpanRestraints"] is List<RestraintDefinition>)
      {
        var speckleSpanRestraints = speckleObject["SpanRestraints"] as List<RestraintDefinition>;
        gsaMember.SpanRestraints = speckleSpanRestraints.Select(s => new RestraintDefinition() { All = s.All, Index = s.Index, Restraint = s.Restraint }).ToList();        
      }
      if (members.ContainsKey("PointRestraints") && speckleMember["PointRestraints"] is List<RestraintDefinition>)
      {
        var specklePointRestraints = speckleObject["PointRestraints"] as List<RestraintDefinition>;
        gsaMember.PointRestraints = specklePointRestraints.Select(s => new RestraintDefinition() { All = s.All, Index = s.Index, Restraint = s.Restraint }).ToList();
      }

      return new List<GsaRecord>() { gsaMember };
    }

    private List<GsaRecord> GSAMember2dToNative(Base speckleObject)
    {
      var speckleMember = (GSAMember2D)speckleObject;
      var gsaMember = new GsaMemb()
      {
        ApplicationId = speckleMember.applicationId,
        Index = speckleMember.GetIndex<GsaMemb>(),
        Name = speckleMember.name,
        Type = speckleMember.type.ToNativeMember(),
        NodeIndices = speckleMember.topology.Select(n => (Base)n).ToList().GetIndicies<GsaNode>(),
        Colour = speckleMember.colour?.ColourToNative() ?? Colour.NotSet,
        Dummy = speckleMember.isDummy,
        IsIntersector = speckleMember.intersectsWithOthers,
        PropertyIndex = speckleMember.property.GetIndex<GsaProp2d>(),

        //Dynamic properties
        Exposure = speckleMember.GetDynamicEnum<ExposedSurfaces>("Exposure"),
        AnalysisType = speckleMember.GetDynamicEnum<AnalysisType>("AnalysisType"),
        Fire = speckleMember.GetDynamicEnum<FireResistance>("Fire"),
        CreationFromStartDays = speckleMember.GetDynamicValue<int>("CreationFromStartDays"),
        StartOfDryingDays = speckleMember.GetDynamicValue<int>("StartOfDryingDays"),
        AgeAtLoadingDays = speckleMember.GetDynamicValue<int>("AgeAtLoadingDays"),
        RemovedAtDays = speckleMember.GetDynamicValue<int>("RemovedAtDays"),
        OffsetAutomaticInternal = speckleMember.GetDynamicValue<bool>("OffsetAutomaticInternal"),
        LimitingTemperature = speckleMember.GetDynamicValue<double?>("LimitingTemperature"),
      };
      
      if (speckleMember.orientationAngle != 0) gsaMember.Angle = speckleMember.orientationAngle;
      if (speckleMember.offset != 0) gsaMember.Offset2dZ = speckleMember.offset;
      if (speckleMember.group > 0) gsaMember.Group = speckleMember.group;
      if (speckleMember.targetMeshSize > 0) gsaMember.MeshSize = speckleMember.targetMeshSize;

      //Dynamic properties
      var members = speckleMember.GetMembers();
      if (members.ContainsKey("Voids") && speckleMember["Voids"] is List<List<Node>>)
      {
        var speckleVoids = speckleObject["Voids"] as List<List<Base>>;
        gsaMember.Voids = speckleVoids.Select(v => v.GetIndicies<GsaNode>()).ToList();
      }
      if (members.ContainsKey("Points") && speckleMember["Points"] is List<Node>)
      {
        var specklePoints = speckleObject["Points"] as List<Base>;
        gsaMember.PointNodeIndices = specklePoints.GetIndicies<GsaNode>();
      }
      if (members.ContainsKey("Lines") && speckleMember["Lines"] is List<List<Node>>)
      {
        var speckleLines = speckleObject["Lines"] as List<List<Base>>;
        gsaMember.Polylines = speckleLines.Select(v => v.GetIndicies<GsaNode>()).ToList();
      }
      if (members.ContainsKey("Areas") && speckleMember["Areas"] is List<List<Node>>)
      {
        var speckleAreas = speckleObject["Areas"] as List<List<Base>>;
        gsaMember.AdditionalAreas = speckleAreas.Select(v => v.GetIndicies<GsaNode>()).ToList();
      }

      return new List<GsaRecord>() { gsaMember };
    }

    private List<GsaRecord> GSAAssemblyToNative(Base speckleObject)
    {
      var speckleAssembly = (GSAAssembly)speckleObject;
      var gsaAssembly = new GsaAssembly()
      {
        ApplicationId = speckleAssembly.applicationId,
        Index = speckleAssembly.GetIndex<GsaAssembly>(),
        Name = speckleAssembly.name,
        SizeY = speckleAssembly.sizeY,
        SizeZ = speckleAssembly.sizeZ,
        CurveType = Enum.TryParse(speckleAssembly.curveType, true, out CurveType ct) ? ct : CurveType.NotSet,
        PointDefn = Enum.TryParse(speckleAssembly.pointDefinition, true, out PointDefinition pd) ? pd : PointDefinition.NotSet,   
        Topo1 = speckleAssembly.end1Node.GetIndex<GsaNode>(),
        Topo2 = speckleAssembly.end2Node.GetIndex<GsaNode>(),
        OrientNode = speckleAssembly.orientationNode.GetIndex<GsaNode>(),
        StoreyIndices = new List<int>(),
        ExplicitPositions = new List<double>(),
      };

      if (speckleAssembly.curveOrder > 0) gsaAssembly.CurveOrder = speckleAssembly.curveOrder;
      if (speckleAssembly.points != null)
      {
        switch (gsaAssembly.PointDefn)
        {
          case PointDefinition.Points: 
            gsaAssembly.NumberOfPoints = (int)speckleAssembly.points[0];
            break;
          case PointDefinition.Spacing:
            gsaAssembly.Spacing = speckleAssembly.points[0];
            break;
          case PointDefinition.Storey:
            gsaAssembly.StoreyIndices = speckleAssembly.points.Select(i => (int)i).ToList();
            break;
          case PointDefinition.Explicit:
            gsaAssembly.ExplicitPositions = speckleAssembly.points;
            break;
        }
      }
      if (speckleAssembly.entities != null)
      {
        gsaAssembly.IntTopo = speckleAssembly.entities.FindAll(e => e is Node)?.GetIndicies<GsaNode>() ?? new List<int>();
        gsaAssembly.ElementIndices = new List<int>();
        gsaAssembly.ElementIndices.AddRange(speckleAssembly.entities.FindAll(e => e is Element1D).GetIndicies<GsaEl>() ?? new List<int>());
        gsaAssembly.ElementIndices.AddRange(speckleAssembly.entities.FindAll(e => e is Element2D).GetIndicies<GsaEl>() ?? new List<int>());
        gsaAssembly.MemberIndices = new List<int>();
        gsaAssembly.MemberIndices.AddRange(speckleAssembly.entities.FindAll(e => e is GSAMember1D).GetIndicies<GsaMemb>() ?? new List<int>());
        gsaAssembly.MemberIndices.AddRange(speckleAssembly.entities.FindAll(e => e is GSAMember2D).GetIndicies<GsaMemb>() ?? new List<int>());
        if (gsaAssembly.ElementIndices.Count() > 0) gsaAssembly.Type = GSAEntity.ELEMENT;
        else if (gsaAssembly.MemberIndices.Count() > 0) gsaAssembly.Type = GSAEntity.MEMBER;
      }

      return new List<GsaRecord>() { gsaAssembly };
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
        Index = speckleLoadCase.GetIndex<GsaLoadCase>(),
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
        Index = speckleCase.GetIndex<GsaAnal>(),
        Name = speckleCase.name,
        //TaskIndex = speckleCase.task.GetIndex<GsaTask>(), //TODO:
        Desc = GetAnalysisCaseDescription(speckleCase.loadCases, speckleCase.loadFactors),
      };
      return new List<GsaRecord>() { gsaCase };
    }

    private List<GsaRecord> GSALoadCombinationToNative(Base speckleObject)
    {
      var gsaLoadCombination = (GsaCombination)LoadCombinationToNative(speckleObject).First(o => o is GsaCombination);
      var speckleLoadCombination = (GSALoadCombination)speckleObject;
      gsaLoadCombination.Bridge = speckleLoadCombination.GetDynamicValue<bool?>("bridge");
      gsaLoadCombination.Note = speckleLoadCombination.GetDynamicValue<string>("note");
      return new List<GsaRecord>() { gsaLoadCombination };
    }

    private List<GsaRecord> LoadCombinationToNative(Base speckleObject)
    {
      var speckleLoadCombination = (LoadCombination)speckleObject;
      var gsaLoadCombination = new GsaCombination()
      {
        ApplicationId = speckleLoadCombination.applicationId,
        Index = speckleLoadCombination.GetIndex<GsaCombination>(),
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
      gsaLoad.Index = speckleLoad.GetIndex<T>();
      gsaLoad.Name = speckleLoad.name;
      gsaLoad.LoadCaseIndex = speckleLoad.loadCase.GetIndex<GsaLoadCase>();
      gsaLoad.Projected = speckleLoad.isProjected;
      gsaLoad.LoadDirection = speckleLoad.direction.ToNative();
      gsaLoad.ElementIndices = speckleLoad.elements.GetIndicies<GsaEl>();
      gsaLoad.MemberIndices = speckleLoad.elements.GetIndicies<GsaMemb>();
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
        Index = speckleLoad.GetIndex<GsaLoad2dFace>(),
        Name = speckleLoad.name,
        Type = speckleLoad.loadType.ToNative(),
        LoadCaseIndex = speckleLoad.loadCase.GetIndex<GsaLoadCase>(),
        Values = speckleLoad.values,
        LoadDirection = speckleLoad.direction.ToNative(),
        Projected = speckleLoad.isProjected,
        ElementIndices = speckleLoad.elements.GetIndicies<GsaEl>(),
        MemberIndices = speckleLoad.elements.GetIndicies<GsaMemb>(),
      };
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
        Index = speckleLoad.GetIndex<GsaLoadNode>(),
        Name = speckleLoad.name,
        LoadDirection = speckleLoad.direction.ToNative(),
        LoadCaseIndex = speckleLoad.loadCase.GetIndex<GsaLoadCase>(),
        NodeIndices = speckleLoad.nodes.Select(o => (Base)o).ToList().GetIndicies<GsaNode>()
      };
      if (speckleLoad.value != 0) gsaLoad.Value = speckleLoad.value;
      if (speckleLoad.loadAxis.definition.IsGlobal())
      {
        gsaLoad.GlobalAxis = true;
      }
      else
      {
        gsaLoad.GlobalAxis = false;
        gsaLoad.AxisIndex = speckleLoad.loadAxis.GetIndex<GsaAxis>();
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
        Index = speckleLoad.GetIndex<GsaLoadGravity>(),
        Name = speckleLoad.name,
        LoadCaseIndex = speckleLoad.loadCase.GetIndex<GsaLoadCase>(),
        Nodes = speckleLoad.nodes.GetIndicies<GsaNode>(),
        ElementIndices = speckleLoad.elements.GetIndicies<GsaEl>(),
        MemberIndices = speckleLoad.elements.GetIndicies<GsaMemb>(),
      };

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
        Index = speckleLoad.GetIndex<GsaLoad2dThermal>(),
        Name = speckleLoad.name,
        LoadCaseIndex = speckleLoad.loadCase.GetIndex<GsaLoadCase>(),
        Type = speckleLoad.type.ToNative(),
        Values = speckleLoad.values,
      };
      if (speckleLoad.elements != null)
      {
        var speckleElements = speckleLoad.elements.Select(o => (Base)o).ToList();
        gsaLoad.ElementIndices = speckleElements.GetIndicies<GsaEl>();
        gsaLoad.MemberIndices = speckleElements.GetIndicies<GsaMemb>();
      }
      return new List<GsaRecord>() { gsaLoad };
    }

    #endregion

    #region Materials
    private List<GsaRecord> GSASteelToNative(Base speckleObject)
    {
      var gsaSteel = (GsaMatSteel)SteelToNative(speckleObject).First(o => o is GsaMatSteel);
      var speckleSteel = (GSASteel)speckleObject;
      gsaSteel.Mat = GetMat(speckleSteel.GetDynamicValue<Base>("Mat"));
      return new List<GsaRecord>() { gsaSteel };
    }

    private List<GsaRecord> SteelToNative(Base speckleObject)
    {
      //Values based on GSA10.1 with design code AS4100-1998, material grade 200-450 from AS3678
      var speckleSteel = (Steel)speckleObject;
      var gsaSteel = new GsaMatSteel()
      {
        ApplicationId = speckleSteel.applicationId,
        Index = speckleSteel.GetIndex<GsaMatSteel>(),
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
            Type = MatAnalType.MAT_ELAS_ISO,
            NumParams = 6,
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
      gsaConcrete.Mat = GetMat(speckleConcrete.GetDynamicValue<Base>("Mat"));
      gsaConcrete.Type = speckleConcrete.GetDynamicEnum<MatConcreteType>("Type");
      gsaConcrete.Cement = speckleConcrete.GetDynamicEnum<MatConcreteCement>("Cement");
      gsaConcrete.Fcd = speckleConcrete.GetDynamicValue<double?>("Fcd");
      gsaConcrete.Fcdc = speckleConcrete.GetDynamicValue<double?>("Fcdc");
      gsaConcrete.Fcfib = speckleConcrete.GetDynamicValue<double?>("Fcfib");
      gsaConcrete.EmEs = speckleConcrete.GetDynamicValue<double?>("EmEs");
      gsaConcrete.N = speckleConcrete.GetDynamicValue<double?>("N");
      gsaConcrete.Emod = speckleConcrete.GetDynamicValue<double?>("Emod");
      gsaConcrete.EpsPeak = speckleConcrete.GetDynamicValue<double?>("EpsPeak");
      gsaConcrete.EpsMax = speckleConcrete.GetDynamicValue<double?>("EpsMax");
      gsaConcrete.EpsAx = speckleConcrete.GetDynamicValue<double?>("EpsAx");
      gsaConcrete.EpsTran = speckleConcrete.GetDynamicValue<double?>("EpsTran");
      gsaConcrete.EpsAxs = speckleConcrete.GetDynamicValue<double?>("EpsAxs");
      gsaConcrete.XdMin = speckleConcrete.GetDynamicValue<double?>("XdMin");
      gsaConcrete.XdMax = speckleConcrete.GetDynamicValue<double?>("XdMax");
      gsaConcrete.Beta = speckleConcrete.GetDynamicValue<double?>("Beta");
      gsaConcrete.Shrink = speckleConcrete.GetDynamicValue<double?>("Shrink");
      gsaConcrete.Confine = speckleConcrete.GetDynamicValue<double?>("Confine");
      gsaConcrete.Fcc = speckleConcrete.GetDynamicValue<double?>("Fcc");
      gsaConcrete.EpsPlasC = speckleConcrete.GetDynamicValue<double?>("EpsPlasC");
      gsaConcrete.EpsUC = speckleConcrete.GetDynamicValue<double?>("EpsUC");

      return new List<GsaRecord>() { gsaConcrete };
    }

    private List<GsaRecord> ConcreteToNative(Base speckleObject)
    {
      //Values based on GSA10.1 with design code AS3600-2018
      var speckleConcrete = (Concrete)speckleObject;
      var gsaConcrete = new GsaMatConcrete()
      {
        ApplicationId = speckleConcrete.applicationId,
        Index = speckleConcrete.GetIndex<GsaMatConcrete>(),
        Name = speckleConcrete.name,
        Mat = new GsaMat()
        {
          E = speckleConcrete.elasticModulus,
          F = speckleConcrete.compressiveStrength,
          Nu = speckleConcrete.poissonsRatio,
          G = speckleConcrete.shearModulus,
          Rho = speckleConcrete.density,
          Alpha = speckleConcrete.thermalExpansivity,
          Prop = new GsaMatAnal()
          {
            Type = MatAnalType.MAT_ELAS_ISO,
            NumParams = 6,
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
            StrainElasticCompression = GetEpsMax(speckleConcrete.compressiveStrength),
            StrainElasticTension = 0,
            StrainPlasticCompression = GetEpsMax(speckleConcrete.compressiveStrength),
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
        EmEs = null, //ratio of initial elastic modulus to secant modulus
        N = 2, //parabolic coefficient (normally 2)
        Emod = 1, //modifier on elastic stiffness typically in range (0.8:1.2)
        EpsPeak = 0.003, //concrete strain at peak SLS stress
        EpsMax = GetEpsMax(speckleConcrete.compressiveStrength), //maximum conrete SLS strain
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
        Index = speckleProperty.GetIndex<GsaSection>(),
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


    private List<GsaRecord> GsaProperty2dToNative(Base speckleObject)
    {
      var speckleProperty = (GSAProperty2D)speckleObject;
      var natives = Property2dToNative(speckleObject);
      var gsaProp2d = (GsaProp2d)natives.FirstOrDefault(n => n is GsaProp2d);
      if (gsaProp2d != null)
      {
        gsaProp2d.Colour = (Enum.TryParse(speckleProperty.colour, true, out Colour gsaColour) ? gsaColour : Colour.NO_RGB);
        gsaProp2d.Mass = speckleProperty.additionalMass;
        gsaProp2d.Profile = speckleProperty.concreteSlabProp;
        if (speckleProperty.designMaterial != null)
        {
          int? materialIndex = null;
          if (speckleProperty.designMaterial.type == MaterialType.Steel && speckleProperty.designMaterial is GSASteel)
          {
            //var mat = (GSASteel)speckleProperty.designMaterial;
            materialIndex = Instance.GsaModel.Cache.LookupIndex<GsaMatSteel>(speckleProperty.designMaterial.applicationId);
            gsaProp2d.MatType = Property2dMaterialType.Steel;
          }
          else if (speckleProperty.material.type == MaterialType.Concrete && speckleProperty.designMaterial is GSAConcrete)
          {
            materialIndex = Instance.GsaModel.Cache.LookupIndex<GsaMatConcrete>(speckleProperty.designMaterial.applicationId);
            gsaProp2d.MatType = Property2dMaterialType.Concrete;
          }
          else
          {
            //Not supported yet

            gsaProp2d.MatType = Property2dMaterialType.Generic;
          }

          if (materialIndex.HasValue)
          {
            gsaProp2d.GradeIndex = materialIndex;
          }
          else
          {
            //TO DO: ToNative() of the material
          }
        }
      }
      return natives;
    }

    //Note: there should be no ToNative for SectionProfile because it's not a type that will create a first-class citizen in the GSA model
    //      so there is basically a ToNative of that class here in this method too
    private List<GsaRecord> Property2dToNative(Base speckleObject)
    {
      var speckleProperty = (Property2D)speckleObject;

      var gsaProp2d = new GsaProp2d()
      {
        Index = speckleProperty.GetIndex<GsaProp2d>(),
        Name = speckleProperty.name,
        ApplicationId = speckleProperty.applicationId,
        Thickness = (speckleProperty.thickness == 0) ? null : (double?)speckleProperty.thickness,
        RefZ = speckleProperty.zOffset,
        RefPt = speckleProperty.refSurface.ToNative(),
        Type = speckleProperty.type.ToNative(),
        InPlaneStiffnessPercentage = speckleProperty.modifierInPlane == 0 ? null : (double?)speckleProperty.modifierInPlane,
        BendingStiffnessPercentage = speckleProperty.modifierBending == 0 ? null : (double?)speckleProperty.modifierBending,
        ShearStiffnessPercentage = speckleProperty.modifierShear == 0 ? null : (double?)speckleProperty.modifierShear,
        VolumePercentage = speckleProperty.modifierVolume == 0 ? null : (double?)speckleProperty.modifierVolume
      };

      var axisIndex = Instance.GsaModel.Cache.LookupIndex<GsaAxis>(speckleProperty.orientationAxis.applicationId);
      if (axisIndex.HasValue)
      {
        gsaProp2d.AxisIndex = axisIndex;
        gsaProp2d.AxisRefType = AxisRefType.Reference;
      }
      else
      {
        gsaProp2d.AxisRefType = AxisRefType.Global;
      }

      return new List<GsaRecord>() { gsaProp2d };
    }

    private List<GsaRecord> PropertyMassToNative(Base speckleObject)
    {
      var specklePropertyMass = (PropertyMass)speckleObject;
      var gsaPropMass = new GsaPropMass()
      {
        Index = Instance.GsaModel.Cache.ResolveIndex<GsaPropMass>(specklePropertyMass.applicationId),
        Name = specklePropertyMass.name,
        ApplicationId = specklePropertyMass.applicationId,
        Mass = specklePropertyMass.mass,
        Ixx = specklePropertyMass.inertiaXX,
        Iyy = specklePropertyMass.inertiaYY,
        Izz = specklePropertyMass.inertiaZZ,
        Ixy = specklePropertyMass.inertiaXY,
        Iyz = specklePropertyMass.inertiaYZ,
        Izx = specklePropertyMass.inertiaZX
      };
      gsaPropMass.Mod = (specklePropertyMass.massModified) ? MassModification.Modified : MassModification.Defined;
      gsaPropMass.ModXPercentage = specklePropertyMass.massModifierX;
      gsaPropMass.ModYPercentage = specklePropertyMass.massModifierY;
      gsaPropMass.ModZPercentage = specklePropertyMass.massModifierZ;

      return new List<GsaRecord>() { gsaPropMass };
    }

    private List<GsaRecord> PropertySpringToNative(Base speckleObject)
    {
      var fns = new Dictionary<PropertyTypeSpring, Func<PropertySpring, GsaPropSpr, bool>>
      { { PropertyTypeSpring.Axial, SetPropertySpringAxial },
        { PropertyTypeSpring.Torsional, SetPropertySpringTorsional },
        { PropertyTypeSpring.CompressionOnly, SetPropertySpringCompression },
        { PropertyTypeSpring.TensionOnly, SetPropertySpringTension },
        { PropertyTypeSpring.LockUp, SetPropertySpringLockup },
        { PropertyTypeSpring.Gap, SetPropertySpringGap },
        { PropertyTypeSpring.Friction, SetPropertySpringFriction },
        { PropertyTypeSpring.General, SetPropertySpringGeneral }
        //CONNECT not yet supported
        //MATRIX not yet supported
      };

      var specklePropertySpring = (PropertySpring)speckleObject;
      var gsaPropSpr = new GsaPropSpr()
      {
        Index = Instance.GsaModel.Cache.ResolveIndex<GsaPropSpr>(specklePropertySpring.applicationId),
        Name = specklePropertySpring.name,
        ApplicationId = specklePropertySpring.applicationId,
        DampingRatio = specklePropertySpring.dampingRatio
      };
      if (fns.ContainsKey(specklePropertySpring.springType))
      {
        gsaPropSpr.Stiffnesses = new Dictionary<GwaAxisDirection6, double>();
        fns[specklePropertySpring.springType](specklePropertySpring, gsaPropSpr);
      }
      else
      {
        ConversionErrors.Add(new Exception("PropertySpring: spring type (" + specklePropertySpring.springType.ToString() + ") is not currently supported"));
      }

      return new List<GsaRecord>() { gsaPropSpr };
    }

    private bool SetPropertySpringAxial(PropertySpring specklePropertySpring, GsaPropSpr gsaPropSpr)
    {
      gsaPropSpr.PropertyType = StructuralSpringPropertyType.Axial;
      gsaPropSpr.Stiffnesses.Add(GwaAxisDirection6.X, specklePropertySpring.stiffnessX);
      return true;
    }

    private bool SetPropertySpringTorsional(PropertySpring specklePropertySpring, GsaPropSpr gsaPropSpr)
    {
      gsaPropSpr.PropertyType = StructuralSpringPropertyType.Torsional;
      gsaPropSpr.Stiffnesses.Add(GwaAxisDirection6.XX, specklePropertySpring.stiffnessXX);
      return true;
    }

    private bool SetPropertySpringCompression(PropertySpring specklePropertySpring, GsaPropSpr gsaPropSpr)
    {
      gsaPropSpr.PropertyType = StructuralSpringPropertyType.Compression;
      gsaPropSpr.Stiffnesses.Add(GwaAxisDirection6.X, specklePropertySpring.stiffnessX);
      return true;
    }

    private bool SetPropertySpringTension(PropertySpring specklePropertySpring, GsaPropSpr gsaPropSpr)
    {
      gsaPropSpr.PropertyType = StructuralSpringPropertyType.Tension;
      gsaPropSpr.Stiffnesses.Add(GwaAxisDirection6.X, specklePropertySpring.stiffnessX);
      return true;
    }

    private bool SetPropertySpringLockup(PropertySpring specklePropertySpring, GsaPropSpr gsaPropSpr)
    {
      //Also for LOCKUP, there are positive and negative parameters, but these aren't supported yet
      gsaPropSpr.PropertyType = StructuralSpringPropertyType.Lockup;
      gsaPropSpr.Stiffnesses.Add(GwaAxisDirection6.X, specklePropertySpring.stiffnessX);
      return true;
    }

    private bool SetPropertySpringGap(PropertySpring specklePropertySpring, GsaPropSpr gsaPropSpr)
    {
      gsaPropSpr.PropertyType = StructuralSpringPropertyType.Gap;
      gsaPropSpr.Stiffnesses.Add(GwaAxisDirection6.X, specklePropertySpring.stiffnessX);
      return true;
    }

    private bool SetPropertySpringFriction(PropertySpring specklePropertySpring, GsaPropSpr gsaPropSpr)
    {
      gsaPropSpr.PropertyType = StructuralSpringPropertyType.Friction;
      gsaPropSpr.Stiffnesses.Add(GwaAxisDirection6.X, specklePropertySpring.stiffnessX);
      gsaPropSpr.Stiffnesses.Add(GwaAxisDirection6.Y, specklePropertySpring.stiffnessY);
      gsaPropSpr.Stiffnesses.Add(GwaAxisDirection6.Z, specklePropertySpring.stiffnessZ);
      gsaPropSpr.FrictionCoeff = specklePropertySpring.frictionCoefficient;
      return true;
    }

    private bool SetPropertySpringGeneral(PropertySpring specklePropertySpring, GsaPropSpr gsaPropSpr)
    {
      gsaPropSpr.PropertyType = StructuralSpringPropertyType.General;
      gsaPropSpr.Stiffnesses.Add(GwaAxisDirection6.X, specklePropertySpring.stiffnessX);
      gsaPropSpr.Stiffnesses.Add(GwaAxisDirection6.Y, specklePropertySpring.stiffnessY);
      gsaPropSpr.Stiffnesses.Add(GwaAxisDirection6.Z, specklePropertySpring.stiffnessZ);
      gsaPropSpr.Stiffnesses.Add(GwaAxisDirection6.XX, specklePropertySpring.stiffnessXX);
      gsaPropSpr.Stiffnesses.Add(GwaAxisDirection6.YY, specklePropertySpring.stiffnessYY);
      gsaPropSpr.Stiffnesses.Add(GwaAxisDirection6.ZZ, specklePropertySpring.stiffnessZZ);
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
      var lookupIndex = Instance.GsaModel.Cache.LookupIndex<GsaAlign>(specklePath.alignment.applicationId);
      GsaAlign gsaAlign = null;
      
      if (lookupIndex != null)
      {
        gsaAlign = (GsaAlign)(AlignToNative(specklePath.alignment)).First();
        lookupIndex = gsaAlign.Index;
      }

      var gsaPath = new GsaPath()
      {
        ApplicationId = speckleObject.applicationId,
        Index = Instance.GsaModel.Cache.ResolveIndex<GsaPath>(specklePath.applicationId),
        Name = specklePath.name,
        Sid = speckleObject.id,
        Factor = specklePath.factor,
        Alignment = lookupIndex,
        Group = specklePath.group,
        Left = specklePath.left,
        Right = specklePath.right,
        NumMarkedLanes = specklePath.numMarkedLanes,
        Type = specklePath.type.ToNative(),
      };
      if(gsaAlign != null)
        return new List<GsaRecord>() { gsaAlign, gsaPath };
      else
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
        gsaAxisIndex = speckleAxis.GetIndex<GsaAxis>();
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

    private bool GetReleases(Restraint speckleRelease, out Dictionary<GwaAxisDirection6, ReleaseCode> gsaRelease, out List<double> gsaStiffnesses)
    {
      if (speckleRelease.code.ToUpperInvariant().IndexOf('K') > 0)
      {
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
        gsaAxisIndex = speckleAxis.GetIndex<GsaAxis>();
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
        gsaDescription += "L" + speckleLoadCases[i].GetIndex<GsaLoadCase>();
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
          desc += "C" + loadCases[i].GetIndex<GsaCombination>();
        }
        else if (loadCases[i].GetType() == typeof(GSAAnalysisCase))
        {
          desc += "A" + loadCases[i].GetIndex<GsaAnal>();
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
        Index = speckleSteel.GetIndex<GsaMatSteel>(),
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
      var gsaMat = new GsaMat();
      gsaMat.Name = speckleObject.GetDynamicValue<string>("Name");
      gsaMat.E = speckleObject.GetDynamicValue<double?>("E");
      gsaMat.F = speckleObject.GetDynamicValue<double?>("F");
      gsaMat.Nu = speckleObject.GetDynamicValue<double?>("Nu");
      gsaMat.G = speckleObject.GetDynamicValue<double?>("G");
      gsaMat.Rho = speckleObject.GetDynamicValue<double?>("Rho");
      gsaMat.Alpha = speckleObject.GetDynamicValue<double?>("Alpha");
      gsaMat.Prop = GetMatAnal(speckleObject.GetDynamicValue<Base>("Prop"));
      gsaMat.Uls = GetMatCurveParam(speckleObject.GetDynamicValue<Base>("Uls"));
      gsaMat.Sls = GetMatCurveParam(speckleObject.GetDynamicValue<Base>("Sls"));
      gsaMat.Eps = speckleObject.GetDynamicValue<double?>("Eps");
      gsaMat.Cost = speckleObject.GetDynamicValue<double?>("Cost");
      gsaMat.Type = speckleObject.GetDynamicEnum<MatType>("Type");
      gsaMat.PtsUC = speckleObject.GetDynamicValue<double[]>("PtsUC");
      gsaMat.PtsUC = speckleObject.GetDynamicValue<double[]>("PtsSC");
      gsaMat.PtsUC = speckleObject.GetDynamicValue<double[]>("PtsUT");
      gsaMat.PtsUC = speckleObject.GetDynamicValue<double[]>("PtsST");
      if (gsaMat.PtsUC != null)
      {
        gsaMat.NumUC = gsaMat.PtsUC.Length;
        gsaMat.AbsUC = speckleObject.GetDynamicEnum<Dimension>("AbsUC");
        gsaMat.OrdUC = speckleObject.GetDynamicEnum<Dimension>("OrdUC");
      }
      if (gsaMat.PtsSC != null)
      {
        gsaMat.NumSC = gsaMat.PtsSC.Length;
        gsaMat.AbsSC = speckleObject.GetDynamicEnum<Dimension>("AbsSC");
        gsaMat.OrdSC = speckleObject.GetDynamicEnum<Dimension>("OrdSC");
      }
      if (gsaMat.PtsUT != null)
      {
        gsaMat.NumUT = gsaMat.PtsUT.Length;
        gsaMat.AbsUT = speckleObject.GetDynamicEnum<Dimension>("AbsUT");
        gsaMat.OrdUT = speckleObject.GetDynamicEnum<Dimension>("OrdUT");
      }
      if (gsaMat.PtsST != null)
      {
        gsaMat.NumST = gsaMat.PtsST.Length;
        gsaMat.AbsST = speckleObject.GetDynamicEnum<Dimension>("AbsST");
        gsaMat.OrdST = speckleObject.GetDynamicEnum<Dimension>("OrdST");
      }
      return gsaMat;
    }

    private GsaMatAnal GetMatAnal(Base speckleObject)
    {
      var gsaMatAnal = new GsaMatAnal();
      gsaMatAnal.Name = speckleObject.GetDynamicValue<string>("Name");
      gsaMatAnal.Colour = speckleObject.GetDynamicEnum<Colour>("Colour");
      gsaMatAnal.Type = speckleObject.GetDynamicEnum<MatAnalType>("Type");
      gsaMatAnal.NumParams = speckleObject.GetDynamicValue<int?>("NumParams");
      gsaMatAnal.E = speckleObject.GetDynamicValue<double?>("E");
      gsaMatAnal.Nu = speckleObject.GetDynamicValue<double?>("Nu");
      gsaMatAnal.Rho = speckleObject.GetDynamicValue<double?>("Rho");
      gsaMatAnal.Alpha = speckleObject.GetDynamicValue<double?>("Alpha");
      gsaMatAnal.G = speckleObject.GetDynamicValue<double?>("G");
      gsaMatAnal.Damp = speckleObject.GetDynamicValue<double?>("Damp");
      gsaMatAnal.Yield = speckleObject.GetDynamicValue<double?>("Yield");
      gsaMatAnal.Ultimate = speckleObject.GetDynamicValue<double?>("Ultimate");
      gsaMatAnal.Eh = speckleObject.GetDynamicValue<double?>("Eh");
      gsaMatAnal.Beta = speckleObject.GetDynamicValue<double?>("Beta");
      gsaMatAnal.Cohesion = speckleObject.GetDynamicValue<double?>("Cohesion");
      gsaMatAnal.Phi = speckleObject.GetDynamicValue<double?>("Phi");
      gsaMatAnal.Psi = speckleObject.GetDynamicValue<double?>("Psi");
      gsaMatAnal.Scribe = speckleObject.GetDynamicValue<double?>("Scribe");
      gsaMatAnal.Ex = speckleObject.GetDynamicValue<double?>("Ex");
      gsaMatAnal.Ey = speckleObject.GetDynamicValue<double?>("Ey");
      gsaMatAnal.Ez = speckleObject.GetDynamicValue<double?>("Ez");
      gsaMatAnal.Nuxy = speckleObject.GetDynamicValue<double?>("Nuxy");
      gsaMatAnal.Nuyz = speckleObject.GetDynamicValue<double?>("Nuyz");
      gsaMatAnal.Nuzx = speckleObject.GetDynamicValue<double?>("Nuzx");
      gsaMatAnal.Alphax = speckleObject.GetDynamicValue<double?>("Alphax");
      gsaMatAnal.Alphay = speckleObject.GetDynamicValue<double?>("Alphay");
      gsaMatAnal.Alphaz = speckleObject.GetDynamicValue<double?>("Alphaz");
      gsaMatAnal.Gxy = speckleObject.GetDynamicValue<double?>("Gxy");
      gsaMatAnal.Gyz = speckleObject.GetDynamicValue<double?>("Gyz");
      gsaMatAnal.Gzx = speckleObject.GetDynamicValue<double?>("Gzx");
      gsaMatAnal.Comp = speckleObject.GetDynamicValue<double?>("Comp");
      return gsaMatAnal;
    }

    private GsaMatCurveParam GetMatCurveParam(Base speckleObject)
    {
      var gsaMatCurveParam = new GsaMatCurveParam();
      gsaMatCurveParam.Name = speckleObject.GetDynamicValue<string>("Name");
      var model = speckleObject.GetDynamicValue<List<string>>("Model");
      gsaMatCurveParam.Model = model.Select(s => Enum.TryParse(s, true, out MatCurveParamType v) ? v : MatCurveParamType.UNDEF).ToList();
      gsaMatCurveParam.StrainElasticCompression = speckleObject.GetDynamicValue<double?>("StrainElasticCompression");
      gsaMatCurveParam.StrainElasticTension = speckleObject.GetDynamicValue<double?>("StrainElasticTension");
      gsaMatCurveParam.StrainPlasticCompression = speckleObject.GetDynamicValue<double?>("StrainPlasticCompression");
      gsaMatCurveParam.StrainPlasticTension = speckleObject.GetDynamicValue<double?>("StrainPlasticTension");
      gsaMatCurveParam.StrainFailureCompression = speckleObject.GetDynamicValue<double?>("StrainFailureCompression");
      gsaMatCurveParam.StrainFailureTension = speckleObject.GetDynamicValue<double?>("StrainFailureTension");
      gsaMatCurveParam.GammaF = speckleObject.GetDynamicValue<double?>("GammaF");
      gsaMatCurveParam.GammaE = speckleObject.GetDynamicValue<double?>("GammaE");
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
        Index = speckleProperty.GetIndex<GsaSection>(),
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
