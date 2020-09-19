using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Speckle.Core.Api;
using Speckle.Core.Credentials;
using Speckle.DesktopUI.Accounts;

namespace Speckle.DesktopUI.Streams
{
  class StreamsRepository
  {
    private AccountsRepository _acctRepo = new AccountsRepository();

    public Branch GetMasterBranch(List<Branch> branches)
    {
      Branch master = branches.Find(branch => branch.name == "master");
      return master;
    }

    public Task<string> CreateStream(Stream stream, Account account)
    {
      var client = new Client(account);

      var streamInput = new StreamCreateInput()
      {
        name = stream.name,
        description = stream.description,
        isPublic = stream.isPublic
      };

      return client.StreamCreate(streamInput);
    }

    public Task<Stream> GetStream(string streamId, Account account)
    {
      var client = new Client(account);
      return client.StreamGet(streamId);
    }

    public ObservableCollection<Stream> LoadTestStreams()
    {
      var collabs = new List<Collaborator>()
      {
        new Collaborator
        {
        id = "123",
        name = "Matteo Cominetti",
        role = "stream:contributor"
        },
        new Collaborator
        {
        id = "321",
        name = "Izzy Lyseggen",
        role = "stream:owner"
        },
        new Collaborator
        {
        id = "456",
        name = "Dimitrie Stefanescu",
        role = "stream:contributor"
        }
      };
      var branches = new Branches()
      {
        totalCount = 2,
        items = new List<Branch>()
        {
        new Branch()
        {
        id = "123",
        name = "master",
        commits = new Commits()
        {
        totalCount = 5,
        }
        },
        new Branch()
        {
        id = "321",
        name = "dev"
        }
        }
      };

      var TestStreams = new ObservableCollection<Stream>()
      {
        new Stream
        {
        id = "stream123",
        name = "Random Stream here 👋",
        description = "this is a test stream",
        isPublic = true,
        collaborators = collabs.GetRange(0, 2),
        branches = branches
        },
        new Stream
        {
        id = "stream456",
        name = "Another Random Stream 🌵",
        description = "we like to test things",
        isPublic = true,
        collaborators = collabs,
        branches = branches
        },
        new Stream
        {
        id = "stream789",
        name = "Woop Cool Stream 🌊",
        description = "cool and good indeed",
        isPublic = true,
        collaborators = collabs.GetRange(1, 2),
        branches = branches
        },
      };

      return TestStreams;
    }

  }
}
