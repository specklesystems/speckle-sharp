using Speckle.Core.Models;

namespace Objects.BuiltElements.Archicad;

public class AssemblySegment : Base
{
  public bool circleBased { get; set; }

  public string modelElemStructureType { get; set; }

  public double nominalHeight { get; set; }

  public double nominalWidth { get; set; }

  public bool isHomogeneous { get; set; }

  public double endWidth { get; set; }

  public double endHeight { get; set; }

  public bool isEndWidthAndHeightLinked { get; set; }

  public bool isWidthAndHeightLinked { get; set; }

  public string profileAttrName { get; set; }

  public string buildingMaterial { get; set; }
}

public class AssemblySegmentScheme : Base
{
  public string lengthType { get; set; }

  public double fixedLength { get; set; }

  public double lengthProportion { get; set; }
}

public class AssemblySegmentCut : Base
{
  public string cutType { get; set; }

  public double customAngle { get; set; }
}

public class Hole : Base
{
  public string holeType { get; set; }

  public bool holeContourOn { get; set; }

  public int holeId { get; set; }

  public double centerx { get; set; }

  public double centerz { get; set; }

  public double width { get; set; }

  public double height { get; set; }
}
