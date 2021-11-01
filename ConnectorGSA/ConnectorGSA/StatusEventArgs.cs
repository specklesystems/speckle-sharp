using System;

namespace ConnectorGSA
{
  public class StatusEventArgs : EventArgs
  {
    private readonly double percent;
    private readonly string name;

    public StatusEventArgs(string name, double percent)
    {
      this.name = name;
      this.percent = percent;
    }

    public double Percent
    {
      get { return percent; }
    }

    public string Name
    {
      get { return name; }
    }
  }
}
