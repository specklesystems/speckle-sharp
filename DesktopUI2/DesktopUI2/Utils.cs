using Avalonia.Controls;
using Avalonia.Data;
using DesktopUI2.Views;
using Material.Dialog;
using Material.Dialog.Icons;
using Material.Dialog.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace DesktopUI2
{
  public static class Utils
  {
    public static async void ShowDialog(string header, string message, DialogIconKind icon)
    {
      var result = await DialogHelper.CreateAlertDialog(new AlertDialogBuilderParams()
      {
        ContentHeader = header,
        SupportingText = message,
        DialogHeaderIcon = icon,
        StartupLocation = WindowStartupLocation.CenterOwner,
        NegativeResult = new DialogResult("ok"),
        WindowTitle = header,
        Borderless = true,
        MaxWidth = MainWindow.Instance.Width - 40,
        DialogButtons = new DialogResultButton[]
                {
                    new DialogResultButton
                    {
                        Content = "OK",
                        Result = "ok"
                    }
                },
      }).ShowDialog(MainWindow.Instance);
    }

    public static IDialogWindow<DialogResult> SendReceiveDialog(string header, object dataContext)
    {
      return DialogHelper.CreateCustomDialog(new CustomDialogBuilderParams()
      {
        ContentHeader = header,
        WindowTitle = header,
        DialogHeaderIcon = Material.Dialog.Icons.DialogIconKind.Info,
        StartupLocation = WindowStartupLocation.CenterOwner,
        NegativeResult = new DialogResult("cancel"),
        Borderless = true,
        DialogButtons = new DialogResultButton[]
          {
            new DialogResultButton
            {
              Content = "CANCEL",
              Result = "cancel"
            },

          },
        Content = new ProgressBar()
        {
          Minimum = 0,
          DataContext = dataContext,
          [!ProgressBar.ValueProperty] = new Binding("Progress.Value"),
          [!ProgressBar.MaximumProperty] = new Binding("Progress.Maximum"),
          [!ProgressBar.IsIndeterminateProperty] = new Binding("Progress.IsIndeterminate")
        }
      });

    }
  }
}
