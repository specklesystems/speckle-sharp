namespace Speckle.Converters.RevitShared;

public interface IReferencePointConverter
{
  DB.XYZ ConvertToExternalCoordindates(DB.XYZ p, bool isPoint);

  DB.XYZ ToInternalCoordinates(DB.XYZ p, bool isPoint);
}
