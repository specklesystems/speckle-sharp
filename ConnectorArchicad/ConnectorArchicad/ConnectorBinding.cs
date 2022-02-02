using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Archicad.Communication;
using DesktopUI2;
using DesktopUI2.Models;
using DesktopUI2.Models.Filters;
using DesktopUI2.ViewModels;
using Speckle.Core.Api;
using Speckle.Core.Credentials;
using Speckle.Core.Models;

namespace Archicad.Launcher
{
  public class ArchicadBinding : ConnectorBindings
  {
    public override string GetActiveViewName()
    {
      throw new NotImplementedException();
    }

    public override List<MenuItem> GetCustomStreamMenuItems()
    {
      return new List<MenuItem>();
    }

    public override string? GetDocumentId()
    {
      Model.ProjectInfoData projectInfo = AsyncCommandProcessor.Execute(new Communication.Commands.GetProjectInfo()).Result;
      if (projectInfo is null)
      {
        return string.Empty;
      }

      return projectInfo.name;
    }

    public override string GetDocumentLocation()
    {
      Model.ProjectInfoData projectInfo = AsyncCommandProcessor.Execute(new Communication.Commands.GetProjectInfo()).Result;
      return projectInfo is null ? string.Empty : projectInfo.location;
    }

    public override string GetFileName()
    {
      return Path.GetFileName(GetDocumentLocation());
    }

    public override string GetHostAppName()
    {
      return "Archicad";
    }

    public override List<string> GetObjectsInView()
    {
      throw new NotImplementedException();
    }

    public override List<string> GetSelectedObjects()
    {
      IEnumerable<string> elementIds = AsyncCommandProcessor.Execute(new Communication.Commands.GetSelectedElements()).Result;
      return elementIds is null ? new List<string>() : elementIds.ToList();
    }

    public override List<ISelectionFilter> GetSelectionFilters()
    {
      return new List<ISelectionFilter> { new ManualSelectionFilter() };
    }

    public override List<StreamState> GetStreamsInFile()
    {
      return new List<StreamState>();
    }

    public override async Task<StreamState> ReceiveStream(StreamState state, ProgressViewModel progress)
    {
      Base commitObject = await Helpers.Receive(IdentifyStream(state));
      if (commitObject is null)
      {
        return null;
      }

      state.SelectedObjectIds = await ElementConverterManager.Instance.ConvertToNative(commitObject, progress.CancellationTokenSource.Token);

      return state;
    }

    public override void SelectClientObjects(string args) { }

    public override async Task SendStream(StreamState state, ProgressViewModel progress)
    {
      if (state.Filter is null)
      {
        return;
      }

      state.SelectedObjectIds = state.Filter.Selection;

      var commitObject = await ElementConverterManager.Instance.ConvertToSpeckle(state.SelectedObjectIds, progress.CancellationTokenSource.Token);
      if (commitObject is not null)
      {
        await Helpers.Send(IdentifyStream(state), commitObject, state.CommitMessage, Speckle.Core.Kits.Applications.Archicad);
      }
    }

    public override void WriteStreamsToFile(List<StreamState> streams) { }

    private static string IdentifyStream(StreamState state)
    {
      StreamWrapper stream = new StreamWrapper { StreamId = state.StreamId, ServerUrl = state.ServerUrl, BranchName = state.BranchName };
      return stream.ToString();
    }
  }
}
