using System;
using System.Collections.Generic;
using System.Text;
using Autodesk.Revit.DB;

namespace ConverterRevitShared.Extensions
{
  internal static class ConnectorExtensions
  {
    public static string GetUniqueApplicationId(this Connector connector)
    {
      return $"{connector.Owner.UniqueId}.{connector.Id}";
    }
  }
}
