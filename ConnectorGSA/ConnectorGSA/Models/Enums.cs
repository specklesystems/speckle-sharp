namespace ConnectorGSA.Models
{
  public enum StreamMethod
  {
    None,
    Continuous,
    Single
  }

  public enum GsaLoadedFileType
  {
    None,
    NewFile,
    ExistingFile
  }

  /*
  public enum AppState
  {
    NotLoggedIn = 0,
    NotLoggedInLinkedToGsa,
    ActiveLoggingIn,
    ActiveRetrievingStreamList,
    LoggedInNotLinkedToGsa,  
    ReceivingWaiting,     //time between active reception in continuous mode
    ActiveReceiving,
    SendingWaiting,
    ActiveSending,
    OpeningFile,
    SavingFile,
    Ready,   // when not continuously sending or receiving: between send/receive events, this is the regular state of being
    ActiveRenamingStream
  }
  */

  //This is independent of logged-in state although tied to it (i.e. you can't be active at the same time
  //It isn't fully normalised because it's intended to have the entire stream state in one enum value for display purposes
  public enum StreamStateUI
  {
    NotLoggedIn = 0,
    ActiveLoggingIn,
    ActiveRenamingStream,
    ActiveRetrievingStreamList,
    Ready,
    ReceivingWaiting,     //time between active reception in continuous mode
    ActiveReceiving,
    SendingWaiting,
    ActiveSending
  }

  public enum FileState
  {
    None,
    Loading,
    Saving,
    Loaded
  }

  public enum GsaUnit
  {
    None,
    Millimetres,
    Metres,
    Inches
  }

  public enum LoggingMinimumLevel
  {
    None,
    Debug,
    Information,
    Error,
    Fatal
  }

  public enum MainTab
  {
    Server = 0,
    GSA = 1,
    Sender = 2,
    Receiver = 3,
    Settings = 4
  }
}
