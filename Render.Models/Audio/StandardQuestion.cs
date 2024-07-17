namespace Render.Models.Audio
{
    /// <summary>
    /// Library question
    /// </summary>
    public class StandardQuestion : NotableAudio
    {
        public StandardQuestion(Guid scopeId, Guid projectId, Guid parentId) 
            : base(scopeId, projectId, parentId)
        {
        }
    }
}