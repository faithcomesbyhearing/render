using Newtonsoft.Json;

namespace Render.Models.Sections
{
    public class BackTranslation : Draft
    {
        /// <summary>
        /// The language the back translation is recorded in
        /// </summary>
        [JsonProperty("ToLanguageId")]
        public Guid ToLanguageId { get; private set; }

        /// <summary>
        /// The language the back translation was translated from
        /// </summary>
        [JsonProperty("FromLanguageId")]
        public Guid FromLanguageId { get; private set; }

        //TODO Do we only need one language id where we can pull the from language from the parent draft


        /// <summary>
        /// Constructor for back translation object.
        /// </summary>
        /// <param name="parentId">This needs to be the passage id</param>
        /// <param name="toLanguageId"></param>
        /// <param name="fromLanguageId"></param>
        /// <param name="projectId"></param>
        /// <param name="scopeId"></param>
        public BackTranslation(
            Guid parentId,
            Guid toLanguageId,
            Guid fromLanguageId,
            Guid projectId,
            Guid scopeId,
            int documentVersion)
            : base(
                  scopeId,
                  projectId,
                  parentId,
                  documentVersion)
        {
            ToLanguageId = toLanguageId;
            FromLanguageId = fromLanguageId;
        }
    }
}