using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Reflection;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Special;

namespace ConnectorGrasshopperUtils;

public static class UserInterfaceUtils
{
  /// <summary>
  /// Used to Create
  /// </summary>
  /// <param name="name"></param>
  /// <param name="x"></param>
  /// <param name="y"></param>
  /// <typeparam name="T"></typeparam>
  /// <returns></returns>
  public static GH_ValueList CreateEnumDropDown(Type enumType, string name, float xPos, float yPos, bool sorted = true)
  {
    if (!enumType.IsEnum)
    {
      throw new ArgumentException($"Input type must be {typeof(Enum)} but got {enumType.Name} ", nameof(enumType));
    }

    // Convert all values of an enum into a GH_ValueListItem and add them to the value list.
    // This preserves both the name of the Enum value, as well as it's declared integer value (saved as a string).
    var items = Enum.GetValues(enumType)
      .Cast<Enum>()
      .Select(x => new GH_ValueListItem(name: x.ToString(), expression: Convert.ToInt32(x).ToString()))
      .ToList();

    var valueList = new GH_ValueList();
    valueList.CreateAttributes();
    valueList.Name = name;
    valueList.NickName = name + ":";
    valueList.Description = "Select an option...";
    valueList.ListMode = GH_ValueListMode.DropDown;
    valueList.Attributes.Pivot = new PointF(xPos, yPos);

    valueList.ListItems.Clear();
    valueList.ListItems.AddRange(items);

    if (sorted)
    {
      valueList.ListItems.Sort((itemA, itemB) => string.Compare(itemA.Name, itemB.Name));
    }

    return valueList;
  }

  /// <summary>
  /// Creates a dropdown component on the canvas for every <see cref="Enum"/> type input of the component.
  /// This assumes the inputs have already been created, and that there is a 1 to 1 relationship between
  /// the number of inputs of the component and the number of properties.
  /// </summary>
  /// <param name="component">The component to connect the dropdowns to</param>
  /// <param name="props">The <see cref="PropertyInfo"/> list of the class being created as a component.</param>
  public static void CreateCanvasDropdownForAllEnumInputs(IGH_Component component, ParameterInfo[] props)
  {
    //Expiring solution ensures the size and position of the parent are  updated before placing any input connections
    component.ExpireSolution(true);

    for (int index = 0; index < props.Length; index++)
    {
      ParameterInfo p = props[index];
      if (!p.ParameterType.IsEnum)
      {
        continue;
      }

      var options = CreateEnumDropDown(
        p.ParameterType,
        p.Name,
        component.Attributes.Bounds.X - 200,
        component.Params.Input[index].Attributes.Bounds.Y - 1
      );
      component.OnPingDocument().AddObject(options, false);
      component.Params.Input[index].AddSource(options);
    }
  }
}
