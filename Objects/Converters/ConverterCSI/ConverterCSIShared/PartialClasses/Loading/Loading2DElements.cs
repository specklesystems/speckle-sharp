using Objects.Structural.Loading;
using System;
using System.Collections.Generic;
using Objects.Structural.Geometry;
using System.Linq;
using Speckle.Core.Models;
using Objects.Structural.CSI.Loading;
using Speckle.Core.Kits;

namespace Objects.Converter.CSI;

public partial class ConverterCSI
{
  readonly Dictionary<string, LoadFace> _loadStoringArea = new();
  readonly Dictionary<string, List<Base>> _areaStoring = new();
  int _counterAreaLoadUniform;
  int _counterAreaLoadWind;

  string LoadFaceToNative(LoadFace loadFace, IList<string>? notes)
  {
    List<string> convertedNames = new(loadFace.elements.Count);
    foreach (Base e in loadFace.elements)
    {
      if (e is not Element2D element)
      {
        notes?.Add($"Expected all elements to be of type {nameof(Element2D)}, other types ignored");
        continue;
      }

      if (loadFace.loadType != FaceLoadType.Constant) // TODO: support other load types
      {
        notes?.Add($"Only {nameof(FaceLoadType.Constant)} are supported, other types ignored");
        continue;
      }

      string elementName = element.name ?? element.id;

      int dir = loadFace.direction switch
      {
        LoadDirection2D.X => 4,
        LoadDirection2D.Y => 5,
        LoadDirection2D.Z => 6,
        _ => throw new ArgumentOutOfRangeException($"Unrecognised {nameof(LoadDirection2D)} {loadFace.direction}")
      };

      int success = Model.AreaObj.SetLoadUniform(elementName, loadFace.loadCase.name, loadFace.values[0], dir);

      //TODO: if one element fails, should all?
      if (success != 0)
      {
        notes?.Add($"Failed to assign uniform load to sub-element {elementName}");
        continue;
      }
      convertedNames.Add(elementName);
    }

    if (!convertedNames.Any())
    {
      throw new ConversionException($"Zero out of {loadFace.elements.Count} sub-elements converted successfully");
    }

    return loadFace.name; //TODO: why return loadFace name here? is there an object with that name?
  }

  void LoadWindToNative(CSIWindLoadingFace windLoadingFace)
  {
    foreach (Element2D element in windLoadingFace.elements)
    {
      switch (windLoadingFace.WindPressureType)
      {
        case Structural.CSI.Analysis.WindPressureType.Windward:
          Model.AreaObj.SetLoadWindPressure(element.name, windLoadingFace.loadCase.name, 1, windLoadingFace.Cp);
          break;
        case Structural.CSI.Analysis.WindPressureType.other:
          Model.AreaObj.SetLoadWindPressure(element.name, windLoadingFace.loadCase.name, 2, windLoadingFace.Cp);
          break;
      }
    }
  }

  Base LoadFaceToSpeckle(string name, int areaNumber)
  {
    int numberItems = 0;
    string[] areaName = null;
    string[] loadPat = null;
    string[] csys = null;
    int[] dir = null;
    double[] value = null;
    int s = Model.AreaObj.GetLoadUniform(
      name,
      ref numberItems,
      ref areaName,
      ref loadPat,
      ref csys,
      ref dir,
      ref value
    );
    if (s == 0)
    {
      foreach (int index in Enumerable.Range(0, numberItems))
      {
        var speckleLoadFace = new LoadFace();
        var element = AreaToSpeckle(areaName[index]);
        var loadID = string.Concat(loadPat[index], csys[index], dir[index], value[index]);
        speckleLoadFace.applicationId = loadID;
        _areaStoring.TryGetValue(loadID, out var element2Dlist);
        if (element2Dlist == null)
        {
          element2Dlist = new List<Base> { };
        }
        element2Dlist.Add(element);
        _areaStoring[loadID] = element2Dlist;

        switch (dir[index])
        {
          case 1:
            speckleLoadFace.direction = LoadDirection2D.X;
            speckleLoadFace.loadAxisType = Structural.LoadAxisType.Local;
            break;
          case 2:
            speckleLoadFace.direction = LoadDirection2D.Y;
            speckleLoadFace.loadAxisType = Structural.LoadAxisType.Local;
            break;
          case 3:
            speckleLoadFace.direction = LoadDirection2D.Z;
            speckleLoadFace.loadAxisType = Structural.LoadAxisType.Local;
            break;
          case 4:
            speckleLoadFace.direction = LoadDirection2D.X;
            speckleLoadFace.loadAxisType = Structural.LoadAxisType.Global;
            break;
          case 5:
            speckleLoadFace.direction = LoadDirection2D.Y;
            speckleLoadFace.loadAxisType = Structural.LoadAxisType.Global;
            break;
          case 6:
            speckleLoadFace.direction = LoadDirection2D.Z;
            speckleLoadFace.loadAxisType = Structural.LoadAxisType.Global;
            break;
          case 7:
            speckleLoadFace.direction = LoadDirection2D.X;
            speckleLoadFace.isProjected = true;
            speckleLoadFace.loadAxisType = Structural.LoadAxisType.Global;
            break;
          case 8:
            speckleLoadFace.direction = LoadDirection2D.Y;
            speckleLoadFace.isProjected = true;
            speckleLoadFace.loadAxisType = Structural.LoadAxisType.Global;
            break;
          case 9:
            speckleLoadFace.direction = LoadDirection2D.Z;
            speckleLoadFace.isProjected = true;
            speckleLoadFace.loadAxisType = Structural.LoadAxisType.Global;
            break;
          case 10:
            speckleLoadFace.direction = LoadDirection2D.Z;
            speckleLoadFace.loadAxisType = Structural.LoadAxisType.Global;
            break;
          case 11:
            speckleLoadFace.direction = LoadDirection2D.Z;
            speckleLoadFace.loadAxisType = Structural.LoadAxisType.Global;
            break;
        }
        if (speckleLoadFace.values == null)
        {
          speckleLoadFace.values = new List<double> { };
        }
        speckleLoadFace.values.Add(value[index]);
        speckleLoadFace.loadCase = LoadPatternCaseToSpeckle(loadPat[index]);
        _loadStoringArea[loadID] = speckleLoadFace;
        speckleLoadFace.loadType = FaceLoadType.Constant;
      }
      _counterAreaLoadUniform += 1;
      if (_counterAreaLoadUniform == areaNumber)
      {
        foreach (var entry in _loadStoringArea.Keys)
        {
          _loadStoringArea.TryGetValue(entry, out var loadFace);
          _areaStoring.TryGetValue(entry, out var areas);
          loadFace.elements = areas;
          SpeckleModel.loads.Add(loadFace);
        }
        _counterAreaLoadUniform = 0;
      }
    }

    int[] myType = null;
    double[] cp = null;
    s = Model.AreaObj.GetLoadWindPressure(name, ref numberItems, ref areaName, ref loadPat, ref myType, ref cp);
    if (s == 0)
    {
      foreach (int index in Enumerable.Range(0, numberItems))
      {
        var speckleLoadFace = new CSIWindLoadingFace();
        var element = AreaToSpeckle(areaName[index]);
        var loadID = string.Concat(loadPat[index], myType[index], cp[index]);
        speckleLoadFace.applicationId = loadID;
        _areaStoring.TryGetValue(loadID, out var element2Dlist);
        if (element2Dlist == null)
        {
          element2Dlist = new List<Base> { };
        }
        element2Dlist.Add(element);
        _areaStoring[loadID] = element2Dlist;
        if (speckleLoadFace.values == null)
        {
          speckleLoadFace.values = new List<double> { };
        }
        speckleLoadFace.Cp = cp[index];
        switch (myType[index])
        {
          case 1:
            speckleLoadFace.WindPressureType = Structural.CSI.Analysis.WindPressureType.Windward;
            break;
          case 2:
            speckleLoadFace.WindPressureType = Structural.CSI.Analysis.WindPressureType.other;
            break;
        }
        //speckleLoadFace.loadCase = LoadPatternCaseToSpeckle(loadPat[index]);
        _loadStoringArea[loadID] = speckleLoadFace;
      }
      _counterAreaLoadWind += 1;
      if (_counterAreaLoadWind == areaNumber)
      {
        foreach (var entry in _loadStoringArea.Keys)
        {
          _loadStoringArea.TryGetValue(entry, out var loadFace);
          _areaStoring.TryGetValue(entry, out var areas);
          loadFace.elements = areas;
          SpeckleModel.loads.Add(loadFace);
        }
        _counterAreaLoadWind = 0;
      }
    }
    var speckleBase = new Base();
    return speckleBase;
  }
}
