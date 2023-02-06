using Objects.Structural.Geometry;
using Objects.Structural.Results;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Objects.Converter.CSI
{
  public partial class ConverterCSI
  {
    public ResultSet2D AreaResultSet2dToSpeckle(List<string> areaNames)
    {
      List<Result2D> results = new List<Result2D>();

      foreach (var areaName in areaNames)
      {
        var element = SpeckleModel.elements.Where(o => (string)o["name"] == areaName && o is Element2D).FirstOrDefault() as Element2D;
        // if the element is null, then it was not part of the user's selection, so don't send its results
        if (element == null)
          continue;

        #region Return force results
        int numberOfForceResults = 0;
        string[] obj, elm, pointElm, loadCase, stepType;
        double[] stepNum, f11, f22, f12, fMax, fMin, fAngle, fVonMises, m11, m22, m12, mMax, mMin, mAngle, v13, v23, vMax, vAngle;
        obj = elm = pointElm = loadCase = stepType = new string[] { };
        stepNum = f11 = f22 = f12 = fMax = fMin = fAngle = fVonMises = m11 = m22 = m12 = mMax = mMin = mAngle = v13 = v23 = vMax = vAngle = new double[] { };

        Model.Results.AreaForceShell(areaName, CSiAPIv1.eItemTypeElm.ObjectElm, ref numberOfForceResults, ref obj, ref elm, ref pointElm, ref loadCase, ref stepType, ref stepNum, ref f11, ref f22, ref f12, ref fMax, ref fMin, ref fAngle, ref fVonMises, ref m11, ref m22, ref m12, ref mMax, ref mMin, ref mAngle, ref v13, ref v23, ref vMax, ref vAngle);
        #endregion

        #region Return stress results
        int numberOfStressResults = 0;
        string[] stressObj, stressElm, stressPointElm, stressLoadCase, stressStepType;
        double[] stressStepNum, S11Top, S22Top, S12Top, SMaxTop, SMinTop, SAngleTop, sVonMisesTop, S11Bot, S22Bot, S12Bot, SMaxBot, SMinBot, SAngleBot, sVonMisesBot, S13Avg, S23Avg, SMaxAvg, SAngleAvg;
        stressObj = stressElm = stressPointElm = stressLoadCase = stressStepType = new string[] { };
        stressStepNum = S11Top = S22Top = S12Top = SMaxTop = SMinTop = SAngleTop = sVonMisesTop = S11Bot = S22Bot = S12Bot = SMaxBot = SMinBot = SAngleBot = sVonMisesBot = S13Avg = S23Avg = SMaxAvg = SAngleAvg = new double[] { };

        Model.Results.AreaStressShell(areaName, CSiAPIv1.eItemTypeElm.ObjectElm, ref numberOfStressResults, ref stressObj, ref stressElm, ref stressPointElm, ref stressLoadCase, ref stressStepType, ref stressStepNum, ref S11Top, ref S22Top, ref S12Top, ref SMaxTop, ref SMinTop, ref SAngleTop, ref sVonMisesTop, ref S11Bot, ref S22Bot, ref S12Bot, ref SMaxBot, ref SMinBot, ref SAngleBot, ref sVonMisesBot, ref S13Avg, ref S23Avg, ref SMaxAvg, ref SAngleAvg);
        #endregion

        for (int i = 0; i < numberOfForceResults; i++)
        {
          results.Add(new Result2D
          {
            element = element, //AreaToSpeckle(areaName),
            permutation = loadCase[i],
            position = new List<double>(),
            dispX = 0, // pulling this data would require large amount of data parsing, implementation TBD
            dispY = 0, // pulling this data would require large amount of data parsing, implementation TBD
            dispZ = 0, // pulling this data would require large amount of data parsing, implementation TBD
            forceXX = (float)f11[i],
            forceYY = (float)f22[i],
            forceXY = (float)f12[i],
            momentXX = (float)m11[i],
            momentYY = (float)m22[i],
            momentXY = (float)m12[i],
            shearX = (float)v13[i],
            shearY = (float)v23[i],
            stressTopXX = (float)S11Top[i],
            stressTopYY = (float)S22Top[i],
            stressTopZZ = 0, // shell elements are 2D elements
            stressTopXY = (float)S12Top[i],
            stressTopYZ = (float)S23Avg[i], // CSI reports avg out-of-plane shear
            stressTopZX = (float)S12Top[i],
            stressMidXX = 0, // CSI does not report
            stressMidYY = 0, // CSI does not report
            stressMidZZ = 0, // CSI does not report
            stressMidXY = 0, // CSI does not report
            stressMidYZ = 0, // CSI does not report
            stressMidZX = 0, // CSI does not report
            stressBotXX = (float)S11Bot[i],
            stressBotYY = (float)S22Bot[i],
            stressBotZZ = 0, // shell elements are 2D elements
            stressBotXY = (float)S12Bot[i],
            stressBotYZ = (float)S23Avg[i], // CSI reports avg out-of-plane shear
            stressBotZX = (float)S12Bot[i],
          });
        }
      }

      return new ResultSet2D { results2D = results };
    }
  }
}
