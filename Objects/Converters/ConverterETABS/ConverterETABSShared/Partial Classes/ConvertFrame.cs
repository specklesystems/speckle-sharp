using System;
using System.Collections.Generic;
using Objects.Geometry;
using Objects.Structural.Geometry;
using Objects.Structural.Analysis;
using ETABSv1;

namespace Objects.Converter.ETABS
{
    public partial class ConverterETABS
    {
        public object FrameToNative(Element1D element1D)
        {
            string units = ModelUnits();
            string newFrame = "";
            Line baseline = element1D.baseLine;
            if (baseline != null)
            {
                Point end1node = baseline.start;
                Point end2node = baseline.end;
                Model.FrameObj.AddByCoord(end1node.x, end1node.y, end1node.z, end2node.x, end2node.y, end2node.z, ref newFrame,element1D.property.name);
            }
            else
            {
                Point end1node = element1D.end1Node.basePoint;
                Point end2node = element1D.end2Node.basePoint;
                Model.FrameObj.AddByCoord(end1node.x, end1node.y, end1node.z, end2node.x, end2node.y, end2node.z, ref newFrame,element1D.property.name);
            }

            return element1D.name;
        }
        public Element1D FrameToSpeckle(string name)
        {
            string units = ModelUnits();

            var speckleStructFrame = new Element1D();
            speckleStructFrame.name = name;
            string pointI, pointJ;
            pointI = pointJ = null;
            _ = Model.FrameObj.GetPoints(name, ref pointI, ref pointJ);
            var pointINode = PointToSpeckle(pointI);
            var pointJNode = PointToSpeckle(pointJ);
            speckleStructFrame.end1Node = pointINode;
            speckleStructFrame.end2Node = pointJNode;
            var speckleLine = new Line();
            if(units != null){
                speckleLine = new Line(pointINode.basePoint, pointJNode.basePoint, units);
            }
            else
            {
                speckleLine = new Line(pointINode.basePoint, pointJNode.basePoint);
            }
            speckleStructFrame.baseLine = speckleLine;
            eFrameDesignOrientation frameDesignOrientation = eFrameDesignOrientation.Null;
            Model.FrameObj.GetDesignOrientation(name, ref frameDesignOrientation);
            switch (frameDesignOrientation)
            {
                case eFrameDesignOrientation.Column:
                    {
                        speckleStructFrame.type = ElementType1D.Column;
                        break;
                    }
                case eFrameDesignOrientation.Beam:
                    {
                        speckleStructFrame.type = ElementType1D.Beam;
                        break;
                    }
                case eFrameDesignOrientation.Brace:
                    {
                        speckleStructFrame.type = ElementType1D.Brace;
                        break;
                    }
                case eFrameDesignOrientation.Null:
                    {
                        speckleStructFrame.type = ElementType1D.Null;
                        break;  
                    }
                case eFrameDesignOrientation.Other:
                    {
                        speckleStructFrame.type = ElementType1D.Other;
                        break;
                    }
            }

            bool[] iRelease, jRelease;
            iRelease = jRelease = null;
            double[] startV, endV;
            startV = endV = null;
            Model.FrameObj.GetReleases(name,ref iRelease,ref jRelease,ref startV,ref endV);
            speckleStructFrame.end1Releases = Restraint(iRelease);
            speckleStructFrame.end2Releases = Restraint(jRelease);

            double localAxis = 0;
            bool advanced = false;
            Model.FrameObj.GetLocalAxes(name, ref localAxis,ref advanced);
            speckleStructFrame.orientationAngle = localAxis;


            string property, SAuto;
            property = SAuto = null;
            Model.FrameObj.GetSection(name, ref property, ref SAuto);
            speckleStructFrame.property = Property1DToSpeckle(property);

            double offSetEnd1 = 0;
            double offSetEnd2 = 0;
            double RZ = 0;
            bool autoOffSet = true;
            Model.FrameObj.GetEndLengthOffset(name, ref autoOffSet, ref offSetEnd1, ref offSetEnd2, ref RZ);
            //Offset needs to be oriented wrt to 1-axis
            Vector end1Offset = new Vector(0, 0, offSetEnd1, units = ModelUnits());
            Vector end2Offset = new Vector(0, 0, offSetEnd2, units = ModelUnits());
            speckleStructFrame.end1Offset = end1Offset;
            speckleStructFrame.end2Offset = end2Offset;

            SpeckleModel.elements.Add(speckleStructFrame);

            return speckleStructFrame;
        }
    }
}
