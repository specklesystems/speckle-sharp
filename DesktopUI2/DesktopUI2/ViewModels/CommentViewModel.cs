using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls.Notifications;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using DesktopUI2.Views;
using ReactiveUI;
using Speckle.Core.Api;
using Speckle.Core.Api.GraphQL.Models;
using Speckle.Core.Helpers;
using Speckle.Core.Logging;
using Splat;
using Stream = System.IO.Stream;

namespace DesktopUI2.ViewModels;

public class CommentViewModel : ReactiveObject
{
  private ConnectorBindings Bindings;

  public CommentViewModel(Comment item, string streamId, Client client)
  {
    Comment = item;
    StreamId = streamId;
    _client = client;
    Text = Comment.rawText;

    //use dependency injection to get bindings
    Bindings = Locator.Current.GetService<ConnectorBindings>();

    if (Comment.replies != null)
    {
      foreach (var r in Comment.replies.items)
      {
        var reply = new CommentViewModel(r, streamId, client);
        Replies.Add(reply);
      }
    }
  }

  public Comment Comment { get; set; }
  public string StreamId { get; private set; }
  public Task<AccountViewModel> Author => GetAuthorAsync();
  public Task<Bitmap> Screenshot => Task.FromResult(GetScreenshot());

  //return string.Join("", Comment.text.Doc?.Content.Select(x => string.Join("", x.Content.Select(x => x.Text).ToList())).ToList());
  public string Text { get; private set; }

  public List<CommentViewModel> Replies { get; private set; } = new();

  public Client _client { get; private set; }

  public string RepliesCount
  {
    get
    {
      if (Comment == null)
      {
        return "No replies";
      }

      var s = Comment.replies.totalCount == 1 ? "y" : "ies";
      var c = Comment.replies.totalCount == 0 ? "No" : Comment.replies.totalCount.ToString();
      return $"{c} repl{s}";
    }
  }

  private async Task<AccountViewModel> GetAuthorAsync()
  {
    return await ApiUtils.GetAccount(Comment.authorId, _client).ConfigureAwait(true);
  }

  private Bitmap GetScreenshot()
  {
    var screenshot = Comment.screenshot;
    byte[] bytes = Convert.FromBase64String(screenshot.Split(',')[1]);
    Stream stream = new MemoryStream(bytes);
    return new Bitmap(stream);
  }

  public void OpenCommentView()
  {
    try
    {
      if (Comment.viewerState is { ui.camera.position: not null })
      {
        var camera = Comment.viewerState.ui.camera;
        //Bindings.Open3DView was designed to take the old flat comment.data.camPos
        //We'll shim the comment.viewState to look like the old style camPos
        var oldStyleCoordinates = camera.position.Concat(camera.target).ToList();
        Bindings.Open3DView(oldStyleCoordinates, Comment.id);
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
    string url;
    if (_client.Account.serverInfo.frontend2)
    {
      url = FormatFe2Url();
    }
    else
    {
      url = FormatFe1Url();
    }

    if (url is not null)
    {
      Open.Url(url);
      Analytics.TrackEvent(Analytics.Events.DUIAction, new Dictionary<string, object> { { "name", "Comment View" } });
    }
  }

  private string FormatFe1Url()
  {
    if (Comment.resources == null || !Comment.resources.Any())
    {
      return null;
    }

    var r0 = Comment.resources[0];
    var overlay = "";

    if (Comment.resources.Count > 1)
    {
      overlay = "&overlay=" + string.Join(",", Comment.resources.Skip(1).Select(x => x.resourceId));
    }

    return $"{_client.Account.serverInfo.url}/streams/{StreamId}/{r0.resourceType}s/{r0.resourceId}?cId={Comment.id}{overlay}";
  }

  private string FormatFe2Url()
  {
    if (Comment.viewerResources == null || !Comment.viewerResources.Any())
    {
      return $"{_client.Account.serverInfo.url}/projects/{StreamId}";
    }

    var r0 = Comment.viewerResources[0];
    var resource = new[] { r0.modelId, r0.versionId }.Where(x => x != null);
    var resourceId = string.Join("@", resource);
    return $"{_client.Account.serverInfo.url}/projects/{StreamId}/models/{resourceId}#threadId={Comment.id}";
  }
}
