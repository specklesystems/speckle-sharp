using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using Objects.Structural.Geometry;
using Speckle.Core.Models;
using DB = Autodesk.Revit.DB;
using Plane = Objects.Geometry.Plane;

namespace Objects.Converter.Revit;

public partial class ConverterRevit
{
  public ApplicationObject AnalyticalNodeToNative(Node speckleNode)
  {
    return new ApplicationObject(speckleNode.id, speckleNode.speckle_type)
    {
      applicationId = speckleNode.applicationId
    };
  }

  private Node AnalyticalNodeToSpeckle(ReferencePoint revitNode)
  {
    var cs = revitNode.GetCoordinateSystem();
    var localAxis = new Plane(
      PointToSpeckle(cs.Origin, revitNode.Document),
      VectorToSpeckle(cs.BasisZ, revitNode.Document),
      VectorToSpeckle(cs.BasisX, revitNode.Document),
      VectorToSpeckle(cs.BasisY, revitNode.Document)
    );
    var basePoint = PointToSpeckle(cs.Origin, revitNode.Document); // alternative to revitNode.Position
    //var speckleNode = new Node(basePoint, revitNode.Name, null, localAxis);
    var speckleNode = new Node();

    GetAllRevitParamsAndIds(speckleNode, revitNode);

    return speckleNode;
  }

  private Base BoundaryConditionsToSpeckle(BoundaryConditions revitBoundary)
  {
    var points = new List<XYZ> { };
    var nodes = new List<Node> { };

    var cs = revitBoundary.GetDegreesOfFreedomCoordinateSystem();
    var localAxis = new Plane(
      PointToSpeckle(cs.Origin, revitBoundary.Document),
      VectorToSpeckle(cs.BasisZ, revitBoundary.Document),
      VectorToSpeckle(cs.BasisX, revitBoundary.Document),
      VectorToSpeckle(cs.BasisY, revitBoundary.Document)
    );

    var restraintType = revitBoundary.GetBoundaryConditionsType();
    var state = 0;
    switch (restraintType)
    {
      case BoundaryConditionsType.Point:
        var point = revitBoundary.Point;
        points.Add(point);
        state = GetParamValue<int>(revitBoundary, BuiltInParameter.BOUNDARY_PARAM_PRESET); // 1 fixed, 2 pinned, 3 roller, 4 user/variable
        break;
      case BoundaryConditionsType.Line:
        var curve = revitBoundary.GetCurve();
        points.Add(curve.GetEndPoint(0));
        points.Add(curve.GetEndPoint(1));
        state = GetParamValue<int>(revitBoundary, BuiltInParameter.BOUNDARY_PARAM_PRESET_LINEAR);
        break;
      case BoundaryConditionsType.Area:
        var loops = revitBoundary.GetLoops();
        foreach (var loop in loops)
        {
          foreach (var areaCurve in loop)
          {
            points.Add(areaCurve.GetEndPoint(1));
          }
        }

        points = points.Distinct().ToList();
        state = GetParamValue<int>(revitBoundary, BuiltInParameter.BOUNDARY_PARAM_PRESET_AREA);
        break;
      default:
        break;
    }

    var restraint = GetRestraintCode(revitBoundary, restraintType, state);

    foreach (var point in points)
    {
      var speckleNode = new Node();
      //var speckleNode = new Node(PointToSpeckle(point), null, restraint, localAxis);

      GetAllRevitParamsAndIds(speckleNode, revitBoundary);

      nodes.Add(speckleNode);
    }

    var speckleBoundaryCondition = new Base();
    if (nodes.Count > 1)
    {
      speckleBoundaryCondition["nodes"] = nodes;
    }
    else
    {
      speckleBoundaryCondition = nodes[0];
    }

    return speckleBoundaryCondition;
  }

  private Restraint GetRestraintCode(DB.Element elem, BoundaryConditionsType type, int presetState)
  {
    if (presetState == 0)
    {
      return new Restraint(RestraintType.Fixed);
    }
    else if (presetState == 1)
    {
      return new Restraint(RestraintType.Pinned);
    }
    else if (presetState == 2)
    {
      return new Restraint(RestraintType.Roller);
    }

    var boundaryParams = new BuiltInParameter[]
    {
      BuiltInParameter.BOUNDARY_DIRECTION_X,
      BuiltInParameter.BOUNDARY_DIRECTION_Y,
      BuiltInParameter.BOUNDARY_DIRECTION_Z,
      BuiltInParameter.BOUNDARY_DIRECTION_ROT_X,
      BuiltInParameter.BOUNDARY_DIRECTION_ROT_Y,
      BuiltInParameter.BOUNDARY_DIRECTION_ROT_Z
    };

    var springValueParams = new BuiltInParameter[]
    {
      BuiltInParameter.BOUNDARY_RESTRAINT_X,
      BuiltInParameter.BOUNDARY_RESTRAINT_Y,
      BuiltInParameter.BOUNDARY_RESTRAINT_Z,
      BuiltInParameter.BOUNDARY_RESTRAINT_ROT_X,
      BuiltInParameter.BOUNDARY_RESTRAINT_ROT_Y,
      BuiltInParameter.BOUNDARY_RESTRAINT_ROT_Z,
    };

    var linSpringValueParams = new BuiltInParameter[]
    {
      BuiltInParameter.BOUNDARY_LINEAR_RESTRAINT_X,
      BuiltInParameter.BOUNDARY_LINEAR_RESTRAINT_Y,
      BuiltInParameter.BOUNDARY_LINEAR_RESTRAINT_Z,
      BuiltInParameter.BOUNDARY_LINEAR_RESTRAINT_ROT_X,
    };

    var areaSpingValueParams = new BuiltInParameter[]
    {
      BuiltInParameter.BOUNDARY_AREA_RESTRAINT_X,
      BuiltInParameter.BOUNDARY_AREA_RESTRAINT_Y,
      BuiltInParameter.BOUNDARY_AREA_RESTRAINT_Z,
      BuiltInParameter.BOUNDARY_LINEAR_RESTRAINT_ROT_X,
    };

    string code = "";
    var springStiffness = new double[6];
    for (int i = 0; i < boundaryParams.Length; i++)
    {
      var value = GetParamValue<int>(elem, boundaryParams[i]);
      switch (value)
      {
        case 0:
          code = code + "F"; //fixed
          break;
        case 1:
          code = code + "R"; //released
          break;
        case 2:
          code = code + "K"; //spring
          if (type == BoundaryConditionsType.Line)
          {
            switch (boundaryParams[i])
            {
              case BuiltInParameter.BOUNDARY_DIRECTION_X:
                springStiffness[i] = GetParamValue<double>(elem, linSpringValueParams[0]); // kN/m²
                break;
              case BuiltInParameter.BOUNDARY_DIRECTION_Y:
                springStiffness[i] = GetParamValue<double>(elem, linSpringValueParams[1]); // kN/m²
                break;
              case BuiltInParameter.BOUNDARY_DIRECTION_Z:
                springStiffness[i] = GetParamValue<double>(elem, linSpringValueParams[2]); // kN/m²
                break;
              case BuiltInParameter.BOUNDARY_DIRECTION_ROT_X:
                springStiffness[i] = GetParamValue<double>(elem, linSpringValueParams[3]); // kN-m/°/m
                break;
              default:
                springStiffness[i] = 0;
                break;
            }
          }
          else if (type == BoundaryConditionsType.Area)
          {
            switch (boundaryParams[i])
            {
              case BuiltInParameter.BOUNDARY_DIRECTION_X:
                springStiffness[i] = GetParamValue<double>(elem, areaSpingValueParams[0]); // kN/m²
                break;
              case BuiltInParameter.BOUNDARY_DIRECTION_Y:
                springStiffness[i] = GetParamValue<double>(elem, areaSpingValueParams[1]); // kN/m²
                break;
              case BuiltInParameter.BOUNDARY_DIRECTION_Z:
                springStiffness[i] = GetParamValue<double>(elem, areaSpingValueParams[2]); // kN/m²
                break;
              case BuiltInParameter.BOUNDARY_DIRECTION_ROT_X:
                springStiffness[i] = GetParamValue<double>(elem, areaSpingValueParams[3]); // kN-m/°/m
                break;
              default:
                springStiffness[i] = 0;
                break;
            }
          }
          else
          {
            springStiffness[i] = GetParamValue<double>(elem, springValueParams[i]);
          }

          break;
        default:
          return null;
      }
    }

    var restraint = new Restraint(
      code,
      springStiffness[0],
      springStiffness[1],
      springStiffness[2],
      springStiffness[3],
      springStiffness[4],
      springStiffness[5]
    );

    return restraint;
  }
}
