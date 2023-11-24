using Objects.Structural.Results;

namespace Objects.Converter.CSI;

public partial class ConverterCSI
{
  public ResultGlobal ResultGlobal()
  {
    var speckleResultGlobal = new ResultGlobal();

    int numberResult = 0;
    string[] loadcase = null;
    string[] stepType = null;
    double[] stepNum = null;
    double[] period = null;
    double[] frequency = null;
    double[] circFreq = null;
    double[] eigenValue = null;
    var s = Model.Results.ModalPeriod(
      ref numberResult,
      ref loadcase,
      ref stepType,
      ref stepNum,
      ref period,
      ref frequency,
      ref circFreq,
      ref eigenValue
    );

    double[] UX = null;
    double[] UY = null;
    double[] UZ = null;
    double[] RX = null;
    double[] RY = null;
    double[] RZ = null;
    double[] ModalMass = null;
    double[] ModalStiff = null;

    var i = Model.Results.ModalParticipationFactors(
      ref numberResult,
      ref loadcase,
      ref stepType,
      ref stepNum,
      ref period,
      ref UX,
      ref UY,
      ref UZ,
      ref RX,
      ref RY,
      ref RZ,
      ref ModalMass,
      ref ModalStiff
    );

    if (s == 0 && i == 0)
    {
      speckleResultGlobal.modalStiffness = (float)ModalStiff[0];
      speckleResultGlobal.reactionX = (float)UX[0];
      speckleResultGlobal.reactionY = (float)UY[0];
      speckleResultGlobal.reactionZ = (float)UZ[0];
      speckleResultGlobal.reactionXX = (float)RX[0];
      speckleResultGlobal.reactionYY = (float)RY[0];
      speckleResultGlobal.reactionZZ = (float)RZ[0];
      speckleResultGlobal.effMassX =
        speckleResultGlobal.effMassXX =
        speckleResultGlobal.effMassY =
        speckleResultGlobal.effMassYY =
        speckleResultGlobal.effMassZ =
        speckleResultGlobal.effMassZZ =
          (float)ModalMass[0];

      speckleResultGlobal.mode = (float)stepNum[0];
      speckleResultGlobal.frequency = (float)frequency[0];
    }

    return speckleResultGlobal;
  }
}
