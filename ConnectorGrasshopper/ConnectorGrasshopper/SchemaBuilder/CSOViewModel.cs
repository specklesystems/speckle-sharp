using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ConnectorGrasshopper
{
  public class CSOViewModel : INotifyPropertyChanged
  {

    //string searchText;
    //public string SearchText
    //{
    //  get { return searchText; }
    //  set
    //  {
    //    if (searchText != value)
    //    {
    //      searchText = value;
    //      OnPropertyChanged();
    //    }
    //  }
    //}

    int selectedIndex;
    public int SelectedIndex
    {
      get { return selectedIndex; }
      set
      {
        if (selectedIndex != value)
        {
          selectedIndex = value;
          OnPropertyChanged();
        }
      }
    }


    Type selectedType;
    public Type SelectedType
    {
      get
      {
        return selectedType;
      }
      set
      {
        if (selectedType != value)
        {
          selectedType = value;
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
