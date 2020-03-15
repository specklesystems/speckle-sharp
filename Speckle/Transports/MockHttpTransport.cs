using System;
using System.Collections.Generic;

namespace Speckle.Transports
{
  public class MockHttpTransport : ITransport
  {
    public Dictionary<string, string> Objects = new Dictionary<string, string>();

    public MockHttpTransport()
    {
    }

    public string TransportName { get; set; } = "Mock HTTP Transport";

    public string GetObject(string hash)
    {
      throw new NotImplementedException();
    }

    public void SaveObject(string hash, string serializedObject, bool overwrite = false)
    {
      throw new NotImplementedException();
    }
  }
}
