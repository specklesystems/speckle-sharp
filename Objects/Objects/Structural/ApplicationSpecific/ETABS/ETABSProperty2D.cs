using Speckle.Newtonsoft.Json;
using Speckle.Core.Kits;
using Speckle.Core.Models;
using System.Collections.Generic;
using Objects.Geometry;
using Objects.Structural.Geometry;
using Objects.Structural.Properties;
using Objects.Structural.ETABS.Analysis;

namespace Objects.Structural.ETABS.Properties
{
    public class ETABSProperty2D : Property2D
    {
        public ETABSPropertyType2D type2D{ get; set; }
        public SlabType slabType{ get; set; }
        public DeckType deckType { get; set; } 
        public ShellType shellType{ get; set; }

        public class WaffleSlab : ETABSProperty2D
        {
            public double OverAllDepth { get; set; }
            public double StemWidthBot { get; set; }
            public double StemWidthTop { get; set; }
            public double RibSpacingDir1 { get; set; }
            public double RibSpacingDir2 { get; set; }
            public WaffleSlab() { }


            public WaffleSlab(double overAllDepth, double stemWidthBot, double stemWidthTop, double ribSpacingDir1, double ribSpacingDir2)
            {
                type2D = ETABSPropertyType2D.Slab;
                slabType = SlabType.Waffle;
                OverAllDepth = overAllDepth;
                StemWidthBot = stemWidthBot;
                StemWidthTop = stemWidthTop;
                RibSpacingDir1 = ribSpacingDir1;
                RibSpacingDir2 = ribSpacingDir2;
            }


            //[SchemaInfo("WaffleSlab","Define a WaffleSlab Area Property")]      
        }

        public class RibbedSlab : ETABSProperty2D
        {
            public double OverAllDepth { get; set; }
            public double StemWidthBot { get; set; }
            public double StemWidthTop { get; set; }
            public double RibSpacing { get; set; }
            public int RibsParallelTo { get; set; }

            public RibbedSlab() { }
            public RibbedSlab(double overAllDepth, double stemWidthBot, double stemWidthTop, double ribSpacing, int ribsParallelTo)
            {

                OverAllDepth = overAllDepth;
                StemWidthBot = stemWidthBot;
                StemWidthTop = stemWidthTop;
                RibSpacing = ribSpacing;
                RibsParallelTo = ribsParallelTo;
            }
        }

        public class DeckFilled : ETABSProperty2D
        {
            public double SlabDepth { get; set; }
            public double ShearStudDia { get; set; }
            public double ShearStudFu { get; set; }
            public double ShearStudHt { get; set; }
            public double RibDepth { get; set; }
            public double RibWidthTop { get; set; }
            public double RibWidthBot { get; set; }
            public double RibSpacing { get; set; }
            public double ShearThickness { get; set; }
            public double UnitWeight { get; set; }

            public DeckFilled() { }
            public DeckFilled(double slabDepth, double shearStudDia, double shearStudFu, double shearStudHt, double ribDepth, double ribWidthTop, double ribWidthBot, double ribSpacing, double shearThickness, double unitWeight)
            {
                SlabDepth = slabDepth;
                ShearStudDia = shearStudDia;
                ShearStudFu = shearStudFu;
                ShearStudHt = shearStudHt;
                RibDepth = ribDepth;
                RibWidthTop = ribWidthTop;
                RibWidthBot = ribWidthBot;
                RibSpacing = ribSpacing;
                ShearThickness = shearThickness;
                UnitWeight = unitWeight;
            }
        }

        public class DeckUnFilled : ETABSProperty2D
        {
            public double SlabDepth { get; set; }
            public double RibDepth { get; set; }
            public double RibWidthTop { get; set; }
            public double RibWidthBot { get; set; }
            public double RibSpacing { get; set; }
            public double ShearThickness { get; set; }
            public double UnitWeight { get; set; }

            public DeckUnFilled(double slabDepth, double ribDepth, double ribWidthTop, double ribWidthBot, double ribSpacing, double shearThickness, double unitWeight)
            {
                SlabDepth = slabDepth;
                RibDepth = ribDepth;
                RibWidthTop = ribWidthTop;
                RibWidthBot = ribWidthBot;
                RibSpacing = ribSpacing;
                ShearThickness = shearThickness;
                UnitWeight = unitWeight;
            }

            public DeckUnFilled()
            {
            }
        }

        public class DeckSlab : ETABSProperty2D
        {
            public double SlabDepth { get; set; }
            public double ShearStudDia { get; set; }
            public double ShearStudFu { get; set; }
            public double ShearStudHt { get; set; }
            public double ShearThickness { get; set; }
            public double UnitWeight { get; set; }

            public DeckSlab(double slabDepth, double shearStudDia, double shearStudFu, double shearStudHt, double shearThickness, double unitWeight)
            {
                SlabDepth = slabDepth;
                ShearStudDia = shearStudDia;
                ShearStudFu = shearStudFu;
                ShearStudHt = shearStudHt;
                ShearThickness = shearThickness;
                UnitWeight = unitWeight;
            }

            public DeckSlab()
            {
            }
        }


 
        public ETABSProperty2D() { }

    }
}
