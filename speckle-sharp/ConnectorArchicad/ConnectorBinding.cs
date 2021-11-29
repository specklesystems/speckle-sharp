using DesktopUI2;
using DesktopUI2.Models;
using DesktopUI2.Models.Filters;
using DesktopUI2.ViewModels;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;


namespace Archicad.Launcher
{
	public class ArchicadBinding : ConnectorBindings
	{
		public override string GetActiveViewName ()
		{
			return "TODO KSZ - GetActiveViewName ()";
		}

		public override List<MenuItem> GetCustomStreamMenuItems ()
		{
			// TODO KSZ
			return null;
		}

		public override string GetDocumentId ()
		{
			return "TODO KSZ - GetDocumentId ()";
		}

		public override string GetDocumentLocation ()
		{
			return "TODO KSZ - GetDocumentLocation ()";
		}

		public override string GetFileName ()
		{
			return "TODO KSZ - GetFileName ()";
		}

		public override string GetHostAppName ()
		{
			return "TODO KSZ - GetHostAppName ()";
		}

		public override List<string> GetObjectsInView ()
		{
			// TODO KSZ
			return new List<string> ();
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
