using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Input;

namespace DesktopUI2.Views.AttachedProperties;

public static class BlockSelection
{
  public static readonly AttachedProperty<bool> IsSelectionBlockedProperty = AvaloniaProperty.RegisterAttached<
    ListBoxItem,
    bool
  >("IsSelectionBlocked", typeof(BlockSelection));

  static BlockSelection()
  {
    IsSelectionBlockedProperty.Changed.AddClassHandler<ListBoxItem>(IsSelectionBlockedChanged);
  }

  public static bool GetIsSelectionBlocked(ListBoxItem item)
  {
    return item.GetValue(IsSelectionBlockedProperty);
  }

  public static void SetIsSelectionBlocked(ListBoxItem item, bool value)
  {
    item.SetValue(IsSelectionBlockedProperty, value);
  }

  private static void IsSelectionBlockedChanged(ListBoxItem item, AvaloniaPropertyChangedEventArgs e)
  {
    if (!(e.OldValue is bool oldValue) || !(e.NewValue is bool newValue) || oldValue == newValue)
    {
      return;
    }

    if (newValue)
    {
      item.PointerPressed += OnItemPointerPressed;
      item.SetValue(ListBoxItem.IsSelectedProperty, false);
    }
    else
    {
      item.PointerPressed -= OnItemPointerPressed;
    }

    item.SetValue(InputElement.FocusableProperty, !newValue, BindingPriority.Style);
  }

  private static void OnItemPointerPressed(object sender, PointerPressedEventArgs e)
  {
    e.Handled = true;
  }
}
