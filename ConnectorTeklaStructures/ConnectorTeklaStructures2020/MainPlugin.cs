using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TSDatatype = Tekla.Structures.Datatype;
using Tekla.Structures.Model;
using Tekla.Structures.Plugins;
using Tekla.Structures;
using Tekla.Structures.Geometry3d;
using Tekla.Structures.Model.UI;
using System.Reflection;

using Avalonia;
using Avalonia.Controls;
using Avalonia.ReactiveUI;

using DesktopUI2.ViewModels;
using DesktopUI2.Views;
using System.Threading.Tasks;
using System.IO;
using Assembly = System.Reflection.Assembly;
using Speckle.ConnectorTeklaStructures.UI;

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
