using Newtonsoft.Json;
using Speckle.Core.Kits;
using Speckle.Core.Models;

namespace Tests
{
  /// <summary>
  /// Simple speckle kit (no conversions) used in tests.
  /// </summary>
  public class TestKit : ISpeckleKit
  {
    public IEnumerable<Type> Types => GetType().Assembly.GetTypes().Where(type => type.IsSubclassOf(typeof(Base)));

    public string Description => "Simple object model for with some types for tests.";

    public string Name => nameof(TestKit);

    public string Author => "Dimitrie";

    public string WebsiteOrEmail => "hello@Speckle.Core.works";

    public IEnumerable<string> Converters { get => new List<string>(); }

    public TestKit() { }

    public Base ToSpeckle(object @object)
    {
      throw new NotImplementedException();
    }

    public bool CanConvertToSpeckle(object @object)
    {
      throw new NotImplementedException();
    }

    public object ToNative(Base @object)
    {
      throw new NotImplementedException();
    }

    public bool CanConvertToNative(Base @object)
    {
      throw new NotImplementedException();
    }

    public IEnumerable<string> GetServicedApplications()
    {
      throw new NotImplementedException();
    }

    public void SetContextDocument(object @object)
    {
      throw new NotImplementedException();
    }

    public ISpeckleConverter LoadConverter(string app)
    {
      return null;
    }
  }

  public class FakeMesh : Base
  {
    [DetachProperty]
    [Chunkable]
    public List<double> Vertices { get; set; } = new List<double>();

    [DetachProperty]
    [Chunkable(1000)]
    public double[] ArrayOfDoubles { get; set; }

    [DetachProperty]
    [Chunkable(1000)]
    public TableLeg[] ArrayOfLegs { get; set; }

    [DetachProperty]
    [Chunkable(2500)]
    public List<Tabletop> Tables { get; set; } = new List<Tabletop>();

    public FakeMesh() { }
  }

  public class DiningTable : Base
  {
    [DetachProperty]
    public TableLeg LegOne { get; set; }

    [DetachProperty]
    public TableLeg LegTwo { get; set; }

    [DetachProperty]
    public List<TableLeg> MoreLegs { get; set; } = new List<TableLeg>();

    [DetachProperty]
    public Tabletop Tabletop { get; set; }

    public string TableModel { get; set; } = "Sample Table";

    public DiningTable()
    {
      LegOne = new TableLeg() { height = 2 * 3, radius = 10 };
      LegTwo = new TableLeg() { height = 1, radius = 5 };

      MoreLegs.Add(new TableLeg() { height = 4 });
      MoreLegs.Add(new TableLeg() { height = 10 });

      Tabletop = new Tabletop() { length = 200, width = 12, thickness = 3 };
    }
  }

  public class Tabletop : Base
  {
    public double length { get; set; }
    public double width { get; set; }
    public double thickness { get; set; }

    public Tabletop() { }
  }

  public class TableLeg : Base
  {
    public double height { get; set; }
    public double radius { get; set; }

    [DetachProperty]
    public TableLegFixture fixture { get; set; } = new TableLegFixture();

    public TableLeg() { }
  }

  public class TableLegFixture : Base
  {
    public string nails { get; set; } = "MANY NAILS WOW ";

    public TableLegFixture() { }
  }

  public class Point : Base
  {
    public double X { get; set; }
    public double Y { get; set; }
    public double Z { get; set; }

    public Point() { }

    public Point(double X, double Y, double Z)
    {
      this.X = X;
      this.Y = Y;
      this.Z = Z;
    }
  }

  public class SuperPoint : Point
  {
    public double W { get; set; }

    public SuperPoint() { }
  }

  public class Mesh : Base
  {
    [JsonIgnore]
    public List<Point> Points = new List<Point>();

    public List<double> Vertices
    {
      get => Points.SelectMany(pt => new List<double>() { pt.X, pt.Y, pt.Z }).ToList();
      set
      {
        for (int i = 0; i < value.Count; i += 3)
        {
          Points.Add(new Point(value[i], value[i + 1], value[i + 2]));
        }
      }
    }

    public List<int> Faces = new List<int>();

    public Mesh() { }
  }

  public interface ICurve
  {
    // Just for fun
  }

  /// <summary>
  /// Store individual points in a list structure for developer ergonomics. Nevertheless, for performance reasons (hashing, serialisation & storage) expose the same list of points as a typed array.
  /// </summary>
  public class Polyline : Base, ICurve
  {
    [JsonIgnore]
    public List<Point> Points = new List<Point>();

    public List<double> Vertices
    {
      get => Points.SelectMany(pt => new List<double>() { pt.X, pt.Y, pt.Z }).ToList();
      set
      {
        for (int i = 0; i < value.Count; i += 3)
        {
          Points.Add(new Point(value[i], value[i + 1], value[i + 2]));
        }
      }
    }

    public Polyline() { }
  }

  public class Line : Base, ICurve
  {
    public Point Start { get; set; }
    public Point End { get; set; }

    public Line() { }
  }

  /// <summary>
  /// This class exists to purely test some weird cases in which Intefaces might trash serialisation.
  /// </summary>
  public class PolygonalFeline : Base
  {
    public List<ICurve> Whiskers { get; set; } = new List<ICurve>();

    public Dictionary<string, ICurve> Claws { get; set; } = new Dictionary<string, ICurve>();

    [DetachProperty]
    public ICurve Tail { get; set; }

    public ICurve[] Fur { get; set; } = new ICurve[1000];

    public PolygonalFeline() { }
  }
}
