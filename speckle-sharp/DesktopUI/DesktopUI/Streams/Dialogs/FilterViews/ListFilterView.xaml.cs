using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Speckle.DesktopUI.Streams.Dialogs.FilterViews
{
  public partial class ListFilterView : UserControl
  {
    public ListFilterView()
    {
      InitializeComponent();
    }

    private void SearchTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
    {
      if (e.Key.Equals(Key.Down))
      {
        var element = sender as UIElement;
        if (element != null) element.MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));
      }
    }
  }
}
