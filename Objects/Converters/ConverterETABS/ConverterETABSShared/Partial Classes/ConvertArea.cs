using System;
using System.Collections.Generic;
using System.Text;
using ETABSv1;
using Objects.Structural.Geometry;
using Objects.Structural.ETABS.Properties;

namespace Objects.Converter.ETABS
{
    public partial class ConverterETABS
    {
        public  Element2D AreaToSpeckle(string name)
        {
            var speckleStructArea = new Element2D();
            speckleStructArea.name = name;
            int numPoints = 0;
            string[] points = null;
            Model.AreaObj.GetPoints(name, ref numPoints, ref points);
            List<Node> nodes = null;
            foreach(string point in points)
            {
                nodes.Add(PointToSpeckle(point));
            }
            speckleStructArea.topology = nodes;
            string propName = "";
            Model.AreaObj.GetProperty(name,ref propName);
            speckleStructArea.property = Property2DToSpeckle(name,propName);

            return speckleStructArea;
        }

    }
}
