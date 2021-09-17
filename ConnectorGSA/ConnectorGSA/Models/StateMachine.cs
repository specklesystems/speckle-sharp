using System;
using System.Collections.Generic;
using System.Linq;

namespace ConnectorGSA.Models
{
  public class StateMachine
  {
    //Overall state - the main output of this class
    //public AppState State { get => stateFns.Keys.FirstOrDefault(k => stateFns[k]()); }
    public bool StreamFileIsOccupied { get => StreamIsOccupied && (commContent == ServerCommContent.Objects); }
    public bool StreamIsOccupied { get => (commState == ServerCommState.Active || commState == ServerCommState.Pending); }
    public bool FileIsOccupied { get => (FileState == FileState.Loading || FileState == FileState.Saving); }
    //The state is effectively three parallel tracks - based on the concept that these states can both be in an "active" state at the same time
    //- e.g. you can be loading a file while doing account-related activies, or renaming a stream while saving the file
    //Note: this currently does tie account operations (log in, get stream list) and stream reception/transmission operations together into a mutually-exclusive arrangement.  This could be broken up
    //and separated at a later point
    public FileState FileState { get; set; } = FileState.None;
    //public ServerCommState StreamState { get; set; } = ServerCommState.None;
    public StreamStateUI StreamState { get => streamStateFns.Keys.FirstOrDefault(k => streamStateFns[k]()); }
    public bool LoggedInState { get => loggedIn; }

    #region private_state_variables
    //Don't need previous state as the information embodied in such a variable is stored in the variables below
    private bool loggedIn = false;
    private bool prevFileLoaded = false;

    private StreamMethod streamMethod = StreamMethod.None;
    private ServerCommDirection direction = ServerCommDirection.None;
    private ServerCommContent commContent = ServerCommContent.None;
    private ServerCommState commState = ServerCommState.None;
    #endregion

    #region private_condition_fns

    private bool Is(ServerCommState scs, ServerCommDirection dir, ServerCommContent cont) => (commState == scs && direction == dir && commContent == cont);
    private bool Is(FileState fs, ServerCommState scs, ServerCommDirection dir, ServerCommContent cont) => (FileState == fs && commState == scs && direction == dir && commContent == cont);
    private void Set(ServerCommState scs, ServerCommDirection dir, ServerCommContent cont)
    {
      commState = scs;
      direction = dir;
      commContent = cont;
    }
    #endregion

    private readonly Dictionary<StreamStateUI, Func<bool>> streamStateFns;

    public StateMachine()
    {
      /*
      stateFns = new Dictionary<AppState, Func<bool>>()
      {
        { AppState.NotLoggedIn,                () => !loggedIn && !StreamFileIsOccupied && FileState != FileState.Loaded },
        { AppState.NotLoggedInLinkedToGsa,     () => !loggedIn && !StreamFileIsOccupied && FileState == FileState.Loaded },
        { AppState.ActiveLoggingIn,            () => !loggedIn && Is(ServerCommState.Active, ServerCommDirection.From, ServerCommContent.Accounts) },
        { AppState.ActiveRetrievingStreamList, () => !loggedIn && Is(ServerCommState.Active, ServerCommDirection.From, ServerCommContent.StreamInfo) },
        { AppState.LoggedInNotLinkedToGsa,     () => loggedIn && FileState != FileState.Loaded && !StreamFileIsOccupied },
        { AppState.OpeningFile,                () => FileState == FileState.Loading },
        { AppState.ReceivingWaiting,           () => loggedIn && Is(FileState.Loaded, ServerCommState.Pending, ServerCommDirection.From, ServerCommContent.Objects) },
        { AppState.ActiveReceiving,            () => loggedIn && Is(FileState.Loaded, ServerCommState.Active, ServerCommDirection.From, ServerCommContent.Objects) },
        { AppState.SendingWaiting,             () => loggedIn && Is(FileState.Loaded, ServerCommState.Pending, ServerCommDirection.To, ServerCommContent.Objects) },
        { AppState.ActiveSending,              () => loggedIn && Is(FileState.Loaded, ServerCommState.Active, ServerCommDirection.To, ServerCommContent.Objects) },
        { AppState.Ready,                      () => loggedIn && Is(FileState.Loaded, ServerCommState.None, ServerCommDirection.None, ServerCommContent.None) },
        { AppState.SavingFile,                 () => Is(FileState.Saving, ServerCommState.None, ServerCommDirection.None, ServerCommContent.None) },
        { AppState.ActiveRenamingStream,       () => loggedIn && Is(ServerCommState.Active, ServerCommDirection.To, ServerCommContent.StreamInfo) }
      };
      */
      streamStateFns = new Dictionary<StreamStateUI, Func<bool>>()
      {
        { StreamStateUI.NotLoggedIn,                () => !loggedIn && commState == ServerCommState.None },
        { StreamStateUI.ActiveLoggingIn,            () => !loggedIn && commState == ServerCommState.Active && commContent == ServerCommContent.Accounts && direction == ServerCommDirection.To },
        { StreamStateUI.ActiveRenamingStream,       () => loggedIn && commState == ServerCommState.Active && commContent == ServerCommContent.StreamInfo && direction == ServerCommDirection.To },
        { StreamStateUI.ActiveRetrievingStreamList, () => loggedIn && commState == ServerCommState.Active && commContent == ServerCommContent.StreamInfo && direction == ServerCommDirection.From },
        { StreamStateUI.Ready,                      () => loggedIn && commState == ServerCommState.None },
        { StreamStateUI.ReceivingWaiting,           () => loggedIn && commState == ServerCommState.Pending && commContent == ServerCommContent.Objects && direction == ServerCommDirection.From },
        { StreamStateUI.ActiveReceiving,            () => loggedIn && commState == ServerCommState.Active && commContent == ServerCommContent.Objects && direction == ServerCommDirection.From },
        { StreamStateUI.SendingWaiting,             () => loggedIn && commState == ServerCommState.Pending && commContent == ServerCommContent.Objects && direction == ServerCommDirection.To },
        { StreamStateUI.ActiveSending,              () => loggedIn && commState == ServerCommState.Active && commContent == ServerCommContent.Objects && direction == ServerCommDirection.To },
      };
    }

    public void StartedLoggingIn()
    {
      if (!loggedIn && !StreamFileIsOccupied)
      {
        //No need to change the state of the file, nor the loggedIn boolean as that serves as the "previous" logged-in state, useful if the logging in is cancelled
        Set(ServerCommState.Active, ServerCommDirection.To, ServerCommContent.Accounts);
      }
    }

    public void CancelledLoggingIn()
    {
      if (Is(ServerCommState.Active, ServerCommDirection.To, ServerCommContent.Accounts))
      {
        //No need to set the loggedIn value as that will essentially revert to how it was
        //No need to change the state of the file
        Set(ServerCommState.None, ServerCommDirection.None, ServerCommContent.None);
      }
    }

    public void LoggedIn()
    {
      if (Is(ServerCommState.Active, ServerCommDirection.To, ServerCommContent.Accounts))
      {
        loggedIn = true;
        //No need to change the state of the file
        Set(ServerCommState.None, ServerCommDirection.None, ServerCommContent.None);
      }
    }

    public void EnteredReceivingMode(StreamMethod streamMethod) => EnteredStreamingMode(ServerCommDirection.From, streamMethod);

    //Used for sending instances after the first one (which is handled by the EnteredSendingMode method)
    public void StartedTriggeredSending()
    {
      if (loggedIn && streamMethod == StreamMethod.Continuous && Is(ServerCommState.Pending, ServerCommDirection.To, ServerCommContent.Objects))
      {
        Set(ServerCommState.Active, ServerCommDirection.To, ServerCommContent.Objects);
      }
    }

    public void StoppedTriggeredSending()
    {
      if (loggedIn && streamMethod == StreamMethod.Continuous && Is(ServerCommState.Active, ServerCommDirection.To, ServerCommContent.Objects))
      {
        Set(ServerCommState.Pending, ServerCommDirection.To, ServerCommContent.Objects);
      }
    }

    //This same method is called at the end of an active receive event and the end of being in the state of continuous receive
    public void StoppedReceiving() => StoppedStreaming(ServerCommDirection.From);

    public void EnteredSendingMode(StreamMethod streamMethod) => EnteredStreamingMode(ServerCommDirection.To, streamMethod);

    //This same method is called at the end of an active send event and the end of being in the state of continuous send
    public void StoppedSending() => StoppedStreaming(ServerCommDirection.To);

    public void StartedUpdatingStreams()
    {
      if (loggedIn && !StreamFileIsOccupied)
      {
        //File state isn't relevant here
        Set(ServerCommState.Active, ServerCommDirection.From, ServerCommContent.StreamInfo);
      }
    }

    public void StoppedUpdatingStreams()
    {
      if (Is(ServerCommState.Active, ServerCommDirection.From, ServerCommContent.StreamInfo))
      {
        Set(ServerCommState.None, ServerCommDirection.None, ServerCommContent.None);
      }
    }


    public void StartedSavingFile()
    {
      if (FileState == FileState.Loaded && !StreamFileIsOccupied)
      {
        //logged in state isn't strictly relevant here
        this.FileState = FileState.Saving;
      }
    }

    public void StoppedSavingFile()
    {
      if (FileState == FileState.Saving)
      {
        FileState = FileState.Loaded;
      }
    }

    public void StartedOpeningFile()
    {
      if (!FileIsOccupied)
      {
        FileState = FileState.Loading;
      }
    }

    public void OpenedFile()
    {
      if (FileState == FileState.Loading)
      {
        FileState = FileState.Loaded;
        prevFileLoaded = true;
      }
    }

    public void CancelledOpeningFile()
    {
      if (FileState == FileState.Loading)
      {
        FileState = prevFileLoaded ? FileState.Loaded : FileState.None;
      }
    }

    public void StartedRenamingStream()
    {
      if (loggedIn && FileState == FileState.Loaded && !StreamFileIsOccupied)
      {
        Set(ServerCommState.Active, ServerCommDirection.To, ServerCommContent.StreamInfo);
      }
    }

    public void StoppedRenamingStream()
    {
      if (Is(ServerCommState.Active, ServerCommDirection.To, ServerCommContent.StreamInfo))
      {
        Set(ServerCommState.None, ServerCommDirection.None, ServerCommContent.None);
      }
    }

    private void EnteredStreamingMode(ServerCommDirection direction, StreamMethod streamMethod)
    {
      if (loggedIn && FileState == FileState.Loaded && ((commState == ServerCommState.Pending && streamMethod == StreamMethod.Continuous) || !StreamFileIsOccupied))
      {
        this.streamMethod = streamMethod;
        //StreamState won't be None as that is filtered out by the IsOccupied check
        Set(ServerCommState.Active, direction, ServerCommContent.Objects);
      }
    }

    private void StoppedStreaming(ServerCommDirection direction)
    {
      if (Is(ServerCommState.Active, direction, ServerCommContent.Objects))
      {
        if (streamMethod == StreamMethod.Single)
        {
          Set(ServerCommState.None, ServerCommDirection.None, ServerCommContent.None);
        }
        else if (streamMethod == StreamMethod.Continuous)
        {
          Set(ServerCommState.Pending, direction, ServerCommContent.Objects);
        }
      }
      else if (Is(ServerCommState.Pending, direction, ServerCommContent.Objects))
      {
        Set(ServerCommState.None, ServerCommDirection.None, ServerCommContent.None);
      }
    }

    #region private_enums

    //These enums make the state a bit more "normalised" (using the relational database concept there)

    private enum ServerCommContent
    {
      None,
      Accounts,
      StreamInfo,
      Objects
    }

    private enum ServerCommDirection
    {
      None,
      From,
      To
    }

    private enum ServerCommState
    {
      None,
      Active,
      Pending
    }

    #endregion
  }

}
