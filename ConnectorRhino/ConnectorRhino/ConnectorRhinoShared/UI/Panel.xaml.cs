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
    public Panel()
    {
      try
      {
        InitializeComponent();
        //Set in the Plugin so that it's not disposed each time the panel is closed
        this.DataContext = SpeckleRhinoConnectorPlugin.Instance.ViewModel;
        AvaloniaHost.Content = new MainUserControl();
      }
      catch (Exception ex)
      {

      }

      

    }




  }
}
