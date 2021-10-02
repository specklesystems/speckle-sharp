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

        void setProperties(ETABSProperty2D property2D, string matProp, double thickeness)
        {
            property2D.thickness = thickeness;
            property2D.material = MaterialToSpeckle(matProp);
            return;
        }
        ETABSProperty2D Property2DToSpeckle(string area, string property)
        {
            var speckleProperty2D = new ETABSProperty2D();
            eAreaDesignOrientation areaDesignOrientation = eAreaDesignOrientation.Null;
            Model.AreaObj.GetDesignOrientation(area, ref areaDesignOrientation);
            eDeckType deckType = eDeckType.Filled;
            eSlabType slabType = eSlabType.Drop;
            eWallPropType wallPropType = eWallPropType.Specified;
            eShellType shellType = eShellType.Layered;
            string matProp = "";
            double thickness = 0;
            int color = 0;
            string notes = "";
            string GUID = "";

            switch (areaDesignOrientation)
            {
                case eAreaDesignOrientation.Wall:
                    speckleProperty2D.type = Structural.PropertyType2D.Wall;
                    Model.PropArea.GetWall(property, ref wallPropType, ref shellType, ref matProp, ref thickness, ref color, ref notes, ref GUID);
                    setProperties(speckleProperty2D, matProp, thickness);
                    speckleProperty2D.type2D = Structural.ETABS.Analysis.ETABSPropertyType2D.Wall;
                    break;
                case eAreaDesignOrientation.Floor:
                    int d = Model.PropArea.GetDeck(property, ref deckType, ref shellType, ref matProp, ref thickness, ref color, ref notes, ref GUID);
                    if (d == 1)
                    {
                        setProperties(speckleProperty2D, matProp, thickness);
                        speckleProperty2D.type2D = Structural.ETABS.Analysis.ETABSPropertyType2D.Deck;
                        break;
                    }
                    int s = Model.PropArea.GetSlab(property, ref slabType, ref shellType, ref matProp, ref thickness, ref color, ref notes, ref GUID);
                    if(s ==1)
                    {
                        setProperties(speckleProperty2D, matProp, thickness);
                        speckleProperty2D.type2D = Structural.ETABS.Analysis.ETABSPropertyType2D.Slab;
                        break;
                    }
                    break;
                default:
                    break;
            }

            double[] value = null;
            Model.AreaObj.GetModifiers(area, ref value);
            speckleProperty2D.modifierInPlane = value[2];
            speckleProperty2D.modifierBending = value[5];
            speckleProperty2D.modifierShear = value[6];

            return speckleProperty2D;
        }

    }
}
