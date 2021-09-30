using System;
using System.Collections.Generic;
using Objects.Geometry;
using Objects.Structural.Geometry;
using ETABSv1;

namespace Objects.Converter.ETABS
{
    public partial class ConverterETABS
    {
        public void FrameToNative()
        {
            throw new NotImplementedException();
        }
        public Element1D FrameToSpeckle(string name)
        {
            var speckleStructFrame = new Element1D();
            speckleStructFrame.name = name;
            string pointI, pointJ;
            pointI = pointJ = null;
            int v = Model.FrameObj.GetPoints(name,ref pointI,ref pointJ);
            var pointINode = PointToSpeckle(pointI);
            var pointJNode = PointToSpeckle(pointJ);
            var speckleLine = new Line(pointINode.basePoint, pointJNode.basePoint);
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

            var end1Release = new Restraint();
            var end2Release = new Restraint();
            bool[] iRelease, jRelease;
            iRelease = jRelease = null;
            double[] startV, endV;
            startV = endV = null;
            Model.FrameObj.GetReleases(name,ref iRelease,ref jRelease,ref startV,ref endV);
            end1Release.stiffnessX = Convert.ToInt32(!iRelease[0]);
            end1Release.stiffnessY = Convert.ToInt32(!iRelease[1]);
            end1Release.stiffnessZ = Convert.ToInt32(!iRelease[2]);
            end1Release.stiffnessXX = Convert.ToInt32(!iRelease[3]);
            end1Release.stiffnessYY = Convert.ToInt32(!iRelease[4]);
            end1Release.stiffnessZZ = Convert.ToInt32(!iRelease[5]);
            end2Release.stiffnessX = Convert.ToInt32(!jRelease[0]);
            end2Release.stiffnessY = Convert.ToInt32(!jRelease[1]);
            end2Release.stiffnessZ = Convert.ToInt32(!jRelease[2]);
            end2Release.stiffnessXX = Convert.ToInt32(!jRelease[3]);
            end2Release.stiffnessYY = Convert.ToInt32(!jRelease[4]);
            end2Release.stiffnessZZ = Convert.ToInt32(!jRelease[5]);
            speckleStructFrame.end1Releases = end1Release;
            speckleStructFrame.end2Releases = end2Release;

            double localAxis = 0;
            bool advanced = false;
            Model.FrameObj.GetLocalAxes(name, ref localAxis,ref advanced);
            speckleStructFrame.orientationAngle = localAxis;


            string property, SAuto;
            property = SAuto = null;
            Model.FrameObj.GetSection(name, ref property, ref SAuto);
            speckleStructFrame.property = Property1DToSpeckle(property);
            speckleStructFrame.property.profile = SectionToSpeckle(name,property);

            return speckleStructFrame;
        }
    }
}
