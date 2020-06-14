using System.Linq;
using MediaBrowser.Controller.Library;
using Tests.Common;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.Users
{
    public class UserTests : IClassFixture<JellyfinApplicationFactory>
    {
        private readonly IUserManager _userManager;

        public UserTests(JellyfinApplicationFactory factory)
        {
            _userManager = (IUserManager)factory.Services.GetService(typeof(IUserManager));
        }

        [Fact]
        public void CreateUser()
        {
            var user = _userManager.CreateUser("jellyfin");
            Assert.True(_userManager.Users.Count() == 1);
            Assert.True(_userManager.Users.First().Id == user.Id);
        }
    }
}
