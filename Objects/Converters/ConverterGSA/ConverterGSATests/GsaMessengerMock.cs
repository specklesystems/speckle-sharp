using Speckle.GSA.API;
using System;

namespace ConverterGSATests
{
  internal class GsaMessengerMock : IGSAMessenger
  {
    public bool Message(MessageIntent intent, MessageLevel level, params string[] messagePortions) => true;

    public bool Message(MessageIntent intent, MessageLevel level, Exception ex, params string[] messagePortions) => true;
  }
}