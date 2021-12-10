
class ServerSettings {
    public Speckle.Core.Api.Stream Stream;

    public ServerSettings(Speckle.Core.Api.Stream stream)
    {
        Stream = stream;
    }

    public static ServerSettings Initialize(){
        return new ServerSettings(new Speckle.Core.Api.Stream());
    }
}

//1. list of supported applications, with a list of supported versions, and a list of filetypes
