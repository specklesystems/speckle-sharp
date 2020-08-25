using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Speckle.Core.Api;
using Speckle.Core.Credentials;

namespace WpfSketches
{
  /// <summary>
  /// Interaction logic for MainWindow.xaml
  /// </summary>
  public partial class MainWindow : Window
  {
    public MainWindow()
    {
      InitializeComponent();
      Main();


    }

    private async Task Main()
    {
      //await Auth();

      var client = new Client(AccountManager.GetDefaultAccount());

      client.SubscribeUserStreamCreated();
      client.OnUserStreamCreated += Client_OnUserStreamCreated;

      client.SubscribeStreamUpdated();
      client.OnStreamUpdated += Client_OnStreamUpdated;
    }

    public static async Task Auth()
    {
      // First log in (uncomment the two lines below to simulate a login/register)
      //var myAccount = await AccountManager.AuthenticateConnectors("http://localhost:3000");
      //Console.WriteLine($"Succesfully added/updated account server: {myAccount.serverInfo.url} user email: {myAccount.userInfo.email}");

      //// Second log in (make sure you use two different email addresses :D )
      //var myAccount2 = await AccountManager.Authenticate("http://localhost:3000");

      //AccountManager.SetDefaultAccount(myAccount2.id);

      var accs = AccountManager.GetAccounts().ToList();
      var deff = AccountManager.GetDefaultAccount();

      //Console.WriteLine($"There are {accs.Count} accounts. The default one is {deff.id} {deff.userInfo.email}");

      foreach (var acc in accs)
      {
        var res = await acc.Validate();

        if (res != null)
          Console.WriteLine($"Validated {acc.id}: {acc.serverInfo.url} / {res.email} / {res.name} (token: {acc.token})");
        else
          Console.WriteLine($"Failed to validate acc {acc.id} / {acc.serverInfo.url} / {acc.userInfo.email}");

        try
        {
          await acc.RotateToken();
          Console.WriteLine($"Rotated {acc.id}: {acc.serverInfo.url} / {res.email} / {res.name} (token: {acc.token})");
        }
        catch (Exception e)
        {
          Console.WriteLine($"Failed to rotate acc {acc.id} / {acc.serverInfo.url} / {acc.userInfo.email}");
        }

      }
    }

    private static void Client_OnStreamUpdated(object sender, object e)
    {
      throw new NotImplementedException();
    }

    private static void Client_OnUserStreamCreated(object sender, object e)
    {
      throw new NotImplementedException();
    }
  }
}
