using System;
using System.Collections.Generic;
using Tekla.Structures.Model;
using System.Linq;
using Objects.Geometry;
using System.Collections;
using Tekla.Structures.Solid;
using Tekla.Structures.Catalogs;
using TSG = Tekla.Structures.Geometry3d;
using StructuralUtilities.PolygonMesher;
using Speckle.Core.Models;
using Objects.Structural.Properties.Profiles;
using Tekla.Structures.Datatype;
using Objects.BuiltElements.TeklaStructures;
using Speckle.Core.Logging;

namespace Objects.Converter.TeklaStructures;

public partial class ConverterTeklaStructures
{
  public void SetUnits(string units) { }

  public string GetUnitsFromModel()
  {
    var unit = Distance.CurrentUnitType;
    switch (unit)
    {
      case Distance.UnitType.Millimeter:
        return "mm";
      case Distance.UnitType.Centimeter:
        return "cm";
      case Distance.UnitType.Meter:
        return "m";
      case Distance.UnitType.Inch:
        return "in";
      case Distance.UnitType.Foot:
        return "ft";
      default:
        return "mm";
    }
  }

  public Mesh GetMeshFromSolid(Solid solid)
  {
    List<double> vertexList = new() { };
    ArrayList MyFaceNormalList = new();
    List<int> facesList = new() { };

    FaceEnumerator MyFaceEnum = solid.GetFaceEnumerator();

    var counter = 0;
    while (MyFaceEnum.MoveNext())
    {
      int faceIndexOffset = vertexList.Count / 3;

      var mesher = new PolygonMesher();

      Face MyFace = MyFaceEnum.Current as Face;
      if (MyFace != null)
      {
        List<double> outerLoopList = new() { };
        List<List<double>> innerLoopList = new();
        LoopEnumerator MyLoopEnum = MyFace.GetLoopEnumerator();
        var inner_loop = 0;
        while (MyLoopEnum.MoveNext())
        {
          Loop MyLoop = MyLoopEnum.Current as Loop;
          if (MyLoop != null)
          {
            VertexEnumerator MyVertexEnum = MyLoop.GetVertexEnumerator() as VertexEnumerator;
            var innerLoopListOfList = new List<double> { };
            while (MyVertexEnum.MoveNext())
            {
              Tekla.Structures.Geometry3d.Point MyVertex = MyVertexEnum.Current as Tekla.Structures.Geometry3d.Point;
              if (MyVertex != null && inner_loop == 0)
              {
                outerLoopList.Add(MyVertex.X);
                outerLoopList.Add(MyVertex.Y);
                outerLoopList.Add(MyVertex.Z);
              }
              else
              {
                innerLoopListOfList.Add(MyVertex.X);
                innerLoopListOfList.Add(MyVertex.Y);
                innerLoopListOfList.Add(MyVertex.Z);
              }

              //speckleBeam.displayMesh = beam.Profile.
            }
            inner_loop++;
            if (innerLoopListOfList.Any())
            {
              innerLoopList.Add(innerLoopListOfList);
            }
          }
        }
        if (!innerLoopList.Any())
        {
          mesher.Init(outerLoopList);
        }
        else
        {
          mesher.Init(outerLoopList, innerLoopList);
        }
        var faces = mesher.Faces(faceIndexOffset);
        var vertices = mesher.Coordinates;
        var verticesList = vertices.ToList();
        vertexList.AddRange(verticesList);
        //var largestVertixCount = 0;
        //if (facesList.Count == 0)
        //{
        //  largestVertixCount = 0;
        //}
        //else
        //{
        //  largestVertixCount = facesList.Max() + 1;
        //}
        //for (int i = 0; i < faces.Length; i++)
        //{
        //  if (i % 4 == 0)
        //  {
        //    continue;
        //  }
        //  else
        //  {
        //    faces[i] += largestVertixCount;
        //  }
        //}
        facesList.AddRange(faces.ToList());
      }
    }
    return new Mesh(vertexList, facesList, units: GetUnitsFromModel());
  }

  /// <summary>
  /// Get all user properties defined for this object in Tekla
  /// </summary>
  /// <param name="speckleElement"></param>
  /// <param name="teklaObject"></param>
  /// <param name="exclusions">List of BuiltInParameters or GUIDs used to indicate what parameters NOT to get,
  /// we exclude all params already defined on the top level object to avoid duplication and
  /// potential conflicts when setting them back on the element</param>
  public void GetAllUserProperties(Base speckleElement, ModelObject teklaObject, List<string> exclusions = null)
  {
    Hashtable propertyHashtable = new();
    teklaObject.GetAllUserProperties(ref propertyHashtable);

    // sort by key
    var sortedproperties = propertyHashtable
      .Cast<DictionaryEntry>()
      .OrderBy(x => x.Key)
      .ToDictionary(d => (string)d.Key, d => d.Value);

    Base paramBase = new();
    foreach (var kv in sortedproperties)
    {
      try
      {
        paramBase[kv.Key] = kv.Value;
      }
      // The exceptions here are thrown by the DynamicBase class for properties that cannot be set.
      // The errors themselves are not fatal and should simply be logged.
      catch (Exception ex) when (ex is InvalidPropNameException or SpeckleException)
      {
        string exceptionDetail = ex is InvalidPropNameException ? "due to an invalid name" : "";

        SpeckleLog.Logger.Warning(
          $"Element {teklaObject.Identifier.GUID} has a "
            + $"property named {kv.Key} that cannot be set "
            + $"{exceptionDetail}. Skipping."
        );
      }
    }

    if (paramBase.GetDynamicMembers().Any())
    {
      speckleElement["parameters"] = paramBase;
    }
    speckleElement.applicationId = teklaObject.Identifier.GUID.ToString();
    speckleElement["units"] = GetUnitsFromModel();
  }

  public Structural.Properties.Profiles.SectionProfile GetBeamProfile(string teklaProfileString)
  {
    SectionProfile profile = null;
    ProfileItem profileItem = null;

    LibraryProfileItem libItem = new();
    ParametricProfileItem paramItem = new();
    if (libItem.Select(teklaProfileString))
    {
      profileItem = libItem;
    }
    else if (paramItem.Select(teklaProfileString))
    {
      profileItem = paramItem;
    }

    if (profileItem != null)
    {
      switch (profileItem.ProfileItemType)
      {
        case ProfileItem.ProfileItemTypeEnum.PROFILE_I:
          profile = GetIProfile(profileItem);
          break;
        case ProfileItem.ProfileItemTypeEnum.PROFILE_L:
          profile = GetAngleProfile(profileItem);
          break;
        case ProfileItem.ProfileItemTypeEnum.PROFILE_PL:
          profile = GetRectangularProfile(profileItem);
          break;
        case ProfileItem.ProfileItemTypeEnum.PROFILE_P:
          profile = GetRectangularHollowProfile(profileItem);
          break;
        case ProfileItem.ProfileItemTypeEnum.PROFILE_C:
        case ProfileItem.ProfileItemTypeEnum.PROFILE_U:
          profile = GetChannelProfile(profileItem);
          break;
        case ProfileItem.ProfileItemTypeEnum.PROFILE_D:
          profile = GetCircularProfile(profileItem);
          break;
        case ProfileItem.ProfileItemTypeEnum.PROFILE_PD:
          profile = GetCircularHollowProfile(profileItem);
          break;
        case ProfileItem.ProfileItemTypeEnum.PROFILE_T:
          profile = GetTeeProfile(profileItem);
          break;
        default:
          profile = new SectionProfile();
          profile.name =
            profileItem is LibraryProfileItem
              ? ((LibraryProfileItem)profileItem).ProfileName
              : ((ParametricProfileItem)profileItem).CreateProfileString();
          break;
      }
    }
    return profile;
  }

  public Structural.Properties.Profiles.SectionProfile GetContourPlateProfile(string teklaProfileString)
  {
    ParametricProfileItem paramItem = new();
    SectionProfile profile = new() { name = teklaProfileString, shapeType = Structural.ShapeType.Perimeter };
    return profile;
  }

  #region Profile type getters
  private Structural.Properties.Profiles.ISection GetIProfile(ProfileItem profileItem)
  {
    // Set profile name depending on type
    var name =
      profileItem is LibraryProfileItem
        ? ((LibraryProfileItem)profileItem).ProfileName
        : ((ParametricProfileItem)profileItem).CreateProfileString();

    // Get properties from catalog
    var properties = profileItem.aProfileItemParameters.Cast<ProfileItemParameter>().ToList();

    var depth = properties.FirstOrDefault(p => p.Property == "HEIGHT")?.Value;
    var width = properties.FirstOrDefault(p => p.Property == "WIDTH")?.Value;
    var tf = properties.FirstOrDefault(p => p.Property == "FLANGE_THICKNESS")?.Value;
    var tw = properties.FirstOrDefault(p => p.Property == "WEB_THICKNESS")?.Value;

    var speckleProfile = new ISection(
      name,
      depth.GetValueOrDefault(),
      width.GetValueOrDefault(),
      tw.GetValueOrDefault(),
      tf.GetValueOrDefault()
    );
    return speckleProfile;
  }

  private Structural.Properties.Profiles.Angle GetAngleProfile(ProfileItem profileItem)
  {
    var properties = profileItem.aProfileItemParameters.Cast<ProfileItemParameter>().ToList();
    string name =
      profileItem is LibraryProfileItem
        ? ((LibraryProfileItem)profileItem).ProfileName
        : ((ParametricProfileItem)profileItem).CreateProfileString();

    var depth = properties.FirstOrDefault(p => p.Property == "HEIGHT")?.Value;
    var width = properties.FirstOrDefault(p => p.Property == "WIDTH")?.Value;
    var tf1 = properties.FirstOrDefault(p => p.Property == "FLANGE_THICKNESS_1")?.Value;
    var tf2 = properties.FirstOrDefault(p => p.Property == "FLANGE_THICKNESS_2")?.Value;

    var speckleProfile = new Structural.Properties.Profiles.Angle(
      name,
      depth.GetValueOrDefault(),
      width.GetValueOrDefault(),
      tf1.GetValueOrDefault(),
      tf2.GetValueOrDefault()
    );

    return speckleProfile;
  }

  private Structural.Properties.Profiles.Rectangular GetRectangularProfile(ProfileItem profileItem)
  {
    var properties = profileItem.aProfileItemParameters.Cast<ProfileItemParameter>().ToList();
    string name =
      profileItem is LibraryProfileItem
        ? ((LibraryProfileItem)profileItem).ProfileName
        : ((ParametricProfileItem)profileItem).CreateProfileString();

    var depth = properties.FirstOrDefault(p => p.Property == "HEIGHT")?.Value;
    var width = properties.FirstOrDefault(p => p.Property == "WIDTH")?.Value;
    var speckleProfile = new Structural.Properties.Profiles.Rectangular(
      name,
      depth.GetValueOrDefault(),
      width.GetValueOrDefault()
    );

    return speckleProfile;
  }

  private Structural.Properties.Profiles.Rectangular GetRectangularHollowProfile(ProfileItem profileItem)
  {
    var properties = profileItem.aProfileItemParameters.Cast<ProfileItemParameter>().ToList();
    string name =
      profileItem is LibraryProfileItem
        ? ((LibraryProfileItem)profileItem).ProfileName
        : ((ParametricProfileItem)profileItem).CreateProfileString();

    var depth = properties.FirstOrDefault(p => p.Property == "HEIGHT")?.Value;
    var width = properties.FirstOrDefault(p => p.Property == "WIDTH")?.Value;
    var wallThk = properties.FirstOrDefault(p => p.Property == "PLATE_THICKNESS")?.Value;
    var speckleProfile = new Structural.Properties.Profiles.Rectangular(
      name,
      depth.GetValueOrDefault(),
      width.GetValueOrDefault(),
      wallThk.GetValueOrDefault(),
      wallThk.GetValueOrDefault()
    );

    return speckleProfile;
  }

  private Structural.Properties.Profiles.Circular GetCircularProfile(ProfileItem profileItem)
  {
    var properties = profileItem.aProfileItemParameters.Cast<ProfileItemParameter>().ToList();
    string name =
      profileItem is LibraryProfileItem
        ? ((LibraryProfileItem)profileItem).ProfileName
        : ((ParametricProfileItem)profileItem).CreateProfileString();

    var depth = properties.FirstOrDefault(p => p.Property == "DIAMETER")?.Value;
    var speckleProfile = new Structural.Properties.Profiles.Circular(name, depth.GetValueOrDefault() * 0.5);

    return speckleProfile;
  }

  private Structural.Properties.Profiles.Circular GetCircularHollowProfile(ProfileItem profileItem)
  {
    var properties = profileItem.aProfileItemParameters.Cast<ProfileItemParameter>().ToList();
    string name =
      profileItem is LibraryProfileItem
        ? ((LibraryProfileItem)profileItem).ProfileName
        : ((ParametricProfileItem)profileItem).CreateProfileString();

    var depth = properties.FirstOrDefault(p => p.Property == "DIAMETER")?.Value;
    var wallThk = properties.FirstOrDefault(p => p.Property == "PLATE_THICKNESS")?.Value;
    var speckleProfile = new Structural.Properties.Profiles.Circular(
      name,
      depth.GetValueOrDefault() * 0.5,
      wallThk.GetValueOrDefault()
    );

    return speckleProfile;
  }

  private Structural.Properties.Profiles.Channel GetChannelProfile(ProfileItem profileItem)
  {
    var properties = profileItem.aProfileItemParameters.Cast<ProfileItemParameter>().ToList();
    string name =
      profileItem is LibraryProfileItem
        ? ((LibraryProfileItem)profileItem).ProfileName
        : ((ParametricProfileItem)profileItem).CreateProfileString();

    var depth = properties.FirstOrDefault(p => p.Property == "HEIGHT")?.Value;
    var width = properties.FirstOrDefault(p => p.Property == "WIDTH")?.Value;
    var tf = properties.FirstOrDefault(p => p.Property == "FLANGE_THICKNESS")?.Value;
    var tw = properties.FirstOrDefault(p => p.Property == "WEB_THICKNESS")?.Value;
    var wallThk = properties.FirstOrDefault(p => p.Property == "PLATE_THICKNESS")?.Value;

    Channel speckleProfile;
    if (tf.HasValue && tw.HasValue)
    {
      speckleProfile = new Channel(
        name,
        depth.GetValueOrDefault(),
        width.GetValueOrDefault(),
        tw.GetValueOrDefault(),
        tf.GetValueOrDefault()
      );
    }
    else
    {
      speckleProfile = new Channel(
        name,
        depth.GetValueOrDefault(),
        width.GetValueOrDefault(),
        wallThk.GetValueOrDefault(),
        wallThk.GetValueOrDefault()
      );
    }

    return speckleProfile;
  }

  private Structural.Properties.Profiles.Tee GetTeeProfile(ProfileItem profileItem)
  {
    var properties = profileItem.aProfileItemParameters.Cast<ProfileItemParameter>().ToList();
    string name =
      profileItem is LibraryProfileItem
        ? ((LibraryProfileItem)profileItem).ProfileName
        : ((ParametricProfileItem)profileItem).CreateProfileString();

    var depth = properties.FirstOrDefault(p => p.Property == "HEIGHT")?.Value;
    var width = properties.FirstOrDefault(p => p.Property == "WIDTH")?.Value;
    var tf = properties.FirstOrDefault(p => p.Property == "FLANGE_THICKNESS")?.Value;
    var tw = properties.FirstOrDefault(p => p.Property == "WEB_THICKNESS")?.Value;

    var speckleProfile = new Tee(
      name,
      depth.GetValueOrDefault(),
      width.GetValueOrDefault(),
      tw.GetValueOrDefault(),
      tf.GetValueOrDefault()
    );
    return speckleProfile;
  }

  #endregion

  public Structural.Materials.StructuralMaterial GetMaterial(string teklaMaterialString)
  {
    Structural.Materials.StructuralMaterial speckleMaterial = null;

    MaterialItem materialItem = new();
    if (materialItem.Select(teklaMaterialString))
    {
      switch (materialItem.Type)
      {
        case MaterialItem.MaterialItemTypeEnum.MATERIAL_STEEL:
          speckleMaterial = new Structural.Materials.Steel();
          speckleMaterial.materialType = Structural.MaterialType.Steel;
          break;
        case MaterialItem.MaterialItemTypeEnum.MATERIAL_CONCRETE:
          speckleMaterial = new Structural.Materials.Concrete();
          speckleMaterial.materialType = Structural.MaterialType.Concrete;
          break;
        default:
          speckleMaterial = new Structural.Materials.StructuralMaterial();
          break;
      }
      speckleMaterial.name = materialItem.MaterialName;
      speckleMaterial.density = materialItem.ProfileDensity;
      speckleMaterial.elasticModulus = materialItem.ModulusOfElasticity;
      speckleMaterial.poissonsRatio = materialItem.PoissonsRatio;
      speckleMaterial.grade = materialItem.MaterialName;
      speckleMaterial.thermalExpansivity = materialItem.ThermalDilatation;
    }
    return speckleMaterial;
  }

  public ArrayList GetContourPointsFromPolyLine(Polyline polyline)
  {
    var contourList = new ArrayList();
    var coordinates = polyline.value;
    for (int j = 0; j < coordinates.Count; j++)
    {
      if (j % 3 == 0)
      {
        var point = new TSG.Point();
        point.X = coordinates[j];
        point.Y = coordinates[j + 1];
        point.Z = coordinates[j + 2];
        contourList.Add(new ContourPoint(point, new Chamfer()));
      }
    }
    return contourList;
  }

  public void ToNativeContourPlate(Polyline polyline, Contour contour)
  {
    var coordinates = polyline.value;
    for (int j = 0; j < coordinates.Count; j++)
    {
      if (j % 3 == 0)
      {
        var point = new TSG.Point();
        point.X = coordinates[j];
        point.Y = coordinates[j + 1];
        point.Z = coordinates[j + 2];
        contour.AddContourPoint(new ContourPoint(point, null));
      }
    }
  }

  public Polyline ToSpecklePolyline(Tekla.Structures.Model.Polygon polygon)
  {
    List<double> coordinateList = new();
    var units = GetUnitsFromModel();
    var polygonPointList = polygon.Points.Cast<TSG.Point>();
    foreach (var pt in polygonPointList)
    {
      coordinateList.Add(pt.X);
      coordinateList.Add(pt.Y);
      coordinateList.Add(pt.Z);
    }

    var specklePolyline = new Polyline(coordinateList, units);
    return specklePolyline;
  }

  public Polycurve ToSpecklePolycurve(Tekla.Structures.Geometry3d.Polycurve teklaPolycurve)
  {
    var units = GetUnitsFromModel();
    var specklePolycurve = new Polycurve(units);

    foreach (var curveSegment in teklaPolycurve)
    {
      if (curveSegment is TSG.LineSegment)
      {
        var lineSeg = (TSG.LineSegment)curveSegment;

        Point start = new(lineSeg.StartPoint.X, lineSeg.StartPoint.Y, lineSeg.StartPoint.Z, units);
        Point end = new(lineSeg.EndPoint.X, lineSeg.EndPoint.Y, lineSeg.EndPoint.Z, units);

        Line speckleLine = new(start, end, units);
        specklePolycurve.segments.Add(speckleLine);
      }
      else if (curveSegment is TSG.Arc)
      {
        var arcSeg = (TSG.Arc)curveSegment;

        Point start = new(arcSeg.StartPoint.X, arcSeg.StartPoint.Y, arcSeg.StartPoint.Z, units);
        Point end = new(arcSeg.EndPoint.X, arcSeg.EndPoint.Y, arcSeg.EndPoint.Z, units);
        Point mid = new(arcSeg.ArcMiddlePoint.X, arcSeg.ArcMiddlePoint.Y, arcSeg.ArcMiddlePoint.Z, units);

        Arc speckleArc = new();
        speckleArc.startPoint = start;
        speckleArc.endPoint = end;
        speckleArc.midPoint = mid;
        speckleArc.radius = arcSeg.Radius;
        speckleArc.angleRadians = arcSeg.Angle;

        specklePolycurve.segments.Add(speckleArc);
      }
    }
    return specklePolycurve;
  }

  public TeklaContourPoint ToSpeckleContourPoint(ContourPoint contourPoint)
  {
    var speckleCP = new TeklaContourPoint();
    speckleCP.x = contourPoint.X;
    speckleCP.y = contourPoint.Y;
    speckleCP.z = contourPoint.Z;

    speckleCP.chamferType = (TeklaChamferType)contourPoint.Chamfer.Type;
    speckleCP.xDim = contourPoint.Chamfer.X;
    speckleCP.yDim = contourPoint.Chamfer.Y;
    speckleCP.dz1 = contourPoint.Chamfer.DZ1;
    speckleCP.dz2 = contourPoint.Chamfer.DZ2;
    speckleCP.units = GetUnitsFromModel();
    return speckleCP;
  }

  public ContourPoint ToTeklaContourPoint(TeklaContourPoint speckleCP)
  {
    var teklaCP = new ContourPoint();
    teklaCP.SetPoint(new TSG.Point(speckleCP.x, speckleCP.y, speckleCP.z));
    teklaCP.Chamfer.Type = (Chamfer.ChamferTypeEnum)speckleCP.chamferType;
    teklaCP.Chamfer.X = speckleCP.xDim;
    teklaCP.Chamfer.Y = speckleCP.yDim;
    teklaCP.Chamfer.DZ1 = speckleCP.dz1;
    teklaCP.Chamfer.DZ2 = speckleCP.dz2;
    return teklaCP;
  }

  public TeklaPosition GetPositioning(Position position)
  {
    var specklePosition = new TeklaPosition()
    {
      Depth = (TeklaDepthEnum)position.Depth,
      Plane = (TeklaPlaneEnum)position.Plane,
      Rotation = (TeklaRotationEnum)position.Rotation,
      depthOffset = position.DepthOffset,
      planeOffset = position.PlaneOffset,
      rotationOffset = position.RotationOffset
    };

    return specklePosition;
  }

  public Position SetPositioning(TeklaPosition position)
  {
    var teklaPosition = new Position()
    {
      Depth = (Position.DepthEnum)position.Depth,
      Plane = (Position.PlaneEnum)position.Plane,
      Rotation = (Position.RotationEnum)position.Rotation,
      DepthOffset = position.depthOffset,
      PlaneOffset = position.planeOffset,
      RotationOffset = position.rotationOffset
    };

    return teklaPosition;
  }

  public bool IsProfileValid(string profileName)
  {
    if (string.IsNullOrEmpty(profileName))
    {
      return false;
    }

    LibraryProfileItem lpi = new();
    if (lpi.Select(profileName))
    {
      return true;
    }
    else
    {
      ParametricProfileItem ppi = new();
      if (ppi.Select(profileName))
      {
        return true;
      }
      else
      {
        return false;
      }
    }
  }
  //public static bool IsElementSupported(this ModelObject e)
  //{

  //  if (SupportedBuiltInCategories.Contains(e)
  //    return true;
  //  return false;
  //}

  ////list of currently supported Categories (for sending only)
  ////exact copy of the one in the Speckle.ConnectorRevit.ConnectorRevitUtils
  ////until issue https://github.com/specklesystems/speckle-sharp/issues/392 is resolved
  //private static List<ModelObject.ModelObjectEnum> SupportedBuiltInCategories = new List<ModelObject.ModelObjectEnum>{

  //ModelObject.ModelObjectEnum.BEAM,
}
