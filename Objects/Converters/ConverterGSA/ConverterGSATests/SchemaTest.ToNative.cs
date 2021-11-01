using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using Objects.Structural.GSA.Geometry;
using Objects.Structural.GSA.Materials;
using Objects.Structural.GSA.Properties;
using Objects.Structural.Geometry;
using Objects.Structural;
using Objects.Geometry;
using ConverterGSA;
using Objects.Structural.GSA.Analysis;
using Objects.Structural.GSA.Bridge;
using Objects.Structural.Loading;
using Objects.Structural.Properties.Profiles;
using Speckle.GSA.API.GwaSchema;
using Restraint = Objects.Structural.Geometry.Restraint;
using MemberType = Objects.Structural.Geometry.MemberType;
using GwaAxisDirection6 = Speckle.GSA.API.GwaSchema.AxisDirection6;
using Speckle.Core.Models;
using PathType = Objects.Structural.GSA.Bridge.PathType;
using Speckle.GSA.API;
using KellermanSoftware.CompareNetObjects;
using Objects.Structural.Analysis;
using Objects.Structural.GSA.Loading;
using Objects.Structural.Properties;
using AutoMapper;
using Objects.Structural.Materials;

namespace ConverterGSATests
{
  public partial class SchemaTest : SpeckleConversionFixture
  {
    //Reminder: conversions could create 1:1, 1:n, n:1, n:n structural per native objects

    #region Geometry
    [Fact]
    public void AxisToNative()
    {
      //Create native objects
      var gsaRecords = new List<GsaRecord>();
      var gsaAxis = GsaAxisExample("axis 1");
      gsaRecords.Add(gsaAxis);
      Instance.GsaModel.Cache.Upsert(gsaRecords);

      //Convert
      Instance.GsaModel.StreamLayer = GSALayer.Both;
      Instance.GsaModel.StreamSendConfig = StreamContentConfig.ModelOnly;
      var speckleObjects = converter.ConvertToSpeckle(gsaRecords.Select(i => (object)i).ToList());
      var gsaConvertedRecords = converter.ConvertToNative(speckleObjects);

      //Checks
      var gsaConvertedAxis = gsaConvertedRecords.FindAll(r => r is GsaAxis).Select(r => (GsaAxis)r).First();
      
      var compareLogic = new CompareLogic();
      compareLogic.Config.MembersToIgnore.Add(GetPropertyName((GsaAxis x) => x.XDirX));
      compareLogic.Config.MembersToIgnore.Add(GetPropertyName((GsaAxis x) => x.XDirY));
      compareLogic.Config.MembersToIgnore.Add(GetPropertyName((GsaAxis x) => x.XDirZ));
      compareLogic.Config.MembersToIgnore.Add(GetPropertyName((GsaAxis x) => x.XYDirX));
      compareLogic.Config.MembersToIgnore.Add(GetPropertyName((GsaAxis x) => x.XYDirY));
      compareLogic.Config.MembersToIgnore.Add(GetPropertyName((GsaAxis x) => x.XYDirZ));
      var result = compareLogic.Compare(gsaAxis, gsaConvertedAxis);
      Assert.Empty(result.Differences);
      Assert.Equal(gsaAxis.XDirX.Value, gsaConvertedAxis.XDirX.Value, 6);
      Assert.Equal(gsaAxis.XDirY.Value, gsaConvertedAxis.XDirY.Value, 6);
      Assert.Equal(gsaAxis.XDirZ.Value, gsaConvertedAxis.XDirZ.Value, 6);
      Assert.Equal(gsaAxis.XYDirX.Value, gsaConvertedAxis.XYDirX.Value, 6);
      Assert.Equal(gsaAxis.XYDirY.Value, gsaConvertedAxis.XYDirY.Value, 6);
      Assert.Equal(gsaAxis.XYDirZ.Value, gsaConvertedAxis.XYDirZ.Value, 6);
    }

    [Fact]
    public void NodeToNative()
    {
      //Create native objects
      var gsaRecords = new List<GsaRecord>();
      gsaRecords.Add(GsaPropMassExample("property mass 1"));
      gsaRecords.Add(GsaPropSprExample("property spring 1"));
      var gsaNodes = GsaNodeExamples(1, "node 1");
      gsaRecords.AddRange(gsaNodes);
      Instance.GsaModel.Cache.Upsert(gsaRecords);

      //Convert
      Instance.GsaModel.StreamLayer = GSALayer.Both;
      Instance.GsaModel.StreamSendConfig = StreamContentConfig.ModelOnly;
      var speckleModels = converter.ConvertToSpeckle(gsaRecords.Select(i => (object)i).ToList());
      var speckleObjects = ((Model)speckleModels.First()).nodes.FindAll(o => o is GSANode).ToList();
      var gsaConvertedRecords = converter.ConvertToNative(speckleObjects);

      //Checks
      var gsaConvertedNodes = gsaConvertedRecords.FindAll(r => r is GsaNode).Select(r => (GsaNode)r).ToList();
      var compareLogic = new CompareLogic();
      var result = compareLogic.Compare(gsaNodes, gsaConvertedNodes);
      Assert.Empty(result.Differences);
    }

    [Fact]
    public void Element2dToNative()
    {
      //Create native objects
      var gsaRecords = new List<GsaRecord>();
      gsaRecords.Add(GsaMatSteelExample("steel material 1"));
      gsaRecords.Add(GsaPropMassExample("property mass 1"));
      gsaRecords.Add(GsaPropSprExample("property spring 1"));
      gsaRecords.AddRange(GsaNodeExamples(5, "node 1", "node 2", "node 3", "node 4", "node 5"));
      gsaRecords.Add(GsaProp2dExample("property 2D 1"));
      var gsaEls = GsaElement2dExamples(2, "element 1", "element 2");
      gsaRecords.AddRange(gsaEls);
      Instance.GsaModel.Cache.Upsert(gsaRecords);

      //Convert
      Instance.GsaModel.StreamLayer = GSALayer.Both;
      Instance.GsaModel.StreamSendConfig = StreamContentConfig.ModelOnly;
      var speckleModels = converter.ConvertToSpeckle(gsaRecords.Select(i => (object)i).ToList());
      var speckleObjects = ((Model)speckleModels.Last()).elements.FindAll(o => o is GSAElement2D).ToList();
      var gsaConvertedRecords = converter.ConvertToNative(speckleObjects);

      //Checks
      var gsaConvertedEls = gsaConvertedRecords.FindAll(r => r is GsaEl).Select(r => (GsaEl)r).ToList();
      var compareLogic = new CompareLogic();
      compareLogic.Config.MembersToIgnore.Add(GetPropertyName((GsaEl x) => x.Angle));
      compareLogic.Config.MembersToIgnore.Add(GetPropertyName((GsaEl x) => x.OffsetZ));
      compareLogic.Config.MembersToIgnore.Add(GetPropertyName((GsaEl x) => x.ParentIndex));
      var result = compareLogic.Compare(gsaEls, gsaConvertedEls);
      Assert.Empty(result.Differences);
      Assert.Null(gsaConvertedEls[0].Angle);
      Assert.Null(gsaConvertedEls[0].OffsetZ);
      Assert.Null(gsaConvertedEls[0].ParentIndex);
      Assert.Null(gsaConvertedEls[1].Angle);
      Assert.Null(gsaConvertedEls[1].OffsetZ);
      Assert.Null(gsaConvertedEls[1].ParentIndex);
    }

    [Fact]
    public void Element1dBaseLineToNative()
    {
      var speckleElement = new GSAElement1D()
      {
        type = ElementType1D.Beam,
        baseLine = new Line(new List<double>() { 20, 21, 22, 50, 51, 52 }),
      };
      var gsaConvertedRecords = converter.ConvertToNative(new List<Base>() { speckleElement });
    }

    [Fact]
    public void Element1dToNative()
    {
      //Create native objects
      var gsaRecords = new List<GsaRecord>();
      gsaRecords.Add(GsaMatSteelExample("steel material 1"));
      gsaRecords.Add(GsaPropMassExample("property mass 1"));
      gsaRecords.Add(GsaPropSprExample("property spring 1"));
      gsaRecords.AddRange(GsaNodeExamples(3, "node 1", "node 2", "node 3"));
      gsaRecords.Add(GsaCatalogueSectionExample("section 1"));
      var gsaEls = GsaElement1dExamples(2, "element 1", "element 2");
      gsaRecords.AddRange(gsaEls);
      Instance.GsaModel.Cache.Upsert(gsaRecords);

      //Convert
      Instance.GsaModel.StreamLayer = GSALayer.Both;
      Instance.GsaModel.StreamSendConfig = StreamContentConfig.ModelOnly;
      var speckleModels = converter.ConvertToSpeckle(gsaRecords.Select(i => (object)i).ToList());
      var speckleObjects = ((Model)speckleModels.Last()).elements.FindAll(o => o is GSAElement1D).ToList();
      var gsaConvertedRecords = converter.ConvertToNative(speckleObjects);

      //Checks
      var gsaConvertedEls = gsaConvertedRecords.FindAll(r => r is GsaEl).Select(r => (GsaEl)r).ToList();
      var compareLogic = new CompareLogic();
      compareLogic.Config.MembersToIgnore.Add(GetPropertyName((GsaEl x) => x.OffsetY));
      compareLogic.Config.MembersToIgnore.Add(GetPropertyName((GsaEl x) => x.OffsetZ));
      compareLogic.Config.MembersToIgnore.Add(GetPropertyName((GsaEl x) => x.ParentIndex));
      var result = compareLogic.Compare(gsaEls, gsaConvertedEls);
      Assert.Empty(result.Differences);
      Assert.Null(gsaConvertedEls[0].OffsetY);
      Assert.Null(gsaConvertedEls[0].OffsetZ);
      Assert.Null(gsaConvertedEls[0].ParentIndex);
      Assert.Null(gsaConvertedEls[1].OffsetY);
      Assert.Null(gsaConvertedEls[1].OffsetZ);
      Assert.Null(gsaConvertedEls[1].ParentIndex);
    }

    [Fact]
    public void GSAMember1dBaseLineToNative()
    {
      var speckleMember1d = new GSAMember1D()
      {
        type = ElementType1D.Beam,
        baseLine = new Line(new List<double>() { 20, 21, 22, 50, 51, 52 }),
      };
      var gsaConvertedRecords = converter.ConvertToNative(new List<Base>() { speckleMember1d });
    }

    [Fact]
    public void GSAMemberToNative()
    {
      //Create native objects
      var gsaRecords = new List<GsaRecord>();
      gsaRecords.Add(GsaMatSteelExample("steel material 1"));
      gsaRecords.Add(GsaPropMassExample("property mass 1"));
      gsaRecords.Add(GsaPropSprExample("property spring 1"));
      gsaRecords.AddRange(GsaNodeExamples(5, "node 1", "node 2", "node 3", "node 4", "node 5"));
      gsaRecords.Add(GsaProp2dExample("prop 2D 1"));
      gsaRecords.Add(GsaCatalogueSectionExample("section 1"));
      var gsaMembers = GsaMemberExamples(2, "member 1", "member 2");
      gsaRecords.AddRange(gsaMembers);
      Instance.GsaModel.Cache.Upsert(gsaRecords);

      //Convert
      Instance.GsaModel.StreamLayer = GSALayer.Both;
      Instance.GsaModel.StreamSendConfig = StreamContentConfig.ModelOnly;
      var speckleModels = converter.ConvertToSpeckle(gsaRecords.Select(i => (object)i).ToList());
      var speckleObjects = new List<Base>();
      speckleObjects.Add(((Model)speckleModels.First()).elements.FindAll(o => o is GSAMember1D).First());
      speckleObjects.Add(((Model)speckleModels.First()).elements.FindAll(o => o is GSAMember2D).First());
      var gsaConvertedRecords = converter.ConvertToNative(speckleObjects);

      //Checks
      var gsaConvertedMembers = gsaConvertedRecords.FindAll(r => r is GsaMemb).Select(r => (GsaMemb)r).ToList();
      var compareLogic = new CompareLogic();
      compareLogic.Config.MembersToIgnore.Add(GetPropertyName((GsaMemb x) => x.Angle)); //example has 0 which is converted to null
      compareLogic.Config.MembersToIgnore.Add(GetPropertyName((GsaMemb x) => x.End1OffsetX));
      compareLogic.Config.MembersToIgnore.Add(GetPropertyName((GsaMemb x) => x.End2OffsetX));
      compareLogic.Config.MembersToIgnore.Add(GetPropertyName((GsaMemb x) => x.OffsetY));
      compareLogic.Config.MembersToIgnore.Add(GetPropertyName((GsaMemb x) => x.OffsetZ));
      compareLogic.Config.MembersToIgnore.Add(GetPropertyName((GsaMemb x) => x.Offset2dZ));
      var result = compareLogic.Compare(gsaMembers, gsaConvertedMembers);
      Assert.Empty(result.Differences);
      Assert.Null(gsaConvertedMembers[0].Angle);
      Assert.Null(gsaConvertedMembers[0].End1OffsetX);
      Assert.Null(gsaConvertedMembers[0].End2OffsetX);
      Assert.Null(gsaConvertedMembers[0].OffsetY);
      Assert.Null(gsaConvertedMembers[0].OffsetZ);
      Assert.Null(gsaConvertedMembers[1].Angle);
      Assert.Null(gsaConvertedMembers[1].Offset2dZ);
    }

    [Fact]
    public void AssemblyToNative()
    {
      //Create native objects
      var gsaRecords = new List<GsaRecord>();
      gsaRecords.Add(GsaMatSteelExample("steel material 1"));
      gsaRecords.Add(GsaPropMassExample("property mass 1"));
      gsaRecords.Add(GsaPropSprExample("property spring 1"));
      gsaRecords.AddRange(GsaNodeExamples(5, "node 1", "node 2", "node 3", "node 4", "node 5"));
      gsaRecords.Add(GsaProp2dExample("section 1"));
      gsaRecords.AddRange(GsaElement2dExamples(2, "element 1", "element 2"));
      var gsaAssemblies = GsaAssemblyExamples(2, "assembly 1", "assembly 2");
      gsaRecords.AddRange(gsaAssemblies);
      Instance.GsaModel.Cache.Upsert(gsaRecords);

      //Convert
      Instance.GsaModel.StreamLayer = GSALayer.Both;
      Instance.GsaModel.StreamSendConfig = StreamContentConfig.ModelOnly;
      var speckleModels = converter.ConvertToSpeckle(gsaRecords.Select(i => (object)i).ToList());
      var speckleObjects = ((Model)speckleModels.Last()).elements.FindAll(o => o is GSAAssembly).ToList();
      var gsaConvertedRecords = converter.ConvertToNative(speckleObjects);

      //Checks
      var gsaConvertedAssemblies = gsaConvertedRecords.FindAll(r => r is GsaAssembly).Select(r => (GsaAssembly)r).ToList();
      var compareLogic = new CompareLogic();
      var result = compareLogic.Compare(gsaAssemblies, gsaConvertedAssemblies);
      Assert.Empty(result.Differences);
    }

    [Fact]
    public void GridLineToNative()
    {
      //Define GSA objects
      //These should be in order that respects the type dependency tree (which is only available in the GSAProxy library, which isn't referenced yet
      var gsaRecords = new List<GsaRecord>();

      //Generation #1: Types with no other dependencies - the leaves of the tree
      var gsaGridLines = GsaGridLineExamples(2, "grid line 1", "grid line 2");
      gsaRecords.AddRange(gsaGridLines);

      Instance.GsaModel.Cache.Upsert(gsaRecords);

      foreach (var record in gsaRecords)
      {
        var speckleObjects = converter.ConvertToSpeckle(new List<object> { record });
        Assert.Empty(converter.ConversionErrors);

        Instance.GsaModel.Cache.SetSpeckleObjects(record, speckleObjects.ToDictionary(so => so.applicationId, so => (object)so));
      }

      Assert.True(Instance.GsaModel.Cache.GetSpeckleObjects(out var structuralObjects));

      Assert.NotEmpty(structuralObjects);
      Assert.Contains(structuralObjects, so => so is GSAGridLine);

      //Convert
      Instance.GsaModel.StreamLayer = GSALayer.Both;
      Instance.GsaModel.StreamSendConfig = StreamContentConfig.ModelOnly;
      var speckleModels = converter.ConvertToSpeckle(gsaRecords.Select(i => (object)i).ToList());
      var speckleGridLines = ((Model)speckleModels.Last()).elements.FindAll(o => o is GSAGridLine).ToList();
      var gsaConvertedRecords = converter.ConvertToNative(speckleGridLines);

      //Checks
      var gsaConvertedGridLines = gsaConvertedRecords.FindAll(r => r is GsaGridLine).Select(r => (GsaGridLine)r).ToList();
      var compareLogic = new CompareLogic();
      compareLogic.Config.MembersToIgnore.Add(GetPropertyName((GsaGridLine x) => x.Theta1));
      compareLogic.Config.MembersToIgnore.Add(GetPropertyName((GsaGridLine x) => x.Theta2));
      var result = compareLogic.Compare(gsaGridLines, gsaConvertedGridLines);
      Assert.Empty(result.Differences);

      for (int i = 0; i < gsaConvertedGridLines.Count(); i++)
      {
        if (gsaGridLines[i].Theta1.HasValue)
        {
          Assert.Equal(Math.Round(gsaGridLines[i].Theta1.Value, 4), Math.Round(gsaConvertedGridLines[i].Theta1.Value, 4));
        }
        if (gsaGridLines[i].Theta2.HasValue)
        {
          Assert.Equal(Math.Round(gsaGridLines[i].Theta2.Value, 4), Math.Round(gsaConvertedGridLines[i].Theta2.Value, 4));
        }
      }
    }

    [Fact]
    public void GridPlaneToNative()
    {
      //Define GSA objects
      //These should be in order that respects the type dependency tree (which is only available in the GSAProxy library, which isn't referenced yet
      var gsaRecords = new List<GsaRecord>();

      //Generation #1: Types with no other dependencies - the leaves of the tree
      gsaRecords.Add(GsaAxisExample("axis 1"));

      //Gen #2
      var gsaGridPlanes = GsaGridPlaneExamples(2, "grid plane 1", "grid plane 2");
      gsaRecords.AddRange(gsaGridPlanes);

      Instance.GsaModel.Cache.Upsert(gsaRecords);

      foreach (var record in gsaRecords)
      {
        var speckleObjects = converter.ConvertToSpeckle(new List<object> { record });
        Assert.Empty(converter.ConversionErrors);

        Instance.GsaModel.Cache.SetSpeckleObjects(record, speckleObjects.ToDictionary(so => so.applicationId, so => (object)so));
      }

      Assert.True(Instance.GsaModel.Cache.GetSpeckleObjects(out var structuralObjects));

      Assert.NotEmpty(structuralObjects);
      Assert.Contains(structuralObjects, so => so is GSAGridPlane);

      var speckleGridPlanes = structuralObjects.FindAll(so => so is GSAGridPlane).Select(so => (GSAGridPlane)so).ToList();
      var gsaConvertedRecords = converter.ConvertToNative(speckleGridPlanes.Select(p => (Base)p).ToList());

      //Checks
      var gsaConvertedGridPlanes = gsaConvertedRecords.FindAll(r => r is GsaGridPlane).Select(r => (GsaGridPlane)r).ToList();
      var compareLogic = new CompareLogic();
      var result = compareLogic.Compare(gsaGridPlanes, gsaConvertedGridPlanes);
      Assert.Empty(result.Differences);
    }

    [Fact]
    public void GridSurfaceToNative()
    {
      //Define GSA objects
      //These should be in order that respects the type dependency tree (which is only available in the GSAProxy library, which isn't referenced yet
      var gsaRecords = new List<GsaRecord>();

      //Generation #1: Types with no other dependencies - the leaves of the tree
      gsaRecords.Add(GsaAxisExample("axis 1"));
      gsaRecords.Add(GsaMatSteelExample("steel material 1"));
      gsaRecords.Add(GsaPropMassExample("property mass 1"));
      gsaRecords.Add(GsaPropSprExample("property spring 1"));

      //Gen #2
      gsaRecords.AddRange(GsaNodeExamples(4, "node 1", "node 2", "node 3", "node 4"));
      gsaRecords.Add(GsaProp2dExample("prop 2D 1"));
      gsaRecords.Add(GsaCatalogueSectionExample("section 1"));
      gsaRecords.Add(GsaGridPlaneExamples(1, "grid plane 1").FirstOrDefault());

      //Gen #3
      gsaRecords.Add(GsaElement1dExamples(1, "beam 1").FirstOrDefault());
      var gsaElement2d = GsaElement2dExamples(1, "quad 1").FirstOrDefault();
      gsaElement2d.Index = 2;
      gsaRecords.Add(gsaElement2d);

      //Gen #4
      var gsaGridSurfaces = GsaGridSurfaceExamples(2, "grid surface 1", "grid surface 2");
      gsaRecords.AddRange(gsaGridSurfaces);

      Instance.GsaModel.Cache.Upsert(gsaRecords);

      //Convert
      Instance.GsaModel.StreamLayer = GSALayer.Both;
      Instance.GsaModel.StreamSendConfig = StreamContentConfig.ModelAndResults;
      var speckleObjects = converter.ConvertToSpeckle(gsaRecords.Select(i => (object)i).ToList());

      //Get speckle results
      var speckleModelObjects = speckleObjects.FindAll(so => so is Model).Select(so => (Model)so).ToList();
      var speckleDesignModel = speckleModelObjects.Where(so => so.layerDescription == "Design").FirstOrDefault();
      var speckleAnalysisModel = speckleModelObjects.Where(so => so.layerDescription == "Analysis").FirstOrDefault();

      #region Design Layer
      var speckleGridSurfaces = speckleDesignModel.elements.FindAll(so => so is GSAGridSurface).Select(so => (GSAGridSurface)so).ToList();
      var gsaDesignRecords = converter.ConvertToNative(speckleGridSurfaces.Select(p => (Base)p).ToList());

      //Checks
      var gsaDesignConverted = gsaDesignRecords.FindAll(r => r is GsaGridSurface).Select(r => (GsaGridSurface)r).ToList();
      var compareLogic = new CompareLogic();
      //Ignore Type because this context didn't create any Design Layer members, so the Model object for the design layer doesn't have
      //anything in its elements collection, so the ToNative of the Grid surface can't work out which type it is
      compareLogic.Config.MembersToIgnore.Add(GetPropertyName((GsaGridSurface x) => x.Type));
      compareLogic.Config.MembersToIgnore.Add(GetPropertyName((GsaGridSurface x) => x.ElementIndices));
      var result = compareLogic.Compare(gsaGridSurfaces, gsaDesignConverted);
      Assert.Empty(result.Differences);

      #endregion

      #region Analysis Layer
      speckleGridSurfaces = speckleAnalysisModel.elements.FindAll(so => so is GSAGridSurface).Select(so => (GSAGridSurface)so).ToList();
      var gsaAnalysisRecords = converter.ConvertToNative(speckleGridSurfaces.Select(p => (Base)p).ToList());

      //Checks
      var gsaAnalysisConverted = gsaAnalysisRecords.FindAll(r => r is GsaGridSurface).Select(r => (GsaGridSurface)r).ToList();
      compareLogic = new CompareLogic();  //Get new clear CompareLogic object with a fresh config without any ignores
      result = compareLogic.Compare(gsaGridSurfaces, gsaAnalysisConverted);
      Assert.Empty(result.Differences);

      #endregion
    }

    [Fact]
    public void PolylineToNative()
    {
      //Create native objects
      var gsaRecords = new List<GsaRecord>();
      gsaRecords.Add(GsaGridPlaneExamples(1, "grid plane 1").FirstOrDefault());
      var gsaPolylines = GsaPolylineExamples(2, "polyline 1", "polyline 2");
      gsaRecords.AddRange(gsaPolylines);
      Instance.GsaModel.Cache.Upsert(gsaRecords);

      //Convert
      Instance.GsaModel.StreamLayer = GSALayer.Both;
      Instance.GsaModel.StreamSendConfig = StreamContentConfig.ModelOnly;
      var speckleModels = converter.ConvertToSpeckle(gsaRecords.Select(i => (object)i).ToList());
      var speckleObjects = ((Model)speckleModels.Last()).elements.FindAll(o => o is GSAPolyline).ToList();
      var gsaConvertedRecords = converter.ConvertToNative(speckleObjects);

      //Checks
      var gsaConvertedPolylines = gsaConvertedRecords.FindAll(r => r is GsaPolyline).Select(r => (GsaPolyline)r).ToList();
      var compareLogic = new CompareLogic();
      var result = compareLogic.Compare(gsaPolylines, gsaConvertedPolylines);
      Assert.Empty(result.Differences);
    }
    #endregion

    #region Loading
    [Fact]
    public void GSALoadCaseToNative()
    {
      //Create native objects
      var gsaRecords = new List<GsaRecord>();
      var gsaLoadCases = GsaLoadCaseExamples(2, "load case 1", "load case 2");
      gsaRecords.AddRange(gsaLoadCases);
      Instance.GsaModel.Cache.Upsert(gsaRecords);

      //Convert
      Instance.GsaModel.StreamLayer = GSALayer.Both;
      Instance.GsaModel.StreamSendConfig = StreamContentConfig.ModelOnly;
      var speckleModels = converter.ConvertToSpeckle(gsaRecords.Select(i => (object)i).ToList());
      var speckleObjects = ((Model)speckleModels.First()).loads.FindAll(o => o is GSALoadCase).ToList();
      var gsaConvertedRecords = converter.ConvertToNative(speckleObjects);

      //Checks
      var gsaConvertedLoadCases = gsaConvertedRecords.FindAll(r => r is GsaLoadCase).Select(r => (GsaLoadCase)r).ToList();
      var compareLogic = new CompareLogic();
      var result = compareLogic.Compare(gsaLoadCases, gsaConvertedLoadCases);
      Assert.Empty(result.Differences);
    }

    [Fact]
    public void LoadCaseToNative()
    {
      //Create native objects
      var gsaRecords = new List<GsaRecord>();
      var gsaLoadCases = GsaLoadCaseExamples(2, "load case 1", "load case 2");
      gsaRecords.AddRange(gsaLoadCases);
      Instance.GsaModel.Cache.Upsert(gsaRecords);

      //Convert
      Instance.GsaModel.StreamLayer = GSALayer.Both;
      Instance.GsaModel.StreamSendConfig = StreamContentConfig.ModelOnly;
      var speckleModels = converter.ConvertToSpeckle(gsaRecords.Select(i => (object)i).ToList());
      var speckleAppSpecificObjects = ((Model)speckleModels.First()).loads.FindAll(o => o is GSALoadCase).Select(o => (GSALoadCase)o).ToList();
      Map<GSALoadCase, LoadCase>(speckleAppSpecificObjects, out var speckleAppAgnosticObjects);
      var gsaConvertedRecords = converter.ConvertToNative(speckleAppAgnosticObjects.Select(o=>(Base)o).ToList());

      //Checks
      var gsaConvertedLoadCases = gsaConvertedRecords.FindAll(r => r is GsaLoadCase).Select(r => (GsaLoadCase)r).ToList();
      var compareLogic = new CompareLogic();
      compareLogic.Config.MembersToIgnore.Add(GetPropertyName((GsaLoadCase x) => x.Direction)); //Ignore app specific data
      compareLogic.Config.MembersToIgnore.Add(GetPropertyName((GsaLoadCase x) => x.Include));
      compareLogic.Config.MembersToIgnore.Add(GetPropertyName((GsaLoadCase x) => x.Bridge));
      var result = compareLogic.Compare(gsaLoadCases, gsaConvertedLoadCases);
      Assert.Empty(result.Differences);
    }

    [Fact]
    public void GSAAnalysisCaseToNative()
    {
      //Create native objects
      var gsaRecords = new List<GsaRecord>();
      gsaRecords.AddRange(GsaLoadCaseExamples(2, "load case 1", "load case 2"));
      var gsaAnalysisCases = GsaAnalysisCaseExamples(2, "analysis case 1", "analysis case 2");
      gsaRecords.AddRange(gsaAnalysisCases);
      Instance.GsaModel.Cache.Upsert(gsaRecords);

      //Convert
      Instance.GsaModel.StreamLayer = GSALayer.Both;
      Instance.GsaModel.StreamSendConfig = StreamContentConfig.ModelOnly;
      var speckleModels = converter.ConvertToSpeckle(gsaRecords.Select(i => (object)i).ToList());
      var speckleObjects = ((Model)speckleModels.First()).loads.FindAll(o => o is GSAAnalysisCase).ToList();
      var gsaConvertedRecords = converter.ConvertToNative(speckleObjects);

      //Checks
      var gsaConvertedAnalysisCases = gsaConvertedRecords.FindAll(r => r is GsaAnal).Select(r => (GsaAnal)r).ToList();
      var compareLogic = new CompareLogic();
      compareLogic.Config.MembersToIgnore.Add(GetPropertyName((GsaAnal x) => x.TaskIndex)); //TODO - add in when this keyword is supported
      compareLogic.Config.MembersToIgnore.Add(GetPropertyName((GsaAnal x) => x.Desc));
      var result = compareLogic.Compare(gsaAnalysisCases, gsaConvertedAnalysisCases);
      Assert.Empty(result.Differences);
      Assert.Equal(gsaAnalysisCases[0].Desc.RemoveWhitespace(), gsaConvertedAnalysisCases[0].Desc);
      Assert.Equal(gsaAnalysisCases[1].Desc.RemoveWhitespace(), gsaConvertedAnalysisCases[1].Desc);
    }

    [Fact]
    public void GSALoadCombinationToNative()
    {
      //Create native objects
      var gsaRecords = new List<GsaRecord>();
      gsaRecords.AddRange(GsaLoadCaseExamples(2, "load case 1", "load case 2"));
      gsaRecords.AddRange(GsaAnalysisCaseExamples(2, "analysis case 1", "analysis case 2"));
      var gsaCombinations = GsaCombinationExamples(2, "combo 1", "combo 2");
      gsaRecords.AddRange(gsaCombinations);
      Instance.GsaModel.Cache.Upsert(gsaRecords);

      //Convert
      Instance.GsaModel.StreamLayer = GSALayer.Both;
      Instance.GsaModel.StreamSendConfig = StreamContentConfig.ModelOnly;
      var speckleModels = converter.ConvertToSpeckle(gsaRecords.Select(i => (object)i).ToList());
      var speckleObjects = ((Model)speckleModels.First()).loads.FindAll(o => o is GSALoadCombination).ToList();
      var gsaConvertedRecords = converter.ConvertToNative(speckleObjects);

      //Checks
      var gsaConvertedCombinations = gsaConvertedRecords.FindAll(r => r is GsaCombination).Select(r => (GsaCombination)r).ToList();
      var compareLogic = new CompareLogic();
      compareLogic.Config.MembersToIgnore.Add(GetPropertyName((GsaCombination x) => x.Desc));
      compareLogic.Config.MembersToIgnore.Add(GetPropertyName((GsaCombination x) => x.Bridge));
      var result = compareLogic.Compare(gsaCombinations, gsaConvertedCombinations);
      Assert.Empty(result.Differences);
      Assert.Equal(gsaConvertedCombinations[0].Desc.RemoveWhitespace(), gsaConvertedCombinations[0].Desc);
      Assert.False(gsaConvertedCombinations[0].Bridge);
      Assert.Equal(gsaConvertedCombinations[1].Desc.RemoveWhitespace(), gsaConvertedCombinations[1].Desc);
      Assert.False(gsaConvertedCombinations[1].Bridge);
    }

    [Fact]
    public void LoadCombinationToNative()
    {
      //Create native objects
      var gsaRecords = new List<GsaRecord>();
      gsaRecords.AddRange(GsaLoadCaseExamples(2, "load case 1", "load case 2"));
      gsaRecords.AddRange(GsaAnalysisCaseExamples(2, "analysis case 1", "analysis case 2"));
      var gsaCombinations = GsaCombinationExamples(2, "combo 1", "combo 2");
      gsaRecords.AddRange(gsaCombinations);
      Instance.GsaModel.Cache.Upsert(gsaRecords);

      //Convert
      Instance.GsaModel.StreamLayer = GSALayer.Both;
      Instance.GsaModel.StreamSendConfig = StreamContentConfig.ModelOnly;
      var speckleModels = converter.ConvertToSpeckle(gsaRecords.Select(i => (object)i).ToList());
      var speckleAppSpecificObjects = ((Model)speckleModels.First()).loads.FindAll(o => o is GSALoadCombination).Select(o => (GSALoadCombination)o).ToList();
      Map<GSALoadCombination, LoadCombination>(speckleAppSpecificObjects, out var speckleAppAgnosticObjects);
      var gsaConvertedRecords = converter.ConvertToNative(speckleAppAgnosticObjects.Select(o => (Base)o).ToList());

      //Checks
      var gsaConvertedCombinations = gsaConvertedRecords.FindAll(r => r is GsaCombination).Select(r => (GsaCombination)r).ToList();
      var compareLogic = new CompareLogic();
      compareLogic.Config.MembersToIgnore.Add(GetPropertyName((GsaCombination x) => x.Desc));
      compareLogic.Config.MembersToIgnore.Add(GetPropertyName((GsaCombination x) => x.Note)); //Ignore app specific data
      compareLogic.Config.MembersToIgnore.Add(GetPropertyName((GsaCombination x) => x.Bridge));
      var result = compareLogic.Compare(gsaCombinations, gsaConvertedCombinations);
      Assert.Empty(result.Differences);
      Assert.Equal(gsaConvertedCombinations[0].Desc.RemoveWhitespace(), gsaConvertedCombinations[0].Desc);
      Assert.Equal(gsaConvertedCombinations[1].Desc.RemoveWhitespace(), gsaConvertedCombinations[1].Desc);
    }

    [Fact]
    public void GSALoadFaceToNative()
    {
      //Create native objects
      var gsaRecords = new List<GsaRecord>();
      gsaRecords.Add(GsaMatSteelExample("steel material 1"));
      gsaRecords.Add(GsaPropMassExample("property mass 1"));
      gsaRecords.Add(GsaPropSprExample("property spring 1"));
      gsaRecords.Add(GsaAxisExample("axis 1"));
      gsaRecords.Add(GsaLoadCaseExamples(1, "load case 1").First());
      gsaRecords.AddRange(GsaNodeExamples(6, "node 1", "node 2", "node 3", "node 4", "node 5", "node 6"));
      gsaRecords.Add(GsaProp2dExample("property 2D 1"));
      gsaRecords.AddRange(GsaElement2dExamples(3, "element 1", "element 2", "element 3"));
      var gsaLoad2dFace = GsaLoad2dFaceExamples(3, "load 2d face 1", "load 2d face 2", "load 2d face 3");
      gsaRecords.AddRange(gsaLoad2dFace);
      Instance.GsaModel.Cache.Upsert(gsaRecords);

      //Convert
      Instance.GsaModel.StreamLayer = GSALayer.Both;
      Instance.GsaModel.StreamSendConfig = StreamContentConfig.ModelOnly;
      var speckleModels = converter.ConvertToSpeckle(gsaRecords.Select(i => (object)i).ToList());
      var speckleObjects = ((Model)speckleModels.Last()).loads.FindAll(o => o is GSALoadFace).ToList();
      var gsaConvertedRecords = converter.ConvertToNative(speckleObjects);

      //Checks
      var gsaConvertedLoad2dFace = gsaConvertedRecords.FindAll(r => r is GsaLoad2dFace).Select(r => (GsaLoad2dFace)r).ToList();
      var compareLogic = new CompareLogic();
      var result = compareLogic.Compare(gsaLoad2dFace, gsaConvertedLoad2dFace);
      Assert.Empty(result.Differences);
    }

    [Fact]
    public void LoadFaceToNative()
    {
      //Create native objects
      var gsaRecords = new List<GsaRecord>();
      gsaRecords.Add(GsaMatSteelExample("steel material 1"));
      gsaRecords.Add(GsaPropMassExample("property mass 1"));
      gsaRecords.Add(GsaPropSprExample("property spring 1"));
      gsaRecords.Add(GsaAxisExample("axis 1"));
      gsaRecords.Add(GsaLoadCaseExamples(1, "load case 1").First());
      gsaRecords.AddRange(GsaNodeExamples(6, "node 1", "node 2", "node 3", "node 4", "node 5", "node 6"));
      gsaRecords.Add(GsaProp2dExample("property 2D 1"));
      gsaRecords.AddRange(GsaElement2dExamples(3, "element 1", "element 2", "element 3"));
      var gsaLoad2dFace = GsaLoad2dFaceExamples(3, "load 2d face 1", "load 2d face 2", "load 2d face 3");
      gsaRecords.AddRange(gsaLoad2dFace);
      Instance.GsaModel.Cache.Upsert(gsaRecords);

      //Convert
      Instance.GsaModel.StreamLayer = GSALayer.Both;
      Instance.GsaModel.StreamSendConfig = StreamContentConfig.ModelOnly;
      var speckleModels = converter.ConvertToSpeckle(gsaRecords.Select(i => (object)i).ToList());
      var speckleAppSpecificObjects = ((Model)speckleModels.Last()).loads.FindAll(o => o is GSALoadFace).Select(o => (GSALoadFace)o).ToList();
      Map<GSALoadFace, LoadFace>(speckleAppSpecificObjects, out var speckleAppAgnosticObjects);
      var gsaConvertedRecords = converter.ConvertToNative(speckleAppAgnosticObjects.Select(o => (Base)o).ToList());

      //Checks
      var gsaConvertedLoad2dFace = gsaConvertedRecords.FindAll(r => r is GsaLoad2dFace).Select(r => (GsaLoad2dFace)r).ToList();
      var compareLogic = new CompareLogic();
      //Ignore app specific data, e.g. compareLogic.Config.MembersToIgnore.Add(GetPropertyName((GsaLoad2dFace x) => x.Name)); 
      var result = compareLogic.Compare(gsaLoad2dFace, gsaConvertedLoad2dFace);
      Assert.Empty(result.Differences);
    }

    [Fact]
    public void GSALoadBeamToNative()
    {
      //Create native objects
      var gsaRecords = new List<GsaRecord>();
      gsaRecords.Add(GsaMatSteelExample("steel material 1"));
      gsaRecords.Add(GsaPropMassExample("property mass 1"));
      gsaRecords.Add(GsaPropSprExample("property spring 1"));
      gsaRecords.Add(GsaLoadCaseExamples(1, "load case 1").First());
      gsaRecords.Add(GsaAxisExample("axis 1"));
      gsaRecords.AddRange(GsaNodeExamples(3, "node 1", "node 2", "node 3"));
      gsaRecords.Add(GsaCatalogueSectionExample("section 1"));
      gsaRecords.AddRange(GsaElement1dExamples(2, "element 1", "element 2"));
      var gsaLoadBeams = GsaLoadBeamExamples(3, "load beam 1", "load beam 2", "load beam 3");
      gsaRecords.AddRange(gsaLoadBeams);
      Instance.GsaModel.Cache.Upsert(gsaRecords);

      //Convert
      Instance.GsaModel.StreamLayer = GSALayer.Both;
      Instance.GsaModel.StreamSendConfig = StreamContentConfig.ModelOnly;
      var speckleModels = converter.ConvertToSpeckle(gsaRecords.Select(i => (object)i).ToList());
      var speckleObjects = ((Model)speckleModels.Last()).loads.FindAll(o => o is GSALoadBeam).ToList();
      var gsaConvertedRecords = converter.ConvertToNative(speckleObjects);

      //Checks
      var gsaConvertedLoadBeams = gsaConvertedRecords.FindAll(r => r is GsaLoadBeam).Select(r => (GsaLoadBeam)r).ToList();
      var compareLogic = new CompareLogic();
      var result = compareLogic.Compare(gsaLoadBeams, gsaConvertedLoadBeams);
      Assert.Empty(result.Differences);
    }

    [Fact]
    public void LoadBeamToNative()
    {
      //Create native objects
      var gsaRecords = new List<GsaRecord>();
      gsaRecords.Add(GsaMatSteelExample("steel material 1"));
      gsaRecords.Add(GsaPropMassExample("property mass 1"));
      gsaRecords.Add(GsaPropSprExample("property spring 1"));
      gsaRecords.Add(GsaLoadCaseExamples(1, "load case 1").First());
      gsaRecords.Add(GsaAxisExample("axis 1"));
      gsaRecords.AddRange(GsaNodeExamples(3, "node 1", "node 2", "node 3"));
      gsaRecords.Add(GsaCatalogueSectionExample("section 1"));
      gsaRecords.AddRange(GsaElement1dExamples(2, "element 1", "element 2"));
      var gsaLoadBeams = GsaLoadBeamExamples(3, "load beam 1", "load beam 2", "load beam 3");
      gsaRecords.AddRange(gsaLoadBeams);
      Instance.GsaModel.Cache.Upsert(gsaRecords);

      //Convert
      Instance.GsaModel.StreamLayer = GSALayer.Both;
      Instance.GsaModel.StreamSendConfig = StreamContentConfig.ModelOnly;
      var speckleModels = converter.ConvertToSpeckle(gsaRecords.Select(i => (object)i).ToList());
      var speckleAppSpecificObjects = ((Model)speckleModels.Last()).loads.FindAll(o => o is GSALoadBeam).Select(o => (GSALoadBeam)o).ToList();
      Map<GSALoadBeam, LoadBeam>(speckleAppSpecificObjects, out var speckleAppAgnosticObjects);
      var gsaConvertedRecords = converter.ConvertToNative(speckleAppAgnosticObjects.Select(o => (Base)o).ToList());

      //Checks
      var gsaConvertedLoadBeams = gsaConvertedRecords.FindAll(r => r is GsaLoadBeam).Select(r => (GsaLoadBeam)r).ToList();
      var compareLogic = new CompareLogic();
      //Ignore app specific data, e.g. compareLogic.Config.MembersToIgnore.Add(GetPropertyName((GsaLoadBeam x) => x.Name));
      var result = compareLogic.Compare(gsaLoadBeams, gsaConvertedLoadBeams);
      Assert.Empty(result.Differences);
    }

    [Fact]
    public void GSALoadNodeToNative()
    {
      //Create native objects
      var gsaRecords = new List<GsaRecord>();
      gsaRecords.Add(GsaAxisExample("axis 1"));
      gsaRecords.Add(GsaPropMassExample("property mass 1"));
      gsaRecords.Add(GsaPropSprExample("property spring 1"));
      gsaRecords.Add(GsaLoadCaseExamples(1, "load case 1").First());
      gsaRecords.AddRange(GsaNodeExamples(2, "node 1", "node 2"));
      var gsaLoadNodes = GsaLoadNodeExamples(2, "load node 1", "load node 2");
      gsaRecords.AddRange(gsaLoadNodes);
      Instance.GsaModel.Cache.Upsert(gsaRecords);

      //Convert
      Instance.GsaModel.StreamLayer = GSALayer.Both;
      Instance.GsaModel.StreamSendConfig = StreamContentConfig.ModelOnly;
      var speckleModels = converter.ConvertToSpeckle(gsaRecords.Select(i => (object)i).ToList());
      var speckleObjects = ((Model)speckleModels.First()).loads.FindAll(o => o is GSALoadNode).ToList();
      var gsaConvertedRecords = converter.ConvertToNative(speckleObjects);

      //Checks
      var gsaConvertedLoadNodes = gsaConvertedRecords.FindAll(r => r is GsaLoadNode).Select(r => (GsaLoadNode)r).ToList();
      var compareLogic = new CompareLogic();
      var result = compareLogic.Compare(gsaLoadNodes, gsaConvertedLoadNodes);
      Assert.Empty(result.Differences);
    }

    [Fact]
    public void LoadNodeToNative()
    {
      //Create native objects
      var gsaRecords = new List<GsaRecord>();
      gsaRecords.Add(GsaAxisExample("axis 1"));
      gsaRecords.Add(GsaPropMassExample("property mass 1"));
      gsaRecords.Add(GsaPropSprExample("property spring 1"));
      gsaRecords.Add(GsaLoadCaseExamples(1, "load case 1").First());
      gsaRecords.AddRange(GsaNodeExamples(2, "node 1", "node 2"));
      var gsaLoadNodes = GsaLoadNodeExamples(2, "load node 1", "load node 2");
      gsaRecords.AddRange(gsaLoadNodes);
      
      Instance.GsaModel.Cache.Upsert(gsaRecords);

      //Convert
      Instance.GsaModel.StreamLayer = GSALayer.Both;
      Instance.GsaModel.StreamSendConfig = StreamContentConfig.ModelOnly;
      var speckleModels = converter.ConvertToSpeckle(gsaRecords.Select(i => (object)i).ToList());
      var speckleAppSpecificObjects = ((Model)speckleModels.First()).loads.FindAll(o => o is GSALoadNode).Select(o => (GSALoadNode)o).ToList();
      Map<GSALoadNode, LoadNode>(speckleAppSpecificObjects, out var speckleAppAgnosticObjects);
      var gsaConvertedRecords = converter.ConvertToNative(speckleAppAgnosticObjects.Select(o => (Base)o).ToList());

      //Checks
      var gsaConvertedLoadNodes = gsaConvertedRecords.FindAll(r => r is GsaLoadNode).Select(r => (GsaLoadNode)r).ToList();
      var compareLogic = new CompareLogic();
      //Ignore app specific data, e.g. compareLogic.Config.MembersToIgnore.Add(GetPropertyName((GsaLoadNode x) => x.Name)); 
      var result = compareLogic.Compare(gsaLoadNodes, gsaConvertedLoadNodes);
      Assert.Empty(result.Differences);
    }

    [Fact]
    public void GSALoadGravityToNative()
    {
      //Create native objects
      var gsaRecords = new List<GsaRecord>();
      gsaRecords.Add(GsaMatSteelExample("steel material 1"));
      gsaRecords.Add(GsaPropMassExample("property mass 1"));
      gsaRecords.Add(GsaPropSprExample("property spring 1"));
      gsaRecords.Add(GsaLoadCaseExamples(1, "load case 1").First());
      gsaRecords.AddRange(GsaNodeExamples(5, "node 1", "node 2", "node 3", "node 4", "node 5"));
      gsaRecords.Add(GsaProp2dExample("property 2D 1"));
      gsaRecords.AddRange(GsaElement2dExamples(2, "element 1", "element 2"));
      var gsaLoadGravity = GsaLoadGravityExample("load gravity 1");
      gsaRecords.Add(gsaLoadGravity);
      Instance.GsaModel.Cache.Upsert(gsaRecords);

      //Convert
      Instance.GsaModel.StreamLayer = GSALayer.Both;
      Instance.GsaModel.StreamSendConfig = StreamContentConfig.ModelOnly;
      var speckleModels = converter.ConvertToSpeckle(gsaRecords.Select(i => (object)i).ToList());
      var speckleObjects = ((Model)speckleModels.Last()).loads.FindAll(o => o is GSALoadGravity).ToList();
      var gsaConvertedRecords = converter.ConvertToNative(speckleObjects);

      //Checks
      var gsaConvertedLoadGravity = gsaConvertedRecords.FindAll(r => r is GsaLoadGravity).Select(r => (GsaLoadGravity)r).First();
      var compareLogic = new CompareLogic();
      compareLogic.Config.MembersToIgnore.Add(GetPropertyName((GsaLoadGravity x) => x.MemberIndices));
      compareLogic.Config.MembersToIgnore.Add(GetPropertyName((GsaLoadGravity x) => x.X));
      compareLogic.Config.MembersToIgnore.Add(GetPropertyName((GsaLoadGravity x) => x.Y));
      var result = compareLogic.Compare(gsaLoadGravity, gsaConvertedLoadGravity);
      Assert.Empty(result.Differences);
      Assert.Null(gsaConvertedLoadGravity.MemberIndices);
      Assert.Null(gsaConvertedLoadGravity.X);
      Assert.Null(gsaConvertedLoadGravity.Y);
    }

    [Fact]
    public void LoadGravityToNative()
    {
      //Create native objects
      var gsaRecords = new List<GsaRecord>();
      gsaRecords.Add(GsaMatSteelExample("steel material 1"));
      gsaRecords.Add(GsaPropMassExample("property mass 1"));
      gsaRecords.Add(GsaPropSprExample("property spring 1"));
      gsaRecords.Add(GsaLoadCaseExamples(1, "load case 1").First());
      gsaRecords.AddRange(GsaNodeExamples(5, "node 1", "node 2", "node 3", "node 4", "node 5"));
      gsaRecords.Add(GsaProp2dExample("property 2D 1"));
      gsaRecords.AddRange(GsaElement2dExamples(2, "element 1", "element 2"));
      var gsaLoadGravity = GsaLoadGravityExample("load gravity 1");
      gsaRecords.Add(gsaLoadGravity);
      Instance.GsaModel.Cache.Upsert(gsaRecords);

      //Convert
      Instance.GsaModel.StreamLayer = GSALayer.Both;
      Instance.GsaModel.StreamSendConfig = StreamContentConfig.ModelOnly;
      var speckleModels = converter.ConvertToSpeckle(gsaRecords.Select(i => (object)i).ToList());
      var speckleAppSpecificObjects = ((Model)speckleModels.Last()).loads.FindAll(o => o is GSALoadGravity).Select(o => (GSALoadGravity)o).ToList();
      Map<GSALoadGravity, LoadGravity>(speckleAppSpecificObjects, out var speckleAppAgnosticObjects);
      var gsaConvertedRecords = converter.ConvertToNative(speckleAppAgnosticObjects.Select(o => (Base)o).ToList());

      //Checks
      var gsaConvertedLoadGravity = gsaConvertedRecords.FindAll(r => r is GsaLoadGravity).Select(r => (GsaLoadGravity)r).First();
      var compareLogic = new CompareLogic();
      compareLogic.Config.MembersToIgnore.Add(GetPropertyName((GsaLoadGravity x) => x.MemberIndices));
      compareLogic.Config.MembersToIgnore.Add(GetPropertyName((GsaLoadGravity x) => x.X));
      compareLogic.Config.MembersToIgnore.Add(GetPropertyName((GsaLoadGravity x) => x.Y));
      //Ignore app specific data - currently no app specific members
      var result = compareLogic.Compare(gsaLoadGravity, gsaConvertedLoadGravity);
      Assert.Empty(result.Differences);
      Assert.Null(gsaConvertedLoadGravity.MemberIndices);
      Assert.Null(gsaConvertedLoadGravity.X);
      Assert.Null(gsaConvertedLoadGravity.Y);
    }

    [Fact]
    public void GSALoadThermal2dToNative()
    {
      //Create native objects
      var gsaRecords = new List<GsaRecord>();
      gsaRecords.AddRange(GsaLoadCaseExamples(2, "load case 1", "load case 2"));
      gsaRecords.Add(GsaAxisExample("axis 1"));
      gsaRecords.Add(GsaMatSteelExample("steel material 1"));
      gsaRecords.Add(GsaPropMassExample("property mass 1"));
      gsaRecords.Add(GsaPropSprExample("property spring 1"));
      gsaRecords.Add(GsaProp2dExample("property 2D 1"));
      gsaRecords.AddRange(GsaNodeExamples(4, "node 1", "node 2", "node 3", "node 4"));
      gsaRecords.Add(GsaElement2dExamples(1, "element 1").FirstOrDefault());
      var gsaLoadThermals = GsaLoad2dThermalExamples(2, "load thermal 1", "load thermal 2");
      gsaRecords.AddRange(gsaLoadThermals);
      Instance.GsaModel.Cache.Upsert(gsaRecords);

      //Convert
      Instance.GsaModel.StreamLayer = GSALayer.Both;
      Instance.GsaModel.StreamSendConfig = StreamContentConfig.ModelOnly;
      var speckleModels = converter.ConvertToSpeckle(gsaRecords.Select(i => (object)i).ToList());
      var speckleObjects = ((Model)speckleModels.Last()).loads.FindAll(o => o is GSALoadThermal2d).ToList();
      var gsaConvertedRecords = converter.ConvertToNative(speckleObjects);

      //Checks
      var gsaConvertedLoadThermals = gsaConvertedRecords.FindAll(r => r is GsaLoad2dThermal).Select(r => (GsaLoad2dThermal)r).ToList();
      var compareLogic = new CompareLogic();
      var result = compareLogic.Compare(gsaLoadThermals, gsaConvertedLoadThermals);
      Assert.Empty(result.Differences);
    }

    [Fact]
    public void GSALoadGridPointToNaitve()
    {
      //Create native objects
      var gsaRecords = new List<GsaRecord>();
      gsaRecords.AddRange(GsaLoadCaseExamples(2, "load case 1", "load case 2"));
      gsaRecords.Add(GsaAxisExample("axis 1"));
      gsaRecords.Add(GsaMatSteelExample("steel material 1"));
      gsaRecords.Add(GsaPropMassExample("property mass 1"));
      gsaRecords.Add(GsaPropSprExample("property spring 1"));
      gsaRecords.AddRange(GsaNodeExamples(2, "node 1", "node 2"));
      gsaRecords.Add(GsaCatalogueSectionExample("section 1"));
      gsaRecords.Add(GsaGridPlaneExamples(1, "grid plane 1").FirstOrDefault());
      gsaRecords.Add(GsaElement1dExamples(1, "beam 1").FirstOrDefault());
      gsaRecords.Add(GsaGridSurfaceExamples(1, "grid surface 1").FirstOrDefault());
      var gsaLoadGridPoints = GsaLoadGridPointExamples(2, "load grid point 1", "load grid point 2");
      gsaRecords.AddRange(gsaLoadGridPoints);
      Instance.GsaModel.Cache.Upsert(gsaRecords);

      //Convert
      Instance.GsaModel.StreamLayer = GSALayer.Both;
      Instance.GsaModel.StreamSendConfig = StreamContentConfig.ModelOnly;
      var speckleModels = converter.ConvertToSpeckle(gsaRecords.Select(i => (object)i).ToList());
      var speckleObjects = ((Model)speckleModels.Last()).loads.FindAll(o => o is GSALoadGridPoint).ToList();
      var gsaConvertedRecords = converter.ConvertToNative(speckleObjects);

      //Checks
      var gsaConvertedLoadGrids = gsaConvertedRecords.FindAll(r => r is GsaLoadGridPoint).Select(r => (GsaLoadGridPoint)r).ToList();
      var compareLogic = new CompareLogic();
      compareLogic.Config.MembersToIgnore.Add(GetPropertyName((GsaLoadGridPoint x) => x.X));
      compareLogic.Config.MembersToIgnore.Add(GetPropertyName((GsaLoadGridPoint x) => x.Y));
      var result = compareLogic.Compare(gsaLoadGridPoints, gsaConvertedLoadGrids);
      Assert.Empty(result.Differences);
      Assert.Null(gsaConvertedLoadGrids[0].X);
      Assert.Null(gsaConvertedLoadGrids[0].Y);
      Assert.Null(gsaConvertedLoadGrids[1].X);
      Assert.Null(gsaConvertedLoadGrids[1].Y);
    }

    [Fact]
    public void GSALoadGridLineToNative()
    {
      //Create native objects
      var gsaRecords = new List<GsaRecord>();
      gsaRecords.AddRange(GsaLoadCaseExamples(2, "load case 1", "load case 2"));
      gsaRecords.Add(GsaAxisExample("axis 1"));
      gsaRecords.Add(GsaMatSteelExample("steel material 1"));
      gsaRecords.Add(GsaPropMassExample("property mass 1"));
      gsaRecords.Add(GsaPropSprExample("property spring 1"));
      gsaRecords.AddRange(GsaNodeExamples(2, "node 1", "node 2"));
      gsaRecords.Add(GsaCatalogueSectionExample("section 1"));
      gsaRecords.Add(GsaGridPlaneExamples(1, "grid plane 1").FirstOrDefault());
      gsaRecords.Add(GsaPolylineExamples(1, "polyline 1").FirstOrDefault());
      gsaRecords.Add(GsaElement1dExamples(1, "beam 1").FirstOrDefault());
      gsaRecords.Add(GsaGridSurfaceExamples(1, "grid surface 1").FirstOrDefault());
      var gsaLoadGridLines = GsaLoadGridLineExamples(2, "load grid line 1", "load grid line 2");
      gsaRecords.AddRange(gsaLoadGridLines);
      Instance.GsaModel.Cache.Upsert(gsaRecords);

      //Convert
      Instance.GsaModel.StreamLayer = GSALayer.Both;
      Instance.GsaModel.StreamSendConfig = StreamContentConfig.ModelOnly;
      var speckleModels = converter.ConvertToSpeckle(gsaRecords.Select(i => (object)i).ToList());
      var speckleObjects = ((Model)speckleModels.Last()).loads.FindAll(o => o is GSALoadGridLine).ToList();
      var gsaConvertedRecords = converter.ConvertToNative(speckleObjects);

      //Checks
      var gsaConvertedLoadGrids = gsaConvertedRecords.FindAll(r => r is GsaLoadGridLine).Select(r => (GsaLoadGridLine)r).ToList();
      var compareLogic = new CompareLogic();
      var result = compareLogic.Compare(gsaLoadGridLines, gsaConvertedLoadGrids);
      Assert.Empty(result.Differences);
    }

    [Fact]
    public void GSALoadGridAreaToNative()
    {
      //Create native objects
      var gsaRecords = new List<GsaRecord>();
      gsaRecords.AddRange(GsaLoadCaseExamples(2, "load case 1", "load case 2"));
      gsaRecords.Add(GsaAxisExample("axis 1"));
      gsaRecords.Add(GsaMatSteelExample("steel material 1"));
      gsaRecords.Add(GsaPropMassExample("property mass 1"));
      gsaRecords.Add(GsaPropSprExample("property spring 1"));
      gsaRecords.AddRange(GsaNodeExamples(2, "node 1", "node 2"));
      gsaRecords.Add(GsaCatalogueSectionExample("section 1"));
      gsaRecords.Add(GsaGridPlaneExamples(1, "grid plane 1").FirstOrDefault());
      gsaRecords.Add(GsaPolylineExamples(1, "polyline 1").FirstOrDefault());
      gsaRecords.Add(GsaElement1dExamples(1, "beam 1").FirstOrDefault());
      gsaRecords.Add(GsaGridSurfaceExamples(1, "grid surface 1").FirstOrDefault());
      var gsaLoadGridAreas = GsaLoadGridAreaExamples(2, "load grid area 1", "load grid area 2");
      gsaRecords.AddRange(gsaLoadGridAreas);
      Instance.GsaModel.Cache.Upsert(gsaRecords);

      //Convert
      Instance.GsaModel.StreamLayer = GSALayer.Both;
      Instance.GsaModel.StreamSendConfig = StreamContentConfig.ModelOnly;
      var speckleModels = converter.ConvertToSpeckle(gsaRecords.Select(i => (object)i).ToList());
      var speckleObjects = ((Model)speckleModels.Last()).loads.FindAll(o => o is GSALoadGridArea).ToList();
      var gsaConvertedRecords = converter.ConvertToNative(speckleObjects);

      //Checks
      var gsaConvertedLoadGrids = gsaConvertedRecords.FindAll(r => r is GsaLoadGridArea).Select(r => (GsaLoadGridArea)r).ToList();
      var compareLogic = new CompareLogic();
      var result = compareLogic.Compare(gsaLoadGridAreas, gsaConvertedLoadGrids);
      Assert.Empty(result.Differences);
    }

    //TODO: add app agnostic test methods
    #endregion

    #region Materials
    [Fact]
    public void GSASteelToNative()
    {
      //Create native objects
      var gsaRecords = new List<GsaRecord>();
      var gsaMatSteel = GsaMatSteelExample("steel 1");
      gsaRecords.Add(gsaMatSteel);
      Instance.GsaModel.Cache.Upsert(gsaRecords);

      //Convert
      Instance.GsaModel.StreamLayer = GSALayer.Both;
      Instance.GsaModel.StreamSendConfig = StreamContentConfig.ModelOnly;
      var speckleObjects = converter.ConvertToSpeckle(gsaRecords.Select(i => (object)i).ToList());
      var gsaConvertedRecords = converter.ConvertToNative(speckleObjects);

      //Checks
      var gsaConvertedSteel = gsaConvertedRecords.FindAll(r => r is GsaMatSteel).Select(r => (GsaMatSteel)r).First();
      var compareLogic = new CompareLogic();
      var result = compareLogic.Compare(gsaMatSteel, gsaConvertedSteel);
      Assert.Empty(result.Differences);
    }

    [Fact]
    public void SteelToNative()
    {
      //Create native objects
      var gsaRecords = new List<GsaRecord>();
      var gsaMatSteel = GsaMatSteelExample("steel 1");
      gsaRecords.Add(gsaMatSteel);
      Instance.GsaModel.Cache.Upsert(gsaRecords);

      //Convert
      Instance.GsaModel.StreamLayer = GSALayer.Both;
      Instance.GsaModel.StreamSendConfig = StreamContentConfig.ModelOnly;
      var speckleAppSpecificObjects = converter.ConvertToSpeckle(gsaRecords.Select(i => (object)i).ToList());
      Map<GSASteel, Steel>(speckleAppSpecificObjects.Select(o=>(GSASteel)o).ToList(), out var speckleAppAgnosticObjects);
      var gsaConvertedRecords = converter.ConvertToNative(speckleAppAgnosticObjects.Select(o => (Base)o).ToList());

      //Checks
      var gsaConvertedSteel = gsaConvertedRecords.FindAll(r => r is GsaMatSteel).Select(r => (GsaMatSteel)r).First();
      var compareLogic = new CompareLogic();
      compareLogic.Config.MembersToIgnore.Add(GetPropertyName((GsaMat x) => x.Name)); //Ignore app specific data
      var result = compareLogic.Compare(gsaMatSteel, gsaConvertedSteel);
      Assert.Empty(result.Differences);
    }

    [Fact]
    public void GSAConcreteToNative()
    {
      //Create native objects
      var gsaRecords = new List<GsaRecord>();
      var gsaMatConcrete = GsaMatConcreteExample("concrete 1");
      gsaRecords.Add(gsaMatConcrete);
      Instance.GsaModel.Cache.Upsert(gsaRecords);

      //Convert
      Instance.GsaModel.StreamLayer = GSALayer.Both;
      Instance.GsaModel.StreamSendConfig = StreamContentConfig.ModelOnly;
      var speckleObjects = converter.ConvertToSpeckle(gsaRecords.Select(i => (object)i).ToList());
      var gsaConvertedRecords = converter.ConvertToNative(speckleObjects);

      //Checks
      var gsaConvertedConcrete = gsaConvertedRecords.FindAll(r => r is GsaMatConcrete).Select(r => (GsaMatConcrete)r).First();
      var compareLogic = new CompareLogic();
      var result = compareLogic.Compare(gsaMatConcrete, gsaConvertedConcrete);
      Assert.Empty(result.Differences);
    }

    [Fact]
    public void ConcreteToNative()
    {
      //Create native objects
      var gsaRecords = new List<GsaRecord>();
      var gsaMatConcrete = GsaMatConcreteExample("concrete 1");
      gsaRecords.Add(gsaMatConcrete);
      Instance.GsaModel.Cache.Upsert(gsaRecords);

      //Convert
      Instance.GsaModel.StreamLayer = GSALayer.Both;
      Instance.GsaModel.StreamSendConfig = StreamContentConfig.ModelOnly;
      var speckleAppSpecificObjects = converter.ConvertToSpeckle(gsaRecords.Select(i => (object)i).ToList());
      Map<GSAConcrete, Concrete>(speckleAppSpecificObjects.Select(o => (GSAConcrete)o).ToList(), out var speckleAppAgnosticObjects);
      var gsaConvertedRecords = converter.ConvertToNative(speckleAppAgnosticObjects.Select(o => (Base)o).ToList());

      //Checks
      var gsaConvertedConcrete = gsaConvertedRecords.FindAll(r => r is GsaMatConcrete).Select(r => (GsaMatConcrete)r).First();
      var compareLogic = new CompareLogic();
      compareLogic.Config.MembersToIgnore.Add(GetPropertyName((GsaMat x) => x.Name)); //Ignore app specific data
      compareLogic.Config.MembersToIgnore.Add(GetPropertyName((GsaMatConcrete x) => x.Fcfib));
      var result = compareLogic.Compare(gsaMatConcrete, gsaConvertedConcrete);
      Assert.Empty(result.Differences);
      Assert.Equal(gsaMatConcrete.Fcfib.Value, gsaConvertedConcrete.Fcfib.Value, 3);
    }
    #endregion

    #region Properties

    [Fact]
    public void Property1dToNative()
    {
      //Define GSA objects
      //These should be in order that respects the type dependency tree (which is only available in the GSAProxy library, which isn't referenced yet
      var gsaRecords = new List<GsaRecord>
      {
        //Generation #1: Types with no other dependencies - the leaves of the tree
        GsaMatSteelExample("steel material 1")
      };

      //Gen #2
      var gsaSections = new List<GsaSection>
      {
        GsaCatalogueSectionExample("section 1"),
        GsaExplicitSectionExample("section 2"),
        GsaPerimeterSectionExample("section 3"),
        GsaRectangularSectionExample("section 4"),
        GsaRectangularHollowSectionExample("section 5"),
        GsaCircularSectionExample("section 6"),
        GsaCircularHollowSectionExample("section 7"),
        GsaISectionSectionExample("section 8"),
        GsaTSectionSectionExample("section 9"),
        GsaAngleSectionExample("section 10"),
        GsaChannelSectionExample("section 11")
      };
      gsaRecords.AddRange(gsaSections);

      Instance.GsaModel.Cache.Upsert(gsaRecords);

      foreach (var record in gsaRecords)
      {
        var speckleObjects = converter.ConvertToSpeckle(new List<object> { record });
        Assert.Empty(converter.ConversionErrors);

        Instance.GsaModel.Cache.SetSpeckleObjects(record, speckleObjects.ToDictionary(so => so.applicationId, so => (object)so));
      }

      Assert.True(Instance.GsaModel.Cache.GetSpeckleObjects(out var objs));

      var structuralObjects = objs.Cast<Base>().OrderBy(o => o.applicationId).ToList();

      Assert.NotEmpty(structuralObjects);
      Assert.Contains(structuralObjects, so => so is GSAProperty1D);

      var speckleProperty1Ds = structuralObjects.FindAll(so => so is GSAProperty1D).Select(so => (GSAProperty1D)so).ToList();

      var compareLogic = new CompareLogic();
      compareLogic.Config.MembersToIgnore.Add("SectionSteel.Type");
      //compareLogic.Config.IgnoreProperty<SectionSteel>(c => c.Type);

      foreach (var prop in speckleProperty1Ds)
      {
        var newNatives = converter.ConvertToNative(new List<Base> { prop });
        var newNative = newNatives.FirstOrDefault(n => n.GetType().IsAssignableFrom(typeof(GsaSection)));
        var oldNative = gsaSections.FirstOrDefault(s => s.ApplicationId.Equals(prop.applicationId, StringComparison.InvariantCultureIgnoreCase));
        var result = compareLogic.Compare(newNative, oldNative);
        Assert.True(result.AreEqual);
      }
    }

    [Fact]
    public void Property2dToNative()
    {
      //Define GSA objects
      //These should be in order that respects the type dependency tree (which is only available in the GSAProxy library, which isn't referenced yet
      var gsaRecords = new List<GsaRecord>();

      //Generation #1: Types with no other dependencies - the leaves of the tree
      gsaRecords.Add(GsaMatSteelExample("steel material 1"));

      //Gen #2
      var gsaProp2d = GsaProp2dExample("property 2D 1");
      gsaRecords.Add(gsaProp2d);

      Instance.GsaModel.Cache.Upsert(gsaRecords);

      foreach (var record in gsaRecords)
      {
        var speckleObjects = converter.ConvertToSpeckle(new List<object> { record });
        Assert.Empty(converter.ConversionErrors);

        Instance.GsaModel.Cache.SetSpeckleObjects(record, speckleObjects.ToDictionary(so => so.applicationId, so => (object)so));
      }

      Assert.True(Instance.GsaModel.Cache.GetSpeckleObjects(out var structuralObjects));

      Assert.NotEmpty(structuralObjects);
      Assert.Contains(structuralObjects, so => so is GSAProperty2D);

      var prop = (GSAProperty2D)structuralObjects.FirstOrDefault(so => so is GSAProperty2D);

      var compareLogic = new CompareLogic();

      var newNatives = converter.ConvertToNative(new List<Base> { prop });
      var newNative = newNatives.FirstOrDefault(n => n.GetType().IsAssignableFrom(typeof(GsaProp2d)));
      var oldNative = gsaProp2d;
      var result = compareLogic.Compare(newNative, oldNative);
      Assert.True(result.AreEqual);
    }

    [Fact]
    public void PropertyMassToNative()
    {
      //Define GSA objects
      //These should be in order that respects the type dependency tree (which is only available in the GSAProxy library, which isn't referenced yet
      var gsaRecords = new List<GsaRecord>();

      //Generation #1: Types with no other dependencies - the leaves of the tree
      var gsaPropMass = GsaPropMassExample("property mass 1");
      gsaRecords.Add(gsaPropMass);

      Instance.GsaModel.Cache.Upsert(gsaRecords);

      foreach (var record in gsaRecords)
      {
        var speckleObjects = converter.ConvertToSpeckle(new List<object> { record });
        Assert.Empty(converter.ConversionErrors);

        Instance.GsaModel.Cache.SetSpeckleObjects(record, speckleObjects.ToDictionary(so => so.applicationId, so => (object)so));
      }

      Assert.True(Instance.GsaModel.Cache.GetSpeckleObjects(out var structuralObjects));

      Assert.NotEmpty(structuralObjects);
      Assert.Contains(structuralObjects, so => so is PropertyMass);

      var specklePropertyMass = (PropertyMass)structuralObjects.FirstOrDefault(so => so is PropertyMass);

      var compareLogic = new CompareLogic();

      var newNatives = converter.ConvertToNative(new List<Base> { specklePropertyMass });
      var newNative = newNatives.FirstOrDefault(n => n.GetType().IsAssignableFrom(typeof(GsaPropMass)));
      var oldNative = gsaPropMass;
      var result = compareLogic.Compare(newNative, oldNative);
      Assert.True(result.AreEqual);
    }

    [Fact]
    public void PropertySpringToNative()
    {
      //Define GSA objects
      //These should be in order that respects the type dependency tree (which is only available in the GSAProxy library, which isn't referenced yet
      var gsaRecords = new List<GsaRecord>();

      //Generation #1: Types with no other dependencies - the leaves of the tree
      var gsaProSpr = GsaPropSprExample("property spring 1");
      gsaRecords.Add(gsaProSpr);

      Instance.GsaModel.Cache.Upsert(gsaRecords);

      foreach (var record in gsaRecords)
      {
        var speckleObjects = converter.ConvertToSpeckle(new List<object> { record });
        Assert.Empty(converter.ConversionErrors);

        Instance.GsaModel.Cache.SetSpeckleObjects(record, speckleObjects.ToDictionary(so => so.applicationId, so => (object)so));
      }

      Assert.True(Instance.GsaModel.Cache.GetSpeckleObjects(out var structuralObjects));

      Assert.NotEmpty(structuralObjects);
      Assert.Contains(structuralObjects, so => so is PropertySpring);

      var specklePropertySpring = (PropertySpring)structuralObjects.FirstOrDefault(so => so is PropertySpring);

      var compareLogic = new CompareLogic();

      var newNatives = converter.ConvertToNative(new List<Base> { specklePropertySpring });
      var newNative = newNatives.FirstOrDefault(n => n.GetType().IsAssignableFrom(typeof(GsaPropSpr)));
      var oldNative = gsaProSpr;
      var result = compareLogic.Compare(newNative, oldNative);
      Assert.True(result.AreEqual);
    }

    #endregion

    #region Results
    #endregion

    #region Constraints
    [Fact]
    public void GSARigidConstraintToNative()
    {
      //Create native objects
      var gsaRecords = new List<GsaRecord>();
      gsaRecords.Add(GsaMatSteelExample("steel material 1"));
      gsaRecords.Add(GsaPropMassExample("property mass 1"));
      gsaRecords.Add(GsaPropSprExample("property spring 1"));
      gsaRecords.AddRange(GsaNodeExamples(3, "node 1", "node 2", "node 3"));
      gsaRecords.Add(GsaCatalogueSectionExample("section 1"));
      gsaRecords.AddRange(GsaElement1dExamples(2, "element 1", "element 2"));
      gsaRecords.Add(GsaAnalStageExamples(1, "stage 1").FirstOrDefault());
      var gsaRigids = GsaRigidExamples(2, "rigid 1", "rigid 2");
      gsaRecords.AddRange(gsaRigids);
      Instance.GsaModel.Cache.Upsert(gsaRecords);

      //Convert
      Instance.GsaModel.StreamLayer = GSALayer.Both;
      Instance.GsaModel.StreamSendConfig = StreamContentConfig.ModelOnly;
      var speckleModels = converter.ConvertToSpeckle(gsaRecords.Select(i => (object)i).ToList());
      var speckleObjects = ((Model)speckleModels.Last()).elements.FindAll(o => o is GSARigidConstraint).ToList();
      var gsaConvertedRecords = converter.ConvertToNative(speckleObjects);

      //Checks
      var gsaConvertedRigids = gsaConvertedRecords.FindAll(r => r is GsaRigid).Select(r => (GsaRigid)r).ToList();
      var compareLogic = new CompareLogic();
      var result = compareLogic.Compare(gsaRigids, gsaConvertedRigids);
      Assert.Empty(result.Differences);
    }

    [Fact]
    public void GSAGeneralisedRestraintToNative()
    {
      //Create native objects
      var gsaRecords = new List<GsaRecord>();
      gsaRecords.Add(GsaMatSteelExample("steel material 1"));
      gsaRecords.Add(GsaPropMassExample("property mass 1"));
      gsaRecords.Add(GsaPropSprExample("property spring 1"));
      gsaRecords.AddRange(GsaNodeExamples(3, "node 1", "node 2", "node 3"));
      gsaRecords.Add(GsaCatalogueSectionExample("section 1"));
      gsaRecords.AddRange(GsaElement1dExamples(2, "element 1", "element 2"));
      gsaRecords.AddRange(GsaAnalStageExamples(2, "stage 1", "stage 2"));
      var gsaGenRests = GsaGenRestExamples(2, "gen rest 1", "gen rest 2");
      gsaRecords.AddRange(gsaGenRests);
      Instance.GsaModel.Cache.Upsert(gsaRecords);

      //Convert
      Instance.GsaModel.StreamLayer = GSALayer.Both;
      Instance.GsaModel.StreamSendConfig = StreamContentConfig.ModelOnly;
      var speckleModels = converter.ConvertToSpeckle(gsaRecords.Select(i => (object)i).ToList());
      var speckleObjects = ((Model)speckleModels.Last()).elements.FindAll(o => o is GSAGeneralisedRestraint).ToList();
      var gsaConvertedRecords = converter.ConvertToNative(speckleObjects);

      //Checks
      var gsaConvertedGenRests = gsaConvertedRecords.FindAll(r => r is GsaGenRest).Select(r => (GsaGenRest)r).ToList();
      var compareLogic = new CompareLogic();
      var result = compareLogic.Compare(gsaGenRests, gsaConvertedGenRests);
      Assert.Empty(result.Differences);
    }
    #endregion

    #region Analysis Stages
    [Fact]
    public void GSAStageToNative()
    {
      var twoElements = new List<GSAElement1D>
      {
        GetElement1d1(),
        new GSAElement1D(2, null, null, ElementType1D.Bar, orientationAngle: 0D),
      }.Select(x => x as Base).ToList();

      var twoLockedElements = new List<GSAElement1D>
      {
        new GSAElement1D(3, null, null, ElementType1D.Bar, orientationAngle: 0D),
        new GSAElement1D(4, null, null, ElementType1D.Bar, orientationAngle: 0D),
      }.Select(x => x as Base).ToList();

      var gsaStage = new GSAStage(1, "", Colour.RED.ToString(), twoElements, 1, 2, twoLockedElements);
      var gsaRecord = converter.ConvertToNative(gsaStage) as List<GsaRecord>;

      var gsaAnalStage = GenericTestForList<GsaAnalStage>(gsaRecord);

      Assert.Equal(gsaStage.colour, gsaAnalStage.Colour.ToString());
      Assert.Equal(gsaStage.name, gsaAnalStage.Name);
      Assert.Equal(gsaStage.creepFactor, gsaAnalStage.Phi);
      Assert.Equal(gsaStage.stageTime, gsaAnalStage.Days);
      Assert.Equal(gsaStage.elements.Count, twoElements.Count);
      Assert.Equal(gsaStage.lockedElements.Count, twoLockedElements.Count);
    }
    #endregion

    #region Bridges
    [Fact]
    public void GSAAlignmentToNativeTest()
    {
      var gsaAlignment = GetGsaAlignment();
      var gsaRecord = converter.ConvertToNative(gsaAlignment) as List<GsaRecord>;

      var gsaAlign = GenericTestForList<GsaAlign>(gsaRecord);

      Assert.Equal(gsaAlignment.chainage, gsaAlign.Chain);
      Assert.Equal(gsaAlignment.curvature, gsaAlign.Curv);
      Assert.Equal(gsaAlignment.name, gsaAlign.Name);
      Assert.Equal(gsaAlignment.id, gsaAlign.Sid);
      Assert.Equal(gsaAlignment.GetNumAlignmentPoints(), gsaAlign.NumAlignmentPoints);
      Assert.Equal(gsaAlignment.GetNumAlignmentPoints(), gsaAlign.NumAlignmentPoints);

      // var copy = converter.ConvertToSpeckle(
      //   converter.ConvertToNative(gsaAlignment));
      // Assert.Equal(gsaAlignment, copy);
    }

    [Fact]
    public void GSAInfluenceBeamToNativeTest()
    {
      var gsaInfluenceBeam = new GSAInfluenceBeam(1, "hey", 1.4, InfluenceType.FORCE, LoadDirection.X, GetElement1d1(), 0.5);
      var gsaRecord = converter.ConvertToNative(gsaInfluenceBeam) as List<GsaRecord>;
      var gsaInfBeam = GenericTestForList<GsaInfBeam>(gsaRecord);

      Assert.Equal(gsaInfluenceBeam.position, gsaInfBeam.Position);
      Assert.Equal(gsaInfluenceBeam.direction.ToNative(), gsaInfBeam.Direction);
      Assert.Equal(gsaInfluenceBeam.factor, gsaInfBeam.Factor);
      Assert.Equal(gsaInfluenceBeam.id, gsaInfBeam.Sid);
      Assert.Equal(gsaInfluenceBeam.element.nativeId, gsaInfBeam.Element);
      Assert.Equal(gsaInfluenceBeam.type.ToNative(), gsaInfBeam.Type);
      Assert.Equal(gsaInfluenceBeam.name, gsaInfBeam.Name);

      //var copy = converter.ConvertToSpeckle(converter.ConvertToNative(gsaInfluenceBeam));
      //Assert.Equal(gsaInfluenceBeam, copy);
    }

    [Fact]
    public void GSAInfluenceNodeToNativeTest()
    {
      var gsaInfluenceNode = new GSAInfluenceNode(1, "hey", 1.4, InfluenceType.FORCE, LoadDirection.X, GetNode(), SpeckleGlobalAxis());
      var gsaRecord = converter.ConvertToNative(gsaInfluenceNode) as List<GsaRecord>;
      var gsaInfNode = GenericTestForList<GsaInfNode>(gsaRecord);

      Assert.Equal(gsaInfluenceNode.direction.ToNative(), gsaInfNode.Direction);
      Assert.Equal(gsaInfluenceNode.factor, gsaInfNode.Factor);
      Assert.Equal(gsaInfluenceNode.id, gsaInfNode.Sid);
      Assert.Equal(gsaInfluenceNode.type.ToNative(), gsaInfNode.Type);
      Assert.Equal(gsaInfluenceNode.name, gsaInfNode.Name);
      Assert.Equal(gsaInfluenceNode.applicationId, gsaInfNode.ApplicationId);
      Assert.Equal(gsaInfluenceNode.nativeId, gsaInfNode.Index);
    }

    [Fact]
    public void GSAPathToNativeTest()
    {
      var gsaPath = new GSAPath(1, "myPath", PathType.TRACK, 2, GetGsaAlignment(), 1, 2, 3, 4);
      var gsaRecord = converter.ConvertToNative(gsaPath) as List<GsaRecord>;
      var gsaP = GenericTestForList<GsaPath>(gsaRecord);

      Assert.Equal(gsaPath.factor, gsaP.Factor);
      Assert.Equal(gsaPath.id, gsaP.Sid);
      Assert.Equal(gsaPath.name, gsaP.Name);
      Assert.Equal(gsaPath.applicationId, gsaP.ApplicationId);
      Assert.Equal(gsaPath.nativeId, gsaP.Index);

      Assert.Equal(gsaPath.type.ToNative(), gsaP.Type);
      Assert.Equal(gsaPath.left, gsaP.Left);
      Assert.Equal(gsaPath.right, gsaP.Right);
      Assert.Equal(gsaPath.numMarkedLanes, gsaP.NumMarkedLanes);
      Assert.Equal(gsaPath.group, gsaP.Group);
      Assert.Equal(gsaPath.factor, gsaP.Factor);
    }

    [Fact]
    public void GSAUserVehicleToNative()
    {
      //Create native objects
      var gsaRecords = new List<GsaRecord>();
      var gsaVehicles = GsaUserVehicleExamples(2, "vehicle 1", "vehicle 2");
      gsaRecords.AddRange(gsaVehicles);
      Instance.GsaModel.Cache.Upsert(gsaRecords);

      //Convert
      Instance.GsaModel.StreamLayer = GSALayer.Both;
      Instance.GsaModel.StreamSendConfig = StreamContentConfig.ModelOnly;
      var speckleModels = converter.ConvertToSpeckle(gsaRecords.Select(i => (object)i).ToList());
      var speckleObjects = ((Model)speckleModels.Last()).elements.FindAll(o => o is GSAUserVehicle).ToList();
      var gsaConvertedRecords = converter.ConvertToNative(speckleObjects);

      //Checks
      var gsaConvertedVehicles = gsaConvertedRecords.FindAll(r => r is GsaUserVehicle).Select(r => (GsaUserVehicle)r).ToList();
      var compareLogic = new CompareLogic();
      var result = compareLogic.Compare(gsaVehicles, gsaConvertedVehicles);
      Assert.Empty(result.Differences);
    }
    #endregion

    #region Helper
    #region Geometry
    private List<GSANode> SpeckleNodeExamples(int num, params string[] appIds)
    {
      var speckleNodes = new List<GSANode>()
      {
        GetNode(),
        new GSANode()
        {
          nativeId = 2,
          name = "",
          basePoint = new Point(1, 0, 0),
          constraintAxis = SpeckleGlobalAxis(),
          localElementSize = 1,
          colour = "NO_RGB",
          restraint = new Restraint(RestraintType.Free),
          springProperty = null,
          massProperty = null,
          damperProperty = null,
          units = "",
        },
        new GSANode()
        {
          nativeId = 3,
          name = "",
          basePoint = new Point(1, 1, 0),
          constraintAxis = SpeckleGlobalAxis(),
          localElementSize = 1,
          colour = "NO_RGB",
          restraint = new Restraint(RestraintType.Free),
          springProperty = null,
          massProperty = null,
          damperProperty = null,
          units = "",
        },
        new GSANode()
        {
          nativeId = 4,
          name = "",
          basePoint = new Point(0, 1, 0),
          constraintAxis = SpeckleGlobalAxis(),
          localElementSize = 1,
          colour = "NO_RGB",
          restraint = new Restraint(RestraintType.Free),
          springProperty = null,
          massProperty = null,
          damperProperty = null,
          units = "",
        },
        new GSANode()
        {
          nativeId = 5,
          name = "",
          basePoint = new Point(2, 0, 0),
          constraintAxis = SpeckleGlobalAxis(),
          localElementSize = 1,
          colour = "NO_RGB",
          restraint = new Restraint(RestraintType.Free),
          springProperty = null,
          massProperty = null,
          damperProperty = null,
          units = "",
        }
      };
      for (int i = 0; i < appIds.Count(); i++)
      {
        speckleNodes[i].applicationId = appIds[i];
      }
      return speckleNodes.GetRange(0, num);
    }

    private GSANode GetNode()
    {
      return new GSANode()
      {
        nativeId = 1,
        name = "",
        basePoint = new Point(0, 0, 0),
        constraintAxis = SpeckleGlobalAxis(),
        localElementSize = 1,
        colour = "NO_RGB",
        restraint = new Restraint(RestraintType.Free),
        springProperty = null,
        massProperty = null,
        damperProperty = null,
        units = "",
      };
    }

    private List<GSAElement1D> SpeckleElement1dExamples(int num, params string[] appIds)
    {
      var speckleNodes = SpeckleNodeExamples(3, "node 1", "node 2", "node 3");
      var speckleElements = new List<GSAElement1D>()
      {
        new GSAElement1D()
        {
          nativeId = 1,
          name = "",
          baseLine = new Line(),
          property = SpeckleProperty1dExamples(1,"property 1").First(),
          type = ElementType1D.Beam,
          end1Releases = new Restraint(RestraintType.Fixed),
          end2Releases = new Restraint(RestraintType.Fixed),
          orientationNode = null,
          orientationAngle = 0,
          localAxis = SpeckleGlobalAxis().definition,
          parent = null,
          end1Node = speckleNodes[0],
          end2Node = speckleNodes[1],
          topology = speckleNodes.Select(n => (Node)n).ToList().GetRange(0, 2),
          displayMesh = null,
          units = "",
          group = 0,
          colour = "NO_RGB",
          action = "",
          isDummy = false,
        },
        new GSAElement1D()
        {
          nativeId = 2,
          name = "",
          baseLine = new Line(),
          property = SpeckleProperty1dExamples(1,"property 1").First(),
          type = ElementType1D.Beam,
          end1Releases = new Restraint(RestraintType.Fixed),
          end2Releases = new Restraint(RestraintType.Fixed),
          orientationNode = null,
          orientationAngle = 0,
          localAxis = SpeckleGlobalAxis().definition,
          parent = null,
          end1Node = speckleNodes[1],
          end2Node = speckleNodes[2],
          topology = speckleNodes.Select(n => (Node)n).ToList().GetRange(1, 2),
          displayMesh = null,
          units = "",
          group = 0,
          colour = "NO_RGB",
          action = "",
          isDummy = false,
        },
      };

      for (int i = 0; i < appIds.Count(); i++)
      {
        speckleElements[i].applicationId = appIds[i];
      }
      return speckleElements.GetRange(0, num);
    }

    private List<GSAElement2D> SpeckleElement2dExamples(int num, params string[] appIds)
    {
      var speckleNodes = SpeckleNodeExamples(5, "node 1", "node 2", "node 3", "node 4", "node 5");
      var speckleElements = new List<GSAElement2D>()
      {
        new GSAElement2D()
        {
          nativeId = 1,
          name = "",
          property = SpeckleProperty2dExamples(1,"property 1").First(),
          type = ElementType2D.Quad4,
          orientationAngle = 0,
          parent = null,
          topology = (new int[] { 1, 2, 4 }).Select(i => (Node)speckleNodes[i]).ToList(),
          displayMesh = null,
          units = "",
          group = 0,
          colour = "NO_RGB",
          isDummy = false,
          offset = 0,
        },
        new GSAElement2D()
        {
          nativeId = 2,
          name = "",
          property = SpeckleProperty2dExamples(1,"property 1").First(),
          type = ElementType2D.Triangle3,
          orientationAngle = 1,
          parent = null,
          topology = speckleNodes.Select(n => (Node)n).ToList().GetRange(1, 3),
          displayMesh = null,
          units = "",
          group = 0,
          colour = "NO_RGB",
          isDummy = false,
          offset = 0.1,
        },
      };

      for (int i = 0; i < appIds.Count(); i++)
      {
        speckleElements[i].applicationId = appIds[i];
      }
      return speckleElements.GetRange(0, num);
    }
    #endregion

    #region Loading
    #endregion

    #region Materials
    private List<GSASteel> SpeckleSteelExamples(int num, params string[] appIds)
    {
      var speckleSteels = new List<GSASteel>()
      {
        new GSASteel()
        {
          nativeId = 1,
          name = "",
          grade = "",
          materialType = MaterialType.Steel,
          designCode = "",
          codeYear = "",
          strength = 2e8,
          elasticModulus = 2e11,
          poissonsRatio = 0.3,
          shearModulus = 8e10,
          density = 7850,
          thermalExpansivity = 1e-6,
          dampingRatio = 0,
          cost = 0,
          materialSafetyFactor = 1,
          colour = "NO_RGB",
          yieldStrength = 2e8,
          ultimateStrength = 2.5e8,
          maxStrain = 0.05,
          strainHardeningModulus = 0,
        },
        new GSASteel()
        {
          nativeId = 2,
          name = "",
          grade = "",
          materialType = MaterialType.Steel,
          designCode = "",
          codeYear = "",
          strength = 2e8,
          elasticModulus = 2e11,
          poissonsRatio = 0.3,
          shearModulus = 8e10,
          density = 7850,
          thermalExpansivity = 1e-6,
          dampingRatio = 0,
          cost = 0,
          materialSafetyFactor = 1,
          colour = "NO_RGB",
          yieldStrength = 2e8,
          ultimateStrength = 2.5e8,
          maxStrain = 0.05,
          strainHardeningModulus = 0,
        },
      };
      for (int i = 0; i < appIds.Count(); i++)
      {
        speckleSteels[i].applicationId = appIds[i];
      }
      return speckleSteels.GetRange(0, num);
    }
    #endregion

    #region Properties
    private List<GSAProperty1D> SpeckleProperty1dExamples(int num, params string[] appIds)
    {
      var speckleProperty1d = new List<GSAProperty1D>()
      {
        new GSAProperty1D()
        {
          nativeId = 1,
          designMaterial = null,
          additionalMass = 0,
          cost = null,
          poolRef = null,
          colour = "NO_RGB",
          memberType = MemberType.Beam,
          material = SpeckleSteelExamples(1, "steel 1").First(),
          profile = SpeckleProfileExamples(1, "profile 1").First(),
          referencePoint = BaseReferencePoint.Centroid,
          offsetY = 0,
          offsetZ = 0,
        },
        new GSAProperty1D()
        {
          nativeId = 2,
          designMaterial = null,
          additionalMass = 0,
          cost = null,
          poolRef = null,
          colour = "NO_RGB",
          memberType = MemberType.Beam,
          material = SpeckleSteelExamples(1, "steel 1").First(),
          profile = SpeckleProfileExamples(1, "profile 1").First(),
          referencePoint = BaseReferencePoint.Centroid,
          offsetY = 0,
          offsetZ = 0,
        },
      };
      for (int i = 0; i < appIds.Count(); i++)
      {
        speckleProperty1d[i].applicationId = appIds[i];
      }
      return speckleProperty1d.GetRange(0, num);
    }

    private List<SectionProfile> SpeckleProfileExamples(int num, params string[] appIds)
    {
      var speckleProfiles = new List<SectionProfile>()
      {
        new Rectangular()
        {
          name = "",
          shapeType = ShapeType.Rectangular,
          width = 0.1,
          depth = 0.4,
          webThickness = 0,
          flangeThickness = 0
        },
        new ISection()
        {
          name = "",
          shapeType = ShapeType.I,
          width = 0.1,
          depth = 0.4,
          webThickness = 0.01,
          flangeThickness = 0.02
        },
      };
      for (int i = 0; i < appIds.Count(); i++)
      {
        speckleProfiles[i].applicationId = appIds[i];
      }
      return speckleProfiles.GetRange(0, num);
    }

    private List<GSAProperty2D> SpeckleProperty2dExamples(int num, params string[] appIds)
    {
      var speckleProperties = new List<GSAProperty2D>()
      {
        new GSAProperty2D()
        {

        },
        new GSAProperty2D()
        {

        },
      };
      for (int i = 0; i < appIds.Count(); i++)
      {
        speckleProperties[i].applicationId = appIds[i];
      }
      return speckleProperties.GetRange(0, num);
    }
    #endregion

    #region Results
    #endregion

    #region Constraints
    #endregion

    #region Analysis Stages
    #endregion

    #region Bridges
    private GSAAlignment GetGsaAlignment()
    {
      var axis = SpeckleGlobalAxis();
      var gsaGridPlane = new GSAGridPlane(1, "myGsaGridPlane", axis, 1);
      var gsaAlignment = new GSAAlignment(2, "myGsaAlignment",
        new GSAGridSurface("myGsaGridSurface", 1, gsaGridPlane, 1, 2,
          LoadExpansion.PlaneCorner, GridSurfaceSpanType.OneWay,
          new List<Base>()),
        new List<double>() { 0, 1 },
        new List<double>() { 3, 3 });
      return gsaAlignment;
    }

    private static GSAElement1D GetElement1d1()
    {
      return new GSAElement1D(1, null, null, ElementType1D.Bar, orientationAngle: 0D){applicationId = "appl1dforGsaElement1d"};
    }

    #endregion

    #region Other
    
    private static T GenericTestForList<T>(List<GsaRecord> gsaRecord)
    {
      Assert.NotEmpty(gsaRecord);
      Assert.Contains(gsaRecord, so => so is T);

      var obj = (T)(object)(gsaRecord.First());
      return obj;
    }
    
    private Axis SpeckleGlobalAxis()
    {
      return new Axis()
      {
        //applicationId = "",
        name = "",
        axisType = AxisType.Cartesian,
        definition = new Plane()
        {
          xdir = new Vector(1, 0, 0),
          ydir = new Vector(0, 1, 0),
          normal = new Vector(0, 0, 1),
          origin = new Point(0, 0, 0)
        }
      };
    }

    private bool Map<T,U>(List<T> source, out List<U> destination) where T : Base
    {
      destination = new List<U>();
      var config = new MapperConfiguration(cfg => { cfg.CreateMap< T, U >(); });
      var mapper = new Mapper(config);
      foreach (var s in source)
      {
        destination.Add(Activator.CreateInstance<U>());
        mapper.Map(s, destination.Last());
      }
      return true;
    }

    private bool Map<T, U>(T source, out U destination) where T : Base
    {
      destination = Activator.CreateInstance<U>();
      var config = new MapperConfiguration(cfg => { cfg.CreateMap<T, U>(); });
      var mapper = new Mapper(config);
      mapper.Map(source, destination);
      return true;
    }
    #endregion
    #endregion
  }
}
