using Objects;
using Objects.Other;
using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;
using Speckle.Core.Models;
using AECPropDB = Autodesk.Aec.PropertyData.DatabaseServices;

namespace Speckle.Converters.Civil3d.ToSpeckle.BuiltElements;

[NameAndRankValue(nameof(CDB.Pipe), NameAndRankValueAttribute.SPECKLE_DEFAULT_RANK)]
public class PipeToSpeckleConverter : IToSpeckleTopLevelConverter
{
  private readonly ITypedConverter<AG.Point3d, SOG.Point> _pointConverter;
  private readonly ITypedConverter<ADB.Curve, Objects.ICurve> _curveConverter;
  private readonly ITypedConverter<ADB.Extents3d, SOG.Box> _boxConverter;
  private readonly ITypedConverter<ADB.Solid3d, SOG.Mesh> _solidConverter;
  private readonly ITypedConverter<AECPropDB.PropertySet, List<DataField>> _propertySetConverter;
  private readonly IConversionContextStack<Document, AAEC.BuiltInUnit> _contextStack;

  public PipeToSpeckleConverter(
    ITypedConverter<AG.Point3d, SOG.Point> pointConverter,
    ITypedConverter<ADB.Curve, Objects.ICurve> curveConverter,
    ITypedConverter<ADB.Extents3d, SOG.Box> boxConverter,
    ITypedConverter<ADB.Solid3d, SOG.Mesh> solidConverter,
    ITypedConverter<AECPropDB.PropertySet, List<DataField>> propertySetConverter,
    IConversionContextStack<Document, AAEC.BuiltInUnit> contextStack
  )
  {
    _pointConverter = pointConverter;
    _curveConverter = curveConverter;
    _boxConverter = boxConverter;
    _solidConverter = solidConverter;
    _propertySetConverter = propertySetConverter;
    _contextStack = contextStack;
  }

  public Base Convert(object target) => Convert((CDB.Pipe)target);

  public SOBE.Pipe Convert(CDB.Pipe target)
  {
    ICurve curve = _curveConverter.Convert(target.BaseCurve);
    SOG.Mesh pipeMesh = _solidConverter.Convert(target.Solid3dBody);

    SOBE.Pipe specklePipe =
      new()
      {
        baseCurve = curve,
        diameter = target.InnerDiameterOrWidth,
        length = target.Length3DToInsideEdge,
        displayValue = new List<SOG.Mesh> { pipeMesh },
        units = _contextStack.Current.SpeckleUnits
      };

    // POC: not setting property sets yet, need to determine connector parameter interoperability
    // POC: not setting part data yet, same reason as above
    // POC: not setting additional pipe properties, probably should scope a CivilPipe class

    return specklePipe;
  }
}
