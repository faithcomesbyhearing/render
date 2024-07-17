using Newtonsoft.Json;
using Render.TempFromVessel.Kernel;

namespace Render.Models.Sections
{
    public class PassageNumber : ValueObject, IComparable, IEquatable<PassageNumber>
    {
        public PassageNumber(int number, int divisionNumber = 0)
        {
            Number = number;
            DivisionNumber = divisionNumber;
        }

        [JsonProperty("Number")] public int Number { get; }

        [JsonProperty("DivisionNumber")] public int DivisionNumber { get; }

        [JsonIgnore]
        public string PassageNumberString => DivisionNumber == 0 ? $"{Number}" : $"{Number}.{DivisionNumber}";
        
        public static bool BothAreNullOrEqual(PassageNumber p1, PassageNumber p2) => p1 == null && p2 == null || (p1 != null && p1.Equals(p2));
        
        public int CompareTo(object obj)
        {
            var other = obj as PassageNumber;

            if (other == null || Number < other.Number)
            {
                return -1;
            }

            if (Number > other.Number)
            {
                return 1;
            }

            if (Number == other.Number)
            {
                if (DivisionNumber < other.DivisionNumber)
                {
                    return -1;
                }
                if (DivisionNumber > other.DivisionNumber)
                {
                    return 1;
                }
                
                return 0;
            }

            return 0;
        }
        
        public bool Equals(PassageNumber other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;

            return Number == other.Number && DivisionNumber == other.DivisionNumber;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;

            return Equals((PassageNumber)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (Number * 397) ^ DivisionNumber;
            }
        }

        public override string ToString()
        {
            return PassageNumberString;
        }
    }
}