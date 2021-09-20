using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xunit;
using Speckle.GSA.API;
using Speckle.GSA.API.GwaSchema;
using Objects.Geometry;
using Objects.Structural;
using Objects.Structural.Geometry;
using Restraint = Objects.Structural.Geometry.Restraint;
using Objects.Structural.Loading;
using Objects.Structural.Materials;
using Objects.Structural.Properties;
using Objects.Structural.Properties.Profiles;
using Objects.Structural.GSA.Geometry;
using Objects.Structural.GSA.Loading;
using Objects.Structural.GSA.Properties;
using Objects.Structural.GSA.Materials;
using Objects.Structural.GSA.Other;
using Speckle.ConnectorGSA.Proxy.GwaParsers;
using GwaMemberType = Speckle.GSA.API.GwaSchema.MemberType;
using MemberType = Objects.Structural.Geometry.MemberType;
using GwaAxisDirection6 = Speckle.GSA.API.GwaSchema.AxisDirection6;
using AxisDirection6 = Objects.Structural.GSA.Other.AxisDirection6;
using Xunit.Sdk;
using Speckle.Core.Kits;
using ConverterGSA;
using Speckle.ConnectorGSA.Proxy.Merger;
using Speckle.GSA.API.CsvSchema;
using Objects.Structural.Results;
using Speckle.Core.Models;

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
