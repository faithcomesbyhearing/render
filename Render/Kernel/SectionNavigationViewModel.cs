using Render.Models.Audio;
using Render.Models.Sections;
using Render.Models.Workflow;
using Render.Resources;
using Render.Resources.Localization;
using Render.Services.SyncService;

namespace Render.Kernel
{
    public class SectionNavigationViewModel : ViewModelBase
    {
        private readonly IAudioIntegrityService _audioIntegrityService;
        
        public SectionNavigationViewModel(string urlPathSegment, IViewModelContextProvider viewModelContextProvider)
            : base(urlPathSegment, viewModelContextProvider)
        {
            _audioIntegrityService = viewModelContextProvider.GetAudioIntegrityService();
        }

        public bool IsAudioMissing(Section section, RenderStepTypes stepType, bool checkForCommunityAudio = false,
            bool checkForBackTranslationAudio = false)
        {
            var foundMissingAudio = false;
            
            var passagesWithMissingAudio = section.Passages
				.Where(x => x.CurrentDraftAudioId != Guid.Empty && IsAudioEmptyOrCorrupted(x.CurrentDraftAudio))
				.ToList();
            
            foreach (var passage in passagesWithMissingAudio)
            {
                foundMissingAudio = true;
                
                var isDocumentPresent = passage.CurrentDraftAudio != null;
                LogInfo("Missing audio detected", new Dictionary<string, string>
                {
                    { "Section Id", section.Id.ToString() },
                    { "Passage Id", passage.Id.ToString() },
					{ "Draft Id", passage.CurrentDraftAudioId.ToString() },
					{ "Draft Document In Local DB", isDocumentPresent.ToString() },
                    { "Step", stepType.ToString() }
                });
            }

            if (section.References != null)
            {
                foreach (var reference in section.References.Where(IsAudioEmptyOrCorrupted))
                {
                    foundMissingAudio = true;
            
                    LogInfo("Missing audio detected", new Dictionary<string, string>
                    {
                        { "Section Id", section.Id.ToString() },
                        { "Reference Id", reference.Id.ToString() },
                        { "Step", stepType.ToString() }
                    });
                }
            }

            if (foundMissingAudio
                || (checkForCommunityAudio && IsCommunityTestAudioMissing(section, stepType))
                || (checkForBackTranslationAudio && IsBackTranslationAudioMissing(section, stepType)))
            {
                ViewModelContextProvider.GetEssentials().InvokeOnMainThread(async () =>
                {
                    var modalManager = ViewModelContextProvider.GetModalService();
                    await modalManager.ShowInfoModal(Icon.OffloadItem,
                        string.Format(AppResources.CannotLoadData, section.Number.ToString()),
                        AppResources.SyncAndTryAgain);
                });

                return true;
            }

            return false;
        }

        private bool IsCommunityTestAudioMissing(Section section, RenderStepTypes stepType)
        {
            if (stepType == RenderStepTypes.CommunitySetup) return false;

            var foundMissingAudio = false;
            foreach (var passage in section.Passages.Where(x => x.CurrentDraftAudio.HasCommunityTest))
            {
                foreach (var flag in passage.CurrentDraftAudio.GetCommunityCheck().FlagsAllStages)
                {
                    foreach (var question in flag.Questions)
                    {
                        if (IsAudioEmptyOrCorrupted(question.QuestionAudio))
                        {
                            //Missing question audio
                            var isDocumentPresent = question.QuestionAudio != null;
                            LogInfo("Missing question audio detected", new Dictionary<string, string>
                            {
                                { "Section Id", section.Id.ToString() },
                                { "Passage Id", passage.Id.ToString() },
                                { "Question Audio Id", question.QuestionAudioId.ToString() },
                                { "Question Audio Document In Local DB", isDocumentPresent.ToString() },
                                { "Step", stepType.ToString() }
                            });
                            foundMissingAudio = true;
                        }

                        if (stepType == RenderStepTypes.CommunityRevise)
                        {
                            foreach (var response in question.Responses)
                            {
                                if (IsAudioEmptyOrCorrupted(response))
                                {
                                    //Missing response audio
                                    LogInfo("Missing response audio detected", new Dictionary<string, string>
                                    {
                                        { "Section Id", section.Id.ToString() },
                                        { "Passage Id", passage.Id.ToString() },
                                        { "Response Id", response.Id.ToString() },
                                        { "Response Document In Local DB", "true" },
                                        { "Step", stepType.ToString() }
                                    });
                                    foundMissingAudio = true;
                                }
                            }
                        }
                    }
                }

                if (stepType == RenderStepTypes.CommunityRevise)
                {
                    foreach (var retell in passage.CurrentDraftAudio.GetCommunityCheck().RetellsAllStages)
                    {
                        if (IsAudioEmptyOrCorrupted(retell))
                        {
                            //Missing retell audio
                            LogInfo("Missing community retell audio detected", new Dictionary<string, string>
                            {
                                { "Section Id", section.Id.ToString() },
                                { "Passage Id", passage.Id.ToString() },
                                { "Retell Id", retell.Id.ToString() },
                                { "Retell Document In Local DB", "true" },
                                { "Step", stepType.ToString() }
                            });
                            foundMissingAudio = true;
                        }
                    }
                }
            }

            return foundMissingAudio;
        }

        private bool IsBackTranslationAudioMissing(Section section, RenderStepTypes stepType)
        {
            var foundMissingAudio = false;
            foreach (var passage in section.Passages.Where(x => x.CurrentDraftAudio != null))
            {
                if (passage.CurrentDraftAudio.RetellBackTranslationAudio != null && IsAudioEmptyOrCorrupted(passage.CurrentDraftAudio.RetellBackTranslationAudio))
                {
                    LogInfo("Missing retell back translation audio detected", new Dictionary<string, string>
                    {
                        { "Section Id", section.Id.ToString() },
                        { "Passage Id", passage.Id.ToString() },
                        { "Retell Id", passage.CurrentDraftAudio.RetellBackTranslationAudio.Id.ToString() },
                        { "Step", stepType.ToString() }
                    });
                    foundMissingAudio = true;
                }

                if (passage.CurrentDraftAudio.RetellBackTranslationAudio?.RetellBackTranslationAudio != null && IsAudioEmptyOrCorrupted(passage.CurrentDraftAudio.RetellBackTranslationAudio?.RetellBackTranslationAudio))
                {
                    LogInfo("Missing retell back translation step 2 audio detected", new Dictionary<string, string>
                    {
                        { "Section Id", section.Id.ToString() },
                        { "Passage Id", passage.Id.ToString() },
                        { "Retell Id", passage.CurrentDraftAudio?.RetellBackTranslationAudio?.RetellBackTranslationAudio.Id.ToString() },
                        { "Step", stepType.ToString() }
                    });
                    foundMissingAudio = true;
                }

                foreach (var segmentBackTranslation in passage.CurrentDraftAudio.SegmentBackTranslationAudios)
                {
                    if (IsAudioEmptyOrCorrupted(segmentBackTranslation))
                    {
                        LogInfo("Missing segment back translation audio detected", new Dictionary<string, string>
                        {
                            { "Section Id", section.Id.ToString() },
                            { "Passage Id", passage.Id.ToString() },
                            { "Retell Id", segmentBackTranslation.Id.ToString() },
                            { "Step", stepType.ToString() }
                        });
                        foundMissingAudio = true;
                    }

                    if (segmentBackTranslation.RetellBackTranslationAudio != null && IsAudioEmptyOrCorrupted(segmentBackTranslation.RetellBackTranslationAudio))
                    {
                        LogInfo("Missing segment back translation step 2 audio detected", new Dictionary<string, string>
                        {
                            { "Section Id", section.Id.ToString() },
                            { "Passage Id", passage.Id.ToString() },
                            { "Retell Id", segmentBackTranslation.RetellBackTranslationAudio.Id.ToString() },
                            { "Step", stepType.ToString() }
                        });
                        foundMissingAudio = true;
                    }
                }
            }

            return foundMissingAudio;
        }
        
        private bool IsAudioEmptyOrCorrupted(Audio audio)
        {
            if (audio?.Data == null || audio.Data.Length == 0)
            {
                return true;
            }

            if (audio.DeclaredLength > 0 && audio.DeclaredLength != audio.Data.Length)
            {
                return true;
            }
            
            if (!string.IsNullOrEmpty(audio.DeclaredDigest) && audio.DeclaredDigest != _audioIntegrityService.ComputeDigest(audio.Data))
            {
                return true;
            }

            return false;
        }
    }
}