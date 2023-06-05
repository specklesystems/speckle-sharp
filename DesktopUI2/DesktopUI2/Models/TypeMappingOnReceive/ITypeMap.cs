using System;
using System.Collections.Generic;
using System.Text;
using Speckle.Core.Models;

namespace DesktopUI2.Models.TypeMappingOnReceive
{
  public interface ITypeMap
  {
    public IEnumerable<string> Categories { get; }
    public bool HasCategory(string category);
    public IEnumerable<ISingleValueToMap> GetValuesToMapOfCategory(string category);
    public void AddIncomingTypes(Dictionary<string, List<ISingleValueToMap>> mappingValues, out bool newTypesExist);
    public void AddIncomingType(Base @base, string incomingType, string category, string initialGuess, out bool isNewType, bool overwriteExisting = false);
    public IEnumerable<(Base, ISingleValueToMap)> GetAllBasesWithMappings();
  }
}
