using RevitSharedResources.Extensions.SpeckleExtensions;
using Speckle.Core.Logging;
using Speckle.Core.Models;
using Speckle.Core.Models.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using Mesh = Objects.Geometry.Mesh;

namespace Objects.Converter.Revit;

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
      appObj.Update(
        status: ApplicationObject.State.Failed,
        logItem: $"No displayable meshes found for object of type {@object.speckle_type}"
      );
      return appObj;
    }
#pragma warning disable CA1031 // Do not catch general exception types
    try
    {
      server = new DirectContext3DServer(meshes, Doc);
      appObj.Update(status: ApplicationObject.State.Created);
    }
    catch (Exception ex)
    {
      // TODO : check if catch statement is necessary
      SpeckleLog.Logger.LogDefaultError(ex);
      appObj.Update(status: ApplicationObject.State.Failed, logItem: ex.Message);
    }
#pragma warning restore CA1031 // Do not catch general exception types

    appObj.Converted = new List<object> { server };
    return appObj;
  }
}
