using System;
using Objects.Structural.Geometry;
using Objects.Geometry;
using Objects.Structural.Analysis;
using ETABSv1;

namespace Objects.Converter.ETABS
{
    public partial class ConverterETABS
    {
        public object PointToNative(Node speckleStructNode)
        {
            var point = speckleStructNode.basePoint;
            string name = "";
            Model.PointObj.AddCartesian(point.x, point.y, point.z, ref name);
            return speckleStructNode.name;
        }
        public Node PointToSpeckle(string name)
        {           
            var speckleStructNode = new Node();
            double x,y,z;
            x = y = z = 0;
            int v = Model.PointObj.GetCoordCartesian(name,ref x,ref y,ref z);
            speckleStructNode.basePoint = new Point();
            speckleStructNode.basePoint.x = x;
            speckleStructNode.basePoint.y = y;
            speckleStructNode.basePoint.z = z;
            speckleStructNode.name = name;

            bool[] restraints = null;
            v = Model.PointObj.GetRestraint(name, ref restraints);

            speckleStructNode.restraint = RestraintToSpeckle(restraints);

            SpeckleModel.restraints.Add(speckleStructNode.restraint);

            SpeckleModel.nodes.Add(speckleStructNode);
            
                //TO DO: detach properties
            return speckleStructNode;
        }

    }
}
