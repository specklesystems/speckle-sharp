using System.Collections.Generic;
using Archicad.Communication;

namespace Archicad.Launcher;

public partial class ArchicadBinding
{
  public override void SelectClientObjects(List<string> elemntIds, bool deselect = false)
  {
    var result = AsyncCommandProcessor.Execute(new Communication.Commands.SelectElements(elemntIds, deselect))?.Result;
  }
}
