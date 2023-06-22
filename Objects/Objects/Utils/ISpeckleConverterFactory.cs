using System;
using System.Collections.Generic;
using System.Text;
using Speckle.Core.Kits;

namespace Objects.Utils
{
  public interface ISpeckleConverterFactory
  {
    ISpeckleConverter Create();
  }
}
