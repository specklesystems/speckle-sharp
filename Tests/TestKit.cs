using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Speckle.Kits;
using Speckle.Models;

namespace Tests
{
  public class TestKit : ISpeckleKit
  {
    public IEnumerable<Type> Types => GetType().Assembly.GetTypes().Where(type => type.IsSubclassOf(typeof(Base)));

    public IEnumerable<Type> Converters => GetType().Assembly.GetTypes().Where(type => type.IsSubclassOf(typeof(Converter)));

    public string Description => "Simple object model for basic geometry types.";

    public string Name => nameof(TestKit);

    public string Author => "Dimitrie";

    public string WebsiteOrEmail => "hello@speckle.works";

    public TestKit() { }
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

      //TableModel += " :: " + DateTime.Now;
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

  /// <summary>
  /// Store individual points in a list structure for developer ergonomics. Nevertheless, for performance reasons (hashing, serialisation & storage) expose the same list of points as a typed array.
  /// </summary>
  public class Polyline : Base
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
}
