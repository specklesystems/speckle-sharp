#if ADVANCESTEEL
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace Objects.Converter.AutocadCivil;

public class ASPropertyMethods
{
  internal ASPropertyMethods(Type typeProperties, string methodInfoGet, string methodInfoSet)
  {
    if (!string.IsNullOrEmpty(methodInfoGet))
    {
      MethodInfoGet = typeProperties.GetMethod(methodInfoGet, BindingFlags.Static | BindingFlags.NonPublic);
      if (
        MethodInfoGet == null
        || MethodInfoGet.GetParameters().Length != 1 && MethodInfoGet.ReturnType == typeof(void)
      )
      {
        throw new Exception($"Method Get '{methodInfoGet}' must have 1 parameter, 1 return and be static");
      }
    }

    if (!string.IsNullOrEmpty(methodInfoSet))
    {
      MethodInfoSet = typeProperties.GetMethod(methodInfoSet, BindingFlags.Static | BindingFlags.NonPublic);
      if (
        MethodInfoSet == null
        || MethodInfoSet.GetParameters().Length != 2 && MethodInfoSet.ReturnType != typeof(void)
      )
      {
        throw new Exception($"Method Set '{MethodInfoSet}' must have 2 parameters, void return and be static");
      }
    }

    if (MethodInfoGet == null && MethodInfoSet == null)
    {
      throw new Exception("Must have at least 1 get or set function");
    }

    if (MethodInfoGet != null && MethodInfoSet != null)
    {
      //Compare set parameter with get return
      if (MethodInfoGet.ReturnType != MethodInfoSet.GetParameters()[1].ParameterType)
      {
        throw new Exception("The get return type must be the same second parameter of set method");
      }
    }
  }

  internal MethodInfo MethodInfoGet { get; private set; }

  internal MethodInfo MethodInfoSet { get; private set; }
}
#endif
