using System;
using System.Runtime.InteropServices;
using System.Windows.Controls;
using DesktopUI2.ViewModels.MappingTool;
using DesktopUI2.Views;

namespace SpeckleRhino;

/// <summary>
/// Interaction logic for Page1.xaml
/// </summary>
[Guid("0EB2F55E-CEB2-4112-8248-EED17ED66CD3")]
public partial class MappingsPanel : UserControl
{
  public MappingsPanel()
  {
    try
    {
      InitializeComponent();
      //set here otherwise we get errors about re-used visual parents when closing and re-opening the panel
      //there might be other solutions too. If changing this behaviour make sure to refresh the view model
      //when opening a new file as well
      var viewModel = new MappingsViewModel(SpeckleRhinoConnectorPlugin.Instance.MappingBindings);
      DataContext = viewModel;
      AvaloniaHost.Content = new MappingsControl();
    }
    catch (Exception ex) { }
  }
}
