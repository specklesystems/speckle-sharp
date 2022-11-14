using System.Collections.Generic;
using System.Linq;
using Speckle.Core.Models;
using Speckle.Core.Models.Extensions;

namespace ConverterDxf.Tests
{
    public static class ConverterSetup
    {
        public static string TestStream = "https://latest.speckle.dev/streams/24c3741255/branches/1%20standard/meters";
        
        public static IEnumerable<T> FetchAllObjectsOfType<T>(string streamUrl) where T: Base 
            => Speckle.Core.Api.Helpers.Receive(streamUrl)
                      .Result
                      .Flatten(b => b is T)
                      .Where(b => b is T)
                      .Cast<T>();
        
        public static IEnumerable<object[]> GetTestMemberData<T>() where T: Base
        {
            var d =FetchAllObjectsOfType<T>(TestStream).Select(v => new object[] { v });
            return d;
        }

    }
}