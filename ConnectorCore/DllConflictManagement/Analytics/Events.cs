namespace Speckle.DllConflictManagement.Analytics;

/// <summary>
/// Default Mixpanel events
/// </summary>
public enum Events
{
  /// <summary>
  /// Event triggered when data is sent to a Speckle Server
  /// </summary>
  Send,

  /// <summary>
  /// Event triggered when data is received from a Speckle Server
  /// </summary>
  Receive,

  /// <summary>
  /// Event triggered when a node is executed in a visual programming environment, it should contain the name of the action and the host application
  /// </summary>
  NodeRun,

  /// <summary>
  /// Event triggered when an action is executed in Desktop UI, it should contain the name of the action and the host application
  /// </summary>
  DUIAction,

  /// <summary>
  /// Event triggered when a node is first created in a visual programming environment, it should contain the name of the action and the host application
  /// </summary>
  NodeCreate,

  /// <summary>
  /// Event triggered when the import/export alert is launched or closed
  /// </summary>
  ImportExportAlert,

  /// <summary>
  /// Event triggered when the connector is registered
  /// </summary>
  Registered,

  /// <summary>
  /// Event triggered by the Mapping Tool
  /// </summary>
  MappingsAction
}
