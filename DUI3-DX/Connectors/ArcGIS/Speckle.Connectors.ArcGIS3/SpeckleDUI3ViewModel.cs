using ArcGIS.Desktop.Framework;
using ArcGIS.Desktop.Framework.Contracts;

namespace Speckle.Connectors.ArcGIS;

internal sealed class SpeckleDUI3ViewModel : DockPane
{
  private const string DOCKPANE_ID = "SpeckleDUI3_SpeckleDUI3";

  internal static void Create()
  {
    var pane = FrameworkApplication.DockPaneManager.Find(DOCKPANE_ID);
    pane?.Activate();
  }

  /// <summary>
  /// Called when the pane is initialized.
  /// </summary>
  protected override async Task InitializeAsync()
  {
    await base.InitializeAsync().ConfigureAwait(false);
  }

  /// <summary>
  /// Called when the pane is uninitialized.
  /// </summary>
  protected override async Task UninitializeAsync()
  {
    await base.UninitializeAsync().ConfigureAwait(false);
  }
}

/// <summary>
/// Button implementation to create a new instance of the pane and activate it.
/// </summary>
internal sealed class SpeckleDUI3OpenButton : Button
{
  protected override void OnClick()
  {
    SpeckleDUI3ViewModel.Create();
  }
}
