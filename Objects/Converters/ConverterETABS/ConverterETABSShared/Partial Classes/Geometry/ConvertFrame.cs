using System;
using System.Collections.Generic;
using Objects.Geometry;
using Objects.Structural.Geometry;
using Objects.Structural.Analysis;
using Speckle.Core.Models;
using System.Linq;
using ETABSv1;

namespace Objects.Converter.ETABS
{
    public partial class ConverterETABS
    {
        public object FrameToNative(Element1D element1D)
        {
            if (GetAllFrameNames(Model).Contains(element1D.name)){
                return null;
            }
            string units = ModelUnits();
            string newFrame = "";
            Line baseline = element1D.baseLine;
            string[] properties = null;
            int number = 0;
            Model.PropFrame.GetNameList(ref number, ref properties);
            if (!properties.Contains(element1D.property.name))
            {
                Property1DToNative(element1D.property);
                Model.PropFrame.GetNameList(ref number, ref properties);
            }
            if (baseline != null)
            {
                Point end1node = baseline.start;
                Point end2node = baseline.end;
                //temp fix code for m 
                if(baseline.units == "m")
                {
                    end1node.x *= 1000;
                    end1node.y *= 1000;
                    end1node.z *= 1000;
                    end2node.x *= 1000;
                    end2node.y *= 1000;
                    end2node.z *= 1000;
                }
                if (properties.Contains(element1D.property.name))
                {
                    Model.FrameObj.AddByCoord(end1node.x, end1node.y, end1node.z, end2node.x, end2node.y, end2node.z, ref newFrame, element1D.property.name);
                }
                else
                {
                    Model.FrameObj.AddByCoord(end1node.x, end1node.y, end1node.z, end2node.x, end2node.y, end2node.z, ref newFrame);
                }
            }
            else
            {
                Point end1node = element1D.end1Node.basePoint;
                Point end2node = element1D.end2Node.basePoint;
                if (baseline.units == "m")
                {
                    end1node.x *= 1000;
                    end1node.y *= 1000;
                    end1node.z *= 1000;
                    end2node.x *= 1000;
                    end2node.y *= 1000;
                    end2node.z *= 1000;
                }
                if (properties.Contains(element1D.property.name))
                {
                    Model.FrameObj.AddByCoord(end1node.x, end1node.y, end1node.z, end2node.x, end2node.y, end2node.z, ref newFrame, element1D.property.name);
                }
                else
                {
                    Model.FrameObj.AddByCoord(end1node.x, end1node.y, end1node.z, end2node.x, end2node.y, end2node.z, ref newFrame);
                }
            }

            bool[] end1Release = null;
            bool[] end2Release = null;
            if (element1D.end1Releases != null && element1D.end2Releases !=null) 
            {
                end1Release = RestraintToNative(element1D.end1Releases);
                end2Release = RestraintToNative(element1D.end2Releases);
            }

            double[] startV, endV;
            startV = new double[] { };
            endV = new double[] { };

            if(element1D.orientationAngle!= null)
            {
                Model.FrameObj.SetLocalAxes(newFrame, element1D.orientationAngle);
            }


            Model.FrameObj.SetReleases(newFrame, ref end1Release, ref end2Release, ref startV, ref endV);
            if(element1D.name != null)
            {
                Model.FrameObj.ChangeName(newFrame, element1D.name);
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
           
            speckleStructFrame.end1Releases = RestraintToSpeckle(iRelease);
            speckleStructFrame.end2Releases = RestraintToSpeckle(jRelease);
            SpeckleModel.restraints.Add(speckleStructFrame.end1Releases);
            SpeckleModel.restraints.Add(speckleStructFrame.end2Releases);


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
            Vector end1Offset = new Vector(0, 0, offSetEnd1, ModelUnits());
            Vector end2Offset = new Vector(0, 0, offSetEnd2, ModelUnits());
            speckleStructFrame.end1Offset = end1Offset;
            speckleStructFrame.end2Offset = end2Offset;

            var GUID = "";
            Model.FrameObj.GetGUID(name, ref GUID);
            speckleStructFrame.applicationId = GUID;
            List<Base> elements = SpeckleModel.elements;
            List<string> application_Id = elements.Select(o => o.applicationId).ToList();
            if (!application_Id.Contains(speckleStructFrame.applicationId))
            {

                SpeckleModel.elements.Add(speckleStructFrame);
            }


            return speckleStructFrame;
        }
    }
}
