using System;

namespace Songify_Slim.Models.Responses
{
    public class Psa
    {
        public int Id { get; set; }
        public string MessageText { get; set; }
        public string Severity { get; set; }
        public long? CreatedAt { get; set; }  // Unix timestamp in seconds
        public long? StartDate { get; set; }  // Unix timestamp in seconds
        public long? EndDate { get; set; }    // Unix timestamp in seconds
        public bool IsActive { get; set; }
        public string Author { get; set; }

        private DateTime? ConvertUnixTimeToDateTime(long? unixTime)
        {
            if (unixTime.HasValue)
            {
                try
                {
                    // Ensure the value is within the valid range for DateTimeOffset
                    return DateTimeOffset.FromUnixTimeSeconds(unixTime.Value).LocalDateTime;
                }
                catch (ArgumentOutOfRangeException)
                {
                    // Log or handle invalid timestamp value
                    return null;
                }
            }

            return null;
        }

        // Computed properties to convert Unix timestamp to DateTime
        public DateTime? CreatedAtDateTime => ConvertUnixTimeToDateTime(CreatedAt);

        public DateTime? StartDateDateTime => ConvertUnixTimeToDateTime(StartDate);
        public DateTime? EndDateDateTime => ConvertUnixTimeToDateTime(EndDate);
    }
}