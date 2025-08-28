using System.Collections.Generic;
using System.Threading.Tasks;
using TwitchLib.Api.Helix;
using TwitchLib.Api.Helix.Models.Channels.GetChannelVIPs;
using TwitchLib.Api.Helix.Models.Chat.GetChatters;
using TwitchLib.Api.Helix.Models.EventSub;
using TwitchLib.Api.Helix.Models.Moderation.GetModerators;
using TwitchLib.Api.Helix.Models.Subscriptions;
using TwitchLib.Api.Helix.Models.Users.GetUsers;

namespace Songify_Slim.Util.Songify.Twitch
{
    internal class TwitchApiHelper
    {
        public static async Task<List<Subscription>> GetAllSubscribersAsync()
        {
            List<Subscription> allSubscribers = [];
            string pagination = null;

            do
            {
                // Get Subscriber status for the user and determine if they are t1 t2 or t3
                GetBroadcasterSubscriptionsResponse subscriptionsResponse =
                    await TwitchHandler.TwitchApi.Helix.Subscriptions.GetBroadcasterSubscriptionsAsync(
                        Settings.Settings.TwitchUser.Id,
                        100,
                        pagination,
                        Settings.Settings.TwitchAccessToken);
                if (subscriptionsResponse?.Data != null)
                {
                    allSubscribers.AddRange(subscriptionsResponse.Data);
                }

                pagination = subscriptionsResponse?.Pagination?.Cursor;
            } while (!string.IsNullOrEmpty(pagination));

            return allSubscribers;
        }

        public static async Task<List<Chatter>> GetAllChattersAsync()
        {
            List<Chatter> allChatters = [];
            string pagination = null;

            do
            {
                // Fetch a page of chatters
                GetChattersResponse chattersResponse = await TwitchHandler.TwitchApi.Helix.Chat.GetChattersAsync(
                    Settings.Settings.TwitchUser.Id,
                    Settings.Settings.TwitchUser.Id,
                    100,
                    pagination,
                    Settings.Settings.TwitchAccessToken);

                // Add chatters from the current page to the list
                if (chattersResponse?.Data != null)
                {
                    allChatters.AddRange(chattersResponse.Data);
                }

                // Update the pagination token for the next page
                pagination = chattersResponse?.Pagination?.Cursor;
            } while (!string.IsNullOrEmpty(pagination));

            return allChatters;
        }

        public static async Task<List<Moderator>> GetAllModeratorsAsync()
        {
            List<Moderator> allModerators = [];
            string pagination = null;

            do
            {
                // Fetch a page of chatters
                GetModeratorsResponse moderatorsResponse = await TwitchHandler.TwitchApi.Helix.Moderation.GetModeratorsAsync(
                    Settings.Settings.TwitchUser.Id,
                    null,
                    100,
                    pagination,
                    Settings.Settings.TwitchAccessToken);

                // Add chatters from the current page to the list
                if (moderatorsResponse?.Data != null)
                {
                    allModerators.AddRange(moderatorsResponse.Data);
                }

                // Update the pagination token for the next page
                pagination = moderatorsResponse?.Pagination?.Cursor;
            } while (!string.IsNullOrEmpty(pagination));

            return allModerators;
        }

        public static async Task<List<ChannelVIPsResponseModel>> GetAllVipsAsync()
        {
            List<ChannelVIPsResponseModel> allVips = [];
            string pagination = null;

            do
            {
                // Fetch a page of chatters
                GetChannelVIPsResponse vipsResponse = await TwitchHandler.TwitchApi.Helix.Channels.GetVIPsAsync(
                    Settings.Settings.TwitchUser.Id,
                    null,
                    100,
                    pagination,
                    Settings.Settings.TwitchAccessToken);

                // Add chatters from the current page to the list
                if (vipsResponse?.Data != null)
                {
                    allVips.AddRange(vipsResponse.Data);
                }

                // Update the pagination token for the next page
                pagination = vipsResponse?.Pagination?.Cursor;
            } while (!string.IsNullOrEmpty(pagination));

            return allVips;
        }

        public static async Task<User[]> GetTwitchUsersAsync(List<string> users)
        {
            GetUsersResponse x = await TwitchHandler.TwitchApi.Helix.Users.GetUsersAsync(null, users, Settings.Settings.TwitchAccessToken);
            return x.Users.Length > 0 ? x.Users : [];
        }

        public static async Task<EventSubSubscription[]> GetEventSubscriptions()
        {
            GetEventSubSubscriptionsResponse x = await TwitchHandler.TwitchApi.Helix.EventSub.GetEventSubSubscriptionsAsync(null, null, null, null, null,
                Settings.Settings.TwitchAccessToken);
            return x.Subscriptions;
        }
    }
}