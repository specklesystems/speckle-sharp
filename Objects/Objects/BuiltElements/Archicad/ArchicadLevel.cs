namespace Objects.BuiltElements.Archicad;

/*
For further informations about given the variables, visit:
https://archicadapi.graphisoft.com/documentation/api_storytype
*/
public class ArchicadLevel : Level
{
  public short index { get; set; }

  public ArchicadLevel() { }

  public ArchicadLevel(string name, double elevation, short index)
  {
    this.name = name;
    this.elevation = elevation;
    this.index = index;
  }

  public ArchicadLevel(string name)
  {
    this.name = name;
  }
}
