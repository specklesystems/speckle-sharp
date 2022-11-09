using DesktopUI2.ViewModels.MappingTool;
using Speckle.Core.Models;
using System;
using System.Collections.Generic;

namespace DesktopUI2
{
  /// <summary>
  /// Notifies that the selection has changed
  /// </summary>
  /// <param name="objects"></param>
  public delegate void UpdateSelection(List<Type> objects);

  public abstract class MappingsBindings
  {

    #region delegates

    public UpdateSelection UpdateSelection;

    #endregion
    /// <summary>
    /// Gets the selected objects in the host application
    /// </summary>
    /// <returns></returns>
    public abstract List<Type> GetSelectionSchemas();

    /// <summary>
    /// Sets the mappings on the current selection
    /// </summary>
    /// <param name="schema">The schema to be applied, has to be an Objects class and have appropriate converter</param>
    /// <param name="viewModel">An ISchema View Model to easily restore the mappings in the Mapping Tool</param>
    public abstract void SetMappings(string schema, string viewModel);



  }
}
