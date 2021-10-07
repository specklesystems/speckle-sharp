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
        public double RibSpacing { get; set; }
        public int RibsParallelTo { get; set; }
        public double OverAllDepth { get; set; }
        public double StemWidthTop { get; set; }
        public double StemWidthBot { get; set; }
        public double RibSpacingDir1 { get; set; }
        public double RibSpacingDir2 { get; set; }
        public double ribDepth{ get; set; }
        public double ribWidthTop{ get; set; }
        public double ribWidthBot{ get; set; }
        public double ribSpacing{ get; set; }
        public double shearThickness{ get; set; }
        public double unitWeight{ get; set; }
        public double slabDepth { get; set; }
        public double shearStudDia { get; set; }
        public double shearStudFu { get; set; }
        public double shearStudHt { get; set; }

        public ETABSProperty2D() { }
    }
}
