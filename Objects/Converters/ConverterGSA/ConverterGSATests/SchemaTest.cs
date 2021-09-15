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
using static Objects.Structural.Properties.Profiles.SectionProfile;
using Objects.Structural.GSA.Geometry;
using Objects.Structural.GSA.Loading;
using Objects.Structural.GSA.Properties;
using Speckle.ConnectorGSA.Proxy.GwaParsers;
using GwaMemberType = Speckle.GSA.API.GwaSchema.MemberType;
using MemberType = Objects.Structural.Geometry.MemberType;
using Xunit.Sdk;
using Speckle.Core.Kits;
using ConverterGSA;
using Speckle.ConnectorGSA.Proxy.Merger;
using Speckle.GSA.API.CsvSchema;
using Objects.Structural.Results;

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
      Assert.Equal("assembly 2", speckleAssemblies[1].applicationId);
      Assert.Equal(gsaAssemblies[1].Index, speckleAssemblies[1].nativeId);
      Assert.Equal("node 2", speckleAssemblies[1].end1Node.applicationId);
      Assert.Equal("node 3", speckleAssemblies[1].end2Node.applicationId);
      Assert.Equal("node 4", speckleAssemblies[1].orientationNode.applicationId);
      Assert.Single(speckleAssemblies[1].entities);
      Assert.Equal("element 1", speckleAssemblies[1].entities[0].applicationId);
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
      //parent
      Assert.Equal("node 1", speckleMember1d.end1Node.applicationId);
      Assert.Equal("node 2", speckleMember1d.end2Node.applicationId);
      Assert.Equal(2, speckleMember1d.topology.Count());
      Assert.Equal("node 1", speckleMember1d.topology[0].applicationId);
      Assert.Equal("node 2", speckleMember1d.topology[1].applicationId);
      Assert.Equal("", speckleMember1d.units);
      Assert.Equal(gsaMembers[0].Colour.ToString(), speckleMember1d.colour);
      Assert.Equal(gsaMembers[0].Dummy, speckleMember1d.isDummy);
      Assert.Equal("", speckleMember1d.units);
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
      //baseMesh
      Assert.Equal("prop 2D 1", speckleMember2d.property.applicationId);
      Assert.Equal(ElementType2D.Quad4, speckleMember2d.type);
      Assert.Equal(gsaMembers[1].Offset2dZ, speckleMember2d.offset);
      Assert.Equal(0, speckleMember2d.orientationAngle);
      //parent
      Assert.Equal(4, speckleMember2d.topology.Count());
      Assert.Equal("node 1", speckleMember2d.topology[0].applicationId);
      Assert.Equal("node 2", speckleMember2d.topology[1].applicationId);
      Assert.Equal("node 3", speckleMember2d.topology[2].applicationId);
      Assert.Equal("node 4", speckleMember2d.topology[3].applicationId);
      //displayMesh
      Assert.Equal("", speckleMember2d.units);
      Assert.Equal(gsaMembers[1].Index.Value, speckleMember2d.nativeId);
      Assert.Equal(gsaMembers[1].Group.Value, speckleMember2d.group);
      Assert.Equal(gsaMembers[1].Colour.ToString(), speckleMember2d.colour);
      Assert.Equal(gsaMembers[1].Dummy, speckleMember2d.isDummy);
      Assert.Equal(gsaMembers[1].IsIntersector, speckleMember2d.intersectsWithOthers);
      Assert.Equal(gsaMembers[1].MeshSize.Value, speckleMember2d.targetMeshSize);
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
      Assert.Equal("steel material 1", speckleProperty1D[0].material.applicationId); //assume tests are done elsewhere
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
      Assert.Equal(gsaSection[0].Mass.Value, speckleProperty1D[0].additionalMass); 
      Assert.Equal(gsaSection[0].Cost.Value, speckleProperty1D[0].cost);
      Assert.Null(speckleProperty1D[0].poolRef);
      #endregion
      #region Explicit
      Assert.Equal("section 2", speckleProperty1D[1].applicationId);
      Assert.Equal(gsaSection[1].Colour.ToString(), speckleProperty1D[1].colour);
      Assert.Equal(MemberType.Generic1D, speckleProperty1D[1].memberType);
      Assert.Equal("steel material 1", speckleProperty1D[1].material.applicationId); //assume tests are done elsewhere
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
      Assert.Equal(gsaSection[1].Mass.Value, speckleProperty1D[1].additionalMass);
      Assert.Equal(gsaSection[1].Cost.Value, speckleProperty1D[1].cost);
      Assert.Null(speckleProperty1D[1].poolRef);
      #endregion
      #region Perimeter
      Assert.Equal("section 3", speckleProperty1D[2].applicationId);
      Assert.Equal(gsaSection[2].Colour.ToString(), speckleProperty1D[2].colour);
      Assert.Equal(MemberType.Generic1D, speckleProperty1D[2].memberType);
      Assert.Equal("steel material 1", speckleProperty1D[2].material.applicationId); //assume tests are done elsewhere
      Assert.Equal(ShapeType.Perimeter, speckleProperty1D[2].profile.shapeType);
      var gsaProfilePerimeter = (ProfileDetailsPerimeter)((SectionComp)gsaSection[2].Components[0]).ProfileDetails;
      // TO DO: test ((Perimeter)speckleProperty1D[2].profile).outline
      //             ((Perimeter)speckleProperty1D[2].profile).voids
      Assert.Equal(BaseReferencePoint.Centroid, speckleProperty1D[2].referencePoint);
      Assert.Equal(0, speckleProperty1D[2].offsetY);
      Assert.Equal(0, speckleProperty1D[2].offsetZ);
      Assert.Equal(gsaSection[2].Index.Value, speckleProperty1D[2].nativeId);
      Assert.Equal(gsaSection[2].Mass.Value, speckleProperty1D[2].additionalMass);
      Assert.Equal(gsaSection[2].Cost.Value, speckleProperty1D[2].cost);
      Assert.Null(speckleProperty1D[2].poolRef);
      #endregion
      #region Standard
      #region Rectangular
      Assert.Equal("section 4", speckleProperty1D[3].applicationId);
      Assert.Equal(gsaSection[3].Colour.ToString(), speckleProperty1D[3].colour);
      Assert.Equal(MemberType.Generic1D, speckleProperty1D[3].memberType);
      Assert.Equal("steel material 1", speckleProperty1D[3].material.applicationId); //assume tests are done elsewhere
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
      Assert.Equal(gsaSection[3].Mass.Value, speckleProperty1D[3].additionalMass);
      Assert.Equal(gsaSection[3].Cost.Value, speckleProperty1D[3].cost);
      Assert.Null(speckleProperty1D[3].poolRef);
      #endregion
      #region Rectangular Hollow
      Assert.Equal("section 5", speckleProperty1D[4].applicationId);
      Assert.Equal(gsaSection[4].Colour.ToString(), speckleProperty1D[4].colour);
      Assert.Equal(MemberType.Generic1D, speckleProperty1D[4].memberType);
      Assert.Equal("steel material 1", speckleProperty1D[4].material.applicationId); //assume tests are done elsewhere
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
      Assert.Equal(gsaSection[4].Mass.Value, speckleProperty1D[4].additionalMass);
      Assert.Equal(gsaSection[4].Cost.Value, speckleProperty1D[4].cost);
      Assert.Null(speckleProperty1D[4].poolRef);
      #endregion
      #region Circular
      Assert.Equal("section 6", speckleProperty1D[5].applicationId);
      Assert.Equal(gsaSection[5].Colour.ToString(), speckleProperty1D[5].colour);
      Assert.Equal(MemberType.Generic1D, speckleProperty1D[5].memberType);
      Assert.Equal("steel material 1", speckleProperty1D[5].material.applicationId); //assume tests are done elsewhere
      Assert.Equal(ShapeType.Circular, speckleProperty1D[5].profile.shapeType);
      var gsaProfileCircular = (ProfileDetailsCircular)((SectionComp)gsaSection[5].Components[0]).ProfileDetails;
      Assert.Equal(gsaProfileCircular.d.Value / 2, ((Circular)speckleProperty1D[5].profile).radius);
      Assert.Equal(0, ((Circular)speckleProperty1D[5].profile).wallThickness);
      Assert.Equal(BaseReferencePoint.Centroid, speckleProperty1D[5].referencePoint);
      Assert.Equal(0, speckleProperty1D[5].offsetY);
      Assert.Equal(0, speckleProperty1D[5].offsetZ);
      Assert.Equal(gsaSection[5].Index.Value, speckleProperty1D[5].nativeId);
      Assert.Equal(gsaSection[5].Mass.Value, speckleProperty1D[5].additionalMass);
      Assert.Equal(gsaSection[5].Cost.Value, speckleProperty1D[5].cost);
      Assert.Null(speckleProperty1D[3].poolRef);
      #endregion
      #region Circular Hollow
      Assert.Equal("section 7", speckleProperty1D[6].applicationId);
      Assert.Equal(gsaSection[6].Colour.ToString(), speckleProperty1D[6].colour);
      Assert.Equal(MemberType.Generic1D, speckleProperty1D[6].memberType);
      Assert.Equal("steel material 1", speckleProperty1D[6].material.applicationId); //assume tests are done elsewhere
      Assert.Equal(ShapeType.Circular, speckleProperty1D[6].profile.shapeType);
      var gsaProfileCHS = (ProfileDetailsCircularHollow)((SectionComp)gsaSection[6].Components[0]).ProfileDetails;
      Assert.Equal(gsaProfileCHS.d.Value / 2, ((Circular)speckleProperty1D[6].profile).radius);
      Assert.Equal(gsaProfileCHS.t.Value, ((Circular)speckleProperty1D[6].profile).wallThickness);
      Assert.Equal(BaseReferencePoint.Centroid, speckleProperty1D[6].referencePoint);
      Assert.Equal(0, speckleProperty1D[6].offsetY);
      Assert.Equal(0, speckleProperty1D[6].offsetZ);
      Assert.Equal(gsaSection[6].Index.Value, speckleProperty1D[6].nativeId);
      Assert.Equal(gsaSection[6].Mass.Value, speckleProperty1D[6].additionalMass);
      Assert.Equal(gsaSection[6].Cost.Value, speckleProperty1D[6].cost);
      Assert.Null(speckleProperty1D[6].poolRef);
      #endregion
      #region I Section
      Assert.Equal("section 8", speckleProperty1D[7].applicationId);
      Assert.Equal(gsaSection[7].Colour.ToString(), speckleProperty1D[7].colour);
      Assert.Equal(MemberType.Generic1D, speckleProperty1D[7].memberType);
      Assert.Equal("steel material 1", speckleProperty1D[7].material.applicationId); //assume tests are done elsewhere
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
      Assert.Equal(gsaSection[7].Mass.Value, speckleProperty1D[7].additionalMass);
      Assert.Equal(gsaSection[7].Cost.Value, speckleProperty1D[7].cost);
      Assert.Null(speckleProperty1D[7].poolRef);
      #endregion
      #region T Section
      Assert.Equal("section 9", speckleProperty1D[8].applicationId);
      Assert.Equal(gsaSection[8].Colour.ToString(), speckleProperty1D[8].colour);
      Assert.Equal(MemberType.Generic1D, speckleProperty1D[8].memberType);
      Assert.Equal("steel material 1", speckleProperty1D[8].material.applicationId); //assume tests are done elsewhere
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
      Assert.Equal(gsaSection[8].Mass.Value, speckleProperty1D[8].additionalMass);
      Assert.Equal(gsaSection[8].Cost.Value, speckleProperty1D[8].cost);
      Assert.Null(speckleProperty1D[8].poolRef);
      #endregion
      #region Angle
      Assert.Equal("section 10", speckleProperty1D[9].applicationId);
      Assert.Equal(gsaSection[9].Colour.ToString(), speckleProperty1D[9].colour);
      Assert.Equal(MemberType.Generic1D, speckleProperty1D[9].memberType);
      Assert.Equal("steel material 1", speckleProperty1D[9].material.applicationId); //assume tests are done elsewhere
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
      Assert.Equal(gsaSection[9].Mass.Value, speckleProperty1D[9].additionalMass);
      Assert.Equal(gsaSection[9].Cost.Value, speckleProperty1D[9].cost);
      Assert.Null(speckleProperty1D[9].poolRef);
      #endregion
      #region Channel
      Assert.Equal("section 11", speckleProperty1D[10].applicationId);
      Assert.Equal(gsaSection[10].Colour.ToString(), speckleProperty1D[10].colour);
      Assert.Equal(MemberType.Generic1D, speckleProperty1D[10].memberType);
      Assert.Equal("steel material 1", speckleProperty1D[10].material.applicationId); //assume tests are done elsewhere
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
      Assert.Equal(gsaSection[10].Mass.Value, speckleProperty1D[10].additionalMass);
      Assert.Equal(gsaSection[10].Cost.Value, speckleProperty1D[10].cost);
      Assert.Null(speckleProperty1D[3].poolRef);
      #endregion
      #endregion
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

    #region Results
    [Fact]
    public void GsaNodeResults()
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

      foreach (var record in gsaRecords)
      {
        var speckleObjects = converter.ConvertToSpeckle(new List<object> { record });
        Assert.Empty(converter.ConversionErrors);

        Instance.GsaModel.Cache.SetSpeckleObjects(record, speckleObjects.ToDictionary(so => so.applicationId, so => (object)so));
      }

      Assert.True(Instance.GsaModel.Cache.GetSpeckleObjects(out var structuralObjects));

      Assert.NotEmpty(structuralObjects);
      Assert.Contains(structuralObjects, so => so is ResultNode);
      var speckleNodeResult = structuralObjects.FindAll(so => so is ResultNode).Select(so => (ResultNode)so).ToList();
      Assert.Equal(2, speckleNodeResult.Count());

      //Checks - Results for node 1 - Load case A1
      Assert.Equal("node 1_load case 1", speckleNodeResult[0].applicationId);
      Assert.Equal("node 1", speckleNodeResult[0].node.applicationId); //assume the conversion of the node is tested elsewhere
      Assert.Equal("", speckleNodeResult[0].description);
      Assert.Equal("", speckleNodeResult[0].permutation);
      Assert.Equal("load case 1", speckleNodeResult[0].resultCase.applicationId);
      Assert.Equal(((CsvNode)nodeResults[0]).Ux.Value, speckleNodeResult[0].dispX);
      Assert.Equal(((CsvNode)nodeResults[0]).Uy.Value, speckleNodeResult[0].dispY);
      Assert.Equal(((CsvNode)nodeResults[0]).Uz.Value, speckleNodeResult[0].dispZ);
      Assert.Equal(((CsvNode)nodeResults[0]).Rxx.Value, speckleNodeResult[0].rotXX);
      Assert.Equal(((CsvNode)nodeResults[0]).Ryy.Value, speckleNodeResult[0].rotYY);
      Assert.Equal(((CsvNode)nodeResults[0]).Rzz.Value, speckleNodeResult[0].rotZZ);
      Assert.Equal(((CsvNode)nodeResults[0]).Vx.Value, speckleNodeResult[0].velX);
      Assert.Equal(((CsvNode)nodeResults[0]).Vy.Value, speckleNodeResult[0].velY);
      Assert.Equal(((CsvNode)nodeResults[0]).Vz.Value, speckleNodeResult[0].velZ);
      Assert.Equal(((CsvNode)nodeResults[0]).Vxx.Value, speckleNodeResult[0].velXX);
      Assert.Equal(((CsvNode)nodeResults[0]).Vyy.Value, speckleNodeResult[0].velYY);
      Assert.Equal(((CsvNode)nodeResults[0]).Vzz.Value, speckleNodeResult[0].velZZ);
      Assert.Equal(((CsvNode)nodeResults[0]).Ax.Value, speckleNodeResult[0].accX);
      Assert.Equal(((CsvNode)nodeResults[0]).Ay.Value, speckleNodeResult[0].accY);
      Assert.Equal(((CsvNode)nodeResults[0]).Az.Value, speckleNodeResult[0].accZ);
      Assert.Equal(((CsvNode)nodeResults[0]).Axx.Value, speckleNodeResult[0].accXX);
      Assert.Equal(((CsvNode)nodeResults[0]).Ayy.Value, speckleNodeResult[0].accYY);
      Assert.Equal(((CsvNode)nodeResults[0]).Azz.Value, speckleNodeResult[0].accZZ);
      Assert.Equal(((CsvNode)nodeResults[0]).Fx_Reac.Value, speckleNodeResult[0].reactionX);
      Assert.Equal(((CsvNode)nodeResults[0]).Fy_Reac.Value, speckleNodeResult[0].reactionY);
      Assert.Equal(((CsvNode)nodeResults[0]).Fz_Reac.Value, speckleNodeResult[0].reactionZ);
      Assert.Equal(((CsvNode)nodeResults[0]).Mxx_Reac.Value, speckleNodeResult[0].reactionXX);
      Assert.Equal(((CsvNode)nodeResults[0]).Myy_Reac.Value, speckleNodeResult[0].reactionYY);
      Assert.Equal(((CsvNode)nodeResults[0]).Mzz_Reac.Value, speckleNodeResult[0].reactionZZ);
      Assert.Equal(((CsvNode)nodeResults[0]).Fx_Cons.Value, speckleNodeResult[0].constraintX);
      Assert.Equal(((CsvNode)nodeResults[0]).Fy_Cons.Value, speckleNodeResult[0].constraintY);
      Assert.Equal(((CsvNode)nodeResults[0]).Fz_Cons.Value, speckleNodeResult[0].constraintZ);
      Assert.Equal(((CsvNode)nodeResults[0]).Mxx_Cons.Value, speckleNodeResult[0].constraintXX);
      Assert.Equal(((CsvNode)nodeResults[0]).Myy_Cons.Value, speckleNodeResult[0].constraintYY);
      Assert.Equal(((CsvNode)nodeResults[0]).Mzz_Cons.Value, speckleNodeResult[0].constraintZZ);

      //Checks - Results for node 1 - Load case A2
      Assert.Equal("node 1_load case 2", speckleNodeResult[1].applicationId);
      Assert.Equal("node 1", speckleNodeResult[0].node.applicationId); //assume the conversion of the node is tested elsewhere
      Assert.Equal("", speckleNodeResult[1].description);
      Assert.Equal("", speckleNodeResult[1].permutation);
      Assert.Equal("load case 2", speckleNodeResult[1].resultCase.applicationId);
      Assert.Equal(((CsvNode)nodeResults[1]).Ux.Value, speckleNodeResult[1].dispX);
      Assert.Equal(((CsvNode)nodeResults[1]).Uy.Value, speckleNodeResult[1].dispY);
      Assert.Equal(((CsvNode)nodeResults[1]).Uz.Value, speckleNodeResult[1].dispZ);
      Assert.Equal(((CsvNode)nodeResults[1]).Rxx.Value, speckleNodeResult[1].rotXX);
      Assert.Equal(((CsvNode)nodeResults[1]).Ryy.Value, speckleNodeResult[1].rotYY);
      Assert.Equal(((CsvNode)nodeResults[1]).Rzz.Value, speckleNodeResult[1].rotZZ);
      Assert.Equal(((CsvNode)nodeResults[1]).Vx.Value, speckleNodeResult[1].velX);
      Assert.Equal(((CsvNode)nodeResults[1]).Vy.Value, speckleNodeResult[1].velY);
      Assert.Equal(((CsvNode)nodeResults[1]).Vz.Value, speckleNodeResult[1].velZ);
      Assert.Equal(((CsvNode)nodeResults[1]).Vxx.Value, speckleNodeResult[1].velXX);
      Assert.Equal(((CsvNode)nodeResults[1]).Vyy.Value, speckleNodeResult[1].velYY);
      Assert.Equal(((CsvNode)nodeResults[1]).Vzz.Value, speckleNodeResult[1].velZZ);
      Assert.Equal(((CsvNode)nodeResults[1]).Ax.Value, speckleNodeResult[1].accX);
      Assert.Equal(((CsvNode)nodeResults[1]).Ay.Value, speckleNodeResult[1].accY);
      Assert.Equal(((CsvNode)nodeResults[1]).Az.Value, speckleNodeResult[1].accZ);
      Assert.Equal(((CsvNode)nodeResults[1]).Axx.Value, speckleNodeResult[1].accXX);
      Assert.Equal(((CsvNode)nodeResults[1]).Ayy.Value, speckleNodeResult[1].accYY);
      Assert.Equal(((CsvNode)nodeResults[1]).Azz.Value, speckleNodeResult[1].accZZ);
      Assert.Equal(((CsvNode)nodeResults[1]).Fx_Reac.Value, speckleNodeResult[1].reactionX);
      Assert.Equal(((CsvNode)nodeResults[1]).Fy_Reac.Value, speckleNodeResult[1].reactionY);
      Assert.Equal(((CsvNode)nodeResults[1]).Fz_Reac.Value, speckleNodeResult[1].reactionZ);
      Assert.Equal(((CsvNode)nodeResults[1]).Mxx_Reac.Value, speckleNodeResult[1].reactionXX);
      Assert.Equal(((CsvNode)nodeResults[1]).Myy_Reac.Value, speckleNodeResult[1].reactionYY);
      Assert.Equal(((CsvNode)nodeResults[1]).Mzz_Reac.Value, speckleNodeResult[1].reactionZZ);
      Assert.Equal(((CsvNode)nodeResults[1]).Fx_Cons.Value, speckleNodeResult[1].constraintX);
      Assert.Equal(((CsvNode)nodeResults[1]).Fy_Cons.Value, speckleNodeResult[1].constraintY);
      Assert.Equal(((CsvNode)nodeResults[1]).Fz_Cons.Value, speckleNodeResult[1].constraintZ);
      Assert.Equal(((CsvNode)nodeResults[1]).Mxx_Cons.Value, speckleNodeResult[1].constraintXX);
      Assert.Equal(((CsvNode)nodeResults[1]).Myy_Cons.Value, speckleNodeResult[1].constraintYY);
      Assert.Equal(((CsvNode)nodeResults[1]).Mzz_Cons.Value, speckleNodeResult[1].constraintZZ);
    }

    [Fact]
    public void GsaElement1dResults()
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

      foreach (var record in gsaRecords)
      {
        var speckleObjects = converter.ConvertToSpeckle(new List<object> { record });
        Assert.Empty(converter.ConversionErrors);

        Instance.GsaModel.Cache.SetSpeckleObjects(record, speckleObjects.ToDictionary(so => so.applicationId, so => (object)so));
      }

      Assert.True(Instance.GsaModel.Cache.GetSpeckleObjects(out var structuralObjects));

      Assert.NotEmpty(structuralObjects);
      Assert.Contains(structuralObjects, so => so is GSAElement1D);

      var speckleElement1dResult = structuralObjects.FindAll(so => so is Result1D).Select(so => (Result1D)so).ToList();
      Assert.Equal(5, speckleElement1dResult.Count());

      //Checks
      for (var i = 0; i < speckleElement1dResult.Count(); i++)
      {
        var gsaResult = (CsvElem1d)gsaElement1dResults[i];

        //result description
        Assert.Equal("element 1_load case 1_" + gsaResult.PosR, speckleElement1dResult[i].applicationId);
        Assert.Equal("", speckleElement1dResult[i].permutation);
        Assert.Equal("", speckleElement1dResult[i].description);
        Assert.Equal("element 1", speckleElement1dResult[i].element.applicationId);
        Assert.Equal(gsaResult.PosR.ToDouble(), speckleElement1dResult[i].position);
        Assert.Equal("load case 1", speckleElement1dResult[i].resultCase.applicationId);

        //results
        Assert.Equal(gsaResult.Ux.Value, speckleElement1dResult[i].dispX);
        Assert.Equal(gsaResult.Uy.Value, speckleElement1dResult[i].dispY);
        Assert.Equal(gsaResult.Uz.Value, speckleElement1dResult[i].dispZ);
        Assert.Equal(gsaResult.Fx.Value, speckleElement1dResult[i].forceX);
        Assert.Equal(gsaResult.Fy.Value, speckleElement1dResult[i].forceY);
        Assert.Equal(gsaResult.Fz.Value, speckleElement1dResult[i].forceZ);
        Assert.Equal(gsaResult.Mxx.Value, speckleElement1dResult[i].momentXX);
        Assert.Equal(gsaResult.Myy.Value, speckleElement1dResult[i].momentYY);
        Assert.Equal(gsaResult.Mzz.Value, speckleElement1dResult[i].momentZZ);

        //results - Not currently supported
        Assert.Equal(0, speckleElement1dResult[i].axialStress);
        Assert.Equal(0, speckleElement1dResult[i].bendingStressYNeg);
        Assert.Equal(0, speckleElement1dResult[i].bendingStressYPos);
        Assert.Equal(0, speckleElement1dResult[i].bendingStressZNeg);
        Assert.Equal(0, speckleElement1dResult[i].bendingStressZPos);
        Assert.Equal(0, speckleElement1dResult[i].combinedStressMax);
        Assert.Equal(0, speckleElement1dResult[i].combinedStressMin);
        Assert.Equal(0, speckleElement1dResult[i].rotXX);
        Assert.Equal(0, speckleElement1dResult[i].rotYY);
        Assert.Equal(0, speckleElement1dResult[i].rotZZ);
        Assert.Equal(0, speckleElement1dResult[i].shearStressY);
        Assert.Equal(0, speckleElement1dResult[i].shearStressZ);
      }
    }

    [Fact]
    public void GsaElement2dResults()
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

      foreach (var record in gsaRecords)
      {
        var speckleObjects = converter.ConvertToSpeckle(new List<object> { record });
        Assert.Empty(converter.ConversionErrors);

        Instance.GsaModel.Cache.SetSpeckleObjects(record, speckleObjects.ToDictionary(o => o.applicationId, o => (object)o));
      }

      Assert.True(Instance.GsaModel.Cache.GetSpeckleObjects(out var structuralObjects));

      Assert.NotEmpty(structuralObjects);
      Assert.Contains(structuralObjects, so => so is Result2D);

      var speckleElement2dResults = structuralObjects.FindAll(so => so is Result2D).Select(so => (Result2D)so).ToList();
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
    public void GsaNodeEmbeddedResults()
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
          LoadDirection = AxisDirection6.Z,
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
          LoadDirection = AxisDirection6.X,
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
    #endregion
  }
}
