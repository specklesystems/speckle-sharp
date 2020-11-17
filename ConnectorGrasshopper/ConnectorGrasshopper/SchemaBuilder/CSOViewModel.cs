using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Eto.Forms;

namespace ConnectorGrasshopper
{
  public class CSOViewModel : INotifyPropertyChanged
  {

    TreeGridItem selectedItem;
    public TreeGridItem SelectedItem
    {
      get
      {
        return selectedItem;
      }
      set
      {
        if (selectedItem != value)
        {
          selectedItem = value;
          OnPropertyChanged();
        }
      }
    }


    void OnPropertyChanged([CallerMemberName] string memberName = null)
    {
      PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(memberName));
    }
    public event PropertyChangedEventHandler PropertyChanged;
  }
}
