using Render.Models.Sections;

namespace Render.Models.FakeModels
{
    public class FakeReference : Reference
    {
        protected FakeReference(
            string name,
            string language,
            string mediaId,
            bool primary,
            Guid projectId,
            Guid bibleVersionId) : 
            base(name, language, mediaId, primary, projectId, bibleVersionId) { }

        public static FakeReference EnglishNIV(Guid projectId, bool primary = false)
        {
            return new FakeReference("English NIV", "English", default, primary, projectId, Guid.Empty);
        }

        public static FakeReference EnglishCEV(Guid projectId, bool primary = false)
        {
            return new FakeReference("English CEV", "English", default, primary, projectId, Guid.Empty);
        }

        public static FakeReference EnglishNIRV(Guid projectId)
        {
            return new FakeReference("English NIRV", "English", default, false, projectId, Guid.Empty);
        }

        public static FakeReference EnglishNLT(Guid projectId)
        {
            return new FakeReference("English NLT", "English", default, false, projectId, Guid.Empty);
        }
    }
}