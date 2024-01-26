namespace Objects.Structural.CSI.Analysis;

public enum CSIPropertyType2D
{
  Deck,
  Slab,
  Shell,
  Wall
}

public enum SlabType
{
  Slab,
  Drop,
  Ribbed,
  Waffle,
  Mat,
  Footing,
  Null
}

public enum ShellType
{
  ShellThin,
  ShellThick,
  Membrane,
  Layered,
  Null
}

public enum DeckType
{
  Filled,
  Unfilled,
  SolidSlab,
  Null
}
