﻿using System;
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
        //set here otherwise we get errors about re-used visual parents when closing and re-opening the panel
        //there might be other solutions too. If changing this behaviour make sure to refresh the view model
        //when opening a new file as well
        var viewModel = new MainViewModel(SpeckleRhinoConnectorPlugin.Instance.Bindings);
        this.DataContext = viewModel;
        AvaloniaHost.Content = new MainUserControl();
      }
      catch (Exception ex)
      {

      }



    }




  }
}
