#if ADVANCESTEEL2023
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Autodesk.AdvanceSteel.Arrangement;
using Autodesk.AdvanceSteel.CADAccess;
using Autodesk.AdvanceSteel.Connection;
using Autodesk.AdvanceSteel.ConstructionTypes;
using Autodesk.AdvanceSteel.Modelling;

namespace Objects.Converter.AutocadCivil
{
  public class ASPropertiesCache
  {
    private ASPropertiesCache()
    {

    }

    private static ASPropertiesCache instance;

    public static ASPropertiesCache Instance
    {
      get 
      {
        if (instance == null) 
        {
          instance = new ASPropertiesCache();
          instance.LoadASTypeDictionary();
        }

        return instance;
      }

    }

    private readonly Dictionary<Type, ASTypeData> ASPropertySets = new Dictionary<Type, ASTypeData>()
    {
      { typeof(AtomicElement), new ASTypeData ("Atomic Element") },
      { typeof(PolyBeam), new ASTypeData("Poly Beam") }
    };

    internal Dictionary<string, ASProperty> GetAllProperties(Type objectType, out string typeDescription)
    {
      if (!ASPropertySets.ContainsKey(objectType))
      {
        throw new Exception($"Properties not found for type '{objectType}'");
      }

      typeDescription = ASPropertySets[objectType].Description;

      return ASPropertySets[objectType].PropertiesAll;
    }

    #region Load dictionary

    /// <summary>
    /// Load all properties of each Advance Steel object type
    /// </summary>
    private void LoadASTypeDictionary()
    {
      var assemblyTypes = System.Reflection.Assembly.GetExecutingAssembly().GetTypes();
      var listIASProperties = assemblyTypes.Where(x => !x.IsAbstract && x.GetInterfaces().Contains(typeof(IASProperties))).Select(x => Activator.CreateInstance(x) as IASProperties);

      //Set specific properties of each type
      foreach (var item in ASPropertySets)
      {
        IASProperties asProperties = listIASProperties.FirstOrDefault(x => x.ObjectType.Equals(item.Key));

        if (asProperties == null)
        {
          throw new NotImplementedException($"{item.Key.ToString()} not implemented");
        }

        item.Value.SetPropertiesSpecific(asProperties.BuildedPropertyList());
      }

      //Set all properties using parents classes
      foreach (var item in ASPropertySets)
      {
        IEnumerable<ASTypeData> steelTypeDataList = ASPropertySets.Where(x => item.Key.IsSubclassOf(x.Key) || item.Key.IsEquivalentTo(x.Key)).Select(x => x.Value);
        foreach (var steelTypeData in steelTypeDataList)
        {
          item.Value.AddPropertiesAll(steelTypeData.PropertiesSpecific);
        }

        item.Value.OrderDictionaryPropertiesAll();
      }
    }

    #endregion

  }
}
#endif
