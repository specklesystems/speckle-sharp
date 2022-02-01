using Speckle.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;


namespace Archicad
{
	public sealed class ElementConverterManager
	{
		#region --- Fields ---

		public static ElementConverterManager Instance { get; } = new ElementConverterManager ();

		private Dictionary<Type, Converters.IConverter> Converters { get; } = new Dictionary<Type, Converters.IConverter> ();

		private Converters.IConverter DefaultConverter { get; } = new Converters.DirectShape ();

		#endregion


		#region --- Ctor \ Dtor ---

		private ElementConverterManager ()
		{
			RegisterConverters ();
		}

		#endregion


		#region --- Functions ---

		public async Task<Base> ConvertToSpeckle (IEnumerable<string> elementIds, CancellationToken token)
		{
			IEnumerable<Model.ElementModelData> rawModels = await Communication.AsyncCommandProcessor.Instance.Execute (new Communication.Commands.GetModelForElements (elementIds), token);
			if (rawModels is null)
			{
				return null;
			}

			Dictionary<Type, IEnumerable<string>> elementTypeTable = await Communication.AsyncCommandProcessor.Instance.Execute (new Communication.Commands.GetElementsType (elementIds), token);
			if (elementTypeTable is null)
			{
				return null;
			}

			List<Base> elements = new List<Base> ();
			foreach (var typeTableEntry in elementTypeTable)
			{
				Converters.IConverter converter = GetConverterForElement (typeTableEntry.Key);
				List<Base> convertedElements = await converter.ConvertToSpeckle (rawModels.Where (model => typeTableEntry.Value.Contains (model.elementId)), token);
				elements.AddRange (convertedElements);
			}

			if (elements.Count == 0)
			{
				return null;
			}

			Base commitObject = new Base ();
			commitObject["Elements"] = elements;

			return commitObject;
		}

		public async Task<List<string>> ConvertToNative (Base obj, CancellationToken token)
		{
			List<string> result = new List<string> ();

			List<object>? elements = obj["Elements"] as List<object>;
			if (elements is null)
			{
				return result;
			}

			Dictionary<Type, IEnumerable<Base>> elementTypeTable = elements.GroupBy (element => element.GetType ()).ToDictionary (group => group.Key, group => group.Cast<Base> ());
			foreach (var elementTableEntry in elementTypeTable)
			{
				Converters.IConverter converter = GetConverterForElement (elementTableEntry.Key);
				List<string> convertedElementIds = await converter.ConvertToArchicad (elementTableEntry.Value, token);
				result.AddRange (convertedElementIds);
			}

			return result;
		}

		private void RegisterConverters ()
		{
			IEnumerable<Type> convertes = Assembly.GetExecutingAssembly ().GetTypes ().Where (t => t.IsClass && !t.IsAbstract && typeof (Converters.IConverter).IsAssignableFrom (t));

			foreach (Type converterType in convertes)
			{
				Converters.IConverter? converter = Activator.CreateInstance (converterType) as Converters.IConverter;
				if (converter?.Type is null)
				{
					continue;
				}

				Converters.Add (converter.Type, converter);
			}
		}

		private Converters.IConverter GetConverterForElement (Type elementType)
		{
			return Converters.ContainsKey (elementType) ? Converters[elementType] : DefaultConverter;
		}
		

		#endregion
	}
}