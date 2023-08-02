namespace DUI3.Utils;

/// <summary>
/// Any polymorphic type base should inherit from this class in order for it to be properly deserialized.
/// - Class inheritance scenario For example, if you have a base class BaseSettings, and from it you create RhinoBaseSettings & AutocadBaseSettings, the BaseSetting class should inherit from this class.
/// - Interface scenario: you have an ISenderCard interface, which you implement as ReceiverCard and SenderCard. Both ReceiverCard and SenderCard should inherit from this class.
/// </summary>
public class DiscriminatedObject
{
  public string TypeDiscriminator { get => this.GetType().Name; set { } } 
}
