using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Archicad.Communication;
using Archicad.Model;
using DesktopUI2;
using DesktopUI2.Models;
using DesktopUI2.Models.Filters;
using DesktopUI2.ViewModels;
using Speckle.Core.Api;
using Speckle.Core.Credentials;
using Speckle.Core.Kits;
using Speckle.Core.Logging;
using Speckle.Core.Models;

namespace Archicad.Launcher
{
  public partial class ArchicadBinding : ConnectorBindings
  {
    public uint archicadVersion { get; }

    public ArchicadBinding(uint archicadVersion)
    {
      this.archicadVersion = archicadVersion;
    }

    public override string GetActiveViewName()
    {
      throw new NotImplementedException();
    }

    public override List<MenuItem> GetCustomStreamMenuItems()
    {
      return new List<MenuItem>();
    }

    public ProjectInfoData? GetProjectInfo()
    {
      return AsyncCommandProcessor.Execute(new Communication.Commands.GetProjectInfo())?.Result;
    }

    public override string GetDocumentId()
    {
      var projectInfo = AsyncCommandProcessor.Execute(new Communication.Commands.GetProjectInfo())?.Result;
      return projectInfo is null ? string.Empty : projectInfo.name;
    }

    public override string GetDocumentLocation()
    {
      var projectInfo = AsyncCommandProcessor.Execute(new Communication.Commands.GetProjectInfo())?.Result;
      return projectInfo is null ? string.Empty : projectInfo.location;
    }

    public override string GetFileName()
    {
      return Path.GetFileName(GetDocumentLocation());
    }

    public override string GetHostAppNameVersion()
    {
      return string.Format("Archicad {0}", archicadVersion);
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
      var elementIds = AsyncCommandProcessor
        .Execute(new Communication.Commands.GetElementIds(Communication.Commands.GetElementIds.ElementFilter.Selection))
        ?.Result;
      return elementIds is null ? new List<string>() : elementIds.ToList();
    }

    public override List<ISelectionFilter> GetSelectionFilters()
    {
      return new List<ISelectionFilter>()
      {
        new ManualSelectionFilter(),
        new AllSelectionFilter
        {
          Slug = "all",
          Name = "Everything",
          Icon = "CubeScan",
          Description = "Sends all supported elements and project information."
        },
        new ListSelectionFilter
        {
          Slug = "elementType",
          Name = "Element Type",
          Icon = "Category",
          Values = new List<string>
          {
            "Wall",
            "Column",
            "Beam",
            "Slab",
            "Roof",
            "Shell",
            "Stair",
            "Railing",
            "Curtain Wall",
            "Door",
            "Window",
            "Skylight",
            "Opening",
            "Zone",
            "Mesh",
            "Morph",
            "Object",
            "Lamp"
          },
          Description = "Adds all elements with the selected Element Types"
        }
      };
    }

    public override List<StreamState> GetStreamsInFile()
    {
      return new List<StreamState>();
    }

    public override void ResetDocument()
    {
      // TODO!
    }

    public override List<ReceiveMode> GetReceiveModes()
    {
      return new List<ReceiveMode> { ReceiveMode.Create };
    }

    public override bool CanPreviewReceive => false;

    public override Task<StreamState> PreviewReceive(StreamState state, ProgressViewModel progress)
    {
      throw new NotImplementedException("Preview receiving not supported");
    }

    public override async Task<StreamState> ReceiveStream(StreamState state, ProgressViewModel progress)
    {
      try
      {
        using (var timer = Archicad.Helpers.Timer.CreateReceive(state.StreamId))
        {
          Base commitObject = await Speckle.Core.Api.Helpers.Receive(IdentifyStream(state));
          if (commitObject is not null)
            await ElementConverterManager.Instance.ConvertToNative(state, commitObject, progress);

          await AsyncCommandProcessor.Execute(new Communication.Commands.FinishReceiveTransaction());

          if (commitObject == null)
          {
            timer.Cancel();
            throw new SpeckleException("Failed to receive specified");
          }
        }
      }
      catch (Exception ex)
      {
        // log
        if (ex is not OperationCanceledException)
          SpeckleLog.Logger.Error("Conversion to native failed.");

        // throw
        switch (ex)
        {
          case OperationCanceledException:
            throw new OperationCanceledException(ex.Message);
          default:
            throw new SpeckleException(ex.Message, ex);
        }
      }

      return state;
    }

    public override bool CanPreviewSend => false;

    public override void PreviewSend(StreamState state, ProgressViewModel progress)
    {
      throw new NotImplementedException("Preview send not supported");
    }

    public override async Task<string> SendStream(StreamState state, ProgressViewModel progress)
    {
      if (state.Filter == null)
        throw new InvalidOperationException("Expected selection filter to be non-null");

      var commitObject = await ElementConverterManager.Instance.ConvertToSpeckle(state.Filter, progress);

      if (commitObject == null)
        throw new SpeckleException("Failed to convert objects to speckle: conversion returned null");

      return await Speckle.Core.Api.Helpers.Send(
        IdentifyStream(state),
        commitObject,
        state.CommitMessage,
        HostApplications.Archicad.Name
      );
    }

    public override void WriteStreamsToFile(List<StreamState> streams) { }

    private static string IdentifyStream(StreamState state)
    {
      var stream = new StreamWrapper
      {
        StreamId = state.StreamId,
        ServerUrl = state.ServerUrl,
        BranchName = state.BranchName,
        CommitId = state.CommitId != "latest" ? state.CommitId : null
      };
      return stream.ToString();
    }
  }
}
