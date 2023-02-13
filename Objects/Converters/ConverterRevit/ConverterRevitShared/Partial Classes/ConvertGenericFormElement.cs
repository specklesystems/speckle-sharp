using Autodesk.Revit.DB;
using Objects.BuiltElements.Revit;
using Speckle.Core.Models;
using System;
using System.Collections.Generic;

namespace Objects.Converter.Revit
{
  public partial class ConverterRevit
  {

    public FreeformElement GenericFormToSpeckle(GenericForm genericForm)
    {
      var cat = ((BuiltInCategory)genericForm.Document.OwnerFamily.FamilyCategoryId.IntegerValue).ToString();
      var element = genericForm.get_Geometry(new Options());

      var geometries = new List<Base>();
      foreach (var obj in element)
      {
        switch (obj)
        {
          case Autodesk.Revit.DB.Mesh mesh:
            geometries.Add(MeshToSpeckle(mesh, genericForm.Document));
            break;
          case Solid solid: // TODO Should be replaced with 'BrepToSpeckle' when it works.
            geometries.AddRange(GetMeshesFromSolids(new[] { solid }, genericForm.Document));
            break;
        }
      }

      var speckleForm = new FreeformElement(
        geometries,
        cat
      );

      //Find display values in geometries
      List<Base> displayValue = new List<Base>();
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
      GetAllRevitParamsAndIds(speckleForm, genericForm);
      speckleForm["type"] = genericForm.Name;
      return speckleForm;
    }
  }
}
