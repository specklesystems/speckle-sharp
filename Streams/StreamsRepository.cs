using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Speckle.Core;
using Speckle.Core.Api.GqlModels;
using System.Collections.ObjectModel;
using Speckle.Core.Api;
using Speckle.DesktopUI.Accounts;
using Speckle.Core.Credentials;

namespace Speckle.DesktopUI.Streams
{
    class StreamsRepository
    {
        private AccountsRepository _acctRepo = new AccountsRepository();
        private Remote _remote
        {
          get
          {
            var defaultAccount = AccountManager.GetDefaultAccount();
            if (defaultAccount != null)
              return new Remote(defaultAccount);
            return new Remote();
          }

        }

    public Branch GetMasterBranch(List<Branch> branches)
        {
            Branch master = branches.Find(branch => branch.name == "master");
            return master;
        }

        public Task<string> CreateStream(Stream stream)
        {
            var streamInput = new StreamCreateInput()
            {
                name = stream.name,
                description = stream.description,
                isPublic = stream.isPublic
            };

            return _remote.StreamCreate(streamInput);
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
                    collaborators = collabs.GetRange(0,2),
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
                    collaborators = collabs.GetRange(1,2),
                    branches = branches
                },
            };

            return TestStreams;
        }

    }
}
