using System;
using System.Windows;
using System.Windows.Controls;
using DesktopUI2.ViewModels;
using DesktopUI2.Views;

namespace SpeckleRhino
{
  /// <summary>
  /// Interaction logic for Page1.xaml
  /// </summary>
  [System.Runtime.InteropServices.Guid("3EA3FEE1-216D-4076-9A06-949DE4C0E8AF")]
  public partial class Panel : UserControl
  {

    public static Panel Instance;
    public Panel()
    {
      Instance = this;

      InitializeComponent();
      try
      {
        var bindings =  new ConnectorBindingsRhino();

        var viewModel = new MainViewModel(bindings);
      this.DataContext = viewModel;

        AvaloniaHost.Content = new MainUserControl();
      }
      catch (Exception e)
      {
      
      }
      

    }




  }
}
