using TwitchLib.Api.Helix.Models.Users.GetUsers;

namespace Songify_Slim.Models.Twitch
{
    public class SimpleTwitchUser
    {
        public string Id { get; set; }
        public string DisplayName { get; set; }
        public string ProfileImageUrl { get; set; }
    }

    public static class UserExtensions
    {
        public static SimpleTwitchUser ToSimpleUser(this User user)
        {
            return new SimpleTwitchUser
            {
                Id = user.Id,
                DisplayName = user.DisplayName,
                ProfileImageUrl = user.ProfileImageUrl
            };
        }
    }
}