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
        new DesignSavedStreamViewModel()
        {

          Stream = new DesignStream { name = "Sample stream" },

          StreamState = new DesignStreamState()
          {
            BranchName = "test",
            CommitId = "latest",
            SchedulerEnabled = true,
            IsReceiver = true,
            Filter = new ListSelectionFilter { Icon = "Mouse", Name = "Category" }

          }
        },
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
           ShowReport = false
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
    public DesignStreamState()
    {

    }
  }

  public class DesignStream
  {
    public string name { get; set; }
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
    public bool ShowReport { get; set; } = true;
    public List<MenuItemViewModel> MenuItems = new List<MenuItemViewModel>();
    public void ReceiveCommand()
    {

    }

    public void SendCommand()
    {

    }

    public void CancelSendOrReceive()
    {

    }

    public void CloseNotificationCommand()
    {

    }

    public void OpenReportCommand()
    {

    }
  }
}
