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
        object Property2DToNative(ETABSProperty2D property2D)
        {
            return property2D.name;
        }

        ETABSProperty2D Property2DToSpeckle(string area, string property)
        {
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
                    var specklePropery2DWall = new ETABSProperty2D();
                    specklePropery2DWall.type = Structural.PropertyType2D.Wall;
                    Model.PropArea.GetWall(property, ref wallPropType, ref shellType, ref matProp, ref thickness, ref color, ref notes, ref GUID);
                    setProperties(specklePropery2DWall, matProp, thickness);
                    specklePropery2DWall.type2D = Structural.ETABS.Analysis.ETABSPropertyType2D.Wall;
                    return specklePropery2DWall;
                case eAreaDesignOrientation.Floor:
                    int d = Model.PropArea.GetDeck(property, ref deckType, ref shellType, ref matProp, ref thickness, ref color, ref notes, ref GUID);

                    if (d == 0)
                    {
                        var speckleProperties2D = new ETABSProperty2D();
                        double slabDepth = 0;
                        double shearStudDia= 0;
                        double shearStudFu= 0;
                        double shearStudHt= 0;
                        double ribDepth= 0;
                        double ribWidthTop = 0;
                        double ribWidthBot= 0;
                        double ribSpacing= 0;
                        double shearThickness= 0;
                        double unitWeight= 0 ;
                        if (deckType == eDeckType.Filled)
                        {
                            var speckleProperty2D = new ETABSProperty2D.DeckFilled();
                            Model.PropArea.GetDeckFilled(property,
                                ref slabDepth,
                                ref ribDepth,
                                ref ribWidthTop,
                                ref ribWidthBot,
                                ref ribSpacing,
                                ref shearThickness,
                                ref unitWeight,
                                ref shearStudDia,
                                ref shearStudHt,
                                ref shearStudFu);
                            speckleProperty2D.SlabDepth = slabDepth;
                            speckleProperty2D.ShearStudDia = shearStudDia;
                            speckleProperty2D.ShearStudFu = shearStudFu;
                            speckleProperty2D.RibDepth = ribDepth;
                            speckleProperty2D.RibWidthTop = ribWidthTop;
                            speckleProperty2D.RibWidthBot = ribWidthBot;
                            speckleProperty2D.RibSpacing = ribSpacing;
                            speckleProperty2D.ShearThickness = shearThickness;
                            speckleProperty2D.UnitWeight = unitWeight;
                            speckleProperty2D.ShearStudHt = shearStudHt;
                            speckleProperty2D.deckType = Structural.ETABS.Analysis.DeckType.Filled;
                            setProperties(speckleProperty2D, matProp, thickness);
                            speckleProperty2D.type2D = Structural.ETABS.Analysis.ETABSPropertyType2D.Deck;
                            return speckleProperty2D;
                        }
                        else if (deckType == eDeckType.Unfilled)
                        {
                            var speckleProperty2D = new ETABSProperty2D.DeckUnFilled();
                            Model.PropArea.GetDeckUnfilled(property,
                                ref ribDepth,
                                ref ribWidthTop,
                                ref ribWidthBot,
                                ref ribSpacing,
                                ref shearThickness,
                                ref unitWeight);
                            speckleProperty2D.RibDepth = ribDepth;
                            speckleProperty2D.RibWidthTop = ribWidthTop;
                            speckleProperty2D.RibWidthBot = ribWidthBot;
                            speckleProperty2D.RibSpacing = ribSpacing;
                            speckleProperty2D.ShearThickness = shearThickness;
                            speckleProperty2D.UnitWeight = unitWeight;
                            speckleProperty2D.deckType = Structural.ETABS.Analysis.DeckType.Filled;
                            setProperties(speckleProperty2D, matProp, thickness);
                            speckleProperty2D.type2D = Structural.ETABS.Analysis.ETABSPropertyType2D.Deck;
                            return speckleProperty2D;

                        }
                        else if (deckType == eDeckType.SolidSlab)
                        {
                            var speckleProperty2D = new ETABSProperty2D.DeckSlab();
                            Model.PropArea.GetDeckSolidSlab(property, ref slabDepth, ref shearStudDia, ref shearStudDia, ref shearStudFu);
                            speckleProperty2D.SlabDepth = slabDepth;
                            speckleProperty2D.ShearStudDia = shearStudDia;
                            speckleProperty2D.ShearStudFu = shearStudFu;
                            speckleProperty2D.ShearStudHt = shearStudHt;
                            speckleProperty2D.deckType = Structural.ETABS.Analysis.DeckType.SolidSlab;
                            setProperties(speckleProperty2D, matProp, thickness);
                            speckleProperty2D.type2D = Structural.ETABS.Analysis.ETABSPropertyType2D.Deck;
                            return speckleProperty2D;

                        }
                        break;
                    }
                    int s = Model.PropArea.GetSlab(property, ref slabType, ref shellType, ref matProp, ref thickness, ref color, ref notes, ref GUID);
                    if(s ==0)
                    {
                        var specklePropery2DSlab = new ETABSProperty2D();
                        setProperties(specklePropery2DSlab, matProp, thickness);
                        specklePropery2DSlab.type2D = Structural.ETABS.Analysis.ETABSPropertyType2D.Slab;
                        double overallDepth = 0;
                        double slabThickness = 0;
                        double stemWidthTop = 0;
                        double stemWidthBot = 0;
                        double ribSpacingDir1 = 0;
                        double ribSpacingDir2 = 0;
                        double ribSpacing = 0;
                        int ribParrallelTo = 0;
                        if (slabType == eSlabType.Waffle)
                        {
                            var speckleProperty2D = new ETABSProperty2D.WaffleSlab();
                            Model.PropArea.GetSlabWaffle(property, ref overallDepth, ref slabThickness,ref stemWidthTop,ref stemWidthBot,ref ribSpacingDir1,ref ribSpacingDir2);
                            speckleProperty2D.OverAllDepth = overallDepth;
                            speckleProperty2D.StemWidthBot = stemWidthBot;
                            speckleProperty2D.StemWidthTop = stemWidthTop;
                            speckleProperty2D.RibSpacingDir1 = ribSpacingDir1;
                            speckleProperty2D.RibSpacingDir2 = ribSpacingDir2;
                            speckleProperty2D.slabType = Structural.ETABS.Analysis.SlabType.Waffle;
                            setProperties(speckleProperty2D, matProp, thickness);
                            return speckleProperty2D;
                        }
                        else if (slabType == eSlabType.Ribbed)
                        {
                            var speckleProperty2D = new ETABSProperty2D.RibbedSlab();
                            Model.PropArea.GetSlabRibbed(property, ref overallDepth, ref slabThickness, ref stemWidthTop, ref stemWidthBot, ref ribSpacing, ref ribParrallelTo);
                            speckleProperty2D.OverAllDepth = overallDepth;
                            speckleProperty2D.StemWidthBot = stemWidthBot;
                            speckleProperty2D.StemWidthTop = stemWidthTop;
                            speckleProperty2D.RibSpacing = ribSpacing;
                            speckleProperty2D.RibsParallelTo = ribParrallelTo;
                            speckleProperty2D.slabType = Structural.ETABS.Analysis.SlabType.Ribbed;
                            setProperties(speckleProperty2D, matProp, thickness);
                            return speckleProperty2D;

                        }
                        else
                        {
                            switch (slabType)
                            {
                                case eSlabType.Slab:
                                    specklePropery2DSlab.slabType = Structural.ETABS.Analysis.SlabType.Slab;
                                    break;
                                case eSlabType.Drop:
                                    specklePropery2DSlab.slabType = Structural.ETABS.Analysis.SlabType.Drop;
                                    break;
                                case eSlabType.Mat:
                                    specklePropery2DSlab.slabType = Structural.ETABS.Analysis.SlabType.Mat;
                                    break;
                                case eSlabType.Footing:
                                    specklePropery2DSlab.slabType = Structural.ETABS.Analysis.SlabType.Footing;
                                    break;
                                default:
                                    specklePropery2DSlab.slabType = Structural.ETABS.Analysis.SlabType.Null;
                                    break;
                            }
                            return specklePropery2DSlab;
                        }
                        break;
                    }
                    break;
            }

            double[] value = null;
            //Model.AreaObj.GetModifiers(area, ref value);
            //speckleProperty2D.modifierInPlane = value[2];
            //speckleProperty2D.modifierBending = value[5];
            //speckleProperty2D.modifierShear = value[6];
            return null;
        }

    }
}
