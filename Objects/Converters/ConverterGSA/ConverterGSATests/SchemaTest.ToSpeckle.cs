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
using Model = Objects.Structural.Analysis.Model;
using Speckle.ConnectorGSA.Proxy.GwaParsers;
using GwaMemberType = Speckle.GSA.API.GwaSchema.MemberType;
using MemberType = Objects.Structural.Geometry.MemberType;
using GwaAxisDirection6 = Speckle.GSA.API.GwaSchema.AxisDirection6;
using AxisDirection6 = Objects.Structural.GSA.Geometry.AxisDirection6;
using Xunit.Sdk;
using Speckle.Core.Kits;
using ConverterGSA;
using Speckle.ConnectorGSA.Proxy.Merger;
using Speckle.GSA.API.CsvSchema;
using Objects.Structural.Results;
using Speckle.Core.Models;
using Objects.Structural.GSA.Analysis;
using Objects.Structural.GSA.Bridge;
using Piwik.Tracker;
using ActionType = Objects.Structural.Loading.ActionType;

namespace ConverterGSATests
{
  public partial class SchemaTest : SpeckleConversionFixture
  {
    //Reminder: conversions could create 1:1, 1:n, n:1, n:n structural per native objects

    #region Geometry
    [Fact]
    public void AxisToSpeckle()
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
    public void NodeToSpeckle()
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
      Assert.True(speckleNode.constraintAxis.definition.IsGlobal());
      Assert.Equal("property spring 1", speckleNode.springProperty.applicationId); //assume conversion code for GsaPropSpr is tested elsewhere
      Assert.Equal("property mass 1", speckleNode.massProperty.applicationId); //assume conversion code for GsaPropMass is tested elsewhere
      Assert.Equal(gsaNodes[0].Index.Value, speckleNode.nativeId);
      Assert.Equal(gsaNodes[0].Colour.ToString(), speckleNode.colour);
      Assert.Equal(gsaNodes[0].MeshSize.Value, speckleNode.localElementSize);
    }

    [Fact]
    public void Element2dToSpeckle()
    {
      //Define GSA objects
      //These should be in order that respects the type dependency tree (which is only available in the GSAProxy library, which isn't referenced yet
      var gsaRecords = new List<GsaRecord>();

      //Generation #1: Types with no other dependencies - the leaves of the tree
      gsaRecords.Add(GsaMatSteelExample("steel material 1"));
      gsaRecords.Add(GsaPropMassExample("property mass 1"));
      gsaRecords.Add(GsaPropSprExample("property spring 1"));

      //Gen #2
      var gsaNodes = GsaNodeExamples(5, "node 1", "node 2", "node 3", "node 4", "node 5");
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

        Instance.GsaModel.Cache.SetSpeckleObjects(record, speckleObjects.ToDictionary(o => o.applicationId, o => (object)o));
      }

      Assert.True(Instance.GsaModel.Cache.GetSpeckleObjects(out var structuralObjects));

      Assert.NotEmpty(structuralObjects);
      Assert.Contains(structuralObjects, so => so is GSAElement2D);

      var speckleElement2D = structuralObjects.FindAll(so => so is GSAElement2D).Select(so => (GSAElement2D)so).ToList();

      //
      List<Point> p;
      List<double> v;
      List<int> f;

      //===========
      // Element 1
      //===========
      Assert.Equal("element 1", speckleElement2D[0].applicationId);
      Assert.Equal(gsaEls[0].Name, speckleElement2D[0].name);
      //Assert.Null(speckleElement2D[0].baseMesh); //TODO: update once conversion code has been updated
      Assert.Equal("property 2D 1", speckleElement2D[0].property.applicationId);
      Assert.Equal(ElementType2D.Quad4, speckleElement2D[0].type);
      Assert.Equal(gsaEls[0].OffsetZ.Value, speckleElement2D[0].offset);
      Assert.Equal(gsaEls[0].Angle.Value, speckleElement2D[0].orientationAngle);
      Assert.Null(speckleElement2D[0].parent); //TODO: update once conversion code has been updated
      Assert.Equal(4, speckleElement2D[0].topology.Count);
      Assert.Equal("node 1", speckleElement2D[0].topology[0].applicationId);
      Assert.Equal("node 2", speckleElement2D[0].topology[1].applicationId);
      Assert.Equal("node 3", speckleElement2D[0].topology[2].applicationId);
      Assert.Equal("node 4", speckleElement2D[0].topology[3].applicationId);
      p = speckleElement2D[0].topology.Select(n => n.basePoint).ToList();
      v = new List<double>() { p[0].x, p[0].y, p[0].z, p[1].x, p[1].y, p[1].z, p[2].x, p[2].y, p[2].z, p[3].x, p[3].y, p[3].z };
      f = new List<int>() { 1, 0, 1, 2, 3 };
      Assert.Equal(v, speckleElement2D[0].displayMesh.vertices);
      Assert.Equal(f, speckleElement2D[0].displayMesh.faces);
      Assert.Equal(gsaEls[0].Colour.ToString(), speckleElement2D[0].colour);
      Assert.False(speckleElement2D[0].isDummy);
      Assert.Equal(gsaEls[0].Index.Value, speckleElement2D[0].nativeId);
      Assert.Equal(gsaEls[0].Group.Value, speckleElement2D[0].group);

      //===========
      // Element 2
      //===========
      Assert.Equal("element 2", speckleElement2D[1].applicationId);
      Assert.Equal(gsaEls[1].Name, speckleElement2D[1].name);
      //Assert.Null(speckleElement2D[1].baseMesh); //TODO: update once conversion code has been updated
      Assert.Equal("property 2D 1", speckleElement2D[1].property.applicationId);
      Assert.Equal(ElementType2D.Triangle3, speckleElement2D[1].type);
      Assert.Equal(gsaEls[1].OffsetZ.Value, speckleElement2D[1].offset);
      Assert.Equal(gsaEls[1].Angle.Value, speckleElement2D[1].orientationAngle);
      Assert.Null(speckleElement2D[1].parent); //TODO: update once conversion code has been updated
      Assert.Equal(3, speckleElement2D[1].topology.Count);
      Assert.Equal("node 2", speckleElement2D[1].topology[0].applicationId);
      Assert.Equal("node 3", speckleElement2D[1].topology[1].applicationId);
      Assert.Equal("node 5", speckleElement2D[1].topology[2].applicationId);
      p = speckleElement2D[1].topology.Select(n => n.basePoint).ToList();
      v = new List<double>() { p[0].x, p[0].y, p[0].z, p[1].x, p[1].y, p[1].z, p[2].x, p[2].y, p[2].z };
      f = new List<int>() { 0, 0, 1, 2 };
      Assert.Equal(v, speckleElement2D[1].displayMesh.vertices);
      Assert.Equal(f, speckleElement2D[1].displayMesh.faces);
      Assert.Equal(gsaEls[1].Colour.ToString(), speckleElement2D[1].colour);
      Assert.False(speckleElement2D[1].isDummy);
      Assert.Equal(gsaEls[1].Index.Value, speckleElement2D[1].nativeId);
      Assert.Equal(gsaEls[1].Group.Value, speckleElement2D[1].group);
    }

    [Fact]
    public void Element1dToSpeckle()
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
      Assert.Equal("NORMAL",speckleElement1d[0].action);
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
      Assert.Equal("NORMAL", speckleElement1d[1].action);
      Assert.Equal(gsaEls[1].Index.Value, speckleElement1d[1].nativeId);
      Assert.Equal(gsaEls[1].Group.Value, speckleElement1d[1].group);
    }

    [Fact]
    public void AssemblyToSpeckle()
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
      Assert.Equal(gsaAssemblies[0].Name, speckleAssemblies[0].name);
      Assert.Equal("node 1", speckleAssemblies[0].end1Node.applicationId);
      Assert.Equal("node 2", speckleAssemblies[0].end2Node.applicationId);
      Assert.Equal("node 3", speckleAssemblies[0].orientationNode.applicationId);
      Assert.Equal(2, speckleAssemblies[0].entities.Count());
      Assert.Equal("element 1", speckleAssemblies[0].entities[0].applicationId);
      Assert.Equal("element 2", speckleAssemblies[0].entities[1].applicationId);
      Assert.Equal(gsaAssemblies[0].SizeY, speckleAssemblies[0].sizeY);
      Assert.Equal(gsaAssemblies[0].SizeZ, speckleAssemblies[0].sizeZ);
      Assert.Equal(gsaAssemblies[0].CurveType.ToString(), speckleAssemblies[0].curveType);
      Assert.Equal(0, speckleAssemblies[0].curveOrder);
      Assert.Equal(gsaAssemblies[0].PointDefn.ToString(), speckleAssemblies[0].pointDefinition);
      Assert.Equal(new List<double>() { 10 }, speckleAssemblies[0].points);

      //Checks - Assembly 2
      Assert.Equal("assembly 2", speckleAssemblies[1].applicationId);
      Assert.Equal(gsaAssemblies[1].Index, speckleAssemblies[1].nativeId);
      Assert.Equal(gsaAssemblies[1].Name, speckleAssemblies[1].name);
      Assert.Equal("node 2", speckleAssemblies[1].end1Node.applicationId);
      Assert.Equal("node 3", speckleAssemblies[1].end2Node.applicationId);
      Assert.Equal("node 4", speckleAssemblies[1].orientationNode.applicationId);
      Assert.Single(speckleAssemblies[1].entities);
      Assert.Equal("element 1", speckleAssemblies[1].entities[0].applicationId);
      Assert.Equal(gsaAssemblies[1].SizeY, speckleAssemblies[1].sizeY);
      Assert.Equal(gsaAssemblies[1].SizeZ, speckleAssemblies[1].sizeZ);
      Assert.Equal(gsaAssemblies[1].CurveType.ToString(), speckleAssemblies[1].curveType);
      Assert.Equal(0, speckleAssemblies[1].curveOrder);
      Assert.Equal(gsaAssemblies[1].PointDefn.ToString(), speckleAssemblies[1].pointDefinition);
      Assert.Equal(new List<double>() { 1 }, speckleAssemblies[1].points);
    }

    [Fact]
    public void MembToSpeckle()
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

      var speckleMember1d = (GSAMember1D)structuralObjects.FirstOrDefault(so => so is GSAMember1D);
      var speckleMember2d = (GSAMember2D)structuralObjects.FirstOrDefault(so => so is GSAMember2D);

      //Checks - Member 1
      Assert.Equal("member 1", speckleMember1d.applicationId);
      Assert.Equal(gsaMembers[0].Name, speckleMember1d.name);
      Assert.Equal(ElementType1D.Beam, speckleMember1d.type);
      Assert.Equal("FFFFFF", speckleMember1d.end1Releases.code);
      Assert.Equal("FFFFFF", speckleMember1d.end2Releases.code);
      Assert.Equal(gsaMembers[0].End1OffsetX.Value, speckleMember1d.end1Offset.x);
      Assert.Equal(gsaMembers[0].OffsetY.Value, speckleMember1d.end1Offset.y);
      Assert.Equal(gsaMembers[0].OffsetZ.Value, speckleMember1d.end1Offset.z);
      Assert.Equal(gsaMembers[0].End2OffsetX.Value, speckleMember1d.end2Offset.x);
      Assert.Equal(gsaMembers[0].OffsetY.Value, speckleMember1d.end2Offset.y);
      Assert.Equal(gsaMembers[0].OffsetZ.Value, speckleMember1d.end2Offset.z);
      Assert.Equal(gsaMembers[0].Angle.Value, speckleMember1d.orientationAngle);
      Assert.Null(speckleMember1d.parent); //not meaningful for member
      Assert.Equal("node 1", speckleMember1d.end1Node.applicationId);
      Assert.Equal("node 2", speckleMember1d.end2Node.applicationId);
      Assert.Equal(2, speckleMember1d.topology.Count());
      Assert.Equal("node 1", speckleMember1d.topology[0].applicationId);
      Assert.Equal("node 2", speckleMember1d.topology[1].applicationId);
      Assert.Null(speckleMember1d.units);
      Assert.Equal(gsaMembers[0].Colour.ToString(), speckleMember1d.colour);
      Assert.Equal(gsaMembers[0].Dummy, speckleMember1d.isDummy);
      Assert.Null(speckleMember1d.units);
      Assert.Equal(gsaMembers[0].IsIntersector, speckleMember1d.intersectsWithOthers);
      Assert.Equal("section 1", speckleMember1d.property.applicationId);
      Assert.Equal(0, speckleMember1d.orientationAngle);
      Assert.True(speckleMember1d.localAxis.IsGlobal());
      Assert.Equal(gsaMembers[0].Index.Value, speckleMember1d.nativeId);
      Assert.Equal(gsaMembers[0].Group.Value, speckleMember1d.group);
      Assert.Equal(gsaMembers[0].MeshSize.Value, speckleMember1d.targetMeshSize);

      //Checks - Member 2
      Assert.Equal("member 2", speckleMember2d.applicationId);
      Assert.Equal(gsaMembers[1].Name, speckleMember2d.name);
      //Assert.Null(speckleMember2d.baseMesh); //TODO: update once conversion code handles base mesh
      Assert.Equal("prop 2D 1", speckleMember2d.property.applicationId);
      Assert.Equal(ElementType2D.Quad4, speckleMember2d.type);
      Assert.Equal(gsaMembers[1].Offset2dZ, speckleMember2d.offset);
      Assert.Equal(0, speckleMember2d.orientationAngle);
      Assert.Null(speckleMember2d.parent); //not meaningful for member
      Assert.Equal(4, speckleMember2d.topology.Count());
      Assert.Equal("node 1", speckleMember2d.topology[0].applicationId);
      Assert.Equal("node 2", speckleMember2d.topology[1].applicationId);
      Assert.Equal("node 3", speckleMember2d.topology[2].applicationId);
      Assert.Equal("node 4", speckleMember2d.topology[3].applicationId);
      //Assert.Null(speckleMember2d.displayMesh); //TODO: update once conversion code handle display mesh
      Assert.Null(speckleMember2d.units);
      Assert.Equal(gsaMembers[1].Index.Value, speckleMember2d.nativeId);
      Assert.Equal(gsaMembers[1].Group.Value, speckleMember2d.group);
      Assert.Equal(gsaMembers[1].Colour.ToString(), speckleMember2d.colour);
      Assert.Equal(gsaMembers[1].Dummy, speckleMember2d.isDummy);
      Assert.Equal(gsaMembers[1].IsIntersector, speckleMember2d.intersectsWithOthers);
      Assert.Equal(gsaMembers[1].MeshSize.Value, speckleMember2d.targetMeshSize);
    }

    [Fact]
    public void GridLineToSpeckle()
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

      var speckleGridLines = structuralObjects.FindAll(so => so is GSAGridLine).Select(so => (GSAGridLine)so).ToList();

      //Checks - grid line 1
      Assert.Equal("grid line 1", speckleGridLines[0].applicationId);
      Assert.Equal(gsaGridLines[0].Name, speckleGridLines[0].label);
      Assert.Equal(gsaGridLines[0].Index.Value, speckleGridLines[0].nativeId);
      var speckleLine = (Line)speckleGridLines[0].baseLine;
      Assert.Equal(gsaGridLines[0].XCoordinate.Value, speckleLine.start.x);
      Assert.Equal(gsaGridLines[0].YCoordinate.Value, speckleLine.start.y);
      Assert.Equal(0, speckleLine.start.z);
      Assert.Equal(gsaGridLines[0].XCoordinate.Value + gsaGridLines[0].Length.Value * Math.Cos(gsaGridLines[0].Theta1.Value.Radians()), speckleLine.end.x);
      Assert.Equal(gsaGridLines[0].YCoordinate.Value + gsaGridLines[0].Length.Value * Math.Sin(gsaGridLines[0].Theta1.Value.Radians()), speckleLine.end.y);
      Assert.Equal(0, speckleLine.end.z);

      //Checks - grid line 2
      Assert.Equal("grid line 2", speckleGridLines[1].applicationId);
      Assert.Equal(gsaGridLines[1].Name, speckleGridLines[1].label);
      Assert.Equal(gsaGridLines[1].Index.Value, speckleGridLines[1].nativeId);
      var speckleArc = (Arc)speckleGridLines[1].baseLine;
      Assert.Equal(gsaGridLines[1].XCoordinate.Value + gsaGridLines[1].Length * Math.Cos(gsaGridLines[1].Theta1.Value.Radians()), speckleArc.startPoint.x);
      Assert.Equal(gsaGridLines[1].YCoordinate.Value + gsaGridLines[1].Length * Math.Sin(gsaGridLines[1].Theta1.Value.Radians()), speckleArc.startPoint.y);
      Assert.Equal(0, speckleArc.startPoint.z);
      Assert.Equal(gsaGridLines[1].XCoordinate.Value + gsaGridLines[1].Length * Math.Cos(gsaGridLines[1].Theta2.Value.Radians()), speckleArc.endPoint.x);
      Assert.Equal(gsaGridLines[1].YCoordinate.Value + gsaGridLines[1].Length * Math.Sin(gsaGridLines[1].Theta2.Value.Radians()), speckleArc.endPoint.y);
      Assert.Equal(0, speckleArc.endPoint.z);
      Assert.Equal(gsaGridLines[1].Length.Value, speckleArc.radius);
      Assert.Equal(gsaGridLines[1].Theta1.Value.Radians(), speckleArc.startAngle);
      Assert.Equal(gsaGridLines[1].Theta2.Value.Radians(), speckleArc.endAngle);
      Assert.True(speckleArc.plane.IsGlobal());
    }

    [Fact]
    public void GridPlaneToSpeckle()
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

      //Checks - grid plane 1
      Assert.Equal("grid plane 1", speckleGridPlanes[0].applicationId);
      Assert.Equal(gsaGridPlanes[0].Name, speckleGridPlanes[0].name);
      Assert.Equal(gsaGridPlanes[0].Index.Value, speckleGridPlanes[0].nativeId);
      Assert.True(speckleGridPlanes[0].axis.definition.IsGlobal());
      Assert.Equal(gsaGridPlanes[0].Elevation.Value, speckleGridPlanes[0].elevation);
      Assert.Equal(gsaGridPlanes[0].StoreyToleranceBelow, speckleGridPlanes[0].toleranceBelow.Value);
      Assert.Equal(gsaGridPlanes[0].StoreyToleranceAbove, speckleGridPlanes[0].toleranceAbove.Value);

      //Checks - grid plane 2
      Assert.Equal("grid plane 2", speckleGridPlanes[1].applicationId);
      Assert.Equal(gsaGridPlanes[1].Name, speckleGridPlanes[1].name);
      Assert.Equal(gsaGridPlanes[1].Index.Value, speckleGridPlanes[1].nativeId);
      Assert.Equal("axis 1", speckleGridPlanes[1].axis.applicationId);
      Assert.Equal(gsaGridPlanes[1].Elevation.Value, speckleGridPlanes[1].elevation);
      Assert.False(speckleGridPlanes[1].toleranceBelow.HasValue);
      Assert.False(speckleGridPlanes[1].toleranceAbove.HasValue);
    }

    [Fact]
    public void GridSurfaceToSpeckle()
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
      Assert.NotEmpty(speckleObjects);
      Assert.Contains(speckleObjects, so => so is Model);
      var speckleModelObjects = speckleObjects.FindAll(so => so is Model).Select(so => (Model)so).ToList();
      var speckleDesignModel = speckleModelObjects.Where(so => so.layerDescription == "Design").FirstOrDefault();
      var speckleAnalysisModel = speckleModelObjects.Where(so => so.layerDescription == "Analysis").FirstOrDefault();
      Assert.NotNull(speckleDesignModel.elements);
      Assert.NotNull(speckleAnalysisModel.elements);
      Assert.Contains(speckleDesignModel.elements, o => o is GSAGridSurface);
      Assert.Contains(speckleAnalysisModel.elements, o => o is GSAGridSurface);

      #region Design Layer
      var speckleGridSurfaces = speckleDesignModel.elements.FindAll(so => so is GSAGridSurface).Select(so => (GSAGridSurface)so).ToList();

      //Checks - grid surface 1
      Assert.Equal("grid surface 1", speckleGridSurfaces[0].applicationId);
      Assert.Equal(gsaGridSurfaces[0].Name, speckleGridSurfaces[0].name);
      Assert.Equal(gsaGridSurfaces[0].Index.Value, speckleGridSurfaces[0].nativeId);
      Assert.True(speckleGridSurfaces[0].gridPlane.axis.definition.IsGlobal());
      Assert.Equal(gsaGridSurfaces[0].Tolerance.Value, speckleGridSurfaces[0].tolerance);
      Assert.Equal(gsaGridSurfaces[0].Angle.Value, speckleGridSurfaces[0].spanDirection);
      Assert.Equal(LoadExpansion.PlaneAspect, speckleGridSurfaces[0].loadExpansion);
      Assert.Equal(GridSurfaceSpanType.OneWay, speckleGridSurfaces[0].span);
      Assert.Null(speckleGridSurfaces[0].elements);

      //Checks - grid surface 2
      Assert.Equal("grid surface 2", speckleGridSurfaces[1].applicationId);
      Assert.Equal(gsaGridSurfaces[1].Name, speckleGridSurfaces[1].name);
      Assert.Equal(gsaGridSurfaces[1].Index.Value, speckleGridSurfaces[1].nativeId);
      Assert.Equal("grid plane 1", speckleGridSurfaces[1].gridPlane.applicationId);
      Assert.Equal(gsaGridSurfaces[1].Tolerance.Value, speckleGridSurfaces[1].tolerance);
      Assert.Equal(gsaGridSurfaces[1].Angle.Value, speckleGridSurfaces[1].spanDirection);
      Assert.Equal(LoadExpansion.PlaneAspect, speckleGridSurfaces[1].loadExpansion);
      Assert.Equal(GridSurfaceSpanType.TwoWay, speckleGridSurfaces[1].span);
      Assert.Null(speckleGridSurfaces[1].elements);
      #endregion

      #region Analysis Layer
      speckleGridSurfaces = speckleAnalysisModel.elements.FindAll(so => so is GSAGridSurface).Select(so => (GSAGridSurface)so).ToList();

      //Checks - grid surface 1
      Assert.Equal("grid surface 1", speckleGridSurfaces[0].applicationId);
      Assert.Equal(gsaGridSurfaces[0].Name, speckleGridSurfaces[0].name);
      Assert.Equal(gsaGridSurfaces[0].Index.Value, speckleGridSurfaces[0].nativeId);
      Assert.True(speckleGridSurfaces[0].gridPlane.axis.definition.IsGlobal());
      Assert.Equal(gsaGridSurfaces[0].Tolerance.Value, speckleGridSurfaces[0].tolerance);
      Assert.Equal(gsaGridSurfaces[0].Angle.Value, speckleGridSurfaces[0].spanDirection);
      Assert.Equal(LoadExpansion.PlaneAspect, speckleGridSurfaces[0].loadExpansion);
      Assert.Equal(GridSurfaceSpanType.OneWay, speckleGridSurfaces[0].span);
      Assert.Equal(gsaGridSurfaces[0].ElementIndices.Count(), speckleGridSurfaces[0].elements.Count());
      Assert.Equal("beam 1", speckleGridSurfaces[0].elements[0].applicationId);

      //Checks - grid surface 2
      Assert.Equal("grid surface 2", speckleGridSurfaces[1].applicationId);
      Assert.Equal(gsaGridSurfaces[1].Name, speckleGridSurfaces[1].name);
      Assert.Equal(gsaGridSurfaces[1].Index.Value, speckleGridSurfaces[1].nativeId);
      Assert.Equal("grid plane 1", speckleGridSurfaces[1].gridPlane.applicationId);
      Assert.Equal(gsaGridSurfaces[1].Tolerance.Value, speckleGridSurfaces[1].tolerance);
      Assert.Equal(gsaGridSurfaces[1].Angle.Value, speckleGridSurfaces[1].spanDirection);
      Assert.Equal(LoadExpansion.PlaneAspect, speckleGridSurfaces[1].loadExpansion);
      Assert.Equal(GridSurfaceSpanType.TwoWay, speckleGridSurfaces[1].span);
      Assert.Equal(gsaGridSurfaces[0].ElementIndices.Count(), speckleGridSurfaces[1].elements.Count());
      Assert.Equal("quad 1", speckleGridSurfaces[1].elements[0].applicationId);
      #endregion
    }

    [Fact]
    public void PolylineToSpeckle()
    {
      //Define GSA objects
      //These should be in order that respects the type dependency tree (which is only available in the GSAProxy library, which isn't referenced yet
      var gsaRecords = new List<GsaRecord>();

      //Generation #1: Types with no other dependencies - the leaves of the tree
      gsaRecords.Add(GsaGridPlaneExamples(1, "grid plane 1").FirstOrDefault());

      //Gen #2
      var gsaPolylines = GsaPolylineExamples(2, "polyline 1", "polyline 2");
      gsaRecords.AddRange(gsaPolylines);

      Instance.GsaModel.Cache.Upsert(gsaRecords);

      foreach (var record in gsaRecords)
      {
        var speckleObjects = converter.ConvertToSpeckle(new List<object> { record });
        Assert.Empty(converter.ConversionErrors);

        Instance.GsaModel.Cache.SetSpeckleObjects(record, speckleObjects.ToDictionary(so => so.applicationId, so => (object)so));
      }

      Assert.True(Instance.GsaModel.Cache.GetSpeckleObjects(out var structuralObjects));

      Assert.NotEmpty(structuralObjects);
      Assert.Contains(structuralObjects, so => so is GSAPolyline);

      var specklePolylines = structuralObjects.FindAll(so => so is GSAPolyline).Select(so => (GSAPolyline)so).ToList();
      Assert.Equal(gsaPolylines.Count(), specklePolylines.Count());

      //Checks - polyline 1
      Assert.Equal("polyline 1", specklePolylines[0].applicationId);
      Assert.Equal(gsaPolylines[0].Name, specklePolylines[0].name);
      Assert.Equal(gsaPolylines[0].Index.Value, specklePolylines[0].nativeId);
      Assert.Equal(gsaPolylines[0].Colour.ToString(), specklePolylines[0].colour);
      Assert.Null(specklePolylines[0].gridPlane);
      Assert.Equal(gsaPolylines[0].Units, specklePolylines[0].units);
      Assert.Equal(new List<double>() { 1, 2, 0, 3, 4, 0, 5, 6, 0, 7, 8, 0 }, specklePolylines[0].value);

      //Checks - polyline 2
      Assert.Equal("polyline 2", specklePolylines[1].applicationId);
      Assert.Equal(gsaPolylines[1].Name, specklePolylines[1].name);
      Assert.Equal(gsaPolylines[1].Index.Value, specklePolylines[1].nativeId);
      Assert.Equal(gsaPolylines[1].Colour.ToString(), specklePolylines[1].colour);
      Assert.Equal("grid plane 1", specklePolylines[1].gridPlane.applicationId);
      Assert.Equal(gsaPolylines[1].Units, specklePolylines[1].units);
      Assert.Equal(gsaPolylines[1].Values, specklePolylines[1].value);
    }
    #endregion

    #region Loading
    [Fact]
    public void LoadCaseToSpeckle()
    {
      //Define GSA objects
      //These should be in order that respects the type dependency tree (which is only available in the GSAProxy library, which isn't referenced yet
      var gsaRecords = new List<GsaRecord>();

      //Generation #1: Types with no other dependencies - the leaves of the tree
      var gsaLoadCase = GsaLoadCaseExamples(2, "load case 1", "load case 2");
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
      Assert.Equal(gsaLoadCase[0].Index.Value, speckleLoadCases[0].nativeId);
      Assert.Equal(gsaLoadCase[0].Title, speckleLoadCases[0].name);
      Assert.Equal(LoadType.Dead, speckleLoadCases[0].loadType);
      Assert.Equal(ActionType.Permanent, speckleLoadCases[0].actionType);
      Assert.Equal(gsaLoadCase[0].Category.ToString(), speckleLoadCases[0].description);
      Assert.Equal(LoadDirection2D.Z, speckleLoadCases[0].direction);
      Assert.Equal(gsaLoadCase[0].Include.ToString(), speckleLoadCases[0].include);
      Assert.False(speckleLoadCases[0].bridge);

      //Checks - Load case 2
      Assert.Equal("load case 2", speckleLoadCases[1].applicationId);
      Assert.Equal(gsaLoadCase[1].Index.Value, speckleLoadCases[1].nativeId);
      Assert.Equal(gsaLoadCase[1].Title, speckleLoadCases[1].name);
      Assert.Equal(LoadType.Live, speckleLoadCases[1].loadType);
      Assert.Equal(ActionType.Variable, speckleLoadCases[1].actionType);
      Assert.Equal(gsaLoadCase[1].Category.ToString(), speckleLoadCases[1].description);
      Assert.Equal(LoadDirection2D.Z, speckleLoadCases[1].direction);
      Assert.Equal(gsaLoadCase[1].Include.ToString(), speckleLoadCases[1].include);
      Assert.False(speckleLoadCases[1].bridge);
    }

    [Fact]
    public void AnalysisCaseToSpeckle()
    {
      //Define GSA objects
      //These should be in order that respects the type dependency tree (which is only available in the GSAProxy library, which isn't referenced yet
      var gsaRecords = new List<GsaRecord>();

      //Generation #1: Types with no other dependencies - the leaves of the tree
      gsaRecords.AddRange(GsaLoadCaseExamples(2, "load case 1", "load case 2"));

      //Gen #2
      //gsaRecords.Add(GsaTaskExamples(1, "task 1").First());

      //Gen #3:
      var gsaAnalysisCases = GsaAnalysisCaseExamples(2, "analysis case 1", "analysis case 2");
      gsaRecords.AddRange(gsaAnalysisCases);

      Instance.GsaModel.Cache.Upsert(gsaRecords);

      foreach (var record in gsaRecords)
      {
        var speckleObjects = converter.ConvertToSpeckle(new List<object> { record });
        Assert.Empty(converter.ConversionErrors);

        Instance.GsaModel.Cache.SetSpeckleObjects(record, speckleObjects.ToDictionary(so => so.applicationId, so => (object)so));
      }

      Assert.True(Instance.GsaModel.Cache.GetSpeckleObjects(out var structuralObjects));

      Assert.NotEmpty(structuralObjects);
      Assert.Contains(structuralObjects, so => so is GSAAnalysisCase);

      var speckleAnalysisCases = structuralObjects.FindAll(so => so is GSAAnalysisCase).Select(so => (GSAAnalysisCase)so).ToList();

      //Checks - Analysis case 1
      Assert.Equal("analysis case 1", speckleAnalysisCases[0].applicationId);
      Assert.Equal(gsaAnalysisCases[0].Index.Value, speckleAnalysisCases[0].nativeId);
      Assert.Equal(gsaAnalysisCases[0].Name, speckleAnalysisCases[0].name);
      Assert.Equal(1, speckleAnalysisCases[0].loadCases.Count());
      Assert.Equal("load case 1", speckleAnalysisCases[0].loadCases[0].applicationId);
      Assert.Equal(1, speckleAnalysisCases[0].loadFactors.Count());
      Assert.Equal(1, speckleAnalysisCases[0].loadFactors[0]);
      Assert.Null(speckleAnalysisCases[0].task); //TODO: update once TASK keyword is added to interim schema

      //Checks - Analysis case 2
      Assert.Equal("analysis case 2", speckleAnalysisCases[1].applicationId);
      Assert.Equal(gsaAnalysisCases[1].Index.Value, speckleAnalysisCases[1].nativeId);
      Assert.Equal(gsaAnalysisCases[1].Name, speckleAnalysisCases[1].name);
      Assert.Equal(2, speckleAnalysisCases[1].loadCases.Count());
      Assert.Equal("load case 1", speckleAnalysisCases[1].loadCases[0].applicationId);
      Assert.Equal("load case 2", speckleAnalysisCases[1].loadCases[1].applicationId);
      Assert.Equal(2, speckleAnalysisCases[1].loadFactors.Count());
      Assert.Equal(1.2, speckleAnalysisCases[1].loadFactors[0]);
      Assert.Equal(1.5, speckleAnalysisCases[1].loadFactors[1]);
      Assert.Null(speckleAnalysisCases[1].task); //TODO: update once TASK keyword is added to interim schema
    }

    [Fact]
    public void LoadCombinationToSpeckle()
    {
      //Define GSA objects
      //These should be in order that respects the type dependency tree (which is only available in the GSAProxy library, which isn't referenced yet
      var gsaRecords = new List<GsaRecord>();

      //Generation #1: Types with no other dependencies - the leaves of the tree
      gsaRecords.AddRange(GsaLoadCaseExamples(2, "load case 1", "load case 2"));

      //Gen #2
      gsaRecords.AddRange(GsaAnalysisCaseExamples(2, "analysis case 1", "analysis case 2"));

      //Gen #3
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
      Assert.Equal(new List<double> { 1, 0.2 }, speckleLoadCombination[0].loadFactors);
      Assert.Equal(2, speckleLoadCombination[0].loadCases.Count());
      Assert.Equal("analysis case 1", speckleLoadCombination[0].loadCases[0].applicationId);
      Assert.Equal("analysis case 2", speckleLoadCombination[0].loadCases[1].applicationId);
      Assert.Equal(CombinationType.LinearAdd, speckleLoadCombination[0].combinationType);
      Assert.Equal(gsaCombinations[0].Index.Value, speckleLoadCombination[0].nativeId);

      //Checks - Combination 2
      Assert.Equal("combo 2", speckleLoadCombination[1].applicationId);
      Assert.Equal(gsaCombinations[1].Name, speckleLoadCombination[1].name);
      Assert.Equal(new List<double> { 1.3, 1, 1 }, speckleLoadCombination[1].loadFactors);
      Assert.Equal(3, speckleLoadCombination[1].loadCases.Count());
      Assert.Equal("combo 1", speckleLoadCombination[1].loadCases[0].applicationId);
      Assert.Equal("analysis case 1", speckleLoadCombination[1].loadCases[1].applicationId);
      Assert.Equal("analysis case 2", speckleLoadCombination[1].loadCases[2].applicationId);
      Assert.Equal(CombinationType.LinearAdd, speckleLoadCombination[1].combinationType);
      Assert.Equal(gsaCombinations[1].Index.Value, speckleLoadCombination[1].nativeId);
    }

    [Fact]
    public void LoadFaceToSpeckle()
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

      //Convert
      Instance.GsaModel.StreamLayer = GSALayer.Both;
      Instance.GsaModel.StreamSendConfig = StreamContentConfig.ModelAndResults;
      var speckleObjects = converter.ConvertToSpeckle(gsaRecords.Select(i => (object)i).ToList());

      //Get speckle results
      Assert.NotEmpty(speckleObjects);
      Assert.Contains(speckleObjects, so => so is Model);
      var speckleModels = speckleObjects.FindAll(so => so is Model).Select(so => (Model)so).ToList();
      List<GSALoadFace> speckleFaceLoads;

      foreach (var m in speckleModels)
      {
        Assert.NotNull(m);
        Assert.NotEmpty(m.loads);
        Assert.Contains(m.loads, o => o is GSALoadFace);
        speckleFaceLoads = m.loads.FindAll(so => so is GSALoadFace).Select(so => (GSALoadFace)so).ToList();

        //Checks - Load 1
        Assert.Equal("load 2d face 1", speckleFaceLoads[0].applicationId);
        Assert.Equal("1", speckleFaceLoads[0].name);
        //Assert.Single(speckleFaceLoads[0].elements);
        //Assert.Equal("element 1", speckleFaceLoads[0].elements[0].applicationId);
        Assert.Equal(FaceLoadType.Constant, speckleFaceLoads[0].loadType);
        Assert.Equal(LoadDirection2D.Z, speckleFaceLoads[0].direction);
        Assert.Equal(LoadAxisType.Global, speckleFaceLoads[0].loadAxisType);
        Assert.False(speckleFaceLoads[0].isProjected);
        Assert.Equal(gsaLoad2dFace[0].Values, speckleFaceLoads[0].values);
        Assert.Null(speckleFaceLoads[0].loadAxis);
        Assert.Null(speckleFaceLoads[0].positions);
        Assert.Equal(gsaLoad2dFace[0].Index.Value, speckleFaceLoads[0].nativeId);

        //Checks - Load 2
        Assert.Equal("load 2d face 2", speckleFaceLoads[1].applicationId);
        Assert.Equal("2", speckleFaceLoads[1].name);
        //Assert.Single(speckleFaceLoads[1].elements);
        //Assert.Equal("element 2", speckleFaceLoads[1].elements[0].applicationId);
        Assert.Equal(FaceLoadType.Point, speckleFaceLoads[1].loadType);
        Assert.Equal(LoadDirection2D.X, speckleFaceLoads[1].direction);
        Assert.Equal(LoadAxisType.Global, speckleFaceLoads[1].loadAxisType);
        Assert.False(speckleFaceLoads[1].isProjected);
        Assert.Equal(gsaLoad2dFace[1].Values, speckleFaceLoads[1].values);
        Assert.Equal("axis 1", speckleFaceLoads[1].loadAxis.applicationId);
        Assert.Equal(new List<double>() { 0, 0 }, speckleFaceLoads[1].positions);
        Assert.Equal(gsaLoad2dFace[1].Index.Value, speckleFaceLoads[1].nativeId);

        //Checks - Load 3
        Assert.Equal("load 2d face 3", speckleFaceLoads[2].applicationId);
        Assert.Equal("3", speckleFaceLoads[2].name);
        //Assert.Single(speckleFaceLoads[2].elements);
        //Assert.Equal("element 3", speckleFaceLoads[2].elements[0].applicationId);
        Assert.Equal(FaceLoadType.Variable, speckleFaceLoads[2].loadType);
        Assert.Equal(LoadDirection2D.Y, speckleFaceLoads[2].direction);
        Assert.Equal(LoadAxisType.Local, speckleFaceLoads[2].loadAxisType);
        Assert.False(speckleFaceLoads[2].isProjected);
        Assert.Equal(gsaLoad2dFace[2].Values, speckleFaceLoads[2].values);
        Assert.Null(speckleFaceLoads[2].loadAxis);
        Assert.Null(speckleFaceLoads[2].positions);
        Assert.Equal(gsaLoad2dFace[2].Index.Value, speckleFaceLoads[2].nativeId);
      }
    }

    [Fact]
    public void LoadBeamToSpeckle()
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

      //Convert
      Instance.GsaModel.StreamLayer = GSALayer.Both;
      Instance.GsaModel.StreamSendConfig = StreamContentConfig.ModelAndResults;
      var speckleObjects = converter.ConvertToSpeckle(gsaRecords.Select(i => (object)i).ToList());

      //Get speckle results
      Assert.NotEmpty(speckleObjects);
      Assert.Contains(speckleObjects, so => so is Model);
      var speckleModels = speckleObjects.FindAll(so => so is Model).Select(so => (Model)so).ToList();
      List<GSALoadBeam> speckleBeamLoads;

      foreach (var m in speckleModels)
      {
        var isAnalysis = m.layerDescription.ToLower().Contains("analysis");
        Assert.NotNull(m);
        Assert.NotEmpty(m.loads);
        Assert.Contains(m.loads, o => o is GSALoadBeam);
        speckleBeamLoads = m.loads.FindAll(so => so is GSALoadBeam).Select(so => (GSALoadBeam)so).ToList();

        //Checks - Element 1
        Assert.Equal("load beam 1", speckleBeamLoads[0].applicationId);
        Assert.Equal("1", speckleBeamLoads[0].name);
        Assert.Equal("load case 1", speckleBeamLoads[0].loadCase.applicationId);
        if (isAnalysis)
        {
          Assert.Single(speckleBeamLoads[0].elements);
          Assert.Equal("element 1", speckleBeamLoads[0].elements[0].applicationId);
        }
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
        if (isAnalysis)
        {
          Assert.Single(speckleBeamLoads[1].elements);
          Assert.Equal("element 2", speckleBeamLoads[1].elements[0].applicationId);
        }
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
        if (isAnalysis)
        {
          Assert.Single(speckleBeamLoads[2].elements);
          Assert.Equal("element 1", speckleBeamLoads[2].elements[0].applicationId);
        }
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
    }

    [Fact]
    public void LoadNodeToSpeckle()
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
      gsaRecords.AddRange(GsaNodeExamples(2, "node 1", "node 2"));

      //Gen #3
      var gsaLoadNodes = GsaLoadNodeExamples(2, "load node 1", "load node 2");
      gsaRecords.AddRange(gsaLoadNodes);

      Instance.GsaModel.Cache.Upsert(gsaRecords);

      foreach (var record in gsaRecords)
      {
        var speckleObjects = converter.ConvertToSpeckle(new List<object> { record });
        Assert.Empty(converter.ConversionErrors);

        Instance.GsaModel.Cache.SetSpeckleObjects(record, speckleObjects.ToDictionary(so => so.applicationId, so => (object)so));
      }

      Assert.True(Instance.GsaModel.Cache.GetSpeckleObjects(out var structuralObjects));

      Assert.NotEmpty(structuralObjects);
      Assert.Contains(structuralObjects, so => so is GSALoadNode);

      var speckleNodeLoads = structuralObjects.FindAll(so => so is GSALoadNode).Select(so => (GSALoadNode)so).ToList();

      //Checks - Load 1
      Assert.Equal("load node 1", speckleNodeLoads[0].applicationId);
      Assert.Equal(gsaLoadNodes[0].Name, speckleNodeLoads[0].name);
      Assert.Equal("load case 1", speckleNodeLoads[0].loadCase.applicationId);  //assume conversion of load case is tested elsewhere
      Assert.Single(speckleNodeLoads[0].nodes);
      Assert.Equal("node 1", speckleNodeLoads[0].nodes[0].applicationId); //assume conversion of node is tested elsewhere
      Assert.True(speckleNodeLoads[0].loadAxis.definition.IsGlobal());
      Assert.Equal(LoadDirection.Z, speckleNodeLoads[0].direction);
      Assert.Equal(gsaLoadNodes[0].Value, speckleNodeLoads[0].value);
      Assert.Equal(gsaLoadNodes[0].Index.Value, speckleNodeLoads[0].nativeId);

      //Checks - Load 2
      Assert.Equal("load node 2", speckleNodeLoads[1].applicationId);
      Assert.Equal(gsaLoadNodes[1].Name, speckleNodeLoads[1].name);
      Assert.Equal("load case 1", speckleNodeLoads[1].loadCase.applicationId);  //assume conversion of load case is tested elsewhere
      Assert.Single(speckleNodeLoads[1].nodes);
      Assert.Equal("node 2", speckleNodeLoads[1].nodes[0].applicationId); //assume conversion of node is tested elsewhere
      Assert.Equal("axis 1", speckleNodeLoads[1].loadAxis.applicationId); //assume conversion of axis is tested elsewhere
      Assert.Equal(LoadDirection.X, speckleNodeLoads[1].direction);
      Assert.Equal(gsaLoadNodes[1].Value, speckleNodeLoads[1].value);
      Assert.Equal(gsaLoadNodes[1].Index.Value, speckleNodeLoads[1].nativeId);
    }

    [Fact]
    public void LoadGravityToSpeckle()
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

      //Convert
      Instance.GsaModel.StreamLayer = GSALayer.Both;
      Instance.GsaModel.StreamSendConfig = StreamContentConfig.ModelAndResults;
      var speckleObjects = converter.ConvertToSpeckle(gsaRecords.Select(i => (object)i).ToList());

      //Get speckle results
      Assert.NotEmpty(speckleObjects);
      Assert.Contains(speckleObjects, so => so is Model);
      var speckleModels = speckleObjects.FindAll(so => so is Model).Select(so => (Model)so).ToList();
      GSALoadGravity speckleGravityLoad;

      foreach (var m in speckleModels)
      {
        Assert.NotNull(m);
        Assert.NotEmpty(m.loads);
        Assert.Contains(m.loads, o => o is GSALoadGravity);
        speckleGravityLoad = m.loads.FindAll(so => so is GSALoadGravity).Select(so => (GSALoadGravity)so).ToList().First();

        //Checks
        Assert.Equal("load gravity 1", speckleGravityLoad.applicationId);
        Assert.Equal("load case 1", speckleGravityLoad.loadCase.applicationId);
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

        if (m.layerDescription == "Design")
        {
          Assert.Null(speckleGravityLoad.elements);
        }
        else if (m.layerDescription == "Analysis")
        {
          Assert.Equal(2, speckleGravityLoad.elements.Count());
          Assert.Equal("element 1", speckleGravityLoad.elements[0].applicationId);
          Assert.Equal("element 2", speckleGravityLoad.elements[1].applicationId);
        }
      }
    }

    [Fact]
    public void LoadGridPointToSpeckle()
    {
      //Define GSA objects
      //These should be in order that respects the type dependency tree (which is only available in the GSAProxy library, which isn't referenced yet
      var gsaRecords = new List<GsaRecord>();

      //Generation #1: Types with no other dependencies - the leaves of the tree
      gsaRecords.AddRange(GsaLoadCaseExamples(2, "load case 1", "load case 2"));
      gsaRecords.Add(GsaAxisExample("axis 1"));
      gsaRecords.Add(GsaMatSteelExample("steel material 1"));
      gsaRecords.Add(GsaPropMassExample("property mass 1"));
      gsaRecords.Add(GsaPropSprExample("property spring 1"));

      //Gen #2
      gsaRecords.AddRange(GsaNodeExamples(2, "node 1", "node 2"));
      gsaRecords.Add(GsaCatalogueSectionExample("section 1"));
      gsaRecords.Add(GsaGridPlaneExamples(1, "grid plane 1").FirstOrDefault());

      //Gen #3
      gsaRecords.Add(GsaElement1dExamples(1, "beam 1").FirstOrDefault());

      //Gen #4
      gsaRecords.Add(GsaGridSurfaceExamples(1, "grid surface 1").FirstOrDefault());

      //Gen #5
      var gsaLoadGridPoints = GsaLoadGridPointExamples(2, "load grid point 1", "load grid point 2");
      gsaRecords.AddRange(gsaLoadGridPoints);

      Instance.GsaModel.Cache.Upsert(gsaRecords);

      //Convert
      Instance.GsaModel.StreamLayer = GSALayer.Both;
      Instance.GsaModel.StreamSendConfig = StreamContentConfig.ModelAndResults;
      var speckleObjects = converter.ConvertToSpeckle(gsaRecords.Select(i => (object)i).ToList());

      //Get speckle results
      Assert.NotEmpty(speckleObjects);
      Assert.Contains(speckleObjects, so => so is Model);
      var speckleModels = speckleObjects.FindAll(so => so is Model).Select(so => (Model)so).ToList();
      List<GSALoadGridPoint> speckleGridPointLoads;

      foreach (var m in speckleModels)
      {
        Assert.NotNull(m);
        Assert.NotEmpty(m.loads);
        Assert.Contains(m.loads, o => o is GSALoadGridPoint);
        speckleGridPointLoads = m.loads.FindAll(so => so is GSALoadGridPoint).Select(so => (GSALoadGridPoint)so).ToList();
        Assert.Equal(gsaLoadGridPoints.Count(), speckleGridPointLoads.Count());

        //Checks - Load grid point 1
        Assert.Equal("load grid point 1", speckleGridPointLoads[0].applicationId);
        Assert.Equal(gsaLoadGridPoints[0].Index.Value, speckleGridPointLoads[0].nativeId);
        Assert.Equal(gsaLoadGridPoints[0].Name, speckleGridPointLoads[0].name);
        Assert.Equal("grid surface 1", speckleGridPointLoads[0].gridSurface.applicationId);
        Assert.Equal(gsaLoadGridPoints[0].X.Value, speckleGridPointLoads[0].position.x);
        Assert.Equal(gsaLoadGridPoints[0].Y.Value, speckleGridPointLoads[0].position.y);
        Assert.Equal("load case 1", speckleGridPointLoads[0].loadCase.applicationId);
        Assert.True(speckleGridPointLoads[0].loadAxis.definition.IsGlobal());
        Assert.Equal(LoadDirection2D.Z, speckleGridPointLoads[0].direction);
        Assert.Equal(gsaLoadGridPoints[0].Value.Value, speckleGridPointLoads[0].value);

        //Checks - Load grid point 2
        Assert.Equal("load grid point 2", speckleGridPointLoads[1].applicationId);
        Assert.Equal(gsaLoadGridPoints[1].Index.Value, speckleGridPointLoads[1].nativeId);
        Assert.Equal(gsaLoadGridPoints[1].Name, speckleGridPointLoads[1].name);
        Assert.Equal("grid surface 1", speckleGridPointLoads[1].gridSurface.applicationId);
        Assert.Equal(gsaLoadGridPoints[1].X.Value, speckleGridPointLoads[1].position.x);
        Assert.Equal(gsaLoadGridPoints[1].Y.Value, speckleGridPointLoads[1].position.y);
        Assert.Equal("load case 2", speckleGridPointLoads[1].loadCase.applicationId);
        Assert.Equal("axis 1", speckleGridPointLoads[1].loadAxis.applicationId);
        Assert.Equal(LoadDirection2D.Z, speckleGridPointLoads[1].direction);
        Assert.Equal(gsaLoadGridPoints[1].Value.Value, speckleGridPointLoads[1].value);
      }      
    }

    [Fact]
    public void LoadGridLineToSpeckle()
    {
      //Define GSA objects
      //These should be in order that respects the type dependency tree (which is only available in the GSAProxy library, which isn't referenced yet
      var gsaRecords = new List<GsaRecord>();

      //Generation #1: Types with no other dependencies - the leaves of the tree
      gsaRecords.AddRange(GsaLoadCaseExamples(2, "load case 1", "load case 2"));
      gsaRecords.Add(GsaAxisExample("axis 1"));
      gsaRecords.Add(GsaMatSteelExample("steel material 1"));
      gsaRecords.Add(GsaPropMassExample("property mass 1"));
      gsaRecords.Add(GsaPropSprExample("property spring 1"));

      //Gen #2
      gsaRecords.AddRange(GsaNodeExamples(2, "node 1", "node 2"));
      gsaRecords.Add(GsaCatalogueSectionExample("section 1"));
      gsaRecords.Add(GsaGridPlaneExamples(1, "grid plane 1").FirstOrDefault());

      //Gen #3
      gsaRecords.Add(GsaPolylineExamples(1, "polyline 1").FirstOrDefault());
      gsaRecords.Add(GsaElement1dExamples(1, "beam 1").FirstOrDefault());

      //Gen #4
      gsaRecords.Add(GsaGridSurfaceExamples(1, "grid surface 1").FirstOrDefault());

      //Gen #5
      var gsaLoadGridLines = GsaLoadGridLineExamples(2, "load grid line 1", "load grid line 2");
      gsaRecords.AddRange(gsaLoadGridLines);

      Instance.GsaModel.Cache.Upsert(gsaRecords);

      //Convert
      Instance.GsaModel.StreamLayer = GSALayer.Both;
      Instance.GsaModel.StreamSendConfig = StreamContentConfig.ModelAndResults;
      var speckleObjects = converter.ConvertToSpeckle(gsaRecords.Select(i => (object)i).ToList());

      //Get speckle results
      Assert.NotEmpty(speckleObjects);
      Assert.Contains(speckleObjects, so => so is Model);
      var speckleModels = speckleObjects.FindAll(so => so is Model).Select(so => (Model)so).ToList();
      List<GSALoadGridLine> speckleGridLineLoads;

      foreach (var m in speckleModels)
      {
        Assert.NotNull(m);
        Assert.NotEmpty(m.loads);
        Assert.Contains(m.loads, o => o is GSALoadGridLine);
        speckleGridLineLoads = m.loads.FindAll(so => so is GSALoadGridLine).Select(so => (GSALoadGridLine)so).ToList();
        Assert.Equal(gsaLoadGridLines.Count(), speckleGridLineLoads.Count());

        //Checks - Load grid line 1
        Assert.Equal("load grid line 1", speckleGridLineLoads[0].applicationId);
        Assert.Equal(gsaLoadGridLines[0].Index.Value, speckleGridLineLoads[0].nativeId);
        Assert.Equal(gsaLoadGridLines[0].Name, speckleGridLineLoads[0].name);
        Assert.Equal("grid surface 1", speckleGridLineLoads[0].gridSurface.applicationId);
        Assert.Equal("polyline 1", speckleGridLineLoads[0].polyline.applicationId);
        Assert.Equal("load case 1", speckleGridLineLoads[0].loadCase.applicationId);
        Assert.True(speckleGridLineLoads[0].loadAxis.definition.IsGlobal());
        Assert.Equal(gsaLoadGridLines[0].Projected, speckleGridLineLoads[0].isProjected);
        Assert.Equal(LoadDirection2D.Z, speckleGridLineLoads[0].direction);
        Assert.Equal(2, speckleGridLineLoads[0].values.Count());
        Assert.Equal(gsaLoadGridLines[0].Value1.Value, speckleGridLineLoads[0].values[0]);
        Assert.Equal(gsaLoadGridLines[0].Value2.Value, speckleGridLineLoads[0].values[1]);

        //Checks - Load grid line 2
        Assert.Equal("load grid line 2", speckleGridLineLoads[1].applicationId);
        Assert.Equal(gsaLoadGridLines[1].Index.Value, speckleGridLineLoads[1].nativeId);
        Assert.Equal(gsaLoadGridLines[1].Name, speckleGridLineLoads[1].name);
        Assert.Equal("grid surface 1", speckleGridLineLoads[1].gridSurface.applicationId);
        Assert.Equal(new List<double>() { 0, 0, 0, 1, 0, 0, 1, 1, 0, 0, 1, 0 }, speckleGridLineLoads[1].polyline.value);
        Assert.Equal("load case 2", speckleGridLineLoads[1].loadCase.applicationId);
        Assert.Equal("axis 1", speckleGridLineLoads[1].loadAxis.applicationId);
        Assert.Equal(gsaLoadGridLines[1].Projected, speckleGridLineLoads[1].isProjected);
        Assert.Equal(LoadDirection2D.Z, speckleGridLineLoads[1].direction);
        Assert.Equal(2, speckleGridLineLoads[1].values.Count());
        Assert.Equal(gsaLoadGridLines[1].Value1.Value, speckleGridLineLoads[1].values[0]);
        Assert.Equal(gsaLoadGridLines[1].Value2.Value, speckleGridLineLoads[1].values[1]);
      }
    }

    [Fact]
    public void LoadGridAreaToSpeckle()
    {
      //Define GSA objects
      //These should be in order that respects the type dependency tree (which is only available in the GSAProxy library, which isn't referenced yet
      var gsaRecords = new List<GsaRecord>();

      //Generation #1: Types with no other dependencies - the leaves of the tree
      gsaRecords.AddRange(GsaLoadCaseExamples(2, "load case 1", "load case 2"));
      gsaRecords.Add(GsaAxisExample("axis 1"));
      gsaRecords.Add(GsaMatSteelExample("steel material 1"));
      gsaRecords.Add(GsaPropMassExample("property mass 1"));
      gsaRecords.Add(GsaPropSprExample("property spring 1"));

      //Gen #2
      gsaRecords.AddRange(GsaNodeExamples(2, "node 1", "node 2"));
      gsaRecords.Add(GsaCatalogueSectionExample("section 1"));
      gsaRecords.Add(GsaGridPlaneExamples(1, "grid plane 1").FirstOrDefault());

      //Gen #3
      gsaRecords.Add(GsaPolylineExamples(1, "polyline 1").FirstOrDefault());
      gsaRecords.Add(GsaElement1dExamples(1, "beam 1").FirstOrDefault());

      //Gen #4
      gsaRecords.Add(GsaGridSurfaceExamples(1, "grid surface 1").FirstOrDefault());

      //Gen #5
      var gsaLoadGridAreas = GsaLoadGridAreaExamples(2, "load grid area 1", "load grid area 2");
      gsaRecords.AddRange(gsaLoadGridAreas);

      Instance.GsaModel.Cache.Upsert(gsaRecords);

      //Convert
      Instance.GsaModel.StreamLayer = GSALayer.Both;
      Instance.GsaModel.StreamSendConfig = StreamContentConfig.ModelAndResults;
      var speckleObjects = converter.ConvertToSpeckle(gsaRecords.Select(i => (object)i).ToList());

      //Get speckle results
      Assert.NotEmpty(speckleObjects);
      Assert.Contains(speckleObjects, so => so is Model);
      var speckleModels = speckleObjects.FindAll(so => so is Model).Select(so => (Model)so).ToList();
      List<GSALoadGridArea> speckleGridAreaLoads;

      foreach (var m in speckleModels)
      {
        Assert.NotNull(m);
        Assert.NotEmpty(m.loads);
        Assert.Contains(m.loads, o => o is GSALoadGridArea);
        speckleGridAreaLoads = m.loads.FindAll(so => so is GSALoadGridArea).Select(so => (GSALoadGridArea)so).ToList();
        Assert.Equal(gsaLoadGridAreas.Count(), speckleGridAreaLoads.Count());

        //Checks - Load grid area 1
        Assert.Equal("load grid area 1", speckleGridAreaLoads[0].applicationId);
        Assert.Equal(gsaLoadGridAreas[0].Index.Value, speckleGridAreaLoads[0].nativeId);
        Assert.Equal(gsaLoadGridAreas[0].Name, speckleGridAreaLoads[0].name);
        Assert.True(speckleGridAreaLoads[0].loadAxis.definition.IsGlobal());
        Assert.Equal(gsaLoadGridAreas[0].Projected, speckleGridAreaLoads[0].isProjected);
        Assert.Equal(LoadDirection2D.Z, speckleGridAreaLoads[0].direction);
        Assert.Null(speckleGridAreaLoads[0].polyline);
        Assert.Equal("grid surface 1", speckleGridAreaLoads[0].gridSurface.applicationId);
        Assert.Equal("load case 1", speckleGridAreaLoads[0].loadCase.applicationId);
        Assert.Equal(gsaLoadGridAreas[0].Value.Value, speckleGridAreaLoads[0].value);

        //Checks - Load grid area 2
        Assert.Equal("load grid area 2", speckleGridAreaLoads[1].applicationId);
        Assert.Equal(gsaLoadGridAreas[1].Index.Value, speckleGridAreaLoads[1].nativeId);
        Assert.Equal(gsaLoadGridAreas[1].Name, speckleGridAreaLoads[1].name);
        Assert.Equal("axis 1", speckleGridAreaLoads[1].loadAxis.applicationId);
        Assert.Equal(gsaLoadGridAreas[1].Projected, speckleGridAreaLoads[1].isProjected);
        Assert.Equal(LoadDirection2D.Z, speckleGridAreaLoads[1].direction);
        Assert.Equal("polyline 1", speckleGridAreaLoads[1].polyline.applicationId);
        Assert.Equal("grid surface 1", speckleGridAreaLoads[1].gridSurface.applicationId);
        Assert.Equal("load case 2", speckleGridAreaLoads[1].loadCase.applicationId);
        Assert.Equal(gsaLoadGridAreas[1].Value.Value, speckleGridAreaLoads[1].value);
      }
    }

    [Fact]
    public void LoadThermal2dToSpeckle()
    {
      //Define GSA objects
      //These should be in order that respects the type dependency tree (which is only available in the GSAProxy library, which isn't referenced yet
      var gsaRecords = new List<GsaRecord>();

      //Generation #1: Types with no other dependencies - the leaves of the tree
      gsaRecords.AddRange(GsaLoadCaseExamples(2, "load case 1", "load case 2"));
      gsaRecords.Add(GsaAxisExample("axis 1"));
      gsaRecords.Add(GsaMatSteelExample("steel material 1"));
      gsaRecords.Add(GsaPropMassExample("property mass 1"));
      gsaRecords.Add(GsaPropSprExample("property spring 1"));

      //Gen #2
      gsaRecords.Add(GsaProp2dExample("property 2D 1"));
      gsaRecords.AddRange(GsaNodeExamples(4, "node 1", "node 2", "node 3", "node 4"));

      //Gen #3
      gsaRecords.Add(GsaElement2dExamples(1, "element 1").FirstOrDefault());

      //Gen #4
      var gsaLoadThermal = GsaLoad2dThermalExamples(2, "load thermal 1", "load thermal 2");
      gsaRecords.AddRange(gsaLoadThermal);

      Instance.GsaModel.Cache.Upsert(gsaRecords);

      //Convert
      Instance.GsaModel.StreamLayer = GSALayer.Both;
      Instance.GsaModel.StreamSendConfig = StreamContentConfig.ModelAndResults;
      var speckleObjects = converter.ConvertToSpeckle(gsaRecords.Select(i => (object)i).ToList());

      //Get speckle results
      Assert.NotEmpty(speckleObjects);
      Assert.Contains(speckleObjects, so => so is Model);
      var speckleModels = speckleObjects.FindAll(so => so is Model).Select(so => (Model)so).ToList();
      List<GSALoadThermal2d> speckleThermalLoads;

      foreach (var m in speckleModels)
      {
        Assert.NotNull(m);
        Assert.NotEmpty(m.loads);
        Assert.Contains(m.loads, o => o is GSALoadThermal2d);
        speckleThermalLoads = m.loads.FindAll(so => so is GSALoadThermal2d).Select(so => (GSALoadThermal2d)so).ToList();
        Assert.Equal(gsaLoadThermal.Count(), speckleThermalLoads.Count());

        //Checks - Load thermal 1
        Assert.Equal("load thermal 1", speckleThermalLoads[0].applicationId);
        Assert.Equal(gsaLoadThermal[0].Index.Value, speckleThermalLoads[0].nativeId);
        Assert.Equal(gsaLoadThermal[0].Name, speckleThermalLoads[0].name);
        Assert.Equal("load case 1", speckleThermalLoads[0].loadCase.applicationId);
        Assert.Equal(Thermal2dLoadType.Uniform, speckleThermalLoads[0].type);
        Assert.Equal(gsaLoadThermal[0].Values, speckleThermalLoads[0].values);

        //Checks - Load thermal 2
        Assert.Equal("load thermal 2", speckleThermalLoads[1].applicationId);
        Assert.Equal(gsaLoadThermal[1].Index.Value, speckleThermalLoads[1].nativeId);
        Assert.Equal(gsaLoadThermal[1].Name, speckleThermalLoads[1].name);
        Assert.Equal("load case 2", speckleThermalLoads[1].loadCase.applicationId);
        Assert.Equal(Thermal2dLoadType.Gradient, speckleThermalLoads[1].type);
        Assert.Equal(gsaLoadThermal[1].Values, speckleThermalLoads[1].values);

        if (m.layerDescription == "Design")
        {
          Assert.Null(speckleThermalLoads[0].elements);
          Assert.Null(speckleThermalLoads[1].elements);
        }
        else if (m.layerDescription == "Analysis")
        {
          Assert.Equal(gsaLoadThermal[0].ElementIndices.Count(), speckleThermalLoads[0].elements.Count());
          Assert.Equal("element 1", speckleThermalLoads[0].elements[0].applicationId);
          Assert.Equal(gsaLoadThermal[1].ElementIndices.Count(), speckleThermalLoads[1].elements.Count());
          Assert.Equal("element 1", speckleThermalLoads[1].elements[0].applicationId);
        }
      }
    }
    #endregion

    #region Materials
    [Fact]
    public void ConcreteToSpeckle()
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
      Assert.Contains(structuralObjects, so => so is GSAConcrete);

      var speckleConcrete = (GSAConcrete)structuralObjects.FirstOrDefault(so => so is GSAConcrete);

      Assert.Equal("concrete 1", speckleConcrete.applicationId);
      Assert.Equal("", speckleConcrete.grade);
      Assert.Equal(MaterialType.Concrete, speckleConcrete.materialType);
      Assert.Equal("", speckleConcrete.designCode);
      Assert.Equal("", speckleConcrete.codeYear);
      Assert.Equal(gsaMatConcrete.Fc.Value, speckleConcrete.compressiveStrength);
      Assert.Equal(gsaMatConcrete.Mat.Rho.Value, speckleConcrete.density);
      Assert.Equal(gsaMatConcrete.Mat.E.Value, speckleConcrete.elasticModulus);
      Assert.Equal(gsaMatConcrete.Mat.G.Value, speckleConcrete.shearModulus);
      Assert.Equal(gsaMatConcrete.Mat.Nu.Value, speckleConcrete.poissonsRatio);
      Assert.Equal(gsaMatConcrete.Mat.Alpha.Value, speckleConcrete.thermalExpansivity);
      Assert.Equal(gsaMatConcrete.EpsU.Value, speckleConcrete.maxCompressiveStrain);
      Assert.Equal(gsaMatConcrete.Agg.Value, speckleConcrete.maxAggregateSize);
      Assert.Equal(gsaMatConcrete.Fcdt.Value, speckleConcrete.tensileStrength);
      Assert.Equal(0, speckleConcrete.flexuralStrength);
    }

    [Fact]
    public void SteelToSpeckle()
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
      Assert.Contains(structuralObjects, so => so is GSASteel);

      var speckleSteel = (GSASteel)structuralObjects.FirstOrDefault(so => so is GSASteel);

      Assert.Equal("steel 1", speckleSteel.applicationId);
      Assert.Equal("", speckleSteel.grade);
      Assert.Equal(MaterialType.Steel, speckleSteel.materialType);
      Assert.Equal("", speckleSteel.designCode);
      Assert.Equal("", speckleSteel.codeYear);
      Assert.Equal(gsaMatSteel.Fy.Value, speckleSteel.yieldStrength);
      Assert.Equal(gsaMatSteel.Fu.Value, speckleSteel.ultimateStrength);
      Assert.Equal(gsaMatSteel.Mat.Eps.Value, speckleSteel.maxStrain);
      Assert.Equal(gsaMatSteel.Mat.E.Value, speckleSteel.elasticModulus);
      Assert.Equal(gsaMatSteel.Mat.Nu.Value, speckleSteel.poissonsRatio);
      Assert.Equal(gsaMatSteel.Mat.G.Value, speckleSteel.shearModulus);
      Assert.Equal(gsaMatSteel.Mat.Rho.Value, speckleSteel.density);
      Assert.Equal(gsaMatSteel.Mat.Alpha.Value, speckleSteel.thermalExpansivity);
    }

    //Timber not yet supported
    #endregion

    #region Property
    [Fact]
    public void Property1dToSpeckle()
    {
      //Define GSA objects
      //These should be in order that respects the type dependency tree (which is only available in the GSAProxy library, which isn't referenced yet
      var gsaRecords = new List<GsaRecord>();

      //Generation #1: Types with no other dependencies - the leaves of the tree
      gsaRecords.Add(GsaMatSteelExample("steel material 1"));

      //Gen #2
      var gsaSection = new List<GsaSection>();
      gsaSection.Add(GsaCatalogueSectionExample("section 1"));
      gsaSection.Add(GsaExplicitSectionExample("section 2"));
      gsaSection.Add(GsaPerimeterSectionExample("section 3"));
      gsaSection.Add(GsaRectangularSectionExample("section 4"));
      gsaSection.Add(GsaRectangularHollowSectionExample("section 5"));
      gsaSection.Add(GsaCircularSectionExample("section 6"));
      gsaSection.Add(GsaCircularHollowSectionExample("section 7"));
      gsaSection.Add(GsaISectionSectionExample("section 8"));
      gsaSection.Add(GsaTSectionSectionExample("section 9"));
      gsaSection.Add(GsaAngleSectionExample("section 10"));
      gsaSection.Add(GsaChannelSectionExample("section 11"));
      gsaRecords.AddRange(gsaSection);

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

      var speckleProperty1D = structuralObjects.FindAll(so => so is GSAProperty1D).Select(so => (GSAProperty1D)so).ToList();

      //TO DO: update once modifiers have been included in the section definition
      //Check
      #region Catalogue
      Assert.Equal("section 1", speckleProperty1D[0].applicationId);
      Assert.Equal(gsaSection[0].Colour.ToString(), speckleProperty1D[0].colour);
      Assert.Equal(MemberType.Generic1D, speckleProperty1D[0].memberType);
      Assert.Equal("steel material 1", speckleProperty1D[0].designMaterial.applicationId);
      Assert.Null(speckleProperty1D[0].material);
      Assert.Equal(ShapeType.Catalogue, speckleProperty1D[0].profile.shapeType);
      var gsaProfileCatalogue = (ProfileDetailsCatalogue)((SectionComp)gsaSection[0].Components[0]).ProfileDetails;
      Assert.Equal(gsaProfileCatalogue.Profile, ((Catalogue)speckleProperty1D[0].profile).description);
      Assert.Equal(gsaProfileCatalogue.Profile.Split(' ')[1].Split('-')[0], ((Catalogue)speckleProperty1D[0].profile).catalogueName);
      Assert.Equal(gsaProfileCatalogue.Profile.Split(' ')[1].Split('-')[1], ((Catalogue)speckleProperty1D[0].profile).sectionType);
      Assert.Equal(gsaProfileCatalogue.Profile.Split(' ')[2], ((Catalogue)speckleProperty1D[0].profile).sectionName);
      Assert.Equal(BaseReferencePoint.Centroid, speckleProperty1D[0].referencePoint);
      Assert.Equal(0, speckleProperty1D[0].offsetY);
      Assert.Equal(0, speckleProperty1D[0].offsetZ);
      Assert.Equal(gsaSection[0].Index.Value, speckleProperty1D[0].nativeId);
      Assert.Equal(gsaSection[0].Mass ?? 0, speckleProperty1D[0].additionalMass);
      Assert.Equal(gsaSection[0].Cost ?? 0, speckleProperty1D[0].cost);
      Assert.Null(speckleProperty1D[0].poolRef);
      #endregion
      #region Explicit
      Assert.Equal("section 2", speckleProperty1D[1].applicationId);
      Assert.Equal(gsaSection[1].Colour.ToString(), speckleProperty1D[1].colour);
      Assert.Equal(MemberType.Generic1D, speckleProperty1D[1].memberType);
      Assert.Equal("steel material 1", speckleProperty1D[1].designMaterial.applicationId);
      Assert.Null(speckleProperty1D[1].material);
      Assert.Equal(ShapeType.Explicit, speckleProperty1D[1].profile.shapeType);
      var gsaProfileExplicit = (ProfileDetailsExplicit)((SectionComp)gsaSection[1].Components[0]).ProfileDetails;
      Assert.Equal(gsaProfileExplicit.Area.Value, ((Explicit)speckleProperty1D[1].profile).area);
      Assert.Equal(gsaProfileExplicit.Iyy.Value, ((Explicit)speckleProperty1D[1].profile).Iyy);
      Assert.Equal(gsaProfileExplicit.Izz.Value, ((Explicit)speckleProperty1D[1].profile).Izz);
      Assert.Equal(gsaProfileExplicit.J.Value, ((Explicit)speckleProperty1D[1].profile).J);
      Assert.Equal(gsaProfileExplicit.Ky.Value, ((Explicit)speckleProperty1D[1].profile).Ky);
      Assert.Equal(gsaProfileExplicit.Kz.Value, ((Explicit)speckleProperty1D[1].profile).Kz);
      Assert.Equal(BaseReferencePoint.Centroid, speckleProperty1D[1].referencePoint);
      Assert.Equal(0, speckleProperty1D[1].offsetY);
      Assert.Equal(0, speckleProperty1D[1].offsetZ);
      Assert.Equal(gsaSection[1].Index.Value, speckleProperty1D[1].nativeId);
      Assert.Equal(gsaSection[1].Mass ?? 0, speckleProperty1D[1].additionalMass);
      Assert.Equal(gsaSection[1].Cost ?? 0, speckleProperty1D[1].cost);
      Assert.Null(speckleProperty1D[1].poolRef);
      #endregion
      #region Perimeter
      Assert.Equal("section 3", speckleProperty1D[2].applicationId);
      Assert.Equal(gsaSection[2].Colour.ToString(), speckleProperty1D[2].colour);
      Assert.Equal(MemberType.Generic1D, speckleProperty1D[2].memberType);
      Assert.Equal("steel material 1", speckleProperty1D[2].designMaterial.applicationId);
      Assert.Null(speckleProperty1D[2].material);
      Assert.Equal(ShapeType.Perimeter, speckleProperty1D[2].profile.shapeType);
      var gsaProfilePerimeter = (ProfileDetailsPerimeter)((SectionComp)gsaSection[2].Components[0]).ProfileDetails;
      // TO DO: test ((Perimeter)speckleProperty1D[2].profile).outline
      //             ((Perimeter)speckleProperty1D[2].profile).voids
      Assert.Equal(BaseReferencePoint.Centroid, speckleProperty1D[2].referencePoint);
      Assert.Equal(0, speckleProperty1D[2].offsetY);
      Assert.Equal(0, speckleProperty1D[2].offsetZ);
      Assert.Equal(gsaSection[2].Index.Value, speckleProperty1D[2].nativeId);
      Assert.Equal(gsaSection[2].Mass ?? 0, speckleProperty1D[2].additionalMass);
      Assert.Equal(gsaSection[2].Cost ?? 0, speckleProperty1D[2].cost);
      Assert.Null(speckleProperty1D[2].poolRef);
      #endregion
      #region Standard
      #region Rectangular
      Assert.Equal("section 4", speckleProperty1D[3].applicationId);
      Assert.Equal(gsaSection[3].Colour.ToString(), speckleProperty1D[3].colour);
      Assert.Equal(MemberType.Generic1D, speckleProperty1D[3].memberType);
      Assert.Equal("steel material 1", speckleProperty1D[3].designMaterial.applicationId);
      Assert.Null(speckleProperty1D[3].material);
      Assert.Equal(ShapeType.Rectangular, speckleProperty1D[3].profile.shapeType);
      var gsaProfileRectangular = (ProfileDetailsRectangular)((SectionComp)gsaSection[3].Components[0]).ProfileDetails;
      Assert.Equal(gsaProfileRectangular.b.Value, ((Rectangular)speckleProperty1D[3].profile).width);
      Assert.Equal(gsaProfileRectangular.d.Value, ((Rectangular)speckleProperty1D[3].profile).depth);
      Assert.Equal(0, ((Rectangular)speckleProperty1D[3].profile).webThickness);
      Assert.Equal(0, ((Rectangular)speckleProperty1D[3].profile).flangeThickness);
      Assert.Equal(BaseReferencePoint.Centroid, speckleProperty1D[3].referencePoint);
      Assert.Equal(0, speckleProperty1D[3].offsetY);
      Assert.Equal(0, speckleProperty1D[3].offsetZ);
      Assert.Equal(gsaSection[3].Index.Value, speckleProperty1D[3].nativeId);
      Assert.Equal(gsaSection[3].Mass ?? 0, speckleProperty1D[3].additionalMass);
      Assert.Equal(gsaSection[3].Cost ?? 0, speckleProperty1D[3].cost);
      Assert.Null(speckleProperty1D[3].poolRef);
      #endregion
      #region Rectangular Hollow
      Assert.Equal("section 5", speckleProperty1D[4].applicationId);
      Assert.Equal(gsaSection[4].Colour.ToString(), speckleProperty1D[4].colour);
      Assert.Equal(MemberType.Generic1D, speckleProperty1D[4].memberType);
      Assert.Equal("steel material 1", speckleProperty1D[4].designMaterial.applicationId);
      Assert.Null(speckleProperty1D[4].material);
      Assert.Equal(ShapeType.Rectangular, speckleProperty1D[4].profile.shapeType);
      var gsaProfileRHS = (ProfileDetailsTwoThickness)((SectionComp)gsaSection[4].Components[0]).ProfileDetails;
      Assert.Equal(gsaProfileRHS.b.Value, ((Rectangular)speckleProperty1D[4].profile).width);
      Assert.Equal(gsaProfileRHS.d.Value, ((Rectangular)speckleProperty1D[4].profile).depth);
      Assert.Equal(gsaProfileRHS.tw.Value, ((Rectangular)speckleProperty1D[4].profile).webThickness);
      Assert.Equal(gsaProfileRHS.tf.Value, ((Rectangular)speckleProperty1D[4].profile).flangeThickness);
      Assert.Equal(BaseReferencePoint.Centroid, speckleProperty1D[4].referencePoint);
      Assert.Equal(0, speckleProperty1D[4].offsetY);
      Assert.Equal(0, speckleProperty1D[4].offsetZ);
      Assert.Equal(gsaSection[4].Index.Value, speckleProperty1D[4].nativeId);
      Assert.Equal(gsaSection[4].Mass ?? 0, speckleProperty1D[4].additionalMass);
      Assert.Equal(gsaSection[4].Cost ?? 0, speckleProperty1D[4].cost);
      Assert.Null(speckleProperty1D[4].poolRef);
      #endregion
      #region Circular
      Assert.Equal("section 6", speckleProperty1D[5].applicationId);
      Assert.Equal(gsaSection[5].Colour.ToString(), speckleProperty1D[5].colour);
      Assert.Equal(MemberType.Generic1D, speckleProperty1D[5].memberType);
      Assert.Equal("steel material 1", speckleProperty1D[5].designMaterial.applicationId);
      Assert.Null(speckleProperty1D[5].material);
      Assert.Equal(ShapeType.Circular, speckleProperty1D[5].profile.shapeType);
      var gsaProfileCircular = (ProfileDetailsCircular)((SectionComp)gsaSection[5].Components[0]).ProfileDetails;
      Assert.Equal(gsaProfileCircular.d.Value / 2, ((Circular)speckleProperty1D[5].profile).radius);
      Assert.Equal(0, ((Circular)speckleProperty1D[5].profile).wallThickness);
      Assert.Equal(BaseReferencePoint.Centroid, speckleProperty1D[5].referencePoint);
      Assert.Equal(0, speckleProperty1D[5].offsetY);
      Assert.Equal(0, speckleProperty1D[5].offsetZ);
      Assert.Equal(gsaSection[5].Index.Value, speckleProperty1D[5].nativeId);
      Assert.Equal(gsaSection[5].Mass ?? 0, speckleProperty1D[5].additionalMass);
      Assert.Equal(gsaSection[5].Cost ?? 0, speckleProperty1D[5].cost);
      Assert.Null(speckleProperty1D[3].poolRef);
      #endregion
      #region Circular Hollow
      Assert.Equal("section 7", speckleProperty1D[6].applicationId);
      Assert.Equal(gsaSection[6].Colour.ToString(), speckleProperty1D[6].colour);
      Assert.Equal(MemberType.Generic1D, speckleProperty1D[6].memberType);
      Assert.Equal("steel material 1", speckleProperty1D[6].designMaterial.applicationId);
      Assert.Null(speckleProperty1D[6].material);
      Assert.Equal(ShapeType.Circular, speckleProperty1D[6].profile.shapeType);
      var gsaProfileCHS = (ProfileDetailsCircularHollow)((SectionComp)gsaSection[6].Components[0]).ProfileDetails;
      Assert.Equal(gsaProfileCHS.d.Value / 2, ((Circular)speckleProperty1D[6].profile).radius);
      Assert.Equal(gsaProfileCHS.t.Value, ((Circular)speckleProperty1D[6].profile).wallThickness);
      Assert.Equal(BaseReferencePoint.Centroid, speckleProperty1D[6].referencePoint);
      Assert.Equal(0, speckleProperty1D[6].offsetY);
      Assert.Equal(0, speckleProperty1D[6].offsetZ);
      Assert.Equal(gsaSection[6].Index.Value, speckleProperty1D[6].nativeId);
      Assert.Equal(gsaSection[6].Mass ?? 0, speckleProperty1D[6].additionalMass);
      Assert.Equal(gsaSection[6].Cost ?? 0, speckleProperty1D[6].cost);
      Assert.Null(speckleProperty1D[6].poolRef);
      #endregion
      #region I Section
      Assert.Equal("section 8", speckleProperty1D[7].applicationId);
      Assert.Equal(gsaSection[7].Colour.ToString(), speckleProperty1D[7].colour);
      Assert.Equal(MemberType.Generic1D, speckleProperty1D[7].memberType);
      Assert.Equal("steel material 1", speckleProperty1D[7].designMaterial.applicationId);
      Assert.Null(speckleProperty1D[7].material);
      Assert.Equal(ShapeType.I, speckleProperty1D[7].profile.shapeType);
      var gsaProfileISection = (ProfileDetailsTwoThickness)((SectionComp)gsaSection[7].Components[0]).ProfileDetails;
      Assert.Equal(gsaProfileISection.b.Value, ((ISection)speckleProperty1D[7].profile).width);
      Assert.Equal(gsaProfileISection.d.Value, ((ISection)speckleProperty1D[7].profile).depth);
      Assert.Equal(gsaProfileISection.tw.Value, ((ISection)speckleProperty1D[7].profile).webThickness);
      Assert.Equal(gsaProfileISection.tf.Value, ((ISection)speckleProperty1D[7].profile).flangeThickness);
      Assert.Equal(BaseReferencePoint.Centroid, speckleProperty1D[7].referencePoint);
      Assert.Equal(0, speckleProperty1D[7].offsetY);
      Assert.Equal(0, speckleProperty1D[7].offsetZ);
      Assert.Equal(gsaSection[7].Index.Value, speckleProperty1D[7].nativeId);
      Assert.Equal(gsaSection[7].Mass ?? 0, speckleProperty1D[7].additionalMass);
      Assert.Equal(gsaSection[7].Cost ?? 0, speckleProperty1D[7].cost);
      Assert.Null(speckleProperty1D[7].poolRef);
      #endregion
      #region T Section
      Assert.Equal("section 9", speckleProperty1D[8].applicationId);
      Assert.Equal(gsaSection[8].Colour.ToString(), speckleProperty1D[8].colour);
      Assert.Equal(MemberType.Generic1D, speckleProperty1D[8].memberType);
      Assert.Equal("steel material 1", speckleProperty1D[8].designMaterial.applicationId);
      Assert.Null(speckleProperty1D[8].material);
      Assert.Equal(ShapeType.Tee, speckleProperty1D[8].profile.shapeType);
      var gsaProfileTSection = (ProfileDetailsTwoThickness)((SectionComp)gsaSection[8].Components[0]).ProfileDetails;
      Assert.Equal(gsaProfileTSection.b.Value, ((Tee)speckleProperty1D[8].profile).width);
      Assert.Equal(gsaProfileTSection.d.Value, ((Tee)speckleProperty1D[8].profile).depth);
      Assert.Equal(gsaProfileTSection.tw.Value, ((Tee)speckleProperty1D[8].profile).webThickness);
      Assert.Equal(gsaProfileTSection.tf.Value, ((Tee)speckleProperty1D[8].profile).flangeThickness);
      Assert.Equal(BaseReferencePoint.Centroid, speckleProperty1D[8].referencePoint);
      Assert.Equal(0, speckleProperty1D[8].offsetY);
      Assert.Equal(0, speckleProperty1D[8].offsetZ);
      Assert.Equal(gsaSection[8].Index.Value, speckleProperty1D[8].nativeId);
      Assert.Equal(gsaSection[8].Mass ?? 0, speckleProperty1D[8].additionalMass);
      Assert.Equal(gsaSection[8].Cost ?? 0, speckleProperty1D[8].cost);
      Assert.Null(speckleProperty1D[8].poolRef);
      #endregion
      #region Angle
      Assert.Equal("section 10", speckleProperty1D[9].applicationId);
      Assert.Equal(gsaSection[9].Colour.ToString(), speckleProperty1D[9].colour);
      Assert.Equal(MemberType.Generic1D, speckleProperty1D[9].memberType);
      Assert.Equal("steel material 1", speckleProperty1D[9].designMaterial.applicationId);
      Assert.Null(speckleProperty1D[9].material);
      Assert.Equal(ShapeType.Angle, speckleProperty1D[9].profile.shapeType);
      var gsaProfileAngle = (ProfileDetailsTwoThickness)((SectionComp)gsaSection[9].Components[0]).ProfileDetails;
      Assert.Equal(gsaProfileAngle.b.Value, ((Angle)speckleProperty1D[9].profile).width);
      Assert.Equal(gsaProfileAngle.d.Value, ((Angle)speckleProperty1D[9].profile).depth);
      Assert.Equal(gsaProfileAngle.tw.Value, ((Angle)speckleProperty1D[9].profile).webThickness);
      Assert.Equal(gsaProfileAngle.tf.Value, ((Angle)speckleProperty1D[9].profile).flangeThickness);
      Assert.Equal(BaseReferencePoint.Centroid, speckleProperty1D[3].referencePoint);
      Assert.Equal(0, speckleProperty1D[9].offsetY);
      Assert.Equal(0, speckleProperty1D[9].offsetZ);
      Assert.Equal(gsaSection[9].Index.Value, speckleProperty1D[9].nativeId);
      Assert.Equal(gsaSection[9].Mass ?? 0, speckleProperty1D[9].additionalMass);
      Assert.Equal(gsaSection[9].Cost ?? 0, speckleProperty1D[9].cost);
      Assert.Null(speckleProperty1D[9].poolRef);
      #endregion
      #region Channel
      Assert.Equal("section 11", speckleProperty1D[10].applicationId);
      Assert.Equal(gsaSection[10].Colour.ToString(), speckleProperty1D[10].colour);
      Assert.Equal(MemberType.Generic1D, speckleProperty1D[10].memberType);
      Assert.Equal("steel material 1", speckleProperty1D[10].designMaterial.applicationId);
      Assert.Null(speckleProperty1D[10].material);
      Assert.Equal(ShapeType.Channel, speckleProperty1D[10].profile.shapeType);
      var gsaProfileChannel = (ProfileDetailsTwoThickness)((SectionComp)gsaSection[10].Components[0]).ProfileDetails;
      Assert.Equal(gsaProfileChannel.b.Value, ((Channel)speckleProperty1D[10].profile).width);
      Assert.Equal(gsaProfileChannel.d.Value, ((Channel)speckleProperty1D[10].profile).depth);
      Assert.Equal(gsaProfileChannel.tw.Value, ((Channel)speckleProperty1D[10].profile).webThickness);
      Assert.Equal(gsaProfileChannel.tf.Value, ((Channel)speckleProperty1D[10].profile).flangeThickness);
      Assert.Equal(BaseReferencePoint.Centroid, speckleProperty1D[10].referencePoint);
      Assert.Equal(0, speckleProperty1D[10].offsetY);
      Assert.Equal(0, speckleProperty1D[10].offsetZ);
      Assert.Equal(gsaSection[10].Index.Value, speckleProperty1D[10].nativeId);
      Assert.Equal(gsaSection[10].Mass ?? 0, speckleProperty1D[10].additionalMass);
      Assert.Equal(gsaSection[10].Cost ?? 0, speckleProperty1D[10].cost);
      Assert.Null(speckleProperty1D[3].poolRef);
      #endregion
      #endregion
    }

    [Fact]
    public void Property2dToSpeckle()
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
      Assert.Equal("steel material 1", speckleProperty2D.designMaterial.applicationId);
      Assert.Null(speckleProperty2D.material);
      Assert.Null(speckleProperty2D.orientationAxis.applicationId); //no application ID for global coordinate system
      Assert.True(speckleProperty2D.orientationAxis.definition.IsGlobal());
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
      Assert.Equal(0, speckleProperty2D.cost);
    }

    //Property3D not yet supported

    [Fact]
    public void PropertyMassToSpeckle()
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
    public void PropertySpringToSpeckle()
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
      Assert.Equal(gsaProSpr.Stiffnesses[GwaAxisDirection6.X], specklePropertySpring.stiffnessX);
      Assert.Equal(gsaProSpr.Stiffnesses[GwaAxisDirection6.Y], specklePropertySpring.stiffnessY);
      Assert.Equal(gsaProSpr.Stiffnesses[GwaAxisDirection6.Z], specklePropertySpring.stiffnessZ);
      Assert.Equal(gsaProSpr.Stiffnesses[GwaAxisDirection6.XX], specklePropertySpring.stiffnessXX);
      Assert.Equal(gsaProSpr.Stiffnesses[GwaAxisDirection6.YY], specklePropertySpring.stiffnessYY);
      Assert.Equal(gsaProSpr.Stiffnesses[GwaAxisDirection6.ZZ], specklePropertySpring.stiffnessZZ);
      Assert.Equal(gsaProSpr.DampingRatio, specklePropertySpring.dampingRatio);
    }
    #endregion

    #region Results
    [Fact]
    public void NodeResultsToSpeckle()
    {
      //Define GSA objects
      //These should be in order that respects the type dependency tree (which is only available in the GSAProxy library, which isn't referenced yet
      var gsaRecords = new List<GsaRecord>();

      //Generation #1: Types with no other dependencies - the leaves of the tree
      gsaRecords.Add(GsaPropMassExample("property mass 1"));
      gsaRecords.Add(GsaPropSprExample("property spring 1"));
      gsaRecords.AddRange(GsaLoadCaseExamples(2, "load case 1", "load case 2"));

      //Gen #2
      var gsaNodes = GsaNodeExamples(1, "node 1");
      gsaRecords.AddRange(gsaNodes);

      Instance.GsaModel.Cache.Upsert(gsaRecords);

      //Results
      var nodeResults = GsaNodeResultExamples();
      ((GsaProxyMockForConverterTests)Instance.GsaModel.Proxy).AddResultData(ResultGroup.Node, nodeResults);

      //Convert
      Instance.GsaModel.StreamLayer = GSALayer.Both;
      Instance.GsaModel.StreamSendConfig = StreamContentConfig.ModelAndResults;
      var speckleObjects = converter.ConvertToSpeckle(gsaRecords.Select(i => (object)i).ToList());

      //Get speckle results
      Assert.NotEmpty(speckleObjects);
      Assert.Contains(speckleObjects, so => so is ResultSetAll);
      var rsa = (ResultSetAll)speckleObjects.FirstOrDefault(so => so is ResultSetAll);
      Assert.NotNull(rsa.resultsNode);
      Assert.NotEmpty(rsa.resultsNode.resultsNode);
      var speckleNodeResults = rsa.resultsNode.resultsNode;
      Assert.Equal(2, speckleNodeResults.Count());

      //Checks - Results for node 1 - Load case A1
      for (var i = 0; i < speckleNodeResults.Count(); i++)
      {
        var gsaResult = (CsvNode)nodeResults[i];
        var loadCase = (i + 1).ToString();

        Assert.Equal("node 1_load case " + loadCase, speckleNodeResults[i].applicationId);
        Assert.Equal("node 1", speckleNodeResults[i].node.applicationId); //assume the conversion of the node is tested elsewhere
        Assert.Equal("", speckleNodeResults[i].description);
        Assert.Equal("", speckleNodeResults[i].permutation);
        Assert.Equal("load case " + loadCase, speckleNodeResults[i].resultCase.applicationId);
        Assert.Equal(gsaResult.Ux.Value, speckleNodeResults[i].dispX);
        Assert.Equal(gsaResult.Uy.Value, speckleNodeResults[i].dispY);
        Assert.Equal(gsaResult.Uz.Value, speckleNodeResults[i].dispZ);
        Assert.Equal(gsaResult.Rxx.Value, speckleNodeResults[i].rotXX);
        Assert.Equal(gsaResult.Ryy.Value, speckleNodeResults[i].rotYY);
        Assert.Equal(gsaResult.Rzz.Value, speckleNodeResults[i].rotZZ);
        Assert.Equal(gsaResult.Vx.Value, speckleNodeResults[i].velX);
        Assert.Equal(gsaResult.Vy.Value, speckleNodeResults[i].velY);
        Assert.Equal(gsaResult.Vz.Value, speckleNodeResults[i].velZ);
        Assert.Equal(gsaResult.Vxx.Value, speckleNodeResults[i].velXX);
        Assert.Equal(gsaResult.Vyy.Value, speckleNodeResults[i].velYY);
        Assert.Equal(gsaResult.Vzz.Value, speckleNodeResults[i].velZZ);
        Assert.Equal(gsaResult.Ax.Value, speckleNodeResults[i].accX);
        Assert.Equal(gsaResult.Ay.Value, speckleNodeResults[i].accY);
        Assert.Equal(gsaResult.Az.Value, speckleNodeResults[i].accZ);
        Assert.Equal(gsaResult.Axx.Value, speckleNodeResults[i].accXX);
        Assert.Equal(gsaResult.Ayy.Value, speckleNodeResults[i].accYY);
        Assert.Equal(gsaResult.Azz.Value, speckleNodeResults[i].accZZ);
        Assert.Equal(gsaResult.Fx_Reac.Value, speckleNodeResults[i].reactionX);
        Assert.Equal(gsaResult.Fy_Reac.Value, speckleNodeResults[i].reactionY);
        Assert.Equal(gsaResult.Fz_Reac.Value, speckleNodeResults[i].reactionZ);
        Assert.Equal(gsaResult.Mxx_Reac.Value, speckleNodeResults[i].reactionXX);
        Assert.Equal(gsaResult.Myy_Reac.Value, speckleNodeResults[i].reactionYY);
        Assert.Equal(gsaResult.Mzz_Reac.Value, speckleNodeResults[i].reactionZZ);
        Assert.Equal(gsaResult.Fx_Cons.Value, speckleNodeResults[i].constraintX);
        Assert.Equal(gsaResult.Fy_Cons.Value, speckleNodeResults[i].constraintY);
        Assert.Equal(gsaResult.Fz_Cons.Value, speckleNodeResults[i].constraintZ);
        Assert.Equal(gsaResult.Mxx_Cons.Value, speckleNodeResults[i].constraintXX);
        Assert.Equal(gsaResult.Myy_Cons.Value, speckleNodeResults[i].constraintYY);
        Assert.Equal(gsaResult.Mzz_Cons.Value, speckleNodeResults[i].constraintZZ);
      }
    }

    [Fact]
    public void Element1dResultsToSpeckle()
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
      gsaRecords.AddRange(GsaNodeExamples(2, "node 1", "node 2"));
      gsaRecords.Add(GsaCatalogueSectionExample("section 1"));

      // Gen #3
      gsaRecords.Add(GsaElement1dExamples(1, "element 1").First());

      Instance.GsaModel.Cache.Upsert(gsaRecords);

      //Results
      var gsaElement1dResults = GsaElement1dResultExamples(1, 5, 1);
      ((GsaProxyMockForConverterTests)Instance.GsaModel.Proxy).AddResultData(ResultGroup.Element1d, gsaElement1dResults);

      //Convert
      Instance.GsaModel.StreamLayer = GSALayer.Both;
      Instance.GsaModel.StreamSendConfig = StreamContentConfig.ModelAndResults;
      var speckleObjects = converter.ConvertToSpeckle(gsaRecords.Select(i => (object)i).ToList());

      //Get speckle results
      Assert.NotEmpty(speckleObjects);
      Assert.Contains(speckleObjects, so => so is ResultSetAll);
      var rsa = (ResultSetAll)speckleObjects.FirstOrDefault(so => so is ResultSetAll);
      Assert.NotNull(rsa.results1D);
      Assert.NotEmpty(rsa.results1D.results1D);
      var speckleElement1dResults = rsa.results1D.results1D;
      Assert.Equal(5, speckleElement1dResults.Count());

      //Checks
      for (var i = 0; i < speckleElement1dResults.Count(); i++)
      {
        var gsaResult = (CsvElem1d)gsaElement1dResults[i];

        //result description
        Assert.Equal("element 1_load case 1_" + gsaResult.PosR, speckleElement1dResults[i].applicationId);
        Assert.Equal("", speckleElement1dResults[i].permutation);
        Assert.Equal("", speckleElement1dResults[i].description);
        Assert.Equal("element 1", speckleElement1dResults[i].element.applicationId);
        Assert.Equal(float.Parse(gsaResult.PosR), speckleElement1dResults[i].position);
        Assert.Equal("load case 1", speckleElement1dResults[i].resultCase.applicationId);

        //results
        Assert.Equal(gsaResult.Ux.Value, speckleElement1dResults[i].dispX);
        Assert.Equal(gsaResult.Uy.Value, speckleElement1dResults[i].dispY);
        Assert.Equal(gsaResult.Uz.Value, speckleElement1dResults[i].dispZ);
        Assert.Equal(gsaResult.Fx.Value, speckleElement1dResults[i].forceX);
        Assert.Equal(gsaResult.Fy.Value, speckleElement1dResults[i].forceY);
        Assert.Equal(gsaResult.Fz.Value, speckleElement1dResults[i].forceZ);
        Assert.Equal(gsaResult.Mxx.Value, speckleElement1dResults[i].momentXX);
        Assert.Equal(gsaResult.Myy.Value, speckleElement1dResults[i].momentYY);
        Assert.Equal(gsaResult.Mzz.Value, speckleElement1dResults[i].momentZZ);

        //results - Not currently supported
        Assert.Null(speckleElement1dResults[i].axialStress);
        Assert.Null(speckleElement1dResults[i].bendingStressYNeg);
        Assert.Null(speckleElement1dResults[i].bendingStressYPos);
        Assert.Null(speckleElement1dResults[i].bendingStressZNeg);
        Assert.Null(speckleElement1dResults[i].bendingStressZPos);
        Assert.Null(speckleElement1dResults[i].combinedStressMax);
        Assert.Null(speckleElement1dResults[i].combinedStressMin);
        Assert.Null(speckleElement1dResults[i].rotXX);
        Assert.Null(speckleElement1dResults[i].rotYY);
        Assert.Null(speckleElement1dResults[i].rotZZ);
        Assert.Null(speckleElement1dResults[i].shearStressY);
        Assert.Null(speckleElement1dResults[i].shearStressZ);
      }
    }

    [Fact]
    public void Element2dResultsToSpeckle()
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
      gsaRecords.AddRange(GsaNodeExamples(4, "node 1", "node 2", "node 3", "node 4"));
      gsaRecords.Add(GsaProp2dExample("property 2D 1"));

      // Gen #3
      gsaRecords.Add(GsaElement2dExamples(1, "element 1").First());

      Instance.GsaModel.Cache.Upsert(gsaRecords);

      //Results
      var gsaElement2dResults = GsaElement2dResultExamples(1, 1);
      ((GsaProxyMockForConverterTests)Instance.GsaModel.Proxy).AddResultData(ResultGroup.Element2d, gsaElement2dResults);

      //Convert
      Instance.GsaModel.StreamLayer = GSALayer.Both;
      Instance.GsaModel.StreamSendConfig = StreamContentConfig.ModelAndResults;
      var speckleObjects = converter.ConvertToSpeckle(gsaRecords.Select(i => (object)i).ToList());

      //Get speckle results
      Assert.NotEmpty(speckleObjects);
      Assert.Contains(speckleObjects, so => so is ResultSetAll);
      var rsa = (ResultSetAll)speckleObjects.FirstOrDefault(so => so is ResultSetAll);
      Assert.NotNull(rsa.results2D);
      Assert.NotEmpty(rsa.results2D.results2D);
      var speckleElement2dResults = rsa.results2D.results2D;
      Assert.Equal(5, speckleElement2dResults.Count());

      //Checks
      for (var i = 0; i < speckleElement2dResults.Count(); i++)
      {
        var gsaResult = (CsvElem2d)gsaElement2dResults[i];

        //result description
        Assert.Equal("element 1_load case 1_" + gsaResult.PosR.ToString() + "_" + gsaResult.PosS.ToString(), speckleElement2dResults[i].applicationId);
        Assert.Equal("", speckleElement2dResults[i].permutation);
        Assert.Equal("", speckleElement2dResults[i].description);
        Assert.Equal("element 1", speckleElement2dResults[i].element.applicationId);
        Assert.Equal("load case 1", speckleElement2dResults[i].resultCase.applicationId);
        Assert.Equal(2, speckleElement2dResults[i].position.Count());
        Assert.Equal(gsaResult.PosR.Value, speckleElement2dResults[i].position[0]);
        Assert.Equal(gsaResult.PosS.Value, speckleElement2dResults[i].position[1]);

        //results
        Assert.Equal(gsaResult.Ux.Value, speckleElement2dResults[i].dispX);
        Assert.Equal(gsaResult.Uy.Value, speckleElement2dResults[i].dispY);
        Assert.Equal(gsaResult.Uz.Value, speckleElement2dResults[i].dispZ);
        Assert.Equal(gsaResult.Nx.Value, speckleElement2dResults[i].forceXX);
        Assert.Equal(gsaResult.Ny.Value, speckleElement2dResults[i].forceYY);
        Assert.Equal(gsaResult.Nxy.Value, speckleElement2dResults[i].forceXY);
        Assert.Equal(gsaResult.Qx.Value, speckleElement2dResults[i].shearX);
        Assert.Equal(gsaResult.Qy.Value, speckleElement2dResults[i].shearY);
        Assert.Equal(gsaResult.Mx.Value, speckleElement2dResults[i].momentXX);
        Assert.Equal(gsaResult.My.Value, speckleElement2dResults[i].momentYY);
        Assert.Equal(gsaResult.Mxy.Value, speckleElement2dResults[i].momentXY);
        Assert.Equal(gsaResult.Xx_b.Value, speckleElement2dResults[i].stressBotXX);
        Assert.Equal(gsaResult.Yy_b.Value, speckleElement2dResults[i].stressBotYY);
        Assert.Equal(gsaResult.Zz_b.Value, speckleElement2dResults[i].stressBotZZ);
        Assert.Equal(gsaResult.Xy_b.Value, speckleElement2dResults[i].stressBotXY);
        Assert.Equal(gsaResult.Yz_b.Value, speckleElement2dResults[i].stressBotYZ);
        Assert.Equal(gsaResult.Zx_b.Value, speckleElement2dResults[i].stressBotZX);
        Assert.Equal(gsaResult.Xx_m.Value, speckleElement2dResults[i].stressMidXX);
        Assert.Equal(gsaResult.Yy_m.Value, speckleElement2dResults[i].stressMidYY);
        Assert.Equal(gsaResult.Zz_m.Value, speckleElement2dResults[i].stressMidZZ);
        Assert.Equal(gsaResult.Xy_m.Value, speckleElement2dResults[i].stressMidXY);
        Assert.Equal(gsaResult.Yz_m.Value, speckleElement2dResults[i].stressMidYZ);
        Assert.Equal(gsaResult.Zx_m.Value, speckleElement2dResults[i].stressMidZX);
        Assert.Equal(gsaResult.Xx_t.Value, speckleElement2dResults[i].stressTopXX);
        Assert.Equal(gsaResult.Yy_t.Value, speckleElement2dResults[i].stressTopYY);
        Assert.Equal(gsaResult.Zz_t.Value, speckleElement2dResults[i].stressTopZZ);
        Assert.Equal(gsaResult.Xy_t.Value, speckleElement2dResults[i].stressTopXY);
        Assert.Equal(gsaResult.Yz_t.Value, speckleElement2dResults[i].stressTopYZ);
        Assert.Equal(gsaResult.Zx_t.Value, speckleElement2dResults[i].stressTopZX);
      }
    }

    [Fact]
    public void NodeEmbeddedResultsToSpeckle()
    {
      var resultRecords = new List<CsvRecord>()
      {
        new CsvNode()
        {
          CaseId = "A1",
          ElemId = 23,
          Ux = 1,
          Uy = 2,
          Uz = 3
        },
        new CsvNode()
        {
          CaseId = "A2",
          ElemId = 23,
          Ux = 1,
          Uy = 2,
          Uz = 3
        }
      };
      ((GsaProxyMockForConverterTests)Instance.GsaModel.Proxy).AddResultData(ResultGroup.Node, resultRecords);

      //---

      Instance.GsaModel.Proxy.GetResultRecords(ResultGroup.Node, 23, out var foundResultRecords);
      Instance.GsaModel.Proxy.GetResultRecords(ResultGroup.Node, 23, "A1", out var foundA1ResultRecords);

      //---

      Assert.Equal(2, foundResultRecords.Count());
      Assert.Single(foundA1ResultRecords);
    }

    [Fact]
    public void NodeSeparateResultsToSpeckle()
    {
    }

    [Fact]
    public void NodeResultsOnlyToSpeckle()
    {

    }
    #endregion

    #region Constraints
    [Fact]
    public void RigidToSpeckle()
    {
      //Define GSA objects
      //These should be in order that respects the type dependency tree (which is only available in the GSAProxy library, which isn't referenced yet
      var gsaRecords = new List<GsaRecord>();

      //Generation #1: Types with no other dependencies - the leaves of the tree
      gsaRecords.Add(GsaMatSteelExample("steel material 1"));
      gsaRecords.Add(GsaPropMassExample("property mass 1"));
      gsaRecords.Add(GsaPropSprExample("property spring 1"));

      //Gen #2
      gsaRecords.AddRange(GsaNodeExamples(3, "node 1", "node 2", "node 3"));
      gsaRecords.Add(GsaCatalogueSectionExample("section 1"));

      // Gen #3
      gsaRecords.AddRange(GsaElement1dExamples(2, "element 1", "element 2"));

      //Gen #4
      gsaRecords.Add(GsaAnalStageExamples(1, "stage 1").FirstOrDefault());

      //Gen #5
      var gsaRigids = GsaRigidExamples(2, "rigid 1", "rigid 2");
      gsaRecords.AddRange(gsaRigids);

      Instance.GsaModel.Cache.Upsert(gsaRecords);

      //Convert
      Instance.GsaModel.StreamLayer = GSALayer.Both;
      Instance.GsaModel.StreamSendConfig = StreamContentConfig.ModelAndResults;
      var speckleObjects = converter.ConvertToSpeckle(gsaRecords.Select(i => (object)i).ToList());

      //Get speckle results
      Assert.NotEmpty(speckleObjects);
      Assert.Contains(speckleObjects, so => so is Model);
      var speckleModels = speckleObjects.FindAll(so => so is Model).Select(so => (Model)so).ToList();
      List<GSARigidConstraint> speckleRigids;

      foreach (var m in speckleModels)
      {
        Assert.NotNull(m);
        Assert.NotEmpty(m.elements);
        Assert.Contains(m.elements, o => o is GSARigidConstraint);
        speckleRigids = m.elements.FindAll(so => so is GSARigidConstraint).Select(so => (GSARigidConstraint)so).ToList();

        //Checks - rigid 1
        Assert.Equal("rigid 1", speckleRigids[0].applicationId);
        Assert.Equal(gsaRigids[0].Index.Value, speckleRigids[0].nativeId);
        Assert.Equal(gsaRigids[0].Name, speckleRigids[0].name);
        Assert.Equal("node 1", speckleRigids[0].primaryNode.applicationId);
        Assert.Equal(gsaRigids[0].ConstrainedNodes.Count(), speckleRigids[0].constrainedNodes.Count());
        Assert.Equal("node 2", speckleRigids[0].constrainedNodes[0].applicationId);
        Assert.Equal(gsaRigids[0].Stage.Count(), speckleRigids[0].stages.Count());
        Assert.Equal("stage 1", speckleRigids[0].stages[0].applicationId);
        Assert.Equal(LinkageType.ALL, speckleRigids[0].type);
        Assert.Null(speckleRigids[0].constraintCondition);

        //Checks - rigid 2
        Assert.Equal("rigid 2", speckleRigids[1].applicationId);
        Assert.Equal(gsaRigids[1].Index.Value, speckleRigids[1].nativeId);
        Assert.Equal(gsaRigids[1].Name, speckleRigids[1].name);
        Assert.Equal("node 1", speckleRigids[1].primaryNode.applicationId);
        Assert.Equal(gsaRigids[1].ConstrainedNodes.Count(), speckleRigids[1].constrainedNodes.Count());
        Assert.Equal("node 2", speckleRigids[1].constrainedNodes[0].applicationId);
        Assert.Equal(gsaRigids[1].Stage.Count(), speckleRigids[1].stages.Count());
        Assert.Equal("stage 1", speckleRigids[1].stages[0].applicationId);
        Assert.Equal(LinkageType.Custom, speckleRigids[1].type);
        var constraintCondition = new Dictionary<AxisDirection6, List<AxisDirection6>>()
        {
          { AxisDirection6.X, new List<AxisDirection6>() { AxisDirection6.X, AxisDirection6.YY, AxisDirection6.ZZ } },
          { AxisDirection6.Y, new List<AxisDirection6>() { AxisDirection6.Y, AxisDirection6.XX, AxisDirection6.ZZ } },
          { AxisDirection6.Z, new List<AxisDirection6>() { AxisDirection6.Z, AxisDirection6.XX, AxisDirection6.YY } },
          { AxisDirection6.XX, new List<AxisDirection6>() { AxisDirection6.XX } },
          { AxisDirection6.YY, new List<AxisDirection6>() { AxisDirection6.YY } },
          { AxisDirection6.ZZ, new List<AxisDirection6>() { AxisDirection6.ZZ } },
        };
        Assert.Equal(constraintCondition, speckleRigids[1].constraintCondition);
      }
    }

    [Fact]
    public void GenRestToSpeckle()
    {
      //Define GSA objects
      //These should be in order that respects the type dependency tree (which is only available in the GSAProxy library, which isn't referenced yet
      var gsaRecords = new List<GsaRecord>();

      //Generation #1: Types with no other dependencies - the leaves of the tree
      gsaRecords.Add(GsaMatSteelExample("steel material 1"));
      gsaRecords.Add(GsaPropMassExample("property mass 1"));
      gsaRecords.Add(GsaPropSprExample("property spring 1"));

      //Gen #2
      gsaRecords.AddRange(GsaNodeExamples(3, "node 1", "node 2", "node 3"));
      gsaRecords.Add(GsaCatalogueSectionExample("section 1"));

      // Gen #3
      gsaRecords.AddRange(GsaElement1dExamples(2, "element 1", "element 2"));

      //Gen #4
      gsaRecords.AddRange(GsaAnalStageExamples(2, "stage 1", "stage 2"));

      //Gen #5
      var gsaGenRests = GsaGenRestExamples(2, "gen rest 1", "gen rest 2");
      gsaRecords.AddRange(gsaGenRests);

      Instance.GsaModel.Cache.Upsert(gsaRecords);

      //Convert
      Instance.GsaModel.StreamLayer = GSALayer.Both;
      Instance.GsaModel.StreamSendConfig = StreamContentConfig.ModelAndResults;
      var speckleObjects = converter.ConvertToSpeckle(gsaRecords.Select(i => (object)i).ToList());

      //Get speckle results
      Assert.NotEmpty(speckleObjects);
      Assert.Contains(speckleObjects, so => so is Model);
      var speckleModels = speckleObjects.FindAll(so => so is Model).Select(so => (Model)so).ToList();
      List<GSAGeneralisedRestraint> speckleGenRests;

      foreach (var m in speckleModels)
      {
        Assert.NotNull(m);
        Assert.NotEmpty(m.elements);
        Assert.Contains(m.elements, o => o is GSAGeneralisedRestraint);
        speckleGenRests = m.elements.FindAll(so => so is GSAGeneralisedRestraint).Select(so => (GSAGeneralisedRestraint)so).ToList();

        //Checks - gen rest 1
        Assert.Equal("gen rest 1", speckleGenRests[0].applicationId);
        Assert.Equal(gsaGenRests[0].Index.Value, speckleGenRests[0].nativeId);
        Assert.Equal(gsaGenRests[0].Name, speckleGenRests[0].name);
        Assert.Equal("FFFRRR", speckleGenRests[0].restraint.code);
        Assert.Equal(gsaGenRests[0].NodeIndices.Count(), speckleGenRests[0].nodes.Count());
        Assert.Equal("node 1", speckleGenRests[0].nodes[0].applicationId);
        Assert.Equal(gsaGenRests[0].StageIndices.Count(), speckleGenRests[0].stages.Count());
        Assert.Equal("stage 1", speckleGenRests[0].stages[0].applicationId);

        //Checks - gen rest 2
        Assert.Equal("gen rest 2", speckleGenRests[1].applicationId);
        Assert.Equal(gsaGenRests[1].Index.Value, speckleGenRests[1].nativeId);
        Assert.Equal(gsaGenRests[1].Name, speckleGenRests[1].name);
        Assert.Equal("FFFFFF", speckleGenRests[1].restraint.code);
        Assert.Equal(gsaGenRests[1].NodeIndices.Count(), speckleGenRests[1].nodes.Count());
        Assert.Equal("node 1", speckleGenRests[1].nodes[0].applicationId);
        Assert.Equal("node 2", speckleGenRests[1].nodes[1].applicationId);
        Assert.Equal(gsaGenRests[1].StageIndices.Count(), speckleGenRests[1].stages.Count());
        Assert.Equal("stage 1", speckleGenRests[1].stages[0].applicationId);
        Assert.Equal("stage 2", speckleGenRests[1].stages[1].applicationId);
      }
    }
    #endregion

    #region Analysis Stage
    [Fact]
    public void StageToSpeckle()
    {
      //Define GSA objects
      //These should be in order that respects the type dependency tree (which is only available in the GSAProxy library, which isn't referenced yet
      var gsaRecords = new List<GsaRecord>();

      //Generation #1: Types with no other dependencies - the leaves of the tree
      gsaRecords.Add(GsaMatSteelExample("steel material 1"));
      gsaRecords.Add(GsaPropMassExample("property mass 1"));
      gsaRecords.Add(GsaPropSprExample("property spring 1"));

      //Gen #2
      gsaRecords.AddRange(GsaNodeExamples(3, "node 1", "node 2", "node 3"));
      gsaRecords.Add(GsaCatalogueSectionExample("section 1"));

      // Gen #3
      gsaRecords.AddRange(GsaElement1dExamples(2, "element 1", "element 2"));

      //Gen #4
      var gsaStage = GsaAnalStageExamples(1, "stage 1").First();
      gsaRecords.Add(gsaStage);

      Instance.GsaModel.Cache.Upsert(gsaRecords);

      //Convert
      Instance.GsaModel.StreamLayer = GSALayer.Both;
      Instance.GsaModel.StreamSendConfig = StreamContentConfig.ModelAndResults;
      var speckleObjects = converter.ConvertToSpeckle(gsaRecords.Select(i => (object)i).ToList());

      //Get speckle results
      Assert.NotEmpty(speckleObjects);
      Assert.Contains(speckleObjects, so => so is Model);
      var speckleModels = speckleObjects.FindAll(so => so is Model).Select(so => (Model)so).ToList();
      GSAStage speckleStage;

      foreach (var m in speckleModels)
      {
        Assert.NotNull(m);
        Assert.NotEmpty(m.elements);
        Assert.Contains(m.elements, o => o is GSAStage);
        speckleStage = (GSAStage)m.elements.FirstOrDefault(so => so is GSAStage);

        //Checks - stage 1
        Assert.Equal("stage 1", speckleStage.applicationId);
        Assert.Equal(gsaStage.Index.Value, speckleStage.nativeId);
        Assert.Equal(gsaStage.Name, speckleStage.name);
        Assert.Equal(gsaStage.Colour.ToString(), speckleStage.colour);
        Assert.Equal(gsaStage.Phi.Value, speckleStage.creepFactor);
        Assert.Equal(gsaStage.Days.Value, speckleStage.stageTime);

        if (m.layerDescription == "Design")
        {
          Assert.Null(speckleStage.elements);
          Assert.Null(speckleStage.lockedElements);
        }
        else if (m.layerDescription == "Analysis")
        {
          Assert.Equal(gsaStage.ElementIndices.Count(), speckleStage.elements.Count());
          Assert.Equal("element 1", speckleStage.elements[0].applicationId);
          Assert.Equal(gsaStage.LockElementIndices.Count(), speckleStage.lockedElements.Count());
          Assert.Equal("element 2", speckleStage.lockedElements[0].applicationId);
        }
      }
    }
    #endregion

    #region Bridge
    [Fact]
    public void InfBeamToSpeckle()
    {
      //Define GSA objects
      //These should be in order that respects the type dependency tree (which is only available in the GSAProxy library, which isn't referenced yet
      var gsaRecords = new List<GsaRecord>();

      //Generation #1: Types with no other dependencies - the leaves of the tree
      gsaRecords.Add(GsaMatSteelExample("steel material 1"));
      gsaRecords.Add(GsaPropMassExample("property mass 1"));
      gsaRecords.Add(GsaPropSprExample("property spring 1"));

      //Gen #2
      gsaRecords.AddRange(GsaNodeExamples(2, "node 1", "node 2"));
      gsaRecords.Add(GsaCatalogueSectionExample("section 1"));

      // Gen #3
      gsaRecords.Add(GsaElement1dExamples(2, "element 1").FirstOrDefault());

      //Gen #4
      var gsaInfBeams = GsaInfBeamExamples(2, "inf beam 1", "inf beam 2");
      gsaRecords.AddRange(gsaInfBeams);

      Instance.GsaModel.Cache.Upsert(gsaRecords);

      foreach (var record in gsaRecords)
      {
        var speckleObjects = converter.ConvertToSpeckle(new List<object> { record });
        Assert.Empty(converter.ConversionErrors);

        Instance.GsaModel.Cache.SetSpeckleObjects(record, speckleObjects.ToDictionary(so => so.applicationId, so => (object)so));
      }

      Assert.True(Instance.GsaModel.Cache.GetSpeckleObjects(out var structuralObjects));

      Assert.NotEmpty(structuralObjects);
      Assert.Contains(structuralObjects, so => so is GSAInfluenceBeam);

      var speckleInfBeams = structuralObjects.FindAll(so => so is GSAInfluenceBeam).Select(so => (GSAInfluenceBeam)so).ToList();

      //Checks - inf beam 1
      Assert.Equal("inf beam 1", speckleInfBeams[0].applicationId);
      Assert.Equal(gsaInfBeams[0].Index.Value, speckleInfBeams[0].nativeId);
      Assert.Equal(gsaInfBeams[0].Name, speckleInfBeams[0].name);
      Assert.Equal("element 1", speckleInfBeams[0].element.applicationId);
      Assert.Equal(gsaInfBeams[0].Position.Value, speckleInfBeams[0].position);
      Assert.Equal(gsaInfBeams[0].Factor.Value, speckleInfBeams[0].factor);
      Assert.Equal(InfluenceType.FORCE, speckleInfBeams[0].type);
      Assert.Equal(LoadDirection.YY, speckleInfBeams[0].direction);

      //Checks - inf beam 2
      Assert.Equal("inf beam 2", speckleInfBeams[1].applicationId);
      Assert.Equal(gsaInfBeams[1].Index.Value, speckleInfBeams[1].nativeId);
      Assert.Equal(gsaInfBeams[1].Name, speckleInfBeams[1].name);
      Assert.Equal("element 1", speckleInfBeams[1].element.applicationId);
      Assert.Equal(gsaInfBeams[1].Position.Value, speckleInfBeams[1].position);
      Assert.Equal(gsaInfBeams[1].Factor.Value, speckleInfBeams[1].factor);
      Assert.Equal(InfluenceType.DISPLACEMENT, speckleInfBeams[1].type);
      Assert.Equal(LoadDirection.YY, speckleInfBeams[1].direction);
    }

    [Fact]
    public void InfNodeToSpeckle()
    {
      //Define GSA objects
      //These should be in order that respects the type dependency tree (which is only available in the GSAProxy library, which isn't referenced yet
      var gsaRecords = new List<GsaRecord>();

      //Generation #1: Types with no other dependencies - the leaves of the tree
      gsaRecords.Add(GsaAxisExample("axis 1"));
      gsaRecords.Add(GsaPropMassExample("property mass 1"));
      gsaRecords.Add(GsaPropSprExample("property spring 1"));

      //Gen #2
      gsaRecords.Add(GsaNodeExamples(1, "node 1").First());

      //Gen #4
      var gsaInfNodes = GsaInfNodeExamples(2, "inf node 1", "inf node 2");
      gsaRecords.AddRange(gsaInfNodes);

      Instance.GsaModel.Cache.Upsert(gsaRecords);

      foreach (var record in gsaRecords)
      {
        var speckleObjects = converter.ConvertToSpeckle(new List<object> { record });
        Assert.Empty(converter.ConversionErrors);

        Instance.GsaModel.Cache.SetSpeckleObjects(record, speckleObjects.ToDictionary(so => so.applicationId, so => (object)so));
      }

      Assert.True(Instance.GsaModel.Cache.GetSpeckleObjects(out var structuralObjects));

      Assert.NotEmpty(structuralObjects);
      Assert.Contains(structuralObjects, so => so is GSAInfluenceNode);

      var speckleInfNodes = structuralObjects.FindAll(so => so is GSAInfluenceNode).Select(so => (GSAInfluenceNode)so).ToList();

      //Checks - inf node 1
      Assert.Equal("inf node 1", speckleInfNodes[0].applicationId);
      Assert.Equal(gsaInfNodes[0].Index.Value, speckleInfNodes[0].nativeId);
      Assert.Equal(gsaInfNodes[0].Name, speckleInfNodes[0].name);
      Assert.Equal("node 1", speckleInfNodes[0].node.applicationId);
      Assert.Equal(gsaInfNodes[0].Factor.Value, speckleInfNodes[0].factor);
      Assert.Equal(InfluenceType.DISPLACEMENT, speckleInfNodes[0].type);
      Assert.True(speckleInfNodes[0].axis.definition.IsGlobal());
      Assert.Equal(LoadDirection.Z, speckleInfNodes[0].direction);

      //Checks - inf node 2
      Assert.Equal("inf node 2", speckleInfNodes[1].applicationId);
      Assert.Equal(gsaInfNodes[1].Index.Value, speckleInfNodes[1].nativeId);
      Assert.Equal(gsaInfNodes[1].Name, speckleInfNodes[1].name);
      Assert.Equal("node 1", speckleInfNodes[1].node.applicationId);
      Assert.Equal(gsaInfNodes[1].Factor.Value, speckleInfNodes[1].factor);
      Assert.Equal(InfluenceType.FORCE, speckleInfNodes[1].type);
      Assert.Equal("axis 1", speckleInfNodes[1].axis.applicationId);
      Assert.Equal(LoadDirection.Z, speckleInfNodes[1].direction);
    }

    [Fact]
    public void AlignToSpeckle()
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
      gsaRecords.AddRange(GsaNodeExamples(2, "node 1", "node 2"));
      gsaRecords.Add(GsaCatalogueSectionExample("section 1"));
      gsaRecords.Add(GsaGridPlaneExamples(1, "grid plane 1").FirstOrDefault());

      //Gen #3
      gsaRecords.Add(GsaElement1dExamples(1, "beam 1").FirstOrDefault());

      //Gen #4
      gsaRecords.Add(GsaGridSurfaceExamples(1, "grid surface 1").FirstOrDefault());

      //Gen #5
      var gsaAligns = GsaAlignExamples(2, "align 1", "align 2");
      gsaRecords.AddRange(gsaAligns);

      Instance.GsaModel.Cache.Upsert(gsaRecords);

      //Convert
      Instance.GsaModel.StreamLayer = GSALayer.Both;
      Instance.GsaModel.StreamSendConfig = StreamContentConfig.ModelAndResults;
      var speckleObjects = converter.ConvertToSpeckle(gsaRecords.Select(i => (object)i).ToList());

      //Get speckle results
      Assert.NotEmpty(speckleObjects);
      Assert.Contains(speckleObjects, so => so is Model);
      var speckleModels = speckleObjects.FindAll(so => so is Model).Select(so => (Model)so).ToList();
      List<GSAAlignment> speckleAligns;

      foreach (var m in speckleModels)
      {
        Assert.NotNull(m);
        Assert.NotEmpty(m.elements);
        Assert.Contains(m.elements, o => o is GSAAlignment);
        speckleAligns = m.elements.FindAll(so => so is GSAAlignment).Select(so => (GSAAlignment)so).ToList();

        //Checks - align 1
        Assert.Equal("align 1", speckleAligns[0].applicationId);
        Assert.Equal(gsaAligns[0].Index.Value, speckleAligns[0].nativeId);
        Assert.Equal(gsaAligns[0].Name, speckleAligns[0].name);
        Assert.Equal("grid surface 1", speckleAligns[0].gridSurface.applicationId);
        Assert.Equal(gsaAligns[0].Chain, speckleAligns[0].chainage);
        Assert.Equal(gsaAligns[0].Curv, speckleAligns[0].curvature);

        //Checks - align 2
        Assert.Equal("align 2", speckleAligns[1].applicationId);
        Assert.Equal(gsaAligns[1].Index.Value, speckleAligns[1].nativeId);
        Assert.Equal(gsaAligns[1].Name, speckleAligns[1].name);
        Assert.Equal("grid surface 1", speckleAligns[1].gridSurface.applicationId);
        Assert.Equal(gsaAligns[1].Chain, speckleAligns[1].chainage);
        Assert.Equal(gsaAligns[1].Curv, speckleAligns[1].curvature);
      }
    }

    [Fact]
    public void PathToSpeckle()
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
      gsaRecords.AddRange(GsaNodeExamples(2, "node 1", "node 2"));
      gsaRecords.Add(GsaCatalogueSectionExample("section 1"));
      gsaRecords.Add(GsaGridPlaneExamples(1, "grid plane 1").FirstOrDefault());

      //Gen #3
      gsaRecords.Add(GsaElement1dExamples(1, "beam 1").FirstOrDefault());

      //Gen #4
      gsaRecords.Add(GsaGridSurfaceExamples(1, "grid surface 1").FirstOrDefault());

      //Gen #5
      gsaRecords.Add(GsaAlignExamples(1, "align 1").FirstOrDefault());

      //Gen #6
      var gsaPaths = GsaPathExamples(2, "path 1", "path 2");
      gsaRecords.AddRange(gsaPaths);

      Instance.GsaModel.Cache.Upsert(gsaRecords);

      //Convert
      Instance.GsaModel.StreamLayer = GSALayer.Both;
      Instance.GsaModel.StreamSendConfig = StreamContentConfig.ModelAndResults;
      var speckleObjects = converter.ConvertToSpeckle(gsaRecords.Select(i => (object)i).ToList());

      //Get speckle results
      Assert.NotEmpty(speckleObjects);
      Assert.Contains(speckleObjects, so => so is Model);
      var speckleModels = speckleObjects.FindAll(so => so is Model).Select(so => (Model)so).ToList();
      List<GSAPath> specklePaths;

      foreach (var m in speckleModels)
      {
        Assert.NotNull(m);
        Assert.NotEmpty(m.elements);
        Assert.Contains(m.elements, o => o is GSAPath);
        specklePaths = m.elements.FindAll(so => so is GSAPath).Select(so => (GSAPath)so).ToList();

        //Checks - path 1
        Assert.Equal("path 1", specklePaths[0].applicationId);
        Assert.Equal(gsaPaths[0].Index.Value, specklePaths[0].nativeId);
        Assert.Equal(gsaPaths[0].Name, specklePaths[0].name);
        Assert.Equal(Objects.Structural.GSA.Bridge.PathType.CWAY_1WAY, specklePaths[0].type);
        Assert.Equal(gsaPaths[0].Group.Value, specklePaths[0].group);
        Assert.Equal("align 1", specklePaths[0].alignment.applicationId);
        Assert.Equal(gsaPaths[0].Left.Value, specklePaths[0].left);
        Assert.Equal(gsaPaths[0].Right.Value, specklePaths[0].right);
        Assert.Equal(gsaPaths[0].Factor.Value, specklePaths[0].factor);
        Assert.Equal(0, specklePaths[0].numMarkedLanes);

        //Checks - path 2
        Assert.Equal("path 2", specklePaths[1].applicationId);
        Assert.Equal(gsaPaths[1].Index.Value, specklePaths[1].nativeId);
        Assert.Equal(gsaPaths[1].Name, specklePaths[1].name);
        Assert.Equal(Objects.Structural.GSA.Bridge.PathType.TRACK, specklePaths[1].type);
        Assert.Equal(gsaPaths[1].Group.Value, specklePaths[1].group);
        Assert.Equal("align 1", specklePaths[1].alignment.applicationId);
        Assert.Equal(0, specklePaths[1].left);
        Assert.Equal(gsaPaths[1].Right.Value, specklePaths[1].right);
        Assert.Equal(gsaPaths[1].Factor.Value, specklePaths[1].factor);
        Assert.Equal(0, specklePaths[1].numMarkedLanes);
      }
    }

    [Fact]
    public void UserVehicleToSpeckle()
    {
      //Define GSA objects
      //These should be in order that respects the type dependency tree (which is only available in the GSAProxy library, which isn't referenced yet
      var gsaRecords = new List<GsaRecord>();

      //Generation #1: Types with no other dependencies - the leaves of the tree
      var gsaVehicles = GsaUserVehicleExamples(2, "vehicle 1", "vehicle 2");
      gsaRecords.AddRange(gsaVehicles);

      Instance.GsaModel.Cache.Upsert(gsaRecords);

      foreach (var record in gsaRecords)
      {
        var speckleObjects = converter.ConvertToSpeckle(new List<object> { record });
        Assert.Empty(converter.ConversionErrors);

        Instance.GsaModel.Cache.SetSpeckleObjects(record, speckleObjects.ToDictionary(so => so.applicationId, so => (object)so));
      }

      Assert.True(Instance.GsaModel.Cache.GetSpeckleObjects(out var structuralObjects));

      Assert.NotEmpty(structuralObjects);
      Assert.Contains(structuralObjects, so => so is GSAUserVehicle);

      var speckleVehicles = structuralObjects.FindAll(so => so is GSAUserVehicle).Select(so => (GSAUserVehicle)so).ToList();

      //Checks - vehicle 1
      Assert.Equal("vehicle 1", speckleVehicles[0].applicationId);
      Assert.Equal(gsaVehicles[0].Index.Value, speckleVehicles[0].nativeId);
      Assert.Equal(gsaVehicles[0].Name, speckleVehicles[0].name);
      Assert.Equal(gsaVehicles[0].Width.Value, speckleVehicles[0].width);
      Assert.Equal(gsaVehicles[0].AxlePosition, speckleVehicles[0].axlePositions);
      Assert.Equal(gsaVehicles[0].AxleOffset, speckleVehicles[0].axleOffsets);
      Assert.Equal(gsaVehicles[0].AxleLeft, speckleVehicles[0].axleLeft);
      Assert.Equal(gsaVehicles[0].AxleRight, speckleVehicles[0].axleRight);

      //Checks - vehicle 2
      Assert.Equal("vehicle 2", speckleVehicles[1].applicationId);
      Assert.Equal(gsaVehicles[1].Index.Value, speckleVehicles[1].nativeId);
      Assert.Equal(gsaVehicles[1].Name, speckleVehicles[1].name);
      Assert.Equal(gsaVehicles[1].Width.Value, speckleVehicles[1].width);
      Assert.Equal(gsaVehicles[1].AxlePosition, speckleVehicles[1].axlePositions);
      Assert.Equal(gsaVehicles[1].AxleOffset, speckleVehicles[1].axleOffsets);
      Assert.Equal(gsaVehicles[1].AxleLeft, speckleVehicles[1].axleLeft);
      Assert.Equal(gsaVehicles[1].AxleRight, speckleVehicles[1].axleRight);
    }
    #endregion

    #region Helper
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
          ParentIndex = 0,
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
          ParentIndex = 0,
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
          ParentIndex = 0,
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
          ParentIndex = 0,
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
          ParentIndex = 0,
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
          ElementIndices = new List<int>() { 1, 2 },
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
          ElementIndices = new List<int>() { 1 },
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
          Angle = 0,
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
          Releases1 = new Dictionary<GwaAxisDirection6, ReleaseCode>()
          {
            { GwaAxisDirection6.X, ReleaseCode.Fixed },
            { GwaAxisDirection6.Y, ReleaseCode.Fixed },
            { GwaAxisDirection6.Z, ReleaseCode.Fixed },
            { GwaAxisDirection6.XX, ReleaseCode.Fixed },
            { GwaAxisDirection6.YY, ReleaseCode.Fixed },
            { GwaAxisDirection6.ZZ, ReleaseCode.Fixed }
          },
          //Stiffnesses1 = new List<double>(),
          Releases2 = new Dictionary<GwaAxisDirection6, ReleaseCode>()
          {
            { GwaAxisDirection6.X, ReleaseCode.Fixed },
            { GwaAxisDirection6.Y, ReleaseCode.Fixed },
            { GwaAxisDirection6.Z, ReleaseCode.Fixed },
            { GwaAxisDirection6.XX, ReleaseCode.Fixed },
            { GwaAxisDirection6.YY, ReleaseCode.Fixed },
            { GwaAxisDirection6.ZZ, ReleaseCode.Fixed }
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
          End1OffsetX = 0,
          End2OffsetX = 0,
          OffsetY = 0,
          OffsetZ = 0,
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
          Angle = 0,
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

    private List<GsaGridLine> GsaGridLineExamples(int numOfGridLines, params string[] appIds)
    {
      var gsaGridLines = new List<GsaGridLine>()
      {
        new GsaGridLine()
        {
          Index = 1,
          Name = "1",
          Type = GridLineType.Line,
          XCoordinate = 0,
          YCoordinate = 0,
          Length = 1,
          Theta1 = 0,
          //Theta2 = null,
        },
        new GsaGridLine()
        {
          Index = 2,
          Name = "2",
          Type = GridLineType.Arc,
          XCoordinate = 0,
          YCoordinate = 0,
          Length = 1,
          Theta1 = 0,
          Theta2 = 30,
        }
      };
      for (int i = 0; i < appIds.Count(); i++)
      {
        gsaGridLines[i].ApplicationId = appIds[i];
      }
      return gsaGridLines.GetRange(0, numOfGridLines);
    }

    private List<GsaGridPlane> GsaGridPlaneExamples(int numOfGridPlanes, params string[] appIds)
    {
      var gsaGridPlanes = new List<GsaGridPlane>()
      {
        new GsaGridPlane()
        {
          Index = 1,
          Name = "1",
          Type = GridPlaneType.Storey,
          AxisRefType = GridPlaneAxisRefType.Global,
          //AxisIndex = null,
          Elevation = 0,
          StoreyToleranceBelowAuto = false,
          StoreyToleranceBelow = 0.1,
          StoreyToleranceAboveAuto = false,
          StoreyToleranceAbove = 0.1,
        },
        new GsaGridPlane()
        {
          Index = 2,
          Name = "2",
          Type = GridPlaneType.General,
          AxisRefType = GridPlaneAxisRefType.Reference,
          AxisIndex = 1,
          Elevation = 0,
          StoreyToleranceBelowAuto = false,
          //StoreyToleranceBelow = null,
          StoreyToleranceAboveAuto = false,
          //StoreyToleranceAbove = null,
        }
      };
      for (int i = 0; i < appIds.Count(); i++)
      {
        gsaGridPlanes[i].ApplicationId = appIds[i];
      }
      return gsaGridPlanes.GetRange(0, numOfGridPlanes);
    }

    private List<GsaGridSurface> GsaGridSurfaceExamples(int numOfGridSurfaces, params string[] appIds)
    {
      var gsaGridSurfaces = new List<GsaGridSurface>()
      {
        new GsaGridSurface()
        {
          Index = 1,
          Name = "1",
          PlaneRefType = GridPlaneAxisRefType.Global,
          //PlaneIndex = null,
          Type = GridSurfaceElementsType.OneD,
          ElementIndices = new List<int>(){ 1 },
          Tolerance = 0.01,
          Span = GridSurfaceSpan.One,
          Angle = 0,
          Expansion = GridExpansion.PlaneAspect,
        },
        new GsaGridSurface()
        {
          Index = 2,
          Name = "2",
          PlaneRefType = GridPlaneAxisRefType.Reference,
          PlaneIndex = 1,
          Type = GridSurfaceElementsType.TwoD,
          ElementIndices = new List<int>(){ 2 },
          Tolerance = 0.01,
          Span = GridSurfaceSpan.Two,
          Angle = 0,
          Expansion = GridExpansion.PlaneAspect,
        }
      };
      for (int i = 0; i < appIds.Count(); i++)
      {
        gsaGridSurfaces[i].ApplicationId = appIds[i];
      }
      return gsaGridSurfaces.GetRange(0, numOfGridSurfaces);
    }

    private List<GsaPolyline> GsaPolylineExamples(int numPolylines, params string[] appIds)
    {
      var gsaPolylines = new List<GsaPolyline>()
      {
        new GsaPolyline()
        {
          Index = 1,
          Name = "1",
          Colour = Colour.NO_RGB,
          GridPlaneIndex = null,
          NumDim = 2,
          Values = new List<double>() { 1, 2, 3, 4, 5, 6, 7, 8 },
          Units = "m",
        },
        new GsaPolyline()
        {
          Index = 2,
          Name = "2",
          Colour = Colour.NO_RGB,
          GridPlaneIndex = 1,
          NumDim = 3,
          Values = new List<double>() { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12 },
          Units = "m",
        }
      };
      for (int i = 0; i < appIds.Count(); i++)
      {
        gsaPolylines[i].ApplicationId = appIds[i];
      }
      return gsaPolylines.GetRange(0, numPolylines);
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
          Source = 1,
          Category = LoadCategory.NotSet,
          Direction = AxisDirection3.Z,
          Include = IncludeOption.Undefined,
          //Bridge = null,
        },
        new GsaLoadCase()
        {
          Index = 2,
          Title = "Live",
          CaseType = StructuralLoadCaseType.Live,
          Source = 2,
          Category = LoadCategory.NotSet,
          Direction = AxisDirection3.Z,
          Include = IncludeOption.Undefined,
          //Bridge = null,
        }
      };
      for (int i = 0; i < appIds.Count(); i++)
      {
        gsaLoadCases[i].ApplicationId = appIds[i];
      }
      return gsaLoadCases.GetRange(0, numberOfLoadCases);
    }

    private List<GsaAnal> GsaAnalysisCaseExamples(int num, params string[] appIds)
    {
      var gsaAnals = new List<GsaAnal>()
      {
        new GsaAnal()
        {
          Index = 1,
          Name = "1",
          TaskIndex = 1,
          Desc = "L1",
        },
        new GsaAnal()
        {
          Index = 2,
          Name = "2",
          TaskIndex = 1,
          Desc = "1.2L1 + 1.5L2",
        },
      };
      for (int i = 0; i < appIds.Count(); i++)
      {
        gsaAnals[i].ApplicationId = appIds[i];
      }
      return gsaAnals.GetRange(0, num);
    }

    private List<GsaLoad2dFace> GsaLoad2dFaceExamples(int numberOfLoads, params string[] appIds)
    {
      var gsaLoad2dFaces = new List<GsaLoad2dFace>()
      {
        new GsaLoad2dFace()
        {
          Index = 1,
          Name = "1",
          ElementIndices = new List<int>(){ 1 },
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
          ElementIndices = new List<int>(){ 2 },
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
          ElementIndices = new List<int>(){ 3 },
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
          ElementIndices = new List<int>(){ 1 },
          LoadCaseIndex = 1,
          AxisRefType = LoadBeamAxisRefType.Global,
          Projected = false,
          LoadDirection = GwaAxisDirection6.Z,
          Position = 0,
          Load = 1
        },
        new GsaLoadBeamUdl()
        {
          Index = 2,
          Name  = "2",
          ElementIndices = new List<int>(){ 2 },
          LoadCaseIndex = 1,
          AxisRefType = LoadBeamAxisRefType.Reference,
          AxisIndex = 1,
          Projected = false,
          LoadDirection = GwaAxisDirection6.X,
          Load = 1,
        },
        new GsaLoadBeamLine()
        {
          Index = 3,
          Name  = "3",
          ElementIndices = new List<int>(){ 1 },
          LoadCaseIndex = 1,
          AxisRefType = LoadBeamAxisRefType.Local,
          Projected = false,
          LoadDirection = GwaAxisDirection6.Y,
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
          LoadDirection = GwaAxisDirection6.Z,
          Value = 1
        },
        new GsaLoadNode()
        {
          Index = 2,
          Name  = "2",
          LoadCaseIndex = 1,
          NodeIndices = new List<int>() { 2 },
          GlobalAxis = false,
          AxisIndex = 1,
          LoadDirection = GwaAxisDirection6.X,
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
        ElementIndices = new List<int>() { 1, 2 },
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

    private List<GsaLoadGridArea> GsaLoadGridAreaExamples(int numOfLoads, params string[] appIds)
    {
      var gsaLoads = new List<GsaLoadGridArea>()
      {
        new GsaLoadGridArea()
        {
          Index = 1,
          Name = "1",
          GridSurfaceIndex = 1,
          Area = LoadAreaOption.Plane,
          //PolygonIndex = null,
          Polygon = "",
          LoadCaseIndex = 1,
          AxisRefType = AxisRefType.Global,
          //AxisIndex = null,
          Projected = false,
          LoadDirection = AxisDirection3.Z,
          Value = 1,
        },
        new GsaLoadGridArea()
        {
          Index = 2,
          Name = "2",
          GridSurfaceIndex = 1,
          Area = LoadAreaOption.PolyRef,
          PolygonIndex = 1,
          Polygon = "",
          LoadCaseIndex = 2,
          AxisRefType = AxisRefType.Reference,
          AxisIndex = 1,
          Projected = false,
          LoadDirection = AxisDirection3.Z,
          Value = 2,
        }
      };
      for (int i = 0; i < appIds.Count(); i++)
      {
        gsaLoads[i].ApplicationId = appIds[i];
      }
      return gsaLoads.GetRange(0, numOfLoads);
    }

    private List<GsaLoadGridLine> GsaLoadGridLineExamples(int numOfLoads, params string[] appIds)
    {
      var gsaLoads = new List<GsaLoadGridLine>()
      {
        new GsaLoadGridLine()
        {
          Index = 1,
          Name = "1",
          GridSurfaceIndex = 1,
          Line = LoadLineOption.PolyRef,
          Polygon = "",
          PolygonIndex = 1,
          LoadCaseIndex = 1,
          AxisRefType = AxisRefType.Global,
          //AxisIndex = null,
          Projected = false,
          LoadDirection = AxisDirection3.Z,
          Value1 = 1,
          Value2 = 2,
        },
        new GsaLoadGridLine()
        {
          Index = 2,
          Name = "2",
          GridSurfaceIndex = 1,
          Line = LoadLineOption.Polygon,
          Polygon = "(0,0) (1,0) (1,1) (0,1)(m)",
          //PolygonIndex = null,
          LoadCaseIndex = 2,
          AxisRefType = AxisRefType.Reference,
          AxisIndex = 1,
          Projected = false,
          LoadDirection = AxisDirection3.Z,
          Value1 = 3,
          Value2 = 4,
        }
      };
      for (int i = 0; i < appIds.Count(); i++)
      {
        gsaLoads[i].ApplicationId = appIds[i];
      }
      return gsaLoads.GetRange(0, numOfLoads);
    }

    private List<GsaLoadGridPoint> GsaLoadGridPointExamples(int numOfLoads, params string[] appIds)
    {
      var gsaLoads = new List<GsaLoadGridPoint>()
      {
        new GsaLoadGridPoint()
        {
          Index = 1,
          Name = "1",
          GridSurfaceIndex = 1,
          X = 0,
          Y = 0,
          LoadCaseIndex = 1,
          AxisRefType = AxisRefType.Global,
          //AxisIndex = null,
          LoadDirection = AxisDirection3.Z,
          Value = 1
        },
        new GsaLoadGridPoint()
        {
          Index = 2,
          Name = "2",
          GridSurfaceIndex = 1,
          X = 0,
          Y = 0,
          LoadCaseIndex = 2,
          AxisRefType = AxisRefType.Reference,
          AxisIndex = 1,
          LoadDirection = AxisDirection3.Z,
          Value = 2
        }
      };
      for (int i = 0; i < appIds.Count(); i++)
      {
        gsaLoads[i].ApplicationId = appIds[i];
      }
      return gsaLoads.GetRange(0, numOfLoads);
    }

    private List<GsaLoad2dThermal> GsaLoad2dThermalExamples(int numOfLoads, params string[] appIds)
    {
      var gsaLoads = new List<GsaLoad2dThermal>()
      {
        new GsaLoad2dThermal()
        {
          Index = 1,
          Name = "1",
          ElementIndices = new List<int>(){ 1 },
          LoadCaseIndex = 1,
          Type = Load2dThermalType.Uniform,
          Values = new List<double>(){ 1 },
        },
        new GsaLoad2dThermal()
        {
          Index = 2,
          Name = "2",
          ElementIndices = new List<int>(){ 1 },
          LoadCaseIndex = 2,
          Type = Load2dThermalType.Gradient,
          Values = new List<double>(){ 1, 2 },
        }
      };
      for (int i = 0; i < appIds.Count(); i++)
      {
        gsaLoads[i].ApplicationId = appIds[i];
      }
      return gsaLoads.GetRange(0, numOfLoads);
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
          PtsUC = null, //new double[0],
          NumSC = 0,
          AbsSC = Dimension.NotSet,
          OrdSC = Dimension.NotSet,
          PtsSC = null, //new double[0],
          NumUT = 0,
          AbsUT = Dimension.NotSet,
          OrdUT = Dimension.NotSet,
          PtsUT = null, //new double[0],
          NumST = 0,
          AbsST = Dimension.NotSet,
          OrdST = Dimension.NotSet,
          PtsST = null, //new double[0],
          Eps = 0,
          Uls = new GsaMatCurveParam()
          {
            Model = new List<MatCurveParamType>() { MatCurveParamType.RECTANGLE, MatCurveParamType.NO_TENSION },
            StrainElasticCompression = 0.00039,
            StrainElasticTension = 0,
            StrainPlasticCompression = 0.00039,
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
        EmEs = null,
        N = 2,
        Emod = 1,
        EpsPeak = 0.003,
        EpsMax = 0.00039,
        EpsU = 0.003,
        EpsAx = 0.0025,
        EpsTran = 0.002,
        EpsAxs = 0.0025,
        Light = false,
        Agg = 0.02,
        XdMin = 0,
        XdMax = 1,
        Beta = 0.87,
        Shrink = null,
        Confine = null,
        Fcc = null,
        EpsPlasC = null,
        EpsUC = null
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
          PtsUC = null, //new double[0],
          NumSC = 0,
          AbsSC = Dimension.NotSet,
          OrdSC = Dimension.NotSet,
          PtsSC = null, //new double[0],
          NumUT = 0,
          AbsUT = Dimension.NotSet,
          OrdUT = Dimension.NotSet,
          PtsUT = null, //new double[0],
          NumST = 0,
          AbsST = Dimension.NotSet,
          OrdST = Dimension.NotSet,
          PtsST = null, //new double[0],
          Eps = 0.05,
          Uls = new GsaMatCurveParam()
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
        MatType = Property2dMaterialType.Steel,
        GradeIndex = 1,
        Thickness = 0.01,
        RefPt = Property2dRefSurface.Centroid,
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
        Fraction = 1,
        Components = new List<GsaSectionComponentBase>()
        {
          new SectionComp()
          {
            //MatAnalIndex = 0,
            MaterialType = Section1dMaterialType.STEEL,
            MaterialIndex = 1,
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

    private GsaSection GsaExplicitSectionExample(string appId)
    {
      return new GsaSection()
      {
        Index = 2,
        Name = "2",
        ApplicationId = appId,
        Colour = Colour.NO_RGB,
        Type = Section1dType.Generic,
        //PoolIndex = 0,
        ReferencePoint = ReferencePoint.Centroid,
        Fraction = 1,
        Components = new List<GsaSectionComponentBase>()
        {
          new SectionComp()
          {
            //MatAnalIndex = 0,
            MaterialType = Section1dMaterialType.STEEL,
            MaterialIndex = 1,
            Reflect = ComponentReflection.NONE,
            //Pool = 0,
            TaperType = Section1dTaperType.NONE,
            //TaperPos = 0
            ProfileGroup = Section1dProfileGroup.Explicit,
            ProfileDetails = new ProfileDetailsExplicit()
            {
              Group = Section1dProfileGroup.Explicit,
              Area = 1,
              Iyy = 2,
              Izz = 3,
              J = 4,
              Ky = 5,
              Kz = 6
            }
          },
          new SectionSteel()
          {
            //GradeIndex = 0,
            PlasElas = 1,
            NetGross = 1,
            Exposed = 1,
            Beta = 0.4,
            Type = SectionSteelSectionType.Undefined,
            Plate = SectionSteelPlateType.Undefined,
            Locked = false
          }
        },
        Environ = false
      };
    }

    private GsaSection GsaPerimeterSectionExample(string appId)
    {
      return new GsaSection()
      {
        Index = 3,
        Name = "3",
        ApplicationId = appId,
        Colour = Colour.NO_RGB,
        Type = Section1dType.Generic,
        //PoolIndex = 0,
        ReferencePoint = ReferencePoint.Centroid,
        Fraction = 1,
        Components = new List<GsaSectionComponentBase>()
        {
          new SectionComp()
          {
            //MatAnalIndex = 0,
            MaterialType = Section1dMaterialType.STEEL,
            MaterialIndex = 1,
            Reflect = ComponentReflection.NONE,
            //Pool = 0,
            TaperType = Section1dTaperType.NONE,
            //TaperPos = 0
            ProfileGroup = Section1dProfileGroup.Perimeter,
            ProfileDetails = new ProfileDetailsPerimeter()
            {
              Group = Section1dProfileGroup.Perimeter,
              Type = "P",
              Actions = new List<string>(){ "M", "L", "L", "L", "M", "L", "L", "L" },
              Y = new List<double?>(){ -50, 50, 50, -50, -40, 40, 40, -40 },
              Z = new List<double?>(){ -50, -50, 50, 50, -40, -40, 40, 40}
            }
          },
          new SectionSteel()
          {
            //GradeIndex = 0,
            PlasElas = 1,
            NetGross = 1,
            Exposed = 1,
            Beta = 0.4,
            Type = SectionSteelSectionType.Undefined,
            Plate = SectionSteelPlateType.Undefined,
            Locked = false
          }
        },
        Environ = false
      };
    }

    private GsaSection GsaStandardSectionExample(string appId)
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
        Fraction = 1,
        Components = new List<GsaSectionComponentBase>()
        {
          new SectionComp()
          {
            //MatAnalIndex = 0,
            MaterialType = Section1dMaterialType.STEEL,
            MaterialIndex = 1,
            Reflect = ComponentReflection.NONE,
            //Pool = 0,
            TaperType = Section1dTaperType.NONE,
            //TaperPos = 0
            ProfileGroup = Section1dProfileGroup.Standard,
          },
          new SectionSteel()
          {
            //GradeIndex = 0,
            PlasElas = 1,
            NetGross = 1,
            Exposed = 1,
            Beta = 0.4,
            Type = SectionSteelSectionType.Undefined,
            Plate = SectionSteelPlateType.Undefined,
            Locked = false
          }
        },
        Environ = false
      };
    }

    private GsaSection GsaRectangularSectionExample(string appId)
    {
      var gsaProfile = new ProfileDetailsRectangular();
      gsaProfile.FromDesc("STD R 100 50");
      var gsaSection = GsaStandardSectionExample(appId);
      gsaSection.Index = 4;
      gsaSection.Name = "4";
      ((SectionComp)gsaSection.Components[0]).ProfileDetails = gsaProfile;
      return gsaSection;
    }

    private GsaSection GsaRectangularHollowSectionExample(string appId)
    {
      var gsaProfile = new ProfileDetailsTwoThickness();
      gsaProfile.FromDesc("STD RHS 100 50 5 10");
      var gsaSection = GsaStandardSectionExample(appId);
      gsaSection.Index = 5;
      gsaSection.Name = "5";
      ((SectionComp)gsaSection.Components[0]).ProfileDetails = gsaProfile;
      return gsaSection;
    }

    private GsaSection GsaCircularSectionExample(string appId)
    {
      var gsaProfile = new ProfileDetailsCircular();
      gsaProfile.FromDesc("STD C 100");
      var gsaSection = GsaStandardSectionExample(appId);
      gsaSection.Index = 6;
      gsaSection.Name = "6";
      ((SectionComp)gsaSection.Components[0]).ProfileDetails = gsaProfile;
      return gsaSection;
    }

    private GsaSection GsaCircularHollowSectionExample(string appId)
    {
      var gsaProfile = new ProfileDetailsCircularHollow();
      gsaProfile.FromDesc("STD CHS 100 5");
      var gsaSection = GsaStandardSectionExample(appId);
      gsaSection.Index = 7;
      gsaSection.Name = "7";
      ((SectionComp)gsaSection.Components[0]).ProfileDetails = gsaProfile;
      return gsaSection;
    }

    private GsaSection GsaISectionSectionExample(string appId)
    {
      var gsaProfile = new ProfileDetailsTwoThickness();
      gsaProfile.FromDesc("STD I 100 50 5 10");
      var gsaSection = GsaStandardSectionExample(appId);
      gsaSection.Index = 8;
      gsaSection.Name = "8";
      ((SectionComp)gsaSection.Components[0]).ProfileDetails = gsaProfile;
      return gsaSection;
    }

    private GsaSection GsaTSectionSectionExample(string appId)
    {
      var gsaProfile = new ProfileDetailsTwoThickness();
      gsaProfile.FromDesc("STD T 100 50 5 10");
      var gsaSection = GsaStandardSectionExample(appId);
      gsaSection.Index = 9;
      gsaSection.Name = "9";
      ((SectionComp)gsaSection.Components[0]).ProfileDetails = gsaProfile;
      return gsaSection;
    }

    private GsaSection GsaAngleSectionExample(string appId)
    {
      var gsaProfile = new ProfileDetailsTwoThickness();
      gsaProfile.FromDesc("STD A 100 50 5 10");
      var gsaSection = GsaStandardSectionExample(appId);
      gsaSection.Index = 10;
      gsaSection.Name = "10";
      ((SectionComp)gsaSection.Components[0]).ProfileDetails = gsaProfile;
      return gsaSection;
    }

    private GsaSection GsaChannelSectionExample(string appId)
    {
      var gsaProfile = new ProfileDetailsTwoThickness();
      gsaProfile.FromDesc("STD CH 100 50 5 10");
      var gsaSection = GsaStandardSectionExample(appId);
      gsaSection.Index = 11;
      gsaSection.Name = "11";
      ((SectionComp)gsaSection.Components[0]).ProfileDetails = gsaProfile;
      return gsaSection;
    }

    private GsaSection GsaLineSegmentSectionExample(string appId)
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
        Fraction = 1,
        Components = new List<GsaSectionComponentBase>()
        {
          new SectionComp()
          {
            //MatAnalIndex = 0,
            MaterialType = Section1dMaterialType.STEEL,
            MaterialIndex = 1,
            Reflect = ComponentReflection.NONE,
            //Pool = 0,
            TaperType = Section1dTaperType.NONE,
            //TaperPos = 0
            ProfileGroup = Section1dProfileGroup.Perimeter,
            ProfileDetails = new ProfileDetailsPerimeter()
            {
              Group = Section1dProfileGroup.Perimeter,
              Type = "L(mm)",
              Actions = new List<string>(){ "T", "M", "L", "L", "L", "L" },
              Y = new List<double?>(){ 10, -45, 45, 45, -45, -45 },
              Z = new List<double?>(){ null, -45, -45, 45, 45, -45}
            }
          },
          new SectionSteel()
          {
            //GradeIndex = 0,
            PlasElas = 1,
            NetGross = 1,
            Exposed = 1,
            Beta = 0.4,
            Type = SectionSteelSectionType.Undefined,
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
        Stiffnesses = new Dictionary<GwaAxisDirection6, double>
        {
          { GwaAxisDirection6.X, 10 },
          { GwaAxisDirection6.Y, 11 },
          { GwaAxisDirection6.Z, 12 },
          { GwaAxisDirection6.XX, 13 },
          { GwaAxisDirection6.YY, 14 },
          { GwaAxisDirection6.ZZ, 15 },
        },
        DampingRatio = 5
      };
    }
    #endregion

    #region Results
    private List<CsvRecord> GsaNodeResultExamples()
    {
      return new List<CsvRecord>()
      {
        new CsvNode()
        {
          CaseId = "A1",
          ElemId = 1,
          Ux = 1,
          Uy = 2,
          Uz = 3,
          Rxx = 4,
          Ryy = 5,
          Rzz = 6,
          Vx = 7,
          Vy = 8,
          Vz = 9,
          Vxx = 10,
          Vyy = 11,
          Vzz = 12,
          Ax = 13,
          Ay = 14,
          Az = 15,
          Axx = 16,
          Ayy = 17,
          Azz = 18,
          Fx_Reac = 19,
          Fy_Reac = 20,
          Fz_Reac = 21,
          Mxx_Reac = 22,
          Myy_Reac = 23,
          Mzz_Reac = 24,
          Fx_Cons = 25,
          Fy_Cons = 26,
          Fz_Cons = 27,
          Mxx_Cons = 28,
          Myy_Cons = 29,
          Mzz_Cons = 30,
        },
        new CsvNode()
        {
          CaseId = "A2",
          ElemId = 1,
          Ux = 101,
          Uy = 102,
          Uz = 103,
          Rxx = 104,
          Ryy = 105,
          Rzz = 106,
          Vx = 107,
          Vy = 108,
          Vz = 109,
          Vxx = 110,
          Vyy = 111,
          Vzz = 112,
          Ax = 113,
          Ay = 114,
          Az = 115,
          Axx = 116,
          Ayy = 117,
          Azz = 118,
          Fx_Reac = 119,
          Fy_Reac = 120,
          Fz_Reac = 121,
          Mxx_Reac = 122,
          Myy_Reac = 123,
          Mzz_Reac = 124,
          Fx_Cons = 125,
          Fy_Cons = 126,
          Fz_Cons = 127,
          Mxx_Cons = 128,
          Myy_Cons = 129,
          Mzz_Cons = 130,
        },
      };
    }

    private List<CsvRecord> GsaElement1dResultExamples(int numElements, int numPositions, int numCases)
    {
      var gsaElement1dResult = new List<CsvRecord>();
      for (var ic = 0; ic < numCases; ic++)
      {
        for (var ie = 0; ie < numElements; ie++)
        {
          for (var ip = 0; ip < numPositions; ip++)
          {
            var result = new CsvElem1d()
            {
              CaseId = "A" + (ic + 1).ToString(),
              ElemId = ie + 1,
              PosR = ip.ToString(),
              Ux = ic * 1000 + ie * 100 + ip * 10 + 1,
              Uy = ic * 1000 + ie * 100 + ip * 10 + 2,
              Uz = ic * 1000 + ie * 100 + ip * 10 + 3,
              Fx = ic * 1000 + ie * 100 + ip * 10 + 4,
              Fy = ic * 1000 + ie * 100 + ip * 10 + 5,
              Fz = ic * 1000 + ie * 100 + ip * 10 + 6,
              Mxx = ic * 1000 + ie * 100 + ip * 10 + 7,
              Myy = ic * 1000 + ie * 100 + ip * 10 + 8,
              Mzz = ic * 1000 + ie * 100 + ip * 10 + 9,
            };
            gsaElement1dResult.Add(result);
          }
        }
      }
      return gsaElement1dResult;
    }

    private List<CsvRecord> GsaElement2dResultExamples(int numElements, int numCases)
    {
      var positions = new List<List<float>>()
      {
        new List<float>() { 0, 0 },
        new List<float>() { 1, 0 },
        new List<float>() { 1, 1 },
        new List<float>() { 0, 1 },
        new List<float>() { 0.5f, 0.5f },
      };
      var gsaElement2dResult = new List<CsvRecord>();
      for (var ic = 0; ic < numCases; ic++)
      {
        for (var ie = 0; ie < numElements; ie++)
        {
          for (var ip = 0; ip < positions.Count(); ip++)
          {
            var result = new CsvElem2d()
            {
              CaseId = "A" + (ic + 1).ToString(),
              ElemId = ie + 1,
              PosR = positions[ip][0],
              PosS = positions[ip][1],
              Ux = ic * 10000 + ie * 1000 + ip * 100 + 1,
              Uy = ic * 10000 + ie * 1000 + ip * 100 + 2,
              Uz = ic * 10000 + ie * 1000 + ip * 100 + 3,
              Mx = ic * 10000 + ie * 1000 + ip * 100 + 4,
              My = ic * 10000 + ie * 1000 + ip * 100 + 5,
              Mxy = ic * 10000 + ie * 1000 + ip * 100 + 6,
              Nx = ic * 10000 + ie * 1000 + ip * 100 + 7,
              Ny = ic * 10000 + ie * 1000 + ip * 100 + 8,
              Nxy = ic * 10000 + ie * 1000 + ip * 100 + 9,
              Qx = ic * 10000 + ie * 1000 + ip * 100 + 10,
              Qy = ic * 10000 + ie * 1000 + ip * 100 + 11,
              Xx_b = ic * 10000 + ie * 1000 + ip * 100 + 12,
              Yy_b = ic * 10000 + ie * 1000 + ip * 100 + 13,
              Zz_b = ic * 10000 + ie * 1000 + ip * 100 + 14,
              Xy_b = ic * 10000 + ie * 1000 + ip * 100 + 15,
              Yz_b = ic * 10000 + ie * 1000 + ip * 100 + 16,
              Zx_b = ic * 10000 + ie * 1000 + ip * 100 + 17,
              Xx_m = ic * 10000 + ie * 1000 + ip * 100 + 18,
              Yy_m = ic * 10000 + ie * 1000 + ip * 100 + 19,
              Zz_m = ic * 10000 + ie * 1000 + ip * 100 + 20,
              Xy_m = ic * 10000 + ie * 1000 + ip * 100 + 21,
              Yz_m = ic * 10000 + ie * 1000 + ip * 100 + 22,
              Zx_m = ic * 10000 + ie * 1000 + ip * 100 + 23,
              Xx_t = ic * 10000 + ie * 1000 + ip * 100 + 24,
              Yy_t = ic * 10000 + ie * 1000 + ip * 100 + 25,
              Zz_t = ic * 10000 + ie * 1000 + ip * 100 + 26,
              Xy_t = ic * 10000 + ie * 1000 + ip * 100 + 27,
              Yz_t = ic * 10000 + ie * 1000 + ip * 100 + 28,
              Zx_t = ic * 10000 + ie * 1000 + ip * 100 + 29,
            };
            gsaElement2dResult.Add(result);
          }
        }
      }
      return gsaElement2dResult;
    }
    #endregion

    #region Constraints
    private List<GsaRigid> GsaRigidExamples(int num, params string[] appIds)
    {
      var gsaRigids = new List<GsaRigid>()
      {
        new GsaRigid()
        {
          Index = 1,
          Name = "1",
          PrimaryNode = 1,
          Type = RigidConstraintType.ALL,
          //Link = null,
          ConstrainedNodes = new List<int>(){ 2 },
          Stage = new List<int>(){ 1 },
          //ParentMember = null,
        },
        new GsaRigid()
        {
          Index = 2,
          Name = "2",
          PrimaryNode = 1,
          Type = RigidConstraintType.Custom,
          Link = new Dictionary<GwaAxisDirection6, List<GwaAxisDirection6>>()
          {
            { GwaAxisDirection6.X, new List<GwaAxisDirection6>() { GwaAxisDirection6.X, GwaAxisDirection6.YY, GwaAxisDirection6.ZZ } },
            { GwaAxisDirection6.Y, new List<GwaAxisDirection6>() { GwaAxisDirection6.Y, GwaAxisDirection6.XX, GwaAxisDirection6.ZZ } },
            { GwaAxisDirection6.Z, new List<GwaAxisDirection6>() { GwaAxisDirection6.Z, GwaAxisDirection6.XX, GwaAxisDirection6.YY } },
            { GwaAxisDirection6.XX, new List<GwaAxisDirection6>() { GwaAxisDirection6.XX } },
            { GwaAxisDirection6.YY, new List<GwaAxisDirection6>() { GwaAxisDirection6.YY } },
            { GwaAxisDirection6.ZZ, new List<GwaAxisDirection6>() { GwaAxisDirection6.ZZ } },
          },
          ConstrainedNodes = new List<int>(){ 2 },
          Stage = new List<int>(){ 1 },
        },
      };
      for (int i = 0; i < appIds.Count(); i++)
      {
        gsaRigids[i].ApplicationId = appIds[i];
      }
      return gsaRigids;
    }

    private List<GsaGenRest> GsaGenRestExamples(int num, params string[] appIds)
    {
      var gsaGenRests = new List<GsaGenRest>()
      {
        new GsaGenRest()
        {
          Index = 1,
          Name = "1",
          X = RestraintCondition.Constrained,
          Y = RestraintCondition.Constrained,
          Z = RestraintCondition.Constrained,
          XX = RestraintCondition.Free,
          YY = RestraintCondition.Free,
          ZZ = RestraintCondition.Free,
          NodeIndices = new List<int>(){ 1 },
          StageIndices = new List<int>(){ 1 },
        },
        new GsaGenRest()
        {
          Index = 2,
          Name = "2",
          X = RestraintCondition.Constrained,
          Y = RestraintCondition.Constrained,
          Z = RestraintCondition.Constrained,
          XX = RestraintCondition.Constrained,
          YY = RestraintCondition.Constrained,
          ZZ = RestraintCondition.Constrained,
          NodeIndices = new List<int>(){ 1, 2 },
          StageIndices = new List<int>(){ 1, 2 },
        },
      };
      for (int i = 0; i < appIds.Count(); i++)
      {
        gsaGenRests[i].ApplicationId = appIds[i];
      }
      return gsaGenRests.GetRange(0, num);
    }
    #endregion

    #region Analysis Stage
    private List<GsaAnalStage> GsaAnalStageExamples(int num, params string[] appIds)
    {
      var gsaAnalStages = new List<GsaAnalStage>()
      {
        new GsaAnalStage()
        {
          Index = 1,
          Name = "1",
          Colour = Colour.NO_RGB,
          ElementIndices = new List<int>(){ 1 },
          //MemberIndices = null,
          Phi = 2,
          Days = 28,
          LockElementIndices = new List<int>() { 2 },
          //LockMemberIndices = null,
        },
        new GsaAnalStage()
        {
          Index = 2,
          Name = "2",
          Colour = Colour.NO_RGB,
          ElementIndices = new List<int>(){ 1 },
          //MemberIndices = new List<int>(){ 1 },
          Phi = 2,
          Days = 28,
          LockElementIndices = new List<int>(){ 2 },
          //LockMemberIndices = new List<int>() { 2 },
        },
      };
      for (int i = 0; i < appIds.Count(); i++)
      {
        gsaAnalStages[i].ApplicationId = appIds[i];
      }
      return gsaAnalStages.GetRange(0, num);
    }
    #endregion

    #region Bridges
    private List<GsaInfBeam> GsaInfBeamExamples(int num, params string[] appIds)
    {
      var gsaInfBeams = new List<GsaInfBeam>()
      {
        new GsaInfBeam()
        {
          Index = 1,
          Name = "1",
          Element = 1,
          Position = 0.5,
          Factor = 1,
          Type = InfType.FORCE,
          Direction = GwaAxisDirection6.YY,
        },
        new GsaInfBeam()
        {
          Index = 2,
          Name = "2",
          Element = 1,
          Position = 0,
          Factor = 2,
          Type = InfType.DISP,
          Direction = GwaAxisDirection6.YY,
        },
      };
      for (int i = 0; i < appIds.Count(); i++)
      {
        gsaInfBeams[i].ApplicationId = appIds[i];
      }
      return gsaInfBeams.GetRange(0, num);
    }

    private List<GsaInfNode> GsaInfNodeExamples(int num, params string[] appIds)
    {
      var gsaInfNodes = new List<GsaInfNode>()
      {
        new GsaInfNode()
        {
          Index = 1,
          Name = "1",
          Node = 1,
          Factor = 1,
          Type = InfType.DISP,
          AxisRefType = AxisRefType.Global,
          //AxisIndex = null,
          Direction = GwaAxisDirection6.Z,
        },
        new GsaInfNode()
        {
          Index = 2,
          Name = "2",
          Node = 1,
          Factor = 2,
          Type = InfType.FORCE,
          AxisRefType = AxisRefType.Reference,
          AxisIndex = 1,
          Direction = GwaAxisDirection6.Z,
        },
      };
      for (int i = 0; i < appIds.Count(); i++)
      {
        gsaInfNodes[i].ApplicationId = appIds[i];
      }
      return gsaInfNodes.GetRange(0, num);
    }

    private List<GsaAlign> GsaAlignExamples(int num, params string[] appIds)
    {
      var gsaAligns = new List<GsaAlign>()
      {
        new GsaAlign()
        {
          Index = 1,
          Name = "1",
          GridSurfaceIndex = 1,
          NumAlignmentPoints = 2,
          Chain = new List<double>() { 0, 60 },
          Curv = new List<double>() { 0, 0 }
        },
        new GsaAlign()
        {
          Index = 2,
          Name = "2",
          GridSurfaceIndex = 1,
          NumAlignmentPoints = 3,
          Chain = new List<double>() { 0, 60, 100 },
          Curv = new List<double>() { 0, 0, 0 }
        },
      };
      for (int i = 0; i < appIds.Count(); i++)
      {
        gsaAligns[i].ApplicationId = appIds[i];
      }
      return gsaAligns.GetRange(0, num);
    }

    private List<GsaPath> GsaPathExamples(int num, params string[] appIds)
    {
      var gsaPaths = new List<GsaPath>()
      {
        new GsaPath()
        {
          Index = 1,
          Name = "1",
          Type = Speckle.GSA.API.GwaSchema.PathType.CWAY_1WAY,
          Group = 1,
          Alignment = 1,
          Left = -6.3,
          Right = 6.3,
          Factor = 0.5,
          //NumMarkedLanes = null
        },
        new GsaPath()
        {
          Index = 2,
          Name = "2",
          Type = Speckle.GSA.API.GwaSchema.PathType.TRACK,
          Group = 1,
          Alignment = 1,
          Left = null, //centreline (no offset)
          Right = 1.435, //gauge
          Factor = 0.5,
          //NumMarkedLanes = null
        },
      };
      for (int i = 0; i < appIds.Count(); i++)
      {
        gsaPaths[i].ApplicationId = appIds[i];
      }
      return gsaPaths.GetRange(0, num);
    }

    private List<GsaUserVehicle> GsaUserVehicleExamples(int num, params string[] appIds)
    {
      var gsaUserVehicles = new List<GsaUserVehicle>()
      {
        new GsaUserVehicle()
        {
          Index = 1,
          Name = "1",
          Width = 4,
          NumAxle = 2,
          AxlePosition = new List<double>(){ 0, 5 },
          AxleOffset = new List<double>(){ 1, 6 },
          AxleLeft = new List<double>(){ 2, 7 },
          AxleRight = new List<double>(){ 3, 8 },
        },
        new GsaUserVehicle()
        {
          Index = 2,
          Name = "2",
          Width = 4,
          NumAxle = 3,
          AxlePosition = new List<double>(){ 0, 5, 9 },
          AxleOffset = new List<double>(){ 1, 6, 10 },
          AxleLeft = new List<double>(){ 2, 7, 11 },
          AxleRight = new List<double>(){ 3, 8, 12 },
        },
      };
      for (int i = 0; i < appIds.Count(); i++)
      {
        gsaUserVehicles[i].ApplicationId = appIds[i];
      }
      return gsaUserVehicles.GetRange(0, num);
    }
    #endregion
    #endregion
  }
}
