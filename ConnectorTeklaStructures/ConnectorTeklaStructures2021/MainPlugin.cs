using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using TSDatatype = Tekla.Structures.Datatype;
using TSModel = Tekla.Structures.Model;
using Tekla.Structures.Plugins;

namespace ConnectorTeklaStructures2021
{
  public class StructuresData
  {
    [StructuresField("ATTRIBUTE_NAME")]
    public string name;

  }
  [Plugin("FormPlugin")]
  [PluginUserInterface("FormPlugin.MainForm")]
  public class MainPlugin : PluginBase
  {
    // Enable inserting of objects in a model
    private readonly TSModel.Model _model;

    public TSModel.Model Model
    {
      get { return _model; }
    }
    public override List<InputDefinition> DefineInput()
    {
      return null;
      // Define input objects.     
    }

    // Enable retrieving of input values
    private readonly StructuresData _data;

    public MainPlugin(StructuresData data)
    {
      // Link to model.         
      _model = new TSModel.Model();

      // Link to input values.         
      _data = data;
    }

    // Specify the user input needed for the plugin.

    // This method is called upon execution of the plug-in and it´s the main method of the plug-in
    public override bool Run(List<InputDefinition> input)
    {
      try
      {
        Console.WriteLine("Testing");
        // Write your code here.         
      }
      catch (Exception e)
      {
        MessageBox.Show(e.ToString());
      }
      return true;
    }
  }
}
