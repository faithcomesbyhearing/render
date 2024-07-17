using Newtonsoft.Json;
using Render.TempFromVessel.Kernel;

namespace Render.Models.Sections
{
    public class TimeMarkers : ValueObject, IEquatable<TimeMarkers>
    {
        public TimeMarkers(int startMarkerTime, int endMarkerTime)
        {
            StartMarkerTime = startMarkerTime;
            EndMarkerTime = endMarkerTime;
        }

        [JsonProperty("StartMarkerTime")]
        public int StartMarkerTime { get; }

        [JsonProperty("EndMarkerTime")]
        public int EndMarkerTime { get; }

        public int Length => EndMarkerTime - StartMarkerTime;
        
        public bool TimeWithinRange(int currentTime)
        {
            return currentTime >= StartMarkerTime && currentTime <= EndMarkerTime;
        }
        
        public (TimeMarkers firstPart, TimeMarkers remainingPart) DivideTimeMarker(int division)
        {
            var firstPart = new TimeMarkers(StartMarkerTime, division);
            var remainingPart = new TimeMarkers(division, EndMarkerTime);
            return (firstPart, remainingPart);
        }

        public bool Equals(TimeMarkers other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;

            return StartMarkerTime == other.StartMarkerTime && EndMarkerTime == other.EndMarkerTime;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;

            return Equals((TimeMarkers)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (StartMarkerTime * 397) ^ EndMarkerTime;
            }
        }
    }
}