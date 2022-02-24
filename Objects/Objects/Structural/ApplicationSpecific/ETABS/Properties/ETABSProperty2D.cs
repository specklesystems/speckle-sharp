using Speckle.Newtonsoft.Json;
using Speckle.Core.Kits;
using Speckle.Core.Models;
using System.Collections.Generic;
using Objects.Geometry;
using Objects.Structural.Geometry;
using Objects.Structural.Properties;
using Objects.Structural.ETABS.Analysis;
using Objects.Structural.Materials;

namespace Objects.Structural.ETABS.Properties
{
    public class ETABSOpening: Property2D{
      public bool isOpening { get; set; }

    [SchemaInfo("Opening", "Create an ETABS Opening", "ETABS", "Properties")]
    public ETABSOpening(bool isOpening)
    {
      this.isOpening = isOpening;
    }

    public ETABSOpening()
    {
    }
  }
    public class ETABSProperty2D : Property2D
    {
        public ETABSPropertyType2D type2D { get; set; }
        public SlabType slabType { get; set; }
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

            [SchemaInfo("WaffleSlab","Create an ETABS Waffle Slab","ETABS", "Properties")]
            public WaffleSlab(string PropertyName, ShellType shell, Material ConcreteMaterial, double Thickness ,double overAllDepth, double stemWidthBot, double stemWidthTop, double ribSpacingDir1, double ribSpacingDir2)
            {

                type2D = ETABSPropertyType2D.Slab;
                slabType = SlabType.Waffle;
                deckType = DeckType.Null;

                name = PropertyName;
                shellType = shell;
                material = ConcreteMaterial;
                thickness = Thickness;

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

            [SchemaInfo("RibbedSlab", "Create an ETABS Ribbed Slab", "ETABS", "Properties")]
            public RibbedSlab(string PropertyName, ShellType shell, Material ConcreteMaterial, double Thickness ,double overAllDepth, double stemWidthBot, double stemWidthTop, double ribSpacing, int ribsParallelTo)
            {
                type2D = ETABSPropertyType2D.Slab;
                slabType = SlabType.Ribbed;
                deckType = DeckType.Null;

                name = PropertyName;
                shellType = shell;
                material = ConcreteMaterial;
                thickness = Thickness;

                OverAllDepth = overAllDepth;
                StemWidthBot = stemWidthBot;
                StemWidthTop = stemWidthTop;
                RibSpacing = ribSpacing;
                RibsParallelTo = ribsParallelTo;
            }
        }

        public class Slab : ETABSProperty2D
        {
      public Slab()
      {
      }

      [SchemaInfo("Slab", "Create an ETABS Slab", "ETABS", "Properties")]
            public Slab(string PropertyName, ShellType shell, Material ConcreteMaterial,double Thickness )
            {

                type2D = ETABSPropertyType2D.Slab;
                slabType = SlabType.Slab;
                deckType = DeckType.Null;
               
                name = PropertyName;
                shellType = shell;
                material = ConcreteMaterial;
                thickness = Thickness;
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

            [SchemaInfo("DeckFilled", "Create an ETABS Filled Deck", "ETABS", "Properties")]
            public DeckFilled(string PropertyName, ShellType shell, Material ConcreteMaterial, double DeckThickness,double slabDepth, double shearStudDia, double shearStudFu, double shearStudHt, double ribDepth, double ribWidthTop, double ribWidthBot, double ribSpacing, double shearThickness, double unitWeight)
            {
                type2D = ETABSPropertyType2D.Deck;
                slabType = SlabType.Null;
                deckType = DeckType.Filled;

                name = PropertyName;
                shellType = shell;
                material = ConcreteMaterial;
                thickness = DeckThickness;

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



            [SchemaInfo("DeckUnFilled", "Create an ETABS UnFilled Deck", "ETABS", "Properties")]
            public DeckUnFilled(string PropertyName, ShellType shell, Material Material, double DeckThickness,double slabDepth, double ribDepth, double ribWidthTop, double ribWidthBot, double ribSpacing, double shearThickness, double unitWeight)
            {
                type2D = ETABSPropertyType2D.Deck;
                slabType = SlabType.Null;
                deckType = DeckType.Unfilled;

                name = PropertyName;
                shellType = shell;
                material = Material;
                thickness = DeckThickness;

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

            [SchemaInfo("DeckSlab", "Create an ETABS Slab Deck", "ETABS", "Properties")]
            public DeckSlab(string PropertyName, ShellType shell, Material ConcreteMaterial, double DeckThickness,double slabDepth, double shearStudDia, double shearStudFu, double shearStudHt)
            {
                type2D = ETABSPropertyType2D.Deck;
                slabType = SlabType.Null;
                deckType = DeckType.SolidSlab;

                name = PropertyName;
                shellType = shell;
                material = ConcreteMaterial;
                thickness = DeckThickness;

                SlabDepth = slabDepth;
                ShearStudDia = shearStudDia;
                ShearStudFu = shearStudFu;
                ShearStudHt = shearStudHt;

            }

            public DeckSlab()
            {
            }
        }


 
        public ETABSProperty2D() { }

    }
}
