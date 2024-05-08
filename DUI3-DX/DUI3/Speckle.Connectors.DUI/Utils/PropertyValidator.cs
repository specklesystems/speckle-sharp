using Speckle.Connectors.Utils;
using Speckle.Newtonsoft.Json;

namespace Speckle.Connectors.DUI.Utils;

public class PropertyValidator : DiscriminatedObject
{
  [JsonIgnore]
  public List<string>? JsonPropertyNames { get; set; }

  public bool InitializeNewProperties()
  {
    bool isUpdated = false;
    var properties = this.GetType().GetProperties();

    // Create a new instance of the current type to get default values
    var defaultInstance = Activator.CreateInstance(this.GetType());

    foreach (var property in properties)
    {
      if (property.GetValue(this) == null)
      {
        // Get the default value from the new instance
        var defaultValue = property.GetValue(defaultInstance);

        // Set this default value to the current instance
        property.SetValue(this, defaultValue);
        isUpdated = true;
      }
    }

    return isUpdated; // Return true if any property was updated
  }

  public bool CheckRemovedProperties()
  {
    bool removedPropertiesExist = false;
    var currentPropertyNames = this.GetType().GetProperties().Select(p => p.Name).ToList();

    foreach (var jsonPropName in JsonPropertyNames.Empty())
    {
      if (!currentPropertyNames.Contains(jsonPropName))
      {
        // This property was in the JSON but not in the class
        removedPropertiesExist = true;
      }
    }

    return removedPropertiesExist;
  }
}
