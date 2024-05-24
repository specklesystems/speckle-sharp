namespace Speckle.Converters.Common;

// NOTE: Do not change the order of the existing ones
/// <summary>
/// Receive modes indicate what to do and not do when receiving objects
/// </summary>
public enum ReceiveMode
{
  /// <summary>
  /// Attemts updating previously received objects by ID, deletes previously received objects that do not exist anymore and creates new ones
  /// </summary>
  // POC: these could be numbered explicitly if the order matters, gaps could be included,
  // so 1000, 2000, 3000 for plenty of expansion
  Update,

  /// <summary>
  /// Always creates new objects
  /// </summary>
  Create,

  /// <summary>
  /// Ignores updating previously received objects and does not attempt updating or deleting them, creates new objects
  /// </summary>
  Ignore
}
