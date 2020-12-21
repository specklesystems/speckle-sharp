using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Speckle.DesktopUI.Streams.Dialogs.FilterViews
{
  public partial class CategoryFilterView : UserControl
  {
    public CategoryFilterView()
    {
      InitializeComponent();
    }

    private void SearchTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
    {
      if (e.Key.Equals(Key.Down))
      {
        var element = sender as UIElement;
        if (element != null)element.MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));
      }
    }
  }
}
