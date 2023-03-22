#if ADVANCESTEEL2023
using System;
using System.Collections.Generic;
using System.Linq;

using Speckle.Core.Models;

using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Acad = Autodesk.AutoCAD.Geometry;
using AcadDB = Autodesk.AutoCAD.DatabaseServices;

using Objects.BuiltElements.AdvanceSteel;
using Alignment = Objects.BuiltElements.Alignment;
using Arc = Objects.Geometry.Arc;
using Interval = Objects.Primitive.Interval;
using Polycurve = Objects.Geometry.Polycurve;
using Curve = Objects.Geometry.Curve;
using Featureline = Objects.BuiltElements.Featureline;
using Line = Objects.Geometry.Line;
using Point = Objects.Geometry.Point;
using Brep = Objects.Geometry.Brep;
using Mesh = Objects.Geometry.Mesh;
using Pipe = Objects.BuiltElements.Pipe;
using Plane = Objects.Geometry.Plane;
using Polyline = Objects.Geometry.Polyline;
using Profile = Objects.BuiltElements.Profile;
using Spiral = Objects.Geometry.Spiral;
using SpiralType = Objects.Geometry.SpiralType;
using Station = Objects.BuiltElements.Station;
using Structure = Objects.BuiltElements.Structure;
using Objects.Other;
using ASBeam = Autodesk.AdvanceSteel.Modelling.Beam;
using ASPolyBeam = Autodesk.AdvanceSteel.Modelling.PolyBeam;
using ASPlate = Autodesk.AdvanceSteel.Modelling.Plate;
using ASBoltPattern = Autodesk.AdvanceSteel.Modelling.BoltPattern;
using ASSpecialPart = Autodesk.AdvanceSteel.Modelling.SpecialPart;
using ASGrating = Autodesk.AdvanceSteel.Modelling.Grating;
using Autodesk.AdvanceSteel.CADAccess;
using Autodesk.AdvanceSteel.CADLink.Database;
using CADObjectId = Autodesk.AutoCAD.DatabaseServices.ObjectId;
using ASObjectId = Autodesk.AdvanceSteel.CADLink.Database.ObjectId;
using Autodesk.AdvanceSteel.DocumentManagement;
using System.Security.Cryptography;
using System.Collections;
using Autodesk.AdvanceSteel.Modeler;
using Objects.Geometry;
using Autodesk.AutoCAD.BoundaryRepresentation;
using MathNet.Spatial.Euclidean;
using MathPlane = MathNet.Spatial.Euclidean.Plane;
using TriangleNet.Geometry;
using TriangleVertex = TriangleNet.Geometry.Vertex;
using TriangleMesh = TriangleNet.Mesh;
using TriangleNet.Topology;
using Speckle.Core.Api;
using Speckle.Core.Kits;
using Autodesk.AdvanceSteel.ConstructionTypes;
using Autodesk.AdvanceSteel.Modelling;
using Objects.BuiltElements;
using ASFilerObject = Autodesk.AdvanceSteel.CADAccess.FilerObject;

using ASPolyline3d = Autodesk.AdvanceSteel.Geometry.Polyline3d;
using ASCurve3d = Autodesk.AdvanceSteel.Geometry.Curve3d;
using ASLineSeg3d = Autodesk.AdvanceSteel.Geometry.LineSeg3d;
using ASCircArc3d = Autodesk.AdvanceSteel.Geometry.CircArc3d;
using ASPoint3d = Autodesk.AdvanceSteel.Geometry.Point3d;
using ASVector3d = Autodesk.AdvanceSteel.Geometry.Vector3d;
using ASExtents = Autodesk.AdvanceSteel.Geometry.Extents;
using ASPlane = Autodesk.AdvanceSteel.Geometry.Plane;
using Autodesk.AdvanceSteel.DotNetRoots.DatabaseAccess;
using Autodesk.AdvanceSteel.Geometry;
using Autodesk.AdvanceSteel.Profiles;
using Objects.Structural.Properties.Profiles;
using System.Xml.Linq;

namespace Objects.Converter.AutocadCivil
{
  public partial class ConverterAutocadCivil
  {
    private IAsteelObject FilerObjectToSpeckle(ASPolyBeam polyBeam, List<string> notes)
    {
      AsteelPolyBeam asteelPolyBeam = new AsteelPolyBeam();

      ASPolyline3d polyline3d = polyBeam.GetPolyline(true);
      asteelPolyBeam.baseLine = PolycurveToSpeckle(polyline3d);

      GetBeamPropertiesToSpeckle(asteelPolyBeam, polyBeam);

      return asteelPolyBeam;
    }

    private IAsteelObject FilerObjectToSpeckle(ASBeam beam, List<string> notes)
    {
      AsteelBeam asteelBeam = new AsteelBeam();

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

      asteelBeam.area = beam.GetPaintArea();

      //There is a bug in some beams that some faces don't appears in ModelerBody (Bug_Polybeam_Speckle.dwg)
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
      SectionProfile sectionProfile = new SectionProfile()
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
      AsteelSectionProfileDB sectionProfileDB = new AsteelSectionProfileDB();

      if (string.IsNullOrEmpty(typeNameText) || string.IsNullOrEmpty(sectionName))
        return sectionProfileDB;

      AstorProfiles astorProfiles = AstorProfiles.Instance;
      System.Data.DataTable table = astorProfiles.getProfileMasterTable();

      var rowSectionType = table.Select(string.Format("TypeNameText='{0}'", typeNameText)).FirstOrDefault();

      if (rowSectionType == null)
        return sectionProfileDB;

      var tableName = rowSectionType["TableName"].ToString();
      var tableProfiles = astorProfiles.getSectionsTable(tableName);

      if (tableProfiles == null)
        return sectionProfileDB;

      var rowSection = tableProfiles.Select(string.Format("SectionName='{0}'", sectionName)).FirstOrDefault();

      if (rowSection == null)
        return sectionProfileDB;

      foreach (var column in tableProfiles.Columns.Cast<System.Data.DataColumn>())
      {
        var rowObject = rowSection[column];

        if (!(rowObject is System.DBNull))
          sectionProfileDB[column.ColumnName] = rowObject;
      }

      return sectionProfileDB;
    }

  }
}

#endif
