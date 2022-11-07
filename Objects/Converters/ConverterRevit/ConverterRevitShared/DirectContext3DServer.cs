using System;
using System.Collections.Generic;
using System.Text;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.DirectContext3D;
using Autodesk.Revit.DB.ExternalService;

namespace ConverterRevitShared
{
  internal class DirectContext3DServer : IDirectContext3DServer
  {
    public bool CanExecute(View dBView)
    {
      throw new NotImplementedException();
    }

    public string GetApplicationId()
    {
      throw new NotImplementedException();
    }

    public Outline GetBoundingBox(View dBView)
    {
      throw new NotImplementedException();
    }

    public string GetDescription()
    {
      throw new NotImplementedException();
    }

    public string GetName()
    {
      throw new NotImplementedException();
    }

    public Guid GetServerId()
    {
      throw new NotImplementedException();
    }

    public ExternalServiceId GetServiceId() => ExternalServices.BuiltInExternalServices.DirectContext3DService;

    public string GetSourceId()
    {
      throw new NotImplementedException();
    }

    public string GetVendorId() => "Speckle";

    public void RenderScene(View dBView, DisplayStyle displayStyle)
    {
      throw new NotImplementedException();
    }

    public bool UseInTransparentPass(View dBView)
    {
      throw new NotImplementedException();
    }

    public bool UsesHandles()
    {
      throw new NotImplementedException();
    }
  }
}
