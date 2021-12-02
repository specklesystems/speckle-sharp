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
using System.Threading;
using Speckle.Core.Models;


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
			Model.ProjectInfo projectInfo = Communication.AsyncCommandProcessor.Instance.Execute (new Communication.Commands.GetProjectInfo ()).Result;
			if (projectInfo is null)
			{
				return string.Empty;
			}

			return projectInfo.Name;
		}

		public override string GetDocumentLocation ()
		{
			Model.ProjectInfo projectInfo = Communication.AsyncCommandProcessor.Instance.Execute (new Communication.Commands.GetProjectInfo ()).Result;
			if (projectInfo is null)
			{
				return string.Empty;
			}

			return projectInfo.Location;
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
			var aa = await Helpers.Receive (state.StreamId);

			return state;
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

			Base commitObject = await CreateCommitObject (state.SelectedObjectIds, progress.CancellationTokenSource.Token);
			if (commitObject is null)
			{
				return;
			}

			await Helpers.Send (state.StreamId, commitObject);
		}

        public override void WriteStreamsToFile (List<StreamState> streams)
		{
		}

		private async Task<Base> CreateCommitObject (IEnumerable<string> elementIds, CancellationToken token)
		{
			IEnumerable<Model.ElementModel> models = await Communication.AsyncCommandProcessor.Instance.Execute (new Communication.Commands.GetModerlForElements (elementIds), token);
			if (models is null)
			{
				return null;
			}

			Base commitObject = new Base ();
			commitObject["BuildingElements"] = models.Select (m => new Objects.DirectShape (m));

			return commitObject;
		}
	}
}
