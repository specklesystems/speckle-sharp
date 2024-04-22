using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using NUnit.Framework.Internal;
using Objects.Geometry;
using Objects.GIS;
using Objects.Other;
using Objects.Structural.Geometry;
using Speckle.Core.Api;
using Speckle.Core.Models;
using Speckle.Core.Models.GraphTraversal;
using BE = Objects.BuiltElements;
using BER = Objects.BuiltElements.Revit;
using BERC = Objects.BuiltElements.Revit.Curve;
using STR = Objects.Structural;

namespace Objects;

public class BaseComparer : Comparer<Base>
{
  public override int Compare(Base x, Base y) => Comparer<string>.Default.Compare(x.id, y.id);
}

public class TraversalComparitor
{
  [TestCase(
    "https://latest.speckle.systems/projects/2099ac4b5f/models/50fca5a383",
    TestName = "revit/objects/releases"
  )]
  [TestCase(
    "https://latest.speckle.systems/projects/2099ac4b5f/models/417d362755",
    TestName = "rhino/objects/releases"
  )]
  [TestCase(
    "https://latest.speckle.systems/projects/2099ac4b5f/models/9a162e2467",
    TestName = "autocad/objects/releases"
  )]
  [TestCase("https://app.speckle.systems/streams/8cb1c26ab9/commits/727c0e8c5f")]
  [TestCase(
    "https://latest.speckle.systems/projects/2099ac4b5f/models/709097d941",
    TestName = "blender/objects/releases"
  )]
  public async Task Foo(string url)
  {
    Base data = await Helpers.Receive(url).ConfigureAwait(false);

    List<Base> Traverse(GraphTraversal func) =>
      func.Traverse(data).Select(tc => tc.current).Where(CanConvertToNative).ToList();

    var actual = Traverse(DefaultTraversal.TypesAreKing());

    var expected = Traverse(DefaultTraversal.ExistingTraversal(CanConvertToNative));

    Assert.That(actual, Is.EquivalentTo(expected).Using<Base>(new BaseComparer()));
  }

  public bool CanConvertToNative(Base @object)
  {
    switch (@object)
    {
      case Point _:
      case Line _:
      case Circle _:
      case Arc _:
      case Ellipse _:
      case Polyline _:
      case Polycurve _:
      case Curve _:
      case Hatch _:
      case Box _:
      case Mesh _:
      case Brep _:
      case Surface _:
      case Element1D _:
      case Text _:
        return true;
#if GRASSHOPPER
      case Interval _:
      case Interval2d _:
      case Plane _:
      case RenderMaterial _:
      case Spiral _:
      case Transform _:
      case Vector _:
        return true;
#else
      // This types are not supported in GH!
      case Pointcloud _:
      case BERC.ModelCurve _:
      case BE.View3D _:
      case Instance _:
      case BE.GridLine _:
      case BE.Alignment _:
      case BE.Level _:
      case Dimension _:
      case Collection c when !c.collectionType.ToLower().Contains("model"):
        return true;
#endif

      default:
        return false;
    }
  }

  //   public bool CanConvertToNative(Base @object)
  //   {
  //     //Project Document
  //     var schema = @object["@SpeckleSchema"] as Base; // check for contained schema
  //     if (schema != null)
  //     {
  //       return CanConvertToNative(schema);
  //     }
  //
  //     var objRes = @object switch
  //     {
  //       //geometry
  //       ICurve _ => true,
  //       Geometry.Brep _ => true,
  //       Geometry.Mesh _ => true,
  //       // non revit built elems
  //       BE.Structure _ => true,
  //       BE.Alignment _ => true,
  //       //built elems
  //       BER.AdaptiveComponent _ => true,
  //       BE.Beam _ => true,
  //       BE.Brace _ => true,
  //       BE.Column _ => true,
  // #if !REVIT2020 && !REVIT2021
  //       BE.Ceiling _ => true,
  // #endif
  //       BERC.DetailCurve _ => true,
  //       BER.DirectShape _ => true,
  //       BER.FreeformElement _ => true,
  //       BER.FamilyInstance _ => true,
  //       BE.Floor _ => true,
  //       BE.Level _ => true,
  //       BERC.ModelCurve _ => true,
  //       BE.Opening _ => true,
  //       BERC.RoomBoundaryLine _ => true,
  //       BERC.SpaceSeparationLine _ => true,
  //       BE.Roof _ => true,
  //
  // #if (REVIT2024)
  //       RevitToposolid _ => true,
  // #endif
  //       BE.Topography _ => true,
  //       BER.RevitCurtainWallPanel _ => true,
  //       BER.RevitFaceWall _ => true,
  //       BER.RevitProfileWall _ => true,
  //       BE.Wall _ => true,
  //       BE.Duct _ => true,
  //       BE.Pipe _ => true,
  //       BE.Wire _ => true,
  //       BE.CableTray _ => true,
  //       BE.Conduit _ => true,
  //       BE.Revit.RevitRailing _ => true,
  //       Other.Revit.RevitInstance _ => true,
  //       BER.ParameterUpdater _ => true,
  //       BE.View3D _ => true,
  //       BE.Room _ => true,
  //       BE.GridLine _ => true,
  //       BE.Space _ => true,
  //       BE.Network _ => true,
  //       //Structural
  //       STR.Geometry.Element1D _ => true,
  //       STR.Geometry.Element2D _ => true,
  //       Other.BlockInstance _ => true,
  //       Other.MappedBlockWrapper => true,
  //       Organization.DataTable _ => true,
  //       // GIS
  //       PolygonElement _ => true,
  //       _ => false,
  //     };
  //     if (objRes)
  //     {
  //       return true;
  //     }
  //
  //     return false;
  //   }
}
