using System;
using NUnit.Framework;

namespace Tests
{
  [TestFixture]
  public class Helpers
  {
    [Test]
    [TestCase(30,"just now")]
    [TestCase(60,"1 minute ago")]
    [TestCase(60 * 2,"2 minutes ago")]
    [TestCase(60 * 60 * 1,"1 hour ago")]
    [TestCase(60 * 60 * 2,"2 hours ago")]
    [TestCase(60 * 60 * 24 * 1,"1 day ago")]
    [TestCase(60 * 60 * 24 * 2,"2 days ago")] 
    [TestCase(60 * 60 * 24 * 7 * 1,"1 week ago")] 
    [TestCase(60 * 60 * 24 * 7 * 2,"2 weeks ago")] 
    [TestCase(60 * 60 * 24 * 31 * 1,"1 month ago")] 
    [TestCase(60 * 60 * 24 * 31 * 2,"2 months ago")] 
    [TestCase(60 * 60 * 24 * 365 * 1,"1 year ago")] 
    [TestCase(60 * 60 * 24 * 365 * 2,"2 years ago")] 
    public void TimeAgo_DisplaysTextCorrectly(int secondsAgo, string expectedText)
    {
      // Get current time and substract the input amount
      var dateTime = DateTime.Now;
      dateTime = dateTime.Subtract(new TimeSpan(0, 0, secondsAgo));
      
      // Get the timeAgo text representation
      var actual = Speckle.Core.Api.Helpers.TimeAgo(dateTime);
      
      Assert.AreEqual(expectedText, actual);
    }
  }
}
