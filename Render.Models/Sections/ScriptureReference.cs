using Newtonsoft.Json;
using Render.TempFromVessel.Kernel;

namespace Render.Models.Sections
{
    public class ScriptureReference : ValueObject
    {
        [JsonProperty(nameof(Book))]
        public string Book { get; private set; }

        [JsonIgnore]
        public string ChapterRange { get => Chapter.ToString(); }

        [JsonIgnore]
        public string VerseRange { get => $"{StartVerse}{((EndVerse != StartVerse) ? "-"+EndVerse : "")}"; }

        [JsonProperty(nameof(Chapter))]
        public int Chapter { get; set; }

        [JsonProperty(nameof(StartVerse))]
        public int StartVerse { get; set; }
        
        [JsonProperty(nameof(EndVerse))]
        public int EndVerse { get; set; }

        [JsonIgnore]
        public string ChapterAndVerseRange { get => $"{Chapter}:{StartVerse}{((EndVerse != StartVerse) ? "-"+EndVerse : "")}"; }

        public ScriptureReference(string book, int chapter, int startVerse, int endVerse)
        {
            Book = book;
            Chapter = chapter;
            StartVerse = startVerse;
            EndVerse = endVerse;
        }

        /// <summary>
        /// Returns the reference as "{Book} {Chapter}:{VerseRange}"
        /// </summary>
        public override string ToString()
        {
            return $"{Book} {ChapterAndVerseRange}";
        }

		public bool Equals(ScriptureReference other)
		{
            return Book?.Equals(other.Book) is true
				&& Chapter == other.Chapter
                && StartVerse == other.StartVerse
                && EndVerse == other.EndVerse;
		}

		public override bool Equals(object obj)
		{
			if (obj is not ScriptureReference other)
			{
				return false;
			}

			return Equals(other);
		}

		public override int GetHashCode()
		{
			return (Book?.GetHashCode() ?? 0)
				^ Chapter.GetHashCode()
				^ StartVerse.GetHashCode()
				^ EndVerse.GetHashCode();
		}
	}
}