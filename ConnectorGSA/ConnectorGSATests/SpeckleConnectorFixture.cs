using Speckle.GSA.API.GwaSchema;
using System;

namespace ConnectorGSATests
{
  public class SpeckleConnectorFixture
  {
    protected string TestDataDirectory { get => AppDomain.CurrentDomain.BaseDirectory.TrimEnd(new[] { '\\' }) + @"\..\..\..\TestModels\"; }

    protected string designLayerExpectedFile = "DesignLayerSpeckleObjects.json";
    protected string modelWithoutResultsFile = "Structural Demo.gwb";
    protected string modelWithResultsFile = "Structural Demo Results.gwb";

    protected static GwaKeyword[] DesignLayerKeywords = new GwaKeyword[] {
      GwaKeyword.LOAD_2D_THERMAL,
      GwaKeyword.ALIGN,
      GwaKeyword.PATH,
      GwaKeyword.USER_VEHICLE,
      GwaKeyword.RIGID,
      GwaKeyword.ASSEMBLY,
      GwaKeyword.LOAD_GRAVITY,
      GwaKeyword.PROP_SPR,
      GwaKeyword.ANAL,
      GwaKeyword.TASK,
      GwaKeyword.GEN_REST,
      GwaKeyword.ANAL_STAGE,
      GwaKeyword.LOAD_GRID_LINE,
      GwaKeyword.GRID_SURFACE,
      GwaKeyword.GRID_PLANE,
      GwaKeyword.AXIS,
      GwaKeyword.MEMB,
      GwaKeyword.NODE,
      GwaKeyword.LOAD_GRID_AREA,
      GwaKeyword.LOAD_2D_FACE,
      GwaKeyword.PROP_2D,
      GwaKeyword.MAT_STEEL,
      GwaKeyword.MAT_CONCRETE,
      GwaKeyword.LOAD_BEAM,
      GwaKeyword.LOAD_NODE,
      GwaKeyword.COMBINATION,
      GwaKeyword.LOAD_TITLE,
      GwaKeyword.PROP_SEC,
      GwaKeyword.PROP_MASS,
      GwaKeyword.GRID_LINE
    };
  }
}
