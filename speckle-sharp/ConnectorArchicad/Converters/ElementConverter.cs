using Objects.BuiltElements.Archicad;
using Objects.BuiltElements.Archicad.Model;
using Speckle.Core.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;


namespace Archicad.Operations
{
	public static class ElementConverter
	{
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
						AddToObject (commitObject, "walls", walls);
						break;

					case "Slab":
						IEnumerable<Base> ceilings = await CreateCeilingObjects (FilterObjectByType (models, elem.Value), token);
						AddToObject (commitObject, "ceilings", ceilings);
						break;

					default:    //unsuported type
						unsupportedBuildingElements.AddRange (elem.Value.Select (e => new DirectShape (models[e])));
						break;
				}
			}

			if (unsupportedBuildingElements.Count > 0)
			{
				commitObject["directShapes"] = unsupportedBuildingElements;
			}

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

		#endregion
	}
}