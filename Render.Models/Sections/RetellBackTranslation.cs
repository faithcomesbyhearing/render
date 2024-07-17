namespace Render.Models.Sections
{
    public class RetellBackTranslation : BackTranslation
    {
        public RetellBackTranslation(Guid parentId, Guid toLanguageId, Guid fromLanguageId,
            Guid projectId, Guid scopeId) : base( parentId, toLanguageId, fromLanguageId, projectId, scopeId)
        {
        }
    }
}