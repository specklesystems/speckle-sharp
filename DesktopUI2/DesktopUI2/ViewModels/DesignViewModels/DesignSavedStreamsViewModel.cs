using DesktopUI2.Models.Filters;
using System.Collections.Generic;

namespace DesktopUI2.ViewModels.DesignViewModels
{
  public class DesignSavedStreamsViewModel
  {
    public List<DesignSavedStreamViewModel> SavedStreams { get; set; }

    public DesignSavedStreamsViewModel()
    {
      SavedStreams = new List<DesignSavedStreamViewModel>
      {
        new DesignSavedStreamViewModel(),
         new DesignSavedStreamViewModel()
         {
           Stream = new DesignStream { name = "BIM data is cool" },
           StreamState = new DesignStreamState()
           {
             BranchName = "main",
             IsReceiver = false,
             Filter = new ListSelectionFilter { Icon = "Mouse", Name = "Category" },
           },
           ShowNotification = false,
           NoAccess = true,
         }
      };
    }
  }

  public class DesignStreamState
  {
    public string BranchName { get; set; }
    public string CommitId { get; set; }
    public string CommitMessage { get; set; }
    public bool IsReceiver { get; set; }
    public bool SchedulerEnabled { get; set; }
    public ListSelectionFilter Filter { get; set; }
    public object Client { get; internal set; }

    public DesignStreamState()
    {

    }
  }

  public class DesignStream
  {
    public string name { get; set; }
    public string id { get; set; }
    public string role { get; set; }
  }

  public class DesignSavedStreamViewModel
  {
    public DesignStreamState StreamState { get; set; }
    public DesignStream Stream { get; set; }
    public ProgressViewModel Progress { get; set; } = new ProgressViewModel();
    public string LastUpdated { get; set; } = "Never";
    public string LastUsed { get; set; } = "Never";
    public string Notification { get; set; } = "Hello";
    public bool ShowNotification { get; set; } = true;
    public bool NoAccess { get; set; } = false;

    public List<MenuItemViewModel> MenuItems = new List<MenuItemViewModel>();
    public List<ActivityViewModel> Activity { get; set; }
    public List<ApplicationObjectViewModel> Report { get; set; }
    public List<CommentViewModel> Comments { get; set; }

    public DesignSavedStreamViewModel()
    {
      Stream = new DesignStream { name = "Sample stream x", id = "1324235", role = "owner" };

      StreamState = new DesignStreamState()
      {
        BranchName = "test",
        CommitId = "latest",
        SchedulerEnabled = true,
        IsReceiver = true,
        Filter = new ListSelectionFilter { Icon = "Mouse", Name = "Category" }
      };

      //Activity = new List<ActivityViewModel>() {
      //  new ActivityViewModel (new Speckle.Core.Api.ActivityItem{ actionType="commit_create",time=""}, null){

      //    Margin = new Avalonia.Thickness(50,10,10,0),
      //    Icon = "ArrowBottomLeft",
      //    Message = "Commit 5aaf00a723 was received by Matteo Cominetti" },
      //  new ActivityViewModel (new Speckle.Core.Api.ActivityItem{ actionType="commit_receive", time=""}, null) {
      //        Margin = new Avalonia.Thickness(10,10,50,0),
      //    Icon = "ArrowTopRight",
      //    Message = "Commit created on branch main: 0ae5a01ad7 (Sent 148 objects from Revit2022.)" } };
    }

    public void ReceiveCommand()
    {

    }

    public void SendCommand()
    {

    }

    public void CancelSendOrReceiveCommand()
    {

    }


    public void OpenReportCommand()
    {

    }

    public void GoBack()
    {

    }
  }
}
