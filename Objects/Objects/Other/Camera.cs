namespace Objects.Other
{
  public class Camera : View
  {
    public SensorFit sensorFit { get; set; } = SensorFit.Auto;
    public double sensorHeight { get; set; } = 24;
    public double sensorWidth { get; set; } = 36;

    // (mayb for l8r) shift X and shift Y
  }

  public enum SensorFit
  {
    Auto,
    Horizontal,
    Vertical
  }
}
