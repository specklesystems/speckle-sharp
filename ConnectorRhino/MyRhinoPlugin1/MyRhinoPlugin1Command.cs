using Rhino;
using Rhino.Commands;
using Rhino.Geometry;
using Rhino.Input;
using Rhino.Input.Custom;
using System;
using System.Collections.Generic;

namespace MyRhinoPlugin1
{
  public class MyRhinoPlugin1Command : Command
  {
    public MyRhinoPlugin1Command()
    {
      // Rhino only creates one instance of each command class defined in a
      // plug-in, so it is safe to store a refence in a static property.
      Instance = this;
    }

    ///<summary>The only instance of this command.</summary>
    public static MyRhinoPlugin1Command Instance { get; private set; }

    ///<returns>The command name as it appears on the Rhino command line.</returns>
    public override string EnglishName => "MyRhinoPlugin1Command";

    protected override Result RunCommand(RhinoDoc doc, RunMode mode)
    {
      RhinoApp.WriteLine("The {0} command is under construction.", EnglishName);
      return Result.Success;
    }
  }
}
