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
        public DeckType deckType{ get; set; }
        public ShellType shellType{ get; set; }

        public class WaffleSlab : ETABSProperty2D
        {
            public double OverAllDepth { get; set; }
            public double StemWidthBot { get; set; }
            public double StemWidthTop { get; set; }
            public double RibSpacingDir1 { get; set; }
            public double RibSpacingDir2 { get; set; }
            public WaffleSlab() { }


            //[SchemaInfo("WaffleSlab","Define a WaffleSlab Area Property")]      
        }

        public class RibbedSlab : ETABSProperty2D
        {
            public double OverAllDepth { get; set; }
            public double StemWidthBot { get; set; }
            public double StemWidthTop { get; set; }
            public double RibSpacing { get; set; }
            public int RibsParallelTo { get; set; }
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

        }

        public class DeckSlab : ETABSProperty2D
        {
            public double SlabDepth { get; set; }
            public double ShearStudDia { get; set; }
            public double ShearStudFu { get; set; }
            public double ShearStudHt { get; set; }
            public double ShearThickness { get; set; }
            public double UnitWeight { get; set; }
        }


        public ETABSProperty2D() { }
    }
}
