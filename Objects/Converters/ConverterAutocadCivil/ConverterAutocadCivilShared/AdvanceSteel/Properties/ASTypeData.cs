#if ADVANCESTEEL
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Objects.Converter.AutocadCivil
{
  public class ASTypeData
  {
    internal string Description { get; private set; }

    internal Dictionary<string, ASProperty> PropertiesSpecific { get; private set; }

    internal Dictionary<string, ASProperty> PropertiesAll { get; private set; }

    internal ASTypeData(string description)
    {
      Description = description;

      PropertiesAll = new Dictionary<string, ASProperty>();
    }

    internal void SetPropertiesSpecific(Dictionary<string, ASProperty> properties)
    {
      PropertiesSpecific = properties;
    }

    internal void AddPropertiesAll(Dictionary<string, ASProperty> properties)
    {
      foreach (var item in properties)
      {
        if (PropertiesAll.ContainsKey(item.Key))
          throw new Exception($"Property '{item.Key}' already added");
        
        PropertiesAll.Add(item.Key, new ASProperty(item.Value));
      }
    }

    internal void OrderDictionaryPropertiesAll()
    {
      PropertiesAll = (from entry in PropertiesAll orderby entry.Key ascending select entry).ToDictionary(x => x.Key, y => y.Value);
    }
  }
}
#endif
