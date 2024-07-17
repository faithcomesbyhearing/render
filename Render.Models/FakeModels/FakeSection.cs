using System.Reflection;
using Render.Models.Sections;

namespace Render.Models.FakeModels
{
    public class FakeSection : Sections.Section
    {
        private const string ResourcePrefix = "Render.Models.FakeModels.";

        //Temptation of Jesus audio file names
        private const string TemptationCev = "Temptation_cev";
        private const string TemptationNIRV = "Temptation_NewInternationalReadersVersion";
        private const string TemptationNLT = "Temptation_nlt";
        private const string TemptationNIV = "Temptation_niv";
        private const string TemptationTitle = "TheTemptationOfJesus_Name";

        //Jesus Helps Many People audio file names
        private const string JesusHealsNIV = "JesusHealsManyPeople";
        private const string JesusHealsTitle = "JesusHelpsManyPeople_Name";

        private FakeSection(string name, Guid projectId, Guid templateId, int sectionNumber, string scriptureReference,
            List<Passage> passages = null, Audio.Audio audio = null) : base(new SectionTitle(name, audio.Id), 
            Guid.Empty, projectId, templateId, sectionNumber, scriptureReference, passages, audio)
        {
        }

        public static FakeSection TheTemptationOfJesus(Guid projectId, bool getDrafts)
        {
            var passage1 = new Passage(new PassageNumber(1));
            var passage2 = new Passage(new PassageNumber(2));
            passage1.ScriptureReferences.Add(new ScriptureReference("Luke", 4, 1, 6));
            passage2.ScriptureReferences.Add(new ScriptureReference("Luke", 4, 7, 13));

            if (getDrafts)
            {
                var passage1Audio = GetFakeAudio("temptation_passage1.opus", projectId, passage1.Id);
                passage1.ChangeCurrentDraftAudio(passage1Audio);

                var passage2Audio = GetFakeAudio("temptation_passage2.opus", projectId, passage2.Id);
                passage2.ChangeCurrentDraftAudio(passage2Audio);
            }
            else
            {
                var passage1Audio = new Draft(Guid.Empty, projectId, passage1.Id);
                passage1.ChangeCurrentDraftAudio(passage1Audio);

                var passage2Audio = new Draft(Guid.Empty, projectId, passage2.Id);
                passage2.ChangeCurrentDraftAudio(passage2Audio);
            }

            var sectionTitleAudio = GetFakeAudio("TheTemptationOfJesus_Name.opus", projectId, Guid.Empty);
            var section = new FakeSection(
                "The Temptation of Jesus",
                projectId,
                Guid.Empty,
                999, "Luke, 4:1-6, 4:7-13",
                new List<Passage> { passage1, passage2 },
                sectionTitleAudio
            );
            var nivPassage1Reference = new PassageReference(new PassageNumber(1), new TimeMarkers(0, 45500));
            var nivPassage2Reference = new PassageReference(new PassageNumber(2), new TimeMarkers(45500, 73460));
            var niv = GetFakeReference(projectId,
                TemptationNIV,
                section.Id,
                FakeReference.EnglishNIV(projectId),
                new List<PassageReference> { nivPassage1Reference, nivPassage2Reference });

            var cevPassage1Reference = new PassageReference(new PassageNumber(1), new TimeMarkers(0, 58500));
            var cevPassage2Reference = new PassageReference(new PassageNumber(2), new TimeMarkers(58500, 92640));
            var cev = GetFakeReference(projectId,
                TemptationCev,
                section.Id,
                FakeReference.EnglishCEV(projectId),
                new List<PassageReference> { cevPassage1Reference, cevPassage2Reference });

            var nirvPassage1Reference = new PassageReference(new PassageNumber(1), new TimeMarkers(0, 69500));
            var nirvPassage2Reference =
                new PassageReference(new PassageNumber(2), new TimeMarkers(69500, 114899));
            var nirv = GetFakeReference(projectId,
                TemptationNIRV,
                section.Id,
                FakeReference.EnglishNIRV(projectId),
                new List<PassageReference> { nirvPassage1Reference, nirvPassage2Reference });

            var nltPassage1Reference = new PassageReference(new PassageNumber(1), new TimeMarkers(0, 51300));
            var nltPassage2Reference = new PassageReference(new PassageNumber(2), new TimeMarkers(51300, 83399));
            var nlt = GetFakeReference(projectId,
                TemptationNLT,
                section.Id,
                FakeReference.EnglishNLT(projectId),
                new List<PassageReference> { nltPassage1Reference, nltPassage2Reference });
            section.References = new List<SectionReferenceAudio> { niv, cev, nirv, nlt };

            section.SetSupplementaryMaterial(GetFakeSupplementaryMaterialForSection(projectId, section));
            passage1.SetSupplementaryMaterial(GetFakeSupplementaryMaterialForPassage(projectId, passage1));
            passage2.SetSupplementaryMaterial(GetFakeSupplementaryMaterialForPassage(projectId, passage2));
            passage1.CurrentDraftAudio.RetellBackTranslationAudio = GetRetellBackTranslation(
                "temptation_passage2.opus", projectId,
                section.Id, Guid.Empty, Guid.Empty, section.ScopeId);
            passage2.CurrentDraftAudio.RetellBackTranslationAudio = GetRetellBackTranslation(
                "temptation_passage2.opus", projectId,
                section.Id, Guid.Empty, Guid.Empty, section.ScopeId);
            passage1.CurrentDraftAudio.SegmentBackTranslationAudios = GetSegmentBackTranslation(
                "temptation_passage2.opus", projectId,
                section.Id, Guid.Empty, Guid.Empty, section.ScopeId);
            passage2.CurrentDraftAudio.SegmentBackTranslationAudios = GetSegmentBackTranslation(
                "temptation_passage2.opus", projectId,
                section.Id, Guid.Empty, Guid.Empty, section.ScopeId);
            return section;
        }

        public static FakeSection JesusHealsManyPeople(Guid projectId)
        {
            var passage1 = new Passage(new PassageNumber(1));
            var passage2 = new Passage(new PassageNumber(2));
            var sectionTitleAudio = GetFakeAudio("JesusHelpsManyPeople_Name.opus", projectId, Guid.Empty);
            var section = new FakeSection(
                "Jesus Heals Many People",
                projectId,
                Guid.Empty,
                1, "Luke, 4:38:-41, 4:42-44",
                new List<Passage> { passage1, passage2 },
                sectionTitleAudio
            );
            var passageReference1 = new PassageReference(new PassageNumber(1), new TimeMarkers(0, 32000));
            var passageReference2 = new PassageReference(new PassageNumber(2), new TimeMarkers(32000, 53000));
            var niv = GetFakeReference(projectId, nameof(JesusHealsManyPeople), section.Id, FakeReference.EnglishNIV
                (projectId), new List<PassageReference> { passageReference1, passageReference2 });
            section.References = new List<SectionReferenceAudio> { niv };
            section.SetApprovedBy(Guid.NewGuid(), DateTimeOffset.Now);
            return section;
        }

        private static SectionReferenceAudio GetFakeReference(Guid projectId, string sectionName, Guid sectionId,
            FakeReference reference, List<PassageReference> passageReferences)
        {
            var sectionReferenceAudio = new SectionReferenceAudio(projectId, Guid.Empty, sectionId, Guid.Empty);
            sectionReferenceAudio.SetPassageReferences(passageReferences);
            var audio = GetFakeAudio($"{sectionName}.opus", projectId, sectionReferenceAudio.Id);
            sectionReferenceAudio.SetAudio(audio.Data);
            sectionReferenceAudio.Reference = reference;
            return sectionReferenceAudio;
        }

        private static Draft GetFakeAudio(string audioName, Guid projectId, Guid parentId)
        {
            var assembly = typeof(FakeSection).GetTypeInfo().Assembly;
            var audioDataStream = assembly.GetManifestResourceStream($"{ResourcePrefix}{audioName}");
            var numberOfBytes = (int)audioDataStream.Length;
            var audioData = new byte[numberOfBytes];
            audioDataStream.Read(audioData, 0, numberOfBytes);
            var audio = new Draft(Guid.Empty, projectId, parentId);
            audio.SetAudio(audioData);
            return audio;
        }

        public static new SupplementaryMaterial GetFakeSupplementaryMaterial(string name, Guid parentId, Audio.Audio audio,
            string text = "")
        {
            var supplementaryMaterial = new SupplementaryMaterial(name, audio.Id, text)
            {
                Audio = audio
            };
            return supplementaryMaterial;
        }

        public static new List<SupplementaryMaterial> GetFakeSupplementaryMaterialForPassage(Guid projectId,
            Passage passage)
        {
            if (passage.PassageNumber.Number == 1)
            {
                var supplementaryPassage1 = GetFakeSupplementaryMaterial("Passage 1", passage.Id,
                    GetFakeAudio("TemptationOfJesus_Passage1SupplementaryMaterial.opus", projectId, passage.Id));
                return new List<SupplementaryMaterial>() { supplementaryPassage1 };
            }

            var supplementaryPassage2 = GetFakeSupplementaryMaterial("Passage 2", passage.Id,
                GetFakeAudio("TemptationOfJesus_Passage2SupplementaryMaterial.opus", projectId, passage.Id));
            return new List<SupplementaryMaterial>() { supplementaryPassage2 };
        }

        public static new List<SupplementaryMaterial> GetFakeSupplementaryMaterialForSection(Guid projectId,
            Section section)
        {
            var supplementaryAudio =
                GetFakeAudio("TemptationOfJesus_SectionSupplementaryMaterial.opus", projectId, Guid.Empty);
            var supplementarySection = GetFakeSupplementaryMaterial("Section",
                section.Id, supplementaryAudio);

            return new List<SupplementaryMaterial>() { supplementarySection };
        }

        public static RetellBackTranslation GetRetellBackTranslation(string audioName, Guid projectId,
            Guid sectionId, Guid fromLanguageId, Guid toLanguageId, Guid scopeId)
        {
            var assembly = typeof(FakeSection).GetTypeInfo().Assembly;
            var audioDataStream = assembly.GetManifestResourceStream($"{ResourcePrefix}{audioName}");
            var numberOfBytes = (int)audioDataStream.Length;
            var audioData = new byte[numberOfBytes];
            audioDataStream.Read(audioData, 0, numberOfBytes);
            var audio = new RetellBackTranslation(sectionId, toLanguageId, fromLanguageId, projectId, scopeId);
            audio.SetAudio(audioData);
            return audio;
        }

        public static List<SegmentBackTranslation> GetSegmentBackTranslation(string audioName, Guid projectId,
            Guid sectionId, Guid fromLanguageId, Guid toLanguageId, Guid scopeId)
        {
            var assembly = typeof(FakeSection).GetTypeInfo().Assembly;
            var audioDataStream = assembly.GetManifestResourceStream($"{ResourcePrefix}{audioName}");
            var numberOfBytes = (int)audioDataStream.Length;
            var audioData = new byte[numberOfBytes];
            audioDataStream.Read(audioData, 0, numberOfBytes);
            var audio = new SegmentBackTranslation(new TimeMarkers(0, 100),sectionId, toLanguageId, fromLanguageId, projectId, scopeId);
            audio.SetAudio(audioData);
            var audio2 =
                new SegmentBackTranslation(new TimeMarkers(100, 200), sectionId, toLanguageId, fromLanguageId, projectId, scopeId);
            audio2.SetAudio(audioData);
            return new List<SegmentBackTranslation> { audio, audio2 };
        }

        public static Message GetMessage(Guid conversationId, Guid userId, Guid projectId, Guid scopeId)
        {
            var assembly = typeof(FakeSection).GetTypeInfo().Assembly;
            var audioName = "JesusHelpsManyPeople_Name.opus";
            var audioDataStream = assembly.GetManifestResourceStream($"{ResourcePrefix}{audioName}");
            var numberOfBytes = (int)audioDataStream.Length;
            var audioData = new byte[numberOfBytes];
            audioDataStream.Read(audioData, 0, numberOfBytes);
            var audio = new Audio.Audio(scopeId, projectId, conversationId);
            audio.SetAudio(audioData);
            var media = new Media(audio.Id) {Audio = audio};
            var message = new Message(userId, media);
            return message;
        }
    }
}