#if ADVANCESTEEL
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using Autodesk.AdvanceSteel.DocumentManagement;
using Autodesk.AdvanceSteel.DotNetRoots.Units;
using Objects.BuiltElements;
using Speckle.Newtonsoft.Json.Linq;
using static Autodesk.AdvanceSteel.DotNetRoots.Units.Unit;

namespace Objects.Converter.AutocadCivil;

public class ASProperty
{
  private Type ObjectType { get; set; }
  private string Description { get; set; }
  private string MemberName { get; set; }
  private MemberInfo MemberInfo { get; set; }
  public Type ValueType { get; private set; }
  private ASPropertyMethods PropertyMethods { get; set; }

  private bool Read { get; set; }
  private bool Write { get; set; }

  public eUnitType? UnitType { get; private set; }

  internal ASProperty(Type objectASType, string description, string memberName, eUnitType? unitType = null)
  {
    ObjectType = objectASType;
    Description = description;
    MemberName = memberName;

    UnitType = unitType;

    PropertyInfo property = objectASType.GetProperty(memberName);
    if (property != null)
    {
      ValueType = property.PropertyType;
      Read = property.CanRead;
      Write = property.CanWrite;

      MemberInfo = property;
      return;
    }

    MethodInfo method = objectASType.GetMethod(memberName, new Type[] { });
    if (method != null)
    {
      ValueType = method.ReturnType;
      Read = true;
      Write = false;

      MemberInfo = method;
      return;
    }

    throw new Exception(
      $"'{memberName}' is not a property nor return method with 0 arguments nor void method with 1 argument"
    );
  }

  internal ASProperty(
    Type objectType,
    string description,
    ASPropertyMethods propertyMethods,
    eUnitType? unitType = null
  )
  {
    ObjectType = objectType;
    Description = description;
    UnitType = unitType;

    PropertyMethods = propertyMethods;
    Read = PropertyMethods.MethodInfoGet != null;
    Write = PropertyMethods.MethodInfoSet != null;

    if (PropertyMethods.MethodInfoGet != null)
    {
      ValueType = PropertyMethods.MethodInfoGet.ReturnType;
    }

    if (PropertyMethods.MethodInfoSet != null)
    {
      ValueType = PropertyMethods.MethodInfoSet.GetParameters()[1].ParameterType;
    }

    //Same name for description and membername
    MemberName = description;
  }

  internal ASProperty(ASProperty existingProperty)
  {
    CopyProperty(existingProperty);
  }

  internal void CopyProperty(ASProperty existingProperty)
  {
    Read = existingProperty.Read;
    Write = existingProperty.Write;
    ObjectType = existingProperty.ObjectType;
    Description = existingProperty.Description;
    MemberInfo = existingProperty.MemberInfo;
    MemberName = existingProperty.MemberName;
    ValueType = existingProperty.ValueType;
    PropertyMethods = existingProperty.PropertyMethods;

    UnitType = existingProperty.UnitType;
  }

  internal object EvaluateValue(object asObject)
  {
    if (asObject == null)
    {
      throw new System.Exception("Advance Steel object not found");
    }

    if (!asObject.GetType().IsSubclassOf(ObjectType) && !asObject.GetType().IsEquivalentTo(ObjectType))
    {
      throw new System.Exception(string.Format("Not '{0}' object", ObjectType.Name));
    }

    try
    {
      object value = GetObjectPropertyValue(asObject);
      return value;
    }
    catch (Exception)
    {
      throw new System.Exception($"Object has no property - {Description?.ToString()}");
    }
  }

  private object GetObjectPropertyValue(object asObject)
  {
    if (MemberInfo is PropertyInfo)
    {
      PropertyInfo propertyInfo = MemberInfo as PropertyInfo;
      return propertyInfo.GetValue(asObject);
    }
    else if (MemberInfo is MethodInfo)
    {
      MethodInfo methodInfo = MemberInfo as MethodInfo;
      return methodInfo.Invoke(asObject, null);
    }
    else if (PropertyMethods?.MethodInfoGet != null)
    {
      return PropertyMethods.MethodInfoGet.Invoke(null, new object[] { asObject });
    }
    else
    {
      throw new NotImplementedException();
    }
  }
}
#endif
