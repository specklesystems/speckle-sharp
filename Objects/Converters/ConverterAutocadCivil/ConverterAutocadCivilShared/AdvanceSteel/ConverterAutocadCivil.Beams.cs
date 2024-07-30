#if ADVANCESTEEL
using System.Collections.Generic;
using System.Linq;

using Autodesk.AdvanceSteel.DotNetRoots.DatabaseAccess;
using Autodesk.AdvanceSteel.Profiles;

using Objects.BuiltElements.AdvanceSteel;
using Line = Objects.Geometry.Line;
using Point = Objects.Geometry.Point;
using ASBeam = Autodesk.AdvanceSteel.Modelling.Beam;
using ASPolyBeam = Autodesk.AdvanceSteel.Modelling.PolyBeam;
using ASPolyline3d = Autodesk.AdvanceSteel.Geometry.Polyline3d;

using Objects.Structural.Properties.Profiles;
using static Autodesk.AdvanceSteel.DotNetRoots.Units.Unit;

namespace Objects.Converter.AutocadCivil;

public partial class ConverterAutocadCivil
{
  private IAsteelObject FilerObjectToSpeckle(ASPolyBeam polyBeam, List<string> notes)
  {
    AsteelPolyBeam asteelPolyBeam = new();

    ASPolyline3d polyline3d = polyBeam.GetPolyline(true);
    asteelPolyBeam.baseLine = PolycurveToSpeckle(polyline3d);

    GetBeamPropertiesToSpeckle(asteelPolyBeam, polyBeam);

    return asteelPolyBeam;
  }

  private IAsteelObject FilerObjectToSpeckle(ASBeam beam, List<string> notes)
  {
    AsteelBeam asteelBeam = new();

    var startPoint = beam.GetPointAtStart();
    var endPoint = beam.GetPointAtEnd();
    var units = ModelUnits;

    Point speckleStartPoint = PointToSpeckle(startPoint, units);
    Point speckleEndPoint = PointToSpeckle(endPoint, units);
    asteelBeam.baseLine = new Line(speckleStartPoint, speckleEndPoint, units);

    GetBeamPropertiesToSpeckle(asteelBeam, beam);

    return asteelBeam;
  }

  private void GetBeamPropertiesToSpeckle(AsteelBeam asteelBeam, ASBeam beam)
  {
    asteelBeam.asteelProfile = new AsteelSectionProfile()
    {
      ProfSectionType = beam.ProfSectionType,
      ProfSectionName = beam.ProfSectionName
    };

    dynamic profType = beam.GetProfType();
    asteelBeam.profile = GetProfileSectionProperties(profType);

    asteelBeam.asteelProfile.SectionProfileDB = GetProfileSectionDBProperties(beam.ProfSectionType, beam.ProfSectionName);

    asteelBeam.area = FromInternalUnits(beam.GetPaintArea(), eUnitType.kArea);

    //There is a bug in some beams that some faces don't appears in ModelerBody (Bug_Polybeam_Speckle.dwg)
    //Changing area unit using ASTORUNITS, bug doesn´t happen.
    //https://speckle.xyz/streams/1a0090e6fc
    SetDisplayValue(asteelBeam, beam);
  }

  private SectionProfile GetProfileSectionProperties(ProfileTypeW profileType)
  {
    var depth = profileType.B;
    var width = profileType.H;
    var tf1 = profileType.Tf;
    var tf2 = profileType.Tw;

    var speckleProfile = new Angle(FormatSectionName(profileType.RunName), depth, width, tf1, tf2);

    return speckleProfile;
  }

  private SectionProfile GetProfileSectionProperties(ProfileTypeR profileType)
  {
    var diameter = profileType.D;

    var speckleProfile = new Circular(FormatSectionName(profileType.RunName), diameter * 0.5);

    return speckleProfile;
  }

  private SectionProfile GetProfileSectionProperties(ProfileTypeI profileType)
  {
    var depth = profileType.H;
    var width = profileType.B;
    var tw = profileType.Tw;
    var tf = profileType.Tf;

    var speckleProfile = new ISection(FormatSectionName(profileType.RunName), depth, width, tw, tf);

    return speckleProfile;
  }

  private SectionProfile GetProfileSectionProperties(ProfileTypeC profileType)
  {
    var depth = profileType.H;
    var width = profileType.B;
    var tw = profileType.Tw;
    var tf = profileType.Tf;

    var speckleProfile = new Channel(FormatSectionName(profileType.RunName), depth, width, tw, tf);

    return speckleProfile;
  }

  private SectionProfile GetProfileSectionProperties(ProfileTypeT profileType)
  {
    var depth = profileType.H;
    var width = profileType.B;
    var tw = profileType.Tw;
    var tf = profileType.Tf;

    var speckleProfile = new Tee(FormatSectionName(profileType.RunName), depth, width, tw, tf);

    return speckleProfile;
  }

  private SectionProfile GetProfileSectionProperties(ProfileType profileType)
  {
    //Undefined
    SectionProfile sectionProfile = new()
    {
      name = profileType.GetType().Name
    };

    return sectionProfile;
  }

  private string FormatSectionName(string sectionName)
  {
    return sectionName.Replace(",", ".");
  }

  /// <summary>
  /// Get profile sections
  /// </summary>
  /// <param name="typeNameText">sectionType</param>
  /// <returns></returns>
  private AsteelSectionProfileDB GetProfileSectionDBProperties(string typeNameText, string sectionName)
  {
    AsteelSectionProfileDB sectionProfileDB = new();

    if (string.IsNullOrEmpty(typeNameText) || string.IsNullOrEmpty(sectionName))
    {
      return sectionProfileDB;
    }

    AstorProfiles astorProfiles = AstorProfiles.Instance;
    System.Data.DataTable table = astorProfiles.getProfileMasterTable();

    var rowSectionType = table.Select(string.Format("TypeNameText='{0}'", typeNameText)).FirstOrDefault();

    if (rowSectionType == null)
    {
      return sectionProfileDB;
    }

    var tableName = rowSectionType["TableName"].ToString();
    var tableProfiles = astorProfiles.getSectionsTable(tableName);

    if (tableProfiles == null)
    {
      return sectionProfileDB;
    }

    var rowSection = tableProfiles.Select(string.Format("SectionName='{0}'", sectionName)).FirstOrDefault();

    if (rowSection == null)
    {
      return sectionProfileDB;
    }

    foreach (var column in tableProfiles.Columns.Cast<System.Data.DataColumn>())
    {
      var rowObject = rowSection[column];

      if (!(rowObject is System.DBNull))
      {
        sectionProfileDB[column.ColumnName] = rowObject;
      }
    }

    return sectionProfileDB;
  }

}

#endif
