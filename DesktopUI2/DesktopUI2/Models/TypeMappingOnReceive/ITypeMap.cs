using System;
using System.Collections.Generic;
using System.Text;
using Speckle.Core.Models;

namespace DesktopUI2.Models.TypeMappingOnReceive
{
  public interface ITypeMap
  {
    public IEnumerable<string> Categories { get; }
    public IEnumerable<ISingleValueToMap> GetValuesToMapOfCategory(string category);
    public void AddIncomingType(Base @base, string incomingType, string incomingFamily, string category, string initialGuess, out bool isNewType, bool overwriteExisting = false);
    public IEnumerable<(Base, ISingleValueToMap)> GetAllBasesWithMappings();
    public ISingleValueToMap? TryGetMappingValueInCategory(string category, string incomingType);
  }
}
