using System;
using System.Collections.Generic;
using System.Text;

namespace RevitSharedResources.Interfaces
{
  public interface IConversionSettings
  {
    bool TryGetSettingBySlug(string slug, out string value);
    void SetSettingBySlug(string slug, string value);
  }
}
