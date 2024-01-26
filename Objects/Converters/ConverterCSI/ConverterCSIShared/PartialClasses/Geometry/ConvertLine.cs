using Objects.Geometry;
using Speckle.Core.Kits;

namespace Objects.Converter.CSI;

public partial class ConverterCSI
{
  public string LineToNative(Line line)
  {
    string newFrame = "";
    Point lineStart = line.start;
    Point end2Node = line.end;

    var modelUnits = ModelUnits();
    var end1NodeSf = Units.GetConversionFactor(lineStart.units, modelUnits);
    var end2NodeSf = Units.GetConversionFactor(end2Node.units, modelUnits);

    var success = Model.FrameObj.AddByCoord(
      lineStart.x * end1NodeSf,
      lineStart.y * end1NodeSf,
      lineStart.z * end1NodeSf,
      end2Node.x * end2NodeSf,
      end2Node.y * end2NodeSf,
      end2Node.z * end2NodeSf,
      ref newFrame
    );

    if (success != 0)
    {
      throw new ConversionException("Failed to add new frame");
    }

    return newFrame;
  }
}
