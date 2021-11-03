using ETABSv1;
using Objects.Structural.Loading;
using System;
using System.Collections.Generic;
using Objects.Structural.Geometry;
using System.Linq;
using System.Text;
using Speckle.Core.Models;

namespace Objects.Converter.ETABS
{
    public partial class ConverterETABS
    {

        Dictionary<string, LoadFace> LoadStoringArea = new Dictionary<string, LoadFace>();
        Dictionary<string, List<Base>> AreaStoring = new Dictionary<string, List<Base>>();
        int counterArea = 0;
        Base LoadFaceToSpeckle(string name,int areaNumber)
        {


            int numberItems = 0;
            string[] areaName = null;
            string[] loadPat = null;
            string[] csys = null;
            int[] dir = null;
            double[] value = null;
            int s = Model.AreaObj.GetLoadUniform(name,ref numberItems,ref areaName, ref loadPat, ref csys, ref dir, ref value);
            if (s == 0)
            {
                foreach(int index in Enumerable.Range(0, numberItems))
                {
                    var speckleLoadFace = new LoadFace();
                    var element = AreaToSpeckle(areaName[index]);
                    var loadID = string.Concat(loadPat[index], csys[index], dir[index], value[index]);
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
                            speckleLoadFace.direction = LoadDirection2D.X ;
                            speckleLoadFace.loadAxisType = Structural.LoadAxisType.Global;
                            break;
                        case 5:
                            speckleLoadFace.direction =  LoadDirection2D.Y;
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
                    if(speckleLoadFace.values == null) { speckleLoadFace.values = new List<double> { }; }
                    speckleLoadFace.values.Add(value[index]);
                    //speckleLoadFace.loadCase = LoadPatternCaseToSpeckle(loadPat[index]);
                    LoadStoringArea[loadID] = speckleLoadFace;

                }
                counterArea += 1;
                if(counterArea == areaNumber)
                {
                    foreach(var entry in LoadStoringArea.Keys)
                    {
                        LoadStoringArea.TryGetValue(entry, out var loadFace);
                        AreaStoring.TryGetValue(entry, out var areas);
                        loadFace.elements = areas;
                        SpeckleModel.loads.Add(loadFace);
                    }
                    counterArea=0;
                }

            }



            var speckleBase = new Base();
            return speckleBase;
        }
            
    }
}
