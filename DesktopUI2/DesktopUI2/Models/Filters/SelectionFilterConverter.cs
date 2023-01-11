using Speckle.Core.Logging;
using Speckle.Newtonsoft.Json;
using Speckle.Newtonsoft.Json.Linq;
using System;

namespace DesktopUI2.Models.Filters
{
  public class SelectionFilterConverter : JsonConverter
  {
    public override bool CanConvert(Type objectType)
    {
      return objectType == typeof(ISelectionFilter);
    }

    public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
    {

      ISelectionFilter filter = default(ISelectionFilter);

      JObject jsonObject = null;
      try
      {
        jsonObject = JObject.Load(reader);
      }
      catch
      {
        return filter;
      }

      var type = jsonObject.Value<string>("Type");

      if (type == typeof(AllSelectionFilter).ToString())
      {
        filter = new AllSelectionFilter();
      }
      else if (type == typeof(ListSelectionFilter).ToString())
      {
        filter = new ListSelectionFilter();
      }
      else if (type == typeof(ManualSelectionFilter).ToString())
      {
        filter = new ManualSelectionFilter();
      }
      else if (type == typeof(PropertySelectionFilter).ToString())
      {
        filter = new PropertySelectionFilter();
      }
      else
      {
        throw new SpeckleException($"Unknown filter type: {type}. Please add a case in DesktopUI2.Models.Filters.SelectionFilterConverter.cs");
      }

      serializer.Populate(jsonObject.CreateReader(), filter);
      return filter;

    }

    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
    {
      serializer.Serialize(writer, value);
    }
  }
}
