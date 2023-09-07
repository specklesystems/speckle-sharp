#if ADVANCESTEEL
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

    private readonly Dictionary<Type, ASTypeData> ASPropertiesSets = new Dictionary<Type, ASTypeData>()
    {
      { typeof(AtomicElement), new ASTypeData ("assembly") },
      { typeof(Beam), new ASTypeData("beam") },
      { typeof(MainAlias), new ASTypeData("manufacturing") },
      { typeof(PolyBeam), new ASTypeData("poly beam") },
      { typeof(BoltPattern), new ASTypeData("bolt") },
      { typeof(ScrewBoltPattern), new ASTypeData("screw bolt") },
      { typeof(ConstructionElement), new ASTypeData("construction") },
      { typeof(FilerObject), new ASTypeData("asteel") }
    };

    /// <summary>
    /// Get all properties sets that are subclasses or equivalents of the type
    /// </summary>
    /// <param name="objectType"></param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    internal IEnumerable<ASTypeData> GetPropertiesSetsByType(Type objectType)
    {
      IEnumerable<ASTypeData> steelTypeDataList = ASPropertiesSets.Where(x => objectType.IsSubclassOf(x.Key) || objectType.IsEquivalentTo(x.Key)).Select(x => x.Value);

      return steelTypeDataList;
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
      foreach (var item in ASPropertiesSets)
      {
        IASProperties asProperties = listIASProperties.FirstOrDefault(x => x.ObjectType.Equals(item.Key));

        if (asProperties == null)
        {
          throw new NotImplementedException($"{item.Key.Name} not implemented");
        }

        item.Value.SetPropertiesSpecific(asProperties.BuildedPropertyList());
        item.Value.OrderDictionaryPropertiesAll();
      }

      //Set all properties using parents classes (checking if all properties have unique name)
      foreach (var item in ASPropertiesSets)
      {
        IEnumerable<ASTypeData> steelTypeDataList = GetPropertiesSetsByType(item.Key);
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
