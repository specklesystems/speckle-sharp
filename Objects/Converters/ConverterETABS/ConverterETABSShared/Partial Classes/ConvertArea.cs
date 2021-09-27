using System;
using System.Collections.Generic;
using System.Text;
using Objects.Structural.Geometry;

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
            Doc.Document.AreaObj.GetPoints(name, ref numPoints, ref points);

            return speckleStructArea;
        }

    }
}
