using System;
using System.Collections.Generic;
using System.Text;
using ETABSv1;
using Objects.Structural.Properties;
using Objects.Structural.Materials;
using Objects.Structural.ETABS.Analysis;
using Objects.Structural.ETABS.Properties;

namespace Objects.Converter.ETABS
{
    public partial class ConverterETABS
    {
        public object WallPropertyToNative(ETABSProperty2D Wall)
        {
            return Wall.name;
        }
        public ETABSProperty2D WallPropertyToSpeckle(string property)
        {
            eWallPropType wallPropType = eWallPropType.Specified;
            eShellType shellType = eShellType.Layered;
            string matProp = "";
            double thickness = 0;
            int color = 0;
            string notes = "";
            string GUID = "";
            var specklePropery2DWall = new ETABSProperty2D();
            specklePropery2DWall.type = Structural.PropertyType2D.Shell;
            Model.PropArea.GetWall(property, ref wallPropType, ref shellType, ref matProp, ref thickness, ref color, ref notes, ref GUID);
            var speckleShellType = ConvertShellType(shellType);
            specklePropery2DWall.shellType = speckleShellType;
            setProperties(specklePropery2DWall, matProp, thickness, property);
            specklePropery2DWall.type2D = Structural.ETABS.Analysis.ETABSPropertyType2D.Wall;
            return specklePropery2DWall;

        }

    }
}
