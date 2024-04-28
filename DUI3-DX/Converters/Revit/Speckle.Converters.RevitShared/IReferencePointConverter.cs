public interface IReferencePointConverter
{
  DB.XYZ ConvertToExternalCoordindates(DB.XYZ inbound, bool isPoint);
}
