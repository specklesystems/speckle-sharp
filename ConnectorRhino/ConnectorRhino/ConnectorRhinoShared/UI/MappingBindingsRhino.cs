using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using DesktopUI2;
using DesktopUI2.Models;
using DesktopUI2.Models.Filters;
using DesktopUI2.Models.Settings;
using DesktopUI2.ViewModels;
using Rhino;
using Rhino.Display;
using Rhino.DocObjects;
using Rhino.Geometry;
using Rhino.Render;
using Speckle.Core.Api;
using Speckle.Core.Kits;
using Speckle.Core.Logging;
using Speckle.Core.Models;
using Speckle.Core.Transports;
using Speckle.Newtonsoft.Json;
using static DesktopUI2.ViewModels.MappingViewModel;
using ApplicationObject = Speckle.Core.Models.ApplicationObject;

namespace SpeckleRhino
{
  public partial class MappingBindingsRhino : MappingsBindings
  {

    public MappingBindingsRhino()
    {

    }

    public override List<Base> GetSelection()
    {
      var selection = RhinoDoc.ActiveDoc.Objects.GetSelectedObjects(false, false).ToList();


      var converter = KitManager.GetDefaultKit().LoadConverter(Utils.RhinoAppName);
      if (converter == null)
      {
        throw (new SpeckleException("Could not find any Kit!"));
      }
      converter.SetContextDocument(RhinoDoc.ActiveDoc);

      var speckleObjects = new List<Base>();

      foreach (var obj in selection)
      {
        if (obj == null || !converter.CanConvertToSpeckle(obj))
          continue;
        var converted = converter.ConvertToSpeckle(obj);
        if (converted != null)
          speckleObjects.Add(converted);

      }

      return speckleObjects;
    }

    public override void SetMappings()
    {

    }
  }
}
