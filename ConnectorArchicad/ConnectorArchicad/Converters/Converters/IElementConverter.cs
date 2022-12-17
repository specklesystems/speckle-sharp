using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Speckle.Core.Models;

namespace Archicad.Converters
{
  public interface IConverter
  {
    #region --- Properties ---

    Type Type { get; }

    #endregion

    #region --- Functions ---

    Task<List<Base>> ConvertToSpeckle(IEnumerable<Model.ElementModelData> elements, CancellationToken token);

    Task<List<string>> ConvertToArchicad(IEnumerable<Base> elements, CancellationToken token);

    #endregion
  }
}
