namespace Speckle.Converters.RevitShared;

public interface IReferencePointConverter
{
  DB.XYZ ConvertToExternalCoordindates(DB.XYZ inbound, bool isPoint);

  DB.XYZ ToInternalCoordinates(DB.XYZ p, bool isPoint);
}
