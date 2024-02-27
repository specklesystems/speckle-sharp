using System;
using System.Linq;
using Amazon;
using Amazon.Runtime;

namespace Speckle.Core.Transports;

public static class AwsCredentials
{
  public static AWSCredentials? GetCredentials(string? accessKey, string? secretKey)
  {
    if (string.IsNullOrWhiteSpace(accessKey) || string.IsNullOrWhiteSpace(secretKey))
    {
      accessKey = Environment.GetEnvironmentVariable("ACCESS_KEY");
      secretKey = Environment.GetEnvironmentVariable("SECRET_KEY");
      if (string.IsNullOrWhiteSpace(accessKey) || string.IsNullOrWhiteSpace(secretKey))
      {
        return null;
      }
    }

    var creds = new BasicAWSCredentials(accessKey, secretKey);
    return creds;
  }

  public static RegionEndpoint GetRegionEndpoint(string? region)
  {
    if (string.IsNullOrWhiteSpace(region))
    {
      region = Environment.GetEnvironmentVariable("REGION");

      if (string.IsNullOrWhiteSpace(region))
      {
        return RegionEndpoint.EUWest2;
      }
    }

    return RegionEndpoint.EnumerableAllRegions.FirstOrDefault(
        x => string.Equals(x.SystemName, region, StringComparison.CurrentCultureIgnoreCase)
      ) ?? RegionEndpoint.EUWest2;
  }
}
