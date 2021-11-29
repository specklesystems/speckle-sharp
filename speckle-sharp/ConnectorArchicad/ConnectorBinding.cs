using DesktopUI2;
using DesktopUI2.Models;
using DesktopUI2.Models.Filters;
using DesktopUI2.ViewModels;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.IO;


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

		public override string GetDocumentId ()
		{
			Model.ProjectInfo projectInfo = Communication.AsyncCommandProcessor.Instance.Execute (new Communication.Commands.GetProjectInfo ()).Result;
			return projectInfo.Name;
		}

		public override string GetDocumentLocation ()
		{
			Model.ProjectInfo projectInfo = Communication.AsyncCommandProcessor.Instance.Execute (new Communication.Commands.GetProjectInfo ()).Result;
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
			IEnumerable<Guid> elementIds = Communication.AsyncCommandProcessor.Instance.Execute (new Communication.Commands.GetSelectedElements ()).Result;

			return elementIds.Select (id => id.ToString ()).ToList ();
		}

		public override List<ISelectionFilter> GetSelectionFilters ()
		{
			return new List<ISelectionFilter> { new ManualSelectionFilter () };
		}

		public override List<StreamState> GetStreamsInFile ()
		{
			// TODO KSZ
			return new List<StreamState> ();
		}

		public override Task<StreamState> ReceiveStream (StreamState state, ProgressViewModel progress)
		{
			return Task.FromResult (new StreamState ());
		}

		public override void SelectClientObjects (string args)
		{
			// TODO KSZ
		}

        public override Task SendStream (StreamState state, ProgressViewModel progress)
        {
            return null;
        }
        public override void WriteStreamsToFile (List<StreamState> streams)
		{
			// TODO KSZ
		}
	}
}
