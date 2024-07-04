using Objects.Other;
using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;
using AECPropDB = Autodesk.Aec.PropertyData.DatabaseServices;

namespace Speckle.Converters.Civil3d.ToSpeckle.Raw;

public class PropertySetToSpeckleRawConverter : ITypedConverter<AECPropDB.PropertySet, List<DataField>>
{
  private readonly ITypedConverter<AG.Vector3d, SOG.Vector> _vectorConverter;
  private readonly ITypedConverter<AG.Point3d, SOG.Point> _pointConverter;
  private readonly IConversionContextStack<Document, ADB.UnitsValue> _contextStack;

  public PropertySetToSpeckleRawConverter(
    ITypedConverter<AG.Vector3d, SOG.Vector> vectorConverter,
    ITypedConverter<AG.Point3d, SOG.Point> pointConverter,
    IConversionContextStack<Document, ADB.UnitsValue> contextStack
  )
  {
    _vectorConverter = vectorConverter;
    _pointConverter = pointConverter;
    _contextStack = contextStack;
  }

  public List<DataField> Convert(object target) => Convert((AECPropDB.PropertySet)target);

  public List<DataField> Convert(AECPropDB.PropertySet target)
  {
    List<DataField> properties = new();

    ADB.Transaction tr = _contextStack.Current.Document.TransactionManager.TopTransaction;
    AECPropDB.PropertySetDefinition setDef = (AECPropDB.PropertySetDefinition)
      tr.GetObject(target.PropertySetDefinition, ADB.OpenMode.ForRead);

    // get property definitions
    var propDefs = new Dictionary<int, AECPropDB.PropertyDefinition>();
    foreach (AECPropDB.PropertyDefinition def in setDef.Definitions)
    {
      propDefs.Add(def.Id, def);
    }

    foreach (AECPropDB.PropertySetData data in target.PropertySetData)
    {
      string fieldName = propDefs.TryGetValue(data.Id, out AECPropDB.PropertyDefinition value)
        ? value.Name
        : data.FieldBucketId;

      object fieldData = data.GetData();
      DataField field = new(fieldName, fieldData.GetType().Name, data.UnitType.PluralName(false), fieldData);
      properties.Add(field);
    }

    return properties;
  }
}
