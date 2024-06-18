using System.Collections;
using ArcGIS.Core.Data;
using ArcGIS.Core.Data.Exceptions;
using Objects.GIS;
using Speckle.Core.Models;
using FieldDescription = ArcGIS.Core.Data.DDL.FieldDescription;

namespace Speckle.Converters.ArcGIS3.Utils;

public class ArcGISFieldUtils : IArcGISFieldUtils
{
  private readonly ICharacterCleaner _characterCleaner;
  private const string FID_FIELD_NAME = "OBJECTID";

  public ArcGISFieldUtils(ICharacterCleaner characterCleaner)
  {
    _characterCleaner = characterCleaner;
  }

  public RowBuffer AssignFieldValuesToRow(
    RowBuffer rowBuffer,
    List<FieldDescription> fields,
    Dictionary<string, object?> attributes
  )
  {
    foreach (FieldDescription field in fields)
    {
      // try to assign values to writeable fields
      if (attributes is not null)
      {
        string key = field.AliasName; // use Alias, as Name is simplified to alphanumeric
        FieldType fieldType = field.FieldType;
        var value = attributes[key];
        if (value is not null)
        {
          // POC: get all values in a correct format
          try
          {
            rowBuffer[key] = GISAttributeFieldType.SpeckleValueToNativeFieldType(fieldType, value);
          }
          catch (GeodatabaseFeatureException)
          {
            //'The value type is incompatible.'
            // log error!
            rowBuffer[key] = null;
          }
          catch (GeodatabaseFieldException)
          {
            // non-editable Field, do nothing
          }
        }
        else
        {
          try
          {
            rowBuffer[key] = null;
          }
          catch (GeodatabaseGeneralException) { } // The index passed was not within the valid range. // unclear reason of the error
        }
      }
    }
    return rowBuffer;
  }

  public List<FieldDescription> GetFieldsFromSpeckleLayer(VectorLayer target)
  {
    List<FieldDescription> fields = new();
    List<string> fieldAdded = new();

    foreach (var field in target.attributes.GetMembers(DynamicBaseMemberType.Dynamic))
    {
      if (!fieldAdded.Contains(field.Key) && field.Key != FID_FIELD_NAME)
      {
        // POC: TODO check for the forbidden characters/combinations: https://support.esri.com/en-us/knowledge-base/what-characters-should-not-be-used-in-arcgis-for-field--000005588
        try
        {
          if (field.Value is not null)
          {
            string key = field.Key;
            FieldType fieldType = GISAttributeFieldType.FieldTypeToNative(field.Value);

            FieldDescription fieldDescription =
              new(_characterCleaner.CleanCharacters(key), fieldType) { AliasName = key };
            fields.Add(fieldDescription);
            fieldAdded.Add(key);
          }
          else
          {
            // log missing field
          }
        }
        catch (GeodatabaseFieldException)
        {
          // log missing field
        }
      }
    }
    return fields;
  }

  public List<(FieldDescription, Func<Base, object?>)> CreateFieldsFromListOfBase(List<Base> target)
  {
    List<(FieldDescription, Func<Base, object?>)> fieldsAndFunctions = new();
    List<string> fieldAdded = new();

    foreach (var baseObj in target)
    {
      foreach (KeyValuePair<string, object?> field in baseObj.GetMembers(DynamicBaseMemberType.Dynamic))
      {
        // POC: TODO check for the forbidden characters/combinations: https://support.esri.com/en-us/knowledge-base/what-characters-should-not-be-used-in-arcgis-for-field--000005588
        Func<Base, object?> function = x => x[field.Key];
        TraverseAttributes(field, function, fieldsAndFunctions, fieldAdded);
      }
    }
    return fieldsAndFunctions;
  }

  private void TraverseAttributes(
    KeyValuePair<string, object?> field,
    Func<Base, object?> function,
    List<(FieldDescription, Func<Base, object?>)> fieldsAndFunctions,
    List<string> fieldAdded
  )
  {
    if (field.Value is Base attributeBase)
    {
      // only traverse Base if it's Revit parameter
      if (field.Value is Objects.BuiltElements.Revit.Parameter)
      {
        foreach (KeyValuePair<string, object?> attributField in attributeBase.GetMembers(DynamicBaseMemberType.Dynamic))
        {
          KeyValuePair<string, object?> newAttributField = new($"{field.Key}.{attributField.Key}", attributField.Value);
          function += x => x[attributField.Key];
          TraverseAttributes(newAttributField, function, fieldsAndFunctions, fieldAdded);
        }
      }
    }
    else if (field.Value is IList attributeList)
    {
      int count = 0;
      foreach (KeyValuePair<string, object?> attributField in attributeList)
      {
        KeyValuePair<string, object?> newAttributField =
          new($"{field.Key}[{count}].{attributField.Key}", attributField.Value);
        function += x => x[attributField.Key];
        TraverseAttributes(newAttributField, function, fieldsAndFunctions, fieldAdded);
        count += 1;
      }
    }
    else
    {
      TryAddField(field, function, fieldsAndFunctions, fieldAdded);
    }
  }

  private void TryAddField(
    KeyValuePair<string, object?> field,
    Func<Base, object?> function,
    List<(FieldDescription, Func<Base, object?>)> fieldsAndFunctions,
    List<string> fieldAdded
  )
  {
    try
    {
      if (field.Value is not null && !fieldAdded.Contains(field.Key) && field.Key != FID_FIELD_NAME)
      {
        string key = field.Key;
        FieldType fieldType = FieldType.String; // GISAttributeFieldType.FieldTypeToNative(field.Value);

        FieldDescription fieldDescription = new(_characterCleaner.CleanCharacters(key), fieldType) { AliasName = key };
        fieldsAndFunctions.Add((fieldDescription, function));
        fieldAdded.Add(key);
      }
    }
    catch (GeodatabaseFieldException)
    {
      // do nothing
    }
  }
}
