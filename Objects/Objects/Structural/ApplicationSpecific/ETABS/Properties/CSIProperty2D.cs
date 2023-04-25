using Objects.Structural.CSI.Analysis;
using Objects.Structural.Materials;
using Objects.Structural.Properties;
using Speckle.Core.Kits;

namespace Objects.Structural.CSI.Properties;

public class CSIOpening : Property2D
{
  [SchemaInfo("Opening", "Create an CSI Opening", "CSI", "Properties")]
  public CSIOpening(bool isOpening)
  {
    this.isOpening = isOpening;
  }

  public CSIOpening() { }

  public bool isOpening { get; set; }
}

public class CSIProperty2D : Property2D
{
  public CSIPropertyType2D type2D { get; set; }
  public SlabType slabType { get; set; }
  public DeckType deckType { get; set; }
  public ShellType shellType { get; set; }

  public class WaffleSlab : CSIProperty2D
  {
    public WaffleSlab() { }

    [SchemaInfo("WaffleSlab", "Create an CSI Waffle Slab", "CSI", "Properties")]
    public WaffleSlab(
      string PropertyName,
      ShellType shell,
      StructuralMaterial ConcreteMaterial,
      double Thickness,
      double overAllDepth,
      double stemWidthBot,
      double stemWidthTop,
      double ribSpacingDir1,
      double ribSpacingDir2
    )
    {
      type2D = CSIPropertyType2D.Slab;
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

    public double OverAllDepth { get; set; }
    public double StemWidthBot { get; set; }
    public double StemWidthTop { get; set; }
    public double RibSpacingDir1 { get; set; }
    public double RibSpacingDir2 { get; set; }

    //[SchemaInfo("WaffleSlab","Define a WaffleSlab Area Property")]
  }

  public class RibbedSlab : CSIProperty2D
  {
    public RibbedSlab() { }

    [SchemaInfo("RibbedSlab", "Create an CSI Ribbed Slab", "CSI", "Properties")]
    public RibbedSlab(
      string PropertyName,
      ShellType shell,
      StructuralMaterial ConcreteMaterial,
      double Thickness,
      double overAllDepth,
      double stemWidthBot,
      double stemWidthTop,
      double ribSpacing,
      int ribsParallelTo
    )
    {
      type2D = CSIPropertyType2D.Slab;
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

    public double OverAllDepth { get; set; }
    public double StemWidthBot { get; set; }
    public double StemWidthTop { get; set; }
    public double RibSpacing { get; set; }
    public int RibsParallelTo { get; set; }
  }

  public class Slab : CSIProperty2D
  {
    public Slab() { }

    [SchemaInfo("Slab", "Create an CSI Slab", "CSI", "Properties")]
    public Slab(string PropertyName, ShellType shell, StructuralMaterial ConcreteMaterial, double Thickness)
    {
      type2D = CSIPropertyType2D.Slab;
      slabType = SlabType.Slab;
      deckType = DeckType.Null;

      name = PropertyName;
      shellType = shell;
      material = ConcreteMaterial;
      thickness = Thickness;
    }
  }

  public class DeckFilled : CSIProperty2D
  {
    public DeckFilled() { }

    [SchemaInfo("DeckFilled", "Create an CSI Filled Deck", "CSI", "Properties")]
    public DeckFilled(
      string PropertyName,
      ShellType shell,
      StructuralMaterial ConcreteMaterial,
      double DeckThickness,
      double slabDepth,
      double shearStudDia,
      double shearStudFu,
      double shearStudHt,
      double ribDepth,
      double ribWidthTop,
      double ribWidthBot,
      double ribSpacing,
      double shearThickness,
      double unitWeight
    )
    {
      type2D = CSIPropertyType2D.Deck;
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

  public class DeckUnFilled : CSIProperty2D
  {
    [SchemaInfo("DeckUnFilled", "Create an CSI UnFilled Deck", "CSI", "Properties")]
    public DeckUnFilled(
      string PropertyName,
      ShellType shell,
      StructuralMaterial Material,
      double DeckThickness,
      double slabDepth,
      double ribDepth,
      double ribWidthTop,
      double ribWidthBot,
      double ribSpacing,
      double shearThickness,
      double unitWeight
    )
    {
      type2D = CSIPropertyType2D.Deck;
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

    public DeckUnFilled() { }

    public double SlabDepth { get; set; }
    public double RibDepth { get; set; }
    public double RibWidthTop { get; set; }
    public double RibWidthBot { get; set; }
    public double RibSpacing { get; set; }
    public double ShearThickness { get; set; }
    public double UnitWeight { get; set; }
  }

  public class DeckSlab : CSIProperty2D
  {
    [SchemaInfo("DeckSlab", "Create an CSI Slab Deck", "CSI", "Properties")]
    public DeckSlab(
      string PropertyName,
      ShellType shell,
      StructuralMaterial ConcreteMaterial,
      double DeckThickness,
      double slabDepth,
      double shearStudDia,
      double shearStudFu,
      double shearStudHt
    )
    {
      type2D = CSIPropertyType2D.Deck;
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

    public DeckSlab() { }

    public double SlabDepth { get; set; }
    public double ShearStudDia { get; set; }
    public double ShearStudFu { get; set; }
    public double ShearStudHt { get; set; }
  }
}
