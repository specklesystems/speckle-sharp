using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Speckle.Core;
using Speckle.Credentials;
using Speckle.Core.Api.GqlModels;
using System.Collections.ObjectModel;

namespace Speckle.DesktopUI.Streams
{
    class StreamsRepository
    {
        public ObservableCollection<Stream> LoadTestStreams()
        {
            var TestStreams = new ObservableCollection<Stream>()
            {
                new Stream
                {
                    id = "stream123",
                    name = "Random Stream here 👋",
                    description = "this is a test stream",
                    isPublic = true
                },
                new Stream
                {
                    id = "stream456",
                    name = "Another Random Stream 🌵",
                    description = "we like to test things",
                    isPublic = true
                },
                new Stream
                {
                    id = "stream789",
                    name = "Woop Cool Stream 🌊",
                    description = "cool and good indeed",
                    isPublic = true
                },
            };

            return TestStreams;
        }
    }
}
