using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Speckle.Core.Api.SubscriptionModels;
using Speckle.Core.Models;
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
        Branches = updatedStream.branches.items;
        NotifyOfPropertyChange(nameof(Stream));
      }
      catch ( Exception )
      {
        return false;
      }

      return true;
    }

    #region Selection events

    public void SetObjectSelection()
    {
      var objIds = Globals.HostBindings.GetSelectedObjects();
      if ( objIds == null || objIds.Count == 0 )
      {
        Globals.Notify("Could not get object selection.");
        return;
      }

      Objects = objIds.Select(id => new Base {applicationId = id}).ToList();

      Globals.Notify("Object selection set.");
      Filter = null;
    }

    public void AddObjectSelection()
    {
      var objIds = Globals.HostBindings.GetSelectedObjects();
      if ( objIds == null || objIds.Count == 0 )
      {
        Globals.Notify("Could not get object selection.");
        return;
      }

      objIds.ForEach(id =>
      {
        if ( Objects.FirstOrDefault(b => b.applicationId == id) == null )
        {
          Objects.Add(new Base {applicationId = id});
        }
      });

      Globals.Notify("Object added.");
      Filter = null;
    }

    public void RemoveObjectSelection()
    {
      var objIds = Globals.HostBindings.GetSelectedObjects();
      if ( objIds == null || objIds.Count == 0 )
      {
        Globals.Notify("Could not get object selection.");
        return;
      }

      var filtered = Objects.Where(o => objIds.IndexOf(o.applicationId) == -1).ToList();

      if ( filtered.Count == Objects.Count )
      {
        Globals.Notify("No objects removed.");
        return;
      }

      Globals.Notify($"{Objects.Count - filtered.Count} objects removed.");
      Objects = filtered;
    }

    public void ClearObjectSelection()
    {
      Objects = new List<Base>();
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
      var updatedStream = await Client.StreamGet(Stream.id);
      Branches = updatedStream.branches.items;

      if ( IsSenderCard ) return;

      var binfo = Branches.FirstOrDefault(b => b.name == info.branchName);
      var cinfo = binfo.commits.items.FirstOrDefault(c => c.id == info.id);

      if ( Branch.name == info.branchName )
      {
        Branch = binfo;
      }

      ServerUpdateSummary = $"{cinfo.authorName} sent new data on branch {info.branchName}: {info.message}";
      ServerUpdates = true;
    }

    private void HandleCommitUpdated(object sender, CommitInfo info)
    {
      var branch = Stream.branches.items.FirstOrDefault(b => b.name == info.branchName);
      var commit = branch?.commits.items.FirstOrDefault(c => c.id == info.id);
      if ( commit == null )
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
      var updatedStream = await Client.StreamGet(Stream.id);
      Branches = updatedStream.branches.items;
    }

    private void HandleBranchUpdated(object sender, BranchInfo info)
    {
      var branch = Branches.FirstOrDefault(b => b.id == info.id);
      if ( branch == null ) return;
      branch.name = info.name;
      branch.description = info.description;
      NotifyOfPropertyChange(nameof(BranchContextMenuItems));
    }

    private void HandleBranchDeleted(object sender, BranchInfo info)
    {
      var branch = Branches.FirstOrDefault(b => b.id == info.id);
      if ( branch == null ) return;
      Stream.branches.items.Remove(branch);
      NotifyOfPropertyChange(nameof(BranchContextMenuItems));
    }

    #endregion
  }
}
