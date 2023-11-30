using System;
using System.Collections.Generic;
using DesktopUI2.Models.Settings;
using Speckle.Newtonsoft.Json;
using Speckle.Newtonsoft.Json.Linq;

namespace DesktopUI2.Models.Filters;

public class SettingsConverter : JsonConverter
{
  public override bool CanConvert(Type objectType)
  {
    return objectType == typeof(ISetting);
  }

  public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
  {
    var settings = new List<ISetting>();
    JArray jsonArray = null;

    try
    {
      jsonArray = JArray.Load(reader);
    }
    catch (Exception ex)
    {
      return settings;
    }

    foreach (JObject jsonObject in jsonArray)
    {
      ISetting setting = default;
      var type = jsonObject.Value<string>("Type");

      if (type == typeof(CheckBoxSetting).ToString())
      {
        setting = new CheckBoxSetting();
      }
      else if (type == typeof(ListBoxSetting).ToString())
      {
        setting = new ListBoxSetting();
      }
      else if (type == typeof(MappingSetting).ToString())
      {
        setting = new MappingSetting();
      }
      else if (type == typeof(MultiSelectBoxSetting).ToString())
      {
        setting = new MultiSelectBoxSetting();
      }
      else if (type == typeof(NumericSetting).ToString())
      {
        setting = new NumericSetting();
      }
      else if (type == typeof(TextBoxSetting).ToString())
      {
        setting = new TextBoxSetting();
      }
      else
      {
        continue;
      }

      serializer.Populate(jsonObject.CreateReader(), setting);

      settings.Add(setting);
    }

    return settings;
  }

  public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
  {
    serializer.Serialize(writer, value);
  }
}
