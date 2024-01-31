using System.Collections;
using System.Collections.Generic;

namespace Speckle.Core.Api.GraphQL.Models;

//TODO: Naming - something that mentions pagination??
public abstract class ResourceCollection<T> : IReadOnlyList<T>
{
  public int totalCount { get; set; }
  public List<T> items { get; set; }
  public string? cursor { get; set; }

  public IEnumerator<T> GetEnumerator() => items.GetEnumerator();

  IEnumerator IEnumerable.GetEnumerator() => items.GetEnumerator();

  public int Count => items.Count;

  public T this[int index] => items[index];
}

//TODO: no current property, check pagination with web team
public sealed class CommentReplyAuthorCollection : ResourceCollection<LimitedUser> { }

public sealed class ProjectCommentCollection : ResourceCollection<Comment>
{
  public int totalArchivedCount { get; set; }
}
