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
using Objects.Structural.GSA.Geometry;
using Objects.Structural.GSA.Loading;
using Objects.Structural.GSA.Properties;
using Speckle.ConnectorGSA.Proxy.GwaParsers;
using GwaMemberType = Speckle.GSA.API.GwaSchema.MemberType;
using MemberType = Objects.Structural.Geometry.MemberType;
using Xunit.Sdk;
using Speckle.Core.Kits;
using ConverterGSA;

namespace ConverterGSATests
{
  public class SchemaTest : SpeckleConversionFixture
  {
    public SchemaTest() : base() { }

    //Reminder: conversions could create 1:1, 1:n, n:1, n:n structural per native objects

    #region Geometry
    [Fact]
    public void GsaAxis()
    {
      //Define GSA objects
      //These should be in order that respects the type dependency tree (which is only available in the GSAProxy library, which isn't referenced yet
      var gsaRecords = new List<GsaRecord>();

      //Generation #1: Types with no other dependencies - the leaves of the tree
      var gsaAxis = GsaAxisExample("axis 1");
      gsaRecords.Add(gsaAxis);

      Instance.GsaModel.Cache.Upsert(gsaRecords);

      foreach (var record in gsaRecords)
      {
        var speckleObjects = converter.ConvertToSpeckle(new List<object> { record });
        Assert.Empty(converter.ConversionErrors);

        Instance.GsaModel.Cache.SetSpeckleObjects(record, speckleObjects.ToDictionary(so => so.applicationId, so => (object)so));
      }

      Assert.True(Instance.GsaModel.Cache.GetSpeckleObjects(out var structuralObjects));

      Assert.NotEmpty(structuralObjects);
      Assert.Contains(structuralObjects, so => so is Axis);

      var speckleAxis = (Axis)structuralObjects.FirstOrDefault(so => so is Axis);

      Assert.Equal("axis 1", speckleAxis.applicationId);
      Assert.Equal(AxisType.Cartesian, speckleAxis.axisType);
      Assert.Equal(gsaAxis.OriginX, speckleAxis.definition.origin.x);
      Assert.Equal(gsaAxis.OriginY, speckleAxis.definition.origin.y);
      Assert.Equal(gsaAxis.OriginZ, speckleAxis.definition.origin.z);
      Assert.Equal(gsaAxis.XDirX.Value, speckleAxis.definition.xdir.x, 6);
      Assert.Equal(gsaAxis.XDirY.Value, speckleAxis.definition.xdir.y, 6);
      Assert.Equal(gsaAxis.XDirZ.Value, speckleAxis.definition.xdir.z, 6);
      Assert.Equal(gsaAxis.XYDirX.Value, speckleAxis.definition.ydir.x, 6);
      Assert.Equal(gsaAxis.XYDirY.Value, speckleAxis.definition.ydir.y, 6);
      Assert.Equal(gsaAxis.XYDirZ.Value, speckleAxis.definition.ydir.z, 6);
    }
    
    [Fact]
    public void GsaNode()
    {
      //Define GSA objects
      //These should be in order that respects the type dependency tree (which is only available in the GSAProxy library, which isn't referenced yet
      var gsaRecords = new List<GsaRecord>();

      //Generation #1: Types with no other dependencies - the leaves of the tree
      gsaRecords.Add(GsaPropMassExample("property mass 1"));
      gsaRecords.Add(GsaPropSprExample("property spring 1"));

      //Gen #2
      var gsaNodes = GsaNodeExamples(1, "node 1");
      gsaRecords.AddRange(gsaNodes);

      Instance.GsaModel.Cache.Upsert(gsaRecords);

      foreach (var record in gsaRecords)
      {
        var speckleObjects = converter.ConvertToSpeckle(new List<object> { record });
        Assert.Empty(converter.ConversionErrors);

        Instance.GsaModel.Cache.SetSpeckleObjects(record, speckleObjects.ToDictionary(so => so.applicationId, so => (object)so));
      }

      Assert.True(Instance.GsaModel.Cache.GetSpeckleObjects(out var structuralObjects));

      Assert.NotEmpty(structuralObjects);
      Assert.Contains(structuralObjects, so => so is GSANode);

      var speckleNode = (GSANode)structuralObjects.FirstOrDefault(so => so is GSANode);

      //Checks
      Assert.Equal("node 1", speckleNode.applicationId);
      Assert.Equal(gsaNodes[0].Name, speckleNode.name);
      Assert.Equal(gsaNodes[0].X, speckleNode.basePoint.x);
      Assert.Equal(gsaNodes[0].Y, speckleNode.basePoint.y);
      Assert.Equal(gsaNodes[0].Z, speckleNode.basePoint.z);
      Assert.Equal(new Restraint(RestraintType.Pinned).code, speckleNode.restraint.code);
      Assert.True(speckleNode.constraintAxis.IsGlobal());
      Assert.Equal("property spring 1", speckleNode.springProperty.applicationId); //assume conversion code for GsaPropSpr is tested elsewhere
      Assert.Equal("property mass 1", speckleNode.massProperty.applicationId); //assume conversion code for GsaPropMass is tested elsewhere
      Assert.Equal(gsaNodes[0].Index.Value, speckleNode.nativeId);
      Assert.Equal(gsaNodes[0].Colour.ToString(), speckleNode.colour);
      Assert.Equal(gsaNodes[0].MeshSize.Value, speckleNode.localElementSize);
    }

    [Fact]
    public void GsaElement2d()
    {
      //Define GSA objects
      //These should be in order that respects the type dependency tree (which is only available in the GSAProxy library, which isn't referenced yet
      var gsaRecords = new List<GsaRecord>();

      //Generation #1: Types with no other dependencies - the leaves of the tree
      gsaRecords.Add(GsaMatSteelExample("steel material 1"));
      gsaRecords.Add(GsaPropMassExample("property mass 1"));
      gsaRecords.Add(GsaPropSprExample("property spring 1"));

      //Gen #2
      var gsaNodes = GsaNodeExamples(5, "node 1", "node 2", "node 3", "node 4" , "node 5");
      gsaRecords.AddRange(gsaNodes);
      var gsaProp2d = GsaProp2dExample("property 2D 1");
      gsaRecords.Add(gsaProp2d);

      // Gen #3
      var gsaEls = GsaElement2dExamples(2, "element 1", "element 2");
      gsaRecords.AddRange(gsaEls);

      Instance.GsaModel.Cache.Upsert(gsaRecords);

      foreach (var record in gsaRecords)
      {
        var speckleObjects = converter.ConvertToSpeckle(new List<object> { record });
        Assert.Empty(converter.ConversionErrors);

        Instance.GsaModel.Cache.SetSpeckleObjects(record, speckleObjects.ToDictionary(o => o.applicationId, o => (object) o));
      }

      Assert.True(Instance.GsaModel.Cache.GetSpeckleObjects(out var structuralObjects));

      Assert.NotEmpty(structuralObjects);
      Assert.Contains(structuralObjects, so => so is GSAElement2D);

      var speckleElement2D = structuralObjects.FindAll(so => so is GSAElement2D).Select(so => (GSAElement2D)so).ToList();

      //===========
      // Element 1
      //===========
      Assert.Equal("element 1", speckleElement2D[0].applicationId);
      Assert.Equal(gsaEls[0].Name, speckleElement2D[0].name);
      //baseMesh
      Assert.Equal("property 2D 1", speckleElement2D[0].property.applicationId);
      Assert.Equal(ElementType2D.Quad4, speckleElement2D[0].type);
      Assert.Equal(gsaEls[0].OffsetZ.Value, speckleElement2D[0].offset);
      Assert.Equal(gsaEls[0].Angle.Value, speckleElement2D[0].orientationAngle);
      //parent
      Assert.Equal("node 1", speckleElement2D[0].topology[0].applicationId);
      Assert.Equal(gsaNodes[0].X, speckleElement2D[0].topology[0].basePoint.x);
      Assert.Equal(gsaNodes[0].Y, speckleElement2D[0].topology[0].basePoint.y);
      Assert.Equal(gsaNodes[0].Z, speckleElement2D[0].topology[0].basePoint.z);
      Assert.Equal("node 2", speckleElement2D[0].topology[1].applicationId);
      Assert.Equal(gsaNodes[1].X, speckleElement2D[0].topology[1].basePoint.x);
      Assert.Equal(gsaNodes[1].Y, speckleElement2D[0].topology[1].basePoint.y);
      Assert.Equal(gsaNodes[1].Z, speckleElement2D[0].topology[1].basePoint.z);
      Assert.Equal("node 3", speckleElement2D[0].topology[2].applicationId);
      Assert.Equal(gsaNodes[2].X, speckleElement2D[0].topology[2].basePoint.x);
      Assert.Equal(gsaNodes[2].Y, speckleElement2D[0].topology[2].basePoint.y);
      Assert.Equal(gsaNodes[2].Z, speckleElement2D[0].topology[2].basePoint.z);
      Assert.Equal("node 4", speckleElement2D[0].topology[3].applicationId);
      Assert.Equal(gsaNodes[3].X, speckleElement2D[0].topology[3].basePoint.x);
      Assert.Equal(gsaNodes[3].Y, speckleElement2D[0].topology[3].basePoint.y);
      Assert.Equal(gsaNodes[3].Z, speckleElement2D[0].topology[3].basePoint.z);
      //displayMesh
      Assert.Equal(gsaEls[0].Colour.ToString(), speckleElement2D[0].colour);
      Assert.False(speckleElement2D[0].isDummy);
      Assert.Equal(gsaEls[0].Index.Value, speckleElement2D[0].nativeId);
      Assert.Equal(gsaEls[0].Group.Value, speckleElement2D[0].group);

      //===========
      // Element 2
      //===========
      Assert.Equal("element 2", speckleElement2D[1].applicationId);
      Assert.Equal(gsaEls[1].Name, speckleElement2D[1].name);
      //baseMesh
      Assert.Equal("property 2D 1", speckleElement2D[1].property.applicationId);
      Assert.Equal(ElementType2D.Triangle3, speckleElement2D[1].type);
      Assert.Equal(gsaEls[1].OffsetZ.Value, speckleElement2D[1].offset);
      Assert.Equal(gsaEls[1].Angle.Value, speckleElement2D[1].orientationAngle);
      //parent
      Assert.Equal("node 2", speckleElement2D[1].topology[0].applicationId);
      Assert.Equal(gsaNodes[1].X, speckleElement2D[1].topology[0].basePoint.x);
      Assert.Equal(gsaNodes[1].Y, speckleElement2D[1].topology[0].basePoint.y);
      Assert.Equal(gsaNodes[1].Z, speckleElement2D[1].topology[0].basePoint.z);
      Assert.Equal("node 3", speckleElement2D[1].topology[1].applicationId);
      Assert.Equal(gsaNodes[2].X, speckleElement2D[1].topology[1].basePoint.x);
      Assert.Equal(gsaNodes[2].Y, speckleElement2D[1].topology[1].basePoint.y);
      Assert.Equal(gsaNodes[2].Z, speckleElement2D[1].topology[1].basePoint.z);
      Assert.Equal("node 5", speckleElement2D[1].topology[2].applicationId);
      Assert.Equal(gsaNodes[4].X, speckleElement2D[1].topology[2].basePoint.x);
      Assert.Equal(gsaNodes[4].Y, speckleElement2D[1].topology[2].basePoint.y);
      Assert.Equal(gsaNodes[4].Z, speckleElement2D[1].topology[2].basePoint.z);
      //displayMesh
      Assert.Equal(gsaEls[1].Colour.ToString(), speckleElement2D[1].colour);
      Assert.False(speckleElement2D[1].isDummy);
      Assert.Equal(gsaEls[1].Index.Value, speckleElement2D[1].nativeId);
      Assert.Equal(gsaEls[1].Group.Value, speckleElement2D[1].group);
    }

    [Fact]
    public void GsaElement1d()
    {
      //Define GSA objects
      //These should be in order that respects the type dependency tree (which is only available in the GSAProxy library, which isn't referenced yet
      var gsaRecords = new List<GsaRecord>();

      //Generation #1: Types with no other dependencies - the leaves of the tree
      gsaRecords.Add(GsaMatSteelExample("steel material 1"));
      gsaRecords.Add(GsaPropMassExample("property mass 1"));
      gsaRecords.Add(GsaPropSprExample("property spring 1"));

      //Gen #2
      var gsaNodes = GsaNodeExamples(3, "node 1", "node 2", "node 3");
      gsaRecords.AddRange(gsaNodes);
      var gsaSection = GsaCatalogueSectionExample("section 1");
      gsaRecords.Add(gsaSection);

      // Gen #3
      var gsaEls = GsaElement1dExamples(2, "element 1", "element 2");
      gsaRecords.AddRange(gsaEls);

      Instance.GsaModel.Cache.Upsert(gsaRecords);

      foreach (var record in gsaRecords)
      {
        var speckleObjects = converter.ConvertToSpeckle(new List<object> { record });
        Assert.Empty(converter.ConversionErrors);

        Instance.GsaModel.Cache.SetSpeckleObjects(record, speckleObjects.ToDictionary(so => so.applicationId, so => (object)so));
      }

      Assert.True(Instance.GsaModel.Cache.GetSpeckleObjects(out var structuralObjects));

      Assert.NotEmpty(structuralObjects);
      Assert.Contains(structuralObjects, so => so is GSAElement1D);

      var speckleElement1d = structuralObjects.FindAll(so => so is GSAElement1D).Select(so => (GSAElement1D)so).ToList();

      //Checks - Element 1
      Assert.Equal("element 1", speckleElement1d[0].applicationId);
      Assert.Equal("node 1", speckleElement1d[0].end1Node.applicationId); //assume conversion to Node is tested elsewhere
      Assert.Equal("node 2", speckleElement1d[0].end2Node.applicationId); //assume conversion to Node is tested elsewhere
      Assert.Equal("node 1", speckleElement1d[0].topology[0].applicationId);
      Assert.Equal("node 2", speckleElement1d[0].topology[1].applicationId);
      Assert.Equal(0, speckleElement1d[0].end1Offset.x);
      Assert.Equal(0, speckleElement1d[0].end1Offset.y);
      Assert.Equal(0, speckleElement1d[0].end1Offset.z);
      Assert.Equal(0, speckleElement1d[0].end2Offset.x);
      Assert.Equal(0, speckleElement1d[0].end2Offset.y);
      Assert.Equal(0, speckleElement1d[0].end2Offset.z);
      Assert.Equal("FFFFFF", speckleElement1d[0].end1Releases.code);
      Assert.Equal("FFFFFF", speckleElement1d[0].end2Releases.code);
      Assert.True(speckleElement1d[0].localAxis.IsGlobal());
      Assert.Equal(0, speckleElement1d[0].orientationAngle);
      Assert.Null(speckleElement1d[0].orientationNode);
      Assert.Equal("section 1", speckleElement1d[0].property.applicationId); //assume conversion to Property1d is tested elsewhere
      Assert.Equal(gsaEls[0].Colour.ToString(), speckleElement1d[0].colour);
      Assert.False(speckleElement1d[0].isDummy);
      Assert.Null(speckleElement1d[0].action);
      Assert.Equal(gsaEls[0].Index.Value, speckleElement1d[0].nativeId);
      Assert.Equal(gsaEls[0].Group.Value, speckleElement1d[0].group);

      //Checks - Element 2
      Assert.Equal("element 2", speckleElement1d[1].applicationId);
      Assert.Equal("node 2", speckleElement1d[1].end1Node.applicationId); //assume conversion to Node is tested elsewhere
      Assert.Equal("node 3", speckleElement1d[1].end2Node.applicationId); //assume conversion to Node is tested elsewhere
      Assert.Equal("node 2", speckleElement1d[1].topology[0].applicationId);
      Assert.Equal("node 3", speckleElement1d[1].topology[1].applicationId);
      Assert.Equal(0, speckleElement1d[1].end1Offset.x);
      Assert.Equal(0, speckleElement1d[1].end1Offset.y);
      Assert.Equal(0, speckleElement1d[1].end1Offset.z);
      Assert.Equal(0, speckleElement1d[1].end2Offset.x);
      Assert.Equal(0, speckleElement1d[1].end2Offset.y);
      Assert.Equal(0, speckleElement1d[1].end2Offset.z);
      Assert.Equal("FFFFFF", speckleElement1d[1].end1Releases.code);
      Assert.Equal("FFFFFF", speckleElement1d[1].end2Releases.code);
      Assert.Equal(1, speckleElement1d[1].localAxis.origin.x, 6);
      Assert.Equal(0, speckleElement1d[1].localAxis.origin.y, 6);
      Assert.Equal(0, speckleElement1d[1].localAxis.origin.z, 6);
      Assert.Equal(0, speckleElement1d[1].localAxis.xdir.x, 6);
      Assert.Equal(1, speckleElement1d[1].localAxis.xdir.y, 6);
      Assert.Equal(0, speckleElement1d[1].localAxis.xdir.z, 6);
      Assert.Equal(0, speckleElement1d[1].localAxis.ydir.x, 6);
      Assert.Equal(0, speckleElement1d[1].localAxis.ydir.y, 6);
      Assert.Equal(1, speckleElement1d[1].localAxis.ydir.z, 6);
      Assert.Equal(1, speckleElement1d[1].localAxis.normal.x, 6);
      Assert.Equal(0, speckleElement1d[1].localAxis.normal.y, 6);
      Assert.Equal(0, speckleElement1d[1].localAxis.normal.z, 6);
      Assert.Equal(90, speckleElement1d[1].orientationAngle);
      Assert.Null(speckleElement1d[1].orientationNode);
      Assert.Equal("section 1", speckleElement1d[1].property.applicationId); //assume conversion to Property1d is tested elsewhere
      Assert.Equal(gsaEls[1].Colour.ToString(), speckleElement1d[0].colour);
      Assert.False(speckleElement1d[1].isDummy);
      Assert.Null(speckleElement1d[1].action);
      Assert.Equal(gsaEls[1].Index.Value, speckleElement1d[1].nativeId);
      Assert.Equal(gsaEls[1].Group.Value, speckleElement1d[1].group);
    }

    [Fact]
    public void GsaAssembly()
    {
      //Define GSA objects
      //These should be in order that respects the type dependency tree (which is only available in the GSAProxy library, which isn't referenced yet
      var gsaRecords = new List<GsaRecord>();

      //Generation #1: Types with no other dependencies - the leaves of the tree
      gsaRecords.Add(GsaMatSteelExample("steel material 1"));
      gsaRecords.Add(GsaPropMassExample("property mass 1"));
      gsaRecords.Add(GsaPropSprExample("property spring 1"));

      //Gen #2
      gsaRecords.AddRange(GsaNodeExamples(5, "node 1", "node 2", "node 3", "node 4", "node 5"));
      gsaRecords.Add(GsaProp2dExample("section 1"));

      //Gen #3
      gsaRecords.AddRange(GsaElement2dExamples(2, "element 1", "element 2"));

      //Gen #4
      var gsaAssemblies = GsaAssemblyExamples(2, "assembly 1", "assembly 2");
      gsaRecords.AddRange(gsaAssemblies);

      Instance.GsaModel.Cache.Upsert(gsaRecords);

      foreach (var record in gsaRecords)
      {
        var speckleObjects = converter.ConvertToSpeckle(new List<object> { record });
        Assert.Empty(converter.ConversionErrors);

        Instance.GsaModel.Cache.SetSpeckleObjects(record, speckleObjects.ToDictionary(so => so.applicationId, so => (object)so));
      }

      Assert.True(Instance.GsaModel.Cache.GetSpeckleObjects(out var structuralObjects));

      Assert.NotEmpty(structuralObjects);
      Assert.Contains(structuralObjects, so => so is GSAAssembly);

      var speckleAssemblies = structuralObjects.FindAll(so => so is GSAAssembly).Select(so => (GSAAssembly)so).ToList();

      //Checks - Assembly 1
      Assert.Equal("assembly 1", speckleAssemblies[0].applicationId);
      Assert.Equal(gsaAssemblies[0].Index, speckleAssemblies[0].nativeId);
      Assert.Equal("node 1", speckleAssemblies[0].end1Node.applicationId);
      Assert.Equal("node 2", speckleAssemblies[0].end2Node.applicationId);
      Assert.Equal("node 3", speckleAssemblies[0].orientationNode.applicationId);
      Assert.Equal(2, speckleAssemblies[0].entities.Count());
      Assert.Equal("element 1", speckleAssemblies[0].entities[0].applicationId);
      Assert.Equal("element 2", speckleAssemblies[0].entities[1].applicationId);
      //TO DO: update once GSAAssembly is updated

      //Checks - Assembly 2
      Assert.Equal("assembly 2", speckleAssemblies[0].applicationId);
      Assert.Equal(gsaAssemblies[0].Index, speckleAssemblies[0].nativeId);
      Assert.Equal("node 2", speckleAssemblies[0].end1Node.applicationId);
      Assert.Equal("node 3", speckleAssemblies[0].end2Node.applicationId);
      Assert.Equal("node 4", speckleAssemblies[0].orientationNode.applicationId);
      Assert.Single(speckleAssemblies[0].entities);
      Assert.Equal("element 1", speckleAssemblies[0].entities[0].applicationId);
    }

    [Fact]
    public void GsaMemb()
    {
      //Define GSA objects
      //These should be in order that respects the type dependency tree (which is only available in the GSAProxy library, which isn't referenced yet
      var gsaRecords = new List<GsaRecord>();

      //Generation #1: Types with no other dependencies - the leaves of the tree
      gsaRecords.Add(GsaMatSteelExample("steel material 1"));
      gsaRecords.Add(GsaPropMassExample("property mass 1"));
      gsaRecords.Add(GsaPropSprExample("property spring 1"));

      //Gen #2
      gsaRecords.AddRange(GsaNodeExamples(5, "node 1", "node 2", "node 3", "node 4"));
      gsaRecords.Add(GsaProp2dExample("prop 2D 1"));
      gsaRecords.Add(GsaCatalogueSectionExample("section 1"));

      //Gen #3
      var gsaMembers = GsaMemberExamples(2, "member 1", "member 2");
      gsaRecords.AddRange(gsaMembers);

      Instance.GsaModel.Cache.Upsert(gsaRecords);

      foreach (var record in gsaRecords)
      {
        var speckleObjects = converter.ConvertToSpeckle(new List<object> { record });
        Assert.Empty(converter.ConversionErrors);

        Instance.GsaModel.Cache.SetSpeckleObjects(record, speckleObjects.ToDictionary(so => so.applicationId, so => (object)so));
      }

      Assert.True(Instance.GsaModel.Cache.GetSpeckleObjects(out var structuralObjects));

      Assert.NotEmpty(structuralObjects);
      Assert.Contains(structuralObjects, so => so is GSAMember1D);
      Assert.Contains(structuralObjects, so => so is GSAMember2D);

      var speckleMembers1D = structuralObjects.FindAll(so => so is GSAMember1D).Select(so => (GSAMember1D)so).ToList();
      var speckleMembers2D = structuralObjects.FindAll(so => so is GSAMember2D).Select(so => (GSAMember2D)so).ToList();

      //Checks - Member 1
      Assert.Equal("member 1", speckleMembers1D[0].applicationId);

      //Checks - Member 2
      Assert.Equal("member 2", speckleMembers2D[0].applicationId);
    }

    #endregion

    #region Loading
    [Fact]
    public void GsaLoadCase()
    {
      //Define GSA objects
      //These should be in order that respects the type dependency tree (which is only available in the GSAProxy library, which isn't referenced yet
      var gsaRecords = new List<GsaRecord>();

      //Generation #1: Types with no other dependencies - the leaves of the tree
      var gsaLoadCase = GsaLoadCaseExamples(2, "load case 1", "load case 2");;
      gsaRecords.AddRange(gsaLoadCase);

      Instance.GsaModel.Cache.Upsert(gsaRecords);

      foreach (var record in gsaRecords)
      {
        var speckleObjects = converter.ConvertToSpeckle(new List<object> { record });
        Assert.Empty(converter.ConversionErrors);

        Instance.GsaModel.Cache.SetSpeckleObjects(record, speckleObjects.ToDictionary(so => so.applicationId, so => (object)so));
      }

      Assert.True(Instance.GsaModel.Cache.GetSpeckleObjects(out var structuralObjects));

      Assert.NotEmpty(structuralObjects);
      Assert.Contains(structuralObjects, so => so is GSALoadCase);

      var speckleLoadCases = structuralObjects.FindAll(so => so is GSALoadCase).Select(so => (GSALoadCase)so).ToList();

      //Checks - Load case 1
      Assert.Equal("load case 1", speckleLoadCases[0].applicationId);
      Assert.Equal("Dead", speckleLoadCases[0].name);
      Assert.Equal("", speckleLoadCases[0].source);
      Assert.Equal(ActionType.None, speckleLoadCases[0].actionType);
      Assert.Equal("", speckleLoadCases[0].description);
      Assert.Equal(gsaLoadCase[0].Index.Value, speckleLoadCases[0].nativeId);

      //Checks - Load case 2
      Assert.Equal("load case 2", speckleLoadCases[1].applicationId);
      Assert.Equal("Live", speckleLoadCases[1].name);
      Assert.Equal("", speckleLoadCases[1].source);
      Assert.Equal(ActionType.None, speckleLoadCases[1].actionType);
      Assert.Equal("", speckleLoadCases[1].description);
      Assert.Equal(gsaLoadCase[1].Index.Value, speckleLoadCases[1].nativeId);
    }

    [Fact]
    public void GsaFaceLoad()
    {
      //Define GSA objects
      //These should be in order that respects the type dependency tree (which is only available in the GSAProxy library, which isn't referenced yet
      var gsaRecords = new List<GsaRecord>();

      //Generation #1: Types with no other dependencies - the leaves of the tree
      gsaRecords.Add(GsaMatSteelExample("steel material 1"));
      gsaRecords.Add(GsaPropMassExample("property mass 1"));
      gsaRecords.Add(GsaPropSprExample("property spring 1"));
      gsaRecords.Add(GsaAxisExample("axis 1"));
      gsaRecords.Add(GsaLoadCaseExamples(1, "load case 1").First());

      //Gen #2
      gsaRecords.AddRange(GsaNodeExamples(6, "node 1", "node 2", "node 3", "node 4", "node 5", "node 6"));
      gsaRecords.Add(GsaProp2dExample("property 2D 1"));

      // Gen #3
      gsaRecords.AddRange(GsaElement2dExamples(3, "element 1", "element 2", "element 3"));

      // Gen #4
      var gsaLoad2dFace = GsaLoad2dFaceExamples(3, "load 2d face 1", "load 2d face 2", "load 2d face 3");
      gsaRecords.AddRange(gsaLoad2dFace);

      Instance.GsaModel.Cache.Upsert(gsaRecords);

      foreach (var record in gsaRecords)
      {
        var speckleObjects = converter.ConvertToSpeckle(new List<object> { record });
        Assert.Empty(converter.ConversionErrors);

        Instance.GsaModel.Cache.SetSpeckleObjects(record, speckleObjects.ToDictionary(so => so.applicationId, so => (object)so));
      }

      Assert.True(Instance.GsaModel.Cache.GetSpeckleObjects(out var structuralObjects));

      Assert.NotEmpty(structuralObjects);
      Assert.Contains(structuralObjects, so => so is GSAFaceLoad);

      var speckleFaceLoads = structuralObjects.FindAll(so => so is GSAFaceLoad).Select(so => (GSAFaceLoad)so).ToList();

      //Checks - Load 1
      Assert.Equal("load 2d face 1", speckleFaceLoads[0].applicationId);
      Assert.Equal("1", speckleFaceLoads[0].name);
      Assert.Single(speckleFaceLoads[0].elements);
      Assert.Equal("element 1", speckleFaceLoads[0].elements[0].applicationId);
      Assert.Equal(AreaLoadType.Constant, speckleFaceLoads[0].loadType);
      Assert.Equal(LoadDirection.Z, speckleFaceLoads[0].direction);
      Assert.Equal(LoadAxisType.Global, speckleFaceLoads[0].loadAxisType);
      Assert.False(speckleFaceLoads[0].isProjected);
      Assert.Equal(gsaLoad2dFace[0].Values, speckleFaceLoads[0].values);
      Assert.Null(speckleFaceLoads[0].loadAxis);
      Assert.Null(speckleFaceLoads[0].positions);
      Assert.Equal(gsaLoad2dFace[0].Index.Value, speckleFaceLoads[0].nativeId);

      //Checks - Load 2
      Assert.Equal("load 2d face 2", speckleFaceLoads[1].applicationId);
      Assert.Equal("2", speckleFaceLoads[1].name);
      Assert.Single(speckleFaceLoads[1].elements);
      Assert.Equal("element 2", speckleFaceLoads[1].elements[0].applicationId);
      Assert.Equal(AreaLoadType.Point, speckleFaceLoads[1].loadType);
      Assert.Equal(LoadDirection.X, speckleFaceLoads[1].direction);
      Assert.Equal(LoadAxisType.Global, speckleFaceLoads[1].loadAxisType);
      Assert.False(speckleFaceLoads[1].isProjected);
      Assert.Equal(gsaLoad2dFace[1].Values, speckleFaceLoads[1].values);
      Assert.Equal("axis 1", speckleFaceLoads[1].loadAxis.applicationId);
      Assert.Equal(new List<double>() { 0, 0 }, speckleFaceLoads[1].positions);
      Assert.Equal(gsaLoad2dFace[1].Index.Value, speckleFaceLoads[1].nativeId);

      //Checks - Load 3
      Assert.Equal("load 2d face 3", speckleFaceLoads[2].applicationId);
      Assert.Equal("3", speckleFaceLoads[2].name);
      Assert.Single(speckleFaceLoads[2].elements);
      Assert.Equal("element 3", speckleFaceLoads[2].elements[0].applicationId);
      Assert.Equal(AreaLoadType.Variable, speckleFaceLoads[2].loadType);
      Assert.Equal(LoadDirection.Y, speckleFaceLoads[2].direction);
      Assert.Equal(LoadAxisType.Local, speckleFaceLoads[2].loadAxisType);
      Assert.False(speckleFaceLoads[2].isProjected);
      Assert.Equal(gsaLoad2dFace[2].Values, speckleFaceLoads[2].values);
      Assert.Null(speckleFaceLoads[2].loadAxis);
      Assert.Null(speckleFaceLoads[2].positions);
      Assert.Equal(gsaLoad2dFace[2].Index.Value, speckleFaceLoads[2].nativeId);
    }

    [Fact]
    public void GsaBeamLoad()
    {
      //Define GSA objects
      //These should be in order that respects the type dependency tree (which is only available in the GSAProxy library, which isn't referenced yet
      var gsaRecords = new List<GsaRecord>();

      //Generation #1: Types with no other dependencies - the leaves of the tree
      gsaRecords.Add(GsaMatSteelExample("steel material 1"));
      gsaRecords.Add(GsaPropMassExample("property mass 1"));
      gsaRecords.Add(GsaPropSprExample("property spring 1"));
      gsaRecords.Add(GsaLoadCaseExamples(1, "load case 1").First());
      gsaRecords.Add(GsaAxisExample("axis 1"));

      //Gen #2
      gsaRecords.AddRange(GsaNodeExamples(3, "node 1", "node 2", "node 3"));
      gsaRecords.Add(GsaCatalogueSectionExample("section 1"));

      //Gen #3
      gsaRecords.AddRange(GsaElement1dExamples(2, "element 1", "element 2"));

      //Gen #4
      var gsaLoadBeams = GsaLoadBeamExamples(3, "load beam 1", "load beam 2", "load beam 3");
      gsaRecords.AddRange(gsaLoadBeams);

      Instance.GsaModel.Cache.Upsert(gsaRecords);

      foreach (var record in gsaRecords)
      {
        var speckleObjects = converter.ConvertToSpeckle(new List<object> { record });
        Assert.Empty(converter.ConversionErrors);

        Instance.GsaModel.Cache.SetSpeckleObjects(record, speckleObjects.ToDictionary(so => so.applicationId, so => (object)so));
      }

      Assert.True(Instance.GsaModel.Cache.GetSpeckleObjects(out var structuralObjects));

      Assert.NotEmpty(structuralObjects);
      Assert.Contains(structuralObjects, so => so is GSABeamLoad);

      var speckleBeamLoads = structuralObjects.FindAll(so => so is GSABeamLoad).Select(so => (GSABeamLoad)so).ToList();

      //Checks - Element 1
      Assert.Equal("load beam 1", speckleBeamLoads[0].applicationId);
      Assert.Equal("1", speckleBeamLoads[0].name);
      Assert.Equal("load case 1", speckleBeamLoads[0].loadCase.applicationId);
      Assert.Single(speckleBeamLoads[0].elements);
      Assert.Equal("element 1", speckleBeamLoads[0].elements[0].applicationId);
      Assert.Equal(BeamLoadType.Point, speckleBeamLoads[0].loadType);
      Assert.Equal(LoadDirection.Z, speckleBeamLoads[0].direction);
      Assert.Null(speckleBeamLoads[0].loadAxis);
      Assert.Equal(LoadAxisType.Global, speckleBeamLoads[0].loadAxisType);
      Assert.False(speckleBeamLoads[0].isProjected);
      Assert.Single(speckleBeamLoads[0].values);
      Assert.Equal(((GsaLoadBeamPoint)gsaLoadBeams[0]).Load, speckleBeamLoads[0].values[0]);
      Assert.Single(speckleBeamLoads[0].positions);
      Assert.Equal(((GsaLoadBeamPoint)gsaLoadBeams[0]).Position, speckleBeamLoads[0].positions[0]);
      Assert.Equal(gsaLoadBeams[0].Index.Value, speckleBeamLoads[0].nativeId);

      //Checks - Element 2
      Assert.Equal("load beam 2", speckleBeamLoads[1].applicationId);
      Assert.Equal("2", speckleBeamLoads[1].name);
      Assert.Equal("load case 1", speckleBeamLoads[1].loadCase.applicationId);
      Assert.Single(speckleBeamLoads[1].elements);
      Assert.Equal("element 2", speckleBeamLoads[1].elements[0].applicationId);
      Assert.Equal(BeamLoadType.Uniform, speckleBeamLoads[1].loadType);
      Assert.Equal(LoadDirection.X, speckleBeamLoads[1].direction);
      Assert.Equal("axis 1", speckleBeamLoads[1].loadAxis.applicationId);
      Assert.Equal(LoadAxisType.Global, speckleBeamLoads[1].loadAxisType);
      Assert.False(speckleBeamLoads[1].isProjected);
      Assert.Single(speckleBeamLoads[1].values);
      Assert.Equal(((GsaLoadBeamUdl)gsaLoadBeams[1]).Load, speckleBeamLoads[1].values[0]);
      Assert.Null(speckleBeamLoads[1].positions);
      Assert.Equal(gsaLoadBeams[1].Index.Value, speckleBeamLoads[1].nativeId);

      //Checks - Element 3
      Assert.Equal("load beam 3", speckleBeamLoads[2].applicationId);
      Assert.Equal("3", speckleBeamLoads[2].name);
      Assert.Equal("load case 1", speckleBeamLoads[2].loadCase.applicationId);
      Assert.Single(speckleBeamLoads[2].elements);
      Assert.Equal("element 1", speckleBeamLoads[2].elements[0].applicationId);
      Assert.Equal(BeamLoadType.Linear, speckleBeamLoads[2].loadType);
      Assert.Equal(LoadDirection.Y, speckleBeamLoads[2].direction);
      Assert.Null(speckleBeamLoads[2].loadAxis);
      Assert.Equal(LoadAxisType.Local, speckleBeamLoads[2].loadAxisType);
      Assert.False(speckleBeamLoads[2].isProjected);
      Assert.Equal(2, speckleBeamLoads[2].values.Count());
      Assert.Equal(((GsaLoadBeamLine)gsaLoadBeams[2]).Load1, speckleBeamLoads[2].values[0]);
      Assert.Equal(((GsaLoadBeamLine)gsaLoadBeams[2]).Load2, speckleBeamLoads[2].values[1]);
      Assert.Null(speckleBeamLoads[2].positions);
      Assert.Equal(gsaLoadBeams[2].Index.Value, speckleBeamLoads[2].nativeId);
    }

    [Fact]
    public void GsaNodeLoad()
    {
      //Define GSA objects
      //These should be in order that respects the type dependency tree (which is only available in the GSAProxy library, which isn't referenced yet
      var gsaRecords = new List<GsaRecord>();

      //Generation #1: Types with no other dependencies - the leaves of the tree
      gsaRecords.Add(GsaAxisExample("axis 1"));
      gsaRecords.Add(GsaPropMassExample("property mass 1"));
      gsaRecords.Add(GsaPropSprExample("property spring 1"));
      gsaRecords.Add(GsaLoadCaseExamples(1, "load case 1").First());

      //Gen #2
      gsaRecords.Add(GsaNodeExamples(1, "node 1").First());

      //Gen #3
      var gsaLoadNodess = GsaLoadNodeExamples(2, "load node 1", "load node 2");
      gsaRecords.AddRange(gsaLoadNodess);

      Instance.GsaModel.Cache.Upsert(gsaRecords);

      foreach (var record in gsaRecords)
      {
        var speckleObjects = converter.ConvertToSpeckle(new List<object> { record });
        Assert.Empty(converter.ConversionErrors);

        Instance.GsaModel.Cache.SetSpeckleObjects(record, speckleObjects.ToDictionary(so => so.applicationId, so => (object)so));
      }

      Assert.True(Instance.GsaModel.Cache.GetSpeckleObjects(out var structuralObjects));

      Assert.NotEmpty(structuralObjects);
      Assert.Contains(structuralObjects, so => so is GSANodeLoad);

      var speckleNodeLoads = structuralObjects.FindAll(so => so is GSANodeLoad).Select(so => (GSANodeLoad)so).ToList();

      //Checks - Load 1
      Assert.Equal("load node 1", speckleNodeLoads[0].applicationId);
      Assert.Equal(gsaLoadNodess[0].Name, speckleNodeLoads[0].name);
      Assert.Equal("load case 1", speckleNodeLoads[0].loadCase.applicationId);  //assume conversion of load case is tested elsewhere
      Assert.Single(speckleNodeLoads[0].nodes);
      Assert.Equal("node 1", speckleNodeLoads[0].nodes[0].applicationId); //assume conversion of node is tested elsewhere
      Assert.True(speckleNodeLoads[0].loadAxis.definition.IsGlobal());
      Assert.Equal(LoadDirection.Z, speckleNodeLoads[0].direction);
      Assert.Equal(gsaLoadNodess[0].Value, speckleNodeLoads[0].value);
      Assert.Equal(gsaLoadNodess[0].Index.Value, speckleNodeLoads[0].nativeId);

      //Checks - Load 2
      Assert.Equal("load node 2", speckleNodeLoads[1].applicationId);
      Assert.Equal(gsaLoadNodess[1].Name, speckleNodeLoads[1].name);
      Assert.Equal("load case 1", speckleNodeLoads[1].loadCase.applicationId);  //assume conversion of load case is tested elsewhere
      Assert.Single(speckleNodeLoads[1].nodes);
      Assert.Equal("node 1", speckleNodeLoads[1].nodes[0].applicationId); //assume conversion of node is tested elsewhere
      Assert.Equal("axis 1", speckleNodeLoads[1].loadAxis.applicationId); //assume conversion of axis is tested elsewhere
      Assert.Equal(LoadDirection.X, speckleNodeLoads[1].direction);
      Assert.Equal(gsaLoadNodess[1].Value, speckleNodeLoads[1].value);
      Assert.Equal(gsaLoadNodess[1].Index.Value, speckleNodeLoads[1].nativeId);
    }

    [Fact]
    public void GsaGravityLoad()
    {
      //Define GSA objects
      //These should be in order that respects the type dependency tree (which is only available in the GSAProxy library, which isn't referenced yet
      var gsaRecords = new List<GsaRecord>();

      //Generation #1: Types with no other dependencies - the leaves of the tree
      gsaRecords.Add(GsaMatSteelExample("steel material 1"));
      gsaRecords.Add(GsaPropMassExample("property mass 1"));
      gsaRecords.Add(GsaPropSprExample("property spring 1"));
      gsaRecords.Add(GsaLoadCaseExamples(1, "load case 1").First());

      //Gen #2
      gsaRecords.AddRange(GsaNodeExamples(5, "node 1", "node 2", "node 3", "node 4", "node 5"));
      gsaRecords.Add(GsaProp2dExample("property 2D 1"));

      //Gen #3
      gsaRecords.AddRange(GsaElement2dExamples(2, "element 1", "element 2"));

      //Gen #4
      var gsaLoadGravity = GsaLoadGravityExample("load gravity 1");
      gsaRecords.Add(gsaLoadGravity);

      Instance.GsaModel.Cache.Upsert(gsaRecords);

      foreach (var record in gsaRecords)
      {
        var speckleObjects = converter.ConvertToSpeckle(new List<object> { record });
        Assert.Empty(converter.ConversionErrors);

        Instance.GsaModel.Cache.SetSpeckleObjects(record, speckleObjects.ToDictionary(so => so.applicationId, so => (object)so));
      }

      Assert.True(Instance.GsaModel.Cache.GetSpeckleObjects(out var structuralObjects));

      Assert.NotEmpty(structuralObjects);
      Assert.Contains(structuralObjects, so => so is GSAGravityLoad);

      var speckleGravityLoad = structuralObjects.FindAll(so => so is GSAGravityLoad).Select(so => (GSAGravityLoad)so).ToList().First();

      //Checks
      Assert.Equal("load gravity 1", speckleGravityLoad.applicationId);
      Assert.Equal("load case 1", speckleGravityLoad.loadCase.applicationId);
      Assert.Equal(2, speckleGravityLoad.elements.Count());
      Assert.Equal("element 1", speckleGravityLoad.elements[0].applicationId);
      Assert.Equal("element 2", speckleGravityLoad.elements[1].applicationId);
      Assert.Equal(5, speckleGravityLoad.nodes.Count());
      Assert.Equal("node 1", speckleGravityLoad.nodes[0].applicationId);
      Assert.Equal("node 2", speckleGravityLoad.nodes[1].applicationId);
      Assert.Equal("node 3", speckleGravityLoad.nodes[2].applicationId);
      Assert.Equal("node 4", speckleGravityLoad.nodes[3].applicationId);
      Assert.Equal("node 5", speckleGravityLoad.nodes[4].applicationId);
      Assert.Equal(0, speckleGravityLoad.gravityFactors.x);
      Assert.Equal(0, speckleGravityLoad.gravityFactors.y);
      Assert.Equal(-1, speckleGravityLoad.gravityFactors.z);
      Assert.Equal(gsaLoadGravity.Index.Value, speckleGravityLoad.nativeId);
    }

    [Fact]
    public void GsaCombination()
    {
      //Define GSA objects
      //These should be in order that respects the type dependency tree (which is only available in the GSAProxy library, which isn't referenced yet
      var gsaRecords = new List<GsaRecord>();

      //Generation #1: Types with no other dependencies - the leaves of the tree
      gsaRecords.AddRange(GsaLoadCaseExamples(2, "load case 1", "load case 2"));

      //Gen #2
      var gsaCombinations = GsaCombinationExamples(2, "combo 1", "combo 2");
      gsaRecords.AddRange(gsaCombinations);

      Instance.GsaModel.Cache.Upsert(gsaRecords);

      foreach (var record in gsaRecords)
      {
        var speckleObjects = converter.ConvertToSpeckle(new List<object> { record });
        Assert.Empty(converter.ConversionErrors);

        Instance.GsaModel.Cache.SetSpeckleObjects(record, speckleObjects.ToDictionary(so => so.applicationId, so => (object)so));
      }

      Assert.True(Instance.GsaModel.Cache.GetSpeckleObjects(out var structuralObjects));

      Assert.NotEmpty(structuralObjects);
      Assert.Contains(structuralObjects, so => so is GSALoadCombination);

      var speckleLoadCombination = structuralObjects.FindAll(so => so is GSALoadCombination).Select(so => (GSALoadCombination)so).ToList();

      //Checks - Combination 1
      Assert.Equal("combo 1", speckleLoadCombination[0].applicationId);
      Assert.Equal(gsaCombinations[0].Name, speckleLoadCombination[0].name);
      Assert.Equal(new Dictionary<string, double> { { "Dead", 1 }, { "Live", 0.2 } }, speckleLoadCombination[0].caseFactors);
      Assert.Equal(CombinationType.LinearAdd, speckleLoadCombination[0].combinationType);
      Assert.Equal(gsaCombinations[0].Index.Value, speckleLoadCombination[0].nativeId);

      //Checks - Combination 2
      Assert.Equal("combo 2", speckleLoadCombination[1].applicationId);
      Assert.Equal(gsaCombinations[1].Name, speckleLoadCombination[1].name);
      Assert.Equal(new Dictionary<string, double> { { "Combination 1", 1.3 }, { "Dead", 1 }, { "Live", 1 } }, speckleLoadCombination[1].caseFactors);
      Assert.Equal(CombinationType.LinearAdd, speckleLoadCombination[1].combinationType);
      Assert.Equal(gsaCombinations[1].Index.Value, speckleLoadCombination[1].nativeId);
    }
    #endregion

    #region Materials
    [Fact]
    public void GsaMatConcrete()
    {
      //Define GSA objects
      //These should be in order that respects the type dependency tree (which is only available in the GSAProxy library, which isn't referenced yet
      var gsaRecords = new List<GsaRecord>();

      //Generation #1: Types with no other dependencies - the leaves of the tree
      var gsaMatConcrete = GsaMatConcreteExample("concrete 1");
      gsaRecords.Add(gsaMatConcrete);

      Instance.GsaModel.Cache.Upsert(gsaRecords);

      foreach (var record in gsaRecords)
      {
        var speckleObjects = converter.ConvertToSpeckle(new List<object> { record });
        Assert.Empty(converter.ConversionErrors);

        Instance.GsaModel.Cache.SetSpeckleObjects(record, speckleObjects.ToDictionary(so => so.applicationId, so => (object)so));
      }

      Assert.True(Instance.GsaModel.Cache.GetSpeckleObjects(out var structuralObjects));

      Assert.NotEmpty(structuralObjects);
      Assert.Contains(structuralObjects, so => so is Concrete);

      var speckleConcrete = (Concrete)structuralObjects.FirstOrDefault(so => so is Concrete);

      Assert.Equal("concrete 1", speckleConcrete.applicationId);
      Assert.Equal("", speckleConcrete.grade);
      Assert.Equal(MaterialType.Concrete, speckleConcrete.type);
      Assert.Equal("", speckleConcrete.designCode);
      Assert.Equal("", speckleConcrete.codeYear);
      Assert.Equal(gsaMatConcrete.Fc.Value, speckleConcrete.compressiveStrength);
      Assert.Equal(gsaMatConcrete.Mat.Rho.Value, speckleConcrete.density);
      Assert.Equal(gsaMatConcrete.Mat.E.Value, speckleConcrete.youngsModulus);
      Assert.Equal(gsaMatConcrete.Mat.G.Value, speckleConcrete.shearModulus);
      Assert.Equal(gsaMatConcrete.Mat.Nu.Value, speckleConcrete.poissonsRatio);
      Assert.Equal(gsaMatConcrete.Mat.Alpha.Value, speckleConcrete.thermalExpansivity);
      Assert.Equal(gsaMatConcrete.EpsU.Value, speckleConcrete.maxStrain);
      Assert.Equal(gsaMatConcrete.Agg.Value, speckleConcrete.maxAggregateSize);
      Assert.Equal(gsaMatConcrete.Fcdt.Value, speckleConcrete.tensileStrength);
      Assert.Equal(0, speckleConcrete.flexuralStrength);
    }

    [Fact]
    public void GsaMatSteel()
    {
      //Define GSA objects
      //These should be in order that respects the type dependency tree (which is only available in the GSAProxy library, which isn't referenced yet
      var gsaRecords = new List<GsaRecord>();

      //Generation #1: Types with no other dependencies - the leaves of the tree
      var gsaMatSteel = GsaMatSteelExample("steel 1");
      gsaRecords.Add(gsaMatSteel);

      Instance.GsaModel.Cache.Upsert(gsaRecords);

      foreach (var record in gsaRecords)
      {
        var speckleObjects = converter.ConvertToSpeckle(new List<object> { record });
        Assert.Empty(converter.ConversionErrors);

        Instance.GsaModel.Cache.SetSpeckleObjects(record, speckleObjects.ToDictionary(so => so.applicationId, so => (object)so));
      }

      Assert.True(Instance.GsaModel.Cache.GetSpeckleObjects(out var structuralObjects));

      Assert.NotEmpty(structuralObjects);
      Assert.Contains(structuralObjects, so => so is Steel);

      var speckleSteel = (Steel)structuralObjects.FirstOrDefault(so => so is Steel);

      Assert.Equal("steel 1", speckleSteel.applicationId);
      Assert.Equal("", speckleSteel.grade);
      Assert.Equal(MaterialType.Steel, speckleSteel.type);
      Assert.Equal("", speckleSteel.designCode);
      Assert.Equal("", speckleSteel.codeYear);
      Assert.Equal(gsaMatSteel.Fy.Value, speckleSteel.yieldStrength);
      Assert.Equal(gsaMatSteel.Fu.Value, speckleSteel.ultimateStrength);
      Assert.Equal(gsaMatSteel.EpsP.Value, speckleSteel.maxStrain);
      Assert.Equal(gsaMatSteel.Mat.E.Value, speckleSteel.youngsModulus);
      Assert.Equal(gsaMatSteel.Mat.Nu.Value, speckleSteel.poissonsRatio);
      Assert.Equal(gsaMatSteel.Mat.G.Value, speckleSteel.shearModulus);
      Assert.Equal(gsaMatSteel.Mat.Rho.Value, speckleSteel.density);
      Assert.Equal(gsaMatSteel.Mat.Alpha.Value, speckleSteel.thermalExpansivity);
    }

    //Timber not yet supported
    #endregion

    #region Property
    [Fact]
    public void GsaProperty1D()
    {
      //Define GSA objects
      //These should be in order that respects the type dependency tree (which is only available in the GSAProxy library, which isn't referenced yet
      var gsaRecords = new List<GsaRecord>();

      //Generation #1: Types with no other dependencies - the leaves of the tree
      gsaRecords.Add(GsaMatSteelExample("steel material 1"));

      //Gen #2
      var gsaSection = GsaCatalogueSectionExample("section 1");
      gsaRecords.Add(gsaSection);

      Instance.GsaModel.Cache.Upsert(gsaRecords);

      foreach (var record in gsaRecords)
      {
        var speckleObjects = converter.ConvertToSpeckle(new List<object> { record });
        Assert.Empty(converter.ConversionErrors);

        Instance.GsaModel.Cache.SetSpeckleObjects(record, speckleObjects.ToDictionary(so => so.applicationId, so => (object)so));
      }

      Assert.True(Instance.GsaModel.Cache.GetSpeckleObjects(out var structuralObjects));

      Assert.NotEmpty(structuralObjects);
      Assert.Contains(structuralObjects, so => so is GSAProperty1D);

      var speckleProperty1D = (GSAProperty1D)structuralObjects.FirstOrDefault(so => so is GSAProperty1D);

      //Checks
      Assert.Equal("section 1", speckleProperty1D.applicationId);
      Assert.Equal(gsaSection.Colour.ToString(), speckleProperty1D.colour);
      Assert.Equal(MemberType.Generic1D, speckleProperty1D.memberType);
      Assert.Equal("steel material 1", speckleProperty1D.material.applicationId); //assume tests are done elsewhere
      Assert.Equal("", speckleProperty1D.grade);
      Assert.Equal(((ProfileDetailsCatalogue)((SectionComp)gsaSection.Components[0]).ProfileDetails).Profile, speckleProperty1D.profile.shapeDescription);
      Assert.Equal(BaseReferencePoint.Centroid, speckleProperty1D.referencePoint);
      Assert.Equal(0, speckleProperty1D.offsetY);
      Assert.Equal(0, speckleProperty1D.offsetZ);
      Assert.Equal(0, speckleProperty1D.area);
      Assert.Equal(0, speckleProperty1D.Iyy);
      Assert.Equal(0, speckleProperty1D.Izz);
      Assert.Equal(0, speckleProperty1D.J);
      Assert.Equal(0, speckleProperty1D.Ky);
      Assert.Equal(0, speckleProperty1D.Kz);
      Assert.Equal(gsaSection.Index.Value, speckleProperty1D.nativeId);
      Assert.Equal(gsaSection.Mass.Value, speckleProperty1D.additionalMass); 
      Assert.Equal(gsaSection.Cost.Value, speckleProperty1D.cost);
      Assert.Equal(0, speckleProperty1D.poolRef);

      //TO DO: update once modifiers have been included in the section definition
    }

    [Fact]
    public void GsaProperty2D()
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

      var speckleProperty2D = (GSAProperty2D)structuralObjects.FirstOrDefault(so => so is GSAProperty2D);

      //Checks
      Assert.Equal("property 2D 1", speckleProperty2D.applicationId);
      Assert.Equal(gsaProp2d.Name, speckleProperty2D.name);
      Assert.Equal(gsaProp2d.Colour.ToString(), speckleProperty2D.colour);
      Assert.Equal(gsaProp2d.Thickness.Value, speckleProperty2D.thickness);
      Assert.Equal("steel material 1", speckleProperty2D.material.applicationId); //assume conversion of material is covered by another test
      Assert.Equal("", speckleProperty2D.grade);
      Assert.Null(speckleProperty2D.orientationAxis.applicationId); //no application ID for global coordinate system
      Assert.Equal(PropertyType2D.Shell, speckleProperty2D.type);
      Assert.Equal(ReferenceSurface.Middle, speckleProperty2D.refSurface);
      Assert.Equal(gsaProp2d.RefZ, speckleProperty2D.zOffset);
      Assert.Equal(gsaProp2d.InPlaneStiffnessPercentage.Value, speckleProperty2D.modifierInPlane);  //Check modifiers (currently no way to distinguish between value and percentage in speckle object)
      Assert.Equal(gsaProp2d.BendingStiffnessPercentage.Value, speckleProperty2D.modifierBending);
      Assert.Equal(gsaProp2d.ShearStiffnessPercentage.Value, speckleProperty2D.modifierShear);
      Assert.Equal(gsaProp2d.VolumePercentage.Value, speckleProperty2D.modifierVolume);
      Assert.Equal(gsaProp2d.Mass, speckleProperty2D.additionalMass);
      Assert.Equal(gsaProp2d.Profile, speckleProperty2D.concreteSlabProp);
      Assert.Equal(gsaProp2d.Index.Value, speckleProperty2D.nativeId);
      Assert.Null(speckleProperty2D.designMaterial);
      Assert.Equal(0, speckleProperty2D.cost);
    }

    //Property3D not yet supported

    [Fact]
    public void GsaPropMass()
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

      Assert.Equal("property mass 1", specklePropertyMass.applicationId);
      Assert.Equal(gsaPropMass.Name, specklePropertyMass.name);
      Assert.Equal(gsaPropMass.Mass, specklePropertyMass.mass);
      Assert.Equal(gsaPropMass.Ixx, specklePropertyMass.inertiaXX);
      Assert.Equal(gsaPropMass.Iyy, specklePropertyMass.inertiaYY);
      Assert.Equal(gsaPropMass.Izz, specklePropertyMass.inertiaZZ);
      Assert.Equal(gsaPropMass.Ixy, specklePropertyMass.inertiaXY);
      Assert.Equal(gsaPropMass.Iyz, specklePropertyMass.inertiaYZ);
      Assert.Equal(gsaPropMass.Izx, specklePropertyMass.inertiaZX);
      Assert.True(specklePropertyMass.massModified);
      Assert.Equal(gsaPropMass.ModXPercentage, specklePropertyMass.massModifierX);
      Assert.Equal(gsaPropMass.ModYPercentage, specklePropertyMass.massModifierY);
      Assert.Equal(gsaPropMass.ModZPercentage, specklePropertyMass.massModifierZ);
    }

    [Fact]
    public void GsaPropSpr()
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

      Assert.Equal("property spring 1", specklePropertySpring.applicationId);
      Assert.Equal(gsaProSpr.Name, specklePropertySpring.name);
      Assert.Equal(gsaProSpr.PropertyType.ToString(), specklePropertySpring.springType.ToString());
      Assert.Equal(gsaProSpr.Stiffnesses[AxisDirection6.X], specklePropertySpring.stiffnessX);
      Assert.Equal(gsaProSpr.Stiffnesses[AxisDirection6.Y], specklePropertySpring.stiffnessY);
      Assert.Equal(gsaProSpr.Stiffnesses[AxisDirection6.Z], specklePropertySpring.stiffnessZ);
      Assert.Equal(gsaProSpr.Stiffnesses[AxisDirection6.XX], specklePropertySpring.stiffnessXX);
      Assert.Equal(gsaProSpr.Stiffnesses[AxisDirection6.YY], specklePropertySpring.stiffnessYY);
      Assert.Equal(gsaProSpr.Stiffnesses[AxisDirection6.ZZ], specklePropertySpring.stiffnessZZ);
      Assert.Equal(gsaProSpr.DampingRatio, specklePropertySpring.dampingRatio);
    }
    #endregion

    #region Results
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
    #endregion

    #region helper
    #region Geometry
    private GsaAxis GsaAxisExample(string appId)
    {
      return new GsaAxis()
      {
        Index = 1,
        ApplicationId = appId,
        Name = "1",
        OriginX = 0,
        OriginY = 0,
        OriginZ = 0,
        XDirX = 1 / Math.Sqrt(2),
        XDirY = 1 / Math.Sqrt(2),
        XDirZ = 0,
        XYDirX = -1 / Math.Sqrt(2),
        XYDirY = 1 / Math.Sqrt(2),
        XYDirZ = 0
      };
    }

    private List<GsaEl> GsaElement2dExamples(int numberOfElements, params string[] appIds)
    {
      var gsaEl = new List<GsaEl> {
        new GsaEl()
        {
          Index = 1,
          Name = "1",
          Colour = Colour.NO_RGB,
          Type = ElementType.Quad4,
          PropertyIndex = 1,
          Group = 1,
          NodeIndices = new List<int>() { 1, 2, 3, 4 },
          Angle = 0,
          ReleaseInclusion = ReleaseInclusion.NotIncluded,
          OffsetZ = 0,
          ParentIndex = 1,
          Dummy = false
        },
        new GsaEl()
        {
          Index = 2,
          Name = "2",
          Colour = Colour.NO_RGB,
          Type = ElementType.Triangle3,
          PropertyIndex = 1,
          Group = 1,
          NodeIndices = new List<int>() { 2, 3, 5 },
          Angle = 0,
          ReleaseInclusion = ReleaseInclusion.NotIncluded,
          OffsetZ = 0,
          ParentIndex = 1,
          Dummy = false
        },
        new GsaEl()
        {
          Index = 3,
          Name = "3",
          Colour = Colour.NO_RGB,
          Type = ElementType.Triangle3,
          PropertyIndex = 1,
          Group = 1,
          NodeIndices = new List<int>() { 3, 5, 6 },
          Angle = 0,
          ReleaseInclusion = ReleaseInclusion.NotIncluded,
          OffsetZ = 0,
          ParentIndex = 1,
          Dummy = false
        }
      };
      for (int i = 0; i < appIds.Count(); i++)
      {
        gsaEl[i].ApplicationId = appIds[i];
      }
      return gsaEl.GetRange(0, numberOfElements);
    }

    private List<GsaEl> GsaElement1dExamples(int numberOfElements, params string[] appIds)
    {
      var gsaEl = new List<GsaEl>()
      {
        new GsaEl()
        {
          Index = 1,
          Name = "1",
          Colour = Colour.NO_RGB,
          Type = ElementType.Beam,
          PropertyIndex = 1,
          Group = 1,
          NodeIndices = new List<int>() { 1, 2 },
          ReleaseInclusion = ReleaseInclusion.NotIncluded,
          OffsetY = 0,
          OffsetZ = 0,
          ParentIndex = 1,
          Dummy = false
        },
        new GsaEl()
        {
          Index = 2,
          Name = "2",
          Colour = Colour.NO_RGB,
          Type = ElementType.Beam,
          PropertyIndex = 1,
          Group = 1,
          NodeIndices = new List<int>() { 2, 3 },
          Angle = 90,
          ReleaseInclusion = ReleaseInclusion.NotIncluded,
          OffsetY = 0,
          OffsetZ = 0,
          ParentIndex = 1,
          Dummy = false
        }
      };
      for (int i = 0; i < appIds.Count(); i++)
      {
        gsaEl[i].ApplicationId = appIds[i];
      }
      return gsaEl.GetRange(0, numberOfElements);
    }

    private List<GsaNode> GsaNodeExamples(int numberOfNodes, params string[] appIds)
    {
      var gsaNodes = new List<GsaNode>
      {
        new GsaNode()
        {
          Name = "1",
          Index = 1,
          Colour = Colour.NO_RGB,
          X = 0,
          Y = 0,
          Z = 0,
          NodeRestraint = NodeRestraint.Pin,
          AxisRefType = NodeAxisRefType.Global,
          MeshSize = 1,
          SpringPropertyIndex = 1,
          MassPropertyIndex = 1
        },
        new GsaNode()
        {
          Name = "2",
          Index = 2,
          Colour = Colour.NO_RGB,
          X = 1,
          Y = 0,
          Z = 0,
          NodeRestraint = NodeRestraint.Free,
          AxisRefType = NodeAxisRefType.Global,
          MeshSize = 1,
        },
        new GsaNode()
        {
          Name = "3",
          Index = 3,
          Colour = Colour.NO_RGB,
          X = 1,
          Y = 1,
          Z = 0,
          NodeRestraint = NodeRestraint.Free,
          AxisRefType = NodeAxisRefType.Global,
          MeshSize = 1,
        },
        new GsaNode()
        {
          Name = "4",
          Index = 4,
          Colour = Colour.NO_RGB,
          X = 0,
          Y = 1,
          Z = 0,
          NodeRestraint = NodeRestraint.Free,
          AxisRefType = NodeAxisRefType.Global,
          MeshSize = 1,
        },
        new GsaNode()
        {
          Name = "5",
          Index = 5,
          Colour = Colour.NO_RGB,
          X = 2,
          Y = 0,
          Z = 0,
          NodeRestraint = NodeRestraint.Free,
          AxisRefType = NodeAxisRefType.Global,
          MeshSize = 1,
        },
        new GsaNode()
        {
          Name = "6",
          Index = 6,
          Colour = Colour.NO_RGB,
          X = 2,
          Y = 1,
          Z = 0,
          NodeRestraint = NodeRestraint.Free,
          AxisRefType = NodeAxisRefType.Global,
          MeshSize = 1,
        }
      };
      for (int i = 0; i < appIds.Count(); i++)
      {
        gsaNodes[i].ApplicationId = appIds[i];
      }
      return gsaNodes.GetRange(0, numberOfNodes);
    }

    private List<GsaAssembly> GsaAssemblyExamples(int numberOfAssemblies, params string[] appIds)
    {
      var gsaAssemblies = new List<GsaAssembly>()
      {
        new GsaAssembly()
        {
          Index = 1,
          Name = "Assembly Name 1",
          Type = GSAEntity.ELEMENT,
          Entities = new List<int>() { 1, 2 },
          Topo1 = 1,
          Topo2 = 2,
          OrientNode = 3,
          //IntTopo = new List<int>(),
          SizeY = 0,
          SizeZ = 0,
          CurveType = CurveType.Lagrange,
          //CurveOrder = 0,
          PointDefn = PointDefinition.Points,
          NumberOfPoints = 10,
          //Spacing = 0,
          //StoreyIndices = new List<int>(),
          //ExplicitPositions = new List<double>()
        },
        new GsaAssembly()
        {
          Index = 2,
          Name = "Assembly Name 2",
          Type = GSAEntity.ELEMENT,
          Entities = new List<int>() { 1 },
          Topo1 = 2,
          Topo2 = 3,
          OrientNode = 4,
          //IntTopo = new List<int>(),
          SizeY = 0,
          SizeZ = 0,
          CurveType = CurveType.Lagrange,
          //CurveOrder = 0,
          PointDefn = PointDefinition.Spacing,
          //NumberOfPoints = 0,
          Spacing = 1,
          //StoreyIndices = new List<int>(),
          //ExplicitPositions = new List<double>()
        }
      };
      for (int i = 0; i < appIds.Count(); i++)
      {
        gsaAssemblies[i].ApplicationId = appIds[i];
      }
      return gsaAssemblies.GetRange(0, numberOfAssemblies);
    }

    private List<GsaMemb> GsaMemberExamples(int numberOfMembers, params string[] appIds)
    {
      var gsaMembs = new List<GsaMemb>()
      {
        new GsaMemb()
        {
          Index = 1,
          Name = "1",
          Colour = Colour.NO_RGB,
          Type = GwaMemberType.Generic1d,
          Exposure = ExposedSurfaces.ALL,
          PropertyIndex = 1,
          Group = 1,
          NodeIndices = new List<int>(){ 1, 2 },
          //Voids = null,
          //PointNodeIndices = null,
          //Polylines = null,
          //AdditionalAreas = null,
          //OrientationNodeIndex = null,
          //Angle = null,
          MeshSize = 0.25,
          IsIntersector = true,
          AnalysisType = AnalysisType.BEAM,
          Fire = FireResistance.Undefined,
          //LimitingTemperature = null,
          CreationFromStartDays = 0,
          StartOfDryingDays = 0,
          AgeAtLoadingDays = 0,
          RemovedAtDays = 0,
          Dummy = false,
          Releases1 = new Dictionary<AxisDirection6, ReleaseCode>()
          {
            { AxisDirection6.X, ReleaseCode.Fixed },
            { AxisDirection6.Y, ReleaseCode.Fixed },
            { AxisDirection6.Z, ReleaseCode.Fixed },
            { AxisDirection6.XX, ReleaseCode.Fixed },
            { AxisDirection6.YY, ReleaseCode.Fixed },
            { AxisDirection6.ZZ, ReleaseCode.Fixed }
          },
          //Stiffnesses1 = new List<double>(),
          Releases2 = new Dictionary<AxisDirection6, ReleaseCode>()
          {
            { AxisDirection6.X, ReleaseCode.Fixed },
            { AxisDirection6.Y, ReleaseCode.Fixed },
            { AxisDirection6.Z, ReleaseCode.Fixed },
            { AxisDirection6.XX, ReleaseCode.Fixed },
            { AxisDirection6.YY, ReleaseCode.Fixed },
            { AxisDirection6.ZZ, ReleaseCode.Fixed }
          },
          //Stiffnesses2 = new List<double>(),
          RestraintEnd1 = Speckle.GSA.API.GwaSchema.Restraint.Free,
          RestraintEnd2 = Speckle.GSA.API.GwaSchema.Restraint.Free,
          EffectiveLengthType = EffectiveLengthType.Automatic,
          //LoadHeight = null,
          LoadHeightReferencePoint = LoadHeightReferencePoint.ShearCentre,
          MemberHasOffsets = false,
          End1AutomaticOffset = false,
          End2AutomaticOffset = false,
          //End1OffsetX = null,
          //End2OffsetX = null,
          //OffsetY = null,
          //OffsetZ = null,
        },
        new GsaMemb()
        {
          Index = 2,
          Name = "2",
          Colour = Colour.NO_RGB,
          Type = GwaMemberType.Generic2d,
          Exposure = ExposedSurfaces.ALL,
          PropertyIndex = 1,
          Group = 1,
          NodeIndices = new List<int>(){ 1, 2, 3, 4 },
          //Voids = null,
          //PointNodeIndices = null,
          //Polylines = null,
          //AdditionalAreas = null,
          //OrientationNodeIndex = null,
          //Angle = null,
          MeshSize = 0.25,
          IsIntersector = true,
          AnalysisType = AnalysisType.LINEAR,
          Fire = FireResistance.Undefined,
          //LimitingTemperature = null,
          CreationFromStartDays = 0,
          StartOfDryingDays = 0,
          AgeAtLoadingDays = 0,
          RemovedAtDays = 0,
          Dummy = false,
          Offset2dZ = 0,
          OffsetAutomaticInternal = false,
        },
      };
      for (int i = 0; i < appIds.Count(); i++)
      {
        gsaMembs[i].ApplicationId = appIds[i];
      }
      return gsaMembs.GetRange(0, numberOfMembers);
    }
    #endregion

    #region Loading
    private List<GsaLoadCase> GsaLoadCaseExamples(int numberOfLoadCases, params string[] appIds)
    {
      var gsaLoadCases = new List<GsaLoadCase>()
      {
        new GsaLoadCase()
        {
          Index = 1,
          Title = "Dead",
          CaseType = StructuralLoadCaseType.Dead,
        },
        new GsaLoadCase()
        {
          Index = 2,
          Title = "Live",
          CaseType = StructuralLoadCaseType.Live,
        }
      };
      for (int i = 0; i < appIds.Count(); i++)
      {
        gsaLoadCases[i].ApplicationId = appIds[i];
      }
      return gsaLoadCases.GetRange(0, numberOfLoadCases);
    }

    private List<GsaLoad2dFace> GsaLoad2dFaceExamples(int numberOfLoads, params string[] appIds)
    {
      var gsaLoad2dFaces = new List<GsaLoad2dFace>()
      {
        new GsaLoad2dFace()
        {
          Index = 1,
          Name = "1",
          Entities = new List<int>(){ 1 },
          LoadCaseIndex = 1,
          AxisRefType = AxisRefType.Global,
          Type = Load2dFaceType.Uniform,
          Projected = false,
          LoadDirection = AxisDirection3.Z,
          Values = new List<double>(){ 1 },
        },
        new GsaLoad2dFace()
        {
          Index = 2,
          Name  = "2",
          Entities = new List<int>(){ 2 },
          LoadCaseIndex = 1,
          AxisRefType = AxisRefType.Reference,
          AxisIndex = 1,
          Type = Load2dFaceType.Point,
          Projected = false,
          LoadDirection = AxisDirection3.X,
          Values = new List<double>(){ 1 },
          R = 0,
          S = 0
        },
        new GsaLoad2dFace()
        {
          Index = 3,
          Name  = "3",
          Entities = new List<int>(){ 3 },
          LoadCaseIndex = 1,
          AxisRefType = AxisRefType.Local,
          Type = Load2dFaceType.General,
          Projected = false,
          LoadDirection = AxisDirection3.Y,
          Values = new List<double>(){ 1, 2, 3, 4 }
        }
      };
      for (int i = 0; i < appIds.Count(); i++)
      {
        gsaLoad2dFaces[i].ApplicationId = appIds[i];
      }
      return gsaLoad2dFaces.GetRange(0, numberOfLoads);
    }

    private List<GsaLoadBeam> GsaLoadBeamExamples(int numberOfLoads, params string[] appIds)
    {
      var gsaLoadBeams = new List<GsaLoadBeam>()
      {
        new GsaLoadBeamPoint()
        {
          Index = 1,
          Name = "1",
          Entities = new List<int>(){ 1 },
          LoadCaseIndex = 1,
          AxisRefType = LoadBeamAxisRefType.Global,
          Projected = false,
          LoadDirection = AxisDirection6.Z,
          Position = 0,
          Load = 1
        },
        new GsaLoadBeamUdl()
        {
          Index = 2,
          Name  = "2",
          Entities = new List<int>(){ 2 },
          LoadCaseIndex = 1,
          AxisRefType = LoadBeamAxisRefType.Reference,
          AxisIndex = 1,
          Projected = false,
          LoadDirection = AxisDirection6.X,
          Load = 1,
        },
        new GsaLoadBeamLine()
        {
          Index = 3,
          Name  = "3",
          Entities = new List<int>(){ 1 },
          LoadCaseIndex = 1,
          AxisRefType = LoadBeamAxisRefType.Local,
          Projected = false,
          LoadDirection = AxisDirection6.Y,
          Load1 = 1,
          Load2 = 2
        }
      };
      for (int i = 0; i < appIds.Count(); i++)
      {
        gsaLoadBeams[i].ApplicationId = appIds[i];
      }
      return gsaLoadBeams.GetRange(0, numberOfLoads);
    }

    private List<GsaLoadNode> GsaLoadNodeExamples(int numberOfLoads, params string[] appIds)
    {
      var gsaLoadNodes = new List<GsaLoadNode>()
      {
        new GsaLoadNode()
        {
          Index = 1,
          Name = "1",
          LoadCaseIndex = 1,
          NodeIndices = new List<int>() { 1 },
          GlobalAxis = true,
          //AxisIndex = null,
          LoadDirection = AxisDirection6.Z,
          Value = 1
        },
        new GsaLoadNode()
        {
          Index = 2,
          Name  = "2",
          LoadCaseIndex = 1,
          NodeIndices = new List<int>() { 1 },
          GlobalAxis = false,
          AxisIndex = 1,
          LoadDirection = AxisDirection6.X,
          Value = 1,
        }
      };
      for (int i = 0; i < appIds.Count(); i++)
      {
        gsaLoadNodes[i].ApplicationId = appIds[i];
      }
      return gsaLoadNodes.GetRange(0, numberOfLoads);
    }

    private GsaLoadGravity GsaLoadGravityExample(string appId)
    {
      return new GsaLoadGravity()
      {
        ApplicationId = appId,
        Index = 1,
        Name = "1",
        Entities = new List<int>() { 1, 2 },
        Nodes = new List<int>() { 1, 2, 3, 4, 5 },
        LoadCaseIndex = 1,
        X = 0,
        Y = 0,
        Z = -1
      };
    }

    private List<GsaCombination> GsaCombinationExamples(int numberOfCombos, params string[] appIds)
    {
      var gsaCombo = new List<GsaCombination>()
      {
        new GsaCombination()
        {
          Index = 1,
          Name = "Combination 1",
          Desc = "A1 + 0.2A2",
          //Bridge = false,
          Note = ""
        },
        new GsaCombination()
        {
          Index = 2,
          Name = "Combination 2",
          Desc = "1.3C1 + (A1 to A2)",
          //Bridge = false,
          Note = ""
        }
      };
      for (int i = 0; i < appIds.Count(); i++)
      {
        gsaCombo[i].ApplicationId = appIds[i];
      }
      return gsaCombo.GetRange(0, numberOfCombos);
    }
    #endregion

    #region Materials
    private GsaMatConcrete GsaMatConcreteExample(string appId)
    {
      return new GsaMatConcrete()
      {
        Index = 1,
        ApplicationId = appId,
        Name = "1",
        Mat = new GsaMat()
        {
          Name = "",
          E = 3.315274903e+10,
          F = 40000000,
          Nu = 0.2,
          G = 1.381364543e+10,
          Rho = 2400,
          Alpha = 1e-5,
          Prop = new GsaMatAnal()
          {
            Type = MatAnalType.MAT_ELAS_ISO,
            NumParams = 6,
            E = 3.315274903e+10,
            Nu = 0.2,
            Rho = 2400,
            Alpha = 1e-5,
            G = 1.381364543e+10,
            Damp = 0
          },
          NumUC = 0,
          AbsUC = Dimension.NotSet,
          OrdUC = Dimension.NotSet,
          PtsUC = new double[0],
          NumSC = 0,
          AbsSC = Dimension.NotSet,
          OrdSC = Dimension.NotSet,
          PtsSC = new double[0],
          NumUT = 0,
          AbsUT = Dimension.NotSet,
          OrdUT = Dimension.NotSet,
          PtsUT = new double[0],
          NumST = 0,
          AbsST = Dimension.NotSet,
          OrdST = Dimension.NotSet,
          PtsST = new double[0],
          Eps = 0,
          Uls = new GsaMatCurveParam()
          {
            Model = new List<MatCurveParamType>() { MatCurveParamType.RECTANGLE, MatCurveParamType.NO_TENSION },
            StrainElasticCompression = 0.00068931,
            StrainElasticTension = 0,
            StrainPlasticCompression = 0.00069069,
            StrainPlasticTension = 0,
            StrainFailureCompression = 0.003,
            StrainFailureTension = 1,
            GammaF = 1,
            GammaE = 1
          },
          Sls = new GsaMatCurveParam()
          {
            Model = new List<MatCurveParamType>() { MatCurveParamType.LINEAR, MatCurveParamType.INTERPOLATED },
            StrainElasticCompression = 0.003,
            StrainElasticTension = 0,
            StrainPlasticCompression = 0.003,
            StrainPlasticTension = 0,
            StrainFailureCompression = 0.003,
            StrainFailureTension = 0.0001144620975,
            GammaF = 1,
            GammaE = 1
          },
          Cost = 0,
          Type = MatType.CONCRETE
        },
        Type = MatConcreteType.CYLINDER,
        Cement = MatConcreteCement.N,
        Fc = 40000000,
        Fcd = 34000000,
        Fcdc = 16000000,
        Fcdt = 3794733.192,
        Fcfib = 2276839.915,
        EmEs = 0,
        N = 2,
        Emod = 1,
        EpsPeak = 0.003,
        EpsMax = 0.00069,
        EpsU = 0.003,
        EpsAx = 0.0025,
        EpsTran = 0.002,
        EpsAxs = 0.0025,
        Light = false,
        Agg = 0.02,
        XdMin = 0,
        XdMax = 1,
        Beta = 0.77,
        Shrink = 0,
        Confine = 0,
        Fcc = 0,
        EpsPlasC = 0,
        EpsUC = 0
      };
    }

    private GsaMatSteel GsaMatSteelExample(string appId)
    {
      return new GsaMatSteel()
      {
        Index = 1,
        ApplicationId = appId,
        Name = "1",
        Mat = new GsaMat()
        {
          Name = "",
          E = 2e11,
          F = 360000000,
          Nu = 0.3,
          G = 7.692307692e+10,
          Rho = 7850,
          Alpha = 1.2e-5,
          Prop = new GsaMatAnal()
          {
            Type = MatAnalType.MAT_ELAS_ISO,
            NumParams = 6,
            E = 2e11,
            Nu = 0.3,
            Rho = 7850,
            Alpha = 1.2e-5,
            G = 7.692307692e+10,
            Damp = 0
          },
          NumUC = 0,
          AbsUC = Dimension.NotSet,
          OrdUC = Dimension.NotSet,
          PtsUC = new double[0],
          NumSC = 0,
          AbsSC = Dimension.NotSet,
          OrdSC = Dimension.NotSet,
          PtsSC = new double[0],
          NumUT = 0,
          AbsUT = Dimension.NotSet,
          OrdUT = Dimension.NotSet,
          PtsUT = new double[0],
          NumST = 0,
          AbsST = Dimension.NotSet,
          OrdST = Dimension.NotSet,
          PtsST = new double[0],
          Eps = 0.05,
          Uls = new GsaMatCurveParam()
          {
            Model = new List<MatCurveParamType>() { MatCurveParamType.UNDEF },
            StrainElasticCompression = 0.0018,
            StrainElasticTension = 0.0018,
            StrainPlasticCompression = 0.0018,
            StrainPlasticTension = 0.0018,
            StrainFailureCompression = 0.05,
            StrainFailureTension = 0.05,
            GammaF = 1,
            GammaE = 1
          },
          Sls = new GsaMatCurveParam()
          {
            Model = new List<MatCurveParamType>() { MatCurveParamType.ELAS_PLAS },
            StrainElasticCompression = 0.0018,
            StrainElasticTension = 0.0018,
            StrainPlasticCompression = 0.0018,
            StrainPlasticTension = 0.0018,
            StrainFailureCompression = 0.05,
            StrainFailureTension = 0.05,
            GammaF = 1,
            GammaE = 1
          },
          Cost = 0,
          Type = MatType.STEEL
        },
        Fy = 360000000,
        Fu = 450000000,
        EpsP = 0,
        Eh = 0,
      };
    }
    #endregion

    #region Properties
    private GsaProp2d GsaProp2dExample(string appId)
    {
      return new GsaProp2d()
      {
        Index = 1,
        Name = "1",
        ApplicationId = appId,
        Colour = Colour.NO_RGB,
        Type = Property2dType.Shell,
        AxisRefType = AxisRefType.Global,
        AnalysisMaterialIndex = 0,
        MatType = Property2dMaterialType.Steel,
        GradeIndex = 1,
        DesignIndex = 0,
        Thickness = 0.01,
        RefPt = Property2dRefSurface.Centroid,
        RefZ = 0,
        Mass = 0,
        BendingStiffnessPercentage = 1,
        ShearStiffnessPercentage = 1,
        InPlaneStiffnessPercentage = 1,
        VolumePercentage = 1
      };
    }

    private GsaSection GsaCatalogueSectionExample(string appId)
    {
      return new GsaSection()
      {
        Index = 1,
        Name = "1",
        ApplicationId = appId,
        Colour = Colour.NO_RGB,
        Type = Section1dType.Generic,
        //PoolIndex = 0,
        ReferencePoint = ReferencePoint.Centroid,
        RefY = 0,
        RefZ = 0,
        Mass = 0,
        Fraction = 1,
        Cost = 0,
        Left = 0,
        Right = 0,
        Slab = 0,
        Components = new List<GsaSectionComponentBase>()
        {
          new SectionComp()
          {
            Name = "",
            //MatAnalIndex = 0,
            MaterialType = Section1dMaterialType.STEEL,
            MaterialIndex = 1,
            OffsetY = 0,
            OffsetZ = 0,
            Rotation = 0,
            Reflect = ComponentReflection.NONE,
            //Pool = 0,
            TaperType = Section1dTaperType.NONE,
            //TaperPos = 0
            ProfileGroup = Section1dProfileGroup.Catalogue,
            ProfileDetails = new ProfileDetailsCatalogue()
            {
              Group = Section1dProfileGroup.Catalogue,
              Profile = "CAT A-UB 610UB125 19981201"
            }
          },
          new SectionSteel()
          {
            //GradeIndex = 0,
            PlasElas = 1,
            NetGross = 1,
            Exposed = 1,
            Beta = 0.4,
            Type = SectionSteelSectionType.HotRolled,
            Plate = SectionSteelPlateType.Undefined,
            Locked = false
          }
        },
        Environ = false
      };
    }

    private GsaPropMass GsaPropMassExample(string appId)
    {
      return new GsaPropMass()
      {
        Index = 1,
        ApplicationId = appId,
        Mass = 10,
        Ixx = 0,
        Iyy = 0,
        Izz = 0,
        Ixy = 0,
        Iyz = 0,
        Izx = 0,
        Mod = MassModification.Modified,
        ModXPercentage = 1,
        ModYPercentage = 1,
        ModZPercentage = 1
      };
    }

    private GsaPropSpr GsaPropSprExample(string appId)
    {
      return new GsaPropSpr()
      {
        Index = 1,
        ApplicationId = appId,
        Name = "1",
        Colour = Colour.NO_RGB,
        PropertyType = StructuralSpringPropertyType.General,
        Stiffnesses = new Dictionary<AxisDirection6, double>
        { { AxisDirection6.X, 10 },
          { AxisDirection6.Y, 11 },
          { AxisDirection6.Z, 12 },
          { AxisDirection6.XX, 13 },
          { AxisDirection6.YY, 14 },
          { AxisDirection6.ZZ, 15 },
        },
        DampingRatio = 5
      };
    }
    #endregion

    #region Results
    #endregion
    #endregion
  }
}
