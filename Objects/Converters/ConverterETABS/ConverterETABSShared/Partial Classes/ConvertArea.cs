using System;
using System.Collections.Generic;
using System.Text;
using ETABSv1;
using Objects.Structural.Geometry;
using Objects.Structural.ETABS.Properties;
using SpeckleStructuralClasses.PolygonMesher;
using System.Linq;

namespace Objects.Converter.ETABS
{
    public partial class ConverterETABS
    {
        public void AreaToNative(Element2D area)
        {
            return;
        }
        public  Element2D AreaToSpeckle(string name)
        {
            string units = ModelUnits();
            var speckleStructArea = new Element2D();
            speckleStructArea.name = name;
            int numPoints = 0;
            string[] points = null;
            Model.AreaObj.GetPoints(name, ref numPoints, ref points);
            List<Node> nodes = new List<Node>();
            foreach(string point in points)
            {
                Node node = PointToSpeckle(point);
                nodes.Add(node);
            }
            speckleStructArea.topology = nodes;
            string propName = "";
            Model.AreaObj.GetProperty(name,ref propName);
            speckleStructArea.property = Property2DToSpeckle(name,propName);

            List<double> coordinates = new List<double> { };
            foreach(Node node in nodes)
            {
                coordinates.Add(node.basePoint.x);
                coordinates.Add(node.basePoint.y);
                coordinates.Add(node.basePoint.z);
            }

            PolygonMesher polygonMesher = new PolygonMesher();
            polygonMesher.Init(coordinates);
            var faces = polygonMesher.Faces();
            var vertices = polygonMesher.Coordinates;
            speckleStructArea.displayMesh = new Geometry.Mesh(vertices, faces);


            return speckleStructArea;
        }

    }
}
