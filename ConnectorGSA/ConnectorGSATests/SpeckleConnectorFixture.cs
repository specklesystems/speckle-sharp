using Moq;
using Speckle.ConnectorGSA.Proxy.Cache;
using Speckle.GSA.API;
using Speckle.GSA.API.GwaSchema;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ConnectorGSATests
{
  public class SpeckleConnectorFixture
  {
    protected string TestDataDirectory { get => AppDomain.CurrentDomain.BaseDirectory.TrimEnd(new[] { '\\' }) + @"\..\..\..\TestModels\"; }

    protected string designLayerExpectedFile = "DesignLayerSpeckleObjects.json";
    protected string modelWithoutResultsFile = "Structural Demo.gwb";
    protected string modelWithResultsFile = "Structural Demo Results.gwb";

    //protected GsaModelMock
    protected Mock<IGSAModel> gsaModelMock;
    protected GsaCache cache;

    public SpeckleConnectorFixture()
    {
      gsaModelMock = new Mock<IGSAModel>();
      gsaModelMock.SetupGet(x => x.GwaDelimiter).Returns('\t');
      gsaModelMock.Setup(x => x.ConvertGSAList(It.IsAny<string>(), It.IsAny<GSAEntity>())).Returns(new Func<string, GSAEntity, List<int>>(ConvertGSAList));
      gsaModelMock.Setup(x => x.LookupIndices(It.IsAny<GwaKeyword>())).Returns((GwaKeyword kw) => cache.LookupIndices(kw).Where(i => i.HasValue).Select(i => i.Value).ToList());
    }

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
      GwaKeyword.SECTION,
      GwaKeyword.MAT_STEEL,
      GwaKeyword.MAT_CONCRETE,
      GwaKeyword.LOAD_BEAM,
      GwaKeyword.LOAD_NODE,
      GwaKeyword.COMBINATION,
      GwaKeyword.LOAD_TITLE,
      GwaKeyword.PROP_MASS,
      GwaKeyword.GRID_LINE
    };

    protected static List<int> ConvertGSAList(string list, GSAEntity type)
    {
      var elements = list.Split(new[] { ' ' });

      var indices = new List<int>();
      foreach (var e in elements)
      {
        if (e.All(c => char.IsDigit(c)) && int.TryParse(e, out int index))
        {
          indices.Add(index);
        }
      }

      //It's assumed for now that any list of GSA indices that would correspond to the App IDs in the list would be a sequence from 1
      return indices;
    }
  }
}
