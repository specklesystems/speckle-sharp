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
using StructuralUtilities.PolygonMesher;

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
      var toSpeckleResult = ToSpeckleFns.ContainsKey(nativeType) ? ToSpeckleFns[nativeType](nativeObject, layer) : null;

      //A pulse with conversion result to help with progress bars on the UI
      if (Instance.GsaModel.ConversionProgress != null && toSpeckleResult != null)
      {
        Instance.GsaModel.ConversionProgress.Report(toSpeckleResult.Success);
      }
      return toSpeckleResult;
    }

    #region Geometry
    private ToSpeckleResult GsaAssemblyToSpeckle(GsaRecord nativeObject, GSALayer layer = GSALayer.Both)
    {
      //One object is required, either for the analysis layer or the design layer.

      var gsaAssembly = (GsaAssembly)nativeObject;
      if (layer == GSALayer.Design && gsaAssembly.Type == GSAEntity.ELEMENT) return new ToSpeckleResult(true); //assume this is meant for the analysis layer only

      //local variables
      var speckleAssembly = new GSAAssembly()
      {
        nativeId = gsaAssembly.Index ?? 0,
        name = gsaAssembly.Name,
        sizeY = gsaAssembly.SizeY,
        sizeZ = gsaAssembly.SizeZ,
        curveType = gsaAssembly.CurveType.ToString(),
        curveOrder = gsaAssembly.CurveOrder ?? 0,
        pointDefinition = gsaAssembly.PointDefn.ToString(),
        points = GetAssemblyPoints(gsaAssembly),
        entities = GetAssemblyEntites(gsaAssembly),
      };
      if (gsaAssembly.Index.IsIndex()) speckleAssembly.applicationId = Instance.GsaModel.Cache.GetApplicationId<GsaAssembly>(gsaAssembly.Index.Value);
      if (gsaAssembly.Topo1.IsIndex())
      {
        speckleAssembly.end1Node = (GSANode)GetNodeFromIndex(gsaAssembly.Topo1.Value);
        AddToMeaningfulNodeIndices(speckleAssembly.end1Node.applicationId);
      }
      if (gsaAssembly.Topo2.IsIndex())
      {
        speckleAssembly.end2Node = (GSANode)GetNodeFromIndex(gsaAssembly.Topo2.Value);
        AddToMeaningfulNodeIndices(speckleAssembly.end2Node.applicationId);
      }
      if (gsaAssembly.OrientNode.IsIndex()) speckleAssembly.orientationNode = (GSANode)GetNodeFromIndex(gsaAssembly.OrientNode.Value);
      if (gsaAssembly.IntTopo.HasValues())
      {
        var intTopo = gsaAssembly.IntTopo.Select(i => GetNodeFromIndex(i)).ToList();
        speckleAssembly.entities.AddRange(intTopo);
        AddToMeaningfulNodeIndices(intTopo.Select(n => n.applicationId));
      }

      if (gsaAssembly.Type == GSAEntity.MEMBER)
      {
        return new ToSpeckleResult(designLayerOnlyObjects: new List<Base>() { speckleAssembly });
      }
      else if (gsaAssembly.Type == GSAEntity.ELEMENT && layer == GSALayer.Both)
      {
        return new ToSpeckleResult(analysisLayerOnlyObjects: new List<Base>() { speckleAssembly });
      }
      else
      {
        ConversionErrors.Add(new Exception("GsaAssemblyToSpeckle: GSAEntity type (" + gsaAssembly.Type.ToString() + ") not supported when GSAlayer (" + layer.ToString() + ") is called."));
        return new ToSpeckleResult(false);
      }
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
        nativeId = gsaNode.Index ?? 0,
        localElementSize = gsaNode.MeshSize ?? 0,
      };

      //-- App agnostic --
      if (gsaNode.Index.IsIndex()) speckleNode.applicationId = Instance.GsaModel.Cache.GetApplicationId<GsaNode>(gsaNode.Index.Value);
      if (gsaNode.MassPropertyIndex.IsIndex()) speckleNode.massProperty = GetPropertyMassFromIndex(gsaNode.MassPropertyIndex.Value);
      if (gsaNode.SpringPropertyIndex.IsIndex()) speckleNode.springProperty = GetPropertySpringFromIndex(gsaNode.SpringPropertyIndex.Value);

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
      //  PropertyDamper damperProperty - dampers not currently supported
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
      if (gsaAxis.Index.IsIndex()) speckleAxis.applicationId = Instance.GsaModel.Cache.GetApplicationId<GsaAxis>(gsaAxis.Index.Value);
      if (gsaAxis.XDirX.HasValue && gsaAxis.XDirY.HasValue && gsaAxis.XDirZ.HasValue && gsaAxis.XYDirX.HasValue && gsaAxis.XYDirY.HasValue && gsaAxis.XYDirZ.HasValue)
      {
        var origin = new Point(gsaAxis.OriginX, gsaAxis.OriginY, gsaAxis.OriginZ);
        var xdir = (new Vector(gsaAxis.XDirX.Value, gsaAxis.XDirY.Value, gsaAxis.XDirZ.Value)).UnitVector();
        var ydir = (new Vector(gsaAxis.XYDirX.Value, gsaAxis.XYDirY.Value, gsaAxis.XYDirZ.Value)).UnitVector(); //vector in xy-plane
        var normal = (xdir * ydir).UnitVector();
        ydir = -(xdir * normal).UnitVector();
        speckleAxis.definition = new Plane(origin, normal, xdir, ydir);
      }
      else
      {
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
      //Currently doesn't handle tapers
      var speckleElement1d = new GSAElement1D()
      {
        //-- App agnostic --
        name = gsaEl.Name,
        type = gsaEl.Type.ToSpeckle1d(),
        end1Releases = GetRestraint(gsaEl.Releases1, gsaEl.Stiffnesses1),
        end2Releases = GetRestraint(gsaEl.Releases2, gsaEl.Stiffnesses1),
        end1Offset = new Vector(),
        end2Offset = new Vector(),
        orientationAngle = gsaEl.Angle ?? 0,
        displayMesh = null, //TODO: update to handle section shape

        //-- GSA specific --
        nativeId = gsaEl.Index ?? 0,
        group = gsaEl.Group ?? 0,
        colour = gsaEl.Colour.ToString(),
        isDummy = gsaEl.Dummy,
        action = "NORMAL", //TODO: update if interim schema is updated
      };

      //-- App agnostic --
      if (gsaEl.NodeIndices.Count >= 2)
      {
        speckleElement1d.end1Node = GetNodeFromIndex(gsaEl.NodeIndices[0]);
        speckleElement1d.end2Node = GetNodeFromIndex(gsaEl.NodeIndices[1]);
        speckleElement1d.topology = gsaEl.NodeIndices.Select(i => GetNodeFromIndex(i)).ToList();
        AddToMeaningfulNodeIndices(speckleElement1d.topology.Select(n => n.applicationId), GSALayer.Analysis);
      }
      else
      {
        ConversionErrors.Add(new Exception("GsaElement1dToSpeckle: "
          + "Error converting 1D element with application id (" + speckleElement1d.applicationId + "). "
          + "There must be atleast 2 nodes to define the element"));
        return null;
      }
      speckleElement1d.baseLine = new Line(speckleElement1d.end1Node.basePoint, speckleElement1d.end2Node.basePoint);
      if (gsaEl.Index.IsIndex()) speckleElement1d.applicationId = Instance.GsaModel.Cache.GetApplicationId<GsaEl>(gsaEl.Index.Value);
      if (gsaEl.PropertyIndex.IsIndex()) speckleElement1d.property = GetProperty1dFromIndex(gsaEl.PropertyIndex.Value);
      if (gsaEl.OrientationNodeIndex.IsIndex())
      {
        speckleElement1d.orientationNode = GetNodeFromIndex(gsaEl.OrientationNodeIndex.Value);
        AddToMeaningfulNodeIndices(speckleElement1d.orientationNode.applicationId, GSALayer.Analysis);
      }
      speckleElement1d.localAxis = GetLocalAxis(speckleElement1d.end1Node, speckleElement1d.end2Node, speckleElement1d.orientationNode, speckleElement1d.orientationAngle.Radians());
      if (gsaEl.End1OffsetX.HasValue) speckleElement1d.end1Offset.x = gsaEl.End1OffsetX.Value;
      if (gsaEl.OffsetY.HasValue) speckleElement1d.end1Offset.y = gsaEl.OffsetY.Value;
      if (gsaEl.OffsetZ.HasValue) speckleElement1d.end1Offset.z = gsaEl.OffsetZ.Value;
      if (gsaEl.End2OffsetX.HasValue) speckleElement1d.end2Offset.x = gsaEl.End2OffsetX.Value;
      if (gsaEl.OffsetY.HasValue) speckleElement1d.end2Offset.y = gsaEl.OffsetY.Value;
      if (gsaEl.OffsetZ.HasValue) speckleElement1d.end2Offset.z = gsaEl.OffsetZ.Value;
      if (gsaEl.ParentIndex.IsIndex()) speckleElement1d.parent = GetMemberFromIndex(gsaEl.ParentIndex.Value);

      //TODO:
      //NativeObject:
      //  TaperOffsetPercentageEnd1
      //  TaperOffsetPercentageEnd2
      //SpeckleObject:
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
        displayMesh = DisplayMesh2d(gsaEl.NodeIndices),
        orientationAngle = gsaEl.Angle ?? 0,
        offset = gsaEl.OffsetZ ?? 0,
        units = "",

        //-- GSA specific --
        nativeId = gsaEl.Index ?? 0,
        group = gsaEl.Group ?? 0,
        colour = gsaEl.Colour.ToString(),
        isDummy = gsaEl.Dummy,
      };

      //-- App agnostic --
      if (gsaEl.NodeIndices.Count >= 3)
      {
        speckleElement2d.topology = gsaEl.NodeIndices.Select(i => GetNodeFromIndex(i)).ToList();
        AddToMeaningfulNodeIndices(speckleElement2d.topology.Select(e => e.applicationId), GSALayer.Analysis);
      }
      else
      {
        ConversionErrors.Add(new Exception("GsaElement2dToSpeckle: "
          + "Error converting 2D element with application id (" + speckleElement2d.applicationId + "). "
          + "There must be atleast 3 nodes to define the element"));
        return null;
      }
      if (gsaEl.Index.IsIndex()) speckleElement2d.applicationId = Instance.GsaModel.Cache.GetApplicationId<GsaEl>(gsaEl.Index.Value);
      if (gsaEl.PropertyIndex.IsIndex()) speckleElement2d.property = GetProperty2dFromIndex(gsaEl.PropertyIndex.Value);
      if (gsaEl.ParentIndex.IsIndex()) speckleElement2d.parent = GetMemberFromIndex(gsaEl.ParentIndex.Value);

      return speckleElement2d;

      //TODO:
      //SpeckleObject:
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
        orientationAngle = gsaMemb.Angle ?? 0,
        parent = null, //no meaning for member, only for element

        //-- GSA specific --
        nativeId = gsaMemb.Index ?? 0,
        group = gsaMemb.Group ?? 0,
        targetMeshSize = gsaMemb.MeshSize ?? 0,
        colour = gsaMemb.Colour.ToString(),
        isDummy = gsaMemb.Dummy,
        intersectsWithOthers = gsaMemb.IsIntersector,
      };

      //-- App agnostic --
      if (gsaMemb.NodeIndices.Count >= 2)
      {
        speckleMember1d.end1Node = GetNodeFromIndex(gsaMemb.NodeIndices[0]);
        speckleMember1d.end2Node = GetNodeFromIndex(gsaMemb.NodeIndices[1]);
        speckleMember1d.topology = gsaMemb.NodeIndices.Select(i => GetNodeFromIndex(i)).ToList();
        AddToMeaningfulNodeIndices(speckleMember1d.topology.Select(n => n.applicationId), GSALayer.Design);
      }
      else
      {
        ConversionErrors.Add(new Exception("GsaMember1dToSpeckle: "
          + "Error converting 1D member with application id (" + speckleMember1d.applicationId + "). "
          + "There must be atleast 2 nodes to define the member"));
        return null;
      }
      speckleMember1d.baseLine = GetBaseLine(speckleMember1d.topology.Select(n => n.basePoint).ToList());
      if (gsaMemb.Index.IsIndex()) speckleMember1d.applicationId = Instance.GsaModel.Cache.GetApplicationId<GsaMemb>(gsaMemb.Index.Value);
      if (gsaMemb.PropertyIndex.IsIndex()) speckleMember1d.property = GetProperty1dFromIndex(gsaMemb.PropertyIndex.Value);
      if (gsaMemb.OrientationNodeIndex.IsIndex()) speckleMember1d.orientationNode = GetNodeFromIndex(gsaMemb.OrientationNodeIndex.Value);
      speckleMember1d.localAxis = GetLocalAxis(speckleMember1d.end1Node, speckleMember1d.end2Node, speckleMember1d.orientationNode, speckleMember1d.orientationAngle.Radians());
      if (gsaMemb.End1OffsetX.HasValue) speckleMember1d.end1Offset.x = gsaMemb.End1OffsetX.Value;
      if (gsaMemb.OffsetY.HasValue) speckleMember1d.end1Offset.y = gsaMemb.OffsetY.Value;
      if (gsaMemb.OffsetZ.HasValue) speckleMember1d.end1Offset.z = gsaMemb.OffsetZ.Value;
      if (gsaMemb.End2OffsetX.HasValue) speckleMember1d.end2Offset.x = gsaMemb.End2OffsetX.Value;
      if (gsaMemb.OffsetY.HasValue) speckleMember1d.end2Offset.y = gsaMemb.OffsetY.Value;
      if (gsaMemb.OffsetZ.HasValue) speckleMember1d.end2Offset.z = gsaMemb.OffsetZ.Value;

      //The following properties aren't part of the structural schema:
      speckleMember1d["Exposure"] = gsaMemb.Exposure.ToString();
      speckleMember1d["AnalysisType"] = gsaMemb.AnalysisType.ToString();
      speckleMember1d["Fire"] = gsaMemb.Fire.ToString();
      speckleMember1d["CreationFromStartDays"] = gsaMemb.CreationFromStartDays;
      speckleMember1d["StartOfDryingDays"] = gsaMemb.StartOfDryingDays;
      speckleMember1d["AgeAtLoadingDays"] = gsaMemb.AgeAtLoadingDays;
      speckleMember1d["RemovedAtDays"] = gsaMemb.RemovedAtDays;
      speckleMember1d["RestraintEnd1"] = gsaMemb.RestraintEnd1.ToString();
      speckleMember1d["RestraintEnd2"] = gsaMemb.RestraintEnd2.ToString();
      speckleMember1d["EffectiveLengthType"] = gsaMemb.EffectiveLengthType.ToString();
      speckleMember1d["LoadHeightReferencePoint"] = gsaMemb.LoadHeightReferencePoint.ToString();
      speckleMember1d["MemberHasOffsets"] = gsaMemb.MemberHasOffsets;
      speckleMember1d["End1AutomaticOffset"] = gsaMemb.End1AutomaticOffset;
      speckleMember1d["End2AutomaticOffset"] = gsaMemb.End2AutomaticOffset;
      if (gsaMemb.Voids.HasValues())
      {
        var speckleVoids = gsaMemb.Voids.Select(v => v.Select(i => GetNodeFromIndex(i)).ToList()).ToList();
        speckleMember1d["Voids"] = speckleVoids;
        AddToMeaningfulNodeIndices(speckleVoids.SelectMany(n => n.Select(n2 => n2.applicationId)), GSALayer.Design);
      }
      if (gsaMemb.PointNodeIndices.HasValues())
      {
        var specklePoints = gsaMemb.PointNodeIndices.Select(i => (GSANode)GetNodeFromIndex(i)).ToList();
        speckleMember1d["Points"] = specklePoints;
        AddToMeaningfulNodeIndices(specklePoints.Select(p => p.applicationId), GSALayer.Design);
      }
      if (gsaMemb.Polylines.HasValues())
      {
        var speckleLines = gsaMemb.Polylines.Select(v => v.Select(i => (GSANode)GetNodeFromIndex(i)).ToList()).ToList();
        speckleMember1d["Lines"] = speckleLines;
        AddToMeaningfulNodeIndices(speckleLines.SelectMany(p => p.Select(p2 => p2.applicationId)), GSALayer.Design);
      }
      if (gsaMemb.AdditionalAreas.HasValues())
      {
        var speckleAreas = gsaMemb.AdditionalAreas.Select(v => v.Select(i => (GSANode)GetNodeFromIndex(i)).ToList()).ToList();
        speckleMember1d["Areas"] = speckleAreas;
        AddToMeaningfulNodeIndices(speckleAreas.SelectMany(p => p.Select(p2 => p2.applicationId)), GSALayer.Design);
      }
      if (gsaMemb.LimitingTemperature.HasValue) speckleMember1d["LimitingTemperature"] = gsaMemb.LimitingTemperature;
      if (gsaMemb.LoadHeight.HasValue) speckleMember1d["LoadHeight"] = gsaMemb.LoadHeight;
      if (gsaMemb.EffectiveLengthYY.HasValue) speckleMember1d["EffectiveLengthYY"] = gsaMemb.EffectiveLengthYY;
      if (gsaMemb.PercentageYY.HasValue) speckleMember1d["PercentageYY"] = gsaMemb.PercentageYY;
      if (gsaMemb.EffectiveLengthZZ.HasValue) speckleMember1d["EffectiveLengthZZ"] = gsaMemb.EffectiveLengthZZ;
      if (gsaMemb.PercentageZZ.HasValue) speckleMember1d["PercentageZZ"] = gsaMemb.PercentageZZ;
      if (gsaMemb.EffectiveLengthLateralTorsional.HasValue) speckleMember1d["EffectiveLengthLateralTorsional"] = gsaMemb.EffectiveLengthLateralTorsional;
      if (gsaMemb.FractionLateralTorsional.HasValue) speckleMember1d["FractionLateralTorsional"] = gsaMemb.FractionLateralTorsional;
      if (gsaMemb.SpanRestraints != null)
      {
        speckleMember1d["SpanRestraints"] = gsaMemb.SpanRestraints.Select(s => new RestraintDefinition() { All = s.All, Index = s.Index, Restraint = s.Restraint }).ToList();
      }
      if (gsaMemb.PointRestraints != null)
      {
        speckleMember1d["PointRestraints"] = gsaMemb.PointRestraints.Select(s => new RestraintDefinition() { All = s.All, Index = s.Index, Restraint = s.Restraint }).ToList();
      }

      return speckleMember1d;

      //TODO:
      //SpeckleObject:
      //  public string units
    }

    private GSAMember2D GsaMember2dToSpeckle(GsaMemb gsaMemb)
    {
      var color = gsaMemb.Type == Speckle.GSA.API.GwaSchema.MemberType.Void2d ? System.Drawing.Color.LightPink : System.Drawing.Color.White;
      var speckleMember2d = new GSAMember2D()
      {
        //-- App agnostic --
        name = gsaMemb.Name,
        type = gsaMemb.Type.ToSpeckle2d(),
        displayMesh = DisplayMeshPolygon(gsaMemb.NodeIndices, color),
        orientationAngle = gsaMemb.Angle ?? 0,
        offset = gsaMemb.Offset2dZ ?? 0,
        parent = null, //no meaning for member, only for element

        //-- GSA specific --
        nativeId = gsaMemb.Index ?? 0,
        group = gsaMemb.Group ?? 0,
        targetMeshSize = gsaMemb.MeshSize ?? 0,
        colour = gsaMemb.Colour.ToString(),
        isDummy = gsaMemb.Dummy,
        intersectsWithOthers = gsaMemb.IsIntersector,
      };

      //-- App agnostic --
      if (gsaMemb.NodeIndices.Count >= 3)
      {
        speckleMember2d.topology = gsaMemb.NodeIndices.Select(i => GetNodeFromIndex(i)).ToList();
        AddToMeaningfulNodeIndices(speckleMember2d.topology.Select(n => n.applicationId), GSALayer.Design);
      }
      else
      {
        ConversionErrors.Add(new Exception("GsaMember2dToSpeckle: "
          + "Error converting 2D member with application id (" + speckleMember2d.applicationId + "). "
          + "There must be atleast 3 nodes to define the member"));
        return null;
      }
      if (gsaMemb.Index.IsIndex()) speckleMember2d.applicationId = Instance.GsaModel.Cache.GetApplicationId<GsaMemb>(gsaMemb.Index.Value);
      if (gsaMemb.PropertyIndex.IsIndex()) speckleMember2d.property = GetProperty2dFromIndex(gsaMemb.PropertyIndex.Value);
      if (gsaMemb.Voids.HasValues())
      {
        speckleMember2d.voids = gsaMemb.Voids.Select(v => v.Select(i => GetNodeFromIndex(i)).ToList()).ToList();
        AddToMeaningfulNodeIndices(speckleMember2d.voids.SelectMany(n => n.Select(n2 => n2.applicationId)), GSALayer.Design);
      } else
      {

      }

      //The following properties aren't part of the structural schema:
      speckleMember2d["Exposure"] = gsaMemb.Exposure.ToString();
      speckleMember2d["AnalysisType"] = gsaMemb.AnalysisType.ToString();
      speckleMember2d["Fire"] = gsaMemb.Fire.ToString();
      speckleMember2d["CreationFromStartDays"] = gsaMemb.CreationFromStartDays;
      speckleMember2d["StartOfDryingDays"] = gsaMemb.StartOfDryingDays;
      speckleMember2d["AgeAtLoadingDays"] = gsaMemb.AgeAtLoadingDays;
      speckleMember2d["RemovedAtDays"] = gsaMemb.RemovedAtDays;
      speckleMember2d["OffsetAutomaticInternal"] = gsaMemb.OffsetAutomaticInternal;
      if (gsaMemb.Voids.HasValues())
      {
        var speckleVoids = gsaMemb.Voids.Select(v => v.Select(i => GetNodeFromIndex(i)).ToList()).ToList();
        speckleMember2d["Voids"] = speckleVoids;
        AddToMeaningfulNodeIndices(speckleVoids.SelectMany(n => n.Select(n2 => n2.applicationId)), GSALayer.Design);
      }
      if (gsaMemb.PointNodeIndices.HasValues())
      {
        var specklePoints = gsaMemb.PointNodeIndices.Select(i => (GSANode)GetNodeFromIndex(i)).ToList();
        speckleMember2d["Points"] = specklePoints;
        AddToMeaningfulNodeIndices(specklePoints.Select(p => p.applicationId), GSALayer.Design);
      }
      if (gsaMemb.Polylines.HasValues())
      {
        var speckleLines = gsaMemb.Polylines.Select(v => v.Select(i => (GSANode)GetNodeFromIndex(i)).ToList()).ToList();
        speckleMember2d["Lines"] = speckleLines;
        AddToMeaningfulNodeIndices(speckleLines.SelectMany(l => l.Select(l2 => l2.applicationId)), GSALayer.Design);
      }
      if (gsaMemb.AdditionalAreas.HasValues())
      {
        var speckleAreas = gsaMemb.AdditionalAreas.Select(v => v.Select(i => (GSANode)GetNodeFromIndex(i)).ToList()).ToList();
        speckleMember2d["Areas"] = speckleAreas;
        AddToMeaningfulNodeIndices(speckleAreas.SelectMany(a => a.Select(a2 => a2.applicationId)), GSALayer.Design);
      }
      if (gsaMemb.LimitingTemperature.HasValue) speckleMember2d["LimitingTemperature"] = gsaMemb.LimitingTemperature.Value;
      
      return speckleMember2d;

      //TODO:
      //SpeckleObject:
      //  public Mesh displayMesh
      //  public string units
    }

    private ToSpeckleResult GsaGridLineToSpeckle(GsaRecord nativeObject, GSALayer layer = GSALayer.Both)
    {
      var gsaGridLine = (GsaGridLine)nativeObject;
      var speckleGridLine = new GSAGridLine()
      {
        label = gsaGridLine.Name,
        nativeId = gsaGridLine.Index ?? 0,
      };
      if (gsaGridLine.Index.IsIndex()) speckleGridLine.applicationId = Instance.GsaModel.Cache.GetApplicationId<GsaGridLine>(gsaGridLine.Index.Value);
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
        nativeId = gsaGridPlane.Index ?? 0,
        name = gsaGridPlane.Name,
        elevation = gsaGridPlane.Elevation ?? 0,
        axis = GetGridPlaneAxis(gsaGridPlane.AxisRefType, gsaGridPlane.AxisIndex),
        toleranceBelow = GetStoreyTolerance(gsaGridPlane.StoreyToleranceBelow, gsaGridPlane.StoreyToleranceBelowAuto, gsaGridPlane.Type),
        toleranceAbove = GetStoreyTolerance(gsaGridPlane.StoreyToleranceAbove, gsaGridPlane.StoreyToleranceAboveAuto, gsaGridPlane.Type),
      };
      if (gsaGridPlane.Index.IsIndex()) speckleGridPlane.applicationId = Instance.GsaModel.Cache.GetApplicationId<GsaGridPlane>(gsaGridPlane.Index.Value);

      return new ToSpeckleResult(speckleGridPlane);

      //TODO:
      //SpeckleObject:
      //  public string units
    }

    private ToSpeckleResult GsaGridSurfaceToSpeckle(GsaRecord nativeObject, GSALayer layer = GSALayer.Both)
    {
      //Two different objects are required, one for the analysis layer and one for the design layer.
      //All conversions will be assigned to local variables first and then the two speckle objects will be created.

      var gsaGridSurface = (GsaGridSurface)nativeObject;
      if (layer == GSALayer.Design && !gsaGridSurface.MemberIndices.HasValues() && gsaGridSurface.ElementIndices.HasValues()) return new ToSpeckleResult(true); //assume this is meant for the analysis layer only

      //Defaults;
      string applicationId = null;
      GSAGridSurface analysisGridSurface = null;

      //local variables
      var gridPlane = GetGridPlane(gsaGridSurface.PlaneRefType, gsaGridSurface.PlaneIndex);
      var loadExpansion = gsaGridSurface.Expansion.ToSpeckle();
      var span = gsaGridSurface.Span.ToSpeckle();
      if (gsaGridSurface.Index.IsIndex()) applicationId = Instance.GsaModel.Cache.GetApplicationId<GsaGridSurface>(gsaGridSurface.Index.Value);

      //design layer
      var designGridSurface = new GSAGridSurface()
      {
        applicationId = applicationId,
        nativeId = gsaGridSurface.Index ?? 0,
        name = gsaGridSurface.Name,
        gridPlane = gridPlane,
        loadExpansion = loadExpansion,
        span = span,
        tolerance = gsaGridSurface.Tolerance ?? 0,
        spanDirection = gsaGridSurface.Angle ?? 0,
      };
      if (gsaGridSurface.MemberIndices.HasValues()) designGridSurface.elements = gsaGridSurface.MemberIndices.Select(i => GetMemberFromIndex(i)).ToList();

      if (layer == GSALayer.Both)
      {
        //analysis layer
        analysisGridSurface = new GSAGridSurface()
        {
          applicationId = applicationId,
          nativeId = gsaGridSurface.Index ?? 0,
          name = gsaGridSurface.Name,
          gridPlane = gridPlane,
          loadExpansion = loadExpansion,
          span = span,
          tolerance = gsaGridSurface.Tolerance ?? 0,
          spanDirection = gsaGridSurface.Angle ?? 0,
        };
        if (gsaGridSurface.ElementIndices.HasValues()) analysisGridSurface.elements = gsaGridSurface.ElementIndices.Select(i => GetElementFromIndex(i)).ToList();
      }

      var toSpeckleResult = (analysisGridSurface == null) ? new ToSpeckleResult(designLayerOnlyObjects: new List<Base>() { designGridSurface })
        : new ToSpeckleResult(designLayerOnlyObjects: new List<Base>() { designGridSurface }, analysisLayerOnlyObjects: new List<Base>() { analysisGridSurface });
      return toSpeckleResult;
    }

    private ToSpeckleResult GsaPolylineToSpeckle(GsaRecord nativeObject, GSALayer layer = GSALayer.Both)
    {
      var gsaPolyline = (GsaPolyline)nativeObject;
      var specklePolyline = new GSAPolyline()
      {
        nativeId = gsaPolyline.Index ?? 0,
        name = gsaPolyline.Name,
        colour = gsaPolyline.Colour.ToString(),
        units = gsaPolyline.Units,
      };
      if (gsaPolyline.Index.IsIndex()) specklePolyline.applicationId = Instance.GsaModel.Cache.GetApplicationId<GsaPolyline>(gsaPolyline.Index.Value);
      if (gsaPolyline.GridPlaneIndex.IsIndex()) specklePolyline.gridPlane = GetGridPlaneFromIndex(gsaPolyline.GridPlaneIndex.Value);
      if (gsaPolyline.NumDim == 2) specklePolyline.value = gsaPolyline.Values.Insert(0.0, 3); //convert from list of x,y values to x,y,z values
      else specklePolyline.value = gsaPolyline.Values;

      return new ToSpeckleResult(specklePolyline);
    }
    #endregion

    #region Loading
    private ToSpeckleResult GsaLoadCaseToSpeckle(GsaRecord nativeObject, GSALayer layer = GSALayer.Both)
    {
      var gsaLoadCase = (GsaLoadCase)nativeObject;
      var speckleLoadCase = new GSALoadCase()
      {
        nativeId = gsaLoadCase.Index ?? 0,
        name = gsaLoadCase.Title,
        loadType = gsaLoadCase.CaseType.ToSpeckle(),
        actionType = gsaLoadCase.CaseType.GetActionType(),
        description = gsaLoadCase.Category.ToString(),
        direction = gsaLoadCase.Direction.ToSpeckle(),
        include = gsaLoadCase.Include.ToString(),
        bridge = gsaLoadCase.Bridge ?? false,
      };
      if (gsaLoadCase.Index.IsIndex()) speckleLoadCase.applicationId = Instance.GsaModel.Cache.GetApplicationId<GsaLoadCase>(gsaLoadCase.Index.Value);
      if (gsaLoadCase.Source.IsIndex()) speckleLoadCase.group = gsaLoadCase.Source.ToString();

      return new ToSpeckleResult(speckleLoadCase);
    }

    private ToSpeckleResult GsaAnalysisCaseToSpeckle(GsaRecord nativeObject, GSALayer layer = GSALayer.Both)
    {
      var gsaAnalysisCase = (GsaAnal)nativeObject;
      var speckleAnalysisCase = new GSAAnalysisCase()
      {
        nativeId = gsaAnalysisCase.Index ?? 0,
        name = gsaAnalysisCase.Name,
      };
      if (gsaAnalysisCase.Index.IsIndex()) speckleAnalysisCase.applicationId = Instance.GsaModel.Cache.GetApplicationId<GsaAnal>(gsaAnalysisCase.Index.Value);
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
        nativeId = gsaCombination.Index ?? 0,
        name = gsaCombination.Name,
        combinationType = GetCombinationType(gsaCombination.Desc)
      };
      if (gsaCombination.Index.IsIndex()) speckleLoadCombination.applicationId = Instance.GsaModel.Cache.GetApplicationId<GsaCombination>(gsaCombination.Index.Value);
      if (GetLoadCombinationFactors(gsaCombination.Desc, out var loadCases, out var loadFactors))
      {
        speckleLoadCombination.loadCases = loadCases;
        speckleLoadCombination.loadFactors = loadFactors;
      }
      //Not currently part of the schema
      speckleLoadCombination["bridge"] = gsaCombination.Bridge ?? false;
      speckleLoadCombination["note"] = gsaCombination.Note;

      return new ToSpeckleResult(speckleLoadCombination);
    }

    private ToSpeckleResult GsaLoadFaceToSpeckle(GsaRecord nativeObject, GSALayer layer = GSALayer.Both)
    {
      //Two different objects are required, one for the analysis layer and one for the design layer.
      //All conversions will be assigned to local variables first and then the two speckle objects will be created.

      var gsaLoad = (GsaLoad2dFace)nativeObject;
      if (layer == GSALayer.Design && !gsaLoad.MemberIndices.HasValues() && gsaLoad.ElementIndices.HasValues()) return new ToSpeckleResult(true); //assume this is meant for the analysis layer only

      //Defaults;
      string applicationId = null;
      GSALoadFace analysisLoad = null;
      GSALoadCase loadCase = null;
      Axis loadAxis = null;
      List<double> positions = null;

      //local variables
      var loadType = gsaLoad.Type.ToSpeckle();
      var direction = gsaLoad.LoadDirection.ToSpeckle();
      var loadAxisType = gsaLoad.AxisRefType.ToSpeckle();
      if (gsaLoad.Index.IsIndex()) applicationId = Instance.GsaModel.Cache.GetApplicationId<GsaLoad2dFace>(gsaLoad.Index.Value);
      if (gsaLoad.LoadCaseIndex.IsIndex()) loadCase = GetLoadCaseFromIndex(gsaLoad.LoadCaseIndex.Value);
      if (gsaLoad.AxisRefType == AxisRefType.Reference && gsaLoad.AxisIndex.IsIndex()) loadAxis = GetAxisFromIndex(gsaLoad.AxisIndex.Value);
      if (gsaLoad.Type == Load2dFaceType.Point && gsaLoad.R.HasValue && gsaLoad.S.HasValue) positions = new List<double>() { gsaLoad.R.Value, gsaLoad.S.Value };

      //design layer
      var designLoad = new GSALoadFace()
      {
        applicationId = applicationId,
        nativeId = gsaLoad.Index ?? 0,
        name = gsaLoad.Name,
        loadCase = loadCase,
        loadType = loadType,
        direction = direction,
        loadAxisType = loadAxisType,
        loadAxis = loadAxis,
        isProjected = gsaLoad.Projected,
        values = gsaLoad.Values,
        positions = positions,
      };
      if (gsaLoad.MemberIndices.HasValues()) designLoad.elements = gsaLoad.MemberIndices.Select(i => GetMemberFromIndex(i)).ToList();

      if (layer == GSALayer.Both)
      {
        //analysis layer
        analysisLoad = new GSALoadFace()
        {
          applicationId = applicationId,
          nativeId = gsaLoad.Index ?? 0,
          name = gsaLoad.Name,
          loadCase = loadCase,
          loadType = loadType,
          direction = direction,
          loadAxisType = loadAxisType,
          loadAxis = loadAxis,
          isProjected = gsaLoad.Projected,
          values = gsaLoad.Values,
          positions = positions,
        };
        if (gsaLoad.ElementIndices.HasValues()) analysisLoad.elements = gsaLoad.ElementIndices.Select(i => GetElementFromIndex(i)).ToList();
      }

      return new ToSpeckleResult(designLayerOnlyObjects: new List<Base>() { designLoad }, analysisLayerOnlyObjects: new List<Base>() { analysisLoad });

      //TODO:
      //SpeckleObject:
      //  string units
    }

    private ToSpeckleResult GsaLoadBeamToSpeckle(GsaRecord nativeObject, GSALayer layer = GSALayer.Both)
    {
      //Two different objects are required, one for the analysis layer and one for the design layer.
      //All conversions will be assigned to local variables first and then the two speckle objects will be created.

      var gsaLoad = (GsaLoadBeam)nativeObject;
      if (layer == GSALayer.Design && !gsaLoad.MemberIndices.HasValues() && gsaLoad.ElementIndices.HasValues()) return new ToSpeckleResult(true); //assume this is meant for the analysis layer only
      var type = gsaLoad.GetType();

      //Defaults;
      string applicationId = null;
      GSALoadBeam analysisLoad = null;
      GSALoadCase loadCase = null;
      Axis loadAxis = null;

      //local variables
      var loadType = type.ToSpeckle();
      var direction = gsaLoad.LoadDirection.ToSpeckleLoad();
      var loadAxisType = gsaLoad.AxisRefType.ToSpeckle();
      var values = GetLoadBeamValues(gsaLoad);
      var positions = GetLoadBeamPositions(gsaLoad);
      if (gsaLoad.Index.IsIndex()) applicationId = Instance.GsaModel.Cache.GetApplicationId(type, gsaLoad.Index.Value);
      if (gsaLoad.LoadCaseIndex.IsIndex()) loadCase = GetLoadCaseFromIndex(gsaLoad.LoadCaseIndex.Value);
      if (gsaLoad.AxisRefType == LoadBeamAxisRefType.Reference && gsaLoad.AxisIndex.IsIndex()) loadAxis = GetAxisFromIndex(gsaLoad.AxisIndex.Value);

      //design layer
      var designLoad = new GSALoadBeam()
      {
        applicationId = applicationId,
        nativeId = gsaLoad.Index ?? 0,
        name = gsaLoad.Name,
        loadCase = loadCase,
        loadType = loadType,
        direction = direction,
        loadAxisType = loadAxisType,
        loadAxis = loadAxis,
        isProjected = gsaLoad.Projected,
        values = values,
        positions = positions,
      };
      if (gsaLoad.MemberIndices.HasValues()) designLoad.elements = gsaLoad.MemberIndices.Select(i => GetMemberFromIndex(i)).ToList();

      if (layer == GSALayer.Both)
      {
        //analysis layer
        analysisLoad = new GSALoadBeam()
        {
          applicationId = applicationId,
          nativeId = gsaLoad.Index ?? 0,
          name = gsaLoad.Name,
          loadCase = loadCase,
          loadType = loadType,
          direction = direction,
          loadAxisType = loadAxisType,
          loadAxis = loadAxis,
          isProjected = gsaLoad.Projected,
          values = values,
          positions = positions,
        };
        if (gsaLoad.ElementIndices.HasValues()) analysisLoad.elements = gsaLoad.ElementIndices.Select(i => GetElementFromIndex(i)).ToList();
      }

      var toSpeckleResult = (analysisLoad == null) ? new ToSpeckleResult(designLayerOnlyObjects: new List<Base>() { designLoad })
              : new ToSpeckleResult(designLayerOnlyObjects: new List<Base>() { designLoad }, analysisLayerOnlyObjects: new List<Base>() { analysisLoad });
      return toSpeckleResult;

      //TODO:
      //SpeckleObject:
      //  string units
    }

    private ToSpeckleResult GsaLoadNodeToSpeckle(GsaRecord nativeObject, GSALayer layer = GSALayer.Both)
    {
      var gsaLoadNode = (GsaLoadNode)nativeObject;
      var speckleNodeLoad = new GSALoadNode()
      {
        nativeId = gsaLoadNode.Index ?? 0,
        name = gsaLoadNode.Name,
        direction = gsaLoadNode.LoadDirection.ToSpeckleLoad(),
        value = gsaLoadNode.Value ?? 0,
      };

      if (gsaLoadNode.Index.IsIndex()) speckleNodeLoad.applicationId = Instance.GsaModel.Cache.GetApplicationId<GsaLoadNode>(gsaLoadNode.Index.Value);
      if (gsaLoadNode.NodeIndices.HasValues())
      {
        speckleNodeLoad.nodes = gsaLoadNode.NodeIndices.Select(i => GetNodeFromIndex(i)).ToList();
        AddToMeaningfulNodeIndices(speckleNodeLoad.nodes.Where(n => n != null && !string.IsNullOrEmpty(n.applicationId)).Select(n => n.applicationId));
      }
      if (gsaLoadNode.LoadCaseIndex.IsIndex()) speckleNodeLoad.loadCase = GetLoadCaseFromIndex(gsaLoadNode.LoadCaseIndex.Value);
      if (gsaLoadNode.GlobalAxis) speckleNodeLoad.loadAxis = GlobalAxis();
      else if (gsaLoadNode.AxisIndex.IsIndex()) speckleNodeLoad.loadAxis = GetAxisFromIndex(gsaLoadNode.AxisIndex.Value);

      return new ToSpeckleResult(speckleNodeLoad);

      //TODO:
      //SpeckleObject:
      //  string units
    }

    private ToSpeckleResult GsaLoadGravityLoadToSpeckle(GsaRecord nativeObject, GSALayer layer = GSALayer.Both)
    {
      //Two different objects are required, one for the analysis layer and one for the design layer.
      //All conversions will be assigned to local variables first and then the two speckle objects will be created.

      var gsaLoad = (GsaLoadGravity)nativeObject;
      if (layer == GSALayer.Design && !gsaLoad.MemberIndices.HasValues() && gsaLoad.ElementIndices.HasValues()) return new ToSpeckleResult(true); //assume this is meant for the analysis layer only

      //Defaults;
      string applicationId = null;
      GSALoadGravity analysisLoad = null;
      GSALoadCase loadCase = null;

      //local variables
      var nodes = gsaLoad.Nodes.Select(i => (Base)GetNodeFromIndex(i)).ToList();
      var gravityFactors = GetGravityFactors(gsaLoad);
      if (gsaLoad.Index.IsIndex()) applicationId = Instance.GsaModel.Cache.GetApplicationId<GsaLoadGravity>(gsaLoad.Index.Value);
      if (gsaLoad.LoadCaseIndex.IsIndex()) loadCase = GetLoadCaseFromIndex(gsaLoad.LoadCaseIndex.Value);

      //design layer
      var designLoad = new GSALoadGravity()
      {
        applicationId = applicationId,
        nativeId = gsaLoad.Index ?? 0,
        name = gsaLoad.Name,
        loadCase = loadCase,
        nodes = nodes,
        gravityFactors = gravityFactors,
      };
      if (gsaLoad.MemberIndices.HasValues()) designLoad.elements = gsaLoad.MemberIndices.Select(i => GetMemberFromIndex(i)).ToList();

      AddToMeaningfulNodeIndices(nodes.Select(n => n.applicationId));

      if (layer == GSALayer.Both)
      {
        //analysis layer
        analysisLoad = new GSALoadGravity()
        {
          applicationId = applicationId,
          nativeId = gsaLoad.Index ?? 0,
          name = gsaLoad.Name,
          loadCase = loadCase,
          nodes = nodes,
          gravityFactors = gravityFactors,
        };
        if (gsaLoad.ElementIndices.HasValues()) analysisLoad.elements = gsaLoad.ElementIndices.Select(i => GetElementFromIndex(i)).ToList();

      }

      return new ToSpeckleResult(designLayerOnlyObjects: new List<Base>() { designLoad }, analysisLayerOnlyObjects: new List<Base>() { analysisLoad });

      //TODO:
      //SpeckleObject:
      //  string units
    }

    private ToSpeckleResult GsaLoadThermal2dToSpeckle(GsaRecord nativeObject, GSALayer layer = GSALayer.Both)
    {
      //Two different objects are required, one for the analysis layer and one for the design layer.
      //All conversions will be assigned to local variables first and then the two speckle objects will be created.

      var gsaLoad = (GsaLoad2dThermal)nativeObject;
      if (layer == GSALayer.Design && !gsaLoad.MemberIndices.HasValues() && gsaLoad.ElementIndices.HasValues()) return new ToSpeckleResult(true); //assume this is meant for the analysis layer only

      //Defaults;
      string applicationId = null;
      GSALoadThermal2d analysisLoad = null;
      GSALoadCase loadCase = null;

      //local variables
      var type = gsaLoad.Type.ToSpeckle();
      if (gsaLoad.Index.IsIndex()) applicationId = Instance.GsaModel.Cache.GetApplicationId<GsaLoad2dThermal>(gsaLoad.Index.Value);
      if (gsaLoad.LoadCaseIndex.IsIndex()) loadCase = GetLoadCaseFromIndex(gsaLoad.LoadCaseIndex.Value);

      //design layer
      var designLoad = new GSALoadThermal2d()
      {
        applicationId = applicationId,
        nativeId = gsaLoad.Index ?? 0,
        name = gsaLoad.Name,
        loadCase = loadCase,
        type = type,
        values = gsaLoad.Values,
      };
      if (gsaLoad.MemberIndices.HasValues()) designLoad.elements = gsaLoad.MemberIndices.Select(i => (Element2D)GetMemberFromIndex(i)).ToList();

      if (layer == GSALayer.Both)
      {
        //analysis layer
        analysisLoad = new GSALoadThermal2d()
        {
          applicationId = applicationId,
          nativeId = gsaLoad.Index ?? 0,
          name = gsaLoad.Name,
          loadCase = loadCase,
          type = type,
          values = gsaLoad.Values,
        };
        if (gsaLoad.ElementIndices.HasValues()) analysisLoad.elements = gsaLoad.ElementIndices.Select(i => (Element2D)GetElement2DFromIndex(i)).ToList();
      }

      return new ToSpeckleResult(designLayerOnlyObjects: new List<Base>() { designLoad }, analysisLayerOnlyObjects: new List<Base>() { analysisLoad });

      //TODO:
      //SpeckleObject:
      //  string units
    }

    private ToSpeckleResult GsaLoadGridAreaToSpeckle(GsaRecord nativeObject, GSALayer layer = GSALayer.Both)
    {
      //Two different objects are required, one for the analysis layer and one for the design layer.
      //All conversions will be assigned to local variables first and then the two speckle objects will be created.

      var gsaLoad = (GsaLoadGridArea)nativeObject;

      //Defaults
      GSALoadGridArea analysisLoad = null;
      GSAGridSurface designGridSurface = null, analysisGridSurface = null;
      GSALoadCase loadCase = null;
      string applicationId = null;

      //local variables
      var loadAxis = GetAxis(gsaLoad.AxisRefType, gsaLoad.AxisIndex);
      var polyline = GetPolyline(gsaLoad.Area, gsaLoad.Polygon, gsaLoad.PolygonIndex);
      var direction = gsaLoad.LoadDirection.ToSpeckle();
      if (gsaLoad.Index.IsIndex()) applicationId = Instance.GsaModel.Cache.GetApplicationId<GsaLoadGridArea>(gsaLoad.Index.Value);
      if (gsaLoad.LoadCaseIndex.IsIndex()) loadCase = GetLoadCaseFromIndex(gsaLoad.LoadCaseIndex.Value);
      if (gsaLoad.GridSurfaceIndex.IsIndex())
      {
        designGridSurface = GetGridSurfaceFromIndex(gsaLoad.GridSurfaceIndex.Value, GSALayer.Design);
        analysisGridSurface = GetGridSurfaceFromIndex(gsaLoad.GridSurfaceIndex.Value, GSALayer.Analysis);
      }

      if (layer == GSALayer.Design && designGridSurface == null) return new ToSpeckleResult(true); //assume this is meant for the analysis layer only

      //design layer
      var designLoad = new GSALoadGridArea()
      {
        applicationId = applicationId,
        nativeId = gsaLoad.Index ?? 0,
        name = gsaLoad.Name,
        loadCase = loadCase,
        gridSurface = designGridSurface,
        loadAxis = loadAxis,
        isProjected = gsaLoad.Projected,
        direction = direction,
        polyline = polyline,
        value = gsaLoad.Value ?? 0,
      };

      if (layer == GSALayer.Both)
      {
        //analysis layer
        analysisLoad = new GSALoadGridArea()
        {
          applicationId = applicationId,
          nativeId = gsaLoad.Index ?? 0,
          name = gsaLoad.Name,
          loadCase = loadCase,
          gridSurface = analysisGridSurface,
          loadAxis = loadAxis,
          isProjected = gsaLoad.Projected,
          direction = direction,
          polyline = polyline,
          value = gsaLoad.Value ?? 0,
        };
      }

      var toSpeckleResult = (analysisLoad == null) ? new ToSpeckleResult(designLayerOnlyObjects: new List<Base>() { designLoad })
              : new ToSpeckleResult(designLayerOnlyObjects: new List<Base>() { designLoad }, analysisLayerOnlyObjects: new List<Base>() { analysisLoad });
      return toSpeckleResult;

      //TODO:
      //SpeckleObject:
      //  string units
    }

    private ToSpeckleResult GsaLoadGridLineToSpeckle(GsaRecord nativeObject, GSALayer layer = GSALayer.Both)
    {
      //Two different objects are required, one for the analysis layer and one for the design layer.
      //All conversions will be assigned to local variables first and then the two speckle objects will be created.

      var gsaLoad = (GsaLoadGridLine)nativeObject;

      //Defaults
      GSALoadGridLine analysisLoad = null;
      GSAGridSurface designGridSurface = null, analysisGridSurface = null;
      GSALoadCase loadCase = null;
      string applicationId = null;
      List<double> values = null;

      //local variables
      var loadAxis = GetAxis(gsaLoad.AxisRefType, gsaLoad.AxisIndex);
      var polyline = GetPolyline(gsaLoad.Line, gsaLoad.Polygon, gsaLoad.PolygonIndex);
      var direction = gsaLoad.LoadDirection.ToSpeckle();
      if (gsaLoad.Index.IsIndex()) applicationId = Instance.GsaModel.Cache.GetApplicationId<GsaLoadGridLine>(gsaLoad.Index.Value);
      if (gsaLoad.LoadCaseIndex.IsIndex()) loadCase = GetLoadCaseFromIndex(gsaLoad.LoadCaseIndex.Value);
      if (gsaLoad.Value1.HasValue && gsaLoad.Value2.HasValue) values = new List<double>() { gsaLoad.Value1.Value, gsaLoad.Value2.Value };
      if (gsaLoad.GridSurfaceIndex.IsIndex())
      {
        designGridSurface = GetGridSurfaceFromIndex(gsaLoad.GridSurfaceIndex.Value, GSALayer.Design);
        analysisGridSurface = GetGridSurfaceFromIndex(gsaLoad.GridSurfaceIndex.Value, GSALayer.Analysis);
      }

      if (layer == GSALayer.Design && designGridSurface == null) return new ToSpeckleResult(true); //assume this is meant for the analysis layer only

      //design layer
      var designLoad = new GSALoadGridLine()
      {
        applicationId = applicationId,
        nativeId = gsaLoad.Index ?? 0,
        name = gsaLoad.Name,
        loadCase = loadCase,
        gridSurface = designGridSurface,
        loadAxis = loadAxis,
        isProjected = gsaLoad.Projected,
        direction = direction,
        polyline = polyline,
        values = values,
      };

      if (layer == GSALayer.Both)
      {
        //analysis layer
        analysisLoad = new GSALoadGridLine()
        {
          applicationId = applicationId,
          nativeId = gsaLoad.Index ?? 0,
          name = gsaLoad.Name,
          loadCase = loadCase,
          gridSurface = analysisGridSurface,
          loadAxis = loadAxis,
          isProjected = gsaLoad.Projected,
          direction = direction,
          polyline = polyline,
          values = values,
        };
      }

      var toSpeckleResult = (analysisLoad == null) ? new ToSpeckleResult(designLayerOnlyObjects: new List<Base>() { designLoad })
              : new ToSpeckleResult(designLayerOnlyObjects: new List<Base>() { designLoad }, analysisLayerOnlyObjects: new List<Base>() { analysisLoad });
      return toSpeckleResult;

      //TODO:
      //SpeckleObject:
      //  string units
    }

    private ToSpeckleResult GsaLoadGridPointToSpeckle(GsaRecord nativeObject, GSALayer layer = GSALayer.Both)
    {
      //Two different objects are required, one for the analysis layer and one for the design layer.
      //All conversions will be assigned to local variables first and then the two speckle objects will be created.

      var gsaLoad = (GsaLoadGridPoint)nativeObject;

      //Defaults
      GSALoadGridPoint analysisLoad = null;
      GSAGridSurface designGridSurface = null, analysisGridSurface = null;
      GSALoadCase loadCase = null;
      Point position = null;
      string applicationId = null;

      //local variables
      var loadAxis = GetAxis(gsaLoad.AxisRefType, gsaLoad.AxisIndex);
      var direction = gsaLoad.LoadDirection.ToSpeckle();
      if (gsaLoad.Index.IsIndex()) applicationId = Instance.GsaModel.Cache.GetApplicationId<GsaLoadGridPoint>(gsaLoad.Index.Value);
      if (gsaLoad.LoadCaseIndex.IsIndex()) loadCase = GetLoadCaseFromIndex(gsaLoad.LoadCaseIndex.Value);
      if (gsaLoad.X.HasValue && gsaLoad.Y.HasValue) position = new Point(gsaLoad.X.Value, gsaLoad.Y.Value, 0);
      if (gsaLoad.GridSurfaceIndex.IsIndex())
      {
        designGridSurface = GetGridSurfaceFromIndex(gsaLoad.GridSurfaceIndex.Value, GSALayer.Design);
        analysisGridSurface = GetGridSurfaceFromIndex(gsaLoad.GridSurfaceIndex.Value, GSALayer.Analysis);
      }

      if (layer == GSALayer.Design && designGridSurface == null) return new ToSpeckleResult(true); //assume this is meant for the analysis layer only

      //design layer
      var designLoad = new GSALoadGridPoint()
      {
        applicationId = applicationId,
        nativeId = gsaLoad.Index ?? 0,
        name = gsaLoad.Name,
        loadCase = loadCase,
        gridSurface = designGridSurface,
        loadAxis = loadAxis,
        direction = direction,
        value = gsaLoad.Value ?? 0,
        position = position,
      };

      if (layer == GSALayer.Both)
      {
        //analysis layer
        analysisLoad = new GSALoadGridPoint()
        {
          applicationId = applicationId,
          nativeId = gsaLoad.Index ?? 0,
          name = gsaLoad.Name,
          loadCase = loadCase,
          gridSurface = analysisGridSurface,
          loadAxis = loadAxis,
          direction = direction,
          value = gsaLoad.Value ?? 0,
          position = position,
        };
      }

      var toSpeckleResult = (analysisLoad == null) ? new ToSpeckleResult(designLayerOnlyObjects: new List<Base>() { designLoad })
              : new ToSpeckleResult(designLayerOnlyObjects: new List<Base>() { designLoad }, analysisLayerOnlyObjects: new List<Base>() { analysisLoad });
      return toSpeckleResult;

      //TODO:
      //SpeckleObject:
      //  string units
    }
    #endregion

    #region Materials

    private ToSpeckleResult GsaMaterialSteelToSpeckle(GsaRecord nativeObject, GSALayer layer = GSALayer.Both)
    {
      //Currently only handles isotropic steel properties.

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
        nativeId = gsaSteel.Index ?? 0,
        name = gsaSteel.Name,
        grade = "",                                 //grade can be determined from gsaMatSteel.Mat.Name (assuming the user doesn't change the default value): e.g. "350(AS3678)"
        materialType = MaterialType.Steel,
        designCode = "",                            //designCode can be determined from SPEC_STEEL_DESIGN gwa keyword
        codeYear = "",                              //codeYear can be determined from SPEC_STEEL_DESIGN gwa keyword
      };
      if (gsaSteel.Index.IsIndex()) speckleSteel.applicationId = Instance.GsaModel.Cache.GetApplicationId<GsaMatSteel>(gsaSteel.Index.Value);
      if (gsaSteel.Fy.IsPositive()) speckleSteel.yieldStrength = gsaSteel.Fy.Value;
      if (gsaSteel.Fu.IsPositive()) speckleSteel.ultimateStrength = gsaSteel.Fu.Value;
      if (gsaSteel.Mat.Eps.IsPositive()) speckleSteel.maxStrain = gsaSteel.Mat.Eps.Value;
      if (gsaSteel.Eh.HasValue) speckleSteel.strainHardeningModulus = gsaSteel.Eh.Value;

      //the following properties are stored in multiple locations in GSA
      if (Choose(gsaSteel.Mat.E, gsaSteel.Mat.Prop == null ? null : gsaSteel.Mat.Prop.E, out var E)) speckleSteel.elasticModulus = E;
      if (Choose(gsaSteel.Mat.Nu, gsaSteel.Mat.Prop == null ? null : gsaSteel.Mat.Prop.Nu, out var Nu)) speckleSteel.poissonsRatio = Nu;
      if (Choose(gsaSteel.Mat.G, gsaSteel.Mat.Prop == null ? null : gsaSteel.Mat.Prop.G, out var G)) speckleSteel.shearModulus = G;
      if (Choose(gsaSteel.Mat.Rho, gsaSteel.Mat.Prop == null ? null : gsaSteel.Mat.Prop.Rho, out var Rho)) speckleSteel.density = Rho;
      if (Choose(gsaSteel.Mat.Alpha, gsaSteel.Mat.Prop == null ? null : gsaSteel.Mat.Prop.Alpha, out var Alpha)) speckleSteel.thermalExpansivity = Alpha;

      //the following properties are not part of the schema
      speckleSteel["Mat"] = GetMat(gsaSteel.Mat);

      return new ToSpeckleResult(speckleSteel);
    }

    private ToSpeckleResult GsaMaterialConcreteToSpeckle(GsaRecord nativeObject, GSALayer layer = GSALayer.Both)
    {
      //Currently only handles isotropic concrete properties.
      var gsaConcrete = (GsaMatConcrete)nativeObject;
      var speckleConcrete = new GSAConcrete()
      {
        nativeId = gsaConcrete.Index ?? 0,
        name = gsaConcrete.Name,
        grade = "",                                 //grade can be determined from gsaMatConcrete.Mat.Name (assuming the user doesn't change the default value): e.g. "32 MPa"
        materialType = MaterialType.Concrete,
        designCode = "",                            //designCode can be determined from SPEC_CONCRETE_DESIGN gwa keyword: e.g. "AS3600_18" -> "AS3600"
        codeYear = "",                              //codeYear can be determined from SPEC_CONCRETE_DESIGN gwa keyword: e.g. "AS3600_18" - "2018"
        flexuralStrength = 0, //TODO: don't think this is part of the GSA definition
        lightweight = gsaConcrete.Light,
      };
      if (gsaConcrete.Index.IsIndex()) speckleConcrete.applicationId = Instance.GsaModel.Cache.GetApplicationId<GsaMatConcrete>(gsaConcrete.Index.Value);

      //the following properties might be null
      if (gsaConcrete.Fc.HasValue) speckleConcrete.compressiveStrength = gsaConcrete.Fc.Value;
      if (gsaConcrete.EpsU.HasValue) speckleConcrete.maxCompressiveStrain = gsaConcrete.EpsU.Value;
      if (gsaConcrete.Agg.HasValue) speckleConcrete.maxAggregateSize = gsaConcrete.Agg.Value;
      if (gsaConcrete.Fcdt.HasValue) speckleConcrete.tensileStrength = gsaConcrete.Fcdt.Value;
      if (gsaConcrete.Mat.Sls != null && gsaConcrete.Mat.Sls.StrainFailureTension.HasValue) speckleConcrete.maxTensileStrain = gsaConcrete.Mat.Sls.StrainFailureTension.Value;

      //the following properties are stored in multiple locations in GSA
      if (Choose(gsaConcrete.Mat.E, gsaConcrete.Mat.Prop == null ? null : gsaConcrete.Mat.Prop.E, out var E)) speckleConcrete.elasticModulus = E;
      if (Choose(gsaConcrete.Mat.Nu, gsaConcrete.Mat.Prop == null ? null : gsaConcrete.Mat.Prop.Nu, out var Nu)) speckleConcrete.poissonsRatio = Nu;
      if (Choose(gsaConcrete.Mat.G, gsaConcrete.Mat.Prop == null ? null : gsaConcrete.Mat.Prop.G, out var G)) speckleConcrete.shearModulus = G;
      if (Choose(gsaConcrete.Mat.Rho, gsaConcrete.Mat.Prop == null ? null : gsaConcrete.Mat.Prop.Rho, out var Rho)) speckleConcrete.density = Rho;
      if (Choose(gsaConcrete.Mat.Alpha, gsaConcrete.Mat.Prop == null ? null : gsaConcrete.Mat.Prop.Alpha, out var Alpha)) speckleConcrete.thermalExpansivity = Alpha;

      //the following properties are not part of the schema
      speckleConcrete["Mat"] = GetMat(gsaConcrete.Mat);
      speckleConcrete["Type"] = gsaConcrete.Type.ToString();
      speckleConcrete["Cement"] = gsaConcrete.Cement.ToString();
      if (gsaConcrete.Fcd.HasValue) speckleConcrete["Fcd"] = gsaConcrete.Fcd.Value;
      if (gsaConcrete.Fcdc.HasValue) speckleConcrete["Fcdc"] = gsaConcrete.Fcdc.Value;
      if (gsaConcrete.Fcfib.HasValue) speckleConcrete["Fcfib"] = gsaConcrete.Fcfib.Value;
      if (gsaConcrete.EmEs.HasValue) speckleConcrete["EmEs"] = gsaConcrete.EmEs.Value;
      if (gsaConcrete.N.HasValue) speckleConcrete["N"] = gsaConcrete.N.Value;
      if (gsaConcrete.Emod.HasValue) speckleConcrete["Emod"] = gsaConcrete.Emod.Value;
      if (gsaConcrete.Eps.HasValue) speckleConcrete["Eps"] = gsaConcrete.Eps.Value;
      if (gsaConcrete.EpsPeak.HasValue) speckleConcrete["EpsPeak"] = gsaConcrete.EpsPeak.Value;
      if (gsaConcrete.EpsMax.HasValue) speckleConcrete["EpsMax"] = gsaConcrete.EpsMax.Value;
      if (gsaConcrete.EpsAx.HasValue) speckleConcrete["EpsAx"] = gsaConcrete.EpsAx.Value;
      if (gsaConcrete.EpsTran.HasValue) speckleConcrete["EpsTran"] = gsaConcrete.EpsTran.Value;
      if (gsaConcrete.EpsAxs.HasValue) speckleConcrete["EpsAxs"] = gsaConcrete.EpsAxs.Value;
      if (gsaConcrete.XdMin.HasValue) speckleConcrete["XdMin"] = gsaConcrete.XdMin.Value;
      if (gsaConcrete.XdMax.HasValue) speckleConcrete["XdMax"] = gsaConcrete.XdMax.Value;
      if (gsaConcrete.Beta.HasValue) speckleConcrete["Beta"] = gsaConcrete.Beta.Value;
      if (gsaConcrete.Shrink.HasValue) speckleConcrete["Shrink"] = gsaConcrete.Shrink.Value;
      if (gsaConcrete.Confine.HasValue) speckleConcrete["Confine"] = gsaConcrete.Confine.Value;
      if (gsaConcrete.Fcc.HasValue) speckleConcrete["Fcc"] = gsaConcrete.Fcc.Value;
      if (gsaConcrete.EpsPlasC.HasValue) speckleConcrete["EpsPlasC"] = gsaConcrete.EpsPlasC.Value;
      if (gsaConcrete.EpsUC.HasValue) speckleConcrete["EpsUC"] = gsaConcrete.EpsUC.Value;

      return new ToSpeckleResult(speckleConcrete);
    }

    //TODO: Timber: GSA keyword not supported yet
    #endregion

    #region Property
    private ToSpeckleResult GsaSectionToSpeckle(GsaRecord nativeObject, GSALayer layer = GSALayer.Both)
    {
      var gsaSection = (GsaSection)nativeObject;
      //TODO: update code to handle modifiers once SECTION_MOD (or SECTION_ANAL) keyword is supported
      var speckleProperty1D = new GSAProperty1D()
      {
        nativeId = gsaSection.Index ?? 0,
        name = gsaSection.Name,
        memberType = gsaSection.Type.ToSpeckle(),
        referencePoint = gsaSection.ReferencePoint.ToSpeckle(),
        offsetY = gsaSection.RefY ?? 0,
        offsetZ = gsaSection.RefZ ?? 0,
        colour = gsaSection.Colour.ToString(),
        additionalMass = gsaSection.Mass ?? 0,
        cost = gsaSection.Cost ?? 0,
        poolRef = gsaSection.PoolIndex,
      };
      if (gsaSection.Index.IsIndex())
      {
        speckleProperty1D.applicationId = Instance.GsaModel.Cache.GetApplicationId<GsaSection>(gsaSection.Index.Value);
      }
      var gsaSectionComp = (SectionComp)gsaSection.Components.Find(x => x.GetType() == typeof(SectionComp));
      if (gsaSectionComp.MatAnalIndex.IsIndex()) //TODO: intention is to use this to convert MAT_ANAL to a material, but this is not currently possible
      {
        speckleProperty1D.material = null;
        ConversionErrors.Add(new Exception("GsaSectionToSpeckle: Conversion of MAT_ANAL keyword not currently supported"));
      }
      if (gsaSectionComp.MaterialIndex.IsIndex())
      {
        speckleProperty1D.designMaterial = GetMaterialFromIndex(gsaSectionComp.MaterialIndex.Value, gsaSectionComp.MaterialType);
      }
      var fns = new Dictionary<Section1dProfileGroup, Func<ProfileDetails, SectionProfile>>
      { { Section1dProfileGroup.Catalogue, GetProfileCatalogue },
        { Section1dProfileGroup.Explicit, GetProfileExplicit },
        { Section1dProfileGroup.Perimeter, GetProfilePerimeter },
        { Section1dProfileGroup.Standard, GetProfileStandard }
      };
      if (gsaSectionComp.ProfileDetails != null && fns.ContainsKey(gsaSectionComp.ProfileGroup))
      {
        speckleProperty1D.profile = fns[gsaSectionComp.ProfileGroup](gsaSectionComp.ProfileDetails);
      }

      return new ToSpeckleResult(speckleProperty1D);
      //TODO:
      //SpeckleObject:
      //  Material designMaterial - MAT_ANAL keyword is not converted to speckle. MAT_STEEL and MAT_CONCRETE are currently the only ones converted to speckle
      //NativeObject: - leave for now. Will address these later when the schema is updated.
      //  double? Fraction
      //  double? Left
      //  double? Right
      //  double? Slab
      //  List<GsaSectionComponentBase> Components
      //  -GsaSectionComp
      //    string Name
      //    int? MatAnalIndex
      //    double? OffsetY - I don't think this is used as RefY is used instead
      //    double? OffsetZ - I don't think this is used as RefZ is used instead
      //    double? Rotation
      //    ComponentReflection Reflect
      //    int? Pool
      //    Section1dTaperType TaperType
      //    double? TaperPos
      //  -GsaSectionSteel
      //    int? GradeIndex
      //    double? PlasElas
      //    double? NetGross
      //    double? Exposed
      //    double? Beta
      //    SectionSteelSectionType Type
      //    SectionSteelPlateType Plate
      //    bool Locked


      //Interim schema keywords not currently supported
      //  GsaSectionConc
      //  GsaSectionCover
      //  GsaSectionLink
      //  GsaSectionTmpl
    }

    private ToSpeckleResult GsaProperty2dToSpeckle(GsaRecord nativeObject, GSALayer layer = GSALayer.Both)
    {
      var gsaProp2d = (GsaProp2d)nativeObject;
      var speckleProperty2d = new GSAProperty2D()
      {
        nativeId = gsaProp2d.Index ?? 0,
        name = gsaProp2d.Name,
        colour = gsaProp2d.Colour.ToString(),
        thickness = gsaProp2d.Thickness ?? 0,
        zOffset = gsaProp2d.RefZ,
        orientationAxis = GetOrientationAxis(gsaProp2d.AxisRefType, gsaProp2d.AxisIndex),
        refSurface = gsaProp2d.RefPt.ToSpeckle(),
        type = gsaProp2d.Type.ToSpeckle(),
        modifierInPlane = gsaProp2d.InPlaneStiffnessPercentage ?? 0, //Only supporting Percentage modifiers
        modifierBending = gsaProp2d.BendingStiffnessPercentage ?? 0,
        modifierShear = gsaProp2d.ShearStiffnessPercentage ?? 0,
        modifierVolume = gsaProp2d.VolumePercentage ?? 0,
        additionalMass = gsaProp2d.Mass,
        concreteSlabProp = gsaProp2d.Profile, 
        cost = 0, //not part of GSA definition
      };
      if (gsaProp2d.Index.IsIndex()) speckleProperty2d.applicationId = Instance.GsaModel.Cache.GetApplicationId<GsaProp2d>(gsaProp2d.Index.Value);
      if (gsaProp2d.GradeIndex.IsIndex()) speckleProperty2d.designMaterial = GetMaterialFromIndex(gsaProp2d.GradeIndex.Value, gsaProp2d.MatType);
      if (gsaProp2d.AnalysisMaterialIndex.IsIndex())
      {
        speckleProperty2d.material = null;
        ConversionErrors.Add(new Exception("GsaProperty2dToSpeckle: Conversion of MAT_ANAL keyword not currently supported"));
      }
      if (gsaProp2d.DesignIndex.IsIndex()) ConversionErrors.Add(new Exception("GsaProperty2dToSpeckle: Conversion of PROP_RC2D keyword not currently supported"));

      return new ToSpeckleResult(speckleProperty2d);

      //TODO:
      //SpeckleObject:
      //  double cost
      //NativeObject:
      //  int? DesignIndex
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
      if (gsaPropMass.Index.IsIndex()) specklePropertyMass.applicationId = Instance.GsaModel.Cache.GetApplicationId<GsaPropMass>(gsaPropMass.Index.Value);

      //Mass modifications
      if (gsaPropMass.Mod != MassModification.NotSet)
      {
        if (gsaPropMass.Mod == MassModification.Modified) specklePropertyMass.massModified = true;
        else if (gsaPropMass.Mod == MassModification.Defined) specklePropertyMass.massModified = false;
        if (gsaPropMass.ModXPercentage.IsPositive()) specklePropertyMass.massModifierX = gsaPropMass.ModXPercentage.Value;
        if (gsaPropMass.ModYPercentage.IsPositive()) specklePropertyMass.massModifierY = gsaPropMass.ModYPercentage.Value;
        if (gsaPropMass.ModZPercentage.IsPositive()) specklePropertyMass.massModifierZ = gsaPropMass.ModZPercentage.Value;
      }

      return new ToSpeckleResult(specklePropertyMass);
    }

    private ToSpeckleResult GsaPropertySpringToSpeckle(GsaRecord nativeObject, GSALayer layer = GSALayer.Both)
    {
      var gsaPropSpr = (GsaPropSpr)nativeObject;
      var specklePropertySpring = new PropertySpring()
      {
        name = gsaPropSpr.Name,
        
      };
      if (gsaPropSpr.Index.IsIndex()) specklePropertySpring.applicationId = Instance.GsaModel.Cache.GetApplicationId<GsaPropSpr>(gsaPropSpr.Index.Value);
      if (gsaPropSpr.DampingRatio.HasValue) specklePropertySpring.dampingRatio = gsaPropSpr.DampingRatio.Value;

      //Dictionary of fns used to apply spring type specific properties. 
      //Functions will pass by reference specklePropertySpring and make the necessary changes to it
      var fns = new Dictionary<StructuralSpringPropertyType, Func<GsaPropSpr, PropertySpring, bool>>
      { { StructuralSpringPropertyType.Axial, SetPropertySpringAxial },
        { StructuralSpringPropertyType.Torsional, SetPropertySpringTorsional },
        { StructuralSpringPropertyType.Compression, SetPropertySpringCompression },
        { StructuralSpringPropertyType.Tension, SetPropertySpringTension },
        { StructuralSpringPropertyType.Lockup, SetPropertySpringLockup },
        { StructuralSpringPropertyType.Gap, SetPropertySpringGap },
        { StructuralSpringPropertyType.Friction, SetPropertySpringFriction },
        { StructuralSpringPropertyType.General, SetPropertySpringGeneral }
        //CONNECT not yet supported
        //MATRIX not yet supported
      };
      //Apply spring type specific properties
      if (fns.ContainsKey(gsaPropSpr.PropertyType)) fns[gsaPropSpr.PropertyType](gsaPropSpr, specklePropertySpring);
      else
      {
        ConversionErrors.Add(new Exception("GsaPropertySpringToSpeckle: spring type (" + gsaPropSpr.PropertyType.ToString() + ") is not currently supported"));
      }

      return new ToSpeckleResult(specklePropertySpring);

      //TODO:
      //SpeckleObject:
      //  double dampingX - spring property can't be a damper. PROP_DAMP not yet supported. Can be removed
      //  double dampingY
      //  double dampingZ
      //  double dampingXX
      //  double dampingYY
      //  double dampingZZ
      //  double matrix - matrix option not currently supported in interim schema. Additionally SPR_MATRIX not supported. This would actually be a matrix, not a single value
      //NativeObject:
      //  Colour Colour - not supported
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
            description = "",
            permutation = "",
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

      //TODO:
      //SpeckleObject:
      //  string permutation
      //  string description
    }

    public bool GsaElement1dResultToSpeckle(int gsaElementIndex, Element1D speckleElement, out List<Result1D> speckleResults)
    {
      speckleResults = null;
      if (Instance.GsaModel.Proxy.GetResultRecords(ResultGroup.Element1d, gsaElementIndex, out var csvRecord))
      {
        speckleResults = new List<Result1D>();
        var gsaElement1dResults = csvRecord.FindAll(so => so is CsvElem1d).Select(so => (CsvElem1d)so).ToList();
        foreach (var gsaResult in gsaElement1dResults)
        {
          var result = new Result1D()
          {
            description = "",
            permutation = "",
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

      //TODO:
      //SpeckleObject:
      //  string permutation
      //  string description
      //  float? rotXX
      //  float? rotYY
      //  float? rotZZ
      //  float? axialStress
      //  float? shearStressY
      //  float? shearStressZ
      //  float? bendingStressYPos
      //  float? bendingStressYNeg
      //  float? bendingStressZPos
      //  float? bendingStressZNeg
      //  float? combinedStressMax
      //  float? combinedStressMin
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
            description = "",
            permutation = "",
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

      //TODO:
      //SpeckleObject:
      //  string permutation
      //  string description
    }

    public bool GsaElement3dResultToSpeckle(int gsaElementIndex, Element3D speckleElement, out List<Result3D> speckleResults)
    {
      //TO DO: update when 3D elements are supported
      speckleResults = null;
      return false;
    }

    //TODO: implement conversion code for result objects
    //CsvAssembly
    /* ResultGlobal
     */
    #endregion

    #region Constraints
    private ToSpeckleResult GsaRigidToSpeckle(GsaRecord nativeObject, GSALayer layer = GSALayer.Both)
    {
      //Two different objects are required, one for the analysis layer and one for the design layer.
      //All conversions will be assigned to local variables first and then the two speckle objects will be created.

      var gsaRigid = (GsaRigid)nativeObject;

      //Defaults
      GSARigidConstraint analysisRigid = null;
      Node primaryNode = null;
      List<Node> constrainedNodes = null;
      List<GSAStage> designStages = null, analysisStages = null;
      Dictionary<AxisDirection6, List<AxisDirection6>> constraintCondition = null;
      string applicationId = null;
      Base parentMember = null;

      //local variables
      if (gsaRigid.Index.IsIndex()) applicationId = Instance.GsaModel.Cache.GetApplicationId<GsaRigid>(gsaRigid.Index.Value);
      if (gsaRigid.PrimaryNode.IsIndex())
      {
        primaryNode = GetNodeFromIndex(gsaRigid.PrimaryNode.Value);
        AddToMeaningfulNodeIndices(primaryNode.applicationId);
      }
      if (gsaRigid.Type == RigidConstraintType.Custom) constraintCondition = GetRigidConstraint(gsaRigid.Link);
      if (gsaRigid.ParentMember.HasValue && gsaRigid.ParentMember > 0) parentMember = GetMemberFromIndex(gsaRigid.ParentMember.Value);
      if (gsaRigid.ConstrainedNodes.HasValues())
      {
        constrainedNodes = gsaRigid.ConstrainedNodes.Select(i => GetNodeFromIndex(i)).ToList();
        AddToMeaningfulNodeIndices(constrainedNodes.Select(cn => cn.applicationId));
      }
      if (gsaRigid.Stage.HasValues())
      {
        designStages = gsaRigid.Stage.Select(i => GetStageFromIndex(i, GSALayer.Design)).ToList();
        analysisStages = gsaRigid.Stage.Select(i => GetStageFromIndex(i, GSALayer.Analysis)).ToList();
      }
      var type = gsaRigid.Type.ToSpeckle();

      if (layer == GSALayer.Design && designStages == null) return new ToSpeckleResult(true); //assume this is meant for the analysis layer only

      //design layer
      var designRigid = new GSARigidConstraint()
      {
        applicationId = applicationId,
        nativeId = gsaRigid.Index ?? 0,
        name = gsaRigid.Name,
        primaryNode = primaryNode,
        constrainedNodes = constrainedNodes,
        stages = designStages,
        type = type,
        constraintCondition = constraintCondition,
        parentMember = parentMember,
      };

      if (layer == GSALayer.Both)
      {
        //analysis layer
        analysisRigid = new GSARigidConstraint()
        {
          applicationId = applicationId,
          nativeId = gsaRigid.Index ?? 0,
          name = gsaRigid.Name,
          primaryNode = primaryNode,
          constrainedNodes = constrainedNodes,
          stages = analysisStages,
          type = type,
          constraintCondition = constraintCondition,
          parentMember = parentMember,
        };
      }

      var toSpeckleResult = (analysisRigid == null) ? new ToSpeckleResult(designLayerOnlyObjects: new List<Base>() { designRigid })
              : new ToSpeckleResult(designLayerOnlyObjects: new List<Base>() { designRigid }, analysisLayerOnlyObjects: new List<Base>() { analysisRigid });
      return toSpeckleResult;
    }

    private ToSpeckleResult GsaGenRestToSpeckle(GsaRecord nativeObject, GSALayer layer = GSALayer.Both)
    {
      //Two different objects are required, one for the analysis layer and one for the design layer.
      //All conversions will be assigned to local variables first and then the two speckle objects will be created.

      var gsaGenRest = (GsaGenRest)nativeObject;

      //Defaults
      GSAGeneralisedRestraint analysisGenRest = null;
      List<Node> nodes = null;
      List<GSAStage> designStages = null, analysisStages = null;
      string applicationId = null;

      //local variables
      if (gsaGenRest.Index.IsIndex()) applicationId = Instance.GsaModel.Cache.GetApplicationId<GsaGenRest>(gsaGenRest.Index.Value);
      if (gsaGenRest.NodeIndices.HasValues())
      {
        nodes = gsaGenRest.NodeIndices.Select(i => GetNodeFromIndex(i)).ToList();
        AddToMeaningfulNodeIndices(nodes.Select(n => n.applicationId));
      }
      if (gsaGenRest.StageIndices.HasValues())
      {
        designStages = gsaGenRest.StageIndices.Select(i => GetStageFromIndex(i, GSALayer.Design)).ToList();
        analysisStages = gsaGenRest.StageIndices.Select(i => GetStageFromIndex(i, GSALayer.Analysis)).ToList();
      }

      if (layer == GSALayer.Design && designStages == null) return new ToSpeckleResult(true); //assume this is meant for the analysis layer only

      //design layer
      var designGenRest = new GSAGeneralisedRestraint()
      {
        applicationId = applicationId,
        nativeId = gsaGenRest.Index ?? 0,
        name = gsaGenRest.Name,
        restraint = GetRestraint(gsaGenRest),
        nodes = nodes,
        stages = designStages,
      };

      if (layer == GSALayer.Both)
      {
        //analysis layer
        analysisGenRest = new GSAGeneralisedRestraint()
        {
          applicationId = applicationId,
          nativeId = gsaGenRest.Index ?? 0,
          name = gsaGenRest.Name,
          restraint = GetRestraint(gsaGenRest),
          nodes = nodes,
          stages = analysisStages,
        };
      }

      var toSpeckleResult = (analysisGenRest == null) ? new ToSpeckleResult(designLayerOnlyObjects: new List<Base>() { designGenRest })
              : new ToSpeckleResult(designLayerOnlyObjects: new List<Base>() { designGenRest }, analysisLayerOnlyObjects: new List<Base>() { analysisGenRest });
      return toSpeckleResult;
    }
    #endregion

    #region Analysis Stage
    private ToSpeckleResult GsaStageToSpeckle(GsaRecord nativeObject, GSALayer layer = GSALayer.Both)
    {
      //Two different objects are required, one for the analysis layer and one for the design layer.
      //All conversions will be assigned to local variables first and then the two speckle objects will be created.

      var gsaStage = (GsaAnalStage)nativeObject;
      if (layer == GSALayer.Design && !gsaStage.MemberIndices.HasValues() && gsaStage.ElementIndices.HasValues()) return new ToSpeckleResult(true); //assume this is meant for the analysis layer only

      //Defaults;
      string applicationId = null;
      GSAStage analysisStage = null;

      //local variables
      var colour = gsaStage.Colour.ToString();
      if (gsaStage.Index.IsIndex()) applicationId = Instance.GsaModel.Cache.GetApplicationId<GsaAnalStage>(gsaStage.Index.Value);

      //design layer
      var designStage = new GSAStage()
      {
        applicationId = applicationId,
        nativeId = gsaStage.Index ?? 0,
        name = gsaStage.Name,
        colour = colour,
        creepFactor = gsaStage.Phi ?? 0,
        stageTime = gsaStage.Days ?? 0,
      };
      if (gsaStage.MemberIndices.HasValues()) designStage.elements = gsaStage.MemberIndices.Select(i => GetMemberFromIndex(i)).ToList();
      if (gsaStage.LockMemberIndices.HasValues()) designStage.lockedElements = gsaStage.LockMemberIndices.Select(i => GetMemberFromIndex(i)).ToList();

      if (layer == GSALayer.Both)
      {
        //analysis layer
        analysisStage = new GSAStage()
        {
          applicationId = applicationId,
          nativeId = gsaStage.Index ?? 0,
          name = gsaStage.Name,
          colour = colour,
          creepFactor = gsaStage.Phi ?? 0,
          stageTime = gsaStage.Days ?? 0,
        };
        if (gsaStage.ElementIndices.HasValues()) analysisStage.elements = gsaStage.ElementIndices.Select(i => GetElementFromIndex(i)).ToList();
        if (gsaStage.LockElementIndices.HasValues()) analysisStage.lockedElements = gsaStage.LockElementIndices.Select(i => GetElementFromIndex(i)).ToList();
      }

      return new ToSpeckleResult(designLayerOnlyObjects: new List<Base>() { designStage }, analysisLayerOnlyObjects: new List<Base>() { analysisStage });
    }
    #endregion

    #region Bridge
    private ToSpeckleResult GsaInfBeamToSpeckle(GsaRecord nativeObject, GSALayer layer = GSALayer.Both)
    {
      var gsaInfBeam = (GsaInfBeam)nativeObject;
      var speckleInfBeam = new GSAInfluenceBeam()
      {
        nativeId = gsaInfBeam.Index ?? 0,
        name = gsaInfBeam.Name,
        direction = gsaInfBeam.Direction.ToSpeckleLoad(),
        type = gsaInfBeam.Type.ToSpeckle(),
        factor = gsaInfBeam.Factor ?? 0,
      };
      if (gsaInfBeam.Index.IsIndex()) speckleInfBeam.applicationId = Instance.GsaModel.Cache.GetApplicationId<GsaInfBeam>(gsaInfBeam.Index.Value);
      if (gsaInfBeam.Position.Value >= 0 && gsaInfBeam.Position.Value <= 1) speckleInfBeam.position = gsaInfBeam.Position.Value;
      if (gsaInfBeam.Element.IsIndex()) speckleInfBeam.element = GetElement1DFromIndex(gsaInfBeam.Element.Value);

      return new ToSpeckleResult(speckleInfBeam);
    }

    private ToSpeckleResult GsaInfNodeToSpeckle(GsaRecord nativeObject, GSALayer layer = GSALayer.Both)
    {
      var gsaInfNode = (GsaInfNode)nativeObject;
      var speckleInfNode = new GSAInfluenceNode()
      {
        nativeId = gsaInfNode.Index ?? 0,
        name = gsaInfNode.Name,
        direction = gsaInfNode.Direction.ToSpeckleLoad(),
        type = gsaInfNode.Type.ToSpeckle(),
        axis = GetAxis(gsaInfNode.AxisRefType, gsaInfNode.AxisIndex),
        factor = gsaInfNode.Factor ?? 0,
      };
      if (gsaInfNode.Index.IsIndex())
      {
        speckleInfNode.applicationId = Instance.GsaModel.Cache.GetApplicationId<GsaInfNode>(gsaInfNode.Index.Value);
        speckleInfNode.nativeId = gsaInfNode.Index.Value;
      }
      if (gsaInfNode.Factor.HasValue) speckleInfNode.factor = gsaInfNode.Factor.Value;
      if (gsaInfNode.Node.IsIndex()) speckleInfNode.node = GetNodeFromIndex(gsaInfNode.Node.Value);

      return new ToSpeckleResult(speckleInfNode);
    }

    private ToSpeckleResult GsaAlignToSpeckle(GsaRecord nativeObject, GSALayer layer = GSALayer.Both)
    {
      //Two different objects are required, one for the analysis layer and one for the design layer.
      //All conversions will be assigned to local variables first and then the two speckle objects will be created.

      var gsaAlign = (GsaAlign)nativeObject;

      //Defaults
      GSAGridSurface designGridSurface = null, analysisGridSurface = null;
      GSAAlignment analysisAlignment = null;
      string applicationId = null;

      //local variables
      if (gsaAlign.Index.IsIndex()) applicationId = Instance.GsaModel.Cache.GetApplicationId<GsaAlign>(gsaAlign.Index.Value);
      if (gsaAlign.GridSurfaceIndex.IsIndex())
      {
        designGridSurface = GetGridSurfaceFromIndex(gsaAlign.GridSurfaceIndex.Value, GSALayer.Design);
        analysisGridSurface = GetGridSurfaceFromIndex(gsaAlign.GridSurfaceIndex.Value, GSALayer.Analysis);
      }

      if (layer == GSALayer.Design && designGridSurface == null) return new ToSpeckleResult(true); //assume this is meant for the analysis layer only

      var designAlignment = new GSAAlignment()
      {
        applicationId = applicationId,
        nativeId = gsaAlign.Index ?? 0,
        name = gsaAlign.Name,
        chainage = gsaAlign.Chain,
        curvature = gsaAlign.Curv,
        gridSurface = designGridSurface,
      };

      if (layer == GSALayer.Both)
      {
        analysisAlignment = new GSAAlignment()
        {
          applicationId = applicationId,
          nativeId = gsaAlign.Index ?? 0,
          name = gsaAlign.Name,
          chainage = gsaAlign.Chain,
          curvature = gsaAlign.Curv,
          gridSurface = analysisGridSurface,
        };
      }

      var toSpeckleResult = (analysisAlignment == null) ? new ToSpeckleResult(designLayerOnlyObjects: new List<Base>() { designAlignment })
        : new ToSpeckleResult(designLayerOnlyObjects: new List<Base>() { designAlignment }, analysisLayerOnlyObjects: new List<Base>() { analysisAlignment });
      return toSpeckleResult;
    }

    private ToSpeckleResult GsaPathToSpeckle(GsaRecord nativeObject, GSALayer layer = GSALayer.Both)
    {
      //Two different objects are required, one for the analysis layer and one for the design layer.
      //All conversions will be assigned to local variables first and then the two speckle objects will be created.

      var gsaPath = (GsaPath)nativeObject;

      //Defaults
      GSAAlignment designAlignment = null, analysisAlignment = null;
      GSAPath analysisPath = null;
      string applicationId = null;

      //local variables
      if (gsaPath.Index.IsIndex()) applicationId = Instance.GsaModel.Cache.GetApplicationId<GsaPath>(gsaPath.Index.Value);
      if (gsaPath.Alignment.IsIndex())
      {
        designAlignment = GetAlignmentFromIndex(gsaPath.Alignment.Value, GSALayer.Design);
        analysisAlignment = GetAlignmentFromIndex(gsaPath.Alignment.Value, GSALayer.Analysis);
      }

      if (layer == GSALayer.Design && designAlignment == null) return new ToSpeckleResult(true); //assume this is meant for the analysis layer only

      //design layer
      var designPath = new GSAPath()
      {
        applicationId = applicationId,
        nativeId = gsaPath.Index ?? 0,
        name = gsaPath.Name,
        type = gsaPath.Type.ToSpeckle(),
        group = gsaPath.Group ?? 0,
        left = gsaPath.Left ?? 0,
        right = gsaPath.Right ?? 0,
        factor = gsaPath.Factor ?? 0,
        numMarkedLanes = gsaPath.NumMarkedLanes ?? 0,
        alignment = designAlignment,
      };

      if (layer == GSALayer.Both)
      {
        //analysis layer
        analysisPath = new GSAPath()
        {
          applicationId = applicationId,
          nativeId = gsaPath.Index ?? 0,
          name = gsaPath.Name,
          type = gsaPath.Type.ToSpeckle(),
          group = gsaPath.Group ?? 0,
          left = gsaPath.Left ?? 0,
          right = gsaPath.Right ?? 0,
          factor = gsaPath.Factor ?? 0,
          numMarkedLanes = gsaPath.NumMarkedLanes ?? 0,
          alignment = analysisAlignment,
        };
      }

      var toSpeckleResult = (analysisPath == null) ? new ToSpeckleResult(designLayerOnlyObjects: new List<Base>() { designPath })
        : new ToSpeckleResult(designLayerOnlyObjects: new List<Base>() { designPath }, analysisLayerOnlyObjects: new List<Base>() { analysisPath });
      return toSpeckleResult;
    }

    private ToSpeckleResult GsaUserVehicleToSpeckle(GsaRecord nativeObject, GSALayer layer = GSALayer.Both)
    {
      var gsaUserVehicle = (GsaUserVehicle)nativeObject;
      var speckleUserVehicle = new GSAUserVehicle()
      {
        nativeId = gsaUserVehicle.Index ?? 0,
        name = gsaUserVehicle.Name,
        axlePositions = gsaUserVehicle.AxlePosition,
        axleOffsets = gsaUserVehicle.AxleOffset,
        axleLeft = gsaUserVehicle.AxleLeft,
        axleRight = gsaUserVehicle.AxleRight,
        width = gsaUserVehicle.Width ?? 0,
      };
      if (gsaUserVehicle.Index.IsIndex()) speckleUserVehicle.applicationId = Instance.GsaModel.Cache.GetApplicationId<GsaUserVehicle>(gsaUserVehicle.Index.Value);

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
        var gsaRecord = Instance.GsaModel.Cache.GetNative<GsaPropSpr>(gsaNode.SpringPropertyIndex.Value);
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
    private GSAElement2D GetElement2DFromIndex(int index)
    {
      return (Instance.GsaModel.Cache.GetSpeckleObjects<GsaEl, GSAElement2D>(index, out var speckleObjects, GSALayer.Analysis)) ? speckleObjects.First() : null;
    }

    /// <summary>
    /// Get Speckle Element1D object from GSA element index
    /// </summary>
    /// <param name="index">GSA element index</param>
    /// <returns></returns>
    private GSAElement1D GetElement1DFromIndex(int index)
    {
      return (Instance.GsaModel.Cache.GetSpeckleObjects<GsaEl, GSAElement1D>(index, out var speckleObjects, GSALayer.Analysis)) ? speckleObjects.First() : null;
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
      else if (xdir.DotProduct(normal) > 0.999848 || xdir.DotProduct(normal) < -0.999848) //Vertical element, TODO: what tolerance to use? currently 1 deg
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
      var _vertices = new List<Node>();
      var _faces = new List<int[]>();
      var vertices = new List<double>();

      var topology = gsaNodeIndicies.Select(i => GetNodeFromIndex(i)).ToList();

      var faceIndices = new List<int>();
      foreach (var node in topology)
      {
        vertices.AddRange(new double[] { node.basePoint.x, node.basePoint.y, node.basePoint.z });

        if (!_vertices.Contains(node))
        {
          faceIndices.Add(_vertices.Count);
          _vertices.Add(node);
        }
        else
        {
          faceIndices.Add(_vertices.IndexOf(node));
        }
      }

      if (topology.Count == 4)
      {
        _faces.Add(new int[] { 1, faceIndices[0], faceIndices[1], faceIndices[2], faceIndices[3] });
      }
      else if (topology.Count == 3)
      {
        _faces.Add(new int[] { 0, faceIndices[0], faceIndices[1], faceIndices[2] });
      }

      var faces = _faces.SelectMany(o => o).ToArray();
      var mesh = new Mesh(vertices.ToArray(), faces);

      return mesh;
    }

    private Mesh DisplayMeshPolygon(List<int> gsaNodeIndicies, System.Drawing.Color color = default)
    {      
      var edgeVertices = new List<double>();
      var topology = gsaNodeIndicies.Select(i => GetNodeFromIndex(i)).ToList();
      foreach (var node in topology)
      {
        edgeVertices.AddRange(new double[] { node.basePoint.x, node.basePoint.y, node.basePoint.z });
      }

      var mesher = new PolygonMesher();
      mesher.Init(edgeVertices);
      
      var faces = mesher.Faces().ToList();
      var vertices = mesher.Coordinates.ToList();

      var mesh = new Mesh();
      mesh.faces = faces;
      mesh.vertices = vertices;

      if (color != null)
      {        
        var colors = Enumerable.Repeat(color.ToArgb(), vertices.Count()).ToList();
        mesh.colors = colors;
      }

      return mesh;
    }

    #endregion

    #region Member
    private Base GetMemberFromIndex(int index)
    {
      var gsaMemb = (GsaMemb)Instance.GsaModel.Cache.GetNative<GsaMemb>(index);
      if (gsaMemb != null && gsaMemb.Is1dMember())
      {
        return (Instance.GsaModel.Cache.GetSpeckleObjects<GsaMemb, GSAMember1D>(index, out var speckleObjects, GSALayer.Design)) ? speckleObjects.First() : null;
      }
      else if (gsaMemb != null && gsaMemb.Is2dMember())
      {
        return (Instance.GsaModel.Cache.GetSpeckleObjects<GsaMemb, GSAMember2D>(index, out var speckleObjects, GSALayer.Design)) ? speckleObjects.First() : null;
      }
      else
      {
        if (gsaMemb == null)
        {
          ConversionErrors.Add(new Exception("GetMemberFromIndex: member with index " + index.ToString() + " does not exist."));
        }
        else
        {
          ConversionErrors.Add(new Exception("GetMemberFromIndex: member type (" + gsaMemb.Type.ToString() + ") is not currently supported."));
        }
        return null;
      }
    }

    private Polyline GetBasePolyline(List<Point> points)
    {
      var v = new List<double>();
      foreach (var pt in points)
      {
        v.AddRange(new List<double> { pt.x, pt.y, pt.z });
      }
      return new Polyline(v.ToArray());
    }

    private Line GetBaseLine(List<Point> points)
    {
      return new Line(points[0], points[1]);
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

    private GSAGridSurface GetGridSurfaceFromIndex(int index, GSALayer layer)
    {
      return (Instance.GsaModel.Cache.GetSpeckleObjects<GsaGridSurface, GSAGridSurface>(index, out var speckleObjects, layer)) 
        ? speckleObjects.First() : null;
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
    private List<Base> GetAssemblyEntites(GsaAssembly gsaAssembly)
    {
      List<Base> entities = null;
      switch (gsaAssembly.Type)
      {
        case GSAEntity.ELEMENT:
          if (gsaAssembly.ElementIndices.HasValues()) entities = gsaAssembly.ElementIndices.Select(i => GetElementFromIndex(i)).ToList();
          break;
        case GSAEntity.MEMBER:
          if (gsaAssembly.MemberIndices.HasValues()) entities = gsaAssembly.MemberIndices.Select(i => GetMemberFromIndex(i)).ToList();
          break;
      }
      return entities;
    }

    private List<double> GetAssemblyPoints(GsaAssembly gsaAssembly)
    {
      //Points
      if (gsaAssembly.PointDefn == PointDefinition.Points && gsaAssembly.NumberOfPoints.IsIndex())
      {
        return new List<double>() { gsaAssembly.NumberOfPoints.Value };
      }
      else if (gsaAssembly.PointDefn == PointDefinition.Spacing && gsaAssembly.Spacing.IsPositive())
      {
        return new List<double>() { gsaAssembly.Spacing.Value };
      }
      else if (gsaAssembly.PointDefn == PointDefinition.Storey && gsaAssembly.StoreyIndices != null && gsaAssembly.StoreyIndices.Count > 0)
      {
        return gsaAssembly.StoreyIndices.Select(i=>i.ToDouble()).ToList();
      }
      else if (gsaAssembly.PointDefn == PointDefinition.Explicit && gsaAssembly.ExplicitPositions != null && gsaAssembly.ExplicitPositions.Count > 0)
      {
        return gsaAssembly.ExplicitPositions;
      }
      else
      {
        return null;
      }
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
      //ConversionErrors.Add(new Exception("GetTaskFromIndex: TASK keyword not currently supported"));
      return null;

      //TODO: when TASK is included in interim schema
      //return (Instance.GsaModel.Cache.GetSpeckleObjects<GsaTask, GSATask>(index, out var speckleObjects)) ? speckleObjects.First() : null;
    }

    private List<double> GetLoadBeamPositions(GsaLoadBeam gsaLoadBeam)
    {
      List<double> positions = null;
      var type = gsaLoadBeam.GetType();
      if (type == typeof(GsaLoadBeamPoint))
      {
        positions = new List<double>() { ((GsaLoadBeamPoint)gsaLoadBeam).Position };
      }
      else if (type == typeof(GsaLoadBeamPatch) || type == typeof(GsaLoadBeamPatchTrilin))
      {
        var lb = (GsaLoadBeamPatchTrilin)gsaLoadBeam;
        positions = new List<double>() { lb.Position1, lb.Position2Percent };
      }
      return positions;
    }

    private List<double> GetLoadBeamValues(GsaLoadBeam gsaLoadBeam)
    {
      List<double> values = null;
      double? v;
      var type = gsaLoadBeam.GetType();
      if (type == typeof(GsaLoadBeamPoint))
      {
        v = ((GsaLoadBeamPoint)gsaLoadBeam).Load;
        if (v.HasValue) values = new List<double>() { v.Value };
      }
      else if (type == typeof(GsaLoadBeamUdl))
      {
        v = ((GsaLoadBeamUdl)gsaLoadBeam).Load;
        if (v.HasValue) values = new List<double>() { v.Value };
      }
      else if (type == typeof(GsaLoadBeamLine))
      {
        var lb = (GsaLoadBeamLine)gsaLoadBeam;
        if (lb.Load1.HasValue && lb.Load2.HasValue) values = new List<double>() { lb.Load1.Value, lb.Load2.Value };
      }
      else if (type == typeof(GsaLoadBeamPatch) || type == typeof(GsaLoadBeamPatchTrilin))
      {
        var lb = (GsaLoadBeamPatchTrilin)gsaLoadBeam;
        if (lb.Load1.HasValue && lb.Load2.HasValue) values = new List<double>() { lb.Load1.Value, lb.Load2.Value };
      }
      return values;
    }

    private Vector GetGravityFactors(GsaLoadGravity gsaLoadGravity)
    {
      return new Vector(gsaLoadGravity.X ?? 0, gsaLoadGravity.Y ?? 0, gsaLoadGravity.Z ?? 0);
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
      //TODO: Use desc to deside combination type
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

    private Base GetMat(GsaMat gsaMat)
    {
      if (gsaMat == null) return null;

      var speckleMat = new Base();
      if (gsaMat.Name != null) speckleMat["Name"] = gsaMat.Name;
      if (gsaMat.E.HasValue) speckleMat["E"] = gsaMat.E.Value;
      if (gsaMat.F.HasValue) speckleMat["F"] = gsaMat.F.Value;
      if (gsaMat.Nu.HasValue) speckleMat["Nu"] = gsaMat.Nu.Value;
      if (gsaMat.G.HasValue) speckleMat["G"] = gsaMat.G.Value;
      if (gsaMat.Rho.HasValue) speckleMat["Rho"] = gsaMat.Rho.Value;
      if (gsaMat.Alpha.HasValue) speckleMat["Alpha"] = gsaMat.Alpha.Value;
      speckleMat["Prop"] = GetMatAnal(gsaMat.Prop);
      if (gsaMat.NumUC > 0)
      {
        speckleMat["AbsUC"] = gsaMat.AbsUC.ToString();
        speckleMat["OrdUC"] = gsaMat.OrdUC.ToString();
        speckleMat["PtsUC"] = gsaMat.PtsUC;
      }
      if (gsaMat.NumSC > 0)
      {
        speckleMat["AbsSC"] = gsaMat.AbsSC.ToString();
        speckleMat["OrdSC"] = gsaMat.OrdSC.ToString();
        speckleMat["PtsSC"] = gsaMat.PtsSC;
      }
      if (gsaMat.NumUT > 0)
      {
        speckleMat["AbsUT"] = gsaMat.AbsUT.ToString();
        speckleMat["OrdUT"] = gsaMat.OrdUT.ToString();
        speckleMat["PtsUT"] = gsaMat.PtsUT;
      }
      if (gsaMat.NumST > 0)
      {
        speckleMat["AbsST"] = gsaMat.AbsST.ToString();
        speckleMat["OrdST"] = gsaMat.OrdST.ToString();
        speckleMat["PtsST"] = gsaMat.PtsST;
      }
      if (gsaMat.Eps.HasValue) speckleMat["Eps"] = gsaMat.Eps.Value;
      speckleMat["Uls"] = GetMatCurveParam(gsaMat.Uls);
      speckleMat["Sls"] = GetMatCurveParam(gsaMat.Sls);
      if (gsaMat.Cost.HasValue) speckleMat["Cost"] = gsaMat.Cost.Value;
      speckleMat["Type"] = gsaMat.Type.ToString();
      return speckleMat;
    }

    private Base GetMatAnal(GsaMatAnal gsaMatAnal)
    {
      if (gsaMatAnal == null) return null;

      var speckleMatAnal = new Base();
      if (gsaMatAnal.Name != null) speckleMatAnal["Name"] = gsaMatAnal.Name;
      speckleMatAnal["Index"] = gsaMatAnal.Index;
      speckleMatAnal["Colour"] = gsaMatAnal.Colour.ToString();
      speckleMatAnal["Type"] = gsaMatAnal.Type.ToString();
      if (gsaMatAnal.NumParams.HasValue) speckleMatAnal["NumParams"] = gsaMatAnal.NumParams.Value;
      if (gsaMatAnal.E.HasValue) speckleMatAnal["E"] = gsaMatAnal.E.Value;
      if (gsaMatAnal.Nu.HasValue) speckleMatAnal["Nu"] = gsaMatAnal.Nu.Value;
      if (gsaMatAnal.Rho.HasValue) speckleMatAnal["Rho"] = gsaMatAnal.Rho.Value;
      if (gsaMatAnal.Alpha.HasValue) speckleMatAnal["Alpha"] = gsaMatAnal.Alpha.Value;
      if (gsaMatAnal.G.HasValue) speckleMatAnal["G"] = gsaMatAnal.G.Value;
      if (gsaMatAnal.Damp.HasValue) speckleMatAnal["Damp"] = gsaMatAnal.Damp.Value;
      if (gsaMatAnal.Yield.HasValue) speckleMatAnal["Yield"] = gsaMatAnal.Yield.Value;
      if (gsaMatAnal.Ultimate.HasValue) speckleMatAnal["Ultimate"] = gsaMatAnal.Ultimate.Value;
      if (gsaMatAnal.Eh.HasValue) speckleMatAnal["Eh"] = gsaMatAnal.Eh.Value;
      if (gsaMatAnal.Beta.HasValue) speckleMatAnal["Beta"] = gsaMatAnal.Beta.Value;
      if (gsaMatAnal.Cohesion.HasValue) speckleMatAnal["Cohesion"] = gsaMatAnal.Cohesion.Value;
      if (gsaMatAnal.Phi.HasValue) speckleMatAnal["Phi"] = gsaMatAnal.Phi.Value;
      if (gsaMatAnal.Psi.HasValue) speckleMatAnal["Psi"] = gsaMatAnal.Psi.Value;
      if (gsaMatAnal.Scribe.HasValue) speckleMatAnal["Scribe"] = gsaMatAnal.Scribe.Value;
      if (gsaMatAnal.Ex.HasValue) speckleMatAnal["Ex"] = gsaMatAnal.Ex.Value;
      if (gsaMatAnal.Ey.HasValue) speckleMatAnal["Ey"] = gsaMatAnal.Ey.Value;
      if (gsaMatAnal.Ez.HasValue) speckleMatAnal["Ez"] = gsaMatAnal.Ez.Value;
      if (gsaMatAnal.Nuxy.HasValue) speckleMatAnal["Nuxy"] = gsaMatAnal.Nuxy.Value;
      if (gsaMatAnal.Nuyz.HasValue) speckleMatAnal["Nuyz"] = gsaMatAnal.Nuyz.Value;
      if (gsaMatAnal.Nuzx.HasValue) speckleMatAnal["Nuzx"] = gsaMatAnal.Nuzx.Value;
      if (gsaMatAnal.Alphax.HasValue) speckleMatAnal["Alphax"] = gsaMatAnal.Alphax.Value;
      if (gsaMatAnal.Alphay.HasValue) speckleMatAnal["Alphay"] = gsaMatAnal.Alphay.Value;
      if (gsaMatAnal.Alphaz.HasValue) speckleMatAnal["Alphaz"] = gsaMatAnal.Alphaz.Value;
      if (gsaMatAnal.Gxy.HasValue) speckleMatAnal["Gxy"] = gsaMatAnal.Gxy.Value;
      if (gsaMatAnal.Gyz.HasValue) speckleMatAnal["Gyz"] = gsaMatAnal.Gyz.Value;
      if (gsaMatAnal.Gzx.HasValue) speckleMatAnal["Gzx"] = gsaMatAnal.Gzx.Value;
      if (gsaMatAnal.Comp.HasValue) speckleMatAnal["Comp"] = gsaMatAnal.Comp.Value;
      return speckleMatAnal;
    }

    private Base GetMatCurveParam(GsaMatCurveParam gsaCurve)
    {
      if (gsaCurve == null) return null;

      var speckleCurve = new Base();
      speckleCurve["Name"] = gsaCurve.Name;
      if (gsaCurve.Model != null && gsaCurve.Model.Count > 0) speckleCurve["Model"] = gsaCurve.Model.Select(m => m.ToString()).ToList();
      if (gsaCurve.StrainElasticCompression.HasValue) speckleCurve["StrainElasticCompression"] = gsaCurve.StrainElasticCompression.Value;
      if (gsaCurve.StrainElasticTension.HasValue) speckleCurve["StrainElasticTension"] = gsaCurve.StrainElasticTension.Value;
      if (gsaCurve.StrainPlasticCompression.HasValue) speckleCurve["StrainPlasticCompression"] = gsaCurve.StrainPlasticCompression.Value;
      if (gsaCurve.StrainPlasticTension.HasValue) speckleCurve["StrainPlasticTension"] = gsaCurve.StrainPlasticTension.Value;
      if (gsaCurve.StrainFailureCompression.HasValue) speckleCurve["StrainFailureCompression"] = gsaCurve.StrainFailureCompression.Value;
      if (gsaCurve.StrainFailureTension.HasValue) speckleCurve["StrainFailureTension"] = gsaCurve.StrainFailureTension.Value;
      if (gsaCurve.GammaF.HasValue) speckleCurve["GammaF"] = gsaCurve.GammaF.Value;
      if (gsaCurve.GammaE.HasValue) speckleCurve["GammaE"] = gsaCurve.GammaE.Value;
      return speckleCurve;
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
    private bool SetPropertySpringAxial(GsaPropSpr gsaPropSpr, PropertySpring specklePropertySpring)
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
    private bool SetPropertySpringCompression(GsaPropSpr gsaPropSpr, PropertySpring specklePropertySpring)
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
    private bool SetPropertySpringTension(GsaPropSpr gsaPropSpr, PropertySpring specklePropertySpring)
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
    private bool SetPropertySpringLockup(GsaPropSpr gsaPropSpr, PropertySpring specklePropertySpring)
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
    private bool SetPropertySpringGap(GsaPropSpr gsaPropSpr, PropertySpring specklePropertySpring)
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
    private bool SetPropertySpringFriction(GsaPropSpr gsaPropSpr, PropertySpring specklePropertySpring)
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
    private bool SetPropertySpringGeneral(GsaPropSpr gsaPropSpr, PropertySpring specklePropertySpring)
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
      { { Section1dStandardProfileType.Rectangular, GetProfileStandardRectangular },
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
    private SectionProfile GetProfileStandardRectangular(ProfileDetailsStandard gsaProfile)
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
    private Axis GetOrientationAxis(AxisRefType gsaAxisRefType, int? gsaAxisIndex )
    {
      //Cartesian coordinate system is the only one supported.
      Axis orientationAxis = null;

      if (gsaAxisRefType == AxisRefType.Local)
      {
        //TODO: handle local reference axis case
        //Local would be a different coordinate system for each element that gsaProp2d was assigned to
        ConversionErrors.Add(new Exception("GetOrientationAxis: Not yet implemented for local reference axis"));
      }
      else if (gsaAxisRefType == AxisRefType.Reference && gsaAxisIndex.IsIndex())
      {
        orientationAxis = GetAxisFromIndex(gsaAxisIndex.Value);
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
    private GSAStage GetStageFromIndex(int index, GSALayer layer = GSALayer.Both)
    {
      return (Instance.GsaModel.Cache.GetSpeckleObjects<GsaAnalStage, GSAStage>(index, out var speckleObjects, layer) && speckleObjects != null && speckleObjects.Count > 0)
        ? speckleObjects.First() : null;
    }
    #endregion

    #region Bridge
    private GSAAlignment GetAlignmentFromIndex(int index, GSALayer layer = GSALayer.Both)
    {
      return (Instance.GsaModel.Cache.GetSpeckleObjects<GsaAlign, GSAAlignment>(index, out var speckleObjects, layer) && speckleObjects != null && speckleObjects.Count > 0)
        ? speckleObjects.First() : null;
    }
    #endregion
    #endregion
    #endregion

    private void AddToMeaningfulNodeIndices(string appId, GSALayer layer = GSALayer.Both)
    {
      AddToMeaningfulNodeIndices(new[] { appId }, layer);
    }

    private void AddToMeaningfulNodeIndices(IEnumerable<string> nodeAppIds, GSALayer layer = GSALayer.Both)
    {
      if (nodeAppIds == null || nodeAppIds.Count() == 0)
      {
        return;
      }
      if (!meaningfulNodesAppIds.ContainsKey(GSALayer.Design))
      {
        meaningfulNodesAppIds.Add(GSALayer.Design, new HashSet<string>());
      }
      if ((layer == GSALayer.Analysis || layer == GSALayer.Both) && (!meaningfulNodesAppIds.ContainsKey(GSALayer.Analysis)))
      {
        meaningfulNodesAppIds.Add(GSALayer.Analysis, new HashSet<string>());
      }
      
      foreach (var id in nodeAppIds)
      {
        if ((layer == GSALayer.Design || layer == GSALayer.Both) && !meaningfulNodesAppIds[GSALayer.Design].Contains(id))
        {
          meaningfulNodesAppIds[GSALayer.Design].Add(id);
        }
        if ((layer == GSALayer.Analysis || layer == GSALayer.Both) && !meaningfulNodesAppIds[GSALayer.Analysis].Contains(id))
        {
          meaningfulNodesAppIds[GSALayer.Analysis].Add(id);
        }
      }
    }
  }
}
