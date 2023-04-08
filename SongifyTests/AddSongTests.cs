using Songify_Slim;

namespace SongifyTests
{
    [TestClass]
    public class AddSongTests
    {
        [TestMethod]
        public void AddSong_BlacklistedTrack_DoesNotAddToQueue()
        {
            // Arrange
            string trackId = "blacklisted_track";
            OnMessageReceivedArgs args = new OnMessageReceivedArgs
            {
                ChatMessage = new ChatMessage
                {
                    Channel = "#test",
                    DisplayName = "testuser"
                }
            };
            Blacklist.AddTrack(trackId);

            // Act
            AddSong(trackId, args);

            // Assert
            Assert.IsFalse(IsTrackInQueue(trackId));
            Assert.IsTrue(ChatMessagesReceived.Contains("This song is blocked"));
        }

        [TestMethod]
        public void AddSong_NullTrackId_ReturnsErrorMessage()
        {
            // Arrange
            string trackId = null;
            OnMessageReceivedArgs args = new OnMessageReceivedArgs
            {
                ChatMessage = new ChatMessage
                {
                    Channel = "#test",
                    DisplayName = "testuser"
                }
            };

            // Act
            AddSong(trackId, args);

            // Assert
            Assert.IsFalse(IsTrackInQueue(trackId));
            Assert.IsTrue(ChatMessagesReceived.Contains("No song found."));
        }
    }
}