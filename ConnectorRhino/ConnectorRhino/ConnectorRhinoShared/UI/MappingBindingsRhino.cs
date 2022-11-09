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
using DesktopUI2.ViewModels.MappingTool;
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
    static string SpeckleMappingKey = "SpeckleMapping";
    static string SpeckleMappingViewKey = "SpeckleMappingView";

    public MappingBindingsRhino()
    {

    }

    public override List<Type> GetSelectionSchemas()
    {
      var selection = RhinoDoc.ActiveDoc.Objects.GetSelectedObjects(false, false).ToList();
      var result = new List<Type>();

      var first = true;
      foreach (var obj in selection)
      {
        var schemas = GetObjectSchemas(obj);
        if (first)
        {
          result = schemas;
          first = false;
          continue;
        }
        result = result.Intersect(schemas).ToList();
      }

      return result;
    }

    private List<Type> GetObjectSchemas(RhinoObject obj)
    {
      var result = new List<Type>();


      switch (obj.Geometry)
      {
        case Mesh m:
          result.Add(typeof(DirectShapeFreeformViewModel));
          break;

        case Brep b:
          result.Add(typeof(DirectShapeFreeformViewModel));
          break;
        //case Brep b:
        //  if (b.IsSurface) cats.Add(DirectShape); // TODO: Wall by face, totally faking it right now
        //  else cats.Add(DirectShape);
        //  break;
        case Extrusion e:
          if (e.ProfileCount > 1) break;
          var crv = e.Profile3d(new ComponentIndex(ComponentIndexType.ExtrusionBottomProfile, 0));
          if (!(crv.IsLinear() || crv.IsArc())) break;
          //TODO check what is this and why it wasn't working 
          //if (crv.PointAtStart.Z == crv.PointAtEnd.Z) 
          result.Add(typeof(RevitWallViewModel));
          break;

          //case Curve c:
          //  if (c.IsLinear()) cats.Add(Beam);
          //  if (c.IsLinear() && c.PointAtEnd.Z == c.PointAtStart.Z) cats.Add(Gridline);
          //  if (c.IsLinear() && c.PointAtEnd.X == c.PointAtStart.X && c.PointAtEnd.Y == c.PointAtStart.Y) cats.Add(Column);
          //  if (c.IsArc() && !c.IsCircle() && c.PointAtEnd.Z == c.PointAtStart.Z) cats.Add(Gridline);
          //  break;
      }

      return result;
    }

    public override void SetMappings(string schema, string viewModel)
    {
      var selection = RhinoDoc.ActiveDoc.Objects.GetSelectedObjects(false, false).ToList();
      foreach (var obj in selection)
      {
        obj.Attributes.SetUserString(SpeckleMappingKey, schema);
        obj.Attributes.SetUserString(SpeckleMappingViewKey, viewModel);
      }

    }
  }
}
