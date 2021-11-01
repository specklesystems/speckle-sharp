using Microsoft.Xaml.Behaviors;
using System.Windows.Controls;
using System.Windows.Input;

namespace ConnectorGSA.Utilities
{
  public class TextBoxEnterKeyUpdateBehaviour : Behavior<TextBox>
  {
    private TextBox textBox;
    protected override void OnAttached()
    {
      if (this.AssociatedObject != null)
      {
        base.OnAttached();
        textBox = AssociatedObject as TextBox;
        //textBox.GotFocus += TextBoxGotFocus;
        this.AssociatedObject.KeyDown += AssociatedObject_KeyDown;
      }
    }

    protected override void OnDetaching()
    {
      if (this.AssociatedObject != null)
      {
        this.AssociatedObject.KeyDown -= AssociatedObject_KeyDown;
        //textBox.GotFocus -= TextBoxGotFocus;
        base.OnDetaching();
      }
    }

    /*
    private void TextBoxGotFocus(object sender, RoutedEventArgs routedEventArgs)
    {
      textBox.CaretIndex = textBox.Text.Length;
    }
    */

    private void AssociatedObject_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
      TextBox textBox = sender as TextBox;
      if (textBox != null)
      {
        if (e.Key == Key.Return)
        {
          if (e.Key == Key.Enter)
          {
            textBox.MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));
          }
        }
      }
    }
  }
}
