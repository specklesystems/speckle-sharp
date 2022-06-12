using ReactiveUI;
using Speckle.Core.Api;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DesktopUI2.ViewModels
{
  public class CommentViewModel : ReactiveObject
  {
    public CommentItem Comment { get; set; }
    public Task<AccountViewModel> Author => GetAuthorAsync();
    private async Task<AccountViewModel> GetAuthorAsync()
    {
      return await ApiUtils.GetAccount(Comment.authorId, _client);
    }

    public List<CommentViewModel> Replies { get; private set; } = new List<CommentViewModel>();

    public Client _client { get; private set; }

    public string RepliesCount
    {
      get
      {
        if (Comment == null)
          return "No replies";
        var s = Comment.replies.totalCount == 1 ? "y" : "ies";
        var c = Comment.replies.totalCount == 0 ? "No" : Comment.replies.totalCount.ToString();
        return $"{c} repl{s}";
      }
    }



    public CommentViewModel(CommentItem item, Client client)
    {
      Comment = item;
      _client = client;
      if (Comment.replies != null)
      {
        foreach (var r in Comment.replies.items)
        {
          var reply = new CommentViewModel(r, client);
          Replies.Add(reply);
        }
      }

    }



  }
}
