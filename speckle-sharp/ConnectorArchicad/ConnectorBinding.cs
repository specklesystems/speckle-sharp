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
		
		private static Dictionary<K, V> ToDictionary<K, V> (List<K> keys, List<V> values) where K : notnull
		{
			if (keys.Count () != values.Count ()) return null;
			
			Dictionary<K, V> result = new Dictionary<K, V> ();
			for (int i = 0; i < keys.Count (); i++)
				result.Add (keys[i], values[i]);

			return result;
		}

		private async Task<Base> CreateCommitObject (IEnumerable<string> elementIds, CancellationToken token)
		{
			//get models -> build dictionary
			IEnumerable<Model.ElementModel> rawModels = await Communication.AsyncCommandProcessor.Instance.Execute (new Communication.Commands.GetModerlForElements (elementIds), token);
			if (rawModels is null) return null;
			Dictionary<string, IEnumerable<Model.MeshData>> models = new Dictionary<string, IEnumerable<Model.MeshData>> ();
			foreach (Model.ElementModel elem in rawModels) models.Add (elem.ElementId, elem.Model);

			//get types -> build dictionary
			IEnumerable<string> rawTypes = await Communication.AsyncCommandProcessor.Instance.Execute (new Communication.Commands.GetElementsType (elementIds), token);
			Dictionary<string, string> types = ToDictionary(elementIds.ToList (), rawTypes.ToList ());
			if (types is null) return null;

			//"sorting" by supporting element (type)
			Dictionary<string, List<string>> sortedByType = new Dictionary<string, List<string>> ();
			foreach (var elem in types)
			{
				if (sortedByType.ContainsKey (elem.Value) == false)
					sortedByType[elem.Value] = new List<string> ();
				sortedByType[elem.Value].Add (elem.Key);
			}

			//build up commit object depending on elem types
			Base commitObject = new Base ();
			List<Objects.DirectShape> meshes = new List<Objects.DirectShape>();
			foreach (var elem in sortedByType)
				switch (elem.Key)
				{
					case "Wall":
						IEnumerable<Model.Wall> walls = await Communication.AsyncCommandProcessor.Instance.Execute (new Communication.Commands.GetWallData (elem.Value), token);
						///if (rawWalls is null) i dont know... error 
						foreach (var wall in walls) wall.Visualization = new Objects.DirectShape (models[wall.ElementId]);
						commitObject["Walls"] = walls;
						break;

					default:	//unsuported type
						foreach (var guid in elem.Value) meshes.Add (new Objects.DirectShape (models[guid]));
						break;
				}
			if (meshes.Count () > 0)
				commitObject["BuildingElements"] = meshes;

			return commitObject;
		}
	}
}
