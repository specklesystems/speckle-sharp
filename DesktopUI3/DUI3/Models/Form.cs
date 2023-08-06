using System.Collections.Generic;
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

public class ListValueInput : FormInputBase
{
  public List<ListValueItem> Options { get; set; } = new();
  public List<ListValueItem> SelectedOptions { get; set; } = new();
  public bool MultiSelect { get; set; } = true;
}

public class ListValueItem : DiscriminatedObject
{
  public string Id { get; set; }
  public string Name { get; set; }
  public string Color { get; set; }
}

public class TreeValueInput : FormInputBase
{
  public List<TreeListValueItem> Nodes { get; set; } = new();
  public bool MultiSelect { get; set; } = true;
}

public class TreeListValueItem : ListValueItem
{
  public bool Selected { get; set; }
  public List<TreeListValueItem> Nodes { get; set; } = new();
}
