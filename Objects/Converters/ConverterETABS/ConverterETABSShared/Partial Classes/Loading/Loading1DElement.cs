using ETABSv1;
using Objects.Structural.Loading;
using System;
using System.Collections.Generic;
using Objects.Structural.Geometry;
using System.Linq;
using System.Text;

namespace Objects.Converter.ETABS
{
    public partial class ConverterETABS
    {
        
        Dictionary<string, LoadBeam> LoadStoringBeam = new Dictionary<string, LoadBeam>();
        Dictionary<string, List<Element1D>> FrameStoring = new Dictionary<string, List<Element1D>>();
            void LoadFrameToSpeckle(string name)
        {
            int numberItems = 0;
            string[] frameName = null;
            string[] loadPat = null;
            int[] MyType = null;
            string[] csys = null;
            int[] dir = null;
            double[] RD1 = null;
            double[] RD2 = null;
            double[] dist1 =null;
            double[] dist2 =null;
            double[] val1 = null;
            double[] val2 = null;

            var element1DList = new List<Element1D>();
            
            Model.FrameObj.GetLoadDistributed(name, ref numberItems, ref frameName,ref loadPat,ref MyType,ref csys, ref dir, ref RD1, ref RD2 , ref dist1, ref dist2, ref val1, ref val2);
            foreach (int index in Enumerable.Range(0, numberItems))
            {
                var speckleLoadFrame = new LoadBeam();
                var element = FrameToSpeckle(frameName[index]);
                var loadID = String.Concat(loadPat[index],val1[index],val2[index],dist1[index],dist2[index],dir[index],MyType[index]);
                FrameStoring.TryGetValue(loadID, out element1DList);
                element1DList.Add(element);
                FrameStoring[loadID] = element1DList;

                switch (dir[index]) {
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
                speckleLoadFrame.values.Add(val1[index]);
                speckleLoadFrame.values.Add(val2[index]);
                speckleLoadFrame.positions.Add(dist1[index]);
                speckleLoadFrame.positions.Add(dist2[index]);
                speckleLoadFrame.loadType = BeamLoadType.Uniform;
                speckleLoadFrame.loadCase = LoadPatternToSpeckle(loadPat[index]);
                LoadStoringBeam[loadID] = speckleLoadFrame;
            }

            return;
        }
    }
}
