using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xunit;
using Speckle.GSA.API.GwaSchema;
using Speckle.ConnectorGSA.Proxy.GwaParsers;
using Speckle.ConnectorGSA.Proxy.Merger;
using Speckle.ConnectorGSA.Proxy;

namespace ConverterGSATests
{
  public partial class SchemaTest : SpeckleConversionFixture
  {
    public SchemaTest() : base() { }

    #region merging
    [Fact]
    public void MergeNativeObjects()
    {
      Assert.True(GetAllSchemaTypes(out var schemaTypes));
      var merger = new GsaRecordMerger();
      merger.Initialise(schemaTypes);

      var axes = new List<GsaAxis>
      {
        new GsaAxis() { XDirX = 10, XDirY = 10 },  //null XDirZ
        new GsaAxis() { XDirY = 15, XDirZ = 20 }
      };
      var merged = merger.Merge(axes.First(), axes.Last());

      var sections = new List<GsaSection>
      {
        GsaCatalogueSectionExample("one"),  //new object
        GsaCatalogueSectionExample("one")   //old object
      };

      ((SectionComp)sections[0].Components[0]).OffsetY = null;
      ((SectionComp)sections[0].Components[0]).OffsetZ = null;
      sections[1].Components.RemoveAt(1);

      merged = merger.Merge(sections.First(), sections.Last());

    }

    private bool GetAllSchemaTypes(out List<Type> types)
    {
      try
      {
        var gsaBaseType = typeof(GsaRecord);
        var assembly = gsaBaseType.Assembly; //This assembly
        var assemblyTypes = assembly.GetTypes().ToList();

        types = assemblyTypes.Where(t => Helper.InheritsOrImplements(t, gsaBaseType)
          && !t.IsAbstract
          ).ToList();
      }
      catch
      {
        types = null;
        return false;
      }
      return (types.Count > 0);
    }
    #endregion
  }
}
