using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls.Notifications;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using DesktopUI2.Models;
using DesktopUI2.Views;
using ReactiveUI;
using Speckle.Core.Api;
using Speckle.Core.Logging;
using Splat;
using Stream = System.IO.Stream;

namespace DesktopUI2.ViewModels;

public class CommentViewModel : ReactiveObject
{
  private ConnectorBindings Bindings;

  public CommentViewModel(CommentItem item, string streamId, Client client)
  {
    Comment = item;
    StreamId = streamId;
    _client = client;
    Text = Comment.rawText;

    //use dependency injection to get bindings
    Bindings = Locator.Current.GetService<ConnectorBindings>();

    if (Comment.replies != null)
      foreach (var r in Comment.replies.items)
      {
        var reply = new CommentViewModel(r, streamId, client);
        Replies.Add(reply);
      }
  }

  public CommentItem Comment { get; set; }
  public string StreamId { get; private set; }
  public Task<AccountViewModel> Author => GetAuthorAsync();
  public Task<Bitmap> Screenshot => GetScreenshotAsync();

  //return string.Join("", Comment.text.Doc?.Content.Select(x => string.Join("", x.Content.Select(x => x.Text).ToList())).ToList());
  public string Text { get; private set; }

  public List<CommentViewModel> Replies { get; private set; } = new();

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

  private async Task<AccountViewModel> GetAuthorAsync()
  {
    return await ApiUtils.GetAccount(Comment.authorId, _client).ConfigureAwait(true);
  }

  private async Task<Bitmap> GetScreenshotAsync()
  {
    var screenshot = await _client.StreamGetCommentScreenshot(Comment.id, StreamId).ConfigureAwait(true);
    byte[] bytes = Convert.FromBase64String(screenshot.Split(',')[1]);
    Stream stream = new MemoryStream(bytes);
    return new Bitmap(stream);
  }

  public void OpenCommentView()
  {
    try
    {
      if (Comment.data != null && Comment.data.camPos != null)
      {
        Bindings.Open3DView(Comment.data.camPos, Comment.id);
        Analytics.TrackEvent(
          Analytics.Events.DUIAction,
          new Dictionary<string, object> { { "name", "Comment Open 3D View" } }
        );
        return;
      }
    }
    catch (Exception ex)
    {
      SpeckleLog.Logger.Error(ex, "Failed to open comment view {exceptionMessage}", ex.Message);
    }

    //something went wrong
    Dispatcher.UIThread.Post(() =>
    {
      MainUserControl.NotificationManager.Show(
        new PopUpNotificationViewModel
        {
          Title = "Could not open view!",
          Message = "Something went wrong",
          Type = NotificationType.Error
        }
      );
    });
  }

  public void OpenComment()
  {
    if (Comment.resources == null || !Comment.resources.Any())
      return;

    var r0 = Comment.resources[0];
    var overlay = "";

    if (Comment.resources.Count > 1)
      overlay = "&overlay=" + string.Join(",", Comment.resources.Skip(1).Select(x => x.resourceId));

    var url =
      $"{_client.Account.serverInfo.url}/streams/{StreamId}/{r0.resourceType}s/{r0.resourceId}?cId={Comment.id}{overlay}";
    var config = ConfigManager.Load();
    if (config.UseFe2)
      url = $"{_client.Account.serverInfo.url}/projects/{StreamId}/";

    Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
    Analytics.TrackEvent(Analytics.Events.DUIAction, new Dictionary<string, object> { { "name", "Comment View" } });
  }
}
