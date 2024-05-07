using Speckle.Connectors.DUI.Bridge;

namespace Speckle.Connectors.DUI.Bindings;

/// <summary>
/// Describes the most basic binding.
/// </summary>
public interface IBinding
{
  /// <summary>
  /// This will be the name under which it will be available in the Frontend, e.g.
  /// window.superBinding, or window.mapperBinding. Please use camelCase even if
  /// it hurts.
  /// </summary>
  public string Name { get; }

  /// <summary>
  /// Bindings will be wrapped by a browser specific bridge, and they will need
  /// to use that bridge to send events to the Frontend, via <see cref="IBridge.Send">SendToBrowser(IHostAppEvent)</see>.
  /// TODO: we'll probably need a factory class of sorts to handle the proper wrapping. Currently, on bridge instantiation the parent is set in the bindings class that has been wrapped around. Not vvv elegant.
  /// </summary>
  public IBridge Parent { get; }
}
