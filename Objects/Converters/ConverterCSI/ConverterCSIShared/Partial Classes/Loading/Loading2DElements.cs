using CSiAPIv1;
using Objects.Structural.Loading;
using System;
using System.Collections.Generic;
using Objects.Structural.Geometry;
using System.Linq;
using System.Text;
using Speckle.Core.Models;
using Objects.Structural.CSI.Loading;

namespace Objects.Converter.CSI
{
  public partial class ConverterCSI
  {
    Dictionary<string, LoadFace> LoadStoringArea = new Dictionary<string, LoadFace>();
    Dictionary<string, List<Base>> AreaStoring = new Dictionary<string, List<Base>>();
    int counterAreaLoadUniform = 0;
    int counterAreaLoadWind = 0;
    void LoadFaceToNative(LoadFace loadFace, ref ApplicationObject appObj)
    {
      foreach (Element2D element in loadFace.elements)
      {
        string elementName = null;
        if (element.name != null) 
          elementName = element.name;
        else 
          elementName = element.id;

        if (loadFace.loadType != FaceLoadType.Constant) // TODO: support other load types
          continue;

        int? success = null;
        switch (loadFace.direction)
        {
          case LoadDirection2D.X:
            success = Model.AreaObj.SetLoadUniform(elementName, loadFace.loadCase.name, loadFace.values[0], 4);
            break;
          case LoadDirection2D.Y:
            success = Model.AreaObj.SetLoadUniform(elementName, loadFace.loadCase.name, loadFace.values[0], 5);
            break;
          case LoadDirection2D.Z:
            success = Model.AreaObj.SetLoadUniform(elementName, loadFace.loadCase.name, loadFace.values[0], 6);
            break;
        }

        if (success == 0)
          appObj.Update(status: ApplicationObject.State.Created, createdId: $"{loadFace.name}");
        else
          appObj.Update(status: ApplicationObject.State.Failed);
      }
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
      int s = Model.AreaObj.GetLoadUniform(name, ref numberItems, ref areaName, ref loadPat, ref csys, ref dir, ref value);
      if (s == 0)
      {
        foreach (int index in Enumerable.Range(0, numberItems))
        {
          var speckleLoadFace = new LoadFace();
          var element = AreaToSpeckle(areaName[index]);
          var loadID = string.Concat(loadPat[index], csys[index], dir[index], value[index]);
          speckleLoadFace.applicationId = loadID;
          AreaStoring.TryGetValue(loadID, out var element2Dlist);
          if (element2Dlist == null) { element2Dlist = new List<Base> { }; }
          element2Dlist.Add(element);
          AreaStoring[loadID] = element2Dlist;

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
          if (speckleLoadFace.values == null) { speckleLoadFace.values = new List<double> { }; }
          speckleLoadFace.values.Add(value[index]);
          speckleLoadFace.loadCase = LoadPatternCaseToSpeckle(loadPat[index]);
          LoadStoringArea[loadID] = speckleLoadFace;
          speckleLoadFace.loadType = FaceLoadType.Constant;
        }
        counterAreaLoadUniform += 1;
        if (counterAreaLoadUniform == areaNumber)
        {
          foreach (var entry in LoadStoringArea.Keys)
          {
            LoadStoringArea.TryGetValue(entry, out var loadFace);
            AreaStoring.TryGetValue(entry, out var areas);
            loadFace.elements = areas;
            SpeckleModel.loads.Add(loadFace);
          }
          counterAreaLoadUniform = 0;
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
          AreaStoring.TryGetValue(loadID, out var element2Dlist);
          if (element2Dlist == null) { element2Dlist = new List<Base> { }; }
          element2Dlist.Add(element);
          AreaStoring[loadID] = element2Dlist;
          if (speckleLoadFace.values == null) { speckleLoadFace.values = new List<double> { }; }
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
          LoadStoringArea[loadID] = speckleLoadFace;
        }
        counterAreaLoadWind += 1;
        if (counterAreaLoadWind == areaNumber)
        {
          foreach (var entry in LoadStoringArea.Keys)
          {
            LoadStoringArea.TryGetValue(entry, out var loadFace);
            AreaStoring.TryGetValue(entry, out var areas);
            loadFace.elements = areas;
            SpeckleModel.loads.Add(loadFace);
          }
          counterAreaLoadWind = 0;
        }
      }
      var speckleBase = new Base();
      return speckleBase;
    }

  }
}