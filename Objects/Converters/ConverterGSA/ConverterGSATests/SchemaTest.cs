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
using Objects.Structural.GSA.Geometry;
using Objects.Structural.Properties;
using Objects.Structural.Materials;

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
        },
        { GwaKeyword.PROP_SPR, new Dictionary<int, GsaRecord_>
          { { 1, new GsaPropSpr() { Index = 1, Stiffnesses = new Dictionary<AxisDirection6, double>{ { AxisDirection6.XX, 10 } } } } }
        } 
      };

      gsaModelMock.IndicesByKeyword = new Dictionary<GwaKeyword, List<int>>
      {
        { GwaKeyword.PROP_SPR, new List<int> { 1 } },
        { GwaKeyword.PROP_MASS, new List<int> { 1 } }
      };

      gsaModelMock.ApplicationIdsByKeywordId = new Dictionary<GwaKeyword, Dictionary<int, string>>
      {
        { GwaKeyword.NODE, new Dictionary<int, string>
          { { 1, "blah" } }
        }
      };

      var gsaNode = new GsaNode() {
        ApplicationId = "blah",
        Index = 1,
        Name = "1",
        Colour = Colour.NO_RGB,
        X = 10,
        Y = 11,
        Z = 12,
        NodeRestraint = NodeRestraint.Pin,
        AxisRefType = NodeAxisRefType.Global,
        AxisIndex = null,
        MeshSize = 0.1,
        SpringPropertyIndex = 1,
        MassPropertyIndex = 1,
        };

      var structuralObjects = converter.ConvertToSpeckle(new List<object> { gsaNode });

      Assert.Empty(converter.ConversionErrors);
      Assert.NotEmpty(structuralObjects);
      Assert.Contains(structuralObjects, so => so is GSANode);

      var node = (GSANode)structuralObjects.FirstOrDefault(so => so is GSANode);

      Assert.Equal("blah", node.applicationId);
      Assert.Equal("1", node.name);
      Assert.Equal("NO_RGB", node.colour);
      Assert.Equal(10, node.basePoint.x);
      Assert.Equal(11, node.basePoint.y);
      Assert.Equal(12, node.basePoint.z);

      //Assert.Equal(new Restraint(RestraintType.Pinned), node.restraint);
      var restraint = new Restraint(RestraintType.Pinned);
      Assert.Equal(restraint.code, node.restraint.code);
      Assert.Equal(0, node.restraint.stiffnessX);
      Assert.Equal(0, node.restraint.stiffnessY);
      Assert.Equal(0, node.restraint.stiffnessZ);
      Assert.Equal(0, node.restraint.stiffnessXX);
      Assert.Equal(0, node.restraint.stiffnessYY);
      Assert.Equal(0, node.restraint.stiffnessZZ);

      //Assert.Equal(new Plane(new Point(0,0,0), new Vector(0,0,1), new Vector(1,0,0), new Vector(0,1,0)), node.constraintAxis);
      Assert.Equal(0, node.constraintAxis.origin.x);
      Assert.Equal(0, node.constraintAxis.origin.y);
      Assert.Equal(0, node.constraintAxis.origin.z);
      Assert.Equal(0, node.constraintAxis.normal.x);
      Assert.Equal(0, node.constraintAxis.normal.y);
      Assert.Equal(1, node.constraintAxis.normal.z);
      Assert.Equal(1, node.constraintAxis.xdir.x);
      Assert.Equal(0, node.constraintAxis.xdir.y);
      Assert.Equal(0, node.constraintAxis.xdir.z);
      Assert.Equal(0, node.constraintAxis.ydir.x);
      Assert.Equal(1, node.constraintAxis.ydir.y);
      Assert.Equal(0, node.constraintAxis.ydir.z);

      Assert.Equal(0.1, node.localElementSize);
      Assert.Equal("1", node.springPropertyRef);
      Assert.Equal("1", node.massPropertyRef);
    }

    [Fact]
    public void GsaPropMass()
    {
      var gsaPropMass = new GsaPropMass()
      {
        ApplicationId = "blah",
        Index = 1,
        Name = "1",
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

      var structuralObjects = converter.ConvertToSpeckle(new List<object> { gsaPropMass });

      Assert.Empty(converter.ConversionErrors);
      Assert.NotEmpty(structuralObjects);
      Assert.Contains(structuralObjects, so => so is PropertyMass);

      var propertyMass = (PropertyMass)structuralObjects.FirstOrDefault(so => so is PropertyMass);

      Assert.Equal("blah", propertyMass.applicationId);
      Assert.Equal(gsaPropMass.Name, propertyMass.name);
      Assert.Equal(gsaPropMass.Mass, propertyMass.mass);
      Assert.Equal(gsaPropMass.Ixx, propertyMass.inertiaXX);
      Assert.Equal(gsaPropMass.Iyy, propertyMass.inertiaYY);
      Assert.Equal(gsaPropMass.Izz, propertyMass.inertiaZZ);
      Assert.Equal(gsaPropMass.Ixy, propertyMass.inertiaXY);
      Assert.Equal(gsaPropMass.Iyz, propertyMass.inertiaYZ);
      Assert.Equal(gsaPropMass.Izx, propertyMass.inertiaZX);
      Assert.True(propertyMass.massModified);
      Assert.Equal(gsaPropMass.ModXPercentage, propertyMass.massModifierX);
      Assert.Equal(gsaPropMass.ModYPercentage, propertyMass.massModifierY);
      Assert.Equal(gsaPropMass.ModZPercentage, propertyMass.massModifierZ);
    }

    [Fact]
    public void GsaPropSpr()
    {
      var gsaProSpr = new GsaPropSpr()
      {
        ApplicationId = "blah",
        Index = 1,
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

      var structuralObjects = converter.ConvertToSpeckle(new List<object> { gsaProSpr });

      Assert.Empty(converter.ConversionErrors);
      Assert.NotEmpty(structuralObjects);
      Assert.Contains(structuralObjects, so => so is PropertySpring);

      var propertySpring = (PropertySpring)structuralObjects.FirstOrDefault(so => so is PropertySpring);

      Assert.Equal(gsaProSpr.ApplicationId, propertySpring.applicationId);
      Assert.Equal(gsaProSpr.Name, propertySpring.name);
      Assert.Equal(gsaProSpr.PropertyType.ToString(), propertySpring.springType.ToString());
      Assert.Equal(gsaProSpr.Stiffnesses[AxisDirection6.X], propertySpring.stiffnessX);
      Assert.Equal(gsaProSpr.Stiffnesses[AxisDirection6.Y], propertySpring.stiffnessY);
      Assert.Equal(gsaProSpr.Stiffnesses[AxisDirection6.Z], propertySpring.stiffnessZ);
      Assert.Equal(gsaProSpr.Stiffnesses[AxisDirection6.XX], propertySpring.stiffnessXX);
      Assert.Equal(gsaProSpr.Stiffnesses[AxisDirection6.YY], propertySpring.stiffnessYY);
      Assert.Equal(gsaProSpr.Stiffnesses[AxisDirection6.ZZ], propertySpring.stiffnessZZ);
      Assert.Equal(gsaProSpr.DampingRatio, propertySpring.dampingRatio);
    }

    [Fact]
    public void GsaMatSteel()
    {
      //Define GSA objects
      var gsaUls = new GsaMatCurveParam()
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
      };
      var gsaSls = new GsaMatCurveParam()
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
      };
      var gsaMatAnal = new GsaMatAnal()
      {
        Type = MatAnalType.MAT_ELAS_ISO,
        NumParams = 6,
        E = 2e11,
        Nu = 0.3,
        Rho = 7850,
        Alpha = 1.2e-5,
        G = 7.692307692e+10,
        Damp = 0
      };
      var gsaMat = new GsaMat()
      {
        Name = "",
        E = 2e11,
        F = 360000000,
        Nu = 0.3,
        G = 7.692307692e+10,
        Rho = 7850,
        Alpha = 1.2e-5,
        Prop = gsaMatAnal,
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
        Uls = gsaUls,
        Sls = gsaSls,
        Cost = 0,
        Type = MatType.STEEL
      };
      var gsaMatSteel = new GsaMatSteel()
      {
        ApplicationId = "blah",
        Index = 1,
        Name = "1",
        Mat = gsaMat,
        Fy = 360000000,
        Fu = 450000000,
        EpsP = 0,
        Eh = 0,
      };

      var structuralObjects = converter.ConvertToSpeckle(new List<object> { gsaMatSteel });

      Assert.Empty(converter.ConversionErrors);
      Assert.NotEmpty(structuralObjects);
      Assert.Contains(structuralObjects, so => so is Steel);

      var speckleSteel = (Steel)structuralObjects.FirstOrDefault(so => so is Steel);

      Assert.Equal(gsaMatSteel.ApplicationId, speckleSteel.applicationId);
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

    [Fact]
    public void GsaMatConcrete()
    {
      //Define GSA objects
      var gsaUls = new GsaMatCurveParam()
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
      };
      var gsaSls = new GsaMatCurveParam()
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
      };
      var gsaMatAnal = new GsaMatAnal()
      {
        Type = MatAnalType.MAT_ELAS_ISO,
        NumParams = 6,
        E = 3.315274903e+10,
        Nu = 0.2,
        Rho = 2400,
        Alpha = 1e-5,
        G = 1.381364543e+10,
        Damp = 0
      };
      var gsaMat = new GsaMat()
      {
        Name = "",
        E = 3.315274903e+10,
        F = 40000000,
        Nu = 0.2,
        G = 1.381364543e+10,
        Rho = 2400,
        Alpha = 1e-5,
        Prop = gsaMatAnal,
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
        Uls = gsaUls,
        Sls = gsaSls,
        Cost = 0,
        Type = MatType.CONCRETE
      };
      var gsaMatConcrete = new GsaMatConcrete()
      {
        ApplicationId = "blah",
        Index = 1,
        Name = "1",
        Mat = gsaMat,
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

      var structuralObjects = converter.ConvertToSpeckle(new List<object> { gsaMatConcrete });

      Assert.Empty(converter.ConversionErrors);
      Assert.NotEmpty(structuralObjects);
      Assert.Contains(structuralObjects, so => so is Concrete);

      var speckleConcrete = (Concrete)structuralObjects.FirstOrDefault(so => so is Concrete);

      Assert.Equal(gsaMatConcrete.ApplicationId, speckleConcrete.applicationId);
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
