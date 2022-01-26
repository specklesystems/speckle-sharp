using System;
using System.Collections.Generic;
using System.Text;
using Objects.Structural.Geometry;
using Tekla.Structures.Model;
using System.Linq;
using Objects.Geometry;
using System.Collections;
using Tekla.Structures.Solid;
using StructuralUtilities.PolygonMesher;

namespace Objects.Converter.TeklaStructures
{
  public partial class ConverterTeklaStructures
  {

    public Mesh GetMeshFromSolid(Tekla.Structures.Model.Solid solid){
      List<double> MyList = new List<double> { };
      ArrayList MyFaceNormalList = new ArrayList();
      List<int> facesList = new List<int> { };

      FaceEnumerator MyFaceEnum = solid.GetFaceEnumerator();

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
            if (i % 4 == 0)
            {
              continue;
            }
            else
            {
              faces[i] += largestVertixCount;
            }
          }
          facesList.AddRange(faces.ToList());
        }
      }
      return new Mesh(MyList, facesList);

    }
    //public static bool IsElementSupported(this ModelObject e)
    //{

    //  if (SupportedBuiltInCategories.Contains(e)
    //    return true;
    //  return false;
    //}

    ////list of currently supported Categories (for sending only)
    ////exact copy of the one in the Speckle.ConnectorRevit.ConnectorRevitUtils
    ////until issue https://github.com/specklesystems/speckle-sharp/issues/392 is resolved
    //private static List<ModelObject.ModelObjectEnum> SupportedBuiltInCategories = new List<ModelObject.ModelObjectEnum>{

    //ModelObject.ModelObjectEnum.BEAM,

  }
}
