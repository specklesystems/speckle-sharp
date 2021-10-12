using System;
using System.Collections.Generic;
using System.Text;
using ETABSv1;
using Objects.Structural.Properties;
using Objects.Structural.Materials;
using Objects.Structural.ETABS.Properties;

namespace Objects.Converter.ETABS
{
    public partial class ConverterETABS
    {
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
            specklePropery2DWall.type = Structural.PropertyType2D.Wall;
            Model.PropArea.GetWall(property, ref wallPropType, ref shellType, ref matProp, ref thickness, ref color, ref notes, ref GUID);
            setProperties(specklePropery2DWall, matProp, thickness);
            specklePropery2DWall.type2D = Structural.ETABS.Analysis.ETABSPropertyType2D.Wall;
            return specklePropery2DWall;

        }

    }
}
