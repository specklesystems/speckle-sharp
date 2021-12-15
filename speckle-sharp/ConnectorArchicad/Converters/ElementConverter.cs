using Objects.BuiltElements.Archicad;
using Objects.BuiltElements.Archicad.Model;
using Speckle.Core.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DesktopUI2.Models;


namespace Archicad.Operations
{
	public static class ElementConverter
	{
		private const string WallExportName = "walls";
		private const string SlabExportName = "ceilings";
		private const string UnsupportedElementExportName = "directShapes";

		#region --- Functions ---

		public static async Task<Base> Convert (IEnumerable<string> elementIds, CancellationToken token)
		{
			IEnumerable<ElementModelData> rawModels = await Communication.AsyncCommandProcessor.Instance.Execute (new Communication.Commands.GetModelForElements (elementIds), token);
			if (rawModels is null)
			{
				return null;
			}

			Dictionary<string, ElementModelData> models = rawModels.ToDictionary (m => m.elementId, m => m);

			//get types -> dictionary group by type
			Dictionary<string, IEnumerable<string>> types = await Communication.AsyncCommandProcessor.Instance.Execute (new Communication.Commands.GetElementsType (elementIds), token);
			if (types is null)
			{
				return null;
			}


			//build up commit object depending on elem types
			Base commitObject = new Base ();
			List<DirectShape> unsupportedBuildingElements = new List<DirectShape> ();
			foreach (var elem in types)
			{
				switch (elem.Key)
				{
					case "Wall":
						IEnumerable<Base> walls = await CreateWallObjects (FilterObjectByType (models, elem.Value), token);
						AddToObject (commitObject, WallExportName, walls);
						break;

					case "Slab":
						IEnumerable<Base> ceilings = await CreateCeilingObjects (FilterObjectByType (models, elem.Value), token);
						AddToObject (commitObject, SlabExportName, ceilings);
						break;

					default:    //unsuported type
						unsupportedBuildingElements.AddRange (elem.Value.Select (e => new DirectShape (models[e])));
						break;
				}
			}

			AddToObject(commitObject, UnsupportedElementExportName, unsupportedBuildingElements);

			return commitObject;
		}

		private static async Task<IEnumerable<Base>> CreateWallObjects (IDictionary<string, ElementModelData> elementModels, CancellationToken token)
		{
			IEnumerable<WallData> datas = await Communication.AsyncCommandProcessor.Instance.Execute (new Communication.Commands.GetWallData (elementModels.Keys), token);
			if (datas is null)
			{
				return Enumerable.Empty<Base> ();
			}

			List<Wall> walls = new List<Wall> ();
			foreach (WallData wallData in datas)
			{
				walls.Add (new Wall (wallData, elementModels[wallData.elementId]));
			}

			return walls;
		}

		private static async Task<IEnumerable<Base>> CreateCeilingObjects (IDictionary<string, ElementModelData> elementModels, CancellationToken token)
		{
			IEnumerable<CeilingData> datas = await Communication.AsyncCommandProcessor.Instance.Execute (new Communication.Commands.GetCeilingData (elementModels.Keys), token);
			if (datas is null)
			{
				return Enumerable.Empty<Base> ();
			}

			List<Ceiling> ceilings = new List<Ceiling> ();
			foreach (CeilingData ceilingData in datas)
			{
				ceilings.Add (new Ceiling (ceilingData, elementModels[ceilingData.elementId]));
			}

			return ceilings;
		}

		private static IDictionary<string, ElementModelData> FilterObjectByType (IDictionary<string, ElementModelData> models, IEnumerable<string> ids)
		{
			return models.Where (m => ids.Contains (m.Key)).ToDictionary (k => k.Key, v => v.Value);
		}

		private static void AddToObject (Base @object, string key, IEnumerable<Base> value)
		{
			if (!value.Any ())
			{
				return;
			}

			@object[key] = value;
		}

		public static async Task<StreamState> ConvertBack (Base commitObject, StreamState state, CancellationToken token)
		{
			ImportWalls (Extract<Wall> (commitObject, WallExportName), token);
			//ImportSlabs (Extract<Ceiling> (commitObject, SlabExportName), token);
			//ImportMophs (Extract<DirectShape> (commitObject, UnsupportedElementExportName), token);

			return state;
		}

		private static IEnumerable<TObject> Extract<TObject> (Base baseObject, string fieldName) where TObject : Base
		{
			if (!baseObject.GetDynamicMemberNames ().Contains (fieldName))
				return Enumerable.Empty<TObject> ();

			if (baseObject[fieldName] is IEnumerable<object> objects)
				return objects.OfType<TObject> ();

			return Enumerable.Empty<TObject> ();
		}

		private static async void ImportWalls(IEnumerable<Wall> walls, CancellationToken token)
		{
			if (!walls.Any ())
				return;   //there is nothing to import
			IEnumerable<WallData> wallDatas = walls.Select (x => x.WallData);
			IEnumerable<string> successedIds = await Communication.AsyncCommandProcessor.Instance.Execute (new Communication.Commands.CreateWall (wallDatas), token);
			if (successedIds.Count() != wallDatas.Count())
				return;  //error? can we do something?
			return;
		}

		private static async void ImportSlabs (IEnumerable<Ceiling> slabs, CancellationToken token)
		{
			if (!slabs.Any ())
				return;   //there is nothing to import
			IEnumerable<CeilingData> slabDatas = slabs.Select (x => x.CeilingData);
			IEnumerable<string> successedIds = await Communication.AsyncCommandProcessor.Instance.Execute (new Communication.Commands.CreateCeiling (slabDatas), token);
			if (successedIds.Count () != slabDatas.Count ())
				return;  //error? can we do something?
			return;
		}

		private static async void ImportMophs (IEnumerable<DirectShape> shapes, CancellationToken token)
		{
			//TODO KSZ
			if (!shapes.Any ())
				return;   //there is nothing to import
		}

		#endregion
	}
}