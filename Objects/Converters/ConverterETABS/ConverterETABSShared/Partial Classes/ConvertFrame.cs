using System;
using System.Collections.Generic;
using Objects.Geometry;
using Objects.Structural.Geometry;

namespace Objects.Converter.ETABS
{
    public partial class ConverterETABS
    {
        public Element1D FrameToSpeckle(string Name)
        {
            var speckleStructFrame = new Element1D();
            speckleStructFrame.name = Name;
            string pointI, pointJ;
            pointI = pointJ = null;
            int v = Doc.Document.LineElm.GetPoints(Name,ref pointI,ref pointJ);
            var pointINode = PointToSpeckle(pointI);
            var pointJNode = PointToSpeckle(pointJ);
            var speckleLine = new Line(pointINode.basePoint, pointJNode.basePoint);
            speckleStructFrame.baseLine = speckleLine;
            return speckleStructFrame;
        }
    }
}
