using System.Collections.Generic;
using System.Threading.Tasks;
using Autodesk.Revit.DB;
using ConverterRevitTests;
using Speckle.Core.Models;
using DB = Autodesk.Revit.DB;

namespace ConverterRevitTestsShared.AssertionClasses
{
  internal class AssertionFactory : IAssertEqual
  {
    public async Task Handle<T>(T sourceElement, T destElement, Base _)
      where T : Element
    {
      switch (sourceElement)
      {
        case DB.CurveElement curve:
          AssertUtils.CurveEqual(curve, destElement as CurveElement);
          break;
        case DB.DirectShape ds:
          AssertUtils.DirectShapeEqual(ds, destElement as DirectShape);
          break;
        case DB.Mechanical.Duct duct:
          AssertUtils.DuctEqual(duct, destElement as DB.Mechanical.Duct);
          break;
        case DB.FamilyInstance fi:
          AssertUtils.FamilyInstanceEqual(fi, destElement as FamilyInstance);
          break;
        case DB.Floor floor:
          await AssertUtils.FloorEqual(floor, destElement as DB.Floor).ConfigureAwait(false);
          break;
        case DB.Opening opening:
          AssertUtils.OpeningEqual(opening, destElement as DB.Opening);
          break;
        case DB.Plumbing.Pipe pipe:
          AssertUtils.PipeEqual(pipe, destElement as DB.Plumbing.Pipe);
          break;
        case DB.RoofBase roof:
          AssertUtils.RoofEqual(roof, destElement as DB.RoofBase);
          break;
        case DB.ViewSchedule sch:
          await AssertUtils.ScheduleEqual(sch, destElement as DB.ViewSchedule).ConfigureAwait(false);
          break;
        case DB.Wall wall:
          AssertUtils.WallEqual(wall, destElement as DB.Wall);
          break;
        case DB.Electrical.Wire wire:
          AssertUtils.WireEqual(wire, destElement as DB.Electrical.Wire);
          break;
        default:
          AssertUtils.ElementEqual(sourceElement, destElement);
          break;
      }
    }
  }
}
