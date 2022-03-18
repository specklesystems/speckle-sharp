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
      var jsonObject = JObject.Load(reader);
      var filer = default(ISelectionFilter);



      var type = jsonObject.Value<string>("Type");


      if (type == typeof(AllSelectionFilter).ToString())
      {
        filer = new AllSelectionFilter();
      }
      else if (type == typeof(ListSelectionFilter).ToString())
      {
        filer = new ListSelectionFilter();
      }
      else if (type == typeof(ManualSelectionFilter).ToString())
      {
        filer = new ManualSelectionFilter();
      }
      else if (type == typeof(PropertySelectionFilter).ToString())
      {
        filer = new PropertySelectionFilter();
      }
      else
      {
        throw new SpeckleException($"Unknown filter type: {type}. Please add a case in DesktopUI2.Models.Filters.FilerUtils.cs");
      }


      serializer.Populate(jsonObject.CreateReader(), filer);
      return filer;

    }

    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
    {
      serializer.Serialize(writer, value);
    }
  }
}
