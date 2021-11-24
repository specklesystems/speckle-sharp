using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Autodesk.Revit.DB;
using Objects.Geometry;
using Speckle.Core.Logging;
using DB = Autodesk.Revit.DB;
using Mesh = Objects.Geometry.Mesh;
using BlockInstance = Objects.Other.BlockInstance;
using BlockDefinition = Objects.Other.BlockDefinition;

namespace Objects.Converter.Revit
{
  public partial class ConverterRevit
  {
    // Creates a new group definition for each scaling
    public Group BlockInstanceToNative(BlockInstance instance, double[ ] scale = null)
    {
      var totscale = new[ ] {1.0, 1.0, 1.0};
      scale?.CopyTo(totscale, 0);
      for ( var i = 0; i < 3; i++ )
        totscale[ i ] *= instance.transform[ i * 5 ];

      var blockDefName = "SpeckleBlock_" + instance.blockDefinition.name;
      if ( totscale.All(s => Math.Abs(s - 1.0) > 0.0001) )
        blockDefName += $"_{totscale[ 0 ]}x{totscale[ 1 ]}x{totscale[ 2 ]}";

      var basePoint = PointToNative(instance.GetInsertionPoint());

      // Get or make group from block definition
      GroupType block_def = new FilteredElementCollector(Doc)
                              .OfClass(typeof(GroupType))
                              .OfType<GroupType>()
                              .FirstOrDefault(f => f.Name.Equals(blockDefName)) ??
                            BlockDefinitionToNative(instance.blockDefinition, totscale);

      Group _instance = Doc.Create.PlaceGroup(basePoint, block_def);

      // transform
      if ( _instance != null )
      {
        if ( MatrixDecompose(instance.transform, out double rotation) )
        {
          try
          {
            // some point based families don't have a rotation, so keep this in a try catch
            if ( rotation != ( _instance.Location as LocationPoint ).Rotation )
            {
              var axis = DB.Line.CreateBound(new XYZ(basePoint.X, basePoint.Y, 0),
                new XYZ(basePoint.X, basePoint.Y, 1000));
              ( _instance.Location as LocationPoint ).Rotate(axis,
                rotation - ( _instance.Location as LocationPoint ).Rotation);
            }
          }
          catch
          {
          }
        }

        SetInstanceParameters(_instance, instance);
      }

      Report.Log($"Created Block {_instance.Id}");
      return _instance;
    }

    private GroupType BlockDefinitionToNative(BlockDefinition definition, double[ ] scale)
    {
      // create a family to represent a block definition
      // TODO: rename block with stream commit info prefix taken from UI - need to figure out cleanest way of storing this in the doc for retrieval by converter

      // convert definition geometry to native
      var isScaled = scale.All(s => Math.Abs(s - 1.0) > 0.0001);
      var ids = new List<ElementId>();
      foreach ( var geometry in definition.geometry )
      {
        switch ( geometry )
        {
          case Brep brep:
            var brepShape = DirectShapeToNative(brep).NativeObject as DB.DirectShape;
            ids.Add(brepShape?.Id);
            break;
          case Mesh mesh:
            if ( isScaled )
            {
              var verts = new List<double>();
              for ( var i = 0; i < mesh.vertices.Count; i += 3 )
              {
                verts.AddRange(ScaleVertexValues(mesh.vertices[ i ], mesh.vertices[ i + 1 ], mesh.vertices[ i + 2 ],
                  scale));
              }

              mesh = new Mesh(verts, mesh.faces, mesh.colors, units: mesh.units,
                applicationId: mesh.applicationId ?? mesh.id);
            }

            var meshShape = DirectShapeToNative(mesh).NativeObject as DB.DirectShape;
            ids.Add(meshShape.Id);
            break;
          case ICurve curve:
            try
            {
              if ( isScaled )
              {
                // need to work out scaling for each curve type 🙃
              }

              var modelCurves = CurveToNative(curve).Cast<DB.Curve>().ToList();
              modelCurves.ForEach(o =>
              {
                var modelCurve = Doc.Create.NewModelCurve(o, NewSketchPlaneFromCurve(o, Doc));
                ids.Add(modelCurve.Id);
              });
            }
            catch ( Exception e )
            {
              Report.LogConversionError(
                new SpeckleException($"Could not convert block {definition.id} curve to native.", e));
            }

            break;
          case BlockInstance instance:
            var grp = BlockInstanceToNative(instance, scale);
            ids.Add(grp.Id);
            break;
        }
      }

      var group = Doc.Create.NewGroup(ids);
      var groupType = group.GroupType;
      Doc.Delete(group.Id);
      groupType.Name = "SpeckleBlock_" + definition.name;
      if ( isScaled )
        groupType.Name += $"_{scale[ 0 ]}x{scale[ 1 ]}x{scale[ 2 ]}";
      return groupType;
    }

    private static IEnumerable<double> ScaleVertexValues(double x, double y, double z, IReadOnlyList<double> scale)
    {
      return new[ ] {x * scale[ 0 ], y * scale[ 1 ], z * scale[ 2 ]};
    }

    private bool MatrixDecompose(double[ ] m, out double rotation)
    {
      var matrix = new Matrix4x4(
        ( float ) m[ 0 ], ( float ) m[ 1 ], ( float ) m[ 2 ], ( float ) m[ 3 ],
        ( float ) m[ 4 ], ( float ) m[ 5 ], ( float ) m[ 6 ], ( float ) m[ 7 ],
        ( float ) m[ 8 ], ( float ) m[ 9 ], ( float ) m[ 10 ], ( float ) m[ 11 ],
        ( float ) m[ 12 ], ( float ) m[ 13 ], ( float ) m[ 14 ], ( float ) m[ 15 ]);

      if ( Matrix4x4.Decompose(matrix, out Vector3 _scale, out Quaternion _rotation, out Vector3 _translation) )
      {
        rotation = Math.Acos(_rotation.W) * 2;
        return true;
      }

      rotation = 0;
      return false;
    }
  }
}