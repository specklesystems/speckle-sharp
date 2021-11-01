using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using Speckle.GSA.API.GwaSchema;
using Speckle.ConnectorGSA.Proxy.GwaParsers;
using Speckle.Core.Kits;
using Speckle.Core.Models;
using Speckle.ConnectorGSA.Proxy;

namespace ConnectorGSATests
{
  public class SchemaTests : SpeckleConnectorFixture
  {
    //Used in multiple tests
    private static readonly GsaAxis gsaAxis1 = new GsaAxis() { Index = 1, ApplicationId = "Axis1", Name = "StandardAxis", XDirX = 1, XDirY = 0, XDirZ = 0, XYDirX = 0, XYDirY = 1, XYDirZ = 0, OriginX = 10, OriginY = 20, OriginZ = 30 };
    private static readonly GsaAxis gsaAxis2 = new GsaAxis() { Index = 2, ApplicationId = "Axis2", Name = "AngledAxis", XDirX = 1, XDirY = 1, XDirZ = 0, XYDirX = -1, XYDirY = 1, XYDirZ = 0 };
    private static readonly string streamId1 = "TestStream1";

    //TEMP
    [Fact]
    public void SpeckleDependencyTree()
    {
      var kit = KitManager.GetDefaultKit();

      var structuralTypes = kit.Types.Where(t => t.Namespace.ToLower().Contains("structural"));
      var tree = new TypeTreeCollection<Type>(structuralTypes);

      var typeChildren = new Dictionary<Type, List<Type>>();
      var baseType = typeof(Base);
      foreach (var t in structuralTypes)
      {
        var baseClasses = t.GetBaseClasses().Where(bc => structuralTypes.Any(st => st == bc) && bc.InheritsOrImplements(baseType) && bc != baseType);
        foreach (var p in baseClasses)
        {
          typeChildren.UpsertDictionary(p, t);
        }
      }

      foreach (var t in structuralTypes)
      {
        if (t == typeof(Objects.Structural.GSA.Geometry.GSANode))
        {

        }
        var referencedStructuralTypes = new List<Type>();
        var propertyInfos = t.GetProperties();
        //.Where(pi => structuralTypes.Any(kt => kt == pi.PropertyType));

        foreach (var pi in propertyInfos)
        {
          Type typeToAdd = null;
          if (pi.IsList(out Type listType))
          {
            if (structuralTypes.Any(st => st == listType))
            {
              typeToAdd = listType;
            }
          }
          else if (structuralTypes.Any(st => st == pi.PropertyType))
          {
            typeToAdd = pi.PropertyType;
          }
          if (typeToAdd != null)
          {
            if (typeChildren.ContainsKey(typeToAdd))
            {
              foreach (var c in typeChildren[typeToAdd])
              {
                if (!referencedStructuralTypes.Contains(c))
                {
                  referencedStructuralTypes.Add(c);
                }
              }
            }
            if (!referencedStructuralTypes.Contains(typeToAdd))
            {
              referencedStructuralTypes.Add(typeToAdd);
            }
          }
        }
        tree.Integrate(t, referencedStructuralTypes.ToArray());
      }
    }

    #region tests
    #region simple
    [Fact]
    public void GsaAlignSimple()
    {
      var alignGwas = new List<string>()
      {
        "ALIGN.1\t1\tedge\t1\t4\t0\t0.02\t25\t0.02\t50\t-0.01\t150\t-0.01"
      };

      var aligns = new List<GsaAlign>();
      foreach (var g in alignGwas)
      {
        var l = new GsaAlignParser();
        Assert.True(l.FromGwa(g));
        aligns.Add((GsaAlign)l.Record);
      }
      Assert.Equal(1, aligns[0].GridSurfaceIndex);
      Assert.Equal(4, aligns[0].NumAlignmentPoints);
      Assert.Equal(new List<double>() { 0, 25, 50, 150 }, aligns[0].Chain);
      Assert.Equal(new List<double>() { 0.02, 0.02, -0.01, -0.01 }, aligns[0].Curv);

      for (int i = 0; i < aligns.Count(); i++)
      {
        Assert.True(new GsaAlignParser(aligns[i]).Gwa(out var gwa));
        Assert.Equal(alignGwas[i], gwa.First());
      }
    }

    [Fact]
    public void GsaAnal()
    {
      var analGwas = new List<string>()
      {
        "ANAL.1\t1\tAnalysis Case 1\t1\tL1"
      };
      var anals = new List<GsaAnal>();
      foreach (var g in analGwas)
      {
        var l = new GsaAnalParser();
        Assert.True(l.FromGwa(g));
        anals.Add((GsaAnal)l.Record);
      }
      Assert.Equal("Analysis Case 1", anals[0].Name);
      Assert.Equal(1, anals[0].TaskIndex);
      Assert.Equal("L1", anals[0].Desc);

      for (int i = 0; i < anals.Count(); i++)
      {
        Assert.True(new GsaAnalParser(anals[i]).Gwa(out var gwa));
        Assert.Equal(analGwas[i],gwa.First());
      }
    }

    [Fact]
    public void GsaAnalStage()
    {
      var analStageGwas = new List<string>()
      {
        "ANAL_STAGE.3\t1\tname\tNO_RGB\tall\t0\t0\tall"
      };
      var analStages = new List<GsaAnalStage>();
      foreach (var g in analStageGwas)
      {
        var l = new GsaAnalStageParser();
        Assert.True(l.FromGwa(g));
        analStages.Add((GsaAnalStage)l.Record);
      }
      Assert.Equal(Colour.NO_RGB, analStages[0].Colour);
      Assert.Equal(new List<int>(), analStages[0].ElementIndices);
      Assert.Equal(0, analStages[0].Phi);
      Assert.Equal(0, analStages[0].Days);
      Assert.Equal(new List<int>(), analStages[0].LockElementIndices);

      for (int i = 0; i < analStages.Count(); i++)
      {
        Assert.True(new GsaAnalStageParser(analStages[i]).Gwa(out var gwa));
        Assert.Equal(analStageGwas[i], gwa.First());
      }
    }

    //This just tests transitions from the GSA schema to GWA commands, and back again, since there is no need at the moment for a ToNativeTest() method for StructuralAxis
    [Fact (Skip = "WIP")]
    public void GsaAxisSimple()
    {
      Assert.True(new GsaAxisParser(gsaAxis1).Gwa(out var axis1gwa));
      Assert.True(new GsaAxisParser(gsaAxis2).Gwa(out var axis2gwa));

      //Assert.True(ModelValidation(new string[] { axis1gwa.First(), axis2gwa.First() }, new Dictionary<string, int> { { GsaRecord.GetKeyword<GsaAxis>(), 2 } }, out var mismatchByKw));
      //Assert.Zero(mismatchByKw.Keys.Count());
      
      Assert.True((new GsaAxisParser()).FromGwa(axis1gwa.First()));
      Assert.True((new GsaAxisParser()).FromGwa(axis2gwa.First()));
    }

    [Fact]
    public void GsaCombination()
    {
      var combinationGwas = new List<string>()
      {
        "COMBINATION.1\t1\tCombination case 1\tA1",
        "COMBINATION.1\t1\tCombination case 2\t1A1 + 1A2 + 1A3\t\tnotes"
      };
      var combinations = new List<GsaCombination>();
      foreach (var g in combinationGwas)
      {
        var l = new GsaCombinationParser();
        Assert.True(l.FromGwa(g));
        combinations.Add((GsaCombination)l.Record);
      }
      Assert.Equal("Combination case 1", combinations[0].Name);
      Assert.Equal("A1", combinations[0].Desc);
      Assert.Null(combinations[0].Bridge);
      Assert.Null(combinations[0].Note);

      Assert.Equal("Combination case 2", combinations[1].Name);
      Assert.Equal("1A1 + 1A2 + 1A3", combinations[1].Desc);
      Assert.False(combinations[1].Bridge);
      Assert.Equal("notes", combinations[1].Note);

      for (int i = 0; i < combinations.Count(); i++)
      {
        Assert.True(new GsaCombinationParser(combinations[i]).Gwa(out var gwa));
        Assert.Equal(combinationGwas[i], gwa.First());
      }
    }

    [Fact (Skip = "WIP")]
    public void GsaElSimple()
    {
      var gsaEls = GenerateMixedGsaEls();
      var gwaToTest = new List<string>();
      foreach (var gsaEl in gsaEls)
      {
        Assert.True(new GsaElParser(gsaEl).Gwa(out var gwa, false));

        var gsaElNew = new GsaElParser();
        Assert.True(gsaElNew.FromGwa(gwa.First()));
        //gsaEl.ShouldDeepEqual((GsaEl)gsaElNew.Record);

        gwaToTest = gwaToTest.Union(gwa).ToList();
      }

      //Assert.True(ModelValidation(gwaToTest, GsaRecord.GetKeyword<GsaEl>(), 2, out var mismatch));
    }

    [Fact]
    public void GsaGenRestSimple()
    {
      var genRestGwas = new List<string>()
      {
        "GEN_REST.2\t\t1\t1\t1\t0\t0\t0\t1 2 3\t1"
      };

      var genRests = new List<GsaGenRest>();
      foreach (var g in genRestGwas)
      {
        var l = new GsaGenRestParser();
        Assert.True(l.FromGwa(g));
        genRests.Add((GsaGenRest)l.Record);
      }

      Assert.Equal(RestraintCondition.Constrained, genRests[0].X);
      Assert.Equal(RestraintCondition.Constrained, genRests[0].Y);
      Assert.Equal(RestraintCondition.Constrained, genRests[0].Z);
      Assert.Equal(RestraintCondition.Free, genRests[0].XX);
      Assert.Equal(RestraintCondition.Free, genRests[0].YY);
      Assert.Equal(RestraintCondition.Free, genRests[0].ZZ);
      genRests[0].NodeIndices = new List<int>() { 1, 2, 3 }; //assume node list is tested elsewhere
      Assert.Equal(new List<int>() { 1 }, genRests[0].StageIndices);

      for (int i = 0; i < genRests.Count(); i++)
      {
        Assert.True(new GsaGenRestParser(genRests[i]).Gwa(out var gwa));
        Assert.Equal(genRestGwas[i], gwa.First());
      }
    }

    [Fact]
    public void GsaGridLineSimple()
    {
      var gridLineGwas = new List<string>()
      {
        "GRID_LINE.1	1	Level	LINE	10	0	50	0	0",
        "GRID_LINE.1	2	Angled	LINE	10	0	50	30	0",
        "GRID_LINE.1	3	Arc	ARC	10	0	50	30	60"
      };

      var gridLines = new List<GsaGridLine>();
      foreach (var g in gridLineGwas)
      {
        var l = new GsaGridLineParser();
        Assert.True(l.FromGwa(g));
        Assert.Equal(10, ((GsaGridLine)l.Record).XCoordinate);
        Assert.Equal(0, ((GsaGridLine)l.Record).YCoordinate);
        Assert.Equal(50, ((GsaGridLine)l.Record).Length); // Length (LINE) or radius (ARC)
        gridLines.Add((GsaGridLine)l.Record);
      }

      Assert.Equal(GridLineType.Line, gridLines[0].Type);
      Assert.Equal(0, gridLines[0].Theta1);
      Assert.Equal(0, gridLines[0].Theta2);

      Assert.Equal(GridLineType.Line, gridLines[1].Type);
      Assert.Equal(30, gridLines[1].Theta1);
      Assert.Equal(0, gridLines[1].Theta2);

      Assert.Equal(GridLineType.Arc, gridLines[2].Type);
      Assert.Equal(30, gridLines[2].Theta1);
      Assert.Equal(60, gridLines[2].Theta2);

      for (int i = 0; i < gridLines.Count(); i++)
      {
        Assert.True(new GsaGridLineParser(gridLines[i]).Gwa(out var gwa));
        Assert.Equal(gridLineGwas[i], gwa.First());
      }
    }

    [Fact]
    public void GsaInfBeamSimple()
    {
      var infBeamGwas = new List<string>()
      {
        "INF_BEAM.2\tAbutment A Headstock - Positive Bending\t1\t5316\t50%\t1\tFORCE\tYY"
      };
      var infBeams = new List<GsaInfBeam>();
      foreach (var g in infBeamGwas)
      {
        var l = new GsaInfBeamParser();
        Assert.True(l.FromGwa(g));
        infBeams.Add((GsaInfBeam)l.Record);
      }
      Assert.Equal(1, infBeams[0].Index);
      Assert.Equal(5316, infBeams[0].Element);
      Assert.Equal(0.5, infBeams[0].Position);
      Assert.Equal(1, infBeams[0].Factor);
      Assert.Equal(InfType.FORCE, infBeams[0].Type);
      Assert.Equal(AxisDirection6.YY, infBeams[0].Direction);

      for (int i = 0; i < infBeams.Count(); i++)
      {
        Assert.True(new GsaInfBeamParser(infBeams[i]).Gwa(out var gwa));
        Assert.Equal(infBeamGwas[i], gwa.First());
      }
    }

    [Fact]
    public void GsaInfNodeSimple()
    {
      var infNodeGwas = new List<string>()
      {
        "INF_NODE.1\tname\t1\t1\t1\tDISP\tGLOBAL\tZ"
      };
      var infNodes = new List<GsaInfNode>();
      foreach (var g in infNodeGwas)
      {
        var l = new GsaInfNodeParser();
        Assert.True(l.FromGwa(g));
        infNodes.Add((GsaInfNode)l.Record);
      }
      Assert.Equal(1, infNodes[0].Index);
      Assert.Equal(1, infNodes[0].Node);
      Assert.Equal(1, infNodes[0].Factor);
      Assert.Equal(InfType.DISP, infNodes[0].Type);
      Assert.Equal(AxisRefType.Global, infNodes[0].AxisRefType);
      Assert.Equal(AxisDirection6.Z, infNodes[0].Direction);

      for (int i = 0; i < infNodes.Count(); i++)
      {
        Assert.True(new GsaInfNodeParser(infNodes[i]).Gwa(out var gwa));
        Assert.Equal(infNodeGwas[i], gwa.First());
      }
    }

    [Fact]
    public void GsaLoad2dFaceSimple()
    {
      var load2dFaceGwas = new List<string>()
      {
        //list of entities set to all for simple test. Lists are tested elsewhere
        "LOAD_2D_FACE.2\t\tall\t30\tGLOBAL\tCONS\tNO\tZ\t-2000",
        "LOAD_2D_FACE.2\t\tall\t2\tLOCAL\tGEN\tYES\tZ\t-2000",
        "LOAD_2D_FACE.2\t\tall\t27\tLOCAL\tPOINT\tNO\tZ\t-10000\t0\t0"
      };

      var load2dFaces = new List<GsaLoad2dFace>();
      foreach (var g in load2dFaceGwas)
      {
        var l = new GsaLoad2dFaceParser();
        Assert.True(l.FromGwa(g));
        load2dFaces.Add((GsaLoad2dFace)l.Record);
      }
      Assert.Equal(30, load2dFaces[0].LoadCaseIndex);
      Assert.Equal(AxisRefType.Global, load2dFaces[0].AxisRefType);
      Assert.Equal(Load2dFaceType.Uniform, load2dFaces[0].Type);
      Assert.False(load2dFaces[0].Projected);
      Assert.Equal(AxisDirection3.Z, load2dFaces[0].LoadDirection);
      Assert.Equal(new List<double> { -2000 }, load2dFaces[0].Values);

      Assert.Equal(2, load2dFaces[1].LoadCaseIndex);
      Assert.Equal(AxisRefType.Local, load2dFaces[1].AxisRefType);
      Assert.Equal(Load2dFaceType.General, load2dFaces[1].Type);
      Assert.True(load2dFaces[1].Projected);
      Assert.Equal(AxisDirection3.Z, load2dFaces[1].LoadDirection);
      Assert.Equal(new List<double> { -2000 }, load2dFaces[1].Values);

      Assert.Equal(27, load2dFaces[2].LoadCaseIndex);
      Assert.Equal(AxisRefType.Local, load2dFaces[2].AxisRefType);
      Assert.Equal(Load2dFaceType.Point, load2dFaces[2].Type);
      Assert.False(load2dFaces[2].Projected);
      Assert.Equal(AxisDirection3.Z, load2dFaces[2].LoadDirection);
      Assert.Equal(new List<double> { -10000 }, load2dFaces[2].Values);
      Assert.Equal(0, load2dFaces[2].R);
      Assert.Equal(0, load2dFaces[2].S);

      for (int i = 0; i < load2dFaces.Count(); i++)
      {
        Assert.True(new GsaLoad2dFaceParser(load2dFaces[i]).Gwa(out var gwa));
        Assert.Equal(load2dFaceGwas[i], gwa.First());
      }
    }

    [Fact]
    public void GsaLoad2dThermal()
    {
      var load2dThermalGwas = new List<string>()
      {
        "LOAD_2D_THERMAL.2\tthermal 1\tall\t1\tCONS\t10",
        "LOAD_2D_THERMAL.2\tthermal 2\tall\t2\tDZ\t10\t0",
        "LOAD_2D_THERMAL.2\tthermal 3\tall\t3\tGEN\t10\t6\t9\t5\t8\t4\t7\t3"
      };
      var load2dThermals = new List<GsaLoad2dThermal>();
      foreach (var g in load2dThermalGwas)
      {
        var l = new GsaLoad2dThermalParser();
        Assert.True(l.FromGwa(g));
        load2dThermals.Add((GsaLoad2dThermal)l.Record);
      }
      Assert.Equal("thermal 1", load2dThermals[0].Name);
      //Assert.Equal(new List<string>() { }, load2dThermals[0].Entities);
      Assert.Equal(1, load2dThermals[0].LoadCaseIndex);
      Assert.Equal(Load2dThermalType.Uniform, load2dThermals[0].Type);
      Assert.Equal(new List<double>() { 10 }, load2dThermals[0].Values);

      Assert.Equal("thermal 2", load2dThermals[1].Name);
      //Assert.Equal(new List<string>() { }, load2dThermals[1].Entities);
      Assert.Equal(2, load2dThermals[1].LoadCaseIndex);
      Assert.Equal(Load2dThermalType.Gradient, load2dThermals[1].Type);
      Assert.Equal(new List<double>() { 10, 0 }, load2dThermals[1].Values);

      Assert.Equal("thermal 3", load2dThermals[2].Name);
      //Assert.Equal(new List<string>() { }, load2dThermals[2].Entities);
      Assert.Equal(3, load2dThermals[2].LoadCaseIndex);
      Assert.Equal(Load2dThermalType.General, load2dThermals[2].Type);
      Assert.Equal(new List<double>() { 10, 6, 9, 5, 8, 4, 7, 3 }, load2dThermals[2].Values);

      for (int i = 0; i < load2dThermals.Count(); i++)
      {
        Assert.True(new GsaLoad2dThermalParser(load2dThermals[i]).Gwa(out var gwa));
        Assert.Equal(load2dThermalGwas[i], gwa.First());
      }
    }

    [Fact]
    public void GsaLoadCase()
    {
      var loadCaseGwas = new List<string>()
      {
        "LOAD_TITLE.2\t1\tLoad case 1\tLC_VAR_IMP\t1\tA\tNONE\tINC_UNDEF\t",
        "LOAD_TITLE.2\t2\tLoad case 2\tLC_VAR_IMP\t1\tB\tNONE\tINC_UNDEF\t",
        "LOAD_TITLE.2\t3\tLoad case 3\tLC_VAR_IMP\t1\tC\tNONE\tINC_UNDEF\t",
        "LOAD_TITLE.2\t4\tLoad case 4\tLC_VAR_IMP\t1\tD\tNONE\tINC_UNDEF\t",
        "LOAD_TITLE.2\t5\tLoad case 5\tLC_VAR_IMP\t1\tE\tNONE\tINC_UNDEF\t",
        "LOAD_TITLE.2\t6\tLoad case 6\tLC_VAR_IMP\t1\tF\tNONE\tINC_UNDEF\t",
        "LOAD_TITLE.2\t7\tLoad case 7\tLC_VAR_IMP\t1\tG\tNONE\tINC_UNDEF\t",
        "LOAD_TITLE.2\t8\tLoad case 8\tLC_VAR_IMP\t1\tH\tNONE\tINC_UNDEF\t",
        "LOAD_TITLE.2\t9\tLoad case 9\tLC_PERM_SELF\t1\t~\tNONE\tINC_BOTH\t",
        "LOAD_TITLE.2\t10\tLoad case 10\tLC_PERM_SOIL\t1\t~\tNONE\tINC_BOTH\t",
        "LOAD_TITLE.2\t11\tLoad case 11\tLC_PERM_EQUIV\t1\t~\tNONE\tINC_BOTH\t",
        "LOAD_TITLE.2\t12\tLoad case 12\tLC_PRESTRESS\t1\t~\tNONE\tINC_BOTH\t",
        "LOAD_TITLE.2\t13\tLoad case 13\tLC_VAR_WIND\t1\t~\tNONE\tINC_UNDEF\t",
        "LOAD_TITLE.2\t14\tLoad case 14\tLC_VAR_SNOW\t1\t~\tNONE\tINC_UNDEF\t",
        "LOAD_TITLE.2\t15\tLoad case 15\tLC_VAR_RAIN\t1\t~\tNONE\tINC_UNDEF\t",
        "LOAD_TITLE.2\t16\tLoad case 16\tLC_VAR_TEMP\t1\t~\tNONE\tINC_UNDEF\t",
        "LOAD_TITLE.2\t17\tLoad case 17\tLC_VAR_EQUIV\t1\t~\tNONE\tINC_UNDEF\t",
        "LOAD_TITLE.2\t18\tLoad case 18\tLC_ACCIDENTAL\t1\t~\tNONE\tINC_UNDEF\t",
        "LOAD_TITLE.2\t19\tLoad case 19\tLC_EQE_RSA\t1\t~\tX\tINC_UNDEF\t",
        "LOAD_TITLE.2\t20\tLoad case 20\tLC_EQE_ACC\t1\t~\tX\tINC_UNDEF\t",
        "LOAD_TITLE.2\t21\tLoad case 21\tLC_EQE_STAT\t1\t~\tX\tINC_UNDEF\t",
      };
      var loadCases = new List<GsaLoadCase>();
      int i = 1;
      foreach (var g in loadCaseGwas)
      {
        var l = new GsaLoadCaseParser();
        Assert.True(l.FromGwa(g));
        var lc = (GsaLoadCase)l.Record;
        loadCases.Add(lc);

        //Checks
        if (i>8) Assert.Equal(LoadCategory.NotSet, lc.Category);
        if (i < 9 || i > 12) Assert.Equal(IncludeOption.Undefined, lc.Include);
        else Assert.Equal(IncludeOption.Both, lc.Include);
        if (i < 19) Assert.Equal(AxisDirection3.NotSet, lc.Direction);
        else Assert.Equal(AxisDirection3.X, lc.Direction);
        Assert.Equal("Load case " + i.ToString(), lc.Title);
        Assert.Equal(i++, lc.Index.Value);
        Assert.Equal(1, lc.Source);
        Assert.False(lc.Bridge);
      }

      //Checks - Category
      Assert.Equal(LoadCategory.Residential, loadCases[0].Category);
      Assert.Equal(LoadCategory.Office, loadCases[1].Category);
      Assert.Equal(LoadCategory.CongregationArea, loadCases[2].Category);
      Assert.Equal(LoadCategory.Shop, loadCases[3].Category);
      Assert.Equal(LoadCategory.Storage, loadCases[4].Category);
      Assert.Equal(LoadCategory.LightTraffic, loadCases[5].Category);
      Assert.Equal(LoadCategory.Traffic, loadCases[6].Category);
      Assert.Equal(LoadCategory.Roofs, loadCases[7].Category);

      //Checks - CaseType
      Assert.Equal(StructuralLoadCaseType.Live, loadCases[0].CaseType);
      Assert.Equal(StructuralLoadCaseType.Live, loadCases[1].CaseType);
      Assert.Equal(StructuralLoadCaseType.Live, loadCases[2].CaseType);
      Assert.Equal(StructuralLoadCaseType.Live, loadCases[3].CaseType);
      Assert.Equal(StructuralLoadCaseType.Live, loadCases[4].CaseType);
      Assert.Equal(StructuralLoadCaseType.Live, loadCases[5].CaseType);
      Assert.Equal(StructuralLoadCaseType.Live, loadCases[6].CaseType);
      Assert.Equal(StructuralLoadCaseType.Live, loadCases[7].CaseType);
      Assert.Equal(StructuralLoadCaseType.Dead, loadCases[8].CaseType);
      Assert.Equal(StructuralLoadCaseType.Soil, loadCases[9].CaseType);
      Assert.Equal(StructuralLoadCaseType.Generic, loadCases[10].CaseType);
      Assert.Equal(StructuralLoadCaseType.Generic, loadCases[11].CaseType);
      Assert.Equal(StructuralLoadCaseType.Wind, loadCases[12].CaseType);
      Assert.Equal(StructuralLoadCaseType.Snow, loadCases[13].CaseType);
      Assert.Equal(StructuralLoadCaseType.Rain, loadCases[14].CaseType);
      Assert.Equal(StructuralLoadCaseType.Thermal, loadCases[15].CaseType);
      Assert.Equal(StructuralLoadCaseType.Generic, loadCases[16].CaseType);
      Assert.Equal(StructuralLoadCaseType.Generic, loadCases[17].CaseType);
      Assert.Equal(StructuralLoadCaseType.Earthquake, loadCases[18].CaseType);
      Assert.Equal(StructuralLoadCaseType.Earthquake, loadCases[19].CaseType);
      Assert.Equal(StructuralLoadCaseType.Earthquake, loadCases[20].CaseType);

      for (i = 0; i < loadCases.Count(); i++)
      {
        Assert.True(new GsaLoadCaseParser(loadCases[i]).Gwa(out var gwa));
        if (i != 10 && i != 11 && i < 16) Assert.Equal(loadCaseGwas[i], gwa.First()); //TODO: check if cases not checked still produce valid gwa strings
      }
    }

    [Fact (Skip = "WIP")]
    public void GsaLoadGravitySimple()
    {
      //((MockSettings)Initialiser.AppResources.Settings).TargetLayer = GSALayer.Analysis;

      var gsaEls = GenerateMixedGsaEls();
      foreach (var gsaEl in gsaEls)
      {
        Assert.True(new GsaElParser(gsaEl).Gwa(out var gwa, true));
        //Helper.GwaToCache(gwa.First(), streamId1);
      }

      var gsaNodes = GenerateGsaNodes();
      foreach (var gsaNode in gsaNodes)
      {
        Assert.True(new GsaNodeParser(gsaNode).Gwa(out var gwa, true));
        //Helper.GwaToCache(gwa.First(), streamId1);
      }

      var gwa1 = "LOAD_GRAVITY.3\t+10% connections\tall\tall\t1\t0\t0\t-1.100000024";

      var gsaGrav1 = new GsaLoadGravity()
      {
        Index = 1,
        Name = "+10% connections",
        ElementIndices = new List<int> { 1, 2 }, //all
        Nodes = new List<int> { 1, 2 }, //all
        LoadCaseIndex = 1,
        Z = -1.100000024
      };

      Assert.True(new GsaLoadGravityParser(gsaGrav1).Gwa(out var gsaGravGwa, false));
      Assert.Equal(gwa1, gsaGravGwa.First());

      //Assert.True(ModelValidation(gsaGravGwa, GsaRecord.GetKeyword<GsaLoadGravity>(), 1, out var mismatch));
    }

    [Fact]
    public void GsaLoadGridLineSimple()
    {
      var loadGridLineGwas = new List<string>()
      {
        "LOAD_GRID_LINE.2	loadZ	1	POLYREF	1	1	GLOBAL	NO	Z	10	15",
        "LOAD_GRID_LINE.2	loadX	2	POLYREF	1	1	GLOBAL	NO	X	10	15"
      };

      var loadGridLines = new List<GsaLoadGridLine>();
      foreach (var g in loadGridLineGwas)
      {
        var l = new GsaLoadGridLineParser();
        Assert.True(l.FromGwa(g));
        loadGridLines.Add((GsaLoadGridLine)l.Record);
      }

      Assert.Equal(AxisDirection3.Z, loadGridLines[0].LoadDirection);
      Assert.Equal(LoadLineOption.PolyRef, loadGridLines[0].Line);
      Assert.Equal(1, loadGridLines[0].PolygonIndex);
      Assert.Equal(1, loadGridLines[0].LoadCaseIndex);
      Assert.Equal(AxisRefType.Global, loadGridLines[0].AxisRefType);
      Assert.False(loadGridLines[0].Projected);
      Assert.Equal(10, loadGridLines[0].Value1);
      Assert.Equal(15, loadGridLines[0].Value2);

      Assert.Equal(AxisDirection3.X, loadGridLines[1].LoadDirection);
      Assert.Equal(LoadLineOption.PolyRef, loadGridLines[1].Line);
      Assert.Equal(1, loadGridLines[1].PolygonIndex);
      Assert.Equal(1, loadGridLines[1].LoadCaseIndex);
      Assert.Equal(AxisRefType.Global, loadGridLines[1].AxisRefType);
      Assert.False(loadGridLines[1].Projected);
      Assert.Equal(10, loadGridLines[1].Value1);
      Assert.Equal(15, loadGridLines[1].Value2);

      for (int i = 0; i < loadGridLines.Count(); i++)
      {
        Assert.True(new GsaLoadGridLineParser(loadGridLines[i]).Gwa(out var gwa));
        Assert.Equal(loadGridLineGwas[i], gwa.First());
      }
    }

    [Fact]
    public void GsaLoadGridPointSimple()
    {
      var loadGridPointGwas = new List<string>()
      {
        "LOAD_GRID_POINT.2	loadZ	1	10	15	1	GLOBAL	Z	20",
        "LOAD_GRID_POINT.2	loadX	1	10	15	1	GLOBAL	X	20"
      };

      var loadGridPoints = new List<GsaLoadGridPoint>();
      foreach (var g in loadGridPointGwas)
      {
        var l = new GsaLoadGridPointParser();
        Assert.True(l.FromGwa(g));
        loadGridPoints.Add((GsaLoadGridPoint)l.Record);
      }

      Assert.Equal(AxisDirection3.Z, loadGridPoints[0].LoadDirection);
      Assert.Equal(1, loadGridPoints[0].LoadCaseIndex);
      Assert.Equal(10, loadGridPoints[0].X);
      Assert.Equal(15, loadGridPoints[0].Y);
      Assert.Equal(AxisRefType.Global, loadGridPoints[0].AxisRefType);
      Assert.Equal(20, loadGridPoints[0].Value);

      Assert.Equal(AxisDirection3.X, loadGridPoints[1].LoadDirection);
      Assert.Equal(1, loadGridPoints[1].LoadCaseIndex);
      Assert.Equal(10, loadGridPoints[1].X);
      Assert.Equal(15, loadGridPoints[1].Y);
      Assert.Equal(AxisRefType.Global, loadGridPoints[1].AxisRefType);
      Assert.Equal(20, loadGridPoints[1].Value);

      for (int i = 0; i < loadGridPoints.Count(); i++)
      {
        Assert.True(new GsaLoadGridPointParser(loadGridPoints[i]).Gwa(out var gwa));
        Assert.Equal(loadGridPointGwas[i], gwa.First());
      }
    }

    [Fact (Skip = "WIP")]
    public void GsaLoadNodeSimple()
    {
      var gsaObjRx = new GsaLoadNode()
      {
        ApplicationId = "AppId",
        Name = "Zero Dee Lode",
        Index = 1,
        NodeIndices = new List<int>() { 3, 4 },
        LoadCaseIndex = 3,
        GlobalAxis = true,
        LoadDirection = AxisDirection6.XX,
        Value = 23
      };

      Assert.True(new GsaLoadNodeParser(gsaObjRx).Gwa(out var gwa, true));
      Assert.NotEmpty(gwa);
      //Assert.True(ModelValidation(gwa, GsaRecord.GetKeyword<GsaLoadNode>(), 1, out var _));
    }

    [Fact (Skip = "Bugs")]
    public void GsaMatAnalSimple()
    {
      var matAnalGwas = new List<string>()
      {
        "MAT_ANAL.1\t1\tMAT_ELAS_ISO\tMaterial 1\tNO_RGB\t6\t2.05e+11\t0.3\t7850\t1.2e-05\t0\t0\t0\t0",
        "MAT_ANAL.1\t2\tMAT_ELAS_ORTHO\tMaterial 2\tNO_RGB\t14\t2.05e+11\t2.05e+11\t2.05e+11\t0.3\t0.3\t0.3\t7850\t1.2e-05\t1.2e-05\t1.2e-05\t7.8846e+10\t7.8846e+10\t7.8846e+10\t0\t0\t0",
        "MAT_ANAL.1\t3\tMAT_ELAS_PLAS_ISO\tMaterial 3\tNO_RGB\t9\t2.05e+11\t0.3\t7850\t1.2e-05\t275000000\t300000000\t0\t0\t0\t0\t0",
        "MAT_ANAL.1\t4\tMAT_MOHR_COULOMB\tMaterial 4\tNO_RGB\t9\t7.8846e+10\t0.3\t7850\t0\t0\t0\t0\t1.2e-05\t0\t0\t0",
        "MAT_ANAL.1\t5\tMAT_DRUCKER_PRAGER\tMaterial 5\tNO_RGB\t10\t7.8846e+10\t0.3\t7850\t0\t0\t0\t0\t-1\t1.2e-05\t0\t0\t0"
        //"MAT_ANAL.1\t6\tMAT_FABRIC\tMaterial 6\tNO_RGB\t5\t800000\t400000\t0.45\t30000\t0\t1\t\t0"
      };

      var matAnals = new List<GsaMatAnal>();
      foreach (var g in matAnalGwas)
      {
        var l = new GsaMatAnalParser();
        Assert.True(l.FromGwa(g));
        matAnals.Add((GsaMatAnal)l.Record);
      }

      #region MAT_ELAS_ISO
      Assert.Equal(MatAnalType.MAT_ELAS_ISO, matAnals[0].Type);
      Assert.Equal(2.05e+11, matAnals[0].E);
      Assert.Equal(0.3, matAnals[0].Nu);
      Assert.Equal(7850, matAnals[0].Rho);
      Assert.Equal(1.2e-5, matAnals[0].Alpha);
      Assert.Equal(0, matAnals[0].G);
      Assert.Equal(0, matAnals[0].Damp);
      #endregion
      #region MAT_ELAS_ORTHO
      Assert.Equal(MatAnalType.MAT_ELAS_ORTHO, matAnals[1].Type);
      Assert.Equal(2.05e+11, matAnals[1].Ex);
      Assert.Equal(2.05e+11, matAnals[1].Ey);
      Assert.Equal(2.05e+11, matAnals[1].Ez);
      Assert.Equal(0.3, matAnals[1].Nuxy);
      Assert.Equal(0.3, matAnals[1].Nuyz);
      Assert.Equal(0.3, matAnals[1].Nuzx);
      Assert.Equal(7850, matAnals[1].Rho);
      Assert.Equal(1.2e-5, matAnals[1].Alphax);
      Assert.Equal(1.2e-5, matAnals[1].Alphay);
      Assert.Equal(1.2e-5, matAnals[1].Alphaz);
      Assert.Equal(7.8846e+10, matAnals[1].Gxy);
      Assert.Equal(7.8846e+10, matAnals[1].Gyz);
      Assert.Equal(7.8846e+10, matAnals[1].Gzx);
      Assert.Equal(0, matAnals[1].Damp);
      #endregion
      #region MAT_ELAS_PLAS_ISO
      Assert.Equal(MatAnalType.MAT_ELAS_PLAS_ISO, matAnals[2].Type);
      Assert.Equal(2.05e+11, matAnals[2].E);
      Assert.Equal(0.3, matAnals[2].Nu);
      Assert.Equal(7850, matAnals[2].Rho);
      Assert.Equal(1.2e-5, matAnals[2].Alpha);
      Assert.Equal(275000000, matAnals[2].Yield);
      Assert.Equal(300000000, matAnals[2].Ultimate);
      Assert.Equal(0, matAnals[2].Eh);
      Assert.Equal(0, matAnals[2].Beta);
      Assert.Equal(0, matAnals[2].Damp);
      #endregion
      #region MAT_MOHR_COULOMB
      Assert.Equal(MatAnalType.MAT_MOHR_COULOMB, matAnals[3].Type);
      Assert.Equal(7.8846e+10, matAnals[3].G);
      Assert.Equal(0.3, matAnals[3].Nu);
      Assert.Equal(7850, matAnals[3].Rho);
      Assert.Equal(0, matAnals[3].Cohesion);
      Assert.Equal(0, matAnals[3].Phi);
      Assert.Equal(0, matAnals[3].Psi);
      Assert.Equal(0, matAnals[3].Eh);
      Assert.Equal(1.2e-5, matAnals[3].Alpha);
      Assert.Equal(0, matAnals[3].Damp);
      #endregion
      #region MAT_DRUCKER_PRAGER
      Assert.Equal(MatAnalType.MAT_DRUCKER_PRAGER, matAnals[4].Type);
      Assert.Equal(7.8846e+10, matAnals[4].G);
      Assert.Equal(0.3, matAnals[4].Nu);
      Assert.Equal(7850, matAnals[4].Rho);
      Assert.Equal(0, matAnals[4].Cohesion);
      Assert.Equal(0, matAnals[4].Phi);
      Assert.Equal(0, matAnals[4].Psi);
      Assert.Equal(0, matAnals[4].Eh);
      Assert.Equal(-1, matAnals[4].Scribe);
      Assert.Equal(1.2e-5, matAnals[4].Alpha);
      Assert.Equal(0, matAnals[4].Damp);
      #endregion

      for (int i = 0; i < matAnals.Count(); i++)
      {
        Assert.True(new GsaMatAnalParser(matAnals[i]).Gwa(out var gwa));

        //replace with scientific notation 
        //gwa can be read directly into GSA without any problems. This is just to simplify the comparison
        gwa[0] = gwa[0].Replace("205000000000", "2.05e+11");
        gwa[0] = gwa[0].Replace("78846000000", "7.8846e+10");
        gwa[0] = gwa[0].Replace("1.2E-05", "1.2e-05");

        //compare with original gwa string
        Assert.Equal(matAnalGwas[i], gwa.First());
      }
    }

    [Fact] // (Skip = "Bugs identified in keyword definition documentation")
    public void GsaMatConcreteSimple()
    {
      var matConcreteGwas = new List<string>()
      {
        //"MAT_CONCRETE.17\t1\tMAT.11\t40 MPa\t3.315274903e+10\t40000000\t0.2\t1.381364543e+10\t2400\t1e-05\tMAT_ANAL.1\tConcrete\t-268435456\tMAT_ELAS_ISO\t6\t3.315274903e+10\t0.2\t2400\t1e-05\t1.381364543e+10\t0\t0\t0\t0\t0\t0\t0\t0\tMAT_CURVE_PARAM.3\t\tRECTANGLE+NO_TENSION\t0.00068931\t0\t0.00069069\t0\t0.003\t1\t1\t1\tMAT_CURVE_PARAM.3\t\tLINEAR+INTERPOLATED\t0.003\t0\t0.003\t0\t0.003\t0.0001144620975\t1\t1\t0\tConcrete\tNO\tCYLINDER\tN\t40000000\t34000000\t16000000\t3794733.192\t2276839.915\t0\t1\t2\t0.003\t0.003\t0.00069\t0.003\t0.0025\t0.002\t0.0025\tNO\t0.02\t0\t1\t0.77\t0\t0\t0\t0\t0"
        "MAT_CONCRETE.17\t1\tMAT.11\t35 MPa\t26423.06328\t35\t0.2\t11009.6097\t2.3\t1e-05\tMAT_ANAL.1\tConcrete\t-268435456\tMAT_ELAS_ISO\t6\t26423.06328\t0.2\t2.3\t1e-05\t11009.6097\t0\t0\t0\t0\t0\t0\t0\t0\tMAT_CURVE_PARAM.3\t\tRECTANGLE+NO_TENSION\t0.00041083875\t0\t0.00041166125\t0\t0.0035\t1\t1.538461538\t1\tMAT_CURVE_PARAM.3\t\tPOPOVICS+INTERPOLATED\t0\t0\t0.00203720187\t0\t0.0035\t0.0001343389989\t1\t1\t0\tConcrete\tNO\tCYLINDER\tN\t35\t27.9125\t14\t3.54964787\t2.18894952\t0\t1\t2\t0.00203720187\t0.0035\t0.00041125\t0.0035\t0.0035\t0.002\t0.0035\tNO\t0.02\t0\t1\t0.8825\t0\t0\t0\t0\t0"
      };
      var matConcretes = new List<GsaMatConcrete>();
      foreach (var g in matConcreteGwas)
      {
        var l = new GsaMatConcreteParser();
        Assert.True(l.FromGwa(g));
        matConcretes.Add((GsaMatConcrete)l.Record);
      }
      Assert.Equal(26423.06328, matConcretes[0].Mat.E);
      Assert.Equal(35, matConcretes[0].Mat.F);
      Assert.Equal(0.2, matConcretes[0].Mat.Nu);
      Assert.Equal(11009.6097, matConcretes[0].Mat.G);
      Assert.Equal(2.3, matConcretes[0].Mat.Rho);
      Assert.Equal(1e-05, matConcretes[0].Mat.Alpha);
      Assert.Equal(MatAnalType.MAT_ELAS_ISO, matConcretes[0].Mat.Prop.Type);
      Assert.Equal(6, matConcretes[0].Mat.Prop.NumParams);
      Assert.Equal(26423.06328, matConcretes[0].Mat.Prop.E);
      Assert.Equal(0.2, matConcretes[0].Mat.Prop.Nu);
      Assert.Equal(2.3, matConcretes[0].Mat.Prop.Rho);
      Assert.Equal(1e-5, matConcretes[0].Mat.Prop.Alpha);
      Assert.Equal(11009.6097, matConcretes[0].Mat.Prop.G);
      Assert.Equal(0, matConcretes[0].Mat.Prop.Damp);
      Assert.Equal(0, matConcretes[0].Mat.NumUC);
      Assert.Equal(Dimension.NotSet, matConcretes[0].Mat.AbsUC);
      Assert.Equal(Dimension.NotSet, matConcretes[0].Mat.OrdUC);
      Assert.Equal(new double[0], matConcretes[0].Mat.PtsUC);
      Assert.Equal(0, matConcretes[0].Mat.NumSC);
      Assert.Equal(Dimension.NotSet, matConcretes[0].Mat.AbsSC);
      Assert.Equal(Dimension.NotSet, matConcretes[0].Mat.OrdSC);
      Assert.Equal(new double[0], matConcretes[0].Mat.PtsSC);
      Assert.Equal(0, matConcretes[0].Mat.NumUT);
      Assert.Equal(Dimension.NotSet, matConcretes[0].Mat.AbsUT);
      Assert.Equal(Dimension.NotSet, matConcretes[0].Mat.OrdUT);
      Assert.Equal(new double[0], matConcretes[0].Mat.PtsUT);
      Assert.Equal(0, matConcretes[0].Mat.NumST);
      Assert.Equal(Dimension.NotSet, matConcretes[0].Mat.AbsST);
      Assert.Equal(Dimension.NotSet, matConcretes[0].Mat.OrdST);
      Assert.Equal(new double[0], matConcretes[0].Mat.PtsST);
      Assert.Equal(0, matConcretes[0].Mat.Eps);
      Assert.Equal(new List<MatCurveParamType>() { MatCurveParamType.RECTANGLE, MatCurveParamType.NO_TENSION }, matConcretes[0].Mat.Uls.Model);
      Assert.Equal(0.00041083875, matConcretes[0].Mat.Uls.StrainElasticCompression);
      Assert.Equal(0, matConcretes[0].Mat.Uls.StrainElasticTension);
      Assert.Equal(0.00041166125, matConcretes[0].Mat.Uls.StrainPlasticCompression);
      Assert.Equal(0, matConcretes[0].Mat.Uls.StrainPlasticTension);
      Assert.Equal(0.0035, matConcretes[0].Mat.Uls.StrainFailureCompression);
      Assert.Equal(1, matConcretes[0].Mat.Uls.StrainFailureTension);
      Assert.Equal(1.538461538, matConcretes[0].Mat.Uls.GammaF);
      Assert.Equal(1, matConcretes[0].Mat.Uls.GammaE);
      Assert.Equal(new List<MatCurveParamType>() { MatCurveParamType.POPOVICS, MatCurveParamType.INTERPOLATED }, matConcretes[0].Mat.Sls.Model);
      Assert.Equal(0, matConcretes[0].Mat.Sls.StrainElasticCompression);
      Assert.Equal(0, matConcretes[0].Mat.Sls.StrainElasticTension);
      Assert.Equal(0.00203720187, matConcretes[0].Mat.Sls.StrainPlasticCompression);
      Assert.Equal(0, matConcretes[0].Mat.Sls.StrainPlasticTension);
      Assert.Equal(0.0035, matConcretes[0].Mat.Sls.StrainFailureCompression);
      Assert.Equal(0.0001343389989, matConcretes[0].Mat.Sls.StrainFailureTension);
      Assert.Equal(1, matConcretes[0].Mat.Sls.GammaF);
      Assert.Equal(1, matConcretes[0].Mat.Sls.GammaE);
      Assert.Equal(0, matConcretes[0].Mat.Cost);
      Assert.Equal(MatType.CONCRETE, matConcretes[0].Mat.Type);
      Assert.Equal(MatConcreteType.CYLINDER, matConcretes[0].Type);
      Assert.Equal(MatConcreteCement.N, matConcretes[0].Cement);
      Assert.Equal(35, matConcretes[0].Fc);
      Assert.Equal(27.9125, matConcretes[0].Fcd);
      Assert.Equal(14, matConcretes[0].Fcdc);
      Assert.Equal(3.54964787, matConcretes[0].Fcdt);
      Assert.Equal(2.18894952, matConcretes[0].Fcfib);
      Assert.Equal(0, matConcretes[0].EmEs);
      Assert.Equal(1, matConcretes[0].Emod);
      Assert.Equal(2, matConcretes[0].N);
      Assert.Equal(0.00203720187, matConcretes[0].Eps);
      Assert.Equal(0.0035, matConcretes[0].EpsPeak);
      Assert.Equal(0.00041125, matConcretes[0].EpsMax);
      Assert.Equal(0.0035, matConcretes[0].EpsU);
      Assert.Equal(0.0035, matConcretes[0].EpsAx);
      Assert.Equal(0.002, matConcretes[0].EpsTran);
      Assert.Equal(0.0035, matConcretes[0].EpsAxs);
      Assert.False(matConcretes[0].Light);
      Assert.Equal(0.02, matConcretes[0].Agg);
      Assert.Equal(0, matConcretes[0].XdMin);
      Assert.Equal(1, matConcretes[0].XdMax);
      Assert.Equal(0.8825, matConcretes[0].Beta);
      Assert.Equal(0, matConcretes[0].Shrink);
      Assert.Equal(0, matConcretes[0].Confine);
      Assert.Equal(0, matConcretes[0].Fcc);
      Assert.Equal(0, matConcretes[0].EpsPlasC);
      Assert.Equal(0, matConcretes[0].EpsUC);

      for (int i = 0; i < matConcretes.Count(); i++)
      {
        Assert.True(new GsaMatConcreteParser(matConcretes[i]).Gwa(out var gwa));

        //replace with scientific notation 
        //gwa can be read directly into GSA without any problems. This is just to simplify the comparison
        gwa[0] = gwa[0].Replace("1E-05", "1e-05");
        gwa[0] = gwa[0].Replace("\tCONCRETE", "\tConcrete");

        //compare with original gwa string
        Assert.Equal(matConcreteGwas[i], gwa.First());
      }
    }

    [Fact]
    public void GsaMatCurveParamSimple()
    {
      var matCurveParamGwas = new List<string>()
      {
        "MAT_CURVE_PARAM.3\t\tRECTANGLE+EXPLICIT\t0.00041083875\t0\t0.00041166125\t0\t0.003\t0\t1\t1",
        "MAT_CURVE_PARAM.3\t\tEXPLICIT\t0\t0\t0\t0\t0\t0\t1\t1",
        "MAT_CURVE_PARAM.3\t\tUNDEF\t0.0018\t0.0018\t0.0018\t0.0018\t0.05\t0.05\t1\t1",
        "MAT_CURVE_PARAM.3\t\tELAS_PLAS\t0.0016\t0.0016\t0.0016\t0.0016\t0.05\t0.05\t1\t1",
        "MAT_CURVE_PARAM.3\t\tRECTANGLE+NO_TENSION\t0.00068931\t0\t0.00069069\t0\t0.003\t1\t1\t1",
        "MAT_CURVE_PARAM.3\t\tLINEAR+INTERPOLATED\t0.003\t0\t0.003\t0\t0.003\t0.0001144620975\t1\t1"
      };

      var matCurveParams = new List<GsaMatCurveParam>();
      foreach (var g in matCurveParamGwas)
      {
        var l = new GsaMatCurveParamParser();
        Assert.True(l.FromGwa(g));
        matCurveParams.Add((GsaMatCurveParam)l.Record);
      }

      #region curve 1
      Assert.Equal(new List<MatCurveParamType>() { MatCurveParamType.RECTANGLE, MatCurveParamType.EXPLICIT }, matCurveParams[0].Model);
      Assert.Equal(0.00041083875, matCurveParams[0].StrainElasticCompression);
      Assert.Equal(0, matCurveParams[0].StrainElasticTension);
      Assert.Equal(0.00041166125, matCurveParams[0].StrainPlasticCompression);
      Assert.Equal(0, matCurveParams[0].StrainPlasticTension);
      Assert.Equal(0.003, matCurveParams[0].StrainFailureCompression);
      Assert.Equal(0, matCurveParams[0].StrainFailureTension);
      Assert.Equal(1, matCurveParams[0].GammaF);
      Assert.Equal(1, matCurveParams[0].GammaE);
      #endregion
      #region curve 2
      Assert.Equal(new List<MatCurveParamType>() { MatCurveParamType.EXPLICIT }, matCurveParams[1].Model);
      Assert.Equal(0, matCurveParams[1].StrainElasticCompression);
      Assert.Equal(0, matCurveParams[1].StrainElasticTension);
      Assert.Equal(0, matCurveParams[1].StrainPlasticCompression);
      Assert.Equal(0, matCurveParams[1].StrainPlasticTension);
      Assert.Equal(0, matCurveParams[1].StrainFailureCompression);
      Assert.Equal(0, matCurveParams[1].StrainFailureTension);
      Assert.Equal(1, matCurveParams[1].GammaF);
      Assert.Equal(1, matCurveParams[1].GammaE);
      #endregion
      #region curve 3
      Assert.Equal(new List<MatCurveParamType>() { MatCurveParamType.UNDEF }, matCurveParams[2].Model);
      Assert.Equal(0.0018, matCurveParams[2].StrainElasticCompression);
      Assert.Equal(0.0018, matCurveParams[2].StrainElasticTension);
      Assert.Equal(0.0018, matCurveParams[2].StrainPlasticCompression);
      Assert.Equal(0.0018, matCurveParams[2].StrainPlasticTension);
      Assert.Equal(0.05, matCurveParams[2].StrainFailureCompression);
      Assert.Equal(0.05, matCurveParams[2].StrainFailureTension);
      Assert.Equal(1, matCurveParams[2].GammaF);
      Assert.Equal(1, matCurveParams[2].GammaE);
      #endregion
      #region curve 4
      Assert.Equal(new List<MatCurveParamType>() { MatCurveParamType.ELAS_PLAS }, matCurveParams[3].Model);
      Assert.Equal(0.0016, matCurveParams[3].StrainElasticCompression);
      Assert.Equal(0.0016, matCurveParams[3].StrainElasticTension);
      Assert.Equal(0.0016, matCurveParams[3].StrainPlasticCompression);
      Assert.Equal(0.0016, matCurveParams[3].StrainPlasticTension);
      Assert.Equal(0.05, matCurveParams[3].StrainFailureCompression);
      Assert.Equal(0.05, matCurveParams[3].StrainFailureTension);
      Assert.Equal(1, matCurveParams[3].GammaF);
      Assert.Equal(1, matCurveParams[3].GammaE);
      #endregion
      #region curve 5
      Assert.Equal(new List<MatCurveParamType>() { MatCurveParamType.RECTANGLE, MatCurveParamType.NO_TENSION }, matCurveParams[4].Model);
      Assert.Equal(0.00068931, matCurveParams[4].StrainElasticCompression);
      Assert.Equal(0, matCurveParams[4].StrainElasticTension);
      Assert.Equal(0.00069069, matCurveParams[4].StrainPlasticCompression);
      Assert.Equal(0, matCurveParams[4].StrainPlasticTension);
      Assert.Equal(0.003, matCurveParams[4].StrainFailureCompression);
      Assert.Equal(1, matCurveParams[4].StrainFailureTension);
      Assert.Equal(1, matCurveParams[4].GammaF);
      Assert.Equal(1, matCurveParams[4].GammaE);
      #endregion
      #region curve 6
      Assert.Equal(new List<MatCurveParamType>() { MatCurveParamType.LINEAR, MatCurveParamType.INTERPOLATED }, matCurveParams[5].Model);
      Assert.Equal(0.003, matCurveParams[5].StrainElasticCompression);
      Assert.Equal(0, matCurveParams[5].StrainElasticTension);
      Assert.Equal(0.003, matCurveParams[5].StrainPlasticCompression);
      Assert.Equal(0, matCurveParams[5].StrainPlasticTension);
      Assert.Equal(0.003, matCurveParams[5].StrainFailureCompression);
      Assert.Equal(0.0001144620975, matCurveParams[5].StrainFailureTension);
      Assert.Equal(1, matCurveParams[5].GammaF);
      Assert.Equal(1, matCurveParams[5].GammaE);
      #endregion

      for (int i = 0; i < matCurveParams.Count(); i++)
      {
        Assert.True(new GsaMatCurveParamParser(matCurveParams[i]).Gwa(out var gwa));
        Assert.Equal(matCurveParamGwas[i], gwa.First());
      }
    }

    [Fact]
    public void GsaMatCurveSimple()
    {
      var matCurveGwas = new List<string>()
      {
        "MAT_CURVE.1\t1\tMaterial Curve 1\tDISP\tFORCE\t\"(1,10) (2,20) (3,30)\""
      };

      var matCurves = new List<GsaMatCurve>();
      foreach (var g in matCurveGwas)
      {
        var l = new GsaMatCurveParser();
        Assert.True(l.FromGwa(g));
        matCurves.Add((GsaMatCurve)l.Record);
      }

      Assert.Equal(Dimension.DISP, matCurves[0].Abscissa);
      Assert.Equal(Dimension.FORCE, matCurves[0].Ordinate);
      Assert.Equal(new double[3, 2] { { 1, 10 }, { 2, 20 }, { 3, 30 } }, matCurves[0].Table);

      for (int i = 0; i < matCurves.Count(); i++)
      {
        Assert.True(new GsaMatCurveParser(matCurves[i]).Gwa(out var gwa));
        Assert.Equal(matCurveGwas[i], gwa.First());
      }
    }

    [Fact (Skip = "Bugs")]
    public void GsaMatSteelSimple()
    {
      var matSteelGwas = new List<string>()
      {
        "MAT_STEEL.3\t1\tMAT.10\t350(AS3678)\t2e+11\t360000000\t0.3\t7.692307692e+10\t7850\t1.2e-05\tMAT_ANAL.1\tSteel\t-268435456\tMAT_ELAS_ISO\t6\t2e+11\t0.3\t7850\t1.2e-05\t7.692307692e+10\t0\t0\t0\t0\t0\t0\t0\t0.05\tMAT_CURVE_PARAM.3\t\tUNDEF\t0.0018\t0.0018\t0.0018\t0.0018\t0.05\t0.05\t1\t1\tMAT_CURVE_PARAM.3\t\tELAS_PLAS\t0.0018\t0.0018\t0.0018\t0.0018\t0.05\t0.05\t1\t1\t0\tSteel\t360000000\t450000000\t0\t0"
      };
      var matSteels = new List<GsaMatSteel>();
      foreach (var g in matSteelGwas)
      {
        var l = new GsaMatSteelParser();
        Assert.True(l.FromGwa(g));
        matSteels.Add((GsaMatSteel)l.Record);
      }

      # region Checks
      Assert.Equal(2e11, matSteels[0].Mat.E);
      Assert.Equal(360000000, matSteels[0].Mat.F);
      Assert.Equal(0.3, matSteels[0].Mat.Nu);
      Assert.Equal(7.692307692e+10, matSteels[0].Mat.G);
      Assert.Equal(7850, matSteels[0].Mat.Rho);
      Assert.Equal(1.2e-05, matSteels[0].Mat.Alpha);
      Assert.Equal(MatAnalType.MAT_ELAS_ISO, matSteels[0].Mat.Prop.Type);
      Assert.Equal(6, matSteels[0].Mat.Prop.NumParams);
      Assert.Equal(2e11, matSteels[0].Mat.Prop.E);
      Assert.Equal(0.3, matSteels[0].Mat.Prop.Nu);
      Assert.Equal(7850, matSteels[0].Mat.Prop.Rho);
      Assert.Equal(1.2e-5, matSteels[0].Mat.Prop.Alpha);
      Assert.Equal(7.692307692e+10, matSteels[0].Mat.Prop.G);
      Assert.Equal(0, matSteels[0].Mat.Prop.Damp);
      Assert.Equal(0, matSteels[0].Mat.NumUC);
      Assert.Equal(Dimension.NotSet, matSteels[0].Mat.AbsUC);
      Assert.Equal(Dimension.NotSet, matSteels[0].Mat.OrdUC);
      Assert.Equal(new double[0], matSteels[0].Mat.PtsUC);
      Assert.Equal(0, matSteels[0].Mat.NumSC);
      Assert.Equal(Dimension.NotSet, matSteels[0].Mat.AbsSC);
      Assert.Equal(Dimension.NotSet, matSteels[0].Mat.OrdSC);
      Assert.Equal(new double[0], matSteels[0].Mat.PtsSC);
      Assert.Equal(0, matSteels[0].Mat.NumUT);
      Assert.Equal(Dimension.NotSet, matSteels[0].Mat.AbsUT);
      Assert.Equal(Dimension.NotSet, matSteels[0].Mat.OrdUT);
      Assert.Equal(new double[0], matSteels[0].Mat.PtsUT);
      Assert.Equal(0, matSteels[0].Mat.NumST);
      Assert.Equal(Dimension.NotSet, matSteels[0].Mat.AbsST);
      Assert.Equal(Dimension.NotSet, matSteels[0].Mat.OrdST);
      Assert.Equal(new double[0], matSteels[0].Mat.PtsST);
      Assert.Equal(0.05, matSteels[0].Mat.Eps);
      Assert.Equal(new List<MatCurveParamType>() { MatCurveParamType.UNDEF }, matSteels[0].Mat.Uls.Model);
      Assert.Equal(0.0018, matSteels[0].Mat.Uls.StrainElasticCompression);
      Assert.Equal(0.0018, matSteels[0].Mat.Uls.StrainElasticTension);
      Assert.Equal(0.0018, matSteels[0].Mat.Uls.StrainPlasticCompression);
      Assert.Equal(0.0018, matSteels[0].Mat.Uls.StrainPlasticTension);
      Assert.Equal(0.05, matSteels[0].Mat.Uls.StrainFailureCompression);
      Assert.Equal(0.05, matSteels[0].Mat.Uls.StrainFailureTension);
      Assert.Equal(1, matSteels[0].Mat.Uls.GammaF);
      Assert.Equal(1, matSteels[0].Mat.Uls.GammaE);
      Assert.Equal(new List<MatCurveParamType>() { MatCurveParamType.ELAS_PLAS }, matSteels[0].Mat.Sls.Model);
      Assert.Equal(0.0018, matSteels[0].Mat.Sls.StrainElasticCompression);
      Assert.Equal(0.0018, matSteels[0].Mat.Sls.StrainElasticTension);
      Assert.Equal(0.0018, matSteels[0].Mat.Sls.StrainPlasticCompression);
      Assert.Equal(0.0018, matSteels[0].Mat.Sls.StrainPlasticTension);
      Assert.Equal(0.05, matSteels[0].Mat.Sls.StrainFailureCompression);
      Assert.Equal(0.05, matSteels[0].Mat.Sls.StrainFailureTension);
      Assert.Equal(1, matSteels[0].Mat.Sls.GammaF);
      Assert.Equal(1, matSteels[0].Mat.Sls.GammaE);
      Assert.Equal(0, matSteels[0].Mat.Cost);
      Assert.Equal(MatType.STEEL, matSteels[0].Mat.Type);
      Assert.Equal(360000000, matSteels[0].Fy);
      Assert.Equal(450000000, matSteels[0].Fu);
      Assert.Equal(0, matSteels[0].EpsP);
      Assert.Equal(0, matSteels[0].Eh);
      #endregion

      for (int i = 0; i < matSteels.Count(); i++)
      {
        Assert.True(new GsaMatSteelParser(matSteels[i]).Gwa(out var gwa));

        //replace with scientific notation 
        //gwa can be read directly into GSA without any problems. This is just to simplify the comparison
        gwa[0] = gwa[0].Replace("200000000000", "2e+11");
        gwa[0] = gwa[0].Replace("76923076920", "7.692307692e+10");
        gwa[0] = gwa[0].Replace("1.2E-05", "1.2e-05");
        gwa[0] = gwa[0].Replace("\tSTEEL", "\tSteel");

        //compare with original gwa string
        Assert.Equal(matSteelGwas[i], gwa.First());
      }
    }

    [Fact (Skip = "WIP")]
    public void GsaMemb1dSimple()
    {
      //An asterisk next to a row signifies non-obvious values I've specifically changed between all 3 (obvious values are application ID, index and name)

      var gsaMembBeamAuto = new GsaMemb()
      {
        ApplicationId = "beamauto",
        Name = "Beam Auto",
        Index = 1,
        Type = MemberType.Beam, //*
        Exposure = ExposedSurfaces.ALL, //*
        PropertyIndex = 3,
        Group = 1,
        NodeIndices = new List<int>() { 4, 5 },
        OrientationNodeIndex = 6,
        Angle = 10,
        MeshSize = 11,
        IsIntersector = true,
        AnalysisType = AnalysisType.BEAM, //*
        Fire = FireResistance.HalfHour, //*
        LimitingTemperature = 12,
        CreationFromStartDays = 13,
        RemovedAtDays = 16,
        Releases1 = new Dictionary<AxisDirection6, ReleaseCode>() { { AxisDirection6.X, ReleaseCode.Released }, { AxisDirection6.XX, ReleaseCode.Released } }, //*
        Releases2 = new Dictionary<AxisDirection6, ReleaseCode>() { { AxisDirection6.Y, ReleaseCode.Released } }, //*
        RestraintEnd1 = Restraint.Fixed, //*
        RestraintEnd2 = Restraint.Pinned, //*
        EffectiveLengthType = EffectiveLengthType.Automatic, //*
        LoadHeight = 19,
        LoadHeightReferencePoint = LoadHeightReferencePoint.TopFlange, //*
        MemberHasOffsets = false
      };
      Assert.True(new GsaMembParser(gsaMembBeamAuto).Gwa(out var gwa1, false));

      var gsaMemb = new GsaMembParser();
      Assert.True(gsaMemb.FromGwa(gwa1.First()));
      //gsaMembBeamAuto.ShouldDeepEqual((GsaMemb)gsaMemb.Record);

      var gsaMembColEffLen = new GsaMemb()
      {
        ApplicationId = "efflencol",
        Name = "Eff Len Col",
        Index = 2,
        Type = MemberType.Column, //*
        Exposure = ExposedSurfaces.ONE, //*
        PropertyIndex = 3,
        Group = 2,
        NodeIndices = new List<int>() { 4, 5 },
        OrientationNodeIndex = 6,
        Angle = 10,
        MeshSize = 11,
        IsIntersector = true,
        AnalysisType = AnalysisType.BAR, //*
        Fire = FireResistance.FourHours,//*
        LimitingTemperature = 12,
        CreationFromStartDays = 13,
        RemovedAtDays = 16,
        Releases1 = new Dictionary<AxisDirection6, ReleaseCode>() { { AxisDirection6.Y, ReleaseCode.Released }, { AxisDirection6.YY, ReleaseCode.Stiff } }, //*
        Stiffnesses1 = new List<double>() { 17 }, //*
        RestraintEnd1 = Restraint.FullRotational, //*
        RestraintEnd2 = Restraint.Pinned, //*
        EffectiveLengthType = EffectiveLengthType.EffectiveLength, //*
        EffectiveLengthYY = 18, //*
        PercentageZZ = 65, //*
        EffectiveLengthLateralTorsional = 19, //*
        LoadHeight = 19, //*
        LoadHeightReferencePoint = LoadHeightReferencePoint.ShearCentre, //*
        MemberHasOffsets = false
      };
      Assert.True(new GsaMembParser(gsaMembColEffLen).Gwa(out var gwa2, false));

      gsaMemb = new GsaMembParser();
      Assert.True(gsaMemb.FromGwa(gwa2.First()));
      //gsaMembColEffLen.ShouldDeepEqual((GsaMemb)gsaMemb.Record);

      var gsaMembGeneric1dExplicit = new GsaMemb()
      {
        ApplicationId = "explicitcol",
        Name = "Explicit Generic 1D",
        Index = 3,
        Type = MemberType.Generic1d, //*
        Exposure = ExposedSurfaces.NONE, //*
        PropertyIndex = 3,
        Group = 3,
        NodeIndices = new List<int>() { 4, 5 },
        OrientationNodeIndex = 6,
        Angle = 10,
        MeshSize = 11,
        IsIntersector = true,
        AnalysisType = AnalysisType.DAMPER, //*
        Fire = FireResistance.FourHours, //*
        LimitingTemperature = 12,
        CreationFromStartDays = 13,
        RemovedAtDays = 16,
        Releases1 = new Dictionary<AxisDirection6, ReleaseCode>() { { AxisDirection6.Y, ReleaseCode.Released }, { AxisDirection6.YY, ReleaseCode.Stiff } }, //*
        Stiffnesses1 = new List<double>() { 17 }, //*
        RestraintEnd1 = Restraint.FullRotational, //*
        RestraintEnd2 = Restraint.Pinned, //*
        EffectiveLengthType = EffectiveLengthType.Explicit, //*
        PointRestraints = new List<RestraintDefinition>()
        {
          new RestraintDefinition() { All = true, Restraint = Restraint.TopFlangeLateral }
        },  //*
        SpanRestraints = new List<RestraintDefinition>()
        {
          new RestraintDefinition() { Index = 1, Restraint = Restraint.Fixed },
          new RestraintDefinition() { Index = 3, Restraint = Restraint.PartialRotational }
        },  //*
        LoadHeight = 19,
        LoadHeightReferencePoint = LoadHeightReferencePoint.BottomFlange, //*
        MemberHasOffsets = false
      };
      Assert.True(new GsaMembParser(gsaMembGeneric1dExplicit).Gwa(out var gwa3, false));

      gsaMemb = new GsaMembParser();
      Assert.True(gsaMemb.FromGwa(gwa3.First()));
      //gsaMembGeneric1dExplicit.ShouldDeepEqual((GsaMemb)gsaMemb.Record);

      var gwaToTest = gwa1.Union(gwa2).Union(gwa3).ToList();

      //Assert.True(ModelValidation(gwaToTest, GsaRecord.GetKeyword<GsaMemb>(), 3, out var mismatch));
    }

    [Fact (Skip = "WIP")]
    public void GsaMemb2dSimple()
    {
      var gsaMembSlabLinear = new GsaMemb()
      {
        ApplicationId = "slablinear",
        Name = "Slab Linear",
        Index = 1,
        Type = MemberType.Slab, //*
        Exposure = ExposedSurfaces.ALL, //*
        PropertyIndex = 2,
        Group = 1,
        NodeIndices = new List<int>() { 4, 5, 6, 7 },
        Voids = new List<List<int>>() { new List<int>() { 8, 9, 10 } },
        OrientationNodeIndex = 3,
        Angle = 11,
        MeshSize = 12,
        IsIntersector = true,
        AnalysisType = AnalysisType.LINEAR, //*
        Fire = FireResistance.HalfHour, //*
        LimitingTemperature = 13,
        CreationFromStartDays = 14,
        RemovedAtDays = 15,
        Offset2dZ = 16,
        OffsetAutomaticInternal = false
      };
      Assert.True(new GsaMembParser(gsaMembSlabLinear).Gwa(out var gwa1, false));

      var gsaMemb = new GsaMembParser();
      Assert.True(gsaMemb.FromGwa(gwa1.First()));
      //gsaMembSlabLinear.ShouldDeepEqual((GsaMemb)gsaMemb.Record);

      var gsaMembWallQuadratic = new GsaMemb()
      {
        ApplicationId = "wallquadratic",
        Name = "Wall Quadratic",
        Index = 2,
        Type = MemberType.Wall, //*
        Exposure = ExposedSurfaces.SIDES, //*
        PropertyIndex = 2,
        Group = 2,
        NodeIndices = new List<int>() { 4, 5, 6, 7 },
        Voids = new List<List<int>>() { new List<int>() { 8, 9, 10 } },
        OrientationNodeIndex = 3,
        Angle = 11,
        MeshSize = 12,
        IsIntersector = true,
        AnalysisType = AnalysisType.QUADRATIC, //*
        Fire = FireResistance.ThreeHours, //*
        LimitingTemperature = 13,
        CreationFromStartDays = 14,
        RemovedAtDays = 15,
        Offset2dZ = 16,
        OffsetAutomaticInternal = false
      };
      Assert.True(new GsaMembParser(gsaMembWallQuadratic).Gwa(out var gwa2, false));

      gsaMemb = new GsaMembParser();
      Assert.True(gsaMemb.FromGwa(gwa2.First()));
      //gsaMembWallQuadratic.ShouldDeepEqual((GsaMemb)gsaMemb.Record);

      var gsaMembGeneric = new GsaMemb()
      {
        ApplicationId = "generic2dRigid",
        Name = "Wall XY Rigid Diaphragm",
        Index = 3,
        Type = MemberType.Wall, //*
        Exposure = ExposedSurfaces.SIDES, //*
        PropertyIndex = 2,
        Group = 3,
        NodeIndices = new List<int>() { 4, 5, 6, 7 },
        Voids = new List<List<int>>() { new List<int>() { 8, 9, 10 } },
        OrientationNodeIndex = 3,
        Angle = 11,
        MeshSize = 12,
        IsIntersector = true,
        AnalysisType = AnalysisType.RIGID, //*
        Fire = FireResistance.TwoHours, //*
        LimitingTemperature = 13,
        CreationFromStartDays = 14,
        RemovedAtDays = 15,
        Offset2dZ = 16,
        OffsetAutomaticInternal = false
      };
      Assert.True(new GsaMembParser(gsaMembGeneric).Gwa(out var gwa3, false));

      gsaMemb = new GsaMembParser();
      Assert.True(gsaMemb.FromGwa(gwa3.First()));
      //gsaMembGeneric.ShouldDeepEqual((GsaMemb)gsaMemb.Record);

      var gwaToTest = gwa1.Union(gwa2).Union(gwa3).ToList();

      //Assert.True(ModelValidation(gwaToTest, GsaRecord.GetKeyword<GsaMemb>(), 3, out var mismatch));
    }

    [Fact]
    public void GsaNodeSimple()
    {
      var nodeGwas = new List<string>()
      {
        "NODE.3\t1\t\tNO_RGB\t628.3\t-107\t222.8",
        "NODE.3\t1\t\tNO_RGB\t628.3\t-107\t222.8\tfree\tGLOBAL\t0\t1\t2",
        "NODE.3\t1\t\tNO_RGB\t628.3\t-107\t222.8\txz\tGLOBAL\t45\t1\t2"
      };
      var nodes = new List<GsaNode>();
      foreach (var g in nodeGwas)
      {
        var n = new GsaNodeParser();
        Assert.True(n.FromGwa(g));
        nodes.Add((GsaNode)n.Record);
      }

      Assert.Equal(628.3, nodes[0].X);
      Assert.Equal(-107, nodes[0].Y);
      Assert.Equal(222.8, nodes[0].Z);
      Assert.Equal(NodeRestraint.Free, nodes[0].NodeRestraint);
      Assert.True(nodes[0].Restraints == null || nodes[0].Restraints.Count() == 0);
      Assert.Null(nodes[0].SpringPropertyIndex);
      Assert.Null(nodes[0].MassPropertyIndex);

      Assert.Equal(628.3, nodes[1].X);
      Assert.Equal(-107, nodes[1].Y);
      Assert.Equal(222.8, nodes[1].Z);
      Assert.Equal(NodeRestraint.Free, nodes[1].NodeRestraint);
      Assert.True(nodes[1].Restraints == null || nodes[1].Restraints.Count() == 0);
      Assert.Equal(1, nodes[1].SpringPropertyIndex);
      Assert.Equal(2, nodes[1].MassPropertyIndex);

      Assert.Equal(628.3, nodes[2].X);
      Assert.Equal(-107, nodes[2].Y);
      Assert.Equal(222.8, nodes[2].Z);
      Assert.Equal(NodeRestraint.Custom, nodes[2].NodeRestraint);
      Assert.True(nodes[2].Restraints.SequenceEqual(new AxisDirection6[] { AxisDirection6.X, AxisDirection6.Z }));
      Assert.Equal(45, nodes[2].MeshSize);

      for (int i = 0; i < nodes.Count(); i++)
      {
        Assert.True(new GsaNodeParser(nodes[i]).Gwa(out var gwa));
        Assert.Equal(nodeGwas[i], gwa.First());
      }
    }

    [Fact]
    public void GsaPathSimple()
    {
      var pathGwas = new List<string>()
      {
        "PATH.1\t1\tLeft Lane\tLANE\t1\t1\t-4\t-1\t0.5\t0",
        "PATH.1\t2\tRight Lane\tLANE\t1\t1\t-7\t-4\t0.5\t0",
        "PATH.1\t3\trailway\tTRACK\t2\t1\t-8\t1.434999943\t0.5\t0"
      };
      var paths = new List<GsaPath>();
      foreach (var g in pathGwas)
      {
        var l = new GsaPathParser();
        Assert.True(l.FromGwa(g));
        paths.Add((GsaPath)l.Record);
      }
      Assert.Equal(PathType.LANE, paths[0].Type);
      Assert.Equal(1, paths[0].Group);
      Assert.Equal(1, paths[0].Alignment);
      Assert.Equal(-4, paths[0].Left);
      Assert.Equal(-1, paths[0].Right);
      Assert.Equal(0.5, paths[0].Factor);
      Assert.Equal(0, paths[0].NumMarkedLanes);

      Assert.Equal(PathType.LANE, paths[1].Type);
      Assert.Equal(1, paths[1].Group);
      Assert.Equal(1, paths[1].Alignment);
      Assert.Equal(-7, paths[1].Left);
      Assert.Equal(-4, paths[1].Right);
      Assert.Equal(0.5, paths[1].Factor);
      Assert.Equal(0, paths[1].NumMarkedLanes);

      Assert.Equal(PathType.TRACK, paths[2].Type);
      Assert.Equal(2, paths[2].Group);
      Assert.Equal(1, paths[2].Alignment);
      Assert.Equal(-8, paths[2].Left);
      Assert.Equal(1.434999943, paths[2].Right);
      Assert.Equal(0.5, paths[2].Factor);
      Assert.Equal(0, paths[2].NumMarkedLanes);

      for (int i = 0; i < paths.Count(); i++)
      {
        Assert.True(new GsaPathParser(paths[i]).Gwa(out var gwa));
        Assert.Equal(pathGwas[i], gwa.First());
      }
    }

    [Fact (Skip = "WIP")]
    public void GsaProp2dSimple()
    {
      var supportedProp2dGwa = "PROP_2D.7\t1\tSlab property\tNO_RGB\tSHELL\tGLOBAL\t0\tCONCRETE\t1\t0\t0.3\tCENTROID\t0\t0\t100%\t100%\t100%\t100%";
      var unsupportedProp2dGwa = "PROP_2D.7\t1\tSlab property\tNO_RGB\tCURVED\tGLOBAL\t0\tCONCRETE\t1\t0\t0.3(m) D 0 CAT RLD Ribdeck AL (0.9)\tCENTROID\t0\t0\t100%\t100%\t100%\t100%";

      var p1 = new GsaProp2dParser();
      Assert.True(p1.FromGwa(supportedProp2dGwa));
      var p2 = new GsaProp2dParser();
      Assert.True(p2.FromGwa(unsupportedProp2dGwa));

      Assert.Equal(0.3, ((GsaProp2d)p1.Record).Thickness);
      Assert.Equal(Property2dRefSurface.Centroid, ((GsaProp2d)p1.Record).RefPt);

      Assert.True(p1.Gwa(out var gwa));
      Assert.Equal(supportedProp2dGwa, gwa.First());

      //Assert.True(ModelValidation(supportedProp2dGwa, GsaRecord.GetKeyword<GsaProp2d>(), 1, out var _));
    }

    [Fact]
    public void GsaPropMassSimple()
    {
      var massGwas = new List<string>()
      {
        "PROP_MASS.3\t1\tMass prop. 1\tNO_RGB\t4\t0\t0\t0\t0\t0\t0\tMOD\t100%\t100%\t100%"
      };
      var masses = new List<GsaPropMass>();
      foreach (var g in massGwas)
      {
        var m = new GsaPropMassParser();
        Assert.True(m.FromGwa(g));
        masses.Add((GsaPropMass)m.Record);
      }

      for (int i = 0; i < masses.Count(); i++)
      {
        Assert.True(new GsaPropMassParser(masses[i]).Gwa(out var gwa));
        Assert.Equal(massGwas[i], gwa.First());
      }
    }

    [Fact]
    public void GsaPropSprSimple()
    {
      var propGwas = new List<string>()
      {
        "PROP_SPR.4\t1\tLSPxGeneral\tNO_RGB\tGENERAL\t0\t12\t0\t15\t0\t20\t0\t25\t0\t30\t0\t38\t0.21",
        "PROP_SPR.4\t2\tLSPxAxial\tNO_RGB\tAXIAL\t12\t0.21",
        "PROP_SPR.4\t3\tLSPxTorsional\tNO_RGB\tTORSIONAL\t12\t0.21",
        "PROP_SPR.4\t4\tLSPxCompression\tNO_RGB\tCOMPRESSION\t12\t0.21",
        "PROP_SPR.4\t5\tLSPxTension\tNO_RGB\tTENSION\t12\t0.21",
        "PROP_SPR.4\t6\tLSPxLockup\tNO_RGB\tLOCKUP\t12\t0.21\t0\t0",
        "PROP_SPR.4\t7\tLSPxGap\tNO_RGB\tGAP\t12\t0.21",
        "PROP_SPR.4\t8\tLSPxFriction\tNO_RGB\tFRICTION\t12\t15\t20\t0\t0.21"
      };
      var props = new List<GsaPropSpr>();
      foreach (var g in propGwas)
      {
        var p = new GsaPropSprParser();
        Assert.True(p.FromGwa(g));
        Assert.Equal(0.21, ((GsaPropSpr)p.Record).DampingRatio.Value, 4);
        Assert.Equal(12, ((GsaPropSpr)p.Record).Stiffnesses[((GsaPropSpr)p.Record).Stiffnesses.Keys.First()]);
        props.Add((GsaPropSpr)p.Record);
      }

      Assert.Equal(25, props[0].Stiffnesses[AxisDirection6.XX]);
      Assert.Equal(38, props[0].Stiffnesses[AxisDirection6.ZZ]);

      Assert.Equal(12, props[2].Stiffnesses[AxisDirection6.XX]);

      Assert.Equal(12, props[7].Stiffnesses[AxisDirection6.X]);
      Assert.Equal(15, props[7].Stiffnesses[AxisDirection6.Y]);
      Assert.Equal(20, props[7].Stiffnesses[AxisDirection6.Z]);


      for (int i = 0; i < props.Count(); i++)
      {
        Assert.True(new GsaPropSprParser(props[i]).Gwa(out var gwa));
        Assert.Equal(propGwas[i], gwa.First());
      }
    }

    [Fact]
    public void GsaRigidSimple()
    {
      var rigidGwas = new List<string>()
      {
        "RIGID.3\t\t71\tALL\t73 75 77 79 81 83\t1 2\t0",
        "RIGID.3\t\t71\tXY_PLANE\t73 75 77 79 81 83\t1\t0",
        "RIGID.3\t\t71\tX:XYYZZ-Y:YZZ-YY:YY-ZZ:ZZ\t73 75 77 79 81 83\t2\t0",
      };

      var rigids = new List<GsaRigid>();
      foreach (var g in rigidGwas)
      {
        var l = new GsaRigidParser();
        Assert.True(l.FromGwa(g));
        rigids.Add((GsaRigid)l.Record);
      }

      Assert.Equal(71, rigids[0].PrimaryNode);
      Assert.Equal(RigidConstraintType.ALL, rigids[0].Type);
      Assert.Null(rigids[0].Link);
      Assert.Equal(new List<int>() { 73, 75, 77, 79, 81, 83 }, rigids[0].ConstrainedNodes);
      Assert.Equal(new List<int>() { 1, 2 }, rigids[0].Stage);
      Assert.Equal(0, rigids[0].ParentMember);

      Assert.Equal(71, rigids[1].PrimaryNode);
      Assert.Equal(RigidConstraintType.XY_PLANE, rigids[1].Type);
      Assert.Null(rigids[1].Link);
      Assert.Equal(new List<int>() { 73, 75, 77, 79, 81, 83 }, rigids[1].ConstrainedNodes);
      Assert.Equal(new List<int>() { 1 }, rigids[1].Stage);
      Assert.Equal(0, rigids[1].ParentMember);

      Assert.Equal(71, rigids[2].PrimaryNode);
      Assert.Equal(RigidConstraintType.Custom, rigids[2].Type);
      Assert.Equal(new List<AxisDirection6> { AxisDirection6.X, AxisDirection6.YY, AxisDirection6.ZZ }, rigids[2].Link[AxisDirection6.X]);
      Assert.Equal(new List<AxisDirection6> { AxisDirection6.Y, AxisDirection6.ZZ }, rigids[2].Link[AxisDirection6.Y]);
      Assert.Equal(new List<AxisDirection6> { AxisDirection6.YY }, rigids[2].Link[AxisDirection6.YY]);
      Assert.Equal(new List<AxisDirection6> { AxisDirection6.ZZ }, rigids[2].Link[AxisDirection6.ZZ]);
      Assert.Equal(new List<int>() { 73, 75, 77, 79, 81, 83 }, rigids[2].ConstrainedNodes);
      Assert.Equal(new List<int>() { 2 }, rigids[2].Stage);
      Assert.Equal(0, rigids[1].ParentMember);

      for (int i = 0; i < rigids.Count(); i++)
      {
        Assert.True(new GsaRigidParser(rigids[i]).Gwa(out var gwa));
        Assert.Equal(rigidGwas[i], gwa.First());
      }
    }

    [Fact]
    public void GsaSectionSimple()
    {
      var gwa1 = "SECTION.7\t3\tNO_RGB\tSTD GZ 10 3 3 1.5 1.6 1\t1D_GENERIC\t0\tCENTROID\t0\t0\t0\t1\t0\t0\t0\t0\t1\tSECTION_COMP.4\t\t0\tGENERIC\t0\tSTD GZ 10 3 3 1.5 1.6 1\t0\t0\t0\tY_AXIS\t0\tNONE\t0\t0\t0\tNO_ENVIRON";
      var gwa2 = "SECTION.7\t2\tNO_RGB\t150x150x12EA-BtB\t1D_GENERIC\t0\tCENTROID\t0\t0\t0\t1\t0\t0\t0\t0\t1\tSECTION_COMP.4\t\t1\tSTEEL\t1\tSTD D 150 150 12 12\t0\t0\t0\tNONE\t0\tNONE\t0\tSECTION_STEEL.2\t0\t1\t1\t1\t0.4\tNO_LOCK\tROLLED\tUNDEF\t0\t0\tNO_ENVIRON";
      var gwa3 = "SECTION.7\t7\tNO_RGB\tfgds\t1D_GENERIC\t0\tCENTROID\t0\t0\t0\t1\t0\t0\t0\t0\t1\tSECTION_COMP.4\t\t2\tCONCRETE\t1\tSTD CH 99 60 8 9\t0\t0\t0\tNONE\t0\tSIMPLE\t0\tSECTION_CONC.6\t1\tNO_SLAB\t89.99999998\t0.025\t0\tSECTION_LINK.3\t0\t0\tDISCRETE\tRECT\t0\t\tSECTION_COVER.3\tUNIFORM\t0\t0\tNO_SMEAR\tSECTION_TMPL.4\tUNDEF\t0\t0\t0\t0\t0\t0\tNO_ENVIRON";
      var gwaExp = "SECTION.7\t2\tNO_RGB\tEXP 1 2 3 4 5 6\t1D_GENERIC\t0\tCENTROID\t0\t0\t0\t1\t0\t0\t0\t0\t1\tSECTION_COMP.4\t\t0\tGENERIC\t0\tEXP 1 2 3 4 5 6\t0\t0\t0\tNONE\t0\tNONE\t0\t0\t0\tNO_ENVIRON";

      var gsaSection1 = new GsaSectionParser();
      gsaSection1.FromGwa(gwa1);
      gsaSection1.Gwa(out var gwaOut1);
      Assert.True(gwa1.Equals(gwaOut1.First(), StringComparison.InvariantCulture));

      var gsaSection2 = new GsaSectionParser();
      gsaSection2.FromGwa(gwa2);
      gsaSection2.Gwa(out var gwaOut2);
      Assert.True(gwa2.Equals(gwaOut2.First(), StringComparison.InvariantCulture));

      var gsaSection3 = new GsaSectionParser();
      gsaSection3.FromGwa(gwa3);
      gsaSection3.Gwa(out var gwaOut3);
      Assert.True(gwa3.Equals(gwaOut3.First(), StringComparison.InvariantCulture));

      var gsaSectionExp = new GsaSectionParser();
      gsaSectionExp.FromGwa(gwaExp);
      gsaSectionExp.Gwa(out var gwaOutExp);
      Assert.True(gwaExp.Equals(gwaOutExp.First(), StringComparison.InvariantCulture));
    }

    [Fact]
    public void GsaSectionSimple2()
    {
      var sectionGwas = new List<string>()
      {
        //Catalogue
        "SECTION.7\t1\tNO_RGB\tCatalogue Section\t1D_GENERIC\t0\tCENTROID\t0\t0\t0\t1\t0\t0\t0\t0\t1\tSECTION_COMP.4\t\t0\tSTEEL\t1\tCAT A-UB 610UB125 19981201\t0\t0\t0\tNONE\t0\tNONE\t0\tSECTION_STEEL.2\t0\t1\t1\t1\t0.4\tNO_LOCK\tROLLED\tUNDEF\t0\t0\tNO_ENVIRON",
        //Perimeter
        "SECTION.7\t2\tNO_RGB\tPerimeter Section\t1D_GENERIC\t0\tCENTROID\t0\t0\t0\t1\t0\t0\t0\t0\t1\tSECTION_COMP.4\t\t0\tSTEEL\t1\tGEO P M(-50|-50) L(50|-50) L(50|50) L(-50|50) M(-40|-40) L(40|-40) L(40|40) L(-40|40)\t0\t0\t0\tNONE\t0\tNONE\t0\tSECTION_STEEL.2\t0\t1\t1\t1\t0.4\tNO_LOCK\tUNDEF\tUNDEF\t0\t0\tNO_ENVIRON",
        "SECTION.7\t3\tNO_RGB\tLine Segment Section\t1D_GENERIC\t0\tCENTROID\t0\t0\t0\t1\t0\t0\t0\t0\t1\tSECTION_COMP.4\t\t0\tSTEEL\t1\tGEO L(mm) T(10) M(-45|-45) L(45|-45) L(45|45) L(-45|45) L(-45|-45)\t0\t0\t0\tNONE\t0\tNONE\t0\tSECTION_STEEL.2\t0\t1\t1\t1\t0.4\tNO_LOCK\tUNDEF\tUNDEF\t0\t0\tNO_ENVIRON",
        //Standard
        "SECTION.7\t4\tNO_RGB\tSolid Rectange\t1D_GENERIC\t0\tCENTROID\t0\t0\t0\t1\t0\t0\t0\t0\t1\tSECTION_COMP.4\t\t0\tSTEEL\t1\tSTD R 100 50\t0\t0\t0\tNONE\t0\tNONE\t0\tSECTION_STEEL.2\t0\t1\t1\t1\t0.4\tNO_LOCK\tUNDEF\tUNDEF\t0\t0\tNO_ENVIRON",
        "SECTION.7\t5\tNO_RGB\tHollow Rectangle\t1D_GENERIC\t0\tCENTROID\t0\t0\t0\t1\t0\t0\t0\t0\t1\tSECTION_COMP.4\t\t0\tSTEEL\t1\tSTD RHS 100 50 5 10\t0\t0\t0\tNONE\t0\tNONE\t0\tSECTION_STEEL.2\t0\t1\t1\t1\t0.4\tNO_LOCK\tUNDEF\tUNDEF\t0\t0\tNO_ENVIRON",
        "SECTION.7\t6\tNO_RGB\tSolid Circle\t1D_GENERIC\t0\tCENTROID\t0\t0\t0\t1\t0\t0\t0\t0\t1\tSECTION_COMP.4\t\t0\tSTEEL\t1\tSTD C 100\t0\t0\t0\tNONE\t0\tNONE\t0\tSECTION_STEEL.2\t0\t1\t1\t1\t0.4\tNO_LOCK\tUNDEF\tUNDEF\t0\t0\tNO_ENVIRON",
        "SECTION.7\t7\tNO_RGB\tHollow Circle\t1D_GENERIC\t0\tCENTROID\t0\t0\t0\t1\t0\t0\t0\t0\t1\tSECTION_COMP.4\t\t0\tSTEEL\t1\tSTD CHS 100 5\t0\t0\t0\tNONE\t0\tNONE\t0\tSECTION_STEEL.2\t0\t1\t1\t1\t0.4\tNO_LOCK\tUNDEF\tUNDEF\t0\t0\tNO_ENVIRON",
        "SECTION.7\t8\tNO_RGB\tI Section\t1D_GENERIC\t0\tCENTROID\t0\t0\t0\t1\t0\t0\t0\t0\t1\tSECTION_COMP.4\t\t0\tSTEEL\t1\tSTD I 100 50 5 10\t0\t0\t0\tNONE\t0\tNONE\t0\tSECTION_STEEL.2\t0\t1\t1\t1\t0.4\tNO_LOCK\tUNDEF\tUNDEF\t0\t0\tNO_ENVIRON",
        "SECTION.7\t9\tNO_RGB\tT Section\t1D_GENERIC\t0\tCENTROID\t0\t0\t0\t1\t0\t0\t0\t0\t1\tSECTION_COMP.4\t\t0\tSTEEL\t1\tSTD T 100 50 5 10\t0\t0\t0\tNONE\t0\tNONE\t0\tSECTION_STEEL.2\t0\t1\t1\t1\t0.4\tNO_LOCK\tUNDEF\tUNDEF\t0\t0\tNO_ENVIRON",
        "SECTION.7\t10\tNO_RGB\tChannel Section\t1D_GENERIC\t0\tCENTROID\t0\t0\t0\t1\t0\t0\t0\t0\t1\tSECTION_COMP.4\t\t0\tSTEEL\t1\tSTD CH 100 50 5 10\t0\t0\t0\tNONE\t0\tNONE\t0\tSECTION_STEEL.2\t0\t1\t1\t1\t0.4\tNO_LOCK\tUNDEF\tUNDEF\t0\t0\tNO_ENVIRON",
        "SECTION.7\t11\tNO_RGB\tAngle Section\t1D_GENERIC\t0\tCENTROID\t0\t0\t0\t1\t0\t0\t0\t0\t1\tSECTION_COMP.4\t\t0\tSTEEL\t1\tSTD A 100 50 5 10\t0\t0\t0\tNONE\t0\tNONE\t0\tSECTION_STEEL.2\t0\t1\t1\t1\t0.4\tNO_LOCK\tUNDEF\tUNDEF\t0\t0\tNO_ENVIRON",
        "SECTION.7\t12\tNO_RGB\tTaper Section\t1D_GENERIC\t0\tCENTROID\t0\t0\t0\t1\t0\t0\t0\t0\t1\tSECTION_COMP.4\t\t0\tSTEEL\t1\tSTD TR 100 50 20\t0\t0\t0\tNONE\t0\tNONE\t0\tSECTION_STEEL.2\t0\t1\t1\t1\t0.4\tNO_LOCK\tUNDEF\tUNDEF\t0\t0\tNO_ENVIRON",
        "SECTION.7\t13\tNO_RGB\tEllipse Section\t1D_GENERIC\t0\tCENTROID\t0\t0\t0\t1\t0\t0\t0\t0\t1\tSECTION_COMP.4\t\t0\tSTEEL\t1\tSTD E 100 50 2\t0\t0\t0\tNONE\t0\tNONE\t0\tSECTION_STEEL.2\t0\t1\t1\t1\t0.4\tNO_LOCK\tUNDEF\tUNDEF\t0\t0\tNO_ENVIRON",
        "SECTION.7\t14\tNO_RGB\tOval Section\t1D_GENERIC\t0\tCENTROID\t0\t0\t0\t1\t0\t0\t0\t0\t1\tSECTION_COMP.4\t\t0\tSTEEL\t1\tSTD OVAL 100 50 5\t0\t0\t0\tNONE\t0\tNONE\t0\tSECTION_STEEL.2\t0\t1\t1\t1\t0.4\tNO_LOCK\tUNDEF\tUNDEF\t0\t0\tNO_ENVIRON",
        "SECTION.7\t15\tNO_RGB\tCruciform Section\t1D_GENERIC\t0\tCENTROID\t0\t0\t0\t1\t0\t0\t0\t0\t1\tSECTION_COMP.4\t\t0\tSTEEL\t1\tSTD X 100 50 5 10\t0\t0\t0\tNONE\t0\tNONE\t0\tSECTION_STEEL.2\t0\t1\t1\t1\t0.4\tNO_LOCK\tUNDEF\tUNDEF\t0\t0\tNO_ENVIRON",
        "SECTION.7\t16\tNO_RGB\tGeneral I Section\t1D_GENERIC\t0\tCENTROID\t0\t0\t0\t1\t0\t0\t0\t0\t1\tSECTION_COMP.4\t\t0\tSTEEL\t1\tSTD GI 100 50 20 2 10 5\t0\t0\t0\tNONE\t0\tNONE\t0\tSECTION_STEEL.2\t0\t1\t1\t1\t0.4\tNO_LOCK\tUNDEF\tUNDEF\t0\t0\tNO_ENVIRON",
        "SECTION.7\t17\tNO_RGB\tGeneral Z Section\t1D_GENERIC\t0\tCENTROID\t0\t0\t0\t1\t0\t0\t0\t0\t1\tSECTION_COMP.4\t\t0\tSTEEL\t1\tSTD GZ 100 50 20 10 5 2\t0\t0\t0\tNONE\t0\tNONE\t0\tSECTION_STEEL.2\t0\t1\t1\t1\t0.4\tNO_LOCK\tUNDEF\tUNDEF\t0\t0\tNO_ENVIRON",
        "SECTION.7\t18\tNO_RGB\tGeneral Channel Section\t1D_GENERIC\t0\tCENTROID\t0\t0\t0\t1\t0\t0\t0\t0\t1\tSECTION_COMP.4\t\t0\tSTEEL\t1\tSTD GC 100 50 20 5\t0\t0\t0\tNONE\t0\tNONE\t0\tSECTION_STEEL.2\t0\t1\t1\t1\t0.4\tNO_LOCK\tUNDEF\tUNDEF\t0\t0\tNO_ENVIRON",
        "SECTION.7\t19\tNO_RGB\tTaper T Section\t1D_GENERIC\t0\tCENTROID\t0\t0\t0\t1\t0\t0\t0\t0\t1\tSECTION_COMP.4\t\t0\tSTEEL\t1\tSTD TT 100 50 20 5 10\t0\t0\t0\tNONE\t0\tNONE\t0\tSECTION_STEEL.2\t0\t1\t1\t1\t0.4\tNO_LOCK\tUNDEF\tUNDEF\t0\t0\tNO_ENVIRON",
        "SECTION.7\t20\tNO_RGB\tTaper Angle Section\t1D_GENERIC\t0\tCENTROID\t0\t0\t0\t1\t0\t0\t0\t0\t1\tSECTION_COMP.4\t\t0\tSTEEL\t1\tSTD TA 100 50 5 20 10\t0\t0\t0\tNONE\t0\tNONE\t0\tSECTION_STEEL.2\t0\t1\t1\t1\t0.4\tNO_LOCK\tUNDEF\tUNDEF\t0\t0\tNO_ENVIRON",
        "SECTION.7\t21\tNO_RGB\tTaper I Section\t1D_GENERIC\t0\tCENTROID\t0\t0\t0\t1\t0\t0\t0\t0\t1\tSECTION_COMP.4\t\t0\tSTEEL\t1\tSTD TI 100 50 20 4 2 10 5\t0\t0\t0\tNONE\t0\tNONE\t0\tSECTION_STEEL.2\t0\t1\t1\t1\t0.4\tNO_LOCK\tUNDEF\tUNDEF\t0\t0\tNO_ENVIRON",
        "SECTION.7\t22\tNO_RGB\tRecto-Circular Section\t1D_GENERIC\t0\tCENTROID\t0\t0\t0\t1\t0\t0\t0\t0\t1\tSECTION_COMP.4\t\t0\tSTEEL\t1\tSTD RC 100 50\t0\t0\t0\tNONE\t0\tNONE\t0\tSECTION_STEEL.2\t0\t1\t1\t1\t0.4\tNO_LOCK\tUNDEF\tUNDEF\t0\t0\tNO_ENVIRON",
        "SECTION.7\t23\tNO_RGB\tRecto-Ellipse Section\t1D_GENERIC\t0\tCENTROID\t0\t0\t0\t1\t0\t0\t0\t0\t1\tSECTION_COMP.4\t\t0\tSTEEL\t1\tSTD RE 100 50 80 30 2\t0\t0\t0\tNONE\t0\tNONE\t0\tSECTION_STEEL.2\t0\t1\t1\t1\t0.4\tNO_LOCK\tUNDEF\tUNDEF\t0\t0\tNO_ENVIRON",
        "SECTION.7\t24\tNO_RGB\tSecant Pile Section\t1D_GENERIC\t0\tCENTROID\t0\t0\t0\t1\t0\t0\t0\t0\t1\tSECTION_COMP.4\t\t0\tCONCRETE\t1\tSTD SPW 100 50 2\t0\t0\t0\tNONE\t0\tNONE\t0\tSECTION_CONC.6\t1\tNO_SLAB\t1.570796327\t0.025\t0\tSECTION_LINK.3\t0\t0\tDISCRETE\tRECT\t0\t\tSECTION_COVER.3\tUNIFORM\t0\t0\tNO_SMEAR\tSECTION_TMPL.4\tUNDEF\t0\t0\t0\t0\t0\t0\tNO_ENVIRON",
        "SECTION.7\t25\tNO_RGB\tCastellated Section\t1D_GENERIC\t0\tCENTROID\t0\t0\t0\t1\t0\t0\t0\t0\t1\tSECTION_COMP.4\t\t0\tSTEEL\t1\tSTD CB 100 50 5 10 60 200\t0\t0\t0\tNONE\t0\tNONE\t0\tSECTION_STEEL.2\t0\t1\t1\t1\t0.4\tNO_LOCK\tUNDEF\tUNDEF\t0\t0\tNO_ENVIRON",
        "SECTION.7\t26\tNO_RGB\tAsymmetric Cellular Section\t1D_GENERIC\t0\tCENTROID\t0\t0\t0\t1\t0\t0\t0\t0\t1\tSECTION_COMP.4\t\t0\tSTEEL\t1\tSTD ACB 100 50 5 10 80 40 5 8 60 200\t0\t0\t0\tNONE\t0\tNONE\t0\tSECTION_STEEL.2\t0\t1\t1\t1\t0.4\tNO_LOCK\tUNDEF\tUNDEF\t0\t0\tNO_ENVIRON",
        "SECTION.7\t27\tNO_RGB\tSheet Pile Section\t1D_GENERIC\t0\tCENTROID\t0\t0\t0\t1\t0\t0\t0\t0\t1\tSECTION_COMP.4\t\t0\tSTEEL\t1\tSTD SHT 100 200 40 50 10 5\t0\t0\t0\tNONE\t0\tNONE\t0\tSECTION_STEEL.2\t0\t1\t1\t1\t0.4\tNO_LOCK\tUNDEF\tUNDEF\t0\t0\tNO_ENVIRON",
        //Explicit
        "SECTION.7\t28\tNO_RGB\tExplicit Section\t1D_GENERIC\t0\tCENTROID\t0\t0\t0\t1\t0\t0\t0\t0\t1\tSECTION_COMP.4\t\t0\tSTEEL\t1\tEXP 10 11 12 13 14 15\t0\t0\t0\tNONE\t0\tNONE\t0\tSECTION_STEEL.2\t0\t1\t1\t1\t0.4\tNO_LOCK\tUNDEF\tUNDEF\t0\t0\tNO_ENVIRON"
      };

      var sections = new List<GsaSection>();
      foreach (var g in sectionGwas)
      {
        var l = new GsaSectionParser();
        Assert.True(l.FromGwa(g));
        sections.Add((GsaSection)l.Record);
      }

      //Checks
      #region Catalogue Section
      //SECTION.7
      Assert.Equal("Catalogue Section", sections[0].Name);
      Assert.Equal(Colour.NO_RGB, sections[0].Colour);
      Assert.Equal(Section1dType.Generic, sections[0].Type);
      Assert.Null(sections[0].PoolIndex);
      Assert.Equal(ReferencePoint.Centroid, sections[0].ReferencePoint);
      Assert.Equal(0, sections[0].RefY);
      Assert.Equal(0, sections[0].RefZ);
      Assert.Equal(0, sections[0].Mass);
      Assert.Equal(1, sections[0].Fraction);
      Assert.Equal(0, sections[0].Cost);
      Assert.Equal(0, sections[0].Left);
      Assert.Equal(0, sections[0].Right);
      Assert.Equal(0, sections[0].Slab);

      //SECTION_COMP.4
      var gsaSectionComp = (SectionComp)sections[0].Components[0];
      Assert.Equal(1, gsaSectionComp.MaterialIndex);
      Assert.Equal(Section1dMaterialType.STEEL, gsaSectionComp.MaterialType);
      Assert.Null(gsaSectionComp.MatAnalIndex);
      Assert.Equal(0, gsaSectionComp.OffsetY);
      Assert.Equal(0, gsaSectionComp.OffsetZ);
      Assert.Equal(0, gsaSectionComp.Rotation);
      Assert.Equal(ComponentReflection.NONE, gsaSectionComp.Reflect);
      Assert.Equal(0, gsaSectionComp.Pool);
      Assert.Equal(Section1dTaperType.NONE, gsaSectionComp.TaperType);
      Assert.Equal(0, gsaSectionComp.TaperPos);
      Assert.Equal(Section1dProfileGroup.Catalogue, gsaSectionComp.ProfileGroup);

      //Profile
      var gsaCatalogue = (ProfileDetailsCatalogue)gsaSectionComp.ProfileDetails;
      Assert.Equal(Section1dProfileGroup.Catalogue, gsaCatalogue.Group);
      Assert.Equal("CAT A-UB 610UB125 19981201", gsaCatalogue.Profile);

      //SECTION_STEEL
      var gsaSectionSteel = (SectionSteel)sections[0].Components[1];
      Assert.Null(gsaSectionSteel.GradeIndex);
      Assert.Equal(1, gsaSectionSteel.PlasElas);
      Assert.Equal(1, gsaSectionSteel.NetGross);
      Assert.Equal(1, gsaSectionSteel.Exposed);
      Assert.Equal(0.4, gsaSectionSteel.Beta);
      Assert.Equal(SectionSteelSectionType.HotRolled, gsaSectionSteel.Type);
      Assert.Equal(SectionSteelPlateType.Undefined, gsaSectionSteel.Plate);
      Assert.False(gsaSectionSteel.Locked);
      #endregion
      #region Perimeter Section
      #region Perimeter Section
      //SECTION.7
      Assert.Equal("Perimeter Section", sections[1].Name);
      Assert.Equal(Colour.NO_RGB, sections[1].Colour);
      Assert.Equal(Section1dType.Generic, sections[1].Type);
      Assert.Null(sections[1].PoolIndex);
      Assert.Equal(ReferencePoint.Centroid, sections[1].ReferencePoint);
      Assert.Equal(0, sections[1].RefY);
      Assert.Equal(0, sections[1].RefZ);
      Assert.Equal(0, sections[1].Mass);
      Assert.Equal(1, sections[1].Fraction);
      Assert.Equal(0, sections[1].Cost);
      Assert.Equal(0, sections[1].Left);
      Assert.Equal(0, sections[1].Right);
      Assert.Equal(0, sections[1].Slab);

      //SECTION_COMP.4
      gsaSectionComp = (SectionComp)sections[1].Components[0];
      Assert.Equal(1, gsaSectionComp.MaterialIndex);
      Assert.Equal(Section1dMaterialType.STEEL, gsaSectionComp.MaterialType);
      Assert.Null(gsaSectionComp.MatAnalIndex);
      Assert.Equal(0, gsaSectionComp.OffsetY);
      Assert.Equal(0, gsaSectionComp.OffsetZ);
      Assert.Equal(0, gsaSectionComp.Rotation);
      Assert.Equal(ComponentReflection.NONE, gsaSectionComp.Reflect);
      Assert.Equal(0, gsaSectionComp.Pool);
      Assert.Equal(Section1dTaperType.NONE, gsaSectionComp.TaperType);
      Assert.Equal(0, gsaSectionComp.TaperPos);
      Assert.Equal(Section1dProfileGroup.Perimeter, gsaSectionComp.ProfileGroup);

      //Profile
      var gsaPerimeter = (ProfileDetailsPerimeter)gsaSectionComp.ProfileDetails;
      Assert.Equal(Section1dProfileGroup.Perimeter, gsaPerimeter.Group);
      Assert.Equal("P", gsaPerimeter.Type);
      Assert.Equal(new List<string>() { "M", "L", "L", "L", "M", "L", "L", "L" }, gsaPerimeter.Actions);
      Assert.Equal(new List<double?>() { -50, 50, 50, -50, -40, 40, 40, -40 }, gsaPerimeter.Y);
      Assert.Equal(new List<double?>() { -50, -50, 50, 50, -40, -40, 40, 40}, gsaPerimeter.Z);

      //SECTION_STEEL
      gsaSectionSteel = (SectionSteel)sections[1].Components[1];
      Assert.Null(gsaSectionSteel.GradeIndex);
      Assert.Equal(1, gsaSectionSteel.PlasElas);
      Assert.Equal(1, gsaSectionSteel.NetGross);
      Assert.Equal(1, gsaSectionSteel.Exposed);
      Assert.Equal(0.4, gsaSectionSteel.Beta);
      Assert.Equal(SectionSteelSectionType.Undefined, gsaSectionSteel.Type);
      Assert.Equal(SectionSteelPlateType.Undefined, gsaSectionSteel.Plate);
      Assert.False(gsaSectionSteel.Locked);
      #endregion
      #region Line Segment Section
      //SECTION.7
      Assert.Equal("Line Segment Section", sections[2].Name);
      Assert.Equal(Colour.NO_RGB, sections[2].Colour);
      Assert.Equal(Section1dType.Generic, sections[2].Type);
      Assert.Null(sections[2].PoolIndex);
      Assert.Equal(ReferencePoint.Centroid, sections[2].ReferencePoint);
      Assert.Equal(0, sections[2].RefY);
      Assert.Equal(0, sections[2].RefZ);
      Assert.Equal(0, sections[2].Mass);
      Assert.Equal(1, sections[2].Fraction);
      Assert.Equal(0, sections[2].Cost);
      Assert.Equal(0, sections[2].Left);
      Assert.Equal(0, sections[2].Right);
      Assert.Equal(0, sections[2].Slab);

      //SECTION_COMP.4
      gsaSectionComp = (SectionComp)sections[2].Components[0];
      Assert.Equal(1, gsaSectionComp.MaterialIndex);
      Assert.Equal(Section1dMaterialType.STEEL, gsaSectionComp.MaterialType);
      Assert.Null(gsaSectionComp.MatAnalIndex);
      Assert.Equal(0, gsaSectionComp.OffsetY);
      Assert.Equal(0, gsaSectionComp.OffsetZ);
      Assert.Equal(0, gsaSectionComp.Rotation);
      Assert.Equal(ComponentReflection.NONE, gsaSectionComp.Reflect);
      Assert.Equal(0, gsaSectionComp.Pool);
      Assert.Equal(Section1dTaperType.NONE, gsaSectionComp.TaperType);
      Assert.Equal(0, gsaSectionComp.TaperPos);
      Assert.Equal(Section1dProfileGroup.Perimeter, gsaSectionComp.ProfileGroup);

      //Profile
      var gsaLineSgement = (ProfileDetailsPerimeter)gsaSectionComp.ProfileDetails;
      Assert.Equal(Section1dProfileGroup.Perimeter, gsaLineSgement.Group);
      Assert.Equal("L(mm)", gsaLineSgement.Type);
      Assert.Equal(new List<string>() { "T", "M", "L", "L", "L", "L" }, gsaLineSgement.Actions);
      Assert.Equal(new List<double?>() { 10, -45, 45, 45, -45, -45 }, gsaLineSgement.Y);
      Assert.Equal(new List<double?>() { null, -45, -45, 45, 45, -45 }, gsaLineSgement.Z);

      //SECTION_STEEL
      gsaSectionSteel = (SectionSteel)sections[2].Components[1];
      Assert.Null(gsaSectionSteel.GradeIndex);
      Assert.Equal(1, gsaSectionSteel.PlasElas);
      Assert.Equal(1, gsaSectionSteel.NetGross);
      Assert.Equal(1, gsaSectionSteel.Exposed);
      Assert.Equal(0.4, gsaSectionSteel.Beta);
      Assert.Equal(SectionSteelSectionType.Undefined, gsaSectionSteel.Type);
      Assert.Equal(SectionSteelPlateType.Undefined, gsaSectionSteel.Plate);
      Assert.False(gsaSectionSteel.Locked);
      #endregion
      #endregion
      #region Standard Section
      #region Solid Rectange
      //SECTION.7
      Assert.Equal("Solid Rectange", sections[3].Name);
      Assert.Equal(Colour.NO_RGB, sections[3].Colour);
      Assert.Equal(Section1dType.Generic, sections[3].Type);
      Assert.Null(sections[3].PoolIndex);
      Assert.Equal(ReferencePoint.Centroid, sections[3].ReferencePoint);
      Assert.Equal(0, sections[3].RefY);
      Assert.Equal(0, sections[3].RefZ);
      Assert.Equal(0, sections[3].Mass);
      Assert.Equal(1, sections[3].Fraction);
      Assert.Equal(0, sections[3].Cost);
      Assert.Equal(0, sections[3].Left);
      Assert.Equal(0, sections[3].Right);
      Assert.Equal(0, sections[3].Slab);

      //SECTION_COMP.4
      gsaSectionComp = (SectionComp)sections[3].Components[0];
      Assert.Equal(1, gsaSectionComp.MaterialIndex);
      Assert.Equal(Section1dMaterialType.STEEL, gsaSectionComp.MaterialType);
      Assert.Null(gsaSectionComp.MatAnalIndex);
      Assert.Equal(0, gsaSectionComp.OffsetY);
      Assert.Equal(0, gsaSectionComp.OffsetZ);
      Assert.Equal(0, gsaSectionComp.Rotation);
      Assert.Equal(ComponentReflection.NONE, gsaSectionComp.Reflect);
      Assert.Equal(0, gsaSectionComp.Pool);
      Assert.Equal(Section1dTaperType.NONE, gsaSectionComp.TaperType);
      Assert.Equal(0, gsaSectionComp.TaperPos);
      Assert.Equal(Section1dProfileGroup.Standard, gsaSectionComp.ProfileGroup);

      //Profile
      var gsaRectanglular = (ProfileDetailsRectangular)gsaSectionComp.ProfileDetails;
      Assert.Equal(Section1dProfileGroup.Standard, gsaRectanglular.Group);
      Assert.Equal(50, gsaRectanglular.b);
      Assert.Equal(100, gsaRectanglular.d);

      //SECTION_STEEL
      gsaSectionSteel = (SectionSteel)sections[3].Components[1];
      Assert.Null(gsaSectionSteel.GradeIndex);
      Assert.Equal(1, gsaSectionSteel.PlasElas);
      Assert.Equal(1, gsaSectionSteel.NetGross);
      Assert.Equal(1, gsaSectionSteel.Exposed);
      Assert.Equal(0.4, gsaSectionSteel.Beta);
      Assert.Equal(SectionSteelSectionType.Undefined, gsaSectionSteel.Type);
      Assert.Equal(SectionSteelPlateType.Undefined, gsaSectionSteel.Plate);
      Assert.False(gsaSectionSteel.Locked);
      #endregion
      #region Hollow Rectangle
      //SECTION.7
      Assert.Equal("Hollow Rectangle", sections[4].Name);
      Assert.Equal(Colour.NO_RGB, sections[4].Colour);
      Assert.Equal(Section1dType.Generic, sections[4].Type);
      Assert.Null(sections[4].PoolIndex);
      Assert.Equal(ReferencePoint.Centroid, sections[4].ReferencePoint);
      Assert.Equal(0, sections[4].RefY);
      Assert.Equal(0, sections[4].RefZ);
      Assert.Equal(0, sections[4].Mass);
      Assert.Equal(1, sections[4].Fraction);
      Assert.Equal(0, sections[4].Cost);
      Assert.Equal(0, sections[4].Left);
      Assert.Equal(0, sections[4].Right);
      Assert.Equal(0, sections[4].Slab);

      //SECTION_COMP.4
      gsaSectionComp = (SectionComp)sections[4].Components[0];
      Assert.Equal(1, gsaSectionComp.MaterialIndex);
      Assert.Equal(Section1dMaterialType.STEEL, gsaSectionComp.MaterialType);
      Assert.Null(gsaSectionComp.MatAnalIndex);
      Assert.Equal(0, gsaSectionComp.OffsetY);
      Assert.Equal(0, gsaSectionComp.OffsetZ);
      Assert.Equal(0, gsaSectionComp.Rotation);
      Assert.Equal(ComponentReflection.NONE, gsaSectionComp.Reflect);
      Assert.Equal(0, gsaSectionComp.Pool);
      Assert.Equal(Section1dTaperType.NONE, gsaSectionComp.TaperType);
      Assert.Equal(0, gsaSectionComp.TaperPos);
      Assert.Equal(Section1dProfileGroup.Standard, gsaSectionComp.ProfileGroup);

      //Profile
      var gsaRectangularHollow = (ProfileDetailsTwoThickness)gsaSectionComp.ProfileDetails;
      Assert.Equal(Section1dProfileGroup.Standard, gsaRectangularHollow.Group);
      Assert.Equal(50, gsaRectangularHollow.b);
      Assert.Equal(100, gsaRectangularHollow.d);
      Assert.Equal(10, gsaRectangularHollow.tf);
      Assert.Equal(5, gsaRectangularHollow.tw);

      //SECTION_STEEL
      gsaSectionSteel = (SectionSteel)sections[4].Components[1];
      Assert.Null(gsaSectionSteel.GradeIndex);
      Assert.Equal(1, gsaSectionSteel.PlasElas);
      Assert.Equal(1, gsaSectionSteel.NetGross);
      Assert.Equal(1, gsaSectionSteel.Exposed);
      Assert.Equal(0.4, gsaSectionSteel.Beta);
      Assert.Equal(SectionSteelSectionType.Undefined, gsaSectionSteel.Type);
      Assert.Equal(SectionSteelPlateType.Undefined, gsaSectionSteel.Plate);
      Assert.False(gsaSectionSteel.Locked);
      #endregion
      #region Solid Circle
      //SECTION.7
      Assert.Equal("Solid Circle", sections[5].Name);
      Assert.Equal(Colour.NO_RGB, sections[5].Colour);
      Assert.Equal(Section1dType.Generic, sections[5].Type);
      Assert.Null(sections[5].PoolIndex);
      Assert.Equal(ReferencePoint.Centroid, sections[5].ReferencePoint);
      Assert.Equal(0, sections[5].RefY);
      Assert.Equal(0, sections[5].RefZ);
      Assert.Equal(0, sections[5].Mass);
      Assert.Equal(1, sections[5].Fraction);
      Assert.Equal(0, sections[5].Cost);
      Assert.Equal(0, sections[5].Left);
      Assert.Equal(0, sections[5].Right);
      Assert.Equal(0, sections[5].Slab);

      //SECTION_COMP.4
      gsaSectionComp = (SectionComp)sections[5].Components[0];
      Assert.Equal(1, gsaSectionComp.MaterialIndex);
      Assert.Equal(Section1dMaterialType.STEEL, gsaSectionComp.MaterialType);
      Assert.Null(gsaSectionComp.MatAnalIndex);
      Assert.Equal(0, gsaSectionComp.OffsetY);
      Assert.Equal(0, gsaSectionComp.OffsetZ);
      Assert.Equal(0, gsaSectionComp.Rotation);
      Assert.Equal(ComponentReflection.NONE, gsaSectionComp.Reflect);
      Assert.Equal(0, gsaSectionComp.Pool);
      Assert.Equal(Section1dTaperType.NONE, gsaSectionComp.TaperType);
      Assert.Equal(0, gsaSectionComp.TaperPos);
      Assert.Equal(Section1dProfileGroup.Standard, gsaSectionComp.ProfileGroup);

      //Profile
      var gsaCircular = (ProfileDetailsCircular)gsaSectionComp.ProfileDetails;
      Assert.Equal(Section1dProfileGroup.Standard, gsaCircular.Group);
      Assert.Equal(100, gsaCircular.d);

      //SECTION_STEEL
      gsaSectionSteel = (SectionSteel)sections[5].Components[1];
      Assert.Null(gsaSectionSteel.GradeIndex);
      Assert.Equal(1, gsaSectionSteel.PlasElas);
      Assert.Equal(1, gsaSectionSteel.NetGross);
      Assert.Equal(1, gsaSectionSteel.Exposed);
      Assert.Equal(0.4, gsaSectionSteel.Beta);
      Assert.Equal(SectionSteelSectionType.Undefined, gsaSectionSteel.Type);
      Assert.Equal(SectionSteelPlateType.Undefined, gsaSectionSteel.Plate);
      Assert.False(gsaSectionSteel.Locked);
      #endregion
      #region Hollow Circle
      //SECTION.7
      Assert.Equal("Hollow Circle", sections[6].Name);
      Assert.Equal(Colour.NO_RGB, sections[6].Colour);
      Assert.Equal(Section1dType.Generic, sections[6].Type);
      Assert.Null(sections[6].PoolIndex);
      Assert.Equal(ReferencePoint.Centroid, sections[6].ReferencePoint);
      Assert.Equal(0, sections[6].RefY);
      Assert.Equal(0, sections[6].RefZ);
      Assert.Equal(0, sections[6].Mass);
      Assert.Equal(1, sections[6].Fraction);
      Assert.Equal(0, sections[6].Cost);
      Assert.Equal(0, sections[6].Left);
      Assert.Equal(0, sections[6].Right);
      Assert.Equal(0, sections[6].Slab);

      //SECTION_COMP.4
      gsaSectionComp = (SectionComp)sections[6].Components[0];
      Assert.Equal(1, gsaSectionComp.MaterialIndex);
      Assert.Equal(Section1dMaterialType.STEEL, gsaSectionComp.MaterialType);
      Assert.Null(gsaSectionComp.MatAnalIndex);
      Assert.Equal(0, gsaSectionComp.OffsetY);
      Assert.Equal(0, gsaSectionComp.OffsetZ);
      Assert.Equal(0, gsaSectionComp.Rotation);
      Assert.Equal(ComponentReflection.NONE, gsaSectionComp.Reflect);
      Assert.Equal(0, gsaSectionComp.Pool);
      Assert.Equal(Section1dTaperType.NONE, gsaSectionComp.TaperType);
      Assert.Equal(0, gsaSectionComp.TaperPos);
      Assert.Equal(Section1dProfileGroup.Standard, gsaSectionComp.ProfileGroup);

      //Profile
      var gsaCircularHollow = (ProfileDetailsCircularHollow)gsaSectionComp.ProfileDetails;
      Assert.Equal(Section1dProfileGroup.Standard, gsaCircularHollow.Group);
      Assert.Equal(100, gsaCircularHollow.d);
      Assert.Equal(5, gsaCircularHollow.t);

      //SECTION_STEEL
      gsaSectionSteel = (SectionSteel)sections[6].Components[1];
      Assert.Null(gsaSectionSteel.GradeIndex);
      Assert.Equal(1, gsaSectionSteel.PlasElas);
      Assert.Equal(1, gsaSectionSteel.NetGross);
      Assert.Equal(1, gsaSectionSteel.Exposed);
      Assert.Equal(0.4, gsaSectionSteel.Beta);
      Assert.Equal(SectionSteelSectionType.Undefined, gsaSectionSteel.Type);
      Assert.Equal(SectionSteelPlateType.Undefined, gsaSectionSteel.Plate);
      Assert.False(gsaSectionSteel.Locked);
      #endregion
      #region I Section
      //SECTION.7
      Assert.Equal("I Section", sections[7].Name);
      Assert.Equal(Colour.NO_RGB, sections[7].Colour);
      Assert.Equal(Section1dType.Generic, sections[7].Type);
      Assert.Null(sections[7].PoolIndex);
      Assert.Equal(ReferencePoint.Centroid, sections[7].ReferencePoint);
      Assert.Equal(0, sections[7].RefY);
      Assert.Equal(0, sections[7].RefZ);
      Assert.Equal(0, sections[7].Mass);
      Assert.Equal(1, sections[7].Fraction);
      Assert.Equal(0, sections[7].Cost);
      Assert.Equal(0, sections[7].Left);
      Assert.Equal(0, sections[7].Right);
      Assert.Equal(0, sections[7].Slab);

      //SECTION_COMP.4
      gsaSectionComp = (SectionComp)sections[7].Components[0];
      Assert.Equal(1, gsaSectionComp.MaterialIndex);
      Assert.Equal(Section1dMaterialType.STEEL, gsaSectionComp.MaterialType);
      Assert.Null(gsaSectionComp.MatAnalIndex);
      Assert.Equal(0, gsaSectionComp.OffsetY);
      Assert.Equal(0, gsaSectionComp.OffsetZ);
      Assert.Equal(0, gsaSectionComp.Rotation);
      Assert.Equal(ComponentReflection.NONE, gsaSectionComp.Reflect);
      Assert.Equal(0, gsaSectionComp.Pool);
      Assert.Equal(Section1dTaperType.NONE, gsaSectionComp.TaperType);
      Assert.Equal(0, gsaSectionComp.TaperPos);
      Assert.Equal(Section1dProfileGroup.Standard, gsaSectionComp.ProfileGroup);

      //Profile
      var gsaISection = (ProfileDetailsTwoThickness)gsaSectionComp.ProfileDetails;
      Assert.Equal(Section1dProfileGroup.Standard, gsaISection.Group);
      Assert.Equal(100, gsaISection.d);
      Assert.Equal(50, gsaISection.b);
      Assert.Equal(5, gsaISection.tw);
      Assert.Equal(10, gsaISection.tf);

      //SECTION_STEEL
      gsaSectionSteel = (SectionSteel)sections[7].Components[1];
      Assert.Null(gsaSectionSteel.GradeIndex);
      Assert.Equal(1, gsaSectionSteel.PlasElas);
      Assert.Equal(1, gsaSectionSteel.NetGross);
      Assert.Equal(1, gsaSectionSteel.Exposed);
      Assert.Equal(0.4, gsaSectionSteel.Beta);
      Assert.Equal(SectionSteelSectionType.Undefined, gsaSectionSteel.Type);
      Assert.Equal(SectionSteelPlateType.Undefined, gsaSectionSteel.Plate);
      Assert.False(gsaSectionSteel.Locked);
      #endregion
      #region T Section
      //SECTION.7
      Assert.Equal("T Section", sections[8].Name);
      Assert.Equal(Colour.NO_RGB, sections[8].Colour);
      Assert.Equal(Section1dType.Generic, sections[8].Type);
      Assert.Null(sections[8].PoolIndex);
      Assert.Equal(ReferencePoint.Centroid, sections[8].ReferencePoint);
      Assert.Equal(0, sections[8].RefY);
      Assert.Equal(0, sections[8].RefZ);
      Assert.Equal(0, sections[8].Mass);
      Assert.Equal(1, sections[8].Fraction);
      Assert.Equal(0, sections[8].Cost);
      Assert.Equal(0, sections[8].Left);
      Assert.Equal(0, sections[8].Right);
      Assert.Equal(0, sections[8].Slab);

      //SECTION_COMP.4
      gsaSectionComp = (SectionComp)sections[8].Components[0];
      Assert.Equal(1, gsaSectionComp.MaterialIndex);
      Assert.Equal(Section1dMaterialType.STEEL, gsaSectionComp.MaterialType);
      Assert.Null(gsaSectionComp.MatAnalIndex);
      Assert.Equal(0, gsaSectionComp.OffsetY);
      Assert.Equal(0, gsaSectionComp.OffsetZ);
      Assert.Equal(0, gsaSectionComp.Rotation);
      Assert.Equal(ComponentReflection.NONE, gsaSectionComp.Reflect);
      Assert.Equal(0, gsaSectionComp.Pool);
      Assert.Equal(Section1dTaperType.NONE, gsaSectionComp.TaperType);
      Assert.Equal(0, gsaSectionComp.TaperPos);
      Assert.Equal(Section1dProfileGroup.Standard, gsaSectionComp.ProfileGroup);

      //Profile
      var gsaTSection = (ProfileDetailsTwoThickness)gsaSectionComp.ProfileDetails;
      Assert.Equal(Section1dProfileGroup.Standard, gsaTSection.Group);
      Assert.Equal(100, gsaTSection.d);
      Assert.Equal(50, gsaTSection.b);
      Assert.Equal(5, gsaTSection.tw);
      Assert.Equal(10, gsaTSection.tf);

      //SECTION_STEEL
      gsaSectionSteel = (SectionSteel)sections[8].Components[1];
      Assert.Null(gsaSectionSteel.GradeIndex);
      Assert.Equal(1, gsaSectionSteel.PlasElas);
      Assert.Equal(1, gsaSectionSteel.NetGross);
      Assert.Equal(1, gsaSectionSteel.Exposed);
      Assert.Equal(0.4, gsaSectionSteel.Beta);
      Assert.Equal(SectionSteelSectionType.Undefined, gsaSectionSteel.Type);
      Assert.Equal(SectionSteelPlateType.Undefined, gsaSectionSteel.Plate);
      Assert.False(gsaSectionSteel.Locked);
      #endregion
      #region Channel Section
      //SECTION.7
      Assert.Equal("Channel Section", sections[9].Name);
      Assert.Equal(Colour.NO_RGB, sections[9].Colour);
      Assert.Equal(Section1dType.Generic, sections[9].Type);
      Assert.Null(sections[9].PoolIndex);
      Assert.Equal(ReferencePoint.Centroid, sections[9].ReferencePoint);
      Assert.Equal(0, sections[9].RefY);
      Assert.Equal(0, sections[9].RefZ);
      Assert.Equal(0, sections[9].Mass);
      Assert.Equal(1, sections[9].Fraction);
      Assert.Equal(0, sections[9].Cost);
      Assert.Equal(0, sections[9].Left);
      Assert.Equal(0, sections[9].Right);
      Assert.Equal(0, sections[9].Slab);

      //SECTION_COMP.4
      gsaSectionComp = (SectionComp)sections[9].Components[0];
      Assert.Equal(1, gsaSectionComp.MaterialIndex);
      Assert.Equal(Section1dMaterialType.STEEL, gsaSectionComp.MaterialType);
      Assert.Null(gsaSectionComp.MatAnalIndex);
      Assert.Equal(0, gsaSectionComp.OffsetY);
      Assert.Equal(0, gsaSectionComp.OffsetZ);
      Assert.Equal(0, gsaSectionComp.Rotation);
      Assert.Equal(ComponentReflection.NONE, gsaSectionComp.Reflect);
      Assert.Equal(0, gsaSectionComp.Pool);
      Assert.Equal(Section1dTaperType.NONE, gsaSectionComp.TaperType);
      Assert.Equal(0, gsaSectionComp.TaperPos);
      Assert.Equal(Section1dProfileGroup.Standard, gsaSectionComp.ProfileGroup);

      //Profile
      var gsaChannelSection = (ProfileDetailsTwoThickness)gsaSectionComp.ProfileDetails;
      Assert.Equal(Section1dProfileGroup.Standard, gsaChannelSection.Group);
      Assert.Equal(100, gsaChannelSection.d);
      Assert.Equal(50, gsaChannelSection.b);
      Assert.Equal(5, gsaChannelSection.tw);
      Assert.Equal(10, gsaChannelSection.tf);

      //SECTION_STEEL
      gsaSectionSteel = (SectionSteel)sections[9].Components[1];
      Assert.Null(gsaSectionSteel.GradeIndex);
      Assert.Equal(1, gsaSectionSteel.PlasElas);
      Assert.Equal(1, gsaSectionSteel.NetGross);
      Assert.Equal(1, gsaSectionSteel.Exposed);
      Assert.Equal(0.4, gsaSectionSteel.Beta);
      Assert.Equal(SectionSteelSectionType.Undefined, gsaSectionSteel.Type);
      Assert.Equal(SectionSteelPlateType.Undefined, gsaSectionSteel.Plate);
      Assert.False(gsaSectionSteel.Locked);
      #endregion
      #region Angle Section
      //SECTION.7
      Assert.Equal("Angle Section", sections[10].Name);
      Assert.Equal(Colour.NO_RGB, sections[10].Colour);
      Assert.Equal(Section1dType.Generic, sections[10].Type);
      Assert.Null(sections[10].PoolIndex);
      Assert.Equal(ReferencePoint.Centroid, sections[10].ReferencePoint);
      Assert.Equal(0, sections[10].RefY);
      Assert.Equal(0, sections[10].RefZ);
      Assert.Equal(0, sections[10].Mass);
      Assert.Equal(1, sections[10].Fraction);
      Assert.Equal(0, sections[10].Cost);
      Assert.Equal(0, sections[10].Left);
      Assert.Equal(0, sections[10].Right);
      Assert.Equal(0, sections[10].Slab);

      //SECTION_COMP.4
      gsaSectionComp = (SectionComp)sections[10].Components[0];
      Assert.Equal(1, gsaSectionComp.MaterialIndex);
      Assert.Equal(Section1dMaterialType.STEEL, gsaSectionComp.MaterialType);
      Assert.Null(gsaSectionComp.MatAnalIndex);
      Assert.Equal(0, gsaSectionComp.OffsetY);
      Assert.Equal(0, gsaSectionComp.OffsetZ);
      Assert.Equal(0, gsaSectionComp.Rotation);
      Assert.Equal(ComponentReflection.NONE, gsaSectionComp.Reflect);
      Assert.Equal(0, gsaSectionComp.Pool);
      Assert.Equal(Section1dTaperType.NONE, gsaSectionComp.TaperType);
      Assert.Equal(0, gsaSectionComp.TaperPos);
      Assert.Equal(Section1dProfileGroup.Standard, gsaSectionComp.ProfileGroup);

      //Profile
      var gsaAngleSection = (ProfileDetailsTwoThickness)gsaSectionComp.ProfileDetails;
      Assert.Equal(Section1dProfileGroup.Standard, gsaAngleSection.Group);
      Assert.Equal(100, gsaAngleSection.d);
      Assert.Equal(50, gsaAngleSection.b);
      Assert.Equal(5, gsaAngleSection.tw);
      Assert.Equal(10, gsaAngleSection.tf);

      //SECTION_STEEL
      gsaSectionSteel = (SectionSteel)sections[10].Components[1];
      Assert.Null(gsaSectionSteel.GradeIndex);
      Assert.Equal(1, gsaSectionSteel.PlasElas);
      Assert.Equal(1, gsaSectionSteel.NetGross);
      Assert.Equal(1, gsaSectionSteel.Exposed);
      Assert.Equal(0.4, gsaSectionSteel.Beta);
      Assert.Equal(SectionSteelSectionType.Undefined, gsaSectionSteel.Type);
      Assert.Equal(SectionSteelPlateType.Undefined, gsaSectionSteel.Plate);
      Assert.False(gsaSectionSteel.Locked);
      #endregion
      #region Taper Section
      //SECTION.7
      Assert.Equal("Taper Section", sections[11].Name);
      Assert.Equal(Colour.NO_RGB, sections[11].Colour);
      Assert.Equal(Section1dType.Generic, sections[11].Type);
      Assert.Null(sections[11].PoolIndex);
      Assert.Equal(ReferencePoint.Centroid, sections[11].ReferencePoint);
      Assert.Equal(0, sections[11].RefY);
      Assert.Equal(0, sections[11].RefZ);
      Assert.Equal(0, sections[11].Mass);
      Assert.Equal(1, sections[11].Fraction);
      Assert.Equal(0, sections[11].Cost);
      Assert.Equal(0, sections[11].Left);
      Assert.Equal(0, sections[11].Right);
      Assert.Equal(0, sections[11].Slab);

      //SECTION_COMP.4
      gsaSectionComp = (SectionComp)sections[11].Components[0];
      Assert.Equal(1, gsaSectionComp.MaterialIndex);
      Assert.Equal(Section1dMaterialType.STEEL, gsaSectionComp.MaterialType);
      Assert.Null(gsaSectionComp.MatAnalIndex);
      Assert.Equal(0, gsaSectionComp.OffsetY);
      Assert.Equal(0, gsaSectionComp.OffsetZ);
      Assert.Equal(0, gsaSectionComp.Rotation);
      Assert.Equal(ComponentReflection.NONE, gsaSectionComp.Reflect);
      Assert.Equal(0, gsaSectionComp.Pool);
      Assert.Equal(Section1dTaperType.NONE, gsaSectionComp.TaperType);
      Assert.Equal(0, gsaSectionComp.TaperPos);
      Assert.Equal(Section1dProfileGroup.Standard, gsaSectionComp.ProfileGroup);

      //Profile
      var gsaTaperSection = (ProfileDetailsTaper)gsaSectionComp.ProfileDetails;
      Assert.Equal(Section1dProfileGroup.Standard, gsaTaperSection.Group);
      Assert.Equal(100, gsaTaperSection.d);
      Assert.Equal(50, gsaTaperSection.bt);
      Assert.Equal(20, gsaTaperSection.bb);

      //SECTION_STEEL
      gsaSectionSteel = (SectionSteel)sections[11].Components[1];
      Assert.Null(gsaSectionSteel.GradeIndex);
      Assert.Equal(1, gsaSectionSteel.PlasElas);
      Assert.Equal(1, gsaSectionSteel.NetGross);
      Assert.Equal(1, gsaSectionSteel.Exposed);
      Assert.Equal(0.4, gsaSectionSteel.Beta);
      Assert.Equal(SectionSteelSectionType.Undefined, gsaSectionSteel.Type);
      Assert.Equal(SectionSteelPlateType.Undefined, gsaSectionSteel.Plate);
      Assert.False(gsaSectionSteel.Locked);
      #endregion
      #region Ellipse Section
      //SECTION.7
      Assert.Equal("Ellipse Section", sections[12].Name);
      Assert.Equal(Colour.NO_RGB, sections[12].Colour);
      Assert.Equal(Section1dType.Generic, sections[12].Type);
      Assert.Null(sections[12].PoolIndex);
      Assert.Equal(ReferencePoint.Centroid, sections[12].ReferencePoint);
      Assert.Equal(0, sections[12].RefY);
      Assert.Equal(0, sections[12].RefZ);
      Assert.Equal(0, sections[12].Mass);
      Assert.Equal(1, sections[12].Fraction);
      Assert.Equal(0, sections[12].Cost);
      Assert.Equal(0, sections[12].Left);
      Assert.Equal(0, sections[12].Right);
      Assert.Equal(0, sections[12].Slab);

      //SECTION_COMP.4
      gsaSectionComp = (SectionComp)sections[12].Components[0];
      Assert.Equal(1, gsaSectionComp.MaterialIndex);
      Assert.Equal(Section1dMaterialType.STEEL, gsaSectionComp.MaterialType);
      Assert.Null(gsaSectionComp.MatAnalIndex);
      Assert.Equal(0, gsaSectionComp.OffsetY);
      Assert.Equal(0, gsaSectionComp.OffsetZ);
      Assert.Equal(0, gsaSectionComp.Rotation);
      Assert.Equal(ComponentReflection.NONE, gsaSectionComp.Reflect);
      Assert.Equal(0, gsaSectionComp.Pool);
      Assert.Equal(Section1dTaperType.NONE, gsaSectionComp.TaperType);
      Assert.Equal(0, gsaSectionComp.TaperPos);
      Assert.Equal(Section1dProfileGroup.Standard, gsaSectionComp.ProfileGroup);

      //Profile
      var gsaEllipseSection = (ProfileDetailsEllipse)gsaSectionComp.ProfileDetails;
      Assert.Equal(Section1dProfileGroup.Standard, gsaEllipseSection.Group);
      Assert.Equal(100, gsaEllipseSection.d);
      Assert.Equal(50, gsaEllipseSection.b);
      Assert.Equal(2, gsaEllipseSection.k);

      //SECTION_STEEL
      gsaSectionSteel = (SectionSteel)sections[12].Components[1];
      Assert.Null(gsaSectionSteel.GradeIndex);
      Assert.Equal(1, gsaSectionSteel.PlasElas);
      Assert.Equal(1, gsaSectionSteel.NetGross);
      Assert.Equal(1, gsaSectionSteel.Exposed);
      Assert.Equal(0.4, gsaSectionSteel.Beta);
      Assert.Equal(SectionSteelSectionType.Undefined, gsaSectionSteel.Type);
      Assert.Equal(SectionSteelPlateType.Undefined, gsaSectionSteel.Plate);
      Assert.False(gsaSectionSteel.Locked);
      #endregion
      #region Oval Section
      //SECTION.7
      Assert.Equal("Oval Section", sections[13].Name);
      Assert.Equal(Colour.NO_RGB, sections[13].Colour);
      Assert.Equal(Section1dType.Generic, sections[13].Type);
      Assert.Null(sections[13].PoolIndex);
      Assert.Equal(ReferencePoint.Centroid, sections[13].ReferencePoint);
      Assert.Equal(0, sections[13].RefY);
      Assert.Equal(0, sections[13].RefZ);
      Assert.Equal(0, sections[13].Mass);
      Assert.Equal(1, sections[13].Fraction);
      Assert.Equal(0, sections[13].Cost);
      Assert.Equal(0, sections[13].Left);
      Assert.Equal(0, sections[13].Right);
      Assert.Equal(0, sections[13].Slab);

      //SECTION_COMP.4
      gsaSectionComp = (SectionComp)sections[13].Components[0];
      Assert.Equal(1, gsaSectionComp.MaterialIndex);
      Assert.Equal(Section1dMaterialType.STEEL, gsaSectionComp.MaterialType);
      Assert.Null(gsaSectionComp.MatAnalIndex);
      Assert.Equal(0, gsaSectionComp.OffsetY);
      Assert.Equal(0, gsaSectionComp.OffsetZ);
      Assert.Equal(0, gsaSectionComp.Rotation);
      Assert.Equal(ComponentReflection.NONE, gsaSectionComp.Reflect);
      Assert.Equal(0, gsaSectionComp.Pool);
      Assert.Equal(Section1dTaperType.NONE, gsaSectionComp.TaperType);
      Assert.Equal(0, gsaSectionComp.TaperPos);
      Assert.Equal(Section1dProfileGroup.Standard, gsaSectionComp.ProfileGroup);

      //Profile
      var gsaOvalSection = (ProfileDetailsOval)gsaSectionComp.ProfileDetails;
      Assert.Equal(Section1dProfileGroup.Standard, gsaOvalSection.Group);
      Assert.Equal(100, gsaOvalSection.d);
      Assert.Equal(50, gsaOvalSection.b);
      Assert.Equal(5, gsaOvalSection.t);

      //SECTION_STEEL
      gsaSectionSteel = (SectionSteel)sections[13].Components[1];
      Assert.Null(gsaSectionSteel.GradeIndex);
      Assert.Equal(1, gsaSectionSteel.PlasElas);
      Assert.Equal(1, gsaSectionSteel.NetGross);
      Assert.Equal(1, gsaSectionSteel.Exposed);
      Assert.Equal(0.4, gsaSectionSteel.Beta);
      Assert.Equal(SectionSteelSectionType.Undefined, gsaSectionSteel.Type);
      Assert.Equal(SectionSteelPlateType.Undefined, gsaSectionSteel.Plate);
      Assert.False(gsaSectionSteel.Locked);
      #endregion
      #region Cruciform Section
      //SECTION.7
      Assert.Equal("Cruciform Section", sections[14].Name);
      Assert.Equal(Colour.NO_RGB, sections[14].Colour);
      Assert.Equal(Section1dType.Generic, sections[14].Type);
      Assert.Null(sections[14].PoolIndex);
      Assert.Equal(ReferencePoint.Centroid, sections[14].ReferencePoint);
      Assert.Equal(0, sections[14].RefY);
      Assert.Equal(0, sections[14].RefZ);
      Assert.Equal(0, sections[14].Mass);
      Assert.Equal(1, sections[14].Fraction);
      Assert.Equal(0, sections[14].Cost);
      Assert.Equal(0, sections[14].Left);
      Assert.Equal(0, sections[14].Right);
      Assert.Equal(0, sections[14].Slab);

      //SECTION_COMP.4
      gsaSectionComp = (SectionComp)sections[14].Components[0];
      Assert.Equal(1, gsaSectionComp.MaterialIndex);
      Assert.Equal(Section1dMaterialType.STEEL, gsaSectionComp.MaterialType);
      Assert.Null(gsaSectionComp.MatAnalIndex);
      Assert.Equal(0, gsaSectionComp.OffsetY);
      Assert.Equal(0, gsaSectionComp.OffsetZ);
      Assert.Equal(0, gsaSectionComp.Rotation);
      Assert.Equal(ComponentReflection.NONE, gsaSectionComp.Reflect);
      Assert.Equal(0, gsaSectionComp.Pool);
      Assert.Equal(Section1dTaperType.NONE, gsaSectionComp.TaperType);
      Assert.Equal(0, gsaSectionComp.TaperPos);
      Assert.Equal(Section1dProfileGroup.Standard, gsaSectionComp.ProfileGroup);

      //Profile
      var gsaCruciformSection = (ProfileDetailsTwoThickness)gsaSectionComp.ProfileDetails;
      Assert.Equal(Section1dProfileGroup.Standard, gsaCruciformSection.Group);
      Assert.Equal(100, gsaCruciformSection.d);
      Assert.Equal(50, gsaCruciformSection.b);
      Assert.Equal(5, gsaCruciformSection.tw);
      Assert.Equal(10, gsaCruciformSection.tf);

      //SECTION_STEEL
      gsaSectionSteel = (SectionSteel)sections[14].Components[1];
      Assert.Null(gsaSectionSteel.GradeIndex);
      Assert.Equal(1, gsaSectionSteel.PlasElas);
      Assert.Equal(1, gsaSectionSteel.NetGross);
      Assert.Equal(1, gsaSectionSteel.Exposed);
      Assert.Equal(0.4, gsaSectionSteel.Beta);
      Assert.Equal(SectionSteelSectionType.Undefined, gsaSectionSteel.Type);
      Assert.Equal(SectionSteelPlateType.Undefined, gsaSectionSteel.Plate);
      Assert.False(gsaSectionSteel.Locked);
      #endregion
      #region General I Section
      //SECTION.7
      Assert.Equal("General I Section", sections[15].Name);
      Assert.Equal(Colour.NO_RGB, sections[15].Colour);
      Assert.Equal(Section1dType.Generic, sections[15].Type);
      Assert.Null(sections[15].PoolIndex);
      Assert.Equal(ReferencePoint.Centroid, sections[15].ReferencePoint);
      Assert.Equal(0, sections[15].RefY);
      Assert.Equal(0, sections[15].RefZ);
      Assert.Equal(0, sections[15].Mass);
      Assert.Equal(1, sections[15].Fraction);
      Assert.Equal(0, sections[15].Cost);
      Assert.Equal(0, sections[15].Left);
      Assert.Equal(0, sections[15].Right);
      Assert.Equal(0, sections[15].Slab);

      //SECTION_COMP.4
      gsaSectionComp = (SectionComp)sections[15].Components[0];
      Assert.Equal(1, gsaSectionComp.MaterialIndex);
      Assert.Equal(Section1dMaterialType.STEEL, gsaSectionComp.MaterialType);
      Assert.Null(gsaSectionComp.MatAnalIndex);
      Assert.Equal(0, gsaSectionComp.OffsetY);
      Assert.Equal(0, gsaSectionComp.OffsetZ);
      Assert.Equal(0, gsaSectionComp.Rotation);
      Assert.Equal(ComponentReflection.NONE, gsaSectionComp.Reflect);
      Assert.Equal(0, gsaSectionComp.Pool);
      Assert.Equal(Section1dTaperType.NONE, gsaSectionComp.TaperType);
      Assert.Equal(0, gsaSectionComp.TaperPos);
      Assert.Equal(Section1dProfileGroup.Standard, gsaSectionComp.ProfileGroup);

      //Profile
      var gsaGeneralISection = (ProfileDetailsGeneralI)gsaSectionComp.ProfileDetails;
      Assert.Equal(Section1dProfileGroup.Standard, gsaGeneralISection.Group);
      Assert.Equal(100, gsaGeneralISection.d);
      Assert.Equal(50, gsaGeneralISection.bt);
      Assert.Equal(20, gsaGeneralISection.bb);
      Assert.Equal(10, gsaGeneralISection.tft);
      Assert.Equal(5, gsaGeneralISection.tfb);
      Assert.Equal(2, gsaGeneralISection.tw);

      //SECTION_STEEL
      gsaSectionSteel = (SectionSteel)sections[15].Components[1];
      Assert.Null(gsaSectionSteel.GradeIndex);
      Assert.Equal(1, gsaSectionSteel.PlasElas);
      Assert.Equal(1, gsaSectionSteel.NetGross);
      Assert.Equal(1, gsaSectionSteel.Exposed);
      Assert.Equal(0.4, gsaSectionSteel.Beta);
      Assert.Equal(SectionSteelSectionType.Undefined, gsaSectionSteel.Type);
      Assert.Equal(SectionSteelPlateType.Undefined, gsaSectionSteel.Plate);
      Assert.False(gsaSectionSteel.Locked);
      #endregion
      #region General Z Section
      //SECTION.7
      Assert.Equal("General Z Section", sections[16].Name);
      Assert.Equal(Colour.NO_RGB, sections[16].Colour);
      Assert.Equal(Section1dType.Generic, sections[16].Type);
      Assert.Null(sections[16].PoolIndex);
      Assert.Equal(ReferencePoint.Centroid, sections[16].ReferencePoint);
      Assert.Equal(0, sections[16].RefY);
      Assert.Equal(0, sections[16].RefZ);
      Assert.Equal(0, sections[16].Mass);
      Assert.Equal(1, sections[16].Fraction);
      Assert.Equal(0, sections[16].Cost);
      Assert.Equal(0, sections[16].Left);
      Assert.Equal(0, sections[16].Right);
      Assert.Equal(0, sections[16].Slab);

      //SECTION_COMP.4
      gsaSectionComp = (SectionComp)sections[16].Components[0];
      Assert.Equal(1, gsaSectionComp.MaterialIndex);
      Assert.Equal(Section1dMaterialType.STEEL, gsaSectionComp.MaterialType);
      Assert.Null(gsaSectionComp.MatAnalIndex);
      Assert.Equal(0, gsaSectionComp.OffsetY);
      Assert.Equal(0, gsaSectionComp.OffsetZ);
      Assert.Equal(0, gsaSectionComp.Rotation);
      Assert.Equal(ComponentReflection.NONE, gsaSectionComp.Reflect);
      Assert.Equal(0, gsaSectionComp.Pool);
      Assert.Equal(Section1dTaperType.NONE, gsaSectionComp.TaperType);
      Assert.Equal(0, gsaSectionComp.TaperPos);
      Assert.Equal(Section1dProfileGroup.Standard, gsaSectionComp.ProfileGroup);

      //Profile
      var gsaGeneralZSection = (ProfileDetailsZ)gsaSectionComp.ProfileDetails;
      Assert.Equal(Section1dProfileGroup.Standard, gsaGeneralZSection.Group);
      Assert.Equal(100, gsaGeneralZSection.d);
      Assert.Equal(50, gsaGeneralZSection.bt);
      Assert.Equal(20, gsaGeneralZSection.bb);
      Assert.Equal(10, gsaGeneralZSection.dt);
      Assert.Equal(5, gsaGeneralZSection.db);
      Assert.Equal(2, gsaGeneralZSection.t);

      //SECTION_STEEL
      gsaSectionSteel = (SectionSteel)sections[16].Components[1];
      Assert.Null(gsaSectionSteel.GradeIndex);
      Assert.Equal(1, gsaSectionSteel.PlasElas);
      Assert.Equal(1, gsaSectionSteel.NetGross);
      Assert.Equal(1, gsaSectionSteel.Exposed);
      Assert.Equal(0.4, gsaSectionSteel.Beta);
      Assert.Equal(SectionSteelSectionType.Undefined, gsaSectionSteel.Type);
      Assert.Equal(SectionSteelPlateType.Undefined, gsaSectionSteel.Plate);
      Assert.False(gsaSectionSteel.Locked);
      #endregion
      #region General Channel Section
      //SECTION.7
      Assert.Equal("General Channel Section", sections[17].Name);
      Assert.Equal(Colour.NO_RGB, sections[17].Colour);
      Assert.Equal(Section1dType.Generic, sections[17].Type);
      Assert.Null(sections[17].PoolIndex);
      Assert.Equal(ReferencePoint.Centroid, sections[17].ReferencePoint);
      Assert.Equal(0, sections[17].RefY);
      Assert.Equal(0, sections[17].RefZ);
      Assert.Equal(0, sections[17].Mass);
      Assert.Equal(1, sections[17].Fraction);
      Assert.Equal(0, sections[17].Cost);
      Assert.Equal(0, sections[17].Left);
      Assert.Equal(0, sections[17].Right);
      Assert.Equal(0, sections[17].Slab);

      //SECTION_COMP.4
      gsaSectionComp = (SectionComp)sections[17].Components[0];
      Assert.Equal(1, gsaSectionComp.MaterialIndex);
      Assert.Equal(Section1dMaterialType.STEEL, gsaSectionComp.MaterialType);
      Assert.Null(gsaSectionComp.MatAnalIndex);
      Assert.Equal(0, gsaSectionComp.OffsetY);
      Assert.Equal(0, gsaSectionComp.OffsetZ);
      Assert.Equal(0, gsaSectionComp.Rotation);
      Assert.Equal(ComponentReflection.NONE, gsaSectionComp.Reflect);
      Assert.Equal(0, gsaSectionComp.Pool);
      Assert.Equal(Section1dTaperType.NONE, gsaSectionComp.TaperType);
      Assert.Equal(0, gsaSectionComp.TaperPos);
      Assert.Equal(Section1dProfileGroup.Standard, gsaSectionComp.ProfileGroup);

      //Profile
      var gsaGeneralChannelSection = (ProfileDetailsTwoThickness)gsaSectionComp.ProfileDetails;
      Assert.Equal(Section1dProfileGroup.Standard, gsaGeneralChannelSection.Group);
      Assert.Equal(100, gsaGeneralChannelSection.d);
      Assert.Equal(50, gsaGeneralChannelSection.b);
      Assert.Equal(20, gsaGeneralChannelSection.tw);
      Assert.Equal(5, gsaGeneralChannelSection.tf);

      //SECTION_STEEL
      gsaSectionSteel = (SectionSteel)sections[17].Components[1];
      Assert.Null(gsaSectionSteel.GradeIndex);
      Assert.Equal(1, gsaSectionSteel.PlasElas);
      Assert.Equal(1, gsaSectionSteel.NetGross);
      Assert.Equal(1, gsaSectionSteel.Exposed);
      Assert.Equal(0.4, gsaSectionSteel.Beta);
      Assert.Equal(SectionSteelSectionType.Undefined, gsaSectionSteel.Type);
      Assert.Equal(SectionSteelPlateType.Undefined, gsaSectionSteel.Plate);
      Assert.False(gsaSectionSteel.Locked);
      #endregion
      #region Taper T Section
      //SECTION.7
      Assert.Equal("Taper T Section", sections[18].Name);
      Assert.Equal(Colour.NO_RGB, sections[18].Colour);
      Assert.Equal(Section1dType.Generic, sections[18].Type);
      Assert.Null(sections[18].PoolIndex);
      Assert.Equal(ReferencePoint.Centroid, sections[18].ReferencePoint);
      Assert.Equal(0, sections[18].RefY);
      Assert.Equal(0, sections[18].RefZ);
      Assert.Equal(0, sections[18].Mass);
      Assert.Equal(1, sections[18].Fraction);
      Assert.Equal(0, sections[18].Cost);
      Assert.Equal(0, sections[18].Left);
      Assert.Equal(0, sections[18].Right);
      Assert.Equal(0, sections[18].Slab);

      //SECTION_COMP.4
      gsaSectionComp = (SectionComp)sections[18].Components[0];
      Assert.Equal(1, gsaSectionComp.MaterialIndex);
      Assert.Equal(Section1dMaterialType.STEEL, gsaSectionComp.MaterialType);
      Assert.Null(gsaSectionComp.MatAnalIndex);
      Assert.Equal(0, gsaSectionComp.OffsetY);
      Assert.Equal(0, gsaSectionComp.OffsetZ);
      Assert.Equal(0, gsaSectionComp.Rotation);
      Assert.Equal(ComponentReflection.NONE, gsaSectionComp.Reflect);
      Assert.Equal(0, gsaSectionComp.Pool);
      Assert.Equal(Section1dTaperType.NONE, gsaSectionComp.TaperType);
      Assert.Equal(0, gsaSectionComp.TaperPos);
      Assert.Equal(Section1dProfileGroup.Standard, gsaSectionComp.ProfileGroup);

      //Profile
      var gsaTaperTSection = (ProfileDetailsTaperTAngle)gsaSectionComp.ProfileDetails;
      Assert.Equal(Section1dProfileGroup.Standard, gsaTaperTSection.Group);
      Assert.Equal(100, gsaTaperTSection.d);
      Assert.Equal(50, gsaTaperTSection.b);
      Assert.Equal(10, gsaTaperTSection.tf);
      Assert.Equal(20, gsaTaperTSection.twt);
      Assert.Equal(5, gsaTaperTSection.twb);

      //SECTION_STEEL
      gsaSectionSteel = (SectionSteel)sections[18].Components[1];
      Assert.Null(gsaSectionSteel.GradeIndex);
      Assert.Equal(1, gsaSectionSteel.PlasElas);
      Assert.Equal(1, gsaSectionSteel.NetGross);
      Assert.Equal(1, gsaSectionSteel.Exposed);
      Assert.Equal(0.4, gsaSectionSteel.Beta);
      Assert.Equal(SectionSteelSectionType.Undefined, gsaSectionSteel.Type);
      Assert.Equal(SectionSteelPlateType.Undefined, gsaSectionSteel.Plate);
      Assert.False(gsaSectionSteel.Locked);
      #endregion
      #region Taper Angle Section
      //SECTION.7
      Assert.Equal("Taper Angle Section", sections[19].Name);
      Assert.Equal(Colour.NO_RGB, sections[19].Colour);
      Assert.Equal(Section1dType.Generic, sections[19].Type);
      Assert.Null(sections[19].PoolIndex);
      Assert.Equal(ReferencePoint.Centroid, sections[19].ReferencePoint);
      Assert.Equal(0, sections[19].RefY);
      Assert.Equal(0, sections[19].RefZ);
      Assert.Equal(0, sections[19].Mass);
      Assert.Equal(1, sections[19].Fraction);
      Assert.Equal(0, sections[19].Cost);
      Assert.Equal(0, sections[19].Left);
      Assert.Equal(0, sections[19].Right);
      Assert.Equal(0, sections[19].Slab);

      //SECTION_COMP.4
      gsaSectionComp = (SectionComp)sections[19].Components[0];
      Assert.Equal(1, gsaSectionComp.MaterialIndex);
      Assert.Equal(Section1dMaterialType.STEEL, gsaSectionComp.MaterialType);
      Assert.Null(gsaSectionComp.MatAnalIndex);
      Assert.Equal(0, gsaSectionComp.OffsetY);
      Assert.Equal(0, gsaSectionComp.OffsetZ);
      Assert.Equal(0, gsaSectionComp.Rotation);
      Assert.Equal(ComponentReflection.NONE, gsaSectionComp.Reflect);
      Assert.Equal(0, gsaSectionComp.Pool);
      Assert.Equal(Section1dTaperType.NONE, gsaSectionComp.TaperType);
      Assert.Equal(0, gsaSectionComp.TaperPos);
      Assert.Equal(Section1dProfileGroup.Standard, gsaSectionComp.ProfileGroup);

      //Profile
      var gsaTaperAngleSection = (ProfileDetailsTaperTAngle)gsaSectionComp.ProfileDetails;
      Assert.Equal(Section1dProfileGroup.Standard, gsaTaperAngleSection.Group);
      Assert.Equal(100, gsaTaperAngleSection.d);
      Assert.Equal(50, gsaTaperAngleSection.b);
      Assert.Equal(10, gsaTaperAngleSection.tf);
      Assert.Equal(5, gsaTaperAngleSection.twt);
      Assert.Equal(20, gsaTaperAngleSection.twb);

      //SECTION_STEEL
      gsaSectionSteel = (SectionSteel)sections[19].Components[1];
      Assert.Null(gsaSectionSteel.GradeIndex);
      Assert.Equal(1, gsaSectionSteel.PlasElas);
      Assert.Equal(1, gsaSectionSteel.NetGross);
      Assert.Equal(1, gsaSectionSteel.Exposed);
      Assert.Equal(0.4, gsaSectionSteel.Beta);
      Assert.Equal(SectionSteelSectionType.Undefined, gsaSectionSteel.Type);
      Assert.Equal(SectionSteelPlateType.Undefined, gsaSectionSteel.Plate);
      Assert.False(gsaSectionSteel.Locked);
      #endregion
      #region Taper I Section
      //SECTION.7
      Assert.Equal("Taper I Section", sections[20].Name);
      Assert.Equal(Colour.NO_RGB, sections[20].Colour);
      Assert.Equal(Section1dType.Generic, sections[20].Type);
      Assert.Null(sections[20].PoolIndex);
      Assert.Equal(ReferencePoint.Centroid, sections[20].ReferencePoint);
      Assert.Equal(0, sections[20].RefY);
      Assert.Equal(0, sections[20].RefZ);
      Assert.Equal(0, sections[20].Mass);
      Assert.Equal(1, sections[20].Fraction);
      Assert.Equal(0, sections[20].Cost);
      Assert.Equal(0, sections[20].Left);
      Assert.Equal(0, sections[20].Right);
      Assert.Equal(0, sections[20].Slab);

      //SECTION_COMP.4
      gsaSectionComp = (SectionComp)sections[20].Components[0];
      Assert.Equal(1, gsaSectionComp.MaterialIndex);
      Assert.Equal(Section1dMaterialType.STEEL, gsaSectionComp.MaterialType);
      Assert.Null(gsaSectionComp.MatAnalIndex);
      Assert.Equal(0, gsaSectionComp.OffsetY);
      Assert.Equal(0, gsaSectionComp.OffsetZ);
      Assert.Equal(0, gsaSectionComp.Rotation);
      Assert.Equal(ComponentReflection.NONE, gsaSectionComp.Reflect);
      Assert.Equal(0, gsaSectionComp.Pool);
      Assert.Equal(Section1dTaperType.NONE, gsaSectionComp.TaperType);
      Assert.Equal(0, gsaSectionComp.TaperPos);
      Assert.Equal(Section1dProfileGroup.Standard, gsaSectionComp.ProfileGroup);

      //Profile
      var gsaTaperISection = (ProfileDetailsTaperI)gsaSectionComp.ProfileDetails;
      Assert.Equal(Section1dProfileGroup.Standard, gsaTaperISection.Group);
      Assert.Equal(100, gsaTaperISection.d);
      Assert.Equal(50, gsaTaperISection.bt);
      Assert.Equal(20, gsaTaperISection.bb);
      Assert.Equal(10, gsaTaperISection.tft);
      Assert.Equal(5, gsaTaperISection.tfb);
      Assert.Equal(4, gsaTaperISection.twt);
      Assert.Equal(2, gsaTaperISection.twb);

      //SECTION_STEEL
      gsaSectionSteel = (SectionSteel)sections[20].Components[1];
      Assert.Null(gsaSectionSteel.GradeIndex);
      Assert.Equal(1, gsaSectionSteel.PlasElas);
      Assert.Equal(1, gsaSectionSteel.NetGross);
      Assert.Equal(1, gsaSectionSteel.Exposed);
      Assert.Equal(0.4, gsaSectionSteel.Beta);
      Assert.Equal(SectionSteelSectionType.Undefined, gsaSectionSteel.Type);
      Assert.Equal(SectionSteelPlateType.Undefined, gsaSectionSteel.Plate);
      Assert.False(gsaSectionSteel.Locked);
      #endregion
      #region Recto-Circular Section
      //SECTION.7
      Assert.Equal("Recto-Circular Section", sections[21].Name);
      Assert.Equal(Colour.NO_RGB, sections[21].Colour);
      Assert.Equal(Section1dType.Generic, sections[21].Type);
      Assert.Null(sections[21].PoolIndex);
      Assert.Equal(ReferencePoint.Centroid, sections[21].ReferencePoint);
      Assert.Equal(0, sections[21].RefY);
      Assert.Equal(0, sections[21].RefZ);
      Assert.Equal(0, sections[21].Mass);
      Assert.Equal(1, sections[21].Fraction);
      Assert.Equal(0, sections[21].Cost);
      Assert.Equal(0, sections[21].Left);
      Assert.Equal(0, sections[21].Right);
      Assert.Equal(0, sections[21].Slab);

      //SECTION_COMP.4
      gsaSectionComp = (SectionComp)sections[21].Components[0];
      Assert.Equal(1, gsaSectionComp.MaterialIndex);
      Assert.Equal(Section1dMaterialType.STEEL, gsaSectionComp.MaterialType);
      Assert.Null(gsaSectionComp.MatAnalIndex);
      Assert.Equal(0, gsaSectionComp.OffsetY);
      Assert.Equal(0, gsaSectionComp.OffsetZ);
      Assert.Equal(0, gsaSectionComp.Rotation);
      Assert.Equal(ComponentReflection.NONE, gsaSectionComp.Reflect);
      Assert.Equal(0, gsaSectionComp.Pool);
      Assert.Equal(Section1dTaperType.NONE, gsaSectionComp.TaperType);
      Assert.Equal(0, gsaSectionComp.TaperPos);
      Assert.Equal(Section1dProfileGroup.Standard, gsaSectionComp.ProfileGroup);

      //Profile
      var gsaRectoCircularSection = (ProfileDetailsRectangular)gsaSectionComp.ProfileDetails;
      Assert.Equal(Section1dProfileGroup.Standard, gsaRectoCircularSection.Group);
      Assert.Equal(100, gsaRectoCircularSection.d);
      Assert.Equal(50, gsaRectoCircularSection.b);

      //SECTION_STEEL
      gsaSectionSteel = (SectionSteel)sections[21].Components[1];
      Assert.Null(gsaSectionSteel.GradeIndex);
      Assert.Equal(1, gsaSectionSteel.PlasElas);
      Assert.Equal(1, gsaSectionSteel.NetGross);
      Assert.Equal(1, gsaSectionSteel.Exposed);
      Assert.Equal(0.4, gsaSectionSteel.Beta);
      Assert.Equal(SectionSteelSectionType.Undefined, gsaSectionSteel.Type);
      Assert.Equal(SectionSteelPlateType.Undefined, gsaSectionSteel.Plate);
      Assert.False(gsaSectionSteel.Locked);
      #endregion
      #region Recto-Ellipse Section
      //SECTION.7
      Assert.Equal("Recto-Ellipse Section", sections[22].Name);
      Assert.Equal(Colour.NO_RGB, sections[22].Colour);
      Assert.Equal(Section1dType.Generic, sections[22].Type);
      Assert.Null(sections[22].PoolIndex);
      Assert.Equal(ReferencePoint.Centroid, sections[22].ReferencePoint);
      Assert.Equal(0, sections[22].RefY);
      Assert.Equal(0, sections[22].RefZ);
      Assert.Equal(0, sections[22].Mass);
      Assert.Equal(1, sections[22].Fraction);
      Assert.Equal(0, sections[22].Cost);
      Assert.Equal(0, sections[22].Left);
      Assert.Equal(0, sections[22].Right);
      Assert.Equal(0, sections[22].Slab);

      //SECTION_COMP.4
      gsaSectionComp = (SectionComp)sections[22].Components[0];
      Assert.Equal(1, gsaSectionComp.MaterialIndex);
      Assert.Equal(Section1dMaterialType.STEEL, gsaSectionComp.MaterialType);
      Assert.Null(gsaSectionComp.MatAnalIndex);
      Assert.Equal(0, gsaSectionComp.OffsetY);
      Assert.Equal(0, gsaSectionComp.OffsetZ);
      Assert.Equal(0, gsaSectionComp.Rotation);
      Assert.Equal(ComponentReflection.NONE, gsaSectionComp.Reflect);
      Assert.Equal(0, gsaSectionComp.Pool);
      Assert.Equal(Section1dTaperType.NONE, gsaSectionComp.TaperType);
      Assert.Equal(0, gsaSectionComp.TaperPos);
      Assert.Equal(Section1dProfileGroup.Standard, gsaSectionComp.ProfileGroup);

      //Profile
      var gsaRectoEllipseSection = (ProfileDetailsRectoEllipse)gsaSectionComp.ProfileDetails;
      Assert.Equal(Section1dProfileGroup.Standard, gsaRectoEllipseSection.Group);
      Assert.Equal(100, gsaRectoEllipseSection.d);
      Assert.Equal(50, gsaRectoEllipseSection.b);
      Assert.Equal(80, gsaRectoEllipseSection.df);
      Assert.Equal(30, gsaRectoEllipseSection.bf);
      Assert.Equal(2, gsaRectoEllipseSection.k);

      //SECTION_STEEL
      gsaSectionSteel = (SectionSteel)sections[22].Components[1];
      Assert.Null(gsaSectionSteel.GradeIndex);
      Assert.Equal(1, gsaSectionSteel.PlasElas);
      Assert.Equal(1, gsaSectionSteel.NetGross);
      Assert.Equal(1, gsaSectionSteel.Exposed);
      Assert.Equal(0.4, gsaSectionSteel.Beta);
      Assert.Equal(SectionSteelSectionType.Undefined, gsaSectionSteel.Type);
      Assert.Equal(SectionSteelPlateType.Undefined, gsaSectionSteel.Plate);
      Assert.False(gsaSectionSteel.Locked);
      #endregion
      #region Secant Pile Section
      //SECTION.7
      Assert.Equal("Secant Pile Section", sections[23].Name);
      Assert.Equal(Colour.NO_RGB, sections[23].Colour);
      Assert.Equal(Section1dType.Generic, sections[23].Type);
      Assert.Null(sections[23].PoolIndex);
      Assert.Equal(ReferencePoint.Centroid, sections[23].ReferencePoint);
      Assert.Equal(0, sections[23].RefY);
      Assert.Equal(0, sections[23].RefZ);
      Assert.Equal(0, sections[23].Mass);
      Assert.Equal(1, sections[23].Fraction);
      Assert.Equal(0, sections[23].Cost);
      Assert.Equal(0, sections[23].Left);
      Assert.Equal(0, sections[23].Right);
      Assert.Equal(0, sections[23].Slab);

      //SECTION_COMP.4
      gsaSectionComp = (SectionComp)sections[23].Components[0];
      Assert.Equal(1, gsaSectionComp.MaterialIndex);
      Assert.Equal(Section1dMaterialType.CONCRETE, gsaSectionComp.MaterialType);
      Assert.Null(gsaSectionComp.MatAnalIndex);
      Assert.Equal(0, gsaSectionComp.OffsetY);
      Assert.Equal(0, gsaSectionComp.OffsetZ);
      Assert.Equal(0, gsaSectionComp.Rotation);
      Assert.Equal(ComponentReflection.NONE, gsaSectionComp.Reflect);
      Assert.Equal(0, gsaSectionComp.Pool);
      Assert.Equal(Section1dTaperType.NONE, gsaSectionComp.TaperType);
      Assert.Equal(0, gsaSectionComp.TaperPos);
      Assert.Equal(Section1dProfileGroup.Standard, gsaSectionComp.ProfileGroup);

      //Profile
      var gsaSecantPileSection = (ProfileDetailsSecant)gsaSectionComp.ProfileDetails;
      Assert.Equal(Section1dProfileGroup.Standard, gsaSecantPileSection.Group);
      Assert.Equal(100, gsaSecantPileSection.d);
      Assert.Equal(50, gsaSecantPileSection.c);
      Assert.Equal(2, gsaSecantPileSection.n);

      //SECTION_CONC.6
      var gsaSectionConc = (SectionConc)sections[23].Components[1];
      //TO DO

      //SECTION_LINK.3
      var gsaSectionLink = (SectionLink)sections[23].Components[2];
      //TO DO

      //SECTION_COVER.3
      var gsaSectionCover = (SectionCover)sections[23].Components[3];
      //TO DO

      //SECTION_TMPL.4
      var gsaSectionTmpl = (SectionTmpl)sections[23].Components[4];
      //TO DO

      #endregion
      #region Castellated Section
      //SECTION.7
      Assert.Equal("Castellated Section", sections[24].Name);
      Assert.Equal(Colour.NO_RGB, sections[24].Colour);
      Assert.Equal(Section1dType.Generic, sections[24].Type);
      Assert.Null(sections[24].PoolIndex);
      Assert.Equal(ReferencePoint.Centroid, sections[24].ReferencePoint);
      Assert.Equal(0, sections[24].RefY);
      Assert.Equal(0, sections[24].RefZ);
      Assert.Equal(0, sections[24].Mass);
      Assert.Equal(1, sections[24].Fraction);
      Assert.Equal(0, sections[24].Cost);
      Assert.Equal(0, sections[24].Left);
      Assert.Equal(0, sections[24].Right);
      Assert.Equal(0, sections[24].Slab);

      //SECTION_COMP.4
      gsaSectionComp = (SectionComp)sections[24].Components[0];
      Assert.Equal(1, gsaSectionComp.MaterialIndex);
      Assert.Equal(Section1dMaterialType.STEEL, gsaSectionComp.MaterialType);
      Assert.Null(gsaSectionComp.MatAnalIndex);
      Assert.Equal(0, gsaSectionComp.OffsetY);
      Assert.Equal(0, gsaSectionComp.OffsetZ);
      Assert.Equal(0, gsaSectionComp.Rotation);
      Assert.Equal(ComponentReflection.NONE, gsaSectionComp.Reflect);
      Assert.Equal(0, gsaSectionComp.Pool);
      Assert.Equal(Section1dTaperType.NONE, gsaSectionComp.TaperType);
      Assert.Equal(0, gsaSectionComp.TaperPos);
      Assert.Equal(Section1dProfileGroup.Standard, gsaSectionComp.ProfileGroup);

      //Profile
      var gsaCastellatedSection = (ProfileDetailsCastellatedCellular)gsaSectionComp.ProfileDetails;
      Assert.Equal(Section1dProfileGroup.Standard, gsaCastellatedSection.Group);
      Assert.Equal(100, gsaCastellatedSection.d);
      Assert.Equal(50, gsaCastellatedSection.b);
      Assert.Equal(5, gsaCastellatedSection.tw);
      Assert.Equal(10, gsaCastellatedSection.tf);
      Assert.Equal(60, gsaCastellatedSection.ds);
      Assert.Equal(200, gsaCastellatedSection.p);

      //SECTION_STEEL
      gsaSectionSteel = (SectionSteel)sections[24].Components[1];
      Assert.Null(gsaSectionSteel.GradeIndex);
      Assert.Equal(1, gsaSectionSteel.PlasElas);
      Assert.Equal(1, gsaSectionSteel.NetGross);
      Assert.Equal(1, gsaSectionSteel.Exposed);
      Assert.Equal(0.4, gsaSectionSteel.Beta);
      Assert.Equal(SectionSteelSectionType.Undefined, gsaSectionSteel.Type);
      Assert.Equal(SectionSteelPlateType.Undefined, gsaSectionSteel.Plate);
      Assert.False(gsaSectionSteel.Locked);
      #endregion
      #region Asymmetric Cellular Section
      //SECTION.7
      Assert.Equal("Asymmetric Cellular Section", sections[25].Name);
      Assert.Equal(Colour.NO_RGB, sections[25].Colour);
      Assert.Equal(Section1dType.Generic, sections[25].Type);
      Assert.Null(sections[25].PoolIndex);
      Assert.Equal(ReferencePoint.Centroid, sections[25].ReferencePoint);
      Assert.Equal(0, sections[25].RefY);
      Assert.Equal(0, sections[25].RefZ);
      Assert.Equal(0, sections[25].Mass);
      Assert.Equal(1, sections[25].Fraction);
      Assert.Equal(0, sections[25].Cost);
      Assert.Equal(0, sections[25].Left);
      Assert.Equal(0, sections[25].Right);
      Assert.Equal(0, sections[25].Slab);

      //SECTION_COMP.4
      gsaSectionComp = (SectionComp)sections[25].Components[0];
      Assert.Equal(1, gsaSectionComp.MaterialIndex);
      Assert.Equal(Section1dMaterialType.STEEL, gsaSectionComp.MaterialType);
      Assert.Null(gsaSectionComp.MatAnalIndex);
      Assert.Equal(0, gsaSectionComp.OffsetY);
      Assert.Equal(0, gsaSectionComp.OffsetZ);
      Assert.Equal(0, gsaSectionComp.Rotation);
      Assert.Equal(ComponentReflection.NONE, gsaSectionComp.Reflect);
      Assert.Equal(0, gsaSectionComp.Pool);
      Assert.Equal(Section1dTaperType.NONE, gsaSectionComp.TaperType);
      Assert.Equal(0, gsaSectionComp.TaperPos);
      Assert.Equal(Section1dProfileGroup.Standard, gsaSectionComp.ProfileGroup);

      //Profile
      var gsaAsymmetricCellularSection = (ProfileDetailsAsymmetricCellular)gsaSectionComp.ProfileDetails;
      Assert.Equal(Section1dProfileGroup.Standard, gsaAsymmetricCellularSection.Group);
      Assert.Equal(100, gsaAsymmetricCellularSection.dt);
      Assert.Equal(50, gsaAsymmetricCellularSection.bt);
      Assert.Equal(5, gsaAsymmetricCellularSection.twt);
      Assert.Equal(10, gsaAsymmetricCellularSection.tft);
      Assert.Equal(80, gsaAsymmetricCellularSection.db);
      Assert.Equal(40, gsaAsymmetricCellularSection.bb);
      Assert.Equal(5, gsaAsymmetricCellularSection.twb);
      Assert.Equal(8, gsaAsymmetricCellularSection.tfb);
      Assert.Equal(200, gsaAsymmetricCellularSection.p);
      Assert.Equal(60, gsaAsymmetricCellularSection.ds);

      //SECTION_STEEL
      gsaSectionSteel = (SectionSteel)sections[25].Components[1];
      Assert.Null(gsaSectionSteel.GradeIndex);
      Assert.Equal(1, gsaSectionSteel.PlasElas);
      Assert.Equal(1, gsaSectionSteel.NetGross);
      Assert.Equal(1, gsaSectionSteel.Exposed);
      Assert.Equal(0.4, gsaSectionSteel.Beta);
      Assert.Equal(SectionSteelSectionType.Undefined, gsaSectionSteel.Type);
      Assert.Equal(SectionSteelPlateType.Undefined, gsaSectionSteel.Plate);
      Assert.False(gsaSectionSteel.Locked);
      #endregion
      #region Sheet Pile Section
      //SECTION.7
      Assert.Equal("Sheet Pile Section", sections[26].Name);
      Assert.Equal(Colour.NO_RGB, sections[26].Colour);
      Assert.Equal(Section1dType.Generic, sections[26].Type);
      Assert.Null(sections[26].PoolIndex);
      Assert.Equal(ReferencePoint.Centroid, sections[26].ReferencePoint);
      Assert.Equal(0, sections[26].RefY);
      Assert.Equal(0, sections[26].RefZ);
      Assert.Equal(0, sections[26].Mass);
      Assert.Equal(1, sections[26].Fraction);
      Assert.Equal(0, sections[26].Cost);
      Assert.Equal(0, sections[26].Left);
      Assert.Equal(0, sections[26].Right);
      Assert.Equal(0, sections[26].Slab);

      //SECTION_COMP.4
      gsaSectionComp = (SectionComp)sections[26].Components[0];
      Assert.Equal(1, gsaSectionComp.MaterialIndex);
      Assert.Equal(Section1dMaterialType.STEEL, gsaSectionComp.MaterialType);
      Assert.Null(gsaSectionComp.MatAnalIndex);
      Assert.Equal(0, gsaSectionComp.OffsetY);
      Assert.Equal(0, gsaSectionComp.OffsetZ);
      Assert.Equal(0, gsaSectionComp.Rotation);
      Assert.Equal(ComponentReflection.NONE, gsaSectionComp.Reflect);
      Assert.Equal(0, gsaSectionComp.Pool);
      Assert.Equal(Section1dTaperType.NONE, gsaSectionComp.TaperType);
      Assert.Equal(0, gsaSectionComp.TaperPos);
      Assert.Equal(Section1dProfileGroup.Standard, gsaSectionComp.ProfileGroup);

      //Profile
      var gsaSheetPileSection = (ProfileDetailsSheetPile)gsaSectionComp.ProfileDetails;
      Assert.Equal(Section1dProfileGroup.Standard, gsaSheetPileSection.Group);
      Assert.Equal(100, gsaSheetPileSection.d);
      Assert.Equal(200, gsaSheetPileSection.b);
      Assert.Equal(40, gsaSheetPileSection.bt);
      Assert.Equal(50, gsaSheetPileSection.bb);
      Assert.Equal(10, gsaSheetPileSection.tf);
      Assert.Equal(5, gsaSheetPileSection.tw);

      //SECTION_STEEL
      gsaSectionSteel = (SectionSteel)sections[26].Components[1];
      Assert.Null(gsaSectionSteel.GradeIndex);
      Assert.Equal(1, gsaSectionSteel.PlasElas);
      Assert.Equal(1, gsaSectionSteel.NetGross);
      Assert.Equal(1, gsaSectionSteel.Exposed);
      Assert.Equal(0.4, gsaSectionSteel.Beta);
      Assert.Equal(SectionSteelSectionType.Undefined, gsaSectionSteel.Type);
      Assert.Equal(SectionSteelPlateType.Undefined, gsaSectionSteel.Plate);
      Assert.False(gsaSectionSteel.Locked);
      #endregion
      #endregion
      #region Explicit Section
      //SECTION.7
      Assert.Equal("Explicit Section", sections[27].Name);
      Assert.Equal(Colour.NO_RGB, sections[27].Colour);
      Assert.Equal(Section1dType.Generic, sections[27].Type);
      Assert.Null(sections[27].PoolIndex);
      Assert.Equal(ReferencePoint.Centroid, sections[27].ReferencePoint);
      Assert.Equal(0, sections[27].RefY);
      Assert.Equal(0, sections[27].RefZ);
      Assert.Equal(0, sections[27].Mass);
      Assert.Equal(1, sections[27].Fraction);
      Assert.Equal(0, sections[27].Cost);
      Assert.Equal(0, sections[27].Left);
      Assert.Equal(0, sections[27].Right);
      Assert.Equal(0, sections[27].Slab);

      //SECTION_COMP.4
      gsaSectionComp = (SectionComp)sections[27].Components[0];
      Assert.Equal(1, gsaSectionComp.MaterialIndex);
      Assert.Equal(Section1dMaterialType.STEEL, gsaSectionComp.MaterialType);
      Assert.Null(gsaSectionComp.MatAnalIndex);
      Assert.Equal(0, gsaSectionComp.OffsetY);
      Assert.Equal(0, gsaSectionComp.OffsetZ);
      Assert.Equal(0, gsaSectionComp.Rotation);
      Assert.Equal(ComponentReflection.NONE, gsaSectionComp.Reflect);
      Assert.Equal(0, gsaSectionComp.Pool);
      Assert.Equal(Section1dTaperType.NONE, gsaSectionComp.TaperType);
      Assert.Equal(0, gsaSectionComp.TaperPos);
      Assert.Equal(Section1dProfileGroup.Explicit, gsaSectionComp.ProfileGroup);

      //Profile
      var gsaExplicitSection = (ProfileDetailsExplicit)gsaSectionComp.ProfileDetails;
      Assert.Equal(Section1dProfileGroup.Explicit, gsaExplicitSection.Group);
      Assert.Equal(10, gsaExplicitSection.Area);
      Assert.Equal(11, gsaExplicitSection.Iyy);
      Assert.Equal(12, gsaExplicitSection.Izz);
      Assert.Equal(13, gsaExplicitSection.J);
      Assert.Equal(14, gsaExplicitSection.Ky);
      Assert.Equal(15, gsaExplicitSection.Kz);

      //SECTION_STEEL
      gsaSectionSteel = (SectionSteel)sections[27].Components[1];
      Assert.Null(gsaSectionSteel.GradeIndex);
      Assert.Equal(1, gsaSectionSteel.PlasElas);
      Assert.Equal(1, gsaSectionSteel.NetGross);
      Assert.Equal(1, gsaSectionSteel.Exposed);
      Assert.Equal(0.4, gsaSectionSteel.Beta);
      Assert.Equal(SectionSteelSectionType.Undefined, gsaSectionSteel.Type);
      Assert.Equal(SectionSteelPlateType.Undefined, gsaSectionSteel.Plate);
      Assert.False(gsaSectionSteel.Locked);
      #endregion

      /* TO DO: fix bug in ProcessComponents in GsaSectionParser class
      for (int i = 0; i < sections.Count(); i++)
      {
        Assert.True(new GsaSectionParser(sections[i]).Gwa(out var gwa));
        Assert.Equal(sectionGwas[i], gwa.First());
      }
      */
    }


    [Fact]
    public void GsaUserVehicleSimple()
    {
      var userVehicleGwas = new List<string>()
      {
        "USER_VEHICLE.1\t1\tVehicle 1\t1\t3\t1\t1\t1\t1\t2\t1\t1\t1\t3\t1\t1\t1"
      };
      var userVehicles = new List<GsaUserVehicle>();
      foreach (var g in userVehicleGwas)
      {
        var l = new GsaUserVehicleParser();
        Assert.True(l.FromGwa(g));
        userVehicles.Add((GsaUserVehicle)l.Record);
      }
      Assert.Equal(1, userVehicles[0].Width);
      Assert.Equal(3, userVehicles[0].NumAxle);
      Assert.Equal(new List<double>() { 1, 2, 3 }, userVehicles[0].AxlePosition);
      Assert.Equal(new List<double>() { 1, 1, 1 }, userVehicles[0].AxleOffset);
      Assert.Equal(new List<double>() { 1, 1, 1 }, userVehicles[0].AxleLeft);
      Assert.Equal(new List<double>() { 1, 1, 1 }, userVehicles[0].AxleRight);

      for (int i = 0; i < userVehicles.Count(); i++)
      {
        Assert.True(new GsaUserVehicleParser(userVehicles[i]).Gwa(out var gwa));
        Assert.Equal(userVehicleGwas[i], gwa.First());
      }
    }
    #endregion
    #endregion

    #region data_gen_fns
    private List<GsaEl> GenerateMixedGsaEls()
    {
      var gsaElBeam = new GsaEl()
      {
        ApplicationId = "elbeam",
        Name = "Beam",
        Index = 1,
        Type = ElementType.Beam, //*
        Group = 1,
        PropertyIndex = 2,
        NodeIndices = new List<int> { 3, 4 },
        OrientationNodeIndex = 5,
        Angle = 6,
        ReleaseInclusion = ReleaseInclusion.Included,
        Releases1 = new Dictionary<AxisDirection6, ReleaseCode>() { { AxisDirection6.Y, ReleaseCode.Released }, { AxisDirection6.YY, ReleaseCode.Stiff } }, //*
        Stiffnesses1 = new List<double>() { 7 }, //*
        End1OffsetX = 8,
        End2OffsetX = 9,
        OffsetY = 10,
        OffsetZ = 11,
        ParentIndex = 1
      };

      var gsaElTri3 = new GsaEl()
      {
        ApplicationId = "eltri3",
        Name = "Triangle 3",
        Index = 2,
        Type = ElementType.Triangle3, //*
        Group = 1,
        PropertyIndex = 3,
        NodeIndices = new List<int> { 4, 5, 6 },
        OrientationNodeIndex = 7,
        Angle = 8,
        ReleaseInclusion = ReleaseInclusion.NotIncluded,  //only BEAMs have releases
        End1OffsetX = 10
      };

      return new List<GsaEl> { gsaElBeam, gsaElTri3 };
    }

    private List<GsaNode> GenerateGsaNodes()
    {
      var node1 = new GsaNode() { Index = 1, X = 10, Y = 10, Z = 0 };
      var node2 = new GsaNode() { Index = 2, X = 30, Y = -10, Z = 10 };
      return new List<GsaNode> { node1, node2 };
    }
    #endregion
    /*
    #region model_validation_fns
    //It's assumed the gwa comands are in the correct order
    private bool ModelValidation(string gwaCommand, string keyword, int expectedCount, out int mismatch, bool nodesWithAppIdOnly = false, bool visible = false)
    {
      var result = ModelValidation(new string[] { gwaCommand }, new Dictionary<string, int>() { { keyword, expectedCount } }, out var mismatchByKw, nodesWithAppIdOnly, visible);
      mismatch = (mismatchByKw == null || mismatchByKw.Keys.Count() == 0) ? 0 : mismatchByKw[keyword];
      return result;
    }

    private bool ModelValidation(IEnumerable<string> gwaCommands, string keyword, int expectedCount, out int mismatch, bool nodesWithAppIdOnly = false, bool visible = false)
    {
      var result = ModelValidation(gwaCommands, new Dictionary<string, int>() { { keyword, expectedCount } }, out var mismatchByKw, nodesWithAppIdOnly, visible);
      mismatch = (mismatchByKw == null || mismatchByKw.Keys.Count() == 0) ? 0 : mismatchByKw[keyword];
      return result;
    }

    //It's assumed the gwa comands are in the correct order
    private bool ModelValidation(IEnumerable<string> gwaCommands, Dictionary<string, int> expectedCountByKw, out Dictionary<string, int> mismatchByKw, bool nodesWithAppIdOnly = false, bool visible = false)
    {
      mismatchByKw = new Dictionary<string, int>();

      //Use a real proxy, not the mock one used elsewhere in tests
      var gsaProxy = new GSAProxy();
      gsaProxy.NewFile(visible);
      foreach (var gwaC in gwaCommands)
      {
        gsaProxy.SetGwa(gwaC);
      }
      gsaProxy.Sync();
      if (visible)
      {
        gsaProxy.UpdateViews();
      }
      var lines = gsaProxy.GetGwaData(expectedCountByKw.Keys, nodesWithAppIdOnly);
      lines.ForEach(l => l.Keyword = Helper.RemoveVersionFromKeyword(l.Keyword));
      gsaProxy.Close();

      foreach (var k in expectedCountByKw.Keys)
      {
        var numFound = lines.Where(l => l.Keyword.Equals(k, StringComparison.InvariantCultureIgnoreCase)).Count();
        if (numFound != expectedCountByKw[k])
        {
          mismatchByKw.Add(k, numFound);
        }
      }

      return (mismatchByKw.Keys.Count() == 0);
    }
    #endregion
    */
  }
}