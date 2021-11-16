using System;
using System.Collections.Generic;
using System.Text;
using ETABSv1;
using Objects.Structural.Geometry;
using Objects.Structural.Analysis;
using Objects.Structural.ETABS.Properties;
using Speckle.Core.Models;
using StructuralUtilities.PolygonMesher;
using System.Linq;

namespace Objects.Converter.ETABS
{
    public partial class ConverterETABS
    {
        public object AreaToNative(Element2D area)
        {
            if (GetAllAreaNames(Model).Contains(area.name))
            {
                return null;
            }
            string name = "";
            int numPoints = area.topology.Count();
            List<double> X = new List<double> { };
            List<double> Y = new List<double>{ };
            List<double> Z = new List<double> { };

            foreach( Node point in area.topology) {
                X.Add(point.basePoint.x);
                Y.Add(point.basePoint.y);
                Z.Add(point.basePoint.z);
            }
            double[] x = X.ToArray();
            double[] y = Y.ToArray();
            double[] z = Z.ToArray();

            if(area.property != null)
            {
                Model.AreaObj.AddByCoord(numPoints, ref x, ref y, ref z, ref name, area.property.name);

            }
            else
            {
                Model.AreaObj.AddByCoord(numPoints, ref x, ref y, ref z, ref name);

            }
            if(area.name != null)
            {
                Model.AreaObj.ChangeName(name, area.name);
            }
            return name;

        }
        public Element2D AreaToSpeckle(string name)
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

            var GUID = "";
            Model.AreaObj.GetGUID(name, ref GUID);
            speckleStructArea.applicationId = GUID;
            List<Base> elements = SpeckleModel.elements;
            List<string> application_Id = elements.Select(o => o.applicationId).ToList();
            if (!application_Id.Contains(speckleStructArea.applicationId))
            {
                SpeckleModel.elements.Add(speckleStructArea);
            }


            return speckleStructArea;
        }

    }
}
