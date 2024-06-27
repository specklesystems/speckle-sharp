using System.Collections.Generic;

namespace Speckle.Core.Api.GraphQL.Models;

//this object is not in the schema
public sealed class CommentData
{
  public Comments comments { get; init; }
  public List<double> camPos { get; init; }
  public object filters { get; init; }
  public Location location { get; init; }
  public object selection { get; init; }
  public object sectionBox { get; init; }
}
