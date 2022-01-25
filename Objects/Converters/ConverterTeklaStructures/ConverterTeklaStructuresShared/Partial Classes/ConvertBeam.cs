using System;
using System.Collections.Generic;
using Objects.Geometry;
using Objects.Structural.Geometry;
using Objects.Structural.Analysis;
using Speckle.Core.Models;
using BE = Objects.BuiltElements;
using System.Linq;
using Tekla.Structures.Model;
using Tekla.Structures.Solid;
using System.Collections;
using StructuralUtilities.PolygonMesher;

namespace Objects.Converter.TeklaStructures
{
  public partial class ConverterTeklaStructures
  {

    public BE.Beam BeamToSpeckle(Tekla.Structures.Model.Beam beam)
    {
      var speckleBeam = new BE.Beam();
      var endPoint = beam.EndPoint;
      var startPoint = beam.StartPoint;
      Point speckleStartPoint = new Point(startPoint.X, startPoint.Y, startPoint.Z);
      Point speckleEndPoint = new Point(endPoint.X, endPoint.Y, endPoint.Z);
      speckleBeam.baseLine = new Line(speckleStartPoint, speckleEndPoint);
      var profile = beam.Profile.ProfileString;

      var mesh = beam.GetSolid();

      List<double> MyList = new List<double> { };
      ArrayList MyFaceNormalList = new ArrayList();
      List<int> facesList = new List<int> { };

      FaceEnumerator MyFaceEnum = mesh.GetFaceEnumerator();

      var counter = 0;
      while (MyFaceEnum.MoveNext())
      {
        var mesher = new PolygonMesher();

        Face MyFace = MyFaceEnum.Current as Face;
        if (MyFace != null)
        {
          List<double> TempList = new List<double> { };
          LoopEnumerator MyLoopEnum = MyFace.GetLoopEnumerator();
          while (MyLoopEnum.MoveNext())
          {
            Loop MyLoop = MyLoopEnum.Current as Loop;
            if (MyLoop != null)
            {

              VertexEnumerator MyVertexEnum = MyLoop.GetVertexEnumerator() as VertexEnumerator;
              while (MyVertexEnum.MoveNext())
              {

                Tekla.Structures.Geometry3d.Point MyVertex = MyVertexEnum.Current as Tekla.Structures.Geometry3d.Point;
                if (MyVertex != null)
                {
                  TempList.Add(MyVertex.X);
                  TempList.Add(MyVertex.Y);
                  TempList.Add(MyVertex.Z);

                }


                //speckleBeam.displayMesh = beam.Profile.
              }
              counter++;
            }
          }

          mesher.Init(TempList);
          var faces = mesher.Faces();
          var vertices = mesher.Coordinates;
          var verticesList = vertices.ToList();
          MyList.AddRange(verticesList);
          var largestVertixCount = 0;
          if (facesList.Count == 0)
          {
            largestVertixCount = 0;
          }
          else
          {
            largestVertixCount = facesList.Max() + 1;
          }
          for (int i = 0; i < faces.Length; i++)
          {
            if (i% 4 == 0 ){
              continue;
            }
            else{
              faces[i] += largestVertixCount;
            }
          }
          facesList.AddRange(faces.ToList());
        }
      }

      speckleBeam.displayMesh = new Geometry.Mesh(MyList, facesList);
      return speckleBeam;
    }
  }
}