using System;
using Objects.Structural.Geometry;
using ETABSv1;

namespace Objects.Converter.ETABS
{
    public partial class ConverterETABS
    {
        public void PointToNative(Node speckleStructNode)
        {
            throw new NotImplementedException(); 
        }
        public Node PointToSpeckle(string name)
        {
           
            var speckleStructNode = new Node();
            double x,y,z;
            x = y = z = 0;
            int v = Doc.Document.PointObj.GetCoordCartesian(name,ref x,ref y,ref z);
            speckleStructNode.basePoint.x = x;
            speckleStructNode.basePoint.y = y;
            speckleStructNode.basePoint.z = z;
            speckleStructNode.name = name;

            bool[] restraints = null;
            v = Doc.Document.PointObj.GetRestraint(name, ref restraints);

            speckleStructNode.restraint.stiffnessX = Convert.ToInt32(!restraints[0]);
            speckleStructNode.restraint.stiffnessY = Convert.ToInt32(!restraints[1]);
            speckleStructNode.restraint.stiffnessZ = Convert.ToInt32(!restraints[2]);
            speckleStructNode.restraint.stiffnessZ = Convert.ToInt32(!restraints[3]);
            speckleStructNode.restraint.stiffnessZ = Convert.ToInt32(!restraints[4]);
            speckleStructNode.restraint.stiffnessZ = Convert.ToInt32(!restraints[5]);

//TO DO: detach properties
            return speckleStructNode;
        }
    }
}
