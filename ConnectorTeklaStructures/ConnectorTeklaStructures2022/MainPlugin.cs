using System;
using System.Collections.Generic;
using Tekla.Structures.Plugins;


namespace Speckle.ConnectorTeklaStructures
{
  public class StructuresData
  {
  }
  [Plugin("Speckle.ConnectorTeklaStructures")]
  [PluginUserInterface("Speckle.ConnectorTeklaStructures.MainForm")]
  [InputObjectDependency(InputObjectDependency.NOT_DEPENDENT)]

  public class MainPlugin : PluginBase
  {
    public override List<InputDefinition> DefineInput()
    {
      return new List<InputDefinition>();
      // Define input objects.     
    }

    // Enable retrieving of input values
    private readonly StructuresData _data;

    public MainPlugin(StructuresData data)
    {

    }

    // This method is called upon execution of the plug-in and it´s the main method of the plug-in
    public override bool Run(List<InputDefinition> input)
    {
      return true;
    }
  }
}
