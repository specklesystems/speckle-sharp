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
			IEnumerable<Model.ElementModelData> rawModels = await Communication.AsyncCommandProcessor.Instance.Execute (new Communication.Commands.GetModelForElements (elementIds), token);
			if (rawModels is null)
			{
				return null;
			}

			Dictionary<string, Model.ElementModelData> models = rawModels.ToDictionary (m => m.ElementId, m => m);

			//get types -> dictionary group by type
			Dictionary<string, IEnumerable<string>> types = await Communication.AsyncCommandProcessor.Instance.Execute (new Communication.Commands.GetElementsType (elementIds), token);
			if (types is null)
			{
				return null;
			}


			//build up commit object depending on elem types
			Base commitObject = new Base ();
			List<Objects.DirectShape> unsupportedBuildingElements = new List<Objects.DirectShape> ();
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
						unsupportedBuildingElements.AddRange (elem.Value.Select (e => new Objects.DirectShape (models[e])));
						break;
				}
			}

			if (unsupportedBuildingElements.Count > 0)
			{
				commitObject["directShapes"] = unsupportedBuildingElements;
			}

			return commitObject;
		}

		private static async Task<IEnumerable<Base>> CreateWallObjects (IDictionary<string, Model.ElementModelData> elementModels, CancellationToken token)
		{
			IEnumerable<Model.WallData> datas = await Communication.AsyncCommandProcessor.Instance.Execute (new Communication.Commands.GetWallData (elementModels.Keys), token);
			if (datas is null)
			{
				return Enumerable.Empty<Base> ();
			}

			List<Objects.Wall> walls = new List<Objects.Wall> ();
			foreach (Model.WallData wallData in datas)
			{
				walls.Add (new Objects.Wall (wallData, elementModels[wallData.ElementId]));
			}

			return walls;
		}

		private static async Task<IEnumerable<Base>> CreateCeilingObjects (IDictionary<string, Model.ElementModelData> elementModels, CancellationToken token)
		{
			IEnumerable<Model.CeilingData> datas = await Communication.AsyncCommandProcessor.Instance.Execute (new Communication.Commands.GetCeilingData (elementModels.Keys), token);
			if (datas is null)
			{
				return Enumerable.Empty<Base> ();
			}

			List<Objects.Ceiling> ceilings = new List<Objects.Ceiling> ();
			foreach (Model.CeilingData ceilingData in datas)
			{
				ceilings.Add (new Objects.Ceiling (ceilingData, elementModels[ceilingData.ElementId]));
			}

			return ceilings;
		}

		private static IDictionary<string, Model.ElementModelData> FilterObjectByType (IDictionary<string, Model.ElementModelData> models, IEnumerable<string> ids)
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