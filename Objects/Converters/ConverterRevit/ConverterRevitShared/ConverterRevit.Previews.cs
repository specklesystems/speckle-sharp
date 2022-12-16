using ConverterRevitShared;
using Objects.Geometry;
using Speckle.Core.Models;
using Speckle.Core.Models.Extensions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Mesh = Objects.Geometry.Mesh;

namespace Objects.Converter.Revit
{
  public partial class ConverterRevit
  {
    private ApplicationObject PreviewGeometry(Base @object)
    {
      var appObj = new ApplicationObject(@object.applicationId, @object.speckle_type);
      DirectContext3DServer server = null;

      Instance = this;
      var meshes = @object.Flatten().Where(b => b is Mesh).Cast<Mesh>();
      if (meshes == null || !meshes.Any())
      {
        appObj.Update(status: ApplicationObject.State.Failed, logItem: $"No displayable meshes found for object of type {@object.speckle_type}");
        return appObj;
      }
      try
      {
        server = new DirectContext3DServer(meshes, Doc);
        appObj.Update(status: ApplicationObject.State.Created);
      }
      catch (Exception ex)
      {
        appObj.Update(status: ApplicationObject.State.Failed, logItem: ex.Message);
      }

      appObj.Converted = new List<object> { server };
      return appObj;
    }
  }
}
