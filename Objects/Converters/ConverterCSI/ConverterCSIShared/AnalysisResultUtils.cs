using ConverterCSIShared.Models;
using CSiAPIv1;

namespace ConverterCSIShared;

internal static class AnalysisResultUtils
{
  public delegate int ApiResultMethod(
    string name,
    eItemTypeElm eItemType,
    ref int numItems,
    ref string[] objs,
    ref string[] elms,
    ref string[] loadCombos,
    ref string[] stepTypes,
    ref double[] stepNum,
    ref double[] x,
    ref double[] y,
    ref double[] z,
    ref double[] xx,
    ref double[] yy,
    ref double[] zz
  );

  public static bool TryGetAPIResult(
    ApiResultMethod apiResultMethod,
    string nodeName,
    out int numberResults,
    out string[] obj,
    out string[] elm,
    out string[] loadCases,
    out string[] stepType,
    out double[] stepNum,
    out double[] F1,
    out double[] F2,
    out double[] F3,
    out double[] M1,
    out double[] M2,
    out double[] M3,
    bool shouldGetResults = true
  )
  {
    numberResults = 0;
    obj = null;
    elm = null;
    loadCases = null;
    stepType = null;
    stepNum = null;
    F1 = null;
    F2 = null;
    F3 = null;
    M1 = null;
    M2 = null;
    M3 = null;

    if (!shouldGetResults)
    {
      return false;
    }

    int success = apiResultMethod(
      nodeName,
      eItemTypeElm.Element,
      ref numberResults,
      ref obj,
      ref elm,
      ref loadCases,
      ref stepType,
      ref stepNum,
      ref F1,
      ref F2,
      ref F3,
      ref M1,
      ref M2,
      ref M3
    );
    return ApiResultValidator.IsSuccessful(success);
  }
}
