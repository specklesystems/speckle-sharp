using MathNet.Spatial.Euclidean;

namespace StructuralUtilities.PolygonMesher;

internal class Vertex
{
  public int Index;
  public Point2D Local;
  public Point3D Global;

  public Vertex(int index, Point2D local, Point3D global)
  {
    this.Local = local;
    this.Index = index;
    this.Global = global;
  }

  public double[] Coordinates
  {
    get { return new double[] { Global.X, Global.Y, Global.Z }; }
  }
}
