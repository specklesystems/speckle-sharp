using System.Collections;
using ArcGIS.Core.Data;
using ArcGIS.Core.Data.Exceptions;
using Objects.GIS;
using Speckle.Core.Logging;
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
        catch (GeodatabaseGeneralException)
        {
          // The index passed was not within the valid range. // unclear reason of the error
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
      // get all members by default, but only Dynamic ones from the basic geometry
      Dictionary<string, object?> members = new();

      // leave out until we decide which properties to support on Receive
      /*
      if (baseObj.speckle_type.StartsWith("Objects.Geometry"))
      {
        members = baseObj.GetMembers(DynamicBaseMemberType.Dynamic);
      }
      else
      {
        members = baseObj.GetMembers(DynamicBaseMemberType.All);
      }
      */

      foreach (KeyValuePair<string, object?> field in members)
      {
        // POC: TODO check for the forbidden characters/combinations: https://support.esri.com/en-us/knowledge-base/what-characters-should-not-be-used-in-arcgis-for-field--000005588
        Func<Base, object?> function = x => x[field.Key];
        TraverseAttributes(field, function, fieldsAndFunctions, fieldAdded);
      }
    }

    // change all FieldType.Blob to String
    // "Blob" will never be used on receive, so it is a placeholder for non-properly identified fields
    for (int i = 0; i < fieldsAndFunctions.Count; i++)
    {
      (FieldDescription description, Func<Base, object?> function) = fieldsAndFunctions[i];
      if (description.FieldType is FieldType.Blob)
      {
        fieldsAndFunctions[i] = new(
          new FieldDescription(description.Name, FieldType.String) { AliasName = description.AliasName },
          function
        );
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
      // only traverse Base if it's Rhino userStrings, or Revit parameter, or Base containing Revit parameters
      if (field.Key == "parameters")
      {
        foreach (KeyValuePair<string, object?> attributField in attributeBase.GetMembers(DynamicBaseMemberType.Dynamic))
        {
          // only iterate through elements if they are actually Revit Parameters or parameter IDs
          if (
            attributField.Value is Objects.BuiltElements.Revit.Parameter
            || attributField.Key == "applicationId"
            || attributField.Key == "id"
          )
          {
            KeyValuePair<string, object?> newAttributField =
              new($"{field.Key}.{attributField.Key}", attributField.Value);
            Func<Base, object?> functionAdded = x => (function(x) as Base)?[attributField.Key];
            TraverseAttributes(newAttributField, functionAdded, fieldsAndFunctions, fieldAdded);
          }
        }
      }
      else if (field.Key == "userStrings")
      {
        foreach (KeyValuePair<string, object?> attributField in attributeBase.GetMembers(DynamicBaseMemberType.Dynamic))
        {
          KeyValuePair<string, object?> newAttributField = new($"{field.Key}.{attributField.Key}", attributField.Value);
          Func<Base, object?> functionAdded = x => (function(x) as Base)?[attributField.Key];
          TraverseAttributes(newAttributField, functionAdded, fieldsAndFunctions, fieldAdded);
        }
      }
      else if (field.Value is Objects.BuiltElements.Revit.Parameter)
      {
        foreach (
          KeyValuePair<string, object?> attributField in attributeBase.GetMembers(DynamicBaseMemberType.Instance)
        )
        {
          KeyValuePair<string, object?> newAttributField = new($"{field.Key}.{attributField.Key}", attributField.Value);
          Func<Base, object?> functionAdded = x => (function(x) as Base)?[attributField.Key];
          TraverseAttributes(newAttributField, functionAdded, fieldsAndFunctions, fieldAdded);
        }
      }
      else
      {
        // for now, ignore all other properties of Base type
      }
    }
    else if (field.Value is IList attributeList)
    {
      int count = 0;
      foreach (var attributField in attributeList)
      {
        KeyValuePair<string, object?> newAttributField = new($"{field.Key}[{count}]", attributField);
        Func<Base, object?> functionAdded = x => (function(x) as List<object?>)?[count];
        TraverseAttributes(newAttributField, functionAdded, fieldsAndFunctions, fieldAdded);
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
      string key = field.Key;
      string cleanKey = _characterCleaner.CleanCharacters(key);

      if (cleanKey == FID_FIELD_NAME) // we cannot add field with reserved name
      {
        return;
      }

      if (!fieldAdded.Contains(cleanKey))
      {
        // use field.Value to define FieldType
        FieldType fieldType = GISAttributeFieldType.GetFieldTypeFromRawValue(field.Value);

        FieldDescription fieldDescription = new(cleanKey, fieldType) { AliasName = key };
        fieldsAndFunctions.Add((fieldDescription, function));
        fieldAdded.Add(cleanKey);
      }
      else
      {
        // if field exists, check field.Value again, and revise FieldType if needed
        int index = fieldsAndFunctions.TakeWhile(x => x.Item1.Name != cleanKey).Count();

        (FieldDescription, Func<Base, object?>) itemInList;
        try
        {
          itemInList = fieldsAndFunctions[index];
        }
        catch (Exception ex) when (!ex.IsFatal())
        {
          return;
        }

        FieldType existingFieldType = itemInList.Item1.FieldType;
        FieldType newFieldType = GISAttributeFieldType.GetFieldTypeFromRawValue(field.Value);

        // adjust FieldType if needed, default everything to Strings if fields types differ:
        // 1. change to NewType, if old type was undefined ("Blob")
        // 2. change to NewType if it's String (and the old one is not)
        if (
          newFieldType != FieldType.Blob && existingFieldType == FieldType.Blob
          || (newFieldType == FieldType.String && existingFieldType != FieldType.String)
        )
        {
          fieldsAndFunctions[index] = (
            new FieldDescription(itemInList.Item1.Name, newFieldType) { AliasName = itemInList.Item1.AliasName },
            itemInList.Item2
          );
        }
      }
    }
    catch (GeodatabaseFieldException)
    {
      // do nothing
    }
  }
}
