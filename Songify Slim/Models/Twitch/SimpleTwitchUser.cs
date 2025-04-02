using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TwitchLib.Api.Helix.Models.Users.GetUsers;

namespace Songify_Slim.Models
{
    public class SimpleTwitchUser
    {
        public string Id { get; set; }
        public string Login { get; set; }
        public string DisplayName { get; set; }
        public DateTime CreatedAt { get; set; }
        public string Type { get; set; }
        public string BroadcasterType { get; set; }
        public string Description { get; set; }
        public string ProfileImageUrl { get; set; }
        public string OfflineImageUrl { get; set; }
        public long ViewCount { get; set; }
        public string Email { get; set; }
    }

    public static class UserExtensions
    {
        public static SimpleTwitchUser ToSimpleUser(this User user)
        {
            return new SimpleTwitchUser
            {
                Id = user.Id,
                Login = user.Login,
                DisplayName = user.DisplayName,
                CreatedAt = user.CreatedAt,
                Type = user.Type,
                BroadcasterType = user.BroadcasterType,
                Description = user.Description,
                ProfileImageUrl = user.ProfileImageUrl,
                OfflineImageUrl = user.OfflineImageUrl,
                ViewCount = user.ViewCount,
                Email = user.Email
            };
        }
    }

}
