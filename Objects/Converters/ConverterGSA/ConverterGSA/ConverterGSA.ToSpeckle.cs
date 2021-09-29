using Objects.Geometry;
using Objects.Structural;
using Objects.Structural.Geometry;
using Objects.Structural.GSA.Geometry;
using Objects.Structural.GSA.Loading;
using Objects.Structural.GSA.Materials;
using Objects.Structural.GSA.Properties;
using Objects.Structural.Loading;
using Objects.Structural.Properties;
using Objects.Structural.Properties.Profiles;
using Objects.Structural.Results;
using Speckle.Core.Models;
using Speckle.GSA.API;
using Speckle.GSA.API.CsvSchema;
using Speckle.GSA.API.GwaSchema;
using System;
using System.Collections.Generic;
using System.Linq;
using Restraint = Objects.Structural.Geometry.Restraint;
using AxisDirection6 = Objects.Structural.GSA.Geometry.AxisDirection6;
using GwaAxisDirection6 = Speckle.GSA.API.GwaSchema.AxisDirection6;
using Objects.Structural.Materials;
using Objects.Structural.GSA.Bridge;
using Objects.Structural.GSA.Analysis;

namespace ConverterGSA
{
  //Container just for ToSpeckle methods, and their helper methods
  public partial class ConverterGSA
  {
    private Dictionary<Type, ToSpeckleMethodDelegate> ToSpeckleFns;

    private void SetupToSpeckleFns()
    {
      ToSpeckleFns = new Dictionary<Type, ToSpeckleMethodDelegate>()
      {
        //Geometry
        { typeof(GsaAssembly), GsaAssemblyToSpeckle },
        { typeof(GsaAxis), GsaAxisToSpeckle },
        { typeof(GsaNode), GsaNodeToSpeckle },
        { typeof(GsaEl), GsaElementToSpeckle },
        { typeof(GsaMemb), GsaMemberToSpeckle },
        { typeof(GsaGridLine), GsaGridLineToSpeckle },
        { typeof(GsaGridPlane), GsaGridPlaneToSpeckle },
        { typeof(GsaGridSurface), GsaGridSurfaceToSpeckle },
        { typeof(GsaPolyline), GsaPolylineToSpeckle },
        //Loading
        { typeof(GsaLoadCase), GsaLoadCaseToSpeckle },
        { typeof(GsaAnal), GsaAnalysisCaseToSpeckle },
        { typeof(GsaCombination), GsaLoadCombinationToSpeckle },
        { typeof(GsaLoad2dFace), GsaLoadFaceToSpeckle },
        { typeof(GsaLoadBeamPoint), GsaLoadBeamToSpeckle },
        { typeof(GsaLoadBeamUdl), GsaLoadBeamToSpeckle },
        { typeof(GsaLoadBeamLine), GsaLoadBeamToSpeckle },
        { typeof(GsaLoadBeamPatch), GsaLoadBeamToSpeckle },
        { typeof(GsaLoadBeamTrilin), GsaLoadBeamToSpeckle },
        { typeof(GsaLoadNode), GsaLoadNodeToSpeckle },
        { typeof(GsaLoadGravity), GsaLoadGravityLoadToSpeckle },
        { typeof(GsaLoad2dThermal), GsaLoadThermal2dToSpeckle },
        { typeof(GsaLoadGridArea), GsaLoadGridAreaToSpeckle },
        { typeof(GsaLoadGridLine), GsaLoadGridLineToSpeckle },
        { typeof(GsaLoadGridPoint), GsaLoadGridPointToSpeckle },
        //Material
        { typeof(GsaMatSteel), GsaMaterialSteelToSpeckle },
        { typeof(GsaMatConcrete), GsaMaterialConcreteToSpeckle },
        //Property
        { typeof(GsaSection), GsaSectionToSpeckle },
        { typeof(GsaProp2d), GsaProperty2dToSpeckle },
        //{ typeof(GsaProp3d), GsaProperty3dToSpeckle }, not supported yet
        { typeof(GsaPropMass), GsaPropertyMassToSpeckle },
        { typeof(GsaPropSpr), GsaPropertySpringToSpeckle },
        //Constraints
        { typeof(GsaRigid), GsaRigidToSpeckle },
        { typeof(GsaGenRest), GsaGenRestToSpeckle },
        //Analysis Stage
        { typeof(GsaAnalStage), GsaStageToSpeckle },
        //Bridge
        { typeof(GsaInfBeam), GsaInfBeamToSpeckle },
        { typeof(GsaInfNode), GsaInfNodeToSpeckle },
        { typeof(GsaAlign), GsaAlignToSpeckle },
        { typeof(GsaPath), GsaPathToSpeckle },
        { typeof(GsaUserVehicle), GsaUserVehicleToSpeckle },
        //TODO: add methods for other GSA keywords
      };
    }
    #region ToSpeckle
    private ToSpeckleResult ToSpeckle(GsaRecord nativeObject, GSALayer layer = GSALayer.Both)
    {
      var nativeType = nativeObject.GetType();
      return ToSpeckleFns.ContainsKey(nativeType) ? ToSpeckleFns[nativeType](nativeObject, layer) : null;
    }

    #region Geometry
    private ToSpeckleResult GsaAssemblyToSpeckle(GsaRecord nativeObject, GSALayer layer = GSALayer.Both)
    {
      var gsaAssembly = (GsaAssembly)nativeObject;
      if (layer == GSALayer.Design && (gsaAssembly.MemberIndices == null || gsaAssembly.MemberIndices.Count == 0)) return new ToSpeckleResult(false);
      if (layer == GSALayer.Analysis && (gsaAssembly.ElementIndices == null || gsaAssembly.ElementIndices.Count == 0)) return new ToSpeckleResult(false);
      var speckleAssembly = new GSAAssembly()
      {
        name = gsaAssembly.Name,
        sizeY = gsaAssembly.SizeY,
        sizeZ = gsaAssembly.SizeZ,
        curveType = gsaAssembly.CurveType.ToString(),
        pointDefinition = gsaAssembly.PointDefn.ToString(),
      };

      if (gsaAssembly.Index.IsIndex()) speckleAssembly.applicationId = Instance.GsaModel.GetApplicationId<GsaAssembly>(gsaAssembly.Index.Value);
      if (gsaAssembly.Index.IsIndex()) speckleAssembly.nativeId = gsaAssembly.Index.Value;
      if (gsaAssembly.Topo1.IsIndex()) speckleAssembly.end1Node = (GSANode)GetNodeFromIndex(gsaAssembly.Topo1.Value);
      if (gsaAssembly.Topo2.IsIndex()) speckleAssembly.end2Node = (GSANode)GetNodeFromIndex(gsaAssembly.Topo2.Value);
      if (gsaAssembly.OrientNode.IsIndex()) speckleAssembly.orientationNode = (GSANode)GetNodeFromIndex(gsaAssembly.OrientNode.Value);
      if (gsaAssembly.CurveOrder.HasValue) speckleAssembly.curveOrder = gsaAssembly.CurveOrder.Value;

      if (GetAssemblyEntites(gsaAssembly, out var entities)) speckleAssembly.entities = entities;
      else return new ToSpeckleResult(false); //TODO: add conversion error

      if (gsaAssembly.IntTopo != null && gsaAssembly.IntTopo.Count > 0) speckleAssembly.entities.AddRange(gsaAssembly.IntTopo.Select(i => GetNodeFromIndex(i)).ToList());

      if (GetAssemblyPoints(gsaAssembly, out var points)) speckleAssembly.points = points;
      else return new ToSpeckleResult(false); //TODO: add conversion error

      return new ToSpeckleResult(speckleAssembly);
    }

    private ToSpeckleResult GsaNodeToSpeckle(GsaRecord nativeObject, GSALayer layer = GSALayer.Both)
    {
      var gsaNode = (GsaNode)nativeObject;
      var speckleNode = new GSANode()
      {
        //-- App agnostic --
        name = gsaNode.Name,
        basePoint = new Point(gsaNode.X, gsaNode.Y, gsaNode.Z),
        constraintAxis = GetConstraintAxis(gsaNode),
        restraint = GetRestraint(gsaNode),

        //-- GSA specific --
        colour = gsaNode.Colour.ToString(),
      };

      //-- App agnostic --
      if (gsaNode.Index.IsIndex()) speckleNode.applicationId = Instance.GsaModel.GetApplicationId<GsaNode>(gsaNode.Index.Value);
      if (gsaNode.MassPropertyIndex.IsIndex()) speckleNode.massProperty = GetPropertyMassFromIndex(gsaNode.MassPropertyIndex.Value);
      if (gsaNode.SpringPropertyIndex.IsIndex()) speckleNode.springProperty = GetPropertySpringFromIndex(gsaNode.SpringPropertyIndex.Value);

      //-- GSA specific --
      if (gsaNode.Index.IsIndex()) speckleNode.nativeId = gsaNode.Index.Value;
      if (gsaNode.MeshSize.IsPositive()) speckleNode.localElementSize = gsaNode.MeshSize.Value;

      if (GsaNodeResultToSpeckle(gsaNode.Index.Value, speckleNode, out var speckleResults))
      {
        return new ToSpeckleResult(layerAgnosticObject: speckleNode, resultObjects: speckleResults.Select(i => (Base)i));
      }
      else
      {
        return new ToSpeckleResult(speckleNode);
      }

      //TODO:
      //SpeckleObject:
      //  PropertyDamper damperProperty { get; set; }
      //  public string units
    }

    private ToSpeckleResult GsaAxisToSpeckle(GsaRecord nativeObject, GSALayer layer = GSALayer.Both)
    {
      var gsaAxis = (GsaAxis)nativeObject;
      //Only supporting cartesian coordinate systems at the moment
      var speckleAxis = new Axis()
      {
        name = gsaAxis.Name,
        axisType = AxisType.Cartesian,
      };
      if (gsaAxis.Index.IsIndex()) speckleAxis.applicationId = Instance.GsaModel.GetApplicationId<GsaAxis>(gsaAxis.Index.Value);
      if (gsaAxis.XDirX.HasValue && gsaAxis.XDirY.HasValue && gsaAxis.XDirZ.HasValue && gsaAxis.XYDirX.HasValue && gsaAxis.XYDirY.HasValue && gsaAxis.XYDirZ.HasValue)
      {
        var origin = new Point(gsaAxis.OriginX, gsaAxis.OriginY, gsaAxis.OriginZ);
        var xdir = (new Vector(gsaAxis.XDirX.Value, gsaAxis.XDirY.Value, gsaAxis.XDirZ.Value)).UnitVector();
        var ydir = (new Vector(gsaAxis.XYDirX.Value, gsaAxis.XYDirY.Value, gsaAxis.XYDirZ.Value)).UnitVector();
        var normal = (xdir * ydir).UnitVector();
        ydir = -(xdir * normal).UnitVector();
        speckleAxis.definition = new Plane(origin, normal, xdir, ydir);
      }
      else
      {
        //TODO: add conversion error
        speckleAxis.definition = GlobalAxis().definition;
      }

      return new ToSpeckleResult(speckleAxis);
    }

    private ToSpeckleResult GsaElementToSpeckle(GsaRecord nativeObject, GSALayer layer = GSALayer.Both)
    {
      if (layer == GSALayer.Design) return new ToSpeckleResult(false);

      var gsaEl = (GsaEl)nativeObject;

      if (gsaEl.Index.IsIndex())
      {
        if (gsaEl.Is1dElement()) //1D element
        {
          var speckleElement1d = GsaElement1dToSpeckle(gsaEl);
          if (speckleElement1d == null)
          {
            return new ToSpeckleResult(false);
          }
          else if (GsaElement1dResultToSpeckle(gsaEl.Index.Value, speckleElement1d, out var speckleResults))
          {
            return new ToSpeckleResult(analysisLayerOnlyObject: speckleElement1d, resultObjects: speckleResults.Select(i => (Base)i));
          }
          else
          {
            return new ToSpeckleResult(analysisLayerOnlyObject: speckleElement1d);
          }
        }
        else if (gsaEl.Is2dElement()) // 2D element
        {
          var speckleElement2d = GsaElement2dToSpeckle(gsaEl);
          if (speckleElement2d == null)
          {
            return new ToSpeckleResult(false);
          }
          else if (GsaElement2dResultToSpeckle(gsaEl.Index.Value, speckleElement2d, out var speckleResults))
          {
            return new ToSpeckleResult(analysisLayerOnlyObject: speckleElement2d, resultObjects: speckleResults.Select(i => (Base)i));
          }
          else
          {
            return new ToSpeckleResult(analysisLayerOnlyObject: speckleElement2d);
          }
        }
        else //3D element
        {
          //TODO: add conversion code for 3D elements
          ConversionErrors.Add(new Exception("GsaElement3dToSpeckle: "
          + "Conversion of 3D elements not yet implemented"));
          return new ToSpeckleResult(false);
        }
      }
      return new ToSpeckleResult(false);
    }

    private GSAElement1D GsaElement1dToSpeckle(GsaEl gsaEl)
    {
      var speckleElement1d = new GSAElement1D()
      {
        //-- App agnostic --
        name = gsaEl.Name,
        type = gsaEl.Type.ToSpeckle1d(),
        end1Releases = GetRestraint(gsaEl.Releases1, gsaEl.Stiffnesses1),
        end2Releases = GetRestraint(gsaEl.Releases2, gsaEl.Stiffnesses1),
        end1Offset = new Vector(),
        end2Offset = new Vector(),
        orientationAngle = 0, //default

        //-- GSA specific --
        colour = gsaEl.Colour.ToString(),
        isDummy = gsaEl.Dummy,
      };

      //-- App agnostic --
      if (gsaEl.Index.IsIndex()) speckleElement1d.applicationId = Instance.GsaModel.GetApplicationId<GsaEl>(gsaEl.Index.Value);
      if (gsaEl.NodeIndices.Count >= 2)
      {
        speckleElement1d.end1Node = GetNodeFromIndex(gsaEl.NodeIndices[0]);
        speckleElement1d.end2Node = GetNodeFromIndex(gsaEl.NodeIndices[1]);
        speckleElement1d.topology = gsaEl.NodeIndices.Select(i => GetNodeFromIndex(i)).ToList();
      }
      else
      {
        ConversionErrors.Add(new Exception("GsaElement1dToSpeckle: "
          + "Error converting 1D element with application id (" + speckleElement1d.applicationId + "). "
          + "There must be atleast 2 nodes to define the element"));
        return null;
      }
      if (gsaEl.PropertyIndex.IsIndex()) speckleElement1d.property = GetProperty1dFromIndex(gsaEl.PropertyIndex.Value);
      if (gsaEl.Angle.HasValue) speckleElement1d.orientationAngle = gsaEl.Angle.Value; //TODO: GSA stores in degrees, do we want to leave it as degrees?
      if (gsaEl.OrientationNodeIndex.IsIndex()) speckleElement1d.orientationNode = GetNodeFromIndex(gsaEl.OrientationNodeIndex.Value);
      speckleElement1d.localAxis = GetLocalAxis(speckleElement1d.end1Node, speckleElement1d.end2Node, speckleElement1d.orientationNode, speckleElement1d.orientationAngle.Radians());
      if (gsaEl.End1OffsetX.HasValue) speckleElement1d.end1Offset.x = gsaEl.End1OffsetX.Value;
      if (gsaEl.OffsetY.HasValue) speckleElement1d.end1Offset.y = gsaEl.OffsetY.Value;
      if (gsaEl.OffsetZ.HasValue) speckleElement1d.end1Offset.z = gsaEl.OffsetZ.Value;
      if (gsaEl.End2OffsetX.HasValue) speckleElement1d.end2Offset.x = gsaEl.End2OffsetX.Value;
      if (gsaEl.OffsetY.HasValue) speckleElement1d.end2Offset.y = gsaEl.OffsetY.Value;
      if (gsaEl.OffsetZ.HasValue) speckleElement1d.end2Offset.z = gsaEl.OffsetZ.Value;

      //-- GSA specific --
      if (gsaEl.Index.IsIndex()) speckleElement1d.nativeId = gsaEl.Index.Value;
      if (gsaEl.Group.IsIndex()) speckleElement1d.group = gsaEl.Group.Value;

      //TODO:
      //NativeObject:
      //  TaperOffsetPercentageEnd1
      //  TaperOffsetPercentageEnd2
      //  ParentIndex
      //SpeckleObject:
      //  public string action
      //  public ICurve baseLine
      //  public Base parent
      //  Mesh displayMesh
      //  public string units

      return speckleElement1d;
    }

    private GSAElement2D GsaElement2dToSpeckle(GsaEl gsaEl)
    {
      var speckleElement2d = new GSAElement2D()
      {
        //-- App agnostic --
        name = gsaEl.Name,
        type = gsaEl.Type.ToSpeckle2d(),
        parent = null,
        displayMesh = DisplayMesh2d(gsaEl.NodeIndices),
        //baseMesh = null,
        orientationAngle = 0, //default

        //-- GSA specific --
        colour = gsaEl.Colour.ToString(),
        isDummy = gsaEl.Dummy,
      };

      //-- App agnostic --
      if (gsaEl.Index.IsIndex()) speckleElement2d.applicationId = Instance.GsaModel.GetApplicationId<GsaEl>(gsaEl.Index.Value);
      if (gsaEl.NodeIndices.Count >= 3)
      {
        speckleElement2d.topology = gsaEl.NodeIndices.Select(i => GetNodeFromIndex(i)).ToList();
      }
      else
      {
        ConversionErrors.Add(new Exception("GsaElement2dToSpeckle: "
          + "Error converting 2D element with application id (" + speckleElement2d.applicationId + "). "
          + "There must be atleast 3 nodes to define the element"));
        return null;
      }
      if (gsaEl.PropertyIndex.IsIndex()) speckleElement2d.property = GetProperty2dFromIndex(gsaEl.PropertyIndex.Value);
      if (gsaEl.OffsetZ.HasValue) speckleElement2d.offset = gsaEl.OffsetZ.Value;
      if (gsaEl.Angle.HasValue) speckleElement2d.orientationAngle = gsaEl.Angle.Value; //TODO: GSA stores in degrees, do we want to leave it as degrees?

      //-- GSA specific --
      if (gsaEl.Index.IsIndex()) speckleElement2d.nativeId = gsaEl.Index.Value;
      if (gsaEl.Group.IsIndex()) speckleElement2d.group = gsaEl.Group.Value;

      return speckleElement2d;

      //TODO:
      //NativeObject:
      //  ParentIndex
      //SpeckleObject:
      //  public Mesh baseMesh
      //  public Base parent
      //  public string units

      //TODO: remove public List<List<Node>> voids from Element2D definition?
    }

    private ToSpeckleResult GsaMemberToSpeckle(GsaRecord nativeObject, GSALayer layer = GSALayer.Both)
    {
      if (layer == GSALayer.Analysis) return new ToSpeckleResult(false);

      var gsaMemb = (GsaMemb)nativeObject;

      if (gsaMemb.Index.IsIndex())
      {
        if (gsaMemb.Is1dMember()) //1D element
        {
          var speckleMember1d = GsaMember1dToSpeckle(gsaMemb);
          if (speckleMember1d == null) return new ToSpeckleResult(false);
          return new ToSpeckleResult(designLayerOnlyObject: speckleMember1d);
        }
        else if (gsaMemb.Is2dMember()) // 2D element
        {
          var speckleMember2d = GsaMember2dToSpeckle(gsaMemb);
          if (speckleMember2d == null) return new ToSpeckleResult(false);
          return new ToSpeckleResult(designLayerOnlyObject: speckleMember2d);
        }
        else //3D element
        {
          //TODO: add conversion code for 3D members
          ConversionErrors.Add(new Exception("GsaMember3dToSpeckle: "
          + "Conversion of 3D members not yet implemented"));
          return new ToSpeckleResult(false);
        }
      }
      return new ToSpeckleResult(false);
    }

    private GSAMember1D GsaMember1dToSpeckle(GsaMemb gsaMemb)
    {
      var speckleMember1d = new GSAMember1D()
      {
        //-- App agnostic --
        name = gsaMemb.Name,
        type = gsaMemb.Type.ToSpeckle1d(),
        end1Releases = GetRestraint(gsaMemb.Releases1, gsaMemb.Stiffnesses1),
        end2Releases = GetRestraint(gsaMemb.Releases2, gsaMemb.Stiffnesses2),
        end1Offset = new Vector(),
        end2Offset = new Vector(),
        orientationAngle = 0, //default

        //-- GSA specific --
        colour = gsaMemb.Colour.ToString(),
        isDummy = gsaMemb.Dummy,
        intersectsWithOthers = gsaMemb.IsIntersector,
      };

      //-- App agnostic --
      if (gsaMemb.Index.IsIndex()) speckleMember1d.applicationId = Instance.GsaModel.GetApplicationId<GsaMemb>(gsaMemb.Index.Value);
      if (gsaMemb.NodeIndices.Count >= 2)
      {
        speckleMember1d.end1Node = GetNodeFromIndex(gsaMemb.NodeIndices[0]);
        speckleMember1d.end2Node = GetNodeFromIndex(gsaMemb.NodeIndices[1]);
        speckleMember1d.topology = gsaMemb.NodeIndices.Select(i => GetNodeFromIndex(i)).ToList();
      }
      else
      {
        ConversionErrors.Add(new Exception("GsaMember1dToSpeckle: "
          + "Error converting 1D member with application id (" + speckleMember1d.applicationId + "). "
          + "There must be atleast 2 nodes to define the member"));
        return null;
      }
      if (gsaMemb.PropertyIndex.IsIndex()) speckleMember1d.property = GetProperty1dFromIndex(gsaMemb.PropertyIndex.Value);
      if (gsaMemb.OrientationNodeIndex.IsIndex()) speckleMember1d.orientationNode = GetNodeFromIndex(gsaMemb.OrientationNodeIndex.Value);
      if (gsaMemb.Angle.HasValue) speckleMember1d.orientationAngle = gsaMemb.Angle.Value; //TODO: GSA stores in degrees, do we want to leave it as degrees?
      speckleMember1d.localAxis = GetLocalAxis(speckleMember1d.end1Node, speckleMember1d.end2Node, speckleMember1d.orientationNode, speckleMember1d.orientationAngle.Radians());
      if (gsaMemb.End1OffsetX.HasValue) speckleMember1d.end1Offset.x = gsaMemb.End1OffsetX.Value;
      if (gsaMemb.OffsetY.HasValue) speckleMember1d.end1Offset.y = gsaMemb.OffsetY.Value;
      if (gsaMemb.OffsetZ.HasValue) speckleMember1d.end1Offset.z = gsaMemb.OffsetZ.Value;
      if (gsaMemb.End2OffsetX.HasValue) speckleMember1d.end2Offset.x = gsaMemb.End2OffsetX.Value;
      if (gsaMemb.OffsetY.HasValue) speckleMember1d.end2Offset.y = gsaMemb.OffsetY.Value;
      if (gsaMemb.OffsetZ.HasValue) speckleMember1d.end2Offset.z = gsaMemb.OffsetZ.Value;

      //-- GSA specific --
      if (gsaMemb.Index.IsIndex()) speckleMember1d.nativeId = gsaMemb.Index.Value;
      if (gsaMemb.Group.IsIndex()) speckleMember1d.group = gsaMemb.Group.Value;
      if (gsaMemb.MeshSize.IsPositive()) speckleMember1d.targetMeshSize = gsaMemb.MeshSize.Value;

      //TODO:
      //NativeObject:
      //  Exposure
      //  Voids
      //  PointNodeIndices
      //  Polylines
      //  AdditionalAreas
      //  AnalysisType
      //  Fire
      //  LimitingTemperature
      //  CreationFromStartDays
      //  StartOfDryingDays
      //  AgeAtLoadingDays
      //  RemovedAtDays
      //  RestraintEnd1
      //  RestraintEnd2
      //  EffectiveLengthType
      //  LoadHeight
      //  LoadHeightReferencePoint
      //  MemberHasOffsets
      //  End1AutomaticOffset
      //  End2AutomaticOffset
      //  EffectiveLengthYY
      //  PercentageYY
      //  EffectiveLengthZZ
      //  PercentageZZ
      //  EffectiveLengthLateralTorsional
      //  FractionLateralTorsional
      //  SpanRestraints
      //  PointRestraints
      //SpeckleObject:
      //  public ICurve baseLine
      //  public Base parent
      //  public Mesh displayMesh
      //  public string units

      return speckleMember1d;
    }

    private GSAMember2D GsaMember2dToSpeckle(GsaMemb gsaMemb)
    {
      var speckleMember2d = new GSAMember2D()
      {
        //-- App agnostic --
        name = gsaMemb.Name,
        type = gsaMemb.Type.ToSpeckle2d(),
        displayMesh = DisplayMesh2d(gsaMemb.NodeIndices),
        orientationAngle = 0, //default

        //-- GSA specific --
        colour = gsaMemb.Colour.ToString(),
        isDummy = gsaMemb.Dummy,
        intersectsWithOthers = gsaMemb.IsIntersector,
      };

      //-- App agnostic --
      if (gsaMemb.Index.IsIndex()) speckleMember2d.applicationId = Instance.GsaModel.GetApplicationId<GsaMemb>(gsaMemb.Index.Value);
      if (gsaMemb.NodeIndices.Count >= 3)
      {
        speckleMember2d.topology = gsaMemb.NodeIndices.Select(i => GetNodeFromIndex(i)).ToList();
      }
      else
      {
        ConversionErrors.Add(new Exception("GsaMember2dToSpeckle: "
          + "Error converting 2D member with application id (" + speckleMember2d.applicationId + "). "
          + "There must be atleast 3 nodes to define the member"));
        return null;
      }
      if (gsaMemb.PropertyIndex.IsIndex()) speckleMember2d.property = GetProperty2dFromIndex(gsaMemb.PropertyIndex.Value);
      if (gsaMemb.Offset2dZ.HasValue) speckleMember2d.offset = gsaMemb.Offset2dZ.Value;
      if (gsaMemb.Angle.HasValue) speckleMember2d.orientationAngle = gsaMemb.Angle.Value;  //TODO: GSA stores in degrees, do we want to leave it as degrees?
      if (gsaMemb.Voids != null && gsaMemb.Voids.Count > 0)
      {
        speckleMember2d.voids = gsaMemb.Voids.Select(v => v.Select(i => GetNodeFromIndex(i)).ToList()).ToList();
      }

      //-- GSA specific --
      if (gsaMemb.Index.IsIndex()) speckleMember2d.nativeId = gsaMemb.Index.Value;
      if (gsaMemb.Group.IsIndex()) speckleMember2d.group = gsaMemb.Group.Value;
      if (gsaMemb.MeshSize.IsPositive()) speckleMember2d.targetMeshSize = gsaMemb.MeshSize.Value;

      //TODO:
      //NativeObject:
      //  Exposure
      //  PointNodeIndices
      //  Polylines
      //  AdditionalAreas
      //  AnalysisType
      //  Fire
      //  LimitingTemperature
      //  CreationFromStartDays
      //  StartOfDryingDays
      //  AgeAtLoadingDays
      //  RemovedAtDays
      //  OffsetAutomaticInternal
      //SpeckleObject:
      //  public Mesh baseMesh
      //  public Base parent
      //  public string units

      //Unsupported interim schema members
      //speckleMember2d["exposure"] = gsaMemb.Exposure.ToString();
      //speckleMember2d["points"] = gsaMemb.PointNodeIndices.Select(i => (GSANode)GetNodeFromIndex(i)).ToList();
      //speckleMember2d["lines"] = gsaMemb.Polylines.Select(v => v.Select(i => (GSANode)GetNodeFromIndex(i)).ToList()).ToList();
      //speckleMember2d["areas"] = gsaMemb.AdditionalAreas.Select(v => v.Select(i => (GSANode)GetNodeFromIndex(i)).ToList()).ToList();
      //speckleMember2d["analysisType"] = gsaMemb.AnalysisType.ToString();

      return speckleMember2d;
    }

    private ToSpeckleResult GsaGridLineToSpeckle(GsaRecord nativeObject, GSALayer layer = GSALayer.Both)
    {
      var gsaGridLine = (GsaGridLine)nativeObject;
      var speckleGridLine = new GSAGridLine()
      {
        label = gsaGridLine.Name,
      };
      if (gsaGridLine.Index.IsIndex())
      {
        speckleGridLine.applicationId = Instance.GsaModel.GetApplicationId<GsaGridLine>(gsaGridLine.Index.Value);
        speckleGridLine.nativeId = gsaGridLine.Index.Value;
      }
      if (gsaGridLine.Type == GridLineType.Line) speckleGridLine.baseLine = GetLine(gsaGridLine);
      else if (gsaGridLine.Type == GridLineType.Arc) speckleGridLine.baseLine = GetArc(gsaGridLine);
      return new ToSpeckleResult(speckleGridLine);

      //TODO:
      //SpeckleObject:
      //  public string units
    }

    private ToSpeckleResult GsaGridPlaneToSpeckle(GsaRecord nativeObject, GSALayer layer = GSALayer.Both)
    {
      var gsaGridPlane = (GsaGridPlane)nativeObject;
      var speckleGridPlane = new GSAGridPlane()
      {
        name = gsaGridPlane.Name,
        axis = GetGridPlaneAxis(gsaGridPlane.AxisRefType, gsaGridPlane.AxisIndex),
        toleranceBelow = GetStoreyTolerance(gsaGridPlane.StoreyToleranceBelow, gsaGridPlane.StoreyToleranceBelowAuto, gsaGridPlane.Type),
        toleranceAbove = GetStoreyTolerance(gsaGridPlane.StoreyToleranceAbove, gsaGridPlane.StoreyToleranceAboveAuto, gsaGridPlane.Type),
      };
      if (gsaGridPlane.Index.IsIndex())
      {
        speckleGridPlane.applicationId = Instance.GsaModel.GetApplicationId<GsaGridPlane>(gsaGridPlane.Index.Value);
        speckleGridPlane.nativeId = gsaGridPlane.Index.Value;
      }
      if (gsaGridPlane.Elevation.HasValue) speckleGridPlane.elevation = gsaGridPlane.Elevation.Value;

      return new ToSpeckleResult(speckleGridPlane);

      //TODO:
      //SpeckleObject:
      //  public string units
    }

    private ToSpeckleResult GsaGridSurfaceToSpeckle(GsaRecord nativeObject, GSALayer layer = GSALayer.Both)
    {
      var gsaGridSurface = (GsaGridSurface)nativeObject;
      if (layer == GSALayer.Design && (gsaGridSurface.MemberIndices == null || gsaGridSurface.MemberIndices.Count == 0)) return new ToSpeckleResult(false);
      if (layer == GSALayer.Analysis && (gsaGridSurface.ElementIndices == null || gsaGridSurface.ElementIndices.Count == 0)) return new ToSpeckleResult(false);
      var speckleGridSurface = new GSAGridSurface()
      {
        name = gsaGridSurface.Name,
        gridPlane = GetGridPlane(gsaGridSurface.PlaneRefType, gsaGridSurface.PlaneIndex),
        loadExpansion = gsaGridSurface.Expansion.ToSpeckle(),
        span = gsaGridSurface.Span.ToSpeckle(),
        elements = new List<Base>(),
      };
      if (gsaGridSurface.Index.IsIndex())
      {
        speckleGridSurface.applicationId = Instance.GsaModel.GetApplicationId<GsaGridSurface>(gsaGridSurface.Index.Value);
        speckleGridSurface.nativeId = gsaGridSurface.Index.Value;
      }
      if (gsaGridSurface.Tolerance.IsPositive()) speckleGridSurface.tolerance = gsaGridSurface.Tolerance.Value;
      if (gsaGridSurface.Angle.HasValue) speckleGridSurface.spanDirection = gsaGridSurface.Angle.Value; //TODO: GSA stores in degrees, do we want to leave it as degrees?
      //TODO: add both elements and members? or just one set?
      if (gsaGridSurface.ElementIndices != null && gsaGridSurface.ElementIndices.Count > 0)
      {
        speckleGridSurface.elements.AddRange(gsaGridSurface.ElementIndices.Select(i => GetElementFromIndex(i)).ToList());
      }
      if (gsaGridSurface.MemberIndices != null && gsaGridSurface.MemberIndices.Count > 0)
      {
        speckleGridSurface.elements.AddRange(gsaGridSurface.MemberIndices.Select(i => GetMemberFromIndex(i)).ToList());
      }
      return new ToSpeckleResult(speckleGridSurface);
    }

    private ToSpeckleResult GsaPolylineToSpeckle(GsaRecord nativeObject, GSALayer layer = GSALayer.Both)
    {
      var gsaPolyline = (GsaPolyline)nativeObject;
      var specklePolyline = new GSAPolyline()
      {
        name = gsaPolyline.Name,
        colour = gsaPolyline.Colour.ToString(),
        value = gsaPolyline.Values,
        units = gsaPolyline.Units,
      };
      if (gsaPolyline.Index.IsIndex())
      {
        specklePolyline.nativeId = gsaPolyline.Index.Value;
        specklePolyline.applicationId = Instance.GsaModel.GetApplicationId<GsaPolyline>(gsaPolyline.Index.Value);
      }
      if (gsaPolyline.GridPlaneIndex.IsIndex()) specklePolyline.gridPlane = GetGridPlaneFromIndex(gsaPolyline.GridPlaneIndex.Value);

      return new ToSpeckleResult(specklePolyline);
    }
    #endregion

    #region Loading
    private ToSpeckleResult GsaLoadCaseToSpeckle(GsaRecord nativeObject, GSALayer layer = GSALayer.Both)
    {
      var gsaLoadCase = (GsaLoadCase)nativeObject;
      var speckleLoadCase = new GSALoadCase()
      {
        //number = 0;//TODO: how is this different to nativeId? Should this be removed from schema?
        name = gsaLoadCase.Title,
        loadType = gsaLoadCase.CaseType.ToSpeckle(),
        actionType = gsaLoadCase.CaseType.GetActionType(),
        description = gsaLoadCase.Category.ToString(),
        direction = gsaLoadCase.Direction.ToSpeckle(),
        include = gsaLoadCase.Include.ToString(),
        bridge = gsaLoadCase.Bridge ?? false,
      };

      if (gsaLoadCase.Index.IsIndex()) speckleLoadCase.applicationId = Instance.GsaModel.GetApplicationId<GsaLoadCase>(gsaLoadCase.Index.Value);
      if (gsaLoadCase.Index.IsIndex()) speckleLoadCase.nativeId = gsaLoadCase.Index.Value;
      if (gsaLoadCase.Source.IsIndex()) speckleLoadCase.group = gsaLoadCase.Source.ToString();

      return new ToSpeckleResult(speckleLoadCase);
    }

    private ToSpeckleResult GsaAnalysisCaseToSpeckle(GsaRecord nativeObject, GSALayer layer = GSALayer.Both)
    {
      var gsaAnalysisCase = (GsaAnal)nativeObject;
      //TO DO: update once GsaLoadCase has been updated
      var speckleAnalysisCase = new GSAAnalysisCase()
      {
        name = gsaAnalysisCase.Name,
      };

      if (gsaAnalysisCase.Index.IsIndex()) speckleAnalysisCase.applicationId = Instance.GsaModel.GetApplicationId<GsaAnal>(gsaAnalysisCase.Index.Value);
      if (gsaAnalysisCase.Index.IsIndex()) speckleAnalysisCase.nativeId = gsaAnalysisCase.Index.Value;
      if (gsaAnalysisCase.TaskIndex.IsIndex()) speckleAnalysisCase.task = GetTaskFromIndex(gsaAnalysisCase.TaskIndex.Value);
      if (GetAnalysisCaseFactors(gsaAnalysisCase.Desc, out var loadCases, out var loadFactors))
      {
        speckleAnalysisCase.loadCases = loadCases;
        speckleAnalysisCase.loadFactors = loadFactors;
      }
      return new ToSpeckleResult(speckleAnalysisCase);
    }

    private ToSpeckleResult GsaLoadCombinationToSpeckle(GsaRecord nativeObject, GSALayer layer = GSALayer.Both)
    {
      var gsaCombination = (GsaCombination)nativeObject;
      var speckleLoadCombination = new GSALoadCombination()
      {
        //-- App agnostic --
        name = gsaCombination.Name,
        combinationType = GetCombinationType(gsaCombination.Desc)
      };

      //-- App agnostic --
      if (gsaCombination.Index.IsIndex()) speckleLoadCombination.applicationId = Instance.GsaModel.GetApplicationId<GsaCombination>(gsaCombination.Index.Value);
      if (GetLoadCombinationFactors(gsaCombination.Desc, out var loadCases, out var loadFactors))
      {
        speckleLoadCombination.loadCases = loadCases;
        speckleLoadCombination.loadFactors = loadFactors;
      }

      //-- GSA specific --
      if (gsaCombination.Index.IsIndex()) speckleLoadCombination.nativeId = gsaCombination.Index.Value;

      return new ToSpeckleResult(speckleLoadCombination);
    }

    private ToSpeckleResult GsaLoadFaceToSpeckle(GsaRecord nativeObject, GSALayer layer = GSALayer.Both)
    {
      var gsaLoad2dFace = (GsaLoad2dFace)nativeObject;
      var speckleFaceLoad = new GSALoadFace()
      {
        //-- App agnostic --
        name = gsaLoad2dFace.Name,
        elements = gsaLoad2dFace.ElementIndices.Select(i => (Base)GetElement2DFromIndex(i)).ToList(),
        loadType = gsaLoad2dFace.Type.ToSpeckle(),
        direction = gsaLoad2dFace.LoadDirection.ToSpeckle(),
        loadAxisType = gsaLoad2dFace.AxisRefType.ToSpeckle(),
        isProjected = gsaLoad2dFace.Projected,
        values = gsaLoad2dFace.Values,
      };

      //-- App agnostic --
      if (gsaLoad2dFace.Index.IsIndex()) speckleFaceLoad.applicationId = Instance.GsaModel.GetApplicationId<GsaLoad2dFace>(gsaLoad2dFace.Index.Value);
      if (gsaLoad2dFace.LoadCaseIndex.IsIndex()) speckleFaceLoad.loadCase = GetLoadCaseFromIndex(gsaLoad2dFace.LoadCaseIndex.Value);
      if (gsaLoad2dFace.AxisRefType == AxisRefType.Reference && gsaLoad2dFace.AxisIndex.IsIndex())
      {
        speckleFaceLoad.loadAxis = GetAxisFromIndex(gsaLoad2dFace.AxisIndex.Value);
      }
      if (gsaLoad2dFace.Type == Load2dFaceType.Point && gsaLoad2dFace.R.HasValue && gsaLoad2dFace.S.HasValue)
      {
        speckleFaceLoad.positions = new List<double>() { gsaLoad2dFace.R.Value, gsaLoad2dFace.S.Value };
      }

      //-- GSA specific --
      if (gsaLoad2dFace.Index.IsIndex()) speckleFaceLoad.nativeId = gsaLoad2dFace.Index.Value;

      return new ToSpeckleResult(speckleFaceLoad);
    }

    private ToSpeckleResult GsaLoadBeamToSpeckle(GsaRecord nativeObject, GSALayer layer = GSALayer.Both)
    {
      var gsaLoadBeam = (GsaLoadBeam)nativeObject;
      var type = gsaLoadBeam.GetType();
      var speckleBeamLoad = new GSALoadBeam()
      {
        //-- App agnostic --
        name = gsaLoadBeam.Name,
        elements = gsaLoadBeam.ElementIndices.Select(i => (Base)GetElement1DFromIndex(i)).ToList(),
        loadType = type.ToSpeckle(),
        direction = gsaLoadBeam.LoadDirection.ToSpeckleLoad(),
        loadAxisType = gsaLoadBeam.AxisRefType.ToSpeckle(),
        isProjected = gsaLoadBeam.Projected,
      };

      //-- App agnostic --
      if (gsaLoadBeam.Index.IsIndex()) speckleBeamLoad.applicationId = Instance.GsaModel.Cache.GetApplicationId(type, gsaLoadBeam.Index.Value);
      if (gsaLoadBeam.LoadCaseIndex.IsIndex()) speckleBeamLoad.loadCase = GetLoadCaseFromIndex(gsaLoadBeam.LoadCaseIndex.Value);
      if (gsaLoadBeam.AxisRefType == LoadBeamAxisRefType.Reference && gsaLoadBeam.AxisIndex.IsIndex())
      {
        speckleBeamLoad.loadAxis = GetAxisFromIndex(gsaLoadBeam.AxisIndex.Value);
      }
      if (GetLoadBeamValues(gsaLoadBeam, out var v)) speckleBeamLoad.values = v;
      if (GetLoadBeamPositions(gsaLoadBeam, out var p)) speckleBeamLoad.positions = p;

      //-- GSA specific --
      if (gsaLoadBeam.Index.IsIndex()) speckleBeamLoad.nativeId = gsaLoadBeam.Index.Value;

      return new ToSpeckleResult(speckleBeamLoad);
    }

    private ToSpeckleResult GsaLoadNodeToSpeckle(GsaRecord nativeObject, GSALayer layer = GSALayer.Both)
    {
      var gsaLoadNode = (GsaLoadNode)nativeObject;
      var speckleNodeLoad = new GSALoadNode()
      {
        //-- App agnostic --
        name = gsaLoadNode.Name,
        direction = gsaLoadNode.LoadDirection.ToSpeckleLoad(),
        nodes = gsaLoadNode.NodeIndices.Select(i => GetNodeFromIndex(i)).ToList(),
      };

      //-- App agnostic --
      if (gsaLoadNode.Index.IsIndex()) speckleNodeLoad.applicationId = Instance.GsaModel.GetApplicationId<GsaLoadNode>(gsaLoadNode.Index.Value);
      if (gsaLoadNode.LoadCaseIndex.IsIndex()) speckleNodeLoad.loadCase = GetLoadCaseFromIndex(gsaLoadNode.LoadCaseIndex.Value);
      if (gsaLoadNode.Value.HasValue) speckleNodeLoad.value = gsaLoadNode.Value.Value;
      if (gsaLoadNode.GlobalAxis)
      {
        speckleNodeLoad.loadAxis = GlobalAxis();
      }
      else if (gsaLoadNode.AxisIndex.IsIndex())
      {
        speckleNodeLoad.loadAxis = GetAxisFromIndex(gsaLoadNode.AxisIndex.Value);
      }

      //-- GSA specific --
      if (gsaLoadNode.Index.IsIndex()) speckleNodeLoad.nativeId = gsaLoadNode.Index.Value;

      return new ToSpeckleResult(speckleNodeLoad);
    }

    private ToSpeckleResult GsaLoadGravityLoadToSpeckle(GsaRecord nativeObject, GSALayer layer = GSALayer.Both)
    {
      var gsaLoadGravity = (GsaLoadGravity)nativeObject;
      var speckleGravityLoad = new GSALoadGravity()
      {
        //-- App agnostic --
        name = gsaLoadGravity.Name,
        elements = gsaLoadGravity.ElementIndices.Select(i => GetElementFromIndex(i)).ToList(),
        nodes = gsaLoadGravity.Nodes.Select(i => (Base)GetNodeFromIndex(i)).ToList(),
        gravityFactors = GetGravityFactors(gsaLoadGravity),
      };

      //-- App agnostic --
      if (gsaLoadGravity.Index.IsIndex()) speckleGravityLoad.applicationId = Instance.GsaModel.GetApplicationId<GsaLoadGravity>(gsaLoadGravity.Index.Value);
      if (gsaLoadGravity.LoadCaseIndex.IsIndex()) speckleGravityLoad.loadCase = GetLoadCaseFromIndex(gsaLoadGravity.LoadCaseIndex.Value);

      //-- GSA specific --
      if (gsaLoadGravity.Index.IsIndex()) speckleGravityLoad.nativeId = gsaLoadGravity.Index.Value;

      return new ToSpeckleResult(speckleGravityLoad);
    }

    private ToSpeckleResult GsaLoadThermal2dToSpeckle(GsaRecord nativeObject, GSALayer layer = GSALayer.Both)
    {
      var gsaLoad2dThermal = (GsaLoad2dThermal)nativeObject;
      var speckleLoad = new GSALoadThermal2d()
      {
        name = gsaLoad2dThermal.Name,
        elements = gsaLoad2dThermal.ElementIndices.Select(i => GetElement2DFromIndex(i)).ToList(),
        type = gsaLoad2dThermal.Type.ToSpeckle(),
        values = gsaLoad2dThermal.Values,
      };
      if (gsaLoad2dThermal.Index.IsIndex())
      {
        speckleLoad.applicationId = Instance.GsaModel.GetApplicationId<GsaLoad2dThermal>(gsaLoad2dThermal.Index.Value);
        speckleLoad.nativeId = gsaLoad2dThermal.Index.Value;
      }
      if (gsaLoad2dThermal.LoadCaseIndex.IsIndex()) speckleLoad.loadCase = GetLoadCaseFromIndex(gsaLoad2dThermal.LoadCaseIndex.Value);
      return new ToSpeckleResult(speckleLoad);
    }

    private ToSpeckleResult GsaLoadGridAreaToSpeckle(GsaRecord nativeObject, GSALayer layer = GSALayer.Both)
    {
      var gsaLoad = (GsaLoadGridArea)nativeObject;
      var speckleLoad = new GSALoadGridArea()
      {
        name = gsaLoad.Name,
        loadAxis = GetAxis(gsaLoad.AxisRefType, gsaLoad.AxisIndex),
        isProjected = gsaLoad.Projected,
        direction = gsaLoad.LoadDirection.ToSpeckle(),
        polyline = GetPolyline(gsaLoad.Area, gsaLoad.Polygon, gsaLoad.PolygonIndex),
      };
      if (gsaLoad.Index.IsIndex())
      {
        speckleLoad.applicationId = Instance.GsaModel.GetApplicationId<GsaLoadGridArea>(gsaLoad.Index.Value);
        speckleLoad.nativeId = gsaLoad.Index.Value;
      }
      if (gsaLoad.GridSurfaceIndex.IsIndex()) speckleLoad.gridSurface = GetGridSurfaceFromIndex(gsaLoad.GridSurfaceIndex.Value);
      if (gsaLoad.LoadCaseIndex.IsIndex()) speckleLoad.loadCase = GetLoadCaseFromIndex(gsaLoad.LoadCaseIndex.Value);
      if (gsaLoad.Value.HasValue) speckleLoad.value = gsaLoad.Value.Value;
      return new ToSpeckleResult(speckleLoad);
    }

    private ToSpeckleResult GsaLoadGridLineToSpeckle(GsaRecord nativeObject, GSALayer layer = GSALayer.Both)
    {
      var gsaLoad = (GsaLoadGridLine)nativeObject;
      var speckleLoad = new GSALoadGridLine()
      {
        name = gsaLoad.Name,
        loadAxis = GetAxis(gsaLoad.AxisRefType, gsaLoad.AxisIndex),
        isProjected = gsaLoad.Projected,
        direction = gsaLoad.LoadDirection.ToSpeckle(),
        polyline = GetPolyline(gsaLoad.Line, gsaLoad.Polygon, gsaLoad.PolygonIndex),
      };
      if (gsaLoad.Index.IsIndex())
      {
        speckleLoad.applicationId = Instance.GsaModel.GetApplicationId<GsaLoadGridLine>(gsaLoad.Index.Value);
        speckleLoad.nativeId = gsaLoad.Index.Value;
      }
      if (gsaLoad.GridSurfaceIndex.IsIndex()) speckleLoad.gridSurface = GetGridSurfaceFromIndex(gsaLoad.GridSurfaceIndex.Value);
      if (gsaLoad.LoadCaseIndex.IsIndex()) speckleLoad.loadCase = GetLoadCaseFromIndex(gsaLoad.LoadCaseIndex.Value);
      if (gsaLoad.Value1.HasValue && gsaLoad.Value2.HasValue) speckleLoad.values = new List<double>() { gsaLoad.Value1.Value, gsaLoad.Value2.Value };
      return new ToSpeckleResult(speckleLoad);
    }

    private ToSpeckleResult GsaLoadGridPointToSpeckle(GsaRecord nativeObject, GSALayer layer = GSALayer.Both)
    {
      var gsaLoad = (GsaLoadGridPoint)nativeObject;
      var speckleLoad = new GSALoadGridPoint()
      {
        name = gsaLoad.Name,
        loadAxis = GetAxis(gsaLoad.AxisRefType, gsaLoad.AxisIndex),
        direction = gsaLoad.LoadDirection.ToSpeckle(),
      };
      if (gsaLoad.Index.IsIndex())
      {
        speckleLoad.applicationId = Instance.GsaModel.GetApplicationId<GsaLoadGridPoint>(gsaLoad.Index.Value);
        speckleLoad.nativeId = gsaLoad.Index.Value;
      }
      if (gsaLoad.GridSurfaceIndex.IsIndex()) speckleLoad.gridSurface = GetGridSurfaceFromIndex(gsaLoad.GridSurfaceIndex.Value);
      if (gsaLoad.LoadCaseIndex.IsIndex()) speckleLoad.loadCase = GetLoadCaseFromIndex(gsaLoad.LoadCaseIndex.Value);
      if (gsaLoad.Value.HasValue) speckleLoad.value = gsaLoad.Value.Value;
      if (gsaLoad.X.HasValue && gsaLoad.Y.HasValue) speckleLoad.position = new Point(gsaLoad.X.Value, gsaLoad.Y.Value, 0);
      return new ToSpeckleResult(speckleLoad);
    }
    #endregion

    #region Materials

    private ToSpeckleResult GsaMaterialSteelToSpeckle(GsaRecord nativeObject, GSALayer layer = GSALayer.Both)
    {
      //Currently only handles isotropic steel properties.
      //A lot of information in the gsa objects are currently ignored.

      //Gwa keyword SPEC_STEEL_DESIGN is not well documented:
      //
      //SPEC_STEEL_DESIGN | code
      //
      //Description
      //  Steel design code
      //
      //Parameters
      //  code      steel design code
      //
      //Example (GSA 10.1)
      //  SPEC_STEEL_DESIGN.1	AS 4100-1998	YES	15	YES	15	15	YES	NO	NO	NO
      //
      var gsaSteel = (GsaMatSteel)nativeObject;
      var speckleSteel = new GSASteel()
      {
        name = gsaSteel.Name,
        grade = "",                                 //grade can be determined from gsaMatSteel.Mat.Name (assuming the user doesn't change the default value): e.g. "350(AS3678)"
        type = MaterialType.Steel,
        designCode = "",                            //designCode can be determined from SPEC_STEEL_DESIGN gwa keyword
        codeYear = "",                              //codeYear can be determined from SPEC_STEEL_DESIGN gwa keyword
        yieldStrength = gsaSteel.Fy.Value,
        ultimateStrength = gsaSteel.Fu.Value,
        maxStrain = gsaSteel.EpsP.Value
      };
      if (gsaSteel.Index.IsIndex()) speckleSteel.applicationId = Instance.GsaModel.GetApplicationId<GsaMatSteel>(gsaSteel.Index.Value);

      //the following properties are stored in multiple locations in GSA
      if (Choose(gsaSteel.Mat.E, gsaSteel.Mat.Prop == null ? null : gsaSteel.Mat.Prop.E, out var E)) speckleSteel.elasticModulus = E;
      if (Choose(gsaSteel.Mat.Nu, gsaSteel.Mat.Prop == null ? null : gsaSteel.Mat.Prop.Nu, out var Nu)) speckleSteel.poissonsRatio = Nu;
      if (Choose(gsaSteel.Mat.G, gsaSteel.Mat.Prop == null ? null : gsaSteel.Mat.Prop.G, out var G)) speckleSteel.shearModulus = G;
      if (Choose(gsaSteel.Mat.Rho, gsaSteel.Mat.Prop == null ? null : gsaSteel.Mat.Prop.Rho, out var Rho)) speckleSteel.density = Rho;
      if (Choose(gsaSteel.Mat.Alpha, gsaSteel.Mat.Prop == null ? null : gsaSteel.Mat.Prop.Alpha, out var Alpha)) speckleSteel.thermalExpansivity = Alpha;

      return new ToSpeckleResult(speckleSteel);

      /*public string Name { get => name; set { name = value; } }
    public GsaMat Mat;
    public double? Fy;
    public double? Fu;
    public double? EpsP;
    public double? Eh;*/
    }

    private ToSpeckleResult GsaMaterialConcreteToSpeckle(GsaRecord nativeObject, GSALayer layer = GSALayer.Both)
    {
      //Currently only handles isotropic concrete properties.
      //A lot of information in the gsa objects are currently ignored.

      var gsaConcrete = (GsaMatConcrete)nativeObject;
      var speckleConcrete = new GSAConcrete()
      {
        name = gsaConcrete.Name,
        grade = "",                                 //grade can be determined from gsaMatConcrete.Mat.Name (assuming the user doesn't change the default value): e.g. "32 MPa"
        type = MaterialType.Concrete,
        designCode = "",                            //designCode can be determined from SPEC_CONCRETE_DESIGN gwa keyword: e.g. "AS3600_18" -> "AS3600"
        codeYear = "",                              //codeYear can be determined from SPEC_CONCRETE_DESIGN gwa keyword: e.g. "AS3600_18" - "2018"
        flexuralStrength = 0
      };
      if (gsaConcrete.Index.IsIndex()) speckleConcrete.applicationId = Instance.GsaModel.GetApplicationId<GsaMatConcrete>(gsaConcrete.Index.Value);

      //the following properties might be null
      if (gsaConcrete.Fc.HasValue) speckleConcrete.compressiveStrength = gsaConcrete.Fc.Value;
      if (gsaConcrete.EpsU.HasValue) speckleConcrete.maxCompressiveStrain = gsaConcrete.EpsU.Value;
      if (gsaConcrete.Agg.HasValue) speckleConcrete.maxAggregateSize = gsaConcrete.Agg.Value;
      if (gsaConcrete.Fcdt.HasValue) speckleConcrete.tensileStrength = gsaConcrete.Fcdt.Value;

      //the following properties are stored in multiple locations in GSA
      if (Choose(gsaConcrete.Mat.E, gsaConcrete.Mat.Prop == null ? null : gsaConcrete.Mat.Prop.E, out var E)) speckleConcrete.elasticModulus = E;
      if (Choose(gsaConcrete.Mat.Nu, gsaConcrete.Mat.Prop == null ? null : gsaConcrete.Mat.Prop.Nu, out var Nu)) speckleConcrete.poissonsRatio = Nu;
      if (Choose(gsaConcrete.Mat.G, gsaConcrete.Mat.Prop == null ? null : gsaConcrete.Mat.Prop.G, out var G)) speckleConcrete.shearModulus = G;
      if (Choose(gsaConcrete.Mat.Rho, gsaConcrete.Mat.Prop == null ? null : gsaConcrete.Mat.Prop.Rho, out var Rho)) speckleConcrete.density = Rho;
      if (Choose(gsaConcrete.Mat.Alpha, gsaConcrete.Mat.Prop == null ? null : gsaConcrete.Mat.Prop.Alpha, out var Alpha)) speckleConcrete.thermalExpansivity = Alpha;

      return new ToSpeckleResult(speckleConcrete);
    }

    //Timber: GSA keyword not supported yet
    #endregion

    #region Property
    private ToSpeckleResult GsaSectionToSpeckle(GsaRecord nativeObject, GSALayer layer = GSALayer.Both)
    {
      var gsaSection = (GsaSection)nativeObject;
      //TO DO: update code to handle modifiers once SECTION_MOD (or SECTION_ANAL) keyword is supported
      var speckleProperty1D = new GSAProperty1D()
      {
        //-- App agnostic --
        name = gsaSection.Name,
        memberType = Objects.Structural.Geometry.MemberType.Generic1D,
        referencePoint = gsaSection.ReferencePoint.ToSpeckle(),

        //-- GSA specific --
        colour = gsaSection.Colour.ToString(),
        //designMaterial = new Material(), // what is this used for? how is it different to material?
      };

      //-- App agnostic --
      if (gsaSection.Index.IsIndex()) speckleProperty1D.applicationId = Instance.GsaModel.GetApplicationId<GsaSection>(gsaSection.Index.Value);
      if (gsaSection.RefY.HasValue) speckleProperty1D.offsetY = gsaSection.RefY.Value;
      if (gsaSection.RefZ.HasValue) speckleProperty1D.offsetZ = gsaSection.RefZ.Value;
      var gsaSectionComp = (SectionComp)gsaSection.Components.Find(x => x.GetType() == typeof(SectionComp));
      if (gsaSectionComp.MaterialIndex.IsIndex()) speckleProperty1D.material = GetMaterialFromIndex(gsaSectionComp.MaterialIndex.Value, gsaSectionComp.MaterialType);
      var fns = new Dictionary<Section1dProfileGroup, Func<ProfileDetails, SectionProfile>>
      { { Section1dProfileGroup.Catalogue, GetProfileCatalogue },
        { Section1dProfileGroup.Explicit, GetProfileExplicit },
        { Section1dProfileGroup.Perimeter, GetProfilePerimeter },
        { Section1dProfileGroup.Standard, GetProfileStandard }
      };
      if (fns.ContainsKey(gsaSectionComp.ProfileGroup)) speckleProperty1D.profile = fns[gsaSectionComp.ProfileGroup](gsaSectionComp.ProfileDetails);

      //-- GSA specific --
      if (gsaSection.Index.IsIndex()) speckleProperty1D.nativeId = gsaSection.Index.Value;
      if (gsaSection.Mass.HasValue) speckleProperty1D.additionalMass = gsaSection.Mass.Value;
      if (gsaSection.Cost.HasValue) speckleProperty1D.cost = gsaSection.Cost.Value;
      if (gsaSection.PoolIndex.IsIndex()) speckleProperty1D.poolRef = gsaSection.PoolIndex.Value;

      return new ToSpeckleResult(speckleProperty1D);
    }

    private ToSpeckleResult GsaProperty2dToSpeckle(GsaRecord nativeObject, GSALayer layer = GSALayer.Both)
    {
      var gsaProp2d = (GsaProp2d)nativeObject;
      var speckleProperty2d = new GSAProperty2D()
      {
        //-- App agnostic --
        name = gsaProp2d.Name,
        colour = gsaProp2d.Colour.ToString(),
        zOffset = gsaProp2d.RefZ,
        orientationAxis = GetOrientationAxis(gsaProp2d),
        refSurface = gsaProp2d.RefPt.ToSpeckle(),

        //-- GSA specific --
        additionalMass = gsaProp2d.Mass,
        concreteSlabProp = gsaProp2d.Profile,
        //designMaterial = new Material(), // what is this used for? how is it different to material?
        //cost = 0, //cost is not part of the GWA
      };

      //-- App agnostic --
      if (gsaProp2d.Index.IsIndex()) speckleProperty2d.applicationId = Instance.GsaModel.GetApplicationId<GsaProp2d>(gsaProp2d.Index.Value);
      if (gsaProp2d.Thickness.IsPositive()) speckleProperty2d.thickness = gsaProp2d.Thickness.Value;
      if (gsaProp2d.GradeIndex.IsIndex()) speckleProperty2d.material = GetMaterialFromIndex(gsaProp2d.GradeIndex.Value, gsaProp2d.MatType);
      if (gsaProp2d.Type != Property2dType.NotSet) speckleProperty2d.type = (PropertyType2D)Enum.Parse(typeof(PropertyType2D), gsaProp2d.Type.ToString());
      if (gsaProp2d.InPlaneStiffnessPercentage.HasValue) speckleProperty2d.modifierInPlane = gsaProp2d.InPlaneStiffnessPercentage.Value;  //Only supporting Percentage modifiers
      if (gsaProp2d.BendingStiffnessPercentage.HasValue) speckleProperty2d.modifierBending = gsaProp2d.BendingStiffnessPercentage.Value;
      if (gsaProp2d.ShearStiffnessPercentage.HasValue) speckleProperty2d.modifierShear = gsaProp2d.ShearStiffnessPercentage.Value;
      if (gsaProp2d.VolumePercentage.HasValue) speckleProperty2d.modifierVolume = gsaProp2d.VolumePercentage.Value;

      //-- GSA specific --
      if (gsaProp2d.Index.IsIndex()) speckleProperty2d.nativeId = gsaProp2d.Index.Value;

      return new ToSpeckleResult(speckleProperty2d);
    }

    //Property3D: GSA keyword not supported yet

    private ToSpeckleResult GsaPropertyMassToSpeckle(GsaRecord nativeObject, GSALayer layer = GSALayer.Both)
    {
      var gsaPropMass = (GsaPropMass)nativeObject;
      var specklePropertyMass = new PropertyMass()
      {
        name = gsaPropMass.Name,
        mass = gsaPropMass.Mass,
        inertiaXX = gsaPropMass.Ixx,
        inertiaYY = gsaPropMass.Iyy,
        inertiaZZ = gsaPropMass.Izz,
        inertiaXY = gsaPropMass.Ixy,
        inertiaYZ = gsaPropMass.Iyz,
        inertiaZX = gsaPropMass.Izx
      };
      if (gsaPropMass.Index.IsIndex()) specklePropertyMass.applicationId = Instance.GsaModel.GetApplicationId<GsaPropMass>(gsaPropMass.Index.Value);

      //Mass modifications
      if (gsaPropMass.Mod == MassModification.Modified)
      {
        specklePropertyMass.massModified = true;
        if (gsaPropMass.ModXPercentage.IsPositive()) specklePropertyMass.massModifierX = gsaPropMass.ModXPercentage.Value;
        if (gsaPropMass.ModYPercentage.IsPositive()) specklePropertyMass.massModifierY = gsaPropMass.ModYPercentage.Value;
        if (gsaPropMass.ModZPercentage.IsPositive()) specklePropertyMass.massModifierZ = gsaPropMass.ModZPercentage.Value;
      }
      else
      {
        specklePropertyMass.massModified = false;
      }

      return new ToSpeckleResult(specklePropertyMass);
    }

    private ToSpeckleResult GsaPropertySpringToSpeckle(GsaRecord nativeObject, GSALayer layer = GSALayer.Both)
    {
      var gsaPropSpr = (GsaPropSpr)nativeObject;
      //Apply properties common to all spring types
      var specklePropertySpring = new PropertySpring()
      {
        name = gsaPropSpr.Name,
        dampingRatio = gsaPropSpr.DampingRatio.Value
      };
      if (gsaPropSpr.Index.IsIndex()) specklePropertySpring.applicationId = Instance.GsaModel.GetApplicationId<GsaPropSpr>(gsaPropSpr.Index.Value);

      //Dictionary of fns used to apply spring type specific properties. 
      //Functions will pass by reference specklePropertySpring and make the necessary changes to it
      var fns = new Dictionary<StructuralSpringPropertyType, Func<GsaPropSpr, PropertySpring, bool>>
      { { StructuralSpringPropertyType.Axial, SetProprtySpringAxial },
        { StructuralSpringPropertyType.Torsional, SetPropertySpringTorsional },
        { StructuralSpringPropertyType.Compression, SetProprtySpringCompression },
        { StructuralSpringPropertyType.Tension, SetProprtySpringTension },
        { StructuralSpringPropertyType.Lockup, SetProprtySpringLockup },
        { StructuralSpringPropertyType.Gap, SetProprtySpringGap },
        { StructuralSpringPropertyType.Friction, SetProprtySpringFriction },
        { StructuralSpringPropertyType.General, SetProprtySpringGeneral }
        //CONNECT not yet supported
      };

      //Apply spring type specific properties
      if (fns.ContainsKey(gsaPropSpr.PropertyType)) fns[gsaPropSpr.PropertyType](gsaPropSpr, specklePropertySpring);

      return new ToSpeckleResult(specklePropertySpring);
    }

    //PropertyDamper: GSA keyword not supported yet
    #endregion

    #region Results
    private bool GsaNodeResultToSpeckle(int gsaNodeIndex, Node speckleNode, out List<ResultNode> speckleResults)
    {
      speckleResults = null;
      if (Instance.GsaModel.Proxy.GetResultRecords(ResultGroup.Node, gsaNodeIndex, out var csvRecords))
      {
        speckleResults = new List<ResultNode>();
        var gsaNodeResults = csvRecords.FindAll(so => so is CsvNode).Select(so => (CsvNode)so).ToList();
        foreach (var gsaResult in gsaNodeResults)
        {
          var result = new ResultNode()
          {
            description = "", //???
            permutation = "", //???
            node = speckleNode,
          };

          var gsaCaseIndex = Convert.ToInt32(gsaResult.CaseId.Substring(1));
          if (gsaResult.CaseId[0] == 'A')
          {
            result.resultCase = GetLoadCaseFromIndex(gsaCaseIndex);
          }
          else if (gsaResult.CaseId[0] == 'C')
          {
            result.resultCase = GetLoadCombinationFromIndex(gsaCaseIndex);
          }
          result.applicationId = speckleNode.applicationId + "_" + result.resultCase.applicationId;

          //displacements / rotations
          if (gsaResult.Ux.HasValue) result.dispX = gsaResult.Ux.Value;
          if (gsaResult.Uy.HasValue) result.dispY = gsaResult.Uy.Value;
          if (gsaResult.Uz.HasValue) result.dispZ = gsaResult.Uz.Value;
          if (gsaResult.Rxx.HasValue) result.rotXX = gsaResult.Rxx.Value;
          if (gsaResult.Ryy.HasValue) result.rotYY = gsaResult.Ryy.Value;
          if (gsaResult.Rzz.HasValue) result.rotZZ = gsaResult.Rzz.Value;

          //velocities
          if (gsaResult.Vx.HasValue) result.velX = gsaResult.Vx.Value;
          if (gsaResult.Vy.HasValue) result.velY = gsaResult.Vy.Value;
          if (gsaResult.Vz.HasValue) result.velZ = gsaResult.Vz.Value;
          if (gsaResult.Vxx.HasValue) result.velXX = gsaResult.Vxx.Value;
          if (gsaResult.Vyy.HasValue) result.velYY = gsaResult.Vyy.Value;
          if (gsaResult.Vzz.HasValue) result.velZZ = gsaResult.Vzz.Value;

          //accelerations
          if (gsaResult.Ax.HasValue) result.accX = gsaResult.Ax.Value;
          if (gsaResult.Ay.HasValue) result.accY = gsaResult.Ay.Value;
          if (gsaResult.Az.HasValue) result.accZ = gsaResult.Az.Value;
          if (gsaResult.Axx.HasValue) result.accXX = gsaResult.Axx.Value;
          if (gsaResult.Ayy.HasValue) result.accYY = gsaResult.Ayy.Value;
          if (gsaResult.Azz.HasValue) result.accZZ = gsaResult.Azz.Value;

          //reactions forces/moments
          if (gsaResult.Fx_Reac.HasValue) result.reactionX = gsaResult.Fx_Reac.Value;
          if (gsaResult.Fy_Reac.HasValue) result.reactionY = gsaResult.Fy_Reac.Value;
          if (gsaResult.Fz_Reac.HasValue) result.reactionZ = gsaResult.Fz_Reac.Value;
          if (gsaResult.Mxx_Reac.HasValue) result.reactionXX = gsaResult.Mxx_Reac.Value;
          if (gsaResult.Myy_Reac.HasValue) result.reactionYY = gsaResult.Myy_Reac.Value;
          if (gsaResult.Mzz_Reac.HasValue) result.reactionZZ = gsaResult.Mzz_Reac.Value;

          //constraint forces/moments
          if (gsaResult.Fx_Cons.HasValue) result.constraintX = gsaResult.Fx_Cons.Value;
          if (gsaResult.Fy_Cons.HasValue) result.constraintY = gsaResult.Fy_Cons.Value;
          if (gsaResult.Fz_Cons.HasValue) result.constraintZ = gsaResult.Fz_Cons.Value;
          if (gsaResult.Mxx_Cons.HasValue) result.constraintXX = gsaResult.Mxx_Cons.Value;
          if (gsaResult.Myy_Cons.HasValue) result.constraintYY = gsaResult.Myy_Cons.Value;
          if (gsaResult.Mzz_Cons.HasValue) result.constraintZZ = gsaResult.Mzz_Cons.Value;

          speckleResults.Add(result);
        }
        return true;
      }
      return false;
    }

    public bool GsaElement1dResultToSpeckle(int gsaElementIndex, Element1D speckleElement, out List<Result1D> speckleResults)
    {
      speckleResults = null;
      //Instance.GsaModel.Result1DNumPosition = 2;
      if (Instance.GsaModel.Proxy.GetResultRecords(ResultGroup.Element1d, gsaElementIndex, out var csvRecord))
      {
        speckleResults = new List<Result1D>();
        var gsaElement1dResults = csvRecord.FindAll(so => so is CsvElem1d).Select(so => (CsvElem1d)so).ToList();
        foreach (var gsaResult in gsaElement1dResults)
        {
          var result = new Result1D()
          {
            description = "", //???
            permutation = "", //???
            element = speckleElement,
            position = float.Parse(gsaResult.PosR),
          };

          var gsaCaseIndex = Convert.ToInt32(gsaResult.CaseId.Substring(1));
          if (gsaResult.CaseId[0] == 'A')
          {
            result.resultCase = GetLoadCaseFromIndex(gsaCaseIndex);
          }
          else if (gsaResult.CaseId[0] == 'C')
          {
            result.resultCase = GetLoadCombinationFromIndex(gsaCaseIndex);
          }
          result.applicationId = speckleElement.applicationId + "_" + result.resultCase.applicationId + "_" + result.position.ToString();

          //displacements
          if (gsaResult.Ux.HasValue) result.dispX = gsaResult.Ux.Value;
          if (gsaResult.Uy.HasValue) result.dispY = gsaResult.Uy.Value;
          if (gsaResult.Uz.HasValue) result.dispZ = gsaResult.Uz.Value;

          //forces
          if (gsaResult.Fx.HasValue) result.forceX = gsaResult.Fx.Value;
          if (gsaResult.Fy.HasValue) result.forceY = gsaResult.Fy.Value;
          if (gsaResult.Fz.HasValue) result.forceZ = gsaResult.Fz.Value;

          //moments
          if (gsaResult.Mxx.HasValue) result.momentXX = gsaResult.Mxx.Value;
          if (gsaResult.Myy.HasValue) result.momentYY = gsaResult.Myy.Value;
          if (gsaResult.Mzz.HasValue) result.momentZZ = gsaResult.Mzz.Value;

          speckleResults.Add(result);
        }
        return true;
      }
      return false;
    }

    public bool GsaElement2dResultToSpeckle(int gsaElementIndex, Element2D speckleElement, out List<Result2D> speckleResults)
    {
      speckleResults = null;
      if (Instance.GsaModel.Proxy.GetResultRecords(ResultGroup.Element2d, gsaElementIndex, out var csvRecord))
      {
        speckleResults = new List<Result2D>();
        var gsaElement2dResults = csvRecord.FindAll(so => so is CsvElem2d).Select(so => (CsvElem2d)so).ToList();
        foreach (var gsaResult in gsaElement2dResults)
        {
          var result = new Result2D()
          {
            description = "", //???
            permutation = "", //???
            element = speckleElement,
          };

          if (gsaResult.PosR.HasValue && gsaResult.PosS.HasValue) result.position = new List<double>() { gsaResult.PosR.Value, gsaResult.PosS.Value };

          var gsaCaseIndex = Convert.ToInt32(gsaResult.CaseId.Substring(1));
          if (gsaResult.CaseId[0] == 'A')
          {
            result.resultCase = GetLoadCaseFromIndex(gsaCaseIndex);
          }
          else if (gsaResult.CaseId[0] == 'C')
          {
            result.resultCase = GetLoadCombinationFromIndex(gsaCaseIndex);
          }
          result.applicationId = speckleElement.applicationId + "_" + result.resultCase.applicationId + "_" + result.position[0].ToString() + "_" + result.position[1].ToString();

          //displacements
          if (gsaResult.Ux.HasValue) result.dispX = gsaResult.Ux.Value;
          if (gsaResult.Uy.HasValue) result.dispY = gsaResult.Uy.Value;
          if (gsaResult.Uz.HasValue) result.dispZ = gsaResult.Uz.Value;

          //forces
          if (gsaResult.Nx.HasValue) result.forceXX = gsaResult.Nx.Value;
          if (gsaResult.Ny.HasValue) result.forceYY = gsaResult.Ny.Value;
          if (gsaResult.Nxy.HasValue) result.forceXY = gsaResult.Nxy.Value;
          if (gsaResult.Qx.HasValue) result.shearX = gsaResult.Qx.Value;
          if (gsaResult.Qy.HasValue) result.shearY = gsaResult.Qy.Value;

          //moments
          if (gsaResult.Mx.HasValue) result.momentXX = gsaResult.Mx.Value;
          if (gsaResult.My.HasValue) result.momentYY = gsaResult.My.Value;
          if (gsaResult.Mxy.HasValue) result.momentXY = gsaResult.Mxy.Value;

          //stresses
          if (gsaResult.Xx_b.HasValue) result.stressBotXX = gsaResult.Xx_b.Value;
          if (gsaResult.Yy_b.HasValue) result.stressBotYY = gsaResult.Yy_b.Value;
          if (gsaResult.Zz_b.HasValue) result.stressBotZZ = gsaResult.Zz_b.Value;
          if (gsaResult.Xy_b.HasValue) result.stressBotXY = gsaResult.Xy_b.Value;
          if (gsaResult.Yz_b.HasValue) result.stressBotYZ = gsaResult.Yz_b.Value;
          if (gsaResult.Zx_b.HasValue) result.stressBotZX = gsaResult.Zx_b.Value;
          if (gsaResult.Xx_m.HasValue) result.stressMidXX = gsaResult.Xx_m.Value;
          if (gsaResult.Yy_m.HasValue) result.stressMidYY = gsaResult.Yy_m.Value;
          if (gsaResult.Zz_m.HasValue) result.stressMidZZ = gsaResult.Zz_m.Value;
          if (gsaResult.Xy_m.HasValue) result.stressMidXY = gsaResult.Xy_m.Value;
          if (gsaResult.Yz_m.HasValue) result.stressMidYZ = gsaResult.Yz_m.Value;
          if (gsaResult.Zx_m.HasValue) result.stressMidZX = gsaResult.Zx_m.Value;
          if (gsaResult.Xx_t.HasValue) result.stressTopXX = gsaResult.Xx_t.Value;
          if (gsaResult.Yy_t.HasValue) result.stressTopYY = gsaResult.Yy_t.Value;
          if (gsaResult.Zz_t.HasValue) result.stressTopZZ = gsaResult.Zz_t.Value;
          if (gsaResult.Xy_t.HasValue) result.stressTopXY = gsaResult.Xy_t.Value;
          if (gsaResult.Yz_t.HasValue) result.stressTopYZ = gsaResult.Yz_t.Value;
          if (gsaResult.Zx_t.HasValue) result.stressTopZX = gsaResult.Zx_t.Value;

          speckleResults.Add(result);
        }
        return true;
      }
      return false;
    }

    public bool GsaElement3dResultToSpeckle(int gsaElementIndex, Element3D speckleElement, out List<Result3D> speckleResults)
    {
      //TO DO: update when 3D elements are supported
      speckleResults = null;
      return false;
    }

    //TODO: implement conversion code for result objects
    /* ResultGlobal
     */
    #endregion

    #region Constraints
    private ToSpeckleResult GsaRigidToSpeckle(GsaRecord nativeObject, GSALayer layer = GSALayer.Both)
    {
      var gsaRigid = (GsaRigid)nativeObject;
      var speckleRigid = new GSARigidConstraint()
      {
        name = gsaRigid.Name,
        constrainedNodes = gsaRigid.ConstrainedNodes.Select(i => GetNodeFromIndex(i)).ToList(),
        stages = gsaRigid.Stage.Select(i => GetStageFromIndex(i)).ToList(),
        type = gsaRigid.Type.ToSpeckle()
      };
      if (gsaRigid.Index.IsIndex())
      {
        speckleRigid.nativeId = gsaRigid.Index.Value;
        speckleRigid.applicationId = Instance.GsaModel.GetApplicationId<GsaRigid>(gsaRigid.Index.Value);
      }
      if (gsaRigid.PrimaryNode.IsIndex()) speckleRigid.primaryNode = GetNodeFromIndex(gsaRigid.PrimaryNode.Value);
      if (gsaRigid.Type == RigidConstraintType.Custom) speckleRigid.constraintCondition = GetRigidConstraint(gsaRigid.Link);
      return new ToSpeckleResult(speckleRigid);
    }

    private ToSpeckleResult GsaGenRestToSpeckle(GsaRecord nativeObject, GSALayer layer = GSALayer.Both)
    {
      var gsaGenRest = (GsaGenRest)nativeObject;
      var speckleGenRest = new GSAGeneralisedRestraint()
      {
        name = gsaGenRest.Name,
        restraint = GetRestraint(gsaGenRest),
        nodes = gsaGenRest.NodeIndices.Select(i => GetNodeFromIndex(i)).ToList(),
        stages = gsaGenRest.StageIndices.Select(i => GetStageFromIndex(i)).ToList(),
      };
      if (gsaGenRest.Index.IsIndex())
      {
        speckleGenRest.nativeId = gsaGenRest.Index.Value;
        speckleGenRest.applicationId = Instance.GsaModel.GetApplicationId<GsaGenRest>(gsaGenRest.Index.Value);
      }
      return new ToSpeckleResult(speckleGenRest);
    }
    #endregion

    #region Analysis Stage
    private ToSpeckleResult GsaStageToSpeckle(GsaRecord nativeObject, GSALayer layer = GSALayer.Both)
    {
      var gsaStage = (GsaAnalStage)nativeObject;
      var speckleStage = new GSAStage()
      {
        name = gsaStage.Name,
        colour = gsaStage.Colour.ToString(),
        elements = gsaStage.ElementIndices.Select(i => GetElementFromIndex(i)).ToList(),
        lockedElements = gsaStage.LockElementIndices.Select(i => GetElementFromIndex(i)).ToList(),
      };
      if (gsaStage.Index.IsIndex())
      {
        speckleStage.nativeId = gsaStage.Index.Value;
        speckleStage.applicationId = Instance.GsaModel.GetApplicationId<GsaAnalStage>(gsaStage.Index.Value);
      }
      if (gsaStage.Phi.IsPositive()) speckleStage.creepFactor = gsaStage.Phi.Value;
      if (gsaStage.Days.IsIndex()) speckleStage.stageTime = gsaStage.Days.Value;

      return new ToSpeckleResult(speckleStage);
    }
    #endregion

    #region Bridge
    private ToSpeckleResult GsaInfBeamToSpeckle(GsaRecord nativeObject, GSALayer layer = GSALayer.Both)
    {
      var gsaInfBeam = (GsaInfBeam)nativeObject;
      var speckleInfBeam = new GSAInfluenceBeam()
      {
        name = gsaInfBeam.Name,
        direction = gsaInfBeam.Direction.ToSpeckleLoad(),
        type = gsaInfBeam.Type.ToSpeckle(),
      };
      if (gsaInfBeam.Index.IsIndex())
      {
        speckleInfBeam.applicationId = Instance.GsaModel.GetApplicationId<GsaInfBeam>(gsaInfBeam.Index.Value);
        speckleInfBeam.nativeId = gsaInfBeam.Index.Value;
      }
      if (gsaInfBeam.Factor.HasValue) speckleInfBeam.factor = gsaInfBeam.Factor.Value;
      if (gsaInfBeam.Position.Value >= 0 && gsaInfBeam.Position.Value <= 1) speckleInfBeam.position = gsaInfBeam.Position.Value;
      if (gsaInfBeam.Element.IsIndex()) speckleInfBeam.element = GetElement1DFromIndex(gsaInfBeam.Element.Value);

      return new ToSpeckleResult(speckleInfBeam);
    }

    private ToSpeckleResult GsaInfNodeToSpeckle(GsaRecord nativeObject, GSALayer layer = GSALayer.Both)
    {
      var gsaInfNode = (GsaInfNode)nativeObject;
      var speckleInfBeam = new GSAInfluenceNode()
      {
        name = gsaInfNode.Name,
        direction = gsaInfNode.Direction.ToSpeckleLoad(),
        type = gsaInfNode.Type.ToSpeckle(),
        axis = GetAxis(gsaInfNode.AxisRefType, gsaInfNode.AxisIndex),
      };
      if (gsaInfNode.Index.IsIndex())
      {
        speckleInfBeam.applicationId = Instance.GsaModel.GetApplicationId<GsaInfNode>(gsaInfNode.Index.Value);
        speckleInfBeam.nativeId = gsaInfNode.Index.Value;
      }
      if (gsaInfNode.Factor.HasValue) speckleInfBeam.factor = gsaInfNode.Factor.Value;
      if (gsaInfNode.Node.IsIndex()) speckleInfBeam.node = GetNodeFromIndex(gsaInfNode.Node.Value);

      return new ToSpeckleResult(speckleInfBeam);
    }

    private ToSpeckleResult GsaAlignToSpeckle(GsaRecord nativeObject, GSALayer layer = GSALayer.Both)
    {
      var gsaAlign = (GsaAlign)nativeObject;
      var speckleAlign = new GSAAlignment()
      {
        name = gsaAlign.Name,
        chainage = gsaAlign.Chain,
        curvature = gsaAlign.Curv,
      };
      if (gsaAlign.Index.IsIndex())
      {
        speckleAlign.applicationId = Instance.GsaModel.GetApplicationId<GsaAlign>(gsaAlign.Index.Value);
        speckleAlign.nativeId = gsaAlign.Index.Value;
      }
      if (gsaAlign.GridSurfaceIndex.IsIndex()) speckleAlign.gridSurface = GetGridSurfaceFromIndex(gsaAlign.GridSurfaceIndex.Value);

      return new ToSpeckleResult(speckleAlign);
    }

    private ToSpeckleResult GsaPathToSpeckle(GsaRecord nativeObject, GSALayer layer = GSALayer.Both)
    {
      var gsaPath = (GsaPath)nativeObject;
      var specklePath = new GSAPath()
      {
        name = gsaPath.Name,
        type = gsaPath.Type.ToSpeckle(),
      };
      if (gsaPath.Index.IsIndex())
      {
        specklePath.applicationId = Instance.GsaModel.GetApplicationId<GsaPath>(gsaPath.Index.Value);
        specklePath.nativeId = gsaPath.Index.Value;
      }
      if (gsaPath.Alignment.IsIndex()) specklePath.alignment = GetAlignmentFromIndex(gsaPath.Alignment.Value);
      if (gsaPath.Group.IsIndex()) specklePath.group = gsaPath.Group.Value;
      if (gsaPath.Left.HasValue) specklePath.left = gsaPath.Left.Value;
      if (gsaPath.Right.HasValue) specklePath.right = gsaPath.Right.Value;
      if (gsaPath.Factor.HasValue) specklePath.factor = gsaPath.Factor.Value;
      if (gsaPath.NumMarkedLanes.HasValue && gsaPath.NumMarkedLanes > 0) specklePath.numMarkedLanes = gsaPath.NumMarkedLanes.Value;

      return new ToSpeckleResult(specklePath);
    }

    private ToSpeckleResult GsaUserVehicleToSpeckle(GsaRecord nativeObject, GSALayer layer = GSALayer.Both)
    {
      var gsaUserVehicle = (GsaUserVehicle)nativeObject;
      var speckleUserVehicle = new GSAUserVehicle()
      {
        name = gsaUserVehicle.Name,
        axlePositions = gsaUserVehicle.AxlePosition,
        axleOffsets = gsaUserVehicle.AxleOffset,
        axleLeft = gsaUserVehicle.AxleLeft,
        axleRight = gsaUserVehicle.AxleRight
      };
      if (gsaUserVehicle.Index.IsIndex())
      {
        speckleUserVehicle.applicationId = Instance.GsaModel.GetApplicationId<GsaUserVehicle>(gsaUserVehicle.Index.Value);
        speckleUserVehicle.nativeId = gsaUserVehicle.Index.Value;
      }
      if (gsaUserVehicle.Width.IsPositive()) speckleUserVehicle.width = gsaUserVehicle.Width.Value;

      return new ToSpeckleResult(speckleUserVehicle);
    }
    #endregion
    #endregion

    #region Helper
    #region ToSpeckle
    #region Geometry
    #region Node
    /// <summary>
    /// Conversion of node restraint from GSA to Speckle
    /// </summary>
    /// <param name="gsaNode">GsaNode object with the restraint definition to be converted</param>
    /// <returns></returns>
    private static Restraint GetRestraint(GsaNode gsaNode)
    {
      Restraint restraint;
      switch (gsaNode.NodeRestraint)
      {
        case NodeRestraint.Pin:
          restraint = new Restraint(RestraintType.Pinned);
          break;
        case NodeRestraint.Fix:
          restraint = new Restraint(RestraintType.Fixed);
          break;
        case NodeRestraint.Free:
          restraint = new Restraint(RestraintType.Free);
          break;
        case NodeRestraint.Custom:
          string code = GetCustomRestraintCode(gsaNode);
          restraint = new Restraint(code.ToString());
          break;
        default:
          restraint = new Restraint();
          break;
      }

      //restraint = UpdateSpringStiffness(restraint, gsaNode);

      return restraint;
    }

    /// <summary>
    /// Conversion of 1D element end releases from GSA to Speckle restraint
    /// </summary>
    /// <param name="release">Dictionary of release codes</param>
    /// <returns></returns>
    private static Restraint GetRestraint(Dictionary<GwaAxisDirection6, ReleaseCode> gsaRelease, List<double> gsaStiffness)
    {
      var code = new List<string>() { "F", "F", "F", "F", "F", "F" }; //Default
      int index = 0;
      if (gsaRelease != null)
      {
        foreach (var k in gsaRelease.Keys.ToList())
        {
          switch (k)
          {
            case GwaAxisDirection6.X:
              code[0] = gsaRelease[k].GetStringValue();
              break;
            case GwaAxisDirection6.Y:
              code[1] = gsaRelease[k].GetStringValue();
              break;
            case GwaAxisDirection6.Z:
              code[2] = gsaRelease[k].GetStringValue();
              break;
            case GwaAxisDirection6.XX:
              code[3] = gsaRelease[k].GetStringValue();
              break;
            case GwaAxisDirection6.YY:
              code[4] = gsaRelease[k].GetStringValue();
              break;
            case GwaAxisDirection6.ZZ:
              code[5] = gsaRelease[k].GetStringValue();
              break;
          }
        }
      }

      var speckleRelease = new Restraint(string.Join("", code));

      //Add stiffnesses
      if (code[0] == "K") speckleRelease.stiffnessX = gsaStiffness[index++];
      if (code[1] == "K") speckleRelease.stiffnessY = gsaStiffness[index++];
      if (code[2] == "K") speckleRelease.stiffnessZ = gsaStiffness[index++];
      if (code[3] == "K") speckleRelease.stiffnessXX = gsaStiffness[index++];
      if (code[4] == "K") speckleRelease.stiffnessYY = gsaStiffness[index++];
      if (code[5] == "K") speckleRelease.stiffnessZZ = gsaStiffness[index++];

      return speckleRelease;
    }

    private Restraint GetRestraint(GsaGenRest gsaGenRest)
    {
      var code = new List<string>() { "R", "R", "R", "R", "R", "R" }; //Default
      if (gsaGenRest.X == RestraintCondition.Constrained) code[0] = "F";
      if (gsaGenRest.Y == RestraintCondition.Constrained) code[1] = "F";
      if (gsaGenRest.Z == RestraintCondition.Constrained) code[2] = "F";
      if (gsaGenRest.XX == RestraintCondition.Constrained) code[3] = "F";
      if (gsaGenRest.YY == RestraintCondition.Constrained) code[4] = "F";
      if (gsaGenRest.ZZ == RestraintCondition.Constrained) code[5] = "F";
      return new Restraint(string.Join("", code));
    }

    /// <summary>
    /// Conversion of node constraint axis from GSA to Speckle
    /// </summary>
    /// <param name="gsaNode">GsaNode object with the constraint axis definition to be converted</param>
    /// <returns></returns>
    private Axis GetConstraintAxis(GsaNode gsaNode)
    {
      Axis speckleAxis;

      if (gsaNode.AxisRefType == NodeAxisRefType.XElevation)
      {
        speckleAxis = XElevationAxis();
      }
      else if (gsaNode.AxisRefType == NodeAxisRefType.YElevation)
      {
        speckleAxis = YElevationAxis();
      }
      else if (gsaNode.AxisRefType == NodeAxisRefType.Vertical)
      {
        speckleAxis = VerticalAxis();
      }
      else if (gsaNode.AxisRefType == NodeAxisRefType.Reference && gsaNode.AxisIndex.IsIndex())
      {
        speckleAxis = GetAxisFromIndex(gsaNode.AxisIndex.Value);
      }
      else
      {
        //Default global coordinates for case: Global or NotSet
        speckleAxis = GlobalAxis();
      }

      return speckleAxis;
    }

    /// <summary>
    /// Speckle structural schema restraint code
    /// </summary>
    /// <param name="gsaNode">GsaNode object with the restraint definition to be converted</param>
    /// <returns></returns>
    private static string GetCustomRestraintCode(GsaNode gsaNode)
    {
      var code = "RRRRRR".ToCharArray();
      for (var i = 0; i < gsaNode.Restraints.Count(); i++)
      {
        switch (gsaNode.Restraints[i])
        {
          case GwaAxisDirection6.X:
            code[0] = 'F';
            break;
          case GwaAxisDirection6.Y:
            code[1] = 'F';
            break;
          case GwaAxisDirection6.Z:
            code[2] = 'F';
            break;
          case GwaAxisDirection6.XX:
            code[3] = 'F';
            break;
          case GwaAxisDirection6.YY:
            code[4] = 'F';
            break;
          case GwaAxisDirection6.ZZ:
            code[5] = 'F';
            break;
        }
      }
      return code.ToString();
    }

    /// <summary>
    /// Add GSA spring stiffness definition to Speckle restraint definition.
    /// Deprecated: Using GSANode instead of Node, so spring stiffness no longer stored in Restraint
    /// </summary>
    /// <param name="restraint">Restraint speckle object to be updated</param>
    /// <param name="gsaNode">GsaNode object with spring stiffness definition</param>
    /// <returns></returns>
    private Restraint UpdateSpringStiffness(Restraint restraint, GsaNode gsaNode)
    {
      //Spring Stiffness
      if (gsaNode.SpringPropertyIndex.IsIndex())
      {
        var gsaRecord = Instance.GsaModel.GetNative<GsaPropSpr>(gsaNode.SpringPropertyIndex.Value);
        if (gsaRecord.GetType() != typeof(GsaPropSpr))
        {
          return restraint;
        }
        var gsaSpring = (GsaPropSpr)gsaRecord;

        //Update spring stiffness
        if (gsaSpring.Stiffnesses[GwaAxisDirection6.X] > 0)
        {
          var code = restraint.code.ToCharArray();
          code[0] = 'K';
          restraint.code = code.ToString();
          restraint.stiffnessX = gsaSpring.Stiffnesses[GwaAxisDirection6.X];
        }
        if (gsaSpring.Stiffnesses[GwaAxisDirection6.Y] > 0)
        {
          var code = restraint.code.ToCharArray();
          code[1] = 'K';
          restraint.code = code.ToString();
          restraint.stiffnessY = gsaSpring.Stiffnesses[GwaAxisDirection6.Y];
        }
        if (gsaSpring.Stiffnesses[GwaAxisDirection6.Z] > 0)
        {
          var code = restraint.code.ToCharArray();
          code[2] = 'K';
          restraint.code = code.ToString();
          restraint.stiffnessZ = gsaSpring.Stiffnesses[GwaAxisDirection6.Z];
        }
        if (gsaSpring.Stiffnesses[GwaAxisDirection6.XX] > 0)
        {
          var code = restraint.code.ToCharArray();
          code[3] = 'K';
          restraint.code = code.ToString();
          restraint.stiffnessXX = gsaSpring.Stiffnesses[GwaAxisDirection6.XX];
        }
        if (gsaSpring.Stiffnesses[GwaAxisDirection6.YY] > 0)
        {
          var code = restraint.code.ToCharArray();
          code[4] = 'K';
          restraint.code = code.ToString();
          restraint.stiffnessYY = gsaSpring.Stiffnesses[GwaAxisDirection6.YY];
        }
        if (gsaSpring.Stiffnesses[GwaAxisDirection6.ZZ] > 0)
        {
          var code = restraint.code.ToCharArray();
          code[5] = 'K';
          restraint.code = code.ToString();
          restraint.stiffnessZZ = gsaSpring.Stiffnesses[GwaAxisDirection6.ZZ];
        }
      }
      return restraint;
    }

    /// <summary>
    /// Get Speckle node object from GSA node index
    /// </summary>
    /// <param name="index">GSA node index</param>
    /// <returns></returns>
    private Node GetNodeFromIndex(int index)
    {
      return (Instance.GsaModel.Cache.GetSpeckleObjects<GsaNode, Node>(index, out var speckleObjects) && speckleObjects != null && speckleObjects.Count > 0)
        ? speckleObjects.First() : null;
    }
    #endregion

    #region Axis
    /// <summary>
    /// Get Speckle axis object from GSA axis index
    /// </summary>
    /// <param name="index">GSA axis index</param>
    /// <returns></returns>
    private Axis GetAxisFromIndex(int index)
    {
      /*
      var gsaAxis = Instance.GsaModel.GetNative<GsaAxis>(index);
      if (gsaAxis == null || gsaAxis.GetType() != typeof(GsaAxis))
      {
        return null;
      }
      return (Axis)GsaAxisToSpeckle((GsaAxis)gsaAxis).LayerAgnosticObjects.FirstOrDefault();
      */
      return (Instance.GsaModel.Cache.GetSpeckleObjects<GsaAxis, Axis>(index, out var speckleObjects) && speckleObjects != null && speckleObjects.Count > 0)
        ? speckleObjects.First() : null;
    }

    private Axis GetAxis(AxisRefType gsaAxisType, int? gsaAxisIndex)
    {
      Axis speckleAxis;

      if (gsaAxisType == AxisRefType.Local)
      {
        //TO DO: handle local reference axis case
        speckleAxis = null;
      }
      else if (gsaAxisType == AxisRefType.Reference && gsaAxisIndex.IsIndex())
      {
        speckleAxis = GetAxisFromIndex(gsaAxisIndex.Value);
      }
      else
      {
        //Default global coordinates for case: Global or NotSet
        speckleAxis = GlobalAxis();
      }

      return speckleAxis;
    }

    /// <summary>
    /// Speckle global axis definition
    /// </summary>
    /// <returns></returns>
    private static Axis GlobalAxis()
    {
      //Default global coordinates for case: Global or NotSet
      var origin = new Point(0, 0, 0);
      var xdir = new Vector(1, 0, 0);
      var ydir = new Vector(0, 1, 0);
      var normal = new Vector(0, 0, 1);

      var axis = new Axis()
      {
        name = "global",
        axisType = AxisType.Cartesian,
        definition = new Plane(origin, normal, xdir, ydir)
      };

      return axis;
    }

    private static Axis XElevationAxis()
    {
      return new Axis()
      {
        name = "xElevation",
        axisType = AxisType.Cartesian,
        definition = new Plane()
        {
          origin = new Point(0, 0, 0),
          normal = new Vector(-1, 0, 0),
          xdir = new Vector(0, -1, 0),
          ydir = new Vector(0, 0, 1)
        }
      };
    }

    private static Axis YElevationAxis()
    {
      return new Axis()
      {
        name = "yElevation",
        axisType = AxisType.Cartesian,
        definition = new Plane()
        {
          origin = new Point(0, 0, 0),
          normal = new Vector(0, -1, 0),
          xdir = new Vector(1, 0, 0),
          ydir = new Vector(0, 0, 1)
        }
      };
    }

    private static Axis VerticalAxis()
    {
      
      return new Axis()
      {
        name = "Vertical",
        axisType = AxisType.Cartesian,
        definition = new Plane()
        {
          origin = new Point(0, 0, 0),
          xdir = new Vector(0, 0, 1),
          ydir = new Vector(1, 0, 0),
          normal = new Vector(0, 1, 0),
        }
      };
    }
    #endregion

    #region Elements
    private Base GetElementFromIndex(int index)
    {
      var gsaEl = (GsaEl)Instance.GsaModel.Cache.GetNative<GsaEl>(index);
      if (gsaEl.Is1dElement())
      {
        return GetElement1DFromIndex(index);
      }
      if (gsaEl.Is2dElement())
      {
        return GetElement2DFromIndex(index);
      }
      else
      {
        return null;
      }
    }

    /// <summary>
    /// Get Speckle Element2D object from GSA element index
    /// </summary>
    /// <param name="index">GSA element index</param>
    /// <returns></returns>
    private Element2D GetElement2DFromIndex(int index)
    {
      return (Instance.GsaModel.Cache.GetSpeckleObjects<GsaEl, Element2D>(index, out var speckleObjects)) ? speckleObjects.First() : null;
    }

    /// <summary>
    /// Get Speckle Element1D object from GSA element index
    /// </summary>
    /// <param name="index">GSA element index</param>
    /// <returns></returns>
    private Element1D GetElement1DFromIndex(int index)
    {
      return (Instance.GsaModel.Cache.GetSpeckleObjects<GsaEl, Element1D>(index, out var speckleObjects)) ? speckleObjects.First() : null;
    }

    /// <summary>
    /// Get the local axis for a 1D element
    /// </summary>
    /// <param name="n1">end1Node</param>
    /// <param name="n2">end2Node</param>
    /// <param name="n3">orientationNode</param>
    /// <param name="angle">orientationAngle in radians</param>
    /// <returns></returns>
    private Plane GetLocalAxis(Node n1, Node n2, Node n3, double angle)
    {
      Vector xdir, ydir, normal;
      Point p1, p2, p3, origin;
      normal = new Vector(0, 0, 1); //default

      p1 = n1.basePoint;
      p2 = n2.basePoint;
      origin = new Point(p1.x, p1.y, p1.z);
      xdir = (new Vector(p2.x - p1.x, p2.y - p1.y, p2.z - p1.z)).UnitVector();

      //Update normal if orientation node exists
      if (n3 != null)
      {
        p3 = n3.basePoint;
        normal = (new Vector(p3.x - p1.x, p3.y - p1.y, p3.z - p1.z)).UnitVector();
      }
      else if (xdir.DotProduct(normal) > 0.99 || xdir.DotProduct(normal) < -0.99) //Vertical element, TODO: what tolerance to use?
      {
        ydir = new Vector(0, 1, 0);
        normal = (xdir * ydir).UnitVector();
      }

      //Apply rotation angle
      if (angle != 0) normal = normal.Rotate(xdir, angle).UnitVector();

      //xdir and normal define a plane:
      // *ensure normal is perpendicular to xdir on that plane
      // *ensure ydir is normal to the plane
      ydir = -(xdir * normal).UnitVector();
      normal = (xdir * ydir).UnitVector();

      return new Plane(origin, normal, xdir, ydir);
    }

    private Mesh DisplayMesh2d(List<int> gsaNodeIndicies)
    {
      //TO DO: check if this actually creates a real mesh
      var vertices = new List<double>();
      var faces = new List<int[]>();

      var topology = gsaNodeIndicies.Select(i => GetNodeFromIndex(i)).ToList();

      foreach (var node in topology)
      {
        vertices.Add(node.basePoint.x);
        vertices.Add(node.basePoint.y);
        vertices.Add(node.basePoint.z);
      }

      if (gsaNodeIndicies.Count == 4)
      {
        faces.Add(new int[] { 1, 1, 2, 3, 4 });
      }
      else if (gsaNodeIndicies.Count == 3)
      {
        faces.Add(new int[] { 0, 1, 2, 3 });
      }

      var speckleMesh = new Mesh(vertices.ToArray(), faces.SelectMany(o => o).ToArray());

      return speckleMesh;
    }
    #endregion

    #region Member
    private Base GetMemberFromIndex(int index)
    {
      var gsaMemb = (GsaMemb)Instance.GsaModel.Cache.GetNative<GsaMemb>(index);
      if (gsaMemb.Is1dMember())
      {
        return (Instance.GsaModel.Cache.GetSpeckleObjects<GsaMemb, GSAMember1D>(index, out var speckleObjects)) ? speckleObjects.First() : null;
      }
      if (gsaMemb.Is2dMember())
      {
        return (Instance.GsaModel.Cache.GetSpeckleObjects<GsaMemb, GSAMember2D>(index, out var speckleObjects)) ? speckleObjects.First() : null;
      }
      else
      {
        return null;
      }
    }
    #endregion

    #region Grids
    private Axis GetGridPlaneAxis(GridPlaneAxisRefType gsaAxisType, int? gsaAxisIndex)
    {
      Axis speckleAxis;

      if (gsaAxisType == GridPlaneAxisRefType.XElevation)
      {
        speckleAxis = XElevationAxis();
      }
      else if (gsaAxisType == GridPlaneAxisRefType.YElevation)
      {
        speckleAxis = YElevationAxis();
      }
      else if (gsaAxisType == GridPlaneAxisRefType.Reference && gsaAxisIndex.IsIndex())
      {
        speckleAxis = GetAxisFromIndex(gsaAxisIndex.Value);
      }
      else
      {
        //Default global coordinates for case: Global or NotSet
        speckleAxis = GlobalAxis();
      }

      return speckleAxis;
    }

    private GSAGridPlane GetGridPlane(GridPlaneAxisRefType gsaAxisType, int? gsaAxisIndex)
    {
      var speckleGridPlane = new GSAGridPlane()
      {
        name = "",
        axis = new Axis(),
        elevation = 0,
        toleranceBelow = null,
        toleranceAbove = null,
      };

      if (gsaAxisType == GridPlaneAxisRefType.XElevation)
      {
        speckleGridPlane.name = "X Elevation Grid";
        speckleGridPlane.axis = XElevationAxis();
      }
      else if (gsaAxisType == GridPlaneAxisRefType.YElevation)
      {
        speckleGridPlane.name = "Y Elevation Grid";
        speckleGridPlane.axis = YElevationAxis();
      }
      else if (gsaAxisType == GridPlaneAxisRefType.Reference && gsaAxisIndex.IsIndex())
      {
        speckleGridPlane = GetGridPlaneFromIndex(gsaAxisIndex.Value);
      }
      else
      {
        speckleGridPlane.name = "Global Grid";
        speckleGridPlane.axis = GlobalAxis();
      }

      return speckleGridPlane;
    }

    /// <summary>
    /// Get Speckle grid plane object from GSA grid plane index
    /// </summary>
    /// <param name="index">GSA axis index</param>
    /// <returns></returns>
    private GSAGridPlane GetGridPlaneFromIndex(int index)
    {
      return (Instance.GsaModel.Cache.GetSpeckleObjects<GsaGridPlane, GSAGridPlane>(index, out var speckleObjects) && speckleObjects != null && speckleObjects.Count > 0)
        ? speckleObjects.First() : null;
    }

    private double? GetStoreyTolerance(double? gsaStoreyTolerance, bool gsaStoreyToleranceAuto, GridPlaneType gsaType)
    {
      double? speckleStoreyTolerance = null; //default

      if (gsaType == GridPlaneType.Storey)
      {
        if (gsaStoreyToleranceAuto)
        {
          speckleStoreyTolerance = null;
        }
        else if (gsaStoreyTolerance.HasValue)
        {
          speckleStoreyTolerance = gsaStoreyTolerance.Value;
        }
      }
      return speckleStoreyTolerance;
    }

    private GSAGridSurface GetGridSurfaceFromIndex(int index)
    {
      return (Instance.GsaModel.Cache.GetSpeckleObjects<GsaGridSurface, GSAGridSurface>(index, out var speckleObjects)) ? speckleObjects.First() : null;
    }

    private Line GetLine(GsaGridLine gsaGridLine)
    {
      var speckleLine = new Line();
      if (gsaGridLine.XCoordinate.HasValue && gsaGridLine.YCoordinate.HasValue && gsaGridLine.Length.HasValue && gsaGridLine.Theta1.HasValue)
      {
        speckleLine.start = new Point(gsaGridLine.XCoordinate.Value, gsaGridLine.YCoordinate.Value, 0);
        speckleLine.end = new Point(gsaGridLine.XCoordinate.Value + gsaGridLine.Length.Value * Math.Cos(gsaGridLine.Theta1.Value.Radians()),
          gsaGridLine.YCoordinate.Value + gsaGridLine.Length.Value * Math.Sin(gsaGridLine.Theta1.Value.Radians()), 0);
      }
      return speckleLine;
    }

    private Arc GetArc(GsaGridLine gsaGridLine)
    {
      var speckleArc = new Arc();
      if (gsaGridLine.XCoordinate.HasValue && gsaGridLine.YCoordinate.HasValue && gsaGridLine.Length.HasValue && gsaGridLine.Theta1.HasValue && gsaGridLine.Theta1.HasValue)
      {
        speckleArc.radius = gsaGridLine.Length.Value;
        speckleArc.startAngle = gsaGridLine.Theta1.Value.Radians();
        speckleArc.endAngle = gsaGridLine.Theta2.Value.Radians();
        speckleArc.startPoint = new Point(gsaGridLine.XCoordinate.Value + gsaGridLine.Length.Value * Math.Cos(gsaGridLine.Theta1.Value.Radians()),
          gsaGridLine.YCoordinate.Value + gsaGridLine.Length.Value * Math.Sin(gsaGridLine.Theta1.Value.Radians()), 0);
        speckleArc.endPoint = new Point(gsaGridLine.XCoordinate.Value + gsaGridLine.Length.Value * Math.Cos(gsaGridLine.Theta2.Value.Radians()),
          gsaGridLine.YCoordinate.Value + gsaGridLine.Length.Value * Math.Sin(gsaGridLine.Theta2.Value.Radians()), 0);
        //speckleArc.angleRadians;
        speckleArc.plane = GlobalAxis().definition;
      }
      return speckleArc;
    }
    #endregion

    #region Polyline
    private Polyline GetPolyline(LoadLineOption gsaType, string gsaPolygon, int? gsaPolygonIndex)
    {
      Polyline specklePolyline;
      if (gsaType == LoadLineOption.Polygon)
      {
        specklePolyline = GetPolygonFromString(gsaPolygon);
      }
      else if (gsaType == LoadLineOption.PolyRef && gsaPolygonIndex.HasValue)
      {
        specklePolyline = GetPolygonFromIndex(gsaPolygonIndex.Value);
      }
      else
      {
        specklePolyline = null;
      }
      return specklePolyline;
    }

    private Polyline GetPolyline(LoadAreaOption gsaType, string gsaPolygon, int? gsaPolygonIndex)
    {
      Polyline specklePolyline;
      if (gsaType == LoadAreaOption.Polygon)
      {
        specklePolyline = GetPolygonFromString(gsaPolygon);
      }
      else if (gsaType == LoadAreaOption.PolyRef && gsaPolygonIndex.HasValue)
      {
        specklePolyline = GetPolygonFromIndex(gsaPolygonIndex.Value);
      }
      else
      {
        specklePolyline = null;
      }
      return specklePolyline;
    }

    private GSAPolyline GetPolygonFromIndex(int index)
    {
      return (Instance.GsaModel.Cache.GetSpeckleObjects<GsaPolyline, GSAPolyline>(index, out var speckleObjects)) ? speckleObjects.First() : null;
    }

    private Polyline GetPolygonFromString(string gsaPolygon)
    {
      //process gsaPolygon string
      //e.g. (0,0) (1,0) (1,1) (0,1)(m)
      var specklePolyline = new Polyline()
      {
        value = new List<double>()
      };
      foreach (var item in gsaPolygon.Split(' '))
      {
        var point = item.Split('(', ')')[1];
        if (point.Split(',').Count() == 2) point += ",0"; //ensure each point has x,y,z value
        specklePolyline.value.AddRange(point.Split(',').Select(p => p.ToDouble()).ToList());
      }
      return specklePolyline;
    }
    #endregion

    #region Assemblies
    private bool GetAssemblyEntites(GsaAssembly gsaAssembly, out List<Base> speckleEntities)
    {
      switch (gsaAssembly.Type)
      {
        case GSAEntity.ELEMENT:
          speckleEntities = gsaAssembly.ElementIndices.Select(i => GetElementFromIndex(i)).ToList();
          break;
        case GSAEntity.MEMBER:
          speckleEntities = gsaAssembly.MemberIndices.Select(i => GetMemberFromIndex(i)).ToList();
          break;
        default:
          speckleEntities = null;
          return false;
      }
      return true;
    }

    private bool GetAssemblyPoints(GsaAssembly gsaAssembly, out List<double> points)
    {
      //Points
      if (gsaAssembly.PointDefn == PointDefinition.Points && gsaAssembly.NumberOfPoints.IsIndex())
      {
        points = new List<double>() { gsaAssembly.NumberOfPoints.Value };
      }
      else if (gsaAssembly.PointDefn == PointDefinition.Spacing && gsaAssembly.Spacing.IsPositive())
      {
        points = new List<double>() { gsaAssembly.Spacing.Value };
      }
      else if (gsaAssembly.PointDefn == PointDefinition.Storey && gsaAssembly.StoreyIndices != null && gsaAssembly.StoreyIndices.Count > 0)
      {
        points = gsaAssembly.StoreyIndices.Select(i=>i.ToDouble()).ToList();
      }
      else if (gsaAssembly.PointDefn == PointDefinition.Explicit && gsaAssembly.ExplicitPositions != null && gsaAssembly.ExplicitPositions.Count > 0)
      {
        points = gsaAssembly.ExplicitPositions;
      }
      else
      {
        points = null;
        return false;
      }
      return true;
    }
    
  #endregion
  #endregion

  #region Loading
  private GSALoadCase GetLoadCaseFromIndex(int index)
    {
      return (Instance.GsaModel.Cache.GetSpeckleObjects<GsaLoadCase, GSALoadCase>(index, out var speckleObjects)) ? speckleObjects.First() : null;
    }

    private GSAAnalysisCase GetAnalysisCaseFromIndex(int index)
    {
      return (Instance.GsaModel.Cache.GetSpeckleObjects<GsaAnal, GSAAnalysisCase>(index, out var speckleObjects)) ? speckleObjects.First() : null;
    }

    private GSATask GetTaskFromIndex(int index)
    {
      return null;

      //TO DO: when TASK is included in interim schema
      //return (Instance.GsaModel.Cache.GetSpeckleObjects<GsaTask, GSATask>(index, out var speckleObjects)) ? speckleObjects.First() : null;
    }

    private bool GetLoadBeamPositions(GsaLoadBeam gsaLoadBeam, out List<double> positions)
    {
      positions = new List<double>();
      var type = gsaLoadBeam.GetType();
      if (type == typeof(GsaLoadBeamPoint))
      {
        positions.Add(((GsaLoadBeamPoint)gsaLoadBeam).Position);
        return true;
      }
      else if (type == typeof(GsaLoadBeamPatch) || type == typeof(GsaLoadBeamPatchTrilin))
      {
        var lb = (GsaLoadBeamPatchTrilin)gsaLoadBeam;
        positions.Add(lb.Position1);
        positions.Add(lb.Position2Percent);
        return true;
      }
      return false;
    }

    private bool GetLoadBeamValues(GsaLoadBeam gsaLoadBeam, out List<double> values)
    {
      values = new List<double>();
      double? v;
      var type = gsaLoadBeam.GetType();
      if (type == typeof(GsaLoadBeamPoint))
      {
        v = ((GsaLoadBeamPoint)gsaLoadBeam).Load;
        if (v.HasValue)
        {
          values.Add(v.Value);
          return true;
        }
      }
      else if (type == typeof(GsaLoadBeamUdl))
      {
        v = ((GsaLoadBeamUdl)gsaLoadBeam).Load;
        if (v.HasValue)
        {
          values.Add(v.Value);
          return true;
        }
      }
      else if (type == typeof(GsaLoadBeamLine))
      {
        var lb = (GsaLoadBeamLine)gsaLoadBeam;
        if (lb.Load1.HasValue && lb.Load2.HasValue)
        {
          values.Add(lb.Load1.Value);
          values.Add(lb.Load2.Value);
          return true;
        }
      }
      else if (type == typeof(GsaLoadBeamPatch) || type == typeof(GsaLoadBeamPatchTrilin))
      {
        var lb = (GsaLoadBeamPatchTrilin)gsaLoadBeam;
        if (lb.Load1.HasValue && lb.Load2.HasValue)
        {
          values.Add(lb.Load1.Value);
          values.Add(lb.Load2.Value);
          return true;
        }
      }
      return false;
    }

    private Vector GetGravityFactors(GsaLoadGravity gsaLoadGravity)
    {
      var speckleGravityFactors = new Vector(0, 0, 0);
      if (gsaLoadGravity.X.HasValue) speckleGravityFactors.x = gsaLoadGravity.X.Value;
      if (gsaLoadGravity.Y.HasValue) speckleGravityFactors.y = gsaLoadGravity.Y.Value;
      if (gsaLoadGravity.Z.HasValue) speckleGravityFactors.z = gsaLoadGravity.Z.Value;

      return speckleGravityFactors;
    }

    private bool GetLoadCombinationFactors(string desc, out List<Base> loadCases, out List<double> loadFactors)
    {
      loadFactors = new List<double>();
      loadCases = new List<Base>();
      var gsaCaseFactors = ParseLoadDescription(desc);

      foreach (var key in gsaCaseFactors.Keys)
      {
        var gsaIndex = Convert.ToInt32(key.Substring(1));
        if (key[0] == 'A')
        {
          loadCases.Add(GetAnalysisCaseFromIndex(gsaIndex));
          loadFactors.Add(gsaCaseFactors[key]);
        }
        else if (key[0] == 'C')
        {
          loadCases.Add(GetLoadCombinationFromIndex(gsaIndex));
          loadFactors.Add(gsaCaseFactors[key]);
        }
      }

      return true;
    }

    private bool GetAnalysisCaseFactors(string desc, out List<LoadCase> loadCases, out List<double> loadFactors)
    {
      loadFactors = new List<double>();
      loadCases = new List<LoadCase>();
      var gsaCaseFactors = ParseLoadDescription(desc);

      foreach (var key in gsaCaseFactors.Keys)
      {
        var gsaIndex = Convert.ToInt32(key.Substring(1));
        if (key[0] == 'L')
        {
          loadCases.Add(GetLoadCaseFromIndex(gsaIndex));
          loadFactors.Add(gsaCaseFactors[key]);
        }
      }

      return true;
    }

    private GSALoadCombination GetLoadCombinationFromIndex(int index)
    {
      return (Instance.GsaModel.Cache.GetSpeckleObjects<GsaCombination, GSALoadCombination>(index, out var speckleObjects)) ? speckleObjects.First() : null;
    }

    /// <summary>
    /// Seperates the load description into a dictionary of the case/task/combo identifier and their factors.
    /// </summary>
    /// <param name="list">Load description.</param>
    /// <param name="currentMultiplier">Factor to multiply the entire list by.</param>
    /// <returns></returns>
    public static Dictionary<string, double> ParseLoadDescription(string list, double currentMultiplier = 1)
    {
      var ret = new Dictionary<string, double>();

      list = list.Replace(" ", "");

      double multiplier = 1;
      var negative = false;

      for (var pos = 0; pos < list.Count(); pos++)
      {
        var currChar = list[pos];

        if (currChar >= '0' && currChar <= '9') //multiplier
        {
          var mult = "";
          mult += currChar.ToString();

          pos++;
          while (pos < list.Count() && ((list[pos] >= '0' && list[pos] <= '9') || list[pos] == '.'))
            mult += list[pos++].ToString();
          pos--;

          multiplier = mult.ToDouble();
        }
        else if (currChar >= 'A' && currChar <= 'Z') //GSA load case or load combination identifier
        {
          var loadDesc = "";
          loadDesc += currChar.ToString();

          pos++;
          while (pos < list.Count() && list[pos] >= '0' && list[pos] <= '9')
            loadDesc += list[pos++].ToString();
          pos--;

          var actualFactor = multiplier == 0 ? 1 : multiplier;
          actualFactor *= currentMultiplier;
          actualFactor = negative ? -1 * actualFactor : actualFactor;

          ret.Add(loadDesc, actualFactor);

          multiplier = 0;
          negative = false;
        }
        else if (currChar == '-') //negative operator
          negative = !negative;
        else if (currChar == 't') //to operator (i.e. add all load cases between the first and second identifier
        {
          if (list[++pos] == 'o')
          {
            var prevDesc = ret.Last();

            var type = prevDesc.Key[0].ToString();
            var start = Convert.ToInt32(prevDesc.Key.Substring(1)) + 1;

            var endDesc = "";

            pos++;
            pos++;
            while (pos < list.Count() && list[pos] >= '0' && list[pos] <= '9')
              endDesc += list[pos++].ToString();
            pos--;

            var end = Convert.ToInt32(endDesc);

            for (var i = start; i <= end; i++)
              ret.Add(type + i.ToString(), prevDesc.Value);
          }
        }
        else if (currChar == '(') //process part inside brackets
        {
          var actualFactor = multiplier == 0 ? 1 : multiplier;
          actualFactor *= currentMultiplier;
          actualFactor = negative ? -1 * actualFactor : actualFactor;

          ret.AddRange(ParseLoadDescription(string.Join("", list.Skip(pos + 1)), actualFactor));

          pos++;
          while (pos < list.Count() && list[pos] != ')')
            pos++;

          multiplier = 0;
          negative = false;
        }
        else if (currChar == ')')
          return ret;
      }

      return ret;
    }

    private CombinationType GetCombinationType(string desc)
    {
      //TO DO: Use desc to deside combination type
      return CombinationType.LinearAdd;
    }
    #endregion

    #region Materials
    //Some material properties are stored in either GsaMat or GsaMatAnal

    /// <summary>
    /// Return true if either v1 or v2 has a value.
    /// </summary>
    /// <param name="v1">value to take precidence if not null</param>
    /// <param name="v2">value to take if v1 is null</param>
    /// <param name="v">returned value</param>
    /// <returns></returns>
    public bool Choose(double? v1, double? v2, out double v)
    {
      if (v1.HasValue)
      {
        v = v1.Value;
        return true;
      }
      else if (v2.HasValue)
      {
        v = v2.Value;
        return true;
      }
      else
      {
        v = 0;
        return false;
      }
    }

    /// <summary>
    /// Get Speckle material object from GSA material index
    /// </summary>
    /// <param name="index">GSA material index</param>
    /// <param name="type">GSA material type</param>
    /// <returns></returns>
    private Material GetMaterialFromIndex(int index, Property2dMaterialType type)
    {
      //Initialise
      GsaRecord gsaMat;
      Material speckleMaterial = null;

      //Get material based on type and gsa index
      //Convert gsa material to speckle material
      if (type == Property2dMaterialType.Steel)
      {
        //gsaMat = Instance.GsaModel.GetNative<GsaMatSteel>(index);
        //if (gsaMat != null) speckleMaterial = GsaMaterialSteelToSpeckle((GsaMatSteel)gsaMat);
        return (Instance.GsaModel.Cache.GetSpeckleObjects<GsaMatSteel, Steel>(index, out var speckleObjects) && speckleObjects != null && speckleObjects.Count > 0)
        ? speckleObjects.First() : null;
      }
      else if (type == Property2dMaterialType.Concrete)
      {
        //gsaMat = Instance.GsaModel.GetNative<GsaMatConcrete>(index);
        //if (gsaMat != null) speckleMaterial = GsaMaterialConcreteToSpeckle((GsaMatConcrete)gsaMat);
        return (Instance.GsaModel.Cache.GetSpeckleObjects<GsaMatConcrete, Concrete>(index, out var speckleObjects) && speckleObjects != null && speckleObjects.Count > 0)
        ? speckleObjects.First() : null;
      }

      return speckleMaterial;
    }

    /// <summary>
    /// Get Speckle material object from GSA material index
    /// </summary>
    /// <param name="index">GSA material index</param>
    /// <param name="type">GSA material type</param>
    /// <returns></returns>
    private Material GetMaterialFromIndex(int index, Section1dMaterialType type)
    {
      //Initialise
      GsaRecord gsaMat;
      Material speckleMaterial = null;

      //Get material based on type and gsa index
      //Convert gsa material to speckle material
      if (type == Section1dMaterialType.STEEL)
      {
        //gsaMat = Instance.GsaModel.GetNative<GsaMatSteel>(index);
        //if (gsaMat != null) speckleMaterial = GsaMaterialSteelToSpeckle((GsaMatSteel)gsaMat);
        return (Instance.GsaModel.Cache.GetSpeckleObjects<GsaMatSteel, Steel>(index, out var speckleObjects) && speckleObjects != null && speckleObjects.Count > 0)
        ? speckleObjects.First() : null;
      }
      else if (type == Section1dMaterialType.CONCRETE)
      {
        //gsaMat = Instance.GsaModel.GetNative<GsaMatConcrete>(index);
        //if (gsaMat != null) speckleMaterial = GsaMaterialConcreteToSpeckle((GsaMatConcrete)gsaMat);
        return (Instance.GsaModel.Cache.GetSpeckleObjects<GsaMatConcrete, Concrete>(index, out var speckleObjects) && speckleObjects != null && speckleObjects.Count > 0)
        ? speckleObjects.First() : null;
      }

      return speckleMaterial;
    }
    #endregion

    #region Properties
    #region Spring
    /// <summary>
    /// Get Speckle PropertySpring object from GSA property spring index
    /// </summary>
    /// <param name="index">GSA property spring index</param>
    /// <returns></returns>
    private PropertySpring GetPropertySpringFromIndex(int index)
    {
      /*
      PropertySpring specklePropertySpring = null;
      var gsaPropSpr = Instance.GsaModel.GetNative<GsaPropSpr>(index);
      if (gsaPropSpr != null) specklePropertySpring = GsaPropertySpringToSpeckle((GsaPropSpr)gsaPropSpr);

      return specklePropertySpring;
      */
      return (Instance.GsaModel.Cache.GetSpeckleObjects<GsaPropSpr, PropertySpring>(index, out var speckleObjects) && speckleObjects != null && speckleObjects.Count > 0)
        ? speckleObjects.First() : null;
    }

    /// <summary>
    /// Set properties for an axial spring
    /// </summary>
    /// <param name="gsaPropSpr">GsaPropSpr object containing the spring definition</param>
    /// <param name="specklePropertySpring">Speckle PropertySPring object to be updated</param>
    /// <returns></returns>
    private bool SetProprtySpringAxial(GsaPropSpr gsaPropSpr, PropertySpring specklePropertySpring)
    {
      specklePropertySpring.springType = PropertyTypeSpring.Axial;
      specklePropertySpring.stiffnessX = gsaPropSpr.Stiffnesses[GwaAxisDirection6.X];
      return true;
    }

    /// <summary>
    /// Set properties for a torsional spring
    /// </summary>
    /// <param name="gsaPropSpr">GsaPropSpr object containing the spring definition</param>
    /// <param name="specklePropertySpring">Speckle PropertySPring object to be updated</param>
    /// <returns></returns>
    private bool SetPropertySpringTorsional(GsaPropSpr gsaPropSpr, PropertySpring specklePropertySpring)
    {
      specklePropertySpring.springType = PropertyTypeSpring.Torsional;
      specklePropertySpring.stiffnessXX = gsaPropSpr.Stiffnesses[GwaAxisDirection6.XX];
      return true;
    }

    /// <summary>
    /// Set properties for a compression only spring
    /// </summary>
    /// <param name="gsaPropSpr">GsaPropSpr object containing the spring definition</param>
    /// <param name="specklePropertySpring">Speckle PropertySPring object to be updated</param>
    /// <returns></returns>
    private bool SetProprtySpringCompression(GsaPropSpr gsaPropSpr, PropertySpring specklePropertySpring)
    {
      specklePropertySpring.springType = PropertyTypeSpring.CompressionOnly;
      specklePropertySpring.stiffnessX = gsaPropSpr.Stiffnesses[GwaAxisDirection6.X];
      return true;
    }

    /// <summary>
    /// Set properties for a tension only spring
    /// </summary>
    /// <param name="gsaPropSpr">GsaPropSpr object containing the spring definition</param>
    /// <param name="specklePropertySpring">Speckle PropertySPring object to be updated</param>
    /// <returns></returns>
    private bool SetProprtySpringTension(GsaPropSpr gsaPropSpr, PropertySpring specklePropertySpring)
    {
      specklePropertySpring.springType = PropertyTypeSpring.TensionOnly;
      specklePropertySpring.stiffnessX = gsaPropSpr.Stiffnesses[GwaAxisDirection6.X];
      return true;
    }

    /// <summary>
    /// Set properties for a lockup spring
    /// </summary>
    /// <param name="gsaPropSpr">GsaPropSpr object containing the spring definition</param>
    /// <param name="specklePropertySpring">Speckle PropertySPring object to be updated</param>
    /// <returns></returns>
    private bool SetProprtySpringLockup(GsaPropSpr gsaPropSpr, PropertySpring specklePropertySpring)
    {
      //Also for LOCKUP, there are positive and negative parameters, but these aren't supported yet
      specklePropertySpring.springType = PropertyTypeSpring.LockUp;
      specklePropertySpring.stiffnessX = gsaPropSpr.Stiffnesses[GwaAxisDirection6.X];
      specklePropertySpring.positiveLockup = 0;
      specklePropertySpring.negativeLockup = 0;
      return true;
    }

    /// <summary>
    /// Set properties for a gap spring
    /// </summary>
    /// <param name="gsaPropSpr">GsaPropSpr object containing the spring definition</param>
    /// <param name="specklePropertySpring">Speckle PropertySPring object to be updated</param>
    /// <returns></returns>
    private bool SetProprtySpringGap(GsaPropSpr gsaPropSpr, PropertySpring specklePropertySpring)
    {
      specklePropertySpring.springType = PropertyTypeSpring.Gap;
      specklePropertySpring.stiffnessX = gsaPropSpr.Stiffnesses[GwaAxisDirection6.X];
      return true;
    }

    /// <summary>
    /// Set properties for a friction spring
    /// </summary>
    /// <param name="gsaPropSpr">GsaPropSpr object containing the spring definition</param>
    /// <param name="specklePropertySpring">Speckle PropertySPring object to be updated</param>
    /// <returns></returns>
    private bool SetProprtySpringFriction(GsaPropSpr gsaPropSpr, PropertySpring specklePropertySpring)
    {
      specklePropertySpring.springType = PropertyTypeSpring.Friction;
      specklePropertySpring.stiffnessX = gsaPropSpr.Stiffnesses[GwaAxisDirection6.X];
      specklePropertySpring.stiffnessY = gsaPropSpr.Stiffnesses[GwaAxisDirection6.Y];
      specklePropertySpring.stiffnessZ = gsaPropSpr.Stiffnesses[GwaAxisDirection6.Z];
      specklePropertySpring.frictionCoefficient = gsaPropSpr.FrictionCoeff.Value;
      return true;
    }

    /// <summary>
    /// Set properties for a general spring
    /// </summary>
    /// <param name="gsaPropSpr">GsaPropSpr object containing the spring definition</param>
    /// <param name="specklePropertySpring">Speckle PropertySPring object to be updated</param>
    /// <returns></returns>
    private bool SetProprtySpringGeneral(GsaPropSpr gsaPropSpr, PropertySpring specklePropertySpring)
    {
      specklePropertySpring.springType = PropertyTypeSpring.General;
      specklePropertySpring.stiffnessX = gsaPropSpr.Stiffnesses[GwaAxisDirection6.X];
      specklePropertySpring.springCurveX = 0;
      specklePropertySpring.stiffnessY = gsaPropSpr.Stiffnesses[GwaAxisDirection6.Y];
      specklePropertySpring.springCurveY = 0;
      specklePropertySpring.stiffnessZ = gsaPropSpr.Stiffnesses[GwaAxisDirection6.Z];
      specklePropertySpring.springCurveZ = 0;
      specklePropertySpring.stiffnessXX = gsaPropSpr.Stiffnesses[GwaAxisDirection6.XX];
      specklePropertySpring.springCurveXX = 0;
      specklePropertySpring.stiffnessYY = gsaPropSpr.Stiffnesses[GwaAxisDirection6.YY];
      specklePropertySpring.springCurveYY = 0;
      specklePropertySpring.stiffnessZZ = gsaPropSpr.Stiffnesses[GwaAxisDirection6.ZZ];
      specklePropertySpring.springCurveZZ = 0;
      return true;
    }
    #endregion

    #region Mass
    /// <summary>
    /// Get Speckle PropertyMass object from GSA property mass index
    /// </summary>
    /// <param name="index">GSA property mass index</param>
    /// <returns></returns>
    private PropertyMass GetPropertyMassFromIndex(int index)
    {
      /*
      PropertyMass specklePropertyMass = null;
      var gsaPropMass = Instance.GsaModel.GetNative<GsaPropMass>(index);
      if (gsaPropMass != null) specklePropertyMass = GsaPropertyMassToSpeckle((GsaPropMass)gsaPropMass);

      return specklePropertyMass;
      */
      return (Instance.GsaModel.Cache.GetSpeckleObjects<GsaPropMass, PropertyMass>(index, out var speckleObjects) && speckleObjects != null && speckleObjects.Count > 0)
        ? speckleObjects.First() : null;
    }
    #endregion

    #region Property1D
    /// <summary>
    /// Get Speckle Property1D object from GSA property 1D index
    /// </summary>
    /// <param name="index">GSA property 1D index</param>
    /// <returns></returns>
    private Property1D GetProperty1dFromIndex(int index)
    {
      return (Instance.GsaModel.Cache.GetSpeckleObjects<GsaSection, Property1D>(index, out var speckleObjects) && speckleObjects != null && speckleObjects.Count > 0)
        ? speckleObjects.First() : null;
    }

    #region Profiles
    private SectionProfile GetProfileCatalogue(ProfileDetails gsaProfile)
    {
      var p = (ProfileDetailsCatalogue)gsaProfile;
      var items = p.Profile.Split(' ');
      var speckleProfile = new Catalogue()
      {
        shapeType = ShapeType.Catalogue,
        description = p.Profile,
        catalogueName = items[1].Split('-')[0],
        sectionType = items[1].Split('-')[1],
        sectionName = items[2],
      };
      return speckleProfile;
    }
    private SectionProfile GetProfileExplicit(ProfileDetails gsaProfile)
    {
      var p = (ProfileDetailsExplicit)gsaProfile;

      var speckleProfile = new Explicit()
      {
        shapeType = ShapeType.Explicit,
      };

      if (p.Area.HasValue) speckleProfile.area = p.Area.Value;
      if (p.Iyy.HasValue) speckleProfile.Iyy = p.Iyy.Value;
      if (p.Izz.HasValue) speckleProfile.Izz = p.Izz.Value;
      if (p.J.HasValue) speckleProfile.J = p.J.Value;
      if (p.Ky.HasValue) speckleProfile.Ky = p.Ky.Value;
      if (p.Kz.HasValue) speckleProfile.Kz = p.Kz.Value;

      return speckleProfile;
    }
    private SectionProfile GetProfilePerimeter(ProfileDetails gsaProfile)
    {
      var gsaProfilePerimeter = (ProfileDetailsPerimeter)gsaProfile;
      var speckleProfile = new Perimeter()
      {
        shapeType = ShapeType.Perimeter,
        voids = new List<Objects.ICurve>(),
      };

      if (gsaProfilePerimeter.Type[0] == 'P') //Perimeter
      {
        var isVoid = false;
        var outline = new List<double>();
        var voids = new List<List<double>>();
        for (var i = 0; i < gsaProfilePerimeter.Actions.Count(); i++)
        {
          if (gsaProfilePerimeter.Actions[i] == "M" && i != 0) isVoid = true;
          if (gsaProfilePerimeter.Actions[i] == "M" && isVoid) voids.Add(new List<double>());

          if (gsaProfilePerimeter.Y[i].HasValue && gsaProfilePerimeter.Z[i].HasValue)
          {
            if (!isVoid)
            {
              outline.Add(0);
              outline.Add(gsaProfilePerimeter.Y[i].Value);
              outline.Add(gsaProfilePerimeter.Z[i].Value);
            }
            else
            {
              voids.Last().Add(0);
              voids.Last().Add(gsaProfilePerimeter.Y[i].Value);
              voids.Last().Add(gsaProfilePerimeter.Z[i].Value);
            }
          }
        }
        speckleProfile.outline = new Curve() { points = outline };
        foreach (var v in voids) speckleProfile.voids.Add(new Curve() { points = v });
      }
      else if (gsaProfilePerimeter.Type[0] == 'L') //Line Segment
      {
        //TO DO:
      }

      return speckleProfile;
    }
    private SectionProfile GetProfileStandard(ProfileDetails gsaProfile)
    {
      var p = (ProfileDetailsStandard)gsaProfile;
      var speckleProfile = new SectionProfile();
      var fns = new Dictionary<Section1dStandardProfileType, Func<ProfileDetailsStandard, SectionProfile>>
      { { Section1dStandardProfileType.Rectangular, GetProfileStandardRectangluar },
        { Section1dStandardProfileType.RectangularHollow, GetProfileStandardRHS },
        { Section1dStandardProfileType.Circular, GetProfileStandardCircular },
        { Section1dStandardProfileType.CircularHollow, GetProfileStandardCHS },
        { Section1dStandardProfileType.ISection, GetProfileStandardISection },
        { Section1dStandardProfileType.Tee, GetProfileStandardTee },
        { Section1dStandardProfileType.Angle, GetProfileStandardAngle },
        { Section1dStandardProfileType.Channel, GetProfileStandardChannel },
        //TO DO: implement other standard sections as more sections are supported
      };
      if (fns.ContainsKey(p.ProfileType)) speckleProfile = fns[p.ProfileType](p);

      return speckleProfile;
    }
    private SectionProfile GetProfileStandardRectangluar(ProfileDetailsStandard gsaProfile)
    {
      var p = (ProfileDetailsRectangular)gsaProfile;
      var speckleProfile = new Rectangular()
      {
        name = "",
        shapeType = ShapeType.Rectangular,
      };
      if (p.b.HasValue) speckleProfile.width = p.b.Value;
      if (p.d.HasValue) speckleProfile.depth = p.d.Value;
      return speckleProfile;
    }
    private SectionProfile GetProfileStandardRHS(ProfileDetailsStandard gsaProfile)
    {
      var p = (ProfileDetailsTwoThickness)gsaProfile;
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
    private SectionProfile GetProfileStandardCircular(ProfileDetailsStandard gsaProfile)
    {
      var p = (ProfileDetailsCircular)gsaProfile;
      var speckleProfile = new Circular()
      {
        name = "",
        shapeType = ShapeType.Circular,
      };
      if (p.d.HasValue) speckleProfile.radius = p.d.Value / 2;
      return speckleProfile;
    }
    private SectionProfile GetProfileStandardCHS(ProfileDetailsStandard gsaProfile)
    {
      var p = (ProfileDetailsCircularHollow)gsaProfile;
      var speckleProfile = new Circular()
      {
        name = "",
        shapeType = ShapeType.Circular,
      };
      if (p.d.HasValue) speckleProfile.radius = p.d.Value / 2;
      if (p.t.HasValue) speckleProfile.wallThickness = p.t.Value;
      return speckleProfile;
    }
    private SectionProfile GetProfileStandardISection(ProfileDetailsStandard gsaProfile)
    {
      var p = (ProfileDetailsTwoThickness)gsaProfile;
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
    private SectionProfile GetProfileStandardTee(ProfileDetailsStandard gsaProfile)
    {
      var p = (ProfileDetailsTwoThickness)gsaProfile;
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
    private SectionProfile GetProfileStandardAngle(ProfileDetailsStandard gsaProfile)
    {
      var p = (ProfileDetailsTwoThickness)gsaProfile;
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
    private SectionProfile GetProfileStandardChannel(ProfileDetailsStandard gsaProfile)
    {
      var p = (ProfileDetailsTwoThickness)gsaProfile;
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
    #endregion
    #endregion

    #region Property2D
    /// <summary>
    /// Convert GSA 2D element reference axis to Speckle
    /// </summary>
    /// <param name="gsaProp2d">GsaProp2d object with reference axis definition</param>
    /// <returns></returns>
    private Axis GetOrientationAxis(GsaProp2d gsaProp2d)
    {
      //Cartesian coordinate system is the only one supported.
      var orientationAxis = new Axis()
      {
        name = "",
        axisType = AxisType.Cartesian,
      };

      if (gsaProp2d.AxisRefType == AxisRefType.Local)
      {
        //TO DO: handle local reference axis case
        //Local would be a different coordinate system for each element that gsaProp2d was assigned to
      }
      else if (gsaProp2d.AxisRefType == AxisRefType.Reference && gsaProp2d.AxisIndex.IsIndex())
      {
        orientationAxis = GetAxisFromIndex(gsaProp2d.AxisIndex.Value);
      }
      else
      {
        //Default global coordinates for case: Global or NotSet
        orientationAxis = GlobalAxis();
      }

      return orientationAxis;
    }

    /// <summary>
    /// Get Speckle Property2D object from GSA property 2D index
    /// </summary>
    /// <param name="index">GSA property 2D index</param>
    /// <returns></returns>
    private Property2D GetProperty2dFromIndex(int index)
    {
      return (Instance.GsaModel.Cache.GetSpeckleObjects<GsaProp2d, Property2D>(index, out var speckleObjects)) ? speckleObjects.First() : null;
    }
    #endregion
    #endregion

    #region Constraint
    private Dictionary<AxisDirection6, List<AxisDirection6>> GetRigidConstraint(Dictionary<GwaAxisDirection6, List<GwaAxisDirection6>> gsaLink)
    {
      var speckleConstraint = new Dictionary<AxisDirection6, List<AxisDirection6>>();
      foreach (var key in gsaLink.Keys)
      {
        var speckleKey = key.ToSpeckle();
        speckleConstraint[speckleKey] = new List<AxisDirection6>();
        foreach (var val in gsaLink[key])
        {
          speckleConstraint[speckleKey].Add(val.ToSpeckle());
        }
      }
      return speckleConstraint;
    }
    #endregion

    #region Analysis Stage
    private GSAStage GetStageFromIndex(int index)
    {
      return (Instance.GsaModel.Cache.GetSpeckleObjects<GsaAnalStage, GSAStage>(index, out var speckleObjects) && speckleObjects != null && speckleObjects.Count > 0)
        ? speckleObjects.First() : null;
    }
    #endregion

    #region Bridge
    private GSAAlignment GetAlignmentFromIndex(int index)
    {
      return (Instance.GsaModel.Cache.GetSpeckleObjects<GsaAlign, GSAAlignment>(index, out var speckleObjects) && speckleObjects != null && speckleObjects.Count > 0)
        ? speckleObjects.First() : null;
    }
    #endregion
    #endregion
    #endregion
  }
}
