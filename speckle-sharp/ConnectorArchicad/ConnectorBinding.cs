using DesktopUI2;
using DesktopUI2.Models;
using DesktopUI2.Models.Filters;
using DesktopUI2.ViewModels;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.IO;
using Speckle.Core.Api;
using Speckle.Core.Models;
using Objects.BuiltElements.Archicad.Model;
using Speckle.Core.Credentials;


namespace Archicad.Launcher
{
	public class ArchicadBinding : ConnectorBindings
	{
		public override string GetActiveViewName ()
		{
			throw new NotImplementedException ();
		}

		public override List<MenuItem> GetCustomStreamMenuItems ()
		{
			return new List<MenuItem> ();
		}

		public override string? GetDocumentId ()
		{
			ProjectInfoData projectInfo = Communication.AsyncCommandProcessor.Instance.Execute (new Communication.Commands.GetProjectInfo ()).Result;
			if (projectInfo is null)
			{
				return string.Empty;
			}

			return projectInfo.name;
		}

		public override string GetDocumentLocation ()
		{
			ProjectInfoData projectInfo = Communication.AsyncCommandProcessor.Instance.Execute (new Communication.Commands.GetProjectInfo ()).Result;
			if (projectInfo is null)
			{
				return string.Empty;
			}

			return projectInfo.location;
		}

		public override string GetFileName ()
		{
			return Path.GetFileName (GetDocumentLocation ());
		}

		public override string GetHostAppName ()
		{
			return "Archicad";
		}

		public override List<string> GetObjectsInView ()
		{
			throw new NotImplementedException ();
		}

		public override List<string> GetSelectedObjects ()
		{
			IEnumerable<string> elementIds = Communication.AsyncCommandProcessor.Instance.Execute (new Communication.Commands.GetSelectedElements ()).Result;
			if (elementIds is null)
			{
				return new List<string> ();
			}

			return elementIds.ToList ();
		}

		public override List<ISelectionFilter> GetSelectionFilters ()
		{
			return new List<ISelectionFilter> { new ManualSelectionFilter () };
		}

		public override List<StreamState> GetStreamsInFile ()
		{
			return new List<StreamState> ();
		}

		public override async Task<StreamState> ReceiveStream (StreamState state, ProgressViewModel progress)
		{
			// TODO KSZ
			Base commitObject = await Helpers.Receive (IdentifyStream (state));
			if (commitObject is null) return null;

			StreamState newstate = await Operations.ElementConverter.ConvertBack(commitObject, state, progress.CancellationTokenSource.Token);

			return newstate;
		}

		public override void SelectClientObjects (string args)
		{
			// TODO KSZ
		}

		public override async Task SendStream (StreamState state, ProgressViewModel progress)
		{
			if (state.Filter is null)
			{
				return;
			}

			state.SelectedObjectIds = state.Filter.Selection;

			Base commitObject = await Operations.ElementConverter.Convert (state.SelectedObjectIds, progress.CancellationTokenSource.Token);
			if (commitObject is null)
			{
				return;
			}

			await Helpers.Send (IdentifyStream (state), commitObject, state.CommitMessage, Speckle.Core.Kits.Applications.Archicad);
		}

		public override void WriteStreamsToFile (List<StreamState> streams)
		{
		}

		private string IdentifyStream (StreamState state)
		{
			StreamWrapper stream = new StreamWrapper { StreamId = state.StreamId, ServerUrl = state.ServerUrl, BranchName = state.BranchName };
			return stream.ToString ();
		}
	}
}
