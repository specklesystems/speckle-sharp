#if ADVANCESTEEL
using System;
using System.Collections.Generic;
using System.Text;

namespace Objects.Converter.AutocadCivil;

public interface IASProperties
{
  Type ObjectType { get; }

  Dictionary<string, ASProperty> BuildedPropertyList();
}
#endif
