using ReactiveUI;
using Speckle.Core.Api;
using Speckle.Core.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace DesktopUI2.ViewModels
{
  public class CommentViewModel : ReactiveObject
  {
    public CommentItem Comment { get; set; }
    public string StreamId { get; private set; }
    public Task<AccountViewModel> Author => GetAuthorAsync();
    public Task<Avalonia.Media.Imaging.Bitmap> Screenshot => GetScreenshotAsync();

    public string Text
    {
      get
      {
        return string.Join("", Comment.text.Doc.Content.Select(x => string.Join("", x.Content.Select(x => x.Text).ToList())).ToList());
      }
    }

    private async Task<AccountViewModel> GetAuthorAsync()
    {
      return await ApiUtils.GetAccount(Comment.authorId, _client);
    }

    private async Task<Avalonia.Media.Imaging.Bitmap> GetScreenshotAsync()
    {
      var screenshot = await _client.StreamGetCommentScreenshot(Comment.id, StreamId);
      byte[] bytes = Convert.FromBase64String(screenshot.Split(',')[1]);
      System.IO.Stream stream = new MemoryStream(bytes);
      return new Avalonia.Media.Imaging.Bitmap(stream);
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

    public CommentViewModel(CommentItem item, string streamId, Client client)
    {
      Comment = item;
      StreamId = streamId;
      _client = client;
      if (Comment.replies != null)
      {
        foreach (var r in Comment.replies.items)
        {
          var reply = new CommentViewModel(r, streamId, client);
          Replies.Add(reply);
        }
      }
    }

    public void OpenComment()
    {
      if (Comment.resources == null || !Comment.resources.Any())
        return;

      var r0 = Comment.resources[0];
      var overlay = "";

      if (Comment.resources.Count > 1)
        overlay = "&overlay=" + string.Join(",", Comment.resources.Skip(1).Select(x => x.resourceId));


      Process.Start(new ProcessStartInfo($"{_client.Account.serverInfo.url}/streams/{StreamId}/{r0.resourceType}s/{r0.resourceId}?cId={Comment.id}{overlay}") { UseShellExecute = true });
      Analytics.TrackEvent(Analytics.Events.DUIAction, new Dictionary<string, object>() { { "name", "Comment View" } });
    }
  }
}
