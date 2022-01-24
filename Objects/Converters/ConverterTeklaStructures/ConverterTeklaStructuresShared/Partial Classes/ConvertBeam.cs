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
      var mesh = beam.GetSolid();

      List<double> MyList = new List<double> { };
      ArrayList MyFaceNormalList = new ArrayList();
      List<int> faces = new List<int> { };

      FaceEnumerator MyFaceEnum = mesh.GetFaceEnumerator();

      var counter = 0;
      while (MyFaceEnum.MoveNext())
      {

        Face MyFace = MyFaceEnum.Current as Face;
        if (MyFace != null)
        {
          MyFaceNormalList.Add(MyFace.Normal);

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
                  MyList.Add(MyVertex.X);
                  MyList.Add(MyVertex.Y);
                  MyList.Add(MyVertex.Z);
                  faces.Add(counter);
                  faces.Add(counter);
                  faces.Add(counter);

                }

                //speckleBeam.displayMesh = beam.Profile.
              }
              counter++;
            }
          }
        }
      }
      //speckleBeam.displayMesh = new Mesh(MyList, faces);
      return speckleBeam;
    }
  }
}