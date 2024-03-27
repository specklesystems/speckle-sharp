using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using Speckle.Converters.Common.Objects;
using Speckle.Converters.RevitShared.Extensions;
using Speckle.Converters.RevitShared.Helpers;
using Speckle.Core.Models;

namespace Speckle.Converters.RevitShared.ToSpeckle;

public sealed class FamilyInstanceConversionToSpeckle : BaseConversionToSpeckle<DB.FamilyInstance, Base>
{
  private readonly IRawConversion<DB.Element> _elementConverter;
  private readonly IRawConversion<DB.FamilyInstance, SOBR.RevitBeam> _beamConverter;
  private readonly IRawConversion<DB.FamilyInstance, SOBR.RevitColumn> _columnConverter;

  public FamilyInstanceConversionToSpeckle(
    IRawConversion<Element> elementConverter,
    IRawConversion<FamilyInstance, SOBR.RevitBeam> beamConverter,
    IRawConversion<FamilyInstance, SOBR.RevitColumn> columnConverter
  )
  {
    _elementConverter = elementConverter;
    _beamConverter = beamConverter;
    _columnConverter = columnConverter;
  }

  public override Base RawConvert(FamilyInstance target)
  {
    Base @base = null;

    ////adaptive components
    //if (AdaptiveComponentInstanceUtils.IsAdaptiveComponentInstance(target))
    //{
    //  @base = AdaptiveComponentToSpeckle(target);
    //}

    ////these elements come when the curtain wall is generated
    ////if they are contained in 'subelements' then they have already been accounted for from a wall
    ////else if they are mullions then convert them as a generic family instance but add a isUGridLine prop
    //bool? isUGridLine = null;
    //if (
    //  @base == null
    //  && (
    //    target.Category.Id.IntegerValue == (int)BuiltInCategory.OST_CurtainWallMullions
    //    || target.Category.Id.IntegerValue == (int)BuiltInCategory.OST_CurtainWallPanels
    //  )
    //)
    //{
    //  if (_convertedObjectsCache.ContainsBaseConvertedFromId(target.UniqueId))
    //  {
    //    return null;
    //  }
    //  else if (target is Mullion mullion)
    //  {
    //    if (mullion.LocationCurve is DB.Line locationLine && locationLine.Direction != null)
    //    {
    //      var direction = locationLine.Direction;
    //      // TODO: add support for more severly sloped mullions. This isn't very robust at the moment
    //      isUGridLine = Math.Abs(direction.X) > Math.Abs(direction.Y);
    //    }
    //  }
    //  else
    //  {
    //    //TODO: sort these so we consistently get sub-elements from the wall element in case also sub-elements are sent
    //    //SubelementIds.Add(target.Id);
    //  }
    //}

    //beams & braces
    if (
      @base == null
      && RevitCategories.StructuralFraming.BuiltInCategories.Contains(target.Category.GetBuiltInCategory())
    )
    {
      if (target.StructuralType == StructuralType.Beam)
      {
        @base = _beamConverter.RawConvert(target);
      }
      else if (target.StructuralType == StructuralType.Brace)
      {
        //@base = BraceToSpeckle(target, out notes);
      }
    }

    ////columns
    if (
      @base == null
        && RevitCategories.StructuralFraming.BuiltInCategories.Contains(target.Category.GetBuiltInCategory())
      || target.StructuralType == StructuralType.Column
    )
    {
      @base = _columnConverter.RawConvert(target);
    }

    //// MEP elements
    //if (target.MEPModel?.ConnectorManager?.Connectors?.Size > 0)
    //{
    //  @base = MEPFamilyInstanceToSpeckle(target);
    //}

    //// curtain panels
    //if (target is DB.Panel panel)
    //{
    //  @base = PanelToSpeckle(panel);
    //}

    //// elements
    //var baseGeometry = LocationToSpeckle(target);
    //var basePoint = baseGeometry as Point;
    //if (@base == null && basePoint == null)
    //{
    //  @base = RevitElementToSpeckle(target, out notes);
    //}

    //// point based, convert these as revit instances
    //if (@base == null)
    //{
    //  @base = RevitInstanceToSpeckle(target, out notes, null);
    //}

    @base ??= _elementConverter.ConvertToBase(target);

    //// add additional props to base object
    //if (isUGridLine.HasValue)
    //{
    //  @base["isUGridLine"] = isUGridLine.Value;
    //}

    //if (target.Room != null)
    //{
    //  @base["roomId"] = target.Room.Id.ToString();
    //}

    //if (target.ToRoom != null)
    //{
    //  @base["toRoomId"] = target.ToRoom.Id.ToString();
    //}

    //if (target.FromRoom != null)
    //{
    //  @base["fromRoomId"] = target.FromRoom.Id.ToString();
    //}

    return @base;
  }
}
