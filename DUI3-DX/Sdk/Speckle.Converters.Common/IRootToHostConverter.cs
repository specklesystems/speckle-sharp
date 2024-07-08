using Speckle.Core.Models;

namespace Speckle.Converters.Common;

public interface IRootToHostConverter
{
  object Convert(Base target);
}
