using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Speckle.Core.Api;
using Speckle.Core.Api.SubscriptionModels;
using Stylet;

namespace Speckle.DesktopUI.Utils
{
  public partial class StreamState : PropertyChangedBase, IHandle<UpdateSelectionCountEvent>
  {
    public async Task<bool> RefreshStream()
    {
      try
      {
        var updatedStream = await Client.StreamGet(Stream.id);
        Stream.name = updatedStream.name;
        Stream.description = updatedStream.description;
        Stream.isPublic = updatedStream.isPublic;
        Stream.collaborators = updatedStream.collaborators;
        Branches = await Client.StreamGetBranches(Stream.id);
        NotifyOfPropertyChange(nameof(Stream));
      }
      catch (Exception)
      {
        return false;
      }

      return true;
    }

    #region Selection events

    public void SetObjectSelection()
    {
      var objIds = Globals.HostBindings.GetSelectedObjects();
      if (objIds == null || objIds.Count == 0)
      {
        Globals.Notify("Could not get object selection.");
        return;
      }

      SelectedObjectIds = objIds;

      Globals.Notify("Object selection set.");
      Filter = null;
    }

    public void AddObjectSelection()
    {
      var objIds = Globals.HostBindings.GetSelectedObjects();
      if (objIds == null || objIds.Count == 0)
      {
        Globals.Notify("Could not get object selection.");
        return;
      }

      objIds.ForEach(id =>
      {
        if (SelectedObjectIds.FirstOrDefault(x => x == id) == null)
        {
          SelectedObjectIds.Add(id);
        }
      });

      Globals.Notify("Object added.");
      Filter = null;
    }

    public void RemoveObjectSelection()
    {
      var objIds = Globals.HostBindings.GetSelectedObjects();
      if (objIds == null || objIds.Count == 0)
      {
        Globals.Notify("Could not get object selection.");
        return;
      }

      var filtered = SelectedObjectIds.Where(o => objIds.IndexOf(o) == -1).ToList();

      if (filtered.Count == SelectedObjectIds.Count)
      {
        Globals.Notify("No objects removed.");
        return;
      }

      Globals.Notify($"{SelectedObjectIds.Count - filtered.Count} objects removed.");
      SelectedObjectIds = filtered;
    }

    public void ClearObjectSelection()
    {
      SelectedObjectIds = new List<string>();
      Filter = null;
      Globals.Notify($"Selection cleared.");
    }

    public void Handle(UpdateSelectionCountEvent message)
    {
      SelectionCount = message.SelectionCount;
    }

    #endregion

    #region subscriptions

    private void HandleStreamUpdated(object sender, StreamInfo info)
    {
      Stream.name = info.name;
      Stream.description = info.description;
      NotifyOfPropertyChange(nameof(Stream));
    }

    private async void HandleCommitCreated(object sender, CommitInfo info)
    {
      Branches = await Client.StreamGetBranches(Stream.id);

      if (IsSenderCard) return;

      var binfo = Branches.FirstOrDefault(b => b.name == info.branchName);
      var cinfo = binfo.commits.items.FirstOrDefault(c => c.id == info.id);

      ServerUpdateSummary = $"{cinfo.authorName} sent new data on branch {info.branchName}: {info.message}";
      ServerUpdates = true;
    }

    private async void HandleCommitDeleted(object sender, CommitInfo info)
    {
      Branches = await Client.StreamGetBranches(Stream.id);

      if ( IsSenderCard ) return;

      ServerUpdateSummary = $"Commit {info.id} has been deleted";
      ServerUpdates = true;
    }

    private void HandleCommitUpdated(object sender, CommitInfo info)
    {
      var branch = Stream.branches.items.FirstOrDefault(b => b.name == info.branchName);
      var commit = branch?.commits.items.FirstOrDefault(c => c.id == info.id);
      if (commit == null)
      {
        // something went wrong, but notify the user there were changes anyway
        // ((look like this sub isn't returning a branch name?))
        ServerUpdates = true;
        return;
      }

      commit.message = info.message;
      NotifyOfPropertyChange(nameof(Stream));
    }

    private async void HandleBranchCreated(object sender, BranchInfo info)
    {
      var updatedBranches = await Client.StreamGetBranches(Stream.id);
      Branches = updatedBranches;
    }

    private void HandleBranchUpdated(object sender, BranchInfo info)
    {
      var branch = Branches.FirstOrDefault(b => b.id == info.id);
      if (branch == null) return;
      branch.name = info.name;
      branch.description = info.description;
      NotifyOfPropertyChange(nameof(BranchContextMenuItems));
    }

    private void HandleBranchDeleted(object sender, BranchInfo info)
    {
      var branch = Branches.FirstOrDefault(b => b.id == info.id);
      if (branch == null) return;
      Stream.branches.items.Remove(branch);
      NotifyOfPropertyChange(nameof(BranchContextMenuItems));
    }

    #endregion
  }
}
