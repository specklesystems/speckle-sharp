using System;
using System.Collections;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Notifications;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Rendering;

namespace DesktopUI2.Models;

/// <summary>
/// An <see cref="INotificationManager"/> that displays notifications in a <see cref="UserControl"/>.
/// Adapted from https://github.com/AvaloniaUI/Avalonia/blob/0.10.18/src/Avalonia.Controls/Notifications/WindowNotificationManager.cs
/// To support User Controls
/// </summary>
[TemplatePart("PART_Items", typeof(Panel)), PseudoClasses(":topleft", ":topright", ":bottomleft", ":bottomright")]
public class NotificationManager : TemplatedControl, IManagedNotificationManager, ICustomSimpleHitTest
{
  /// <summary>
  /// Defines the <see cref="Position"/> property.
  /// </summary>
  public static readonly StyledProperty<NotificationPosition> PositionProperty = AvaloniaProperty.Register<
    NotificationManager,
    NotificationPosition
  >(nameof(Position), NotificationPosition.TopRight);

  /// <summary>
  /// Defines the <see cref="MaxItems"/> property.
  /// </summary>
  public static readonly StyledProperty<int> MaxItemsProperty = AvaloniaProperty.Register<NotificationManager, int>(
    nameof(MaxItems),
    5
  );

  private IList _items;

  static NotificationManager()
  {
    HorizontalAlignmentProperty.OverrideDefaultValue<NotificationManager>(HorizontalAlignment.Stretch);
    VerticalAlignmentProperty.OverrideDefaultValue<NotificationManager>(VerticalAlignment.Stretch);
  }

  /// <summary>
  /// Initializes a new instance of the <see cref="NotificationManager"/> class.
  /// </summary>
  /// <param name="host">The window that will host the control.</param>
  public NotificationManager()
  {
    UpdatePseudoClasses(Position);
  }

  /// <summary>
  /// Defines which corner of the screen notifications can be displayed in.
  /// </summary>
  /// <seealso cref="NotificationPosition"/>
  public NotificationPosition Position
  {
    get => GetValue(PositionProperty);
    set => SetValue(PositionProperty, value);
  }

  /// <summary>
  /// Defines the maximum number of notifications visible at once.
  /// </summary>
  public int MaxItems
  {
    get => GetValue(MaxItemsProperty);
    set => SetValue(MaxItemsProperty, value);
  }

  public bool HitTest(Point point)
  {
    return VisualChildren.HitTestCustom(point);
  }

  /// <inheritdoc/>
  public void Show(INotification content)
  {
    Show(content as object);
  }

  /// <inheritdoc/>
  public async void Show(object content)
  {
    if (_items == null)
    {
      return;
    }

    var notification = content as INotification;

    var notificationControl = new NotificationCard
    {
      Content = content,
      Classes = new Classes(notification?.Type.ToString())
    };

    if (notification != null)
    {
      notificationControl.NotificationClosed += (sender, args) =>
      {
        notification.OnClose?.Invoke();

        _items.Remove(sender);
      };
    }

    notificationControl.PointerPressed += (sender, args) =>
    {
      if (notification != null && notification.OnClick != null)
      {
        notification.OnClick.Invoke();
      }

      (sender as NotificationCard)?.Close();
    };

    _items.Add(notificationControl);

    if (_items.OfType<NotificationCard>().Count(i => !i.IsClosing) > MaxItems)
    {
      _items.OfType<NotificationCard>().First(i => !i.IsClosing).Close();
    }

    if (notification != null && notification.Expiration == TimeSpan.Zero)
    {
      return;
    }

    await Task.Delay(notification?.Expiration ?? TimeSpan.FromSeconds(7)).ConfigureAwait(true);

    notificationControl.Close();
  }

  /// <inheritdoc/>
  protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
  {
    var itemsControl = e.NameScope.Find<Panel>("PART_Items");
    _items = itemsControl?.Children;
  }

  protected override void OnPropertyChanged<T>(AvaloniaPropertyChangedEventArgs<T> change)
  {
    base.OnPropertyChanged(change);

    if (change.Property == PositionProperty)
    {
      UpdatePseudoClasses(change.NewValue.GetValueOrDefault<NotificationPosition>());
    }
  }

  private void UpdatePseudoClasses(NotificationPosition position)
  {
    PseudoClasses.Set(":topleft", position == NotificationPosition.TopLeft);
    PseudoClasses.Set(":topright", position == NotificationPosition.TopRight);
    PseudoClasses.Set(":bottomleft", position == NotificationPosition.BottomLeft);
    PseudoClasses.Set(":bottomright", position == NotificationPosition.BottomRight);
  }
}
