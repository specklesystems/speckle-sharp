using DesktopUI2.Models;
using DesktopUI2.Models.Filters;
using ReactiveUI;
using Speckle.Core.Credentials;
using System;
using System.Collections.Generic;
using System.Reactive;
using System.Text;

namespace DesktopUI2.ViewModels.DesignViewModels
{
  public class DesignSavedStreamsViewModel
  {

    public List<DesignSavedStreamViewModel> SavedStreams { get; set; }


    public DesignSavedStreamsViewModel()
    {
      SavedStreams = new List<DesignSavedStreamViewModel>
      {
         new DesignSavedStreamViewModel()
         {

           Stream = new DesignStream{name = "Sample stream"},
           StreamState = new StreamState(null, new Speckle.Core.Api.Stream{id = "999" })
           {
             BranchName = "test",
             CommitId = "latest",
             IsReceiver = true,
             Filter = new ListSelectionFilter { Icon = "Mouse", Name = "Category" },
             SelectedObjectIds = new List<string> { "", "", "" },
           }
         },
         new DesignSavedStreamViewModel()
         {

           Stream = new DesignStream{name = "BIM data is cool"},
           StreamState = new StreamState(null, new Speckle.Core.Api.Stream{id = "999" })
           {
             BranchName = "main",
           IsReceiver = false,
           Filter = new ListSelectionFilter { Icon = "Mouse", Name = "Category" },
           SelectedObjectIds = new List<string> { "", "", "" },
           }
         }

      };
    }
  }

  public class DesignStream
  {
    public string name { get; set; }
  }


  public class DesignSavedStreamViewModel
  {
    public StreamState StreamState { get; set; }
    public DesignStream Stream { get; set; }
  }
}
