using AutoMapper;
using KellermanSoftware.CompareNetObjects;
using Objects.Structural.Loading;
using Objects.Structural.Properties;
using Speckle.Core.Models;
using Speckle.GSA.API.GwaSchema;
using System.Collections.Generic;
using Xunit;

namespace ConverterGSATests
{
  public class TestingLibraryExamples : SpeckleConversionFixture
  {
    [Fact]
    public void TestComplexObjectComparison()
    {
      var p1 = GsaCatalogueSectionExample("section 1");
      var p2 = GsaCatalogueSectionExample("section 1");

      //Two differences ...
      p1.Cost = 100;
      p1.Sid = "Sidney";

      var compareLogic = new CompareLogic();

      //Set config to ignore one difference ...
      //(Creating a lambda expression, as shown below, avoids having to pass a hard-coded string with the property name ("Cost"))
      compareLogic.Config.MembersToIgnore.Add(GetPropertyName((GsaSection x) => x.Cost));

      var result = compareLogic.Compare(p1, p2);

      //.. leaving one difference left (Sid)
      Assert.Single(result.Differences);
    }

    [Fact]
    public void TestRetrievalOfDynamicObjectFromBase()
    {
      var testBase = new Base();
      testBase["MyProp"] = new Property1D("ThisMy1dPropertyDon'tYouMessWithMeYouHearGeeeez");

      var members = testBase.GetMembers();

      var testBaseHasMyProp = members.ContainsKey("MyProp");
    }

    [Fact]
    public void TestCopyIntoBaseClass()
    {
      var baseLoadCase = new LoadCase();
      var gsaLoadCase = new Objects.Structural.GSA.Loading.GSALoadCase()
      {
        //Base class properties
        name = "Dead Load Innit",
        loadType = LoadType.Dead,
        actionType = ActionType.Permanent,
        description = "This load is DEAD to me!",

        //GSA specific
        nativeId = 5,
        direction = LoadDirection2D.Y,
        bridge = true
      };

      var config = new MapperConfiguration(cfg => {
        cfg.CreateMap<Objects.Structural.GSA.Loading.GSALoadCase, LoadCase>();
      });

      var mapper = new Mapper(config);
      mapper.Map(gsaLoadCase, baseLoadCase);

      Assert.Equal(baseLoadCase.name, gsaLoadCase.name);
      Assert.Equal(baseLoadCase.loadType, gsaLoadCase.loadType);
      Assert.Equal(baseLoadCase.actionType, gsaLoadCase.actionType);
      Assert.Equal(baseLoadCase.description, gsaLoadCase.description);
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
        ReferencePoint = ReferencePoint.Centroid,
        Fraction = 1,
        Components = new List<GsaSectionComponentBase>()
        {
          new SectionComp()
          {
            MaterialType = Section1dMaterialType.STEEL,
            MaterialIndex = 1,
            Reflect = ComponentReflection.NONE,
            TaperType = Section1dTaperType.NONE,
            ProfileGroup = Section1dProfileGroup.Catalogue,
            ProfileDetails = new ProfileDetailsCatalogue()
            {
              Group = Section1dProfileGroup.Catalogue,
              Profile = "CAT A-UB 610UB125 19981201"
            }
          },
          new SectionSteel()
          {
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
  }
}
