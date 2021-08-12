using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xunit;
using Speckle.GSA.API;
using Speckle.GSA.API.GwaSchema;
using Objects.Structural;
using Objects.Structural.Geometry;

namespace ConverterGSATests
{
  public class SchemaTest : SpeckleConversionFixture
  {
    public SchemaTest() : base() { }

    //Reminder: conversions could create 1:1, 1:n, n:1, n:n structural per native objects

    [Fact]
    public void GsaNode()
    {
      //Set up context 
      gsaModelMock.Layer = GSALayer.Design;
      gsaModelMock.NativesByKeywordId = new Dictionary<GwaKeyword, Dictionary<int, GsaRecord_>>
      {
        { GwaKeyword.PROP_MASS, new Dictionary<int, GsaRecord_>
          { { 1, new GsaPropMass() { Index = 1, Mass = 10 } } }
        }
      };
      gsaModelMock.IndicesByKeyword = new Dictionary<GwaKeyword, List<int>>
      {
        { GwaKeyword.PROP_SPR, new List<int> { 1 } }
      };

      var gsaNode = new GsaNode() { ApplicationId = "blah", MassPropertyIndex = 1, AxisIndex = 1 };

      var structuralObjects = converter.ConvertToSpeckle(new List<object> { gsaNode });

      Assert.Empty(converter.ConversionErrors);
      Assert.NotEmpty(structuralObjects);
      Assert.Contains(structuralObjects, so => so is Node);

      var node = (Node)structuralObjects.FirstOrDefault(so => so is Node);

      Assert.Equal("blah", node.applicationId);
    }

    [Fact]
    public void GsaNodeEmbeddedResults()
    {
    }

    [Fact]
    public void GsaNodeSeparateResults()
    {
    }

    [Fact]
    public void GsaNodeResultsOnly()
    {
    }
  }
}
