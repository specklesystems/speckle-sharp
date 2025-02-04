using System.Collections.Generic;
using Autodesk.Revit.DB;
using Objects.BuiltElements.Revit;
using Speckle.Core.Models;

namespace Objects.Converter.Revit;

public partial class ConverterRevit
{
  public FreeformElement CombinableElementToSpeckle(CombinableElement combinableElement)
  {
    var cat = ((BuiltInCategory)combinableElement.Document.OwnerFamily.FamilyCategoryId.IntegerValue).ToString();

    Options options = new();
    if (combinableElement is GenericForm gf && gf.Combinations.Size != 0)
    {
      //for receive and convert to native
      options.IncludeNonVisibleObjects = true;
    }

    var element = combinableElement.get_Geometry(options);
    var geometries = new List<Base>();
    foreach (var obj in element)
    {
      switch (obj)
      {
        case Autodesk.Revit.DB.Mesh mesh:
          geometries.Add(MeshToSpeckle(mesh, combinableElement.Document));
          break;
        case Solid solid: // TODO Should be replaced with 'BrepToSpeckle' when it works.
          geometries.AddRange(ConvertSolidsByRenderMaterial(new[] { solid }, combinableElement.Document));
          break;
      }
    }
    var speckleForm = new FreeformElement();
    speckleForm.subcategory = cat;
    speckleForm["type"] = combinableElement.Name;
    speckleForm.baseGeometries = new List<Base>();

    if (combinableElement is GenericForm)
    {
      speckleForm.baseGeometries = geometries;
      GetAllRevitParamsAndIds(speckleForm, combinableElement);
    }

    if (
      combinableElement is GeomCombination
      || (combinableElement is GenericForm genericForm && genericForm.Combinations.Size == 0)
    )
    {
      List<Base> displayValue = new();
      foreach (Base geo in geometries)
      {
        switch (geo["displayValue"])
        {
          case null:
            //geo has no display value, we assume it is itself a valid displayValue
            displayValue.Add(geo);
            break;

          case Base b:
            displayValue.Add(b);
            break;

          case IEnumerable<Base> e:
            displayValue.AddRange(e);
            break;
        }
      }

      speckleForm.displayValue = displayValue;
    }

    return speckleForm;
  }
}
