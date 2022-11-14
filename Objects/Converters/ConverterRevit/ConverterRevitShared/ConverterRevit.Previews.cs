using ConverterRevitShared;
using Objects.Geometry;
using Speckle.Core.Models;
using System;
using System.Collections.Generic;
using System.Text;
using Mesh = Objects.Geometry.Mesh;

namespace Objects.Converter.Revit
{
  public partial class ConverterRevit
  {
    private ApplicationObject PreviewGeometry(Base @object)
    {
      var appObj = new ApplicationObject(null, null);

      var x = new DirectContext3DServer(@object, Doc);

      appObj.Converted = new List<object> { x };
      return appObj;
    }
  }
}
