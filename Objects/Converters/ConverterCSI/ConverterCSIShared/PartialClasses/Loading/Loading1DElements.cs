using System;
using System.Collections.Generic;
using System.Linq;
using Objects.Structural.Geometry;
using Objects.Structural.Loading;
using Speckle.Core.Kits;
using Speckle.Core.Models;

namespace Objects.Converter.CSI;

public partial class ConverterCSI
{
  readonly Dictionary<string, LoadBeam> _loadStoringBeam = new();
  readonly Dictionary<string, List<Base>> _frameStoring = new();
  int _counterFrame;

  List<string> LoadFrameToNative(LoadBeam loadBeam, IList<string>? notes)
  {
    int direction = 11;
    int myType = 1;

    if (loadBeam.isProjected)
    {
      switch (loadBeam.direction)
      {
        case LoadDirection.X:
          direction = 7;
          myType = 1;
          break;
        case LoadDirection.Y:
          direction = 8;
          myType = 1;
          break;
        case LoadDirection.Z:
          direction = 9;
          myType = 1;
          break;
        case LoadDirection.XX:
          direction = 7;
          myType = 2;
          break;
        case LoadDirection.YY:
          direction = 8;
          myType = 2;
          break;
        case LoadDirection.ZZ:
          direction = 9;
          myType = 2;
          break;
        default:
          throw new ArgumentOutOfRangeException($"Unrecognised load direction {loadBeam.direction}");
      }
    }
    else if (loadBeam.loadAxisType == Structural.LoadAxisType.Local)
    {
      switch (loadBeam.direction)
      {
        case LoadDirection.X:
          direction = 1;
          myType = 1;
          break;
        case LoadDirection.Y:
          direction = 2;
          myType = 1;
          break;
        case LoadDirection.Z:
          direction = 3;
          myType = 1;
          break;
        case LoadDirection.XX:
          direction = 1;
          myType = 2;
          break;
        case LoadDirection.YY:
          direction = 2;
          myType = 2;
          break;
        case LoadDirection.ZZ:
          direction = 3;
          myType = 2;
          break;
        default:
          throw new ArgumentOutOfRangeException(nameof(loadBeam), $"Unrecognised load direction {loadBeam.direction}");
      }
    }
    else
    {
      switch (loadBeam.direction)
      {
        case LoadDirection.X:
          direction = 4;
          myType = 1;
          break;
        case LoadDirection.Y:
          direction = 5;
          myType = 1;
          break;
        case LoadDirection.Z:
          direction = 6;
          myType = 1;
          break;
        case LoadDirection.XX:
          direction = 4;
          myType = 2;
          break;
        case LoadDirection.YY:
          direction = 5;
          myType = 2;
          break;
        case LoadDirection.ZZ:
          direction = 6;
          myType = 2;
          break;
        default:
          throw new ArgumentOutOfRangeException(nameof(loadBeam), $"Unrecognised load direction {loadBeam.direction}");
      }
    }

    List<string> createdElements = new(loadBeam.elements.Count);

    foreach (var el in loadBeam.elements)
    {
      if (el is not Element1D element)
      {
        notes?.Add($"Skipping converting sub-element {el} as is not an {nameof(Element1D)}"); //TODO: is the term "sub-element" correct here? or is there a better word to use?
        continue;
      }

      int success;
      string name = element.name ?? element.id;

      if (loadBeam.loadType == BeamLoadType.Point)
      {
        success = Model.FrameObj.SetLoadDistributed(
          name,
          loadBeam.loadCase.name,
          myType,
          direction,
          loadBeam.positions[0],
          loadBeam.positions[1],
          loadBeam.values[0],
          loadBeam.values[1]
        );
      }
      else
      {
        success = Model.FrameObj.SetLoadPoint(
          name,
          loadBeam.loadCase.name,
          myType,
          direction,
          loadBeam.positions[0],
          loadBeam.values[0]
        );
      }

      if (success != 0)
      {
        notes?.Add($"Failed to convert sub-element {element.name}");
      }
    }

    if (!createdElements.Any())
    {
      throw new ConversionException($"Zero out of {loadBeam.elements.Count} elements converted successfully");
    }

    return createdElements;
  }

  Base LoadFrameToSpeckle(string name, int frameNumber)
  {
    int numberItems = 0;
    string[] frameName = null;
    string[] loadPat = null;
    int[] MyType = null;
    string[] csys = null;
    int[] dir = null;
    double[] RD1 = null;
    double[] RD2 = null;
    double[] dist1 = null;
    double[] dist2 = null;
    double[] val1 = null;
    double[] val2 = null;

    //var element1DList = new List<Base>();

    int s = Model.FrameObj.GetLoadDistributed(
      name,
      ref numberItems,
      ref frameName,
      ref loadPat,
      ref MyType,
      ref csys,
      ref dir,
      ref RD1,
      ref RD2,
      ref dist1,
      ref dist2,
      ref val1,
      ref val2
    );
    if (s == 0)
    {
      foreach (int index in Enumerable.Range(0, numberItems))
      {
        var speckleLoadFrame = new LoadBeam();
        var element = FrameToSpeckle(frameName[index]);
        var loadID = String.Concat(
          loadPat[index],
          val1[index],
          val2[index],
          dist1[index],
          dist2[index],
          dir[index],
          MyType[index]
        );
        speckleLoadFrame.applicationId = loadID;
        _frameStoring.TryGetValue(loadID, out var element1DList);
        if (element1DList == null)
        {
          element1DList = new List<Base> { };
        }
        if (!element1DList.Select(el => el.applicationId).Contains(element.applicationId))
        {
          element1DList.Add(element);
        }

        _frameStoring[loadID] = element1DList;

        switch (dir[index])
        {
          case 1:
            speckleLoadFrame.direction = (MyType[index] == 1) ? LoadDirection.X : LoadDirection.XX;
            speckleLoadFrame.loadAxisType = Structural.LoadAxisType.Local;
            break;
          case 2:
            speckleLoadFrame.direction = (MyType[index] == 1) ? LoadDirection.Y : LoadDirection.YY;
            speckleLoadFrame.loadAxisType = Structural.LoadAxisType.Local;
            break;
          case 3:
            speckleLoadFrame.direction = (MyType[index] == 1) ? LoadDirection.Z : LoadDirection.ZZ;
            speckleLoadFrame.loadAxisType = Structural.LoadAxisType.Local;
            break;
          case 4:
            speckleLoadFrame.direction = (MyType[index] == 1) ? LoadDirection.X : LoadDirection.XX;
            speckleLoadFrame.loadAxisType = Structural.LoadAxisType.Global;
            break;
          case 5:
            speckleLoadFrame.direction = (MyType[index] == 1) ? LoadDirection.Y : LoadDirection.YY;
            speckleLoadFrame.loadAxisType = Structural.LoadAxisType.Global;
            break;
          case 6:
            speckleLoadFrame.direction = (MyType[index] == 1) ? LoadDirection.Z : LoadDirection.ZZ;
            speckleLoadFrame.loadAxisType = Structural.LoadAxisType.Global;
            break;
          case 7:
            speckleLoadFrame.direction = (MyType[index] == 1) ? LoadDirection.X : LoadDirection.XX;
            speckleLoadFrame.isProjected = true;
            speckleLoadFrame.loadAxisType = Structural.LoadAxisType.Global;
            break;
          case 8:
            speckleLoadFrame.direction = (MyType[index] == 1) ? LoadDirection.Y : LoadDirection.YY;
            speckleLoadFrame.isProjected = true;
            speckleLoadFrame.loadAxisType = Structural.LoadAxisType.Global;
            break;
          case 9:
            speckleLoadFrame.direction = (MyType[index] == 1) ? LoadDirection.Z : LoadDirection.ZZ;
            speckleLoadFrame.isProjected = true;
            speckleLoadFrame.loadAxisType = Structural.LoadAxisType.Global;
            break;
          case 10:
            speckleLoadFrame.direction = (MyType[index] == 1) ? LoadDirection.Z : LoadDirection.ZZ;
            speckleLoadFrame.loadAxisType = Structural.LoadAxisType.Global;
            break;
          case 11:
            speckleLoadFrame.direction = (MyType[index] == 1) ? LoadDirection.Z : LoadDirection.ZZ;
            speckleLoadFrame.isProjected = true;
            speckleLoadFrame.loadAxisType = Structural.LoadAxisType.Global;
            break;
        }
        if (speckleLoadFrame.values == null)
        {
          speckleLoadFrame.values = new List<double> { };
        }
        speckleLoadFrame.values.Add(val1[index]);
        speckleLoadFrame.values.Add(val2[index]);
        if (speckleLoadFrame.positions == null)
        {
          speckleLoadFrame.positions = new List<double> { };
        }
        speckleLoadFrame.positions.Add(dist1[index]);
        speckleLoadFrame.positions.Add(dist2[index]);
        speckleLoadFrame.loadType = BeamLoadType.Uniform;
        speckleLoadFrame.loadCase = LoadPatternCaseToSpeckle(loadPat[index]);
        _loadStoringBeam[loadID] = speckleLoadFrame;
      }
      _counterFrame += 1;

      if (_counterFrame == frameNumber)
      {
        foreach (var entry in _loadStoringBeam.Keys)
        {
          _loadStoringBeam.TryGetValue(entry, out var loadBeam);
          _frameStoring.TryGetValue(entry, out var elements);
          loadBeam.elements = elements;
          SpeckleModel.loads.Add(loadBeam);
        }
        _counterFrame = 0;
      }
    }

    double[] dist = null;
    double[] relDist = null;
    double[] val = null;
    s = Model.FrameObj.GetLoadPoint(
      name,
      ref numberItems,
      ref frameName,
      ref loadPat,
      ref MyType,
      ref csys,
      ref dir,
      ref relDist,
      ref dist,
      ref val
    );
    if (s == 0)
    {
      foreach (int index in Enumerable.Range(0, numberItems))
      {
        var speckleLoadFrame = new LoadBeam();
        var element = FrameToSpeckle(frameName[index]);
        var loadID = String.Concat(loadPat[index], val, dist[index], dir[index], MyType[index]);
        speckleLoadFrame.applicationId = loadID;
        _frameStoring.TryGetValue(loadID, out var element1DList);
        if (element1DList == null)
        {
          element1DList = new List<Base> { };
        }
        if (!element1DList.Select(el => el.applicationId).Contains(element.applicationId))
        {
          element1DList.Add(element);
        }

        _frameStoring[loadID] = element1DList;

        switch (dir[index])
        {
          case 1:
            speckleLoadFrame.direction = (MyType[index] == 1) ? LoadDirection.X : LoadDirection.XX;
            speckleLoadFrame.loadAxisType = Structural.LoadAxisType.Local;
            break;
          case 2:
            speckleLoadFrame.direction = (MyType[index] == 1) ? LoadDirection.Y : LoadDirection.YY;
            speckleLoadFrame.loadAxisType = Structural.LoadAxisType.Local;
            break;
          case 3:
            speckleLoadFrame.direction = (MyType[index] == 1) ? LoadDirection.Z : LoadDirection.ZZ;
            speckleLoadFrame.loadAxisType = Structural.LoadAxisType.Local;
            break;
          case 4:
            speckleLoadFrame.direction = (MyType[index] == 1) ? LoadDirection.X : LoadDirection.XX;
            speckleLoadFrame.loadAxisType = Structural.LoadAxisType.Global;
            break;
          case 5:
            speckleLoadFrame.direction = (MyType[index] == 1) ? LoadDirection.Y : LoadDirection.YY;
            speckleLoadFrame.loadAxisType = Structural.LoadAxisType.Global;
            break;
          case 6:
            speckleLoadFrame.direction = (MyType[index] == 1) ? LoadDirection.Z : LoadDirection.ZZ;
            speckleLoadFrame.loadAxisType = Structural.LoadAxisType.Global;
            break;
          case 7:
            speckleLoadFrame.direction = (MyType[index] == 1) ? LoadDirection.X : LoadDirection.XX;
            speckleLoadFrame.isProjected = true;
            speckleLoadFrame.loadAxisType = Structural.LoadAxisType.Global;
            break;
          case 8:
            speckleLoadFrame.direction = (MyType[index] == 1) ? LoadDirection.Y : LoadDirection.YY;
            speckleLoadFrame.isProjected = true;
            speckleLoadFrame.loadAxisType = Structural.LoadAxisType.Global;
            break;
          case 9:
            speckleLoadFrame.direction = (MyType[index] == 1) ? LoadDirection.Z : LoadDirection.ZZ;
            speckleLoadFrame.isProjected = true;
            speckleLoadFrame.loadAxisType = Structural.LoadAxisType.Global;
            break;
          case 10:
            speckleLoadFrame.direction = (MyType[index] == 1) ? LoadDirection.Z : LoadDirection.ZZ;
            speckleLoadFrame.loadAxisType = Structural.LoadAxisType.Global;
            break;
          case 11:
            speckleLoadFrame.direction = (MyType[index] == 1) ? LoadDirection.Z : LoadDirection.ZZ;
            speckleLoadFrame.loadAxisType = Structural.LoadAxisType.Global;
            break;
        }
        if (speckleLoadFrame.values == null)
        {
          speckleLoadFrame.values = new List<double> { };
        }
        speckleLoadFrame.values.Add(val[index]);

        if (speckleLoadFrame.positions == null)
        {
          speckleLoadFrame.positions = new List<double> { };
        }
        speckleLoadFrame.positions.Add(dist[index]);
        speckleLoadFrame.loadType = BeamLoadType.Point;
        speckleLoadFrame.loadCase = LoadPatternCaseToSpeckle(loadPat[index]);
        _loadStoringBeam[loadID] = speckleLoadFrame;
      }
      _counterFrame += 1;

      if (_counterFrame == frameNumber)
      {
        foreach (var entry in _loadStoringBeam.Keys)
        {
          _loadStoringBeam.TryGetValue(entry, out var loadBeam);
          _frameStoring.TryGetValue(entry, out var elements);
          loadBeam.elements = elements;
          SpeckleModel.loads.Add(loadBeam);
        }
        _counterFrame = 0;
      }
    }
    var speckleObject = new Base();
    return speckleObject;
  }
}
