/*

   Copyright 2022 Esri

   Licensed under the Apache License, Version 2.0 (the "License");
   you may not use this file except in compliance with the License.
   You may obtain a copy of the License at

       https://www.apache.org/licenses/LICENSE-2.0

   Unless required by applicable law or agreed to in writing, software
   distributed under the License is distributed on an "AS IS" BASIS,
   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.

   See the License for the specific language governing permissions and
   limitations under the License.

*/

using ArcGIS.Core.CIM;
using ArcGIS.Desktop.Core;
using ArcGIS.Desktop.Framework;
using ArcGIS.Desktop.Framework.Contracts;
using System.Threading.Tasks;

namespace Speckle.Connectors.ArcGIS.HostApp;

internal class SpeckleDUI3ViewModel : ViewStatePane
{
  private const string VIEW_PANE_ID = "SpeckleDUI3_SpeckleDUI3";

  /// <summary>
  /// Consume the passed in CIMView. Call the base constructor to wire up the CIMView.
  /// </summary>
  public SpeckleDUI3ViewModel(CIMView view)
    : base(view) { }

  /// <summary>
  /// Create a new instance of the pane.
  /// </summary>
  internal static SpeckleDUI3ViewModel Create()
  {
    // Otherwise crash on SqliteConnection
    SQLitePCL.Batteries.Init();
    var view = new CIMGenericView { ViewType = VIEW_PANE_ID };
    return FrameworkApplication.Panes.Create(VIEW_PANE_ID, new object[] { view }) as SpeckleDUI3ViewModel;
  }

  #region Pane Overrides

  /// <summary>
  /// Must be overridden in child classes used to persist the state of the view to the CIM.
  /// </summary>
  public override CIMView ViewState
  {
    get
    {
      _cimView.InstanceID = (int)InstanceID;
      return _cimView;
    }
  }

  /// <summary>
  /// Called when the pane is initialized.
  /// </summary>
  protected override async Task InitializeAsync()
  {
    await base.InitializeAsync();
  }

  /// <summary>
  /// Called when the pane is uninitialized.
  /// </summary>
  protected override async Task UninitializeAsync()
  {
    await base.UninitializeAsync();
  }

  #endregion Pane Overrides
}

/// <summary>
/// Button implementation to create a new instance of the pane and activate it.
/// </summary>
internal class SpeckleDUI3OpenButton : Button
{
  protected override void OnClick()
  {
    SpeckleDUI3ViewModel.Create();
  }
}
