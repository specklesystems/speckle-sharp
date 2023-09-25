using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using DesktopUI2;
using Rhino.Display;
using Rhino.Geometry;

namespace SpeckleRhino;

public partial class ConnectorBindingsRhino : ConnectorBindings
{
  public override bool CanOpen3DView => true;

  public override async Task Open3DView(List<double> viewCoordinates, string viewName = "")
  {
    // Create positional objects for camera
    Point3d cameraLocation = new(viewCoordinates[0], viewCoordinates[1], viewCoordinates[2]);
    Point3d target = new(viewCoordinates[3], viewCoordinates[4], viewCoordinates[5]);
    Vector3d direction = target - cameraLocation;

    if (!Doc.Views.Any(v => v.ActiveViewport.Name == "SpeckleCommentView"))
    {
      // Get bounds from active view
      Rectangle bounds = Doc.Views.ActiveView.ScreenRectangle;
      // Reset margins
      bounds.X = 0;
      bounds.Y = 0;
      Doc.Views.Add("SpeckleCommentView", DefinedViewportProjection.Perspective, bounds, false);
    }

    await Task.Run(() =>
    {
      IEnumerable<RhinoView> views = Doc.Views.Where(v => v.ActiveViewport.Name == "SpeckleCommentView");
      if (views.Any())
      {
        RhinoView speckleCommentView = views.First();
        speckleCommentView.ActiveViewport.SetCameraDirection(direction, false);
        speckleCommentView.ActiveViewport.SetCameraLocation(cameraLocation, true);

        DisplayModeDescription shaded = DisplayModeDescription.FindByName("Shaded");
        if (shaded != null)
          speckleCommentView.ActiveViewport.DisplayMode = shaded;

        // Minimized all maximized views.
        IEnumerable<RhinoView> maximizedViews = Doc.Views.Where(v => v.Maximized);
        foreach (RhinoView view in maximizedViews)
          view.Maximized = false;

        // Maximized speckle comment view.
        speckleCommentView.Maximized = true;

        if (Doc.Views.ActiveView.ActiveViewport.Name != "SpeckleCommentView")
          Doc.Views.ActiveView = speckleCommentView;
      }

      Doc.Views.Redraw();
    });
  }
}
