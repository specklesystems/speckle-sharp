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
    private IAsteelObject FilerObjectToSpeckle(ASBoltPattern bolt, List<string> notes)
    {
      AsteelBolt asteelBolt = bolt is CircleScrewBoltPattern ? (AsteelBolt)new AsteelCircularBolt() : (AsteelBolt)new AsteelRectangularBolt();

      SetDisplayValue(asteelBolt, bolt);

      return asteelBolt;
    }
  }
}

#endif
