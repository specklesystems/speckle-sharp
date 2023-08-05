using DUI3.Utils;

namespace DUI3.Models;

public class FormInputBase : DiscriminatedObject
{
  public string Label { get; set; }
  public bool ShowLabel { get; set; } = false;
}

public class FormTextInput : FormInputBase {
  public string Value { get; set; }
  public string Placeholder { get; set; }
}

public class BooleanValueInput: FormInputBase
{
  public bool Value { get; set; } = false;
}

