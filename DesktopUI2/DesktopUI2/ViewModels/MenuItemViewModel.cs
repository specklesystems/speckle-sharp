using DesktopUI2.Models;
using Material.Icons;
using Material.Icons.Avalonia;
using ReactiveUI;
using Speckle.Core.Credentials;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace DesktopUI2.ViewModels
{
  public class MenuItemViewModel
  {
    public object Header { get; set; }
    public object Icon { get; set; }
    public ICommand Command { get; set; }
    public object CommandParameter { get; set; }
    public IList<MenuItemViewModel> Items { get; set; }

    public MenuItemViewModel()
    {

    }

    public MenuItemViewModel(MenuItem item, object commandParameter)
    {

      Header = item.Header;

      MaterialIconKind icon;
      if (Enum.TryParse<MaterialIconKind>(item.Icon, out icon))
        Icon = new MaterialIcon { Kind = icon, Foreground = Avalonia.Media.Brushes.Gray };

      if (item.Action != null)
      {
        Command = ReactiveCommand.Create(item.Action);
      }

      CommandParameter = commandParameter;

      if (item.Items != null)
      {
        Items = item.Items.Select(x => new MenuItemViewModel(x, commandParameter)).ToList();
      }

    }

    //public MenuItemViewModel(ICommand command, object commandParameter, string header, MaterialIconKind icon)
    //{
    //  Command = command;
    //  CommandParameter = commandParameter;
    //  Header = header;
    //  Icon = new MaterialIcon { Kind = icon, Foreground = Avalonia.Media.Brushes.Gray };
    //}

    public MenuItemViewModel(Action action, string header, MaterialIconKind icon)
    {
      Command = ReactiveCommand.Create(action);
      Header = header;
      Icon = new MaterialIcon { Kind = icon, Foreground = Avalonia.Media.Brushes.Gray };
    }

    public MenuItemViewModel(Action<Account> action, object commandParameter, string header, MaterialIconKind icon)
    {
      Command = ReactiveCommand.Create(action);
      CommandParameter = commandParameter;
      Header = header;
      Icon = new MaterialIcon { Kind = icon, Foreground = Avalonia.Media.Brushes.Gray };
    }

  }
}
