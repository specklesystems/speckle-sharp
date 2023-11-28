using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using ConnectorGrasshopper.Properties;
using Grasshopper.GUI.Canvas;
using Grasshopper.Kernel;

namespace ConnectorGrasshopper.Extras;

public class OptionalStateTag : SpeckleStateTag
{
  public override string Description => "Determines if a parameter is optional";
  public override string Name => "Optional";
  public override Bitmap Icon => Resources.StateTag_Optional;
  public override bool Crossed => false;
  public override string Letter => "?";
}

public class SchemaTagStateTag : SpeckleStateTag
{
  public override string Description =>
    "Will output the main geometry with the schema attached as a 'SpeckleSchema' property";
  public override string Name => "Schema Tag";
  public override Bitmap Icon => Resources.StateTag_Optional;
  public override bool Crossed => false;
  public override string Letter => "#";
}

public class DetachedStateTag : SpeckleStateTag
{
  public override string Description => "This property will be detached";
  public override string Name => "Detached";
  public override Bitmap Icon => Resources.StateTag_Detach;
  public override bool Crossed => false;
  public override string Letter => "@";
}

public class ListAccesStateTag : SpeckleStateTag
{
  public override string Description => "This parameter is set to List access";
  public override string Name => "List Access";
  public override Bitmap Icon => Resources.StateTag_List;
  public override bool Crossed => false;
  public override string Letter => "L";
}

public abstract class SpeckleStateTag : GH_StateTag
{
  public override string Description { get; }
  public override string Name { get; }
  public override Bitmap Icon { get; }

  public abstract bool Crossed { get; }

  public abstract string Letter { get; }

  public override void Render(Graphics graphics)
  {
    if (GH_Canvas.ZoomFadeLow < 5)
    {
      return;
    }

    RenderSpeckleTagBlankIcon(graphics);
    RenderSpeckleTagContents(graphics);
    if (Crossed)
    {
      RenderRedDiagonalLine(graphics);
    }
  }

  public void RenderSpeckleTagBlankIcon(Graphics graphics)
  {
    int zoomFadeLow = GH_Canvas.ZoomFadeLow;
    Rectangle stage = Stage;
    --stage.Width;
    --stage.Height;
    stage.Inflate(-1, -1);
    var roundedRectangle1 = GH_CapsuleRenderEngine.CreateRoundedRectangle(stage, 2);
    stage.Inflate(-1, -1);
    var roundedRectangle2 = GH_CapsuleRenderEngine.CreateRoundedRectangle(stage, 1);
    var linearGradientBrush1 = new LinearGradientBrush(
      stage,
      Color.FromArgb(zoomFadeLow, 240, 240, 240),
      Color.FromArgb(zoomFadeLow, 185, 185, 185),
      LinearGradientMode.Vertical
    );
    var linearGradientBrush2 = new LinearGradientBrush(
      stage,
      Color.FromArgb(0, Color.White),
      Color.FromArgb(Convert.ToInt32(zoomFadeLow * 0.8), Color.White),
      LinearGradientMode.Vertical
    );
    linearGradientBrush1.WrapMode = WrapMode.TileFlipXY;
    linearGradientBrush2.WrapMode = WrapMode.TileFlipXY;
    Pen pen1 = new(Color.FromArgb(zoomFadeLow, 10, 107, 252));
    Pen pen2 = new(linearGradientBrush2);
    graphics.FillPath(linearGradientBrush1, roundedRectangle1);
    // if (drawCallBack != null)
    //   drawCallBack(graphics, (double) zoomFadeLow);
    graphics.DrawPath(pen1, roundedRectangle1);
    graphics.DrawPath(pen2, roundedRectangle2);
    pen1.Dispose();
    pen2.Dispose();
    linearGradientBrush1.Dispose();
    linearGradientBrush2.Dispose();
    roundedRectangle1.Dispose();
    roundedRectangle2.Dispose();
  }

  public void RenderSpeckleTagContents(Graphics graphics)
  {
    var p = new GraphicsPath();
    var format = StringFormat.GenericDefault;
    format.Alignment = StringAlignment.Center;
    p.AddString(
      Letter,
      FontFamily.GenericMonospace,
      1,
      Stage.Height - 2,
      new Point(Stage.Location.X + Stage.Width / 2, Stage.Location.Y - 1),
      format
    );

    LinearGradientBrush blueGradient =
      new(
        Stage,
        Color.FromArgb(100, 10, 107, 252),
        Color.FromArgb(Convert.ToInt32((double)GH_Canvas.ZoomFadeLow), Color.FromArgb(0, 10, 107, 252)),
        LinearGradientMode.Vertical
      );

    graphics.FillPath(blueGradient, p);
    blueGradient.Dispose();
    p.Dispose();
  }

  public void RenderRedDiagonalLine(Graphics graphics)
  {
    var stage = Stage;
    --stage.Height;
    --stage.Width;
    var offX = 3;
    var offY = 4;
    var ptA = new Point(stage.Location.X + stage.Width - offX, stage.Location.Y + offY);
    var ptB = new Point(stage.Location.X + offX, stage.Location.Y + stage.Height - offY);
    var g = new GraphicsPath();
    g.AddLine(ptA, ptB);
    var redPen = new Pen(Color.Firebrick);
    graphics.DrawLine(redPen, ptA, ptB);
    redPen.Dispose();
    g.Dispose();
  }
}
