using System;
using System.Collections.Generic;
using Objects.Geometry;
using Objects.Structural.Geometry;
using Objects.Structural.Analysis;
using CSiAPIv1;
using Objects.Organization;
using Speckle.Core.Models.Extensions;

namespace Objects.Converter.CSI
{
  public partial class ConverterCSI
  {
    public object BuiltElementModelToNative(Organization.Model model)
    {
      var bases = BaseExtensions.Flatten(model);
      foreach (var @base in bases)
      {

        switch (@base)
        {
          case Element1D o:
            FrameToNative(o);
            Report.Log($"Created Element1D {o.id}");
            break;
          case Element2D o:
            AreaToNative(o);
            Report.Log($"Created Element2D {o.id}");
            break;
          default:
            Report.Log($"Skipped not supported type: {@base.GetType()} {@base.id}");
            break;
        }
      }
      Model.View.RefreshWindow();
      return true;
    }
  }
}

