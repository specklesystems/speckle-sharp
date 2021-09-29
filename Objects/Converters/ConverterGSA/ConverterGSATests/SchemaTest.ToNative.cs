using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using Objects.Structural.GSA.Geometry;
using Objects.Structural.GSA.Materials;
using Objects.Structural.GSA.Properties;
using Objects.Structural.Geometry;
using Objects.Structural;
using Objects.Geometry;
using System.Text.RegularExpressions;
using AutoMapper;
using Objects.Structural.Properties.Profiles;
using Speckle.GSA.API.GwaSchema;
using Restraint = Objects.Structural.Geometry.Restraint;
using MemberType = Objects.Structural.Geometry.MemberType;
using Speckle.Core.Models;

namespace ConverterGSATests
{
  public partial class SchemaTest : SpeckleConversionFixture
  {
    //Reminder: conversions could create 1:1, 1:n, n:1, n:n structural per native objects

    #region Geometry
    [Fact(Skip = "Not implemented yet")]
    public void AxisToNative()
    {
      //TO DO: 
    }

    [Fact]
    public void NodeToNative()
    {
      //TO DO: 
      var speckleNodes = SpeckleNodeExamples(2, "node 1", "node 2");
      var gsaRecords = converter.ConvertToNative(speckleNodes.Select(n => (Base)n).ToList());

      Assert.NotEmpty(gsaRecords);
      Assert.Contains(gsaRecords, so => so is GsaNode);

      var gsaNodes = gsaRecords.FindAll(r => r is GsaNode).Select(r => (GsaNode)r).ToList();
      Assert.Equal("node 1", gsaNodes[0].ApplicationId);
      Assert.Equal("node 2", gsaNodes[1].ApplicationId);
      //TO DO: complete checks
    }

    [Fact(Skip = "Not implemented yet")]
    public void Element1dToNative()
    {
      //TO DO: 
      var speckleElement1d = SpeckleElement1dExamples(2, "element 1", "element 2");
    }

    [Fact(Skip = "Not implemented yet")]
    public void Element2dToNative()
    {
      //TO DO: 
      var speckleElement2d = SpeckleElement2dExamples(2, "element 1", "element 2");
    }
    #endregion

    #region Loading
    #endregion

    #region Materials
    [Fact (Skip = "Not implemented yet")]
    public void SteelToNative()
    {
      //TO DO:
    }
    #endregion

    #region Properties
    [Fact (Skip = "Not implemented yet")]
    public void Property1dToNative()
    {
      //TO DO: 
    }
    #endregion

    #region Results
    #endregion

    #region Constraints
    #endregion

    #region Analysis Stages
    #endregion

    #region Bridges
    #endregion

    #region Helper
    #region Geometry
    private List<GSANode> SpeckleNodeExamples(int num, params string[] appIds)
    {
      var speckleNodes = new List<GSANode>()
      {
        new GSANode()
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
        },
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
          type = MaterialType.Steel,
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
          type = MaterialType.Steel,
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
    #endregion

    #region Other
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
    #endregion
    #endregion
  }
}
