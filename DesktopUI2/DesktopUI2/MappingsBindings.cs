using System.Collections.Generic;
using DesktopUI2.ViewModels.MappingTool;

namespace DesktopUI2;

/// <summary>
/// Notifies that the selection has changed
/// </summary>
/// <param name="objects"></param>
public delegate void UpdateSelection(MappingSelectionInfo info);

/// <summary>
/// Notifies that the existing elements with a schema in the Doc have changed
/// </summary>
/// <param name="schemas"></param>
public delegate void UpdateExistingSchemaElements(List<Schema> schemas);

public class MappingSelectionInfo
{
  public MappingSelectionInfo(List<Schema> schemas, int count)
  {
    Schemas = schemas;
    Count = count;
  }

  public List<Schema> Schemas { get; set; }
  public int Count { get; set; }
}

public abstract class MappingsBindings
{
  /// <summary>
  /// Gets the selected objects in the host application
  /// </summary>
  /// <returns></returns>
  public abstract MappingSelectionInfo GetSelectionInfo();

  /// <summary>
  /// Gets all the objects with a schema in the host application
  /// </summary>
  /// <returns></returns>
  public abstract List<Schema> GetExistingSchemaElements();

  /// <summary>
  /// Sets the mappings on the current selection
  /// </summary>
  /// <param name="schema">The schema to be applied, has to be an Objects class and have appropriate converter</param>
  /// <param name="viewModel">An ISchema View Model to easily restore the mappings in the Mapping Tool</param>
  public abstract void SetMappings(string schema, string viewModel);

  /// <summary>
  /// Clears the mappings on the current selection
  /// </summary>
  public abstract void ClearMappings(List<string> ids);

  /// <summary>
  /// Highlights a list of elements given their IDs
  /// </summary>
  /// <param name="ids"></param>
  public abstract void HighlightElements(List<string> ids);

  /// <summary>
  /// Selects a list of elements given their IDs
  /// </summary>
  /// <param name="ids"></param>
  public abstract void SelectElements(List<string> ids);

  #region delegates

  public UpdateSelection UpdateSelection;
  public UpdateExistingSchemaElements UpdateExistingSchemaElements;

  #endregion
}
