using System;
using System.Globalization;
using System.Net;
using Microsoft.Graph;
using Azure.Identity;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace ExcelAddinRequest
{
  class Program
  {
    static void Main(string[] args)
    {
      string _internalDomain = args[0].ToLower(new CultureInfo("en-GB", false));
      string _machineName = Environment.MachineName.ToLower(new CultureInfo("en-GB", false));
      string _domainName = Dns.GetHostEntry("").HostName.ToLower(new CultureInfo("en-GB", false));

      bool _isInternalDomain = _machineName.Contains(_internalDomain) || _domainName.Contains(_internalDomain);

      if (_isInternalDomain)
      {
        string _userEmail = Environment.UserName + "@" + _internalDomain + ".com";

        try
        {
          RunAsync(_userEmail).GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
          Console.WriteLine(ex.Message);
        }
      }
    }

    public class AppConfig
    {
      public string Tenant { get; set; }
      public string ClientId { get; set; }
      public string ClientSecret { get; set; }
      public string GroupId { get; set; }
      public static AppConfig ReadEnvironmentVariables()
      {
        var config = new AppConfig
        {
          Tenant = Environment.GetEnvironmentVariable("EXCEL_ADDIN_APP_TENANT"),
          ClientId = Environment.GetEnvironmentVariable("EXCEL_ADDIN_APP_CLIENT_ID"),
          ClientSecret = Environment.GetEnvironmentVariable("EXCEL_ADDIN_APP_CLIENT_SECRET"),
          GroupId = Environment.GetEnvironmentVariable("EXCEL_ADDIN_APP_GROUP_ID")
        };

        return config;
      }
    }

    private static async Task RunAsync(string _userEmail)
    {
      AppConfig config = AppConfig.ReadEnvironmentVariables();

      // Requested and preconfigured on app registration in Azure
      var scopes = new[] { "https://graph.microsoft.com/.default" };

      // The tenant ID from the Azure portal
      var tenantId = config.Tenant;

      // Values from app registration
      var clientId = config.ClientId;
      var clientSecret = config.ClientSecret;

      // using Azure.Identity;
      var options = new TokenCredentialOptions
      {
        AuthorityHost = AzureAuthorityHosts.AzurePublicCloud
      };

      // https://docs.microsoft.com/dotnet/api/azure.identity.clientsecretcredential
      var clientSecretCredential = new ClientSecretCredential(
          tenantId, clientId, clientSecret, options);

      var graphClient = new GraphServiceClient(clientSecretCredential, scopes);

      var user = await graphClient.Users[_userEmail]
          .Request()
          .GetAsync();

      var membership = await graphClient.Users[user.Id].CheckMemberGroups(new List<string> { config.GroupId })
          .Request()
          .PostAsync();

      bool isMember = membership.Count > 0;

      if (!isMember)
      {
        var directoryObject = new DirectoryObject
        {
          Id = user.Id
        };

        await graphClient.Groups[config.GroupId].Members.References
        .Request()
        .AddAsync(directoryObject);

        Console.WriteLine("You have been added to the Excel Add-in installer request group. The Add-in will now be pushed to your computer from Software Center.");
      }
      else Console.WriteLine("You have already been added to the Excel Add-in installer request group.");
    }
  }
}
