using System.Reflection;
using Newtonsoft.Json;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Render.Models.Scope;
using Render.TempFromVessel.Kernel;

namespace Render.Models.Sections
{
    public class Section : ScopeDomainEntity, IAggregateRoot
    {
        [JsonProperty("ScriptureReference")]
        public string ScriptureReference { get; private set; }

        [JsonIgnore] public bool HasReferenceAudio => References?.Count > 0;     
        
        [Reactive]
        [JsonIgnore] public IReadOnlyCollection<SectionReferenceAudio> References { get; set; }
        
        [Reactive]
        [JsonIgnore] public bool HasSegmentTranscriptions { get; set; }

        [Reactive]
        [JsonIgnore] public bool HasPassageTranscriptions { get; set; }
        
        [JsonProperty("SupplementaryMaterials")]
        public IReadOnlyCollection<SupplementaryMaterial> SupplementaryMaterials { get; private set; } =
            new List<SupplementaryMaterial>();

        [JsonProperty("Passages")] public List<Passage> Passages { get; private set; }

        [JsonProperty("Title")] public SectionTitle Title { get; set; }
        
        [JsonProperty("DefaultTitleText")]
        public string DefaultTitleText { get; private set; }

        [JsonProperty("Number")] public int Number { get; }
        
        [JsonProperty("TemplateId")] 
        public Guid TemplateId { get; private set; }
        
        [JsonProperty("Deleted")] 
        public bool Deleted { get; private set; }

        [JsonProperty("ApprovedBy")]
        public Guid ApprovedBy { get; private set; }
        
        [JsonProperty("ApprovedDate")]
        public DateTimeOffset ApprovedDate { get; private set; }
        
        [JsonProperty("CheckedBy")]
        public Guid CheckedBy { get; private set; }
        
        [JsonProperty("HasReferencesWithFailedVerification")]
        public bool HasReferencesWithFailedVerification { get; private set; }

        [JsonProperty("DeploymentVersionNumber")] 
        public int DeploymentVersionNumber { get; private set; }

        public Section(SectionTitle title, Guid scopeId, Guid projectId, Guid templateId, int number,
            string scriptureReference, List<Passage> passages = null, Audio.Audio sectionTitleAudio = null, 
            int deploymentVersionNumber = 1) : base(scopeId, projectId, 4)
        {
            Title = title;
            if (sectionTitleAudio != null)
            {
                Title.SetAudio(sectionTitleAudio);
            }

            TemplateId = templateId;
            ScriptureReference = scriptureReference;
            DeploymentVersionNumber = deploymentVersionNumber;
            Passages = passages ?? new List<Passage>();
            Number = number;
            var referencesAreSorted = false;
            this.WhenAnyValue(x => x.References)
                .Subscribe(r =>
                {
                    if (referencesAreSorted)
                    {
                        return;
                    }
                    //Make primary references first in the list
                    if (r != null && r.Count > 0)
                    {
                        var sortedList =  References.OrderByDescending(
                            x => x.Reference != null && x.Reference.Primary).ToList();
                        referencesAreSorted = true;
                        References = sortedList;
                    }
                });
            if (string.IsNullOrEmpty(ScriptureReference))
            {
                ScriptureReference = GetScriptureReferenceString();
            }
        }

        public List<SectionReferenceAudio> GetPrimaryReferenceAudios()
        {
            return References.Where(x => x.Reference.Primary).ToList();
        }

        public List<SectionReferenceAudio> GetSecondaryReferenceAudios()
        {
            return References.Where(x => !x.Reference.Primary).ToList();
        }

        public void SetSupplementaryMaterial(List<SupplementaryMaterial> supplementaryMaterials)
        {
            SupplementaryMaterials = supplementaryMaterials;
        }

        public void SetApprovedBy(Guid id, DateTimeOffset approvedDate)
        {
            ApprovedBy = id;
            ApprovedDate = approvedDate;
        }
        
        public void SetCheckedBy(Guid id)
        {
            CheckedBy = id;
        }
        
        public void SetTemplateId(Guid id)
        {
            TemplateId = id;
        }
        
        public void SetPassages(List<Passage> passages)
        {
            Passages = passages;
        }
        
        public void SetDeleted(bool deleted)
        {
            Deleted = deleted;
        }
        
        public string  GetScriptureReferenceString()
        {
			if (Passages.Count < 1)
			{
				return string.Empty;
			}

            var allScriptureReferences = Passages
                .SelectMany(a => a.ScriptureReferences)
                .Distinct()
                .ToArray();
			var firstBook = allScriptureReferences.FirstOrDefault()?.Book;
			var hasMultiBooks = !string.IsNullOrEmpty(firstBook)
					&& allScriptureReferences.Any(a => a.Book != firstBook);
			var allBooks = hasMultiBooks
					? allScriptureReferences.Select(a => a.ToString())
					: new[] { firstBook }.Concat(allScriptureReferences.Select(a => a.ChapterAndVerseRange));

			return string.Join(", ", allBooks.Where(a => !string.IsNullOrEmpty(a)));
		}

        public static Section UnitTestEmptySection(
            List<Passage> passageList = null, 
            List<PassageReference> passageReferences = null,
            Guid scopeId = default)
        {
            var passageNumber1 = new PassageNumber(1);
            var passageNumber2 = new PassageNumber(2);
            var passageNumber3 = new PassageNumber(3);
            if (passageList == null)
            {
                passageList = new List<Passage>
                {
                    new Passage(passageNumber1),
                    new Passage(passageNumber2),
                    new Passage(passageNumber3)
                };
                passageList.FirstOrDefault().SetSupplementaryMaterial(new List<SupplementaryMaterial>());
            }
            
            passageList[0].ScriptureReferences.Add(new ScriptureReference("Matthew", 1, 1, 7));
            passageList[0].ScriptureReferences.Add(new ScriptureReference("Matthew", 1, 8, 14));
            passageList[0].ScriptureReferences.Add(new ScriptureReference("Matthew", 1, 15, 20));
            
            foreach (var passage in passageList)
            {
                passage.CurrentDraftAudio = new Draft(Guid.Empty, Guid.Empty, Guid.Empty)
                {
                    Duration = 10000,
                };
                passage.CurrentDraftAudio.SetAudio(new byte[] {0,1,2});
                passage.CurrentDraftAudio.RetellBackTranslationAudio =
                    new RetellBackTranslation(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
                        Guid.NewGuid());
                passage.CurrentDraftAudio.RetellBackTranslationAudio.Duration = 10;
                passage.CurrentDraftAudio.RetellBackTranslationAudio.SetAudio(new byte[] {0, 1, 3, 5});
            }

            var reference1 = new SectionReferenceAudio(Guid.Empty, Guid.Empty, Guid.Empty, Guid.Empty);
            if (passageReferences == null)
            {
                passageReferences = new List<PassageReference>()
                {
                    new PassageReference(passageNumber1, new TimeMarkers(0, 1499)),
                    new PassageReference(passageNumber2, new TimeMarkers(1500, 2000))
                };
            }
            reference1.SetPassageReferences(passageReferences);
            reference1.Reference = new Reference("Ref1", "English", default, true, Guid.Empty, Guid.Empty);
            reference1.SetAudio(new byte[] { 0 });
            reference1.SetPassageReferences(new List<PassageReference>
            {
                new PassageReference(passageNumber1, new TimeMarkers(0, 100))
            });
            var sectionTitleAudio = new Audio.Audio(Guid.Empty, Guid.Empty, Guid.Empty);
            var section = new Section(new SectionTitle("Test Section", sectionTitleAudio.Id),
                scopeId, Guid.Empty, Guid.Empty, 1, "Matthew, 1:1-7, 1:8-9, 1:15-20",
                passageList, sectionTitleAudio);
            section.References = new List<SectionReferenceAudio>() { reference1 };
            return section;
        }

        /// <summary>
        /// Returns the next <see cref="Passage"/> after the passage given, or null if it is the last passage. 
        /// </summary>
        /// <param name="passage">The current passage.</param>
        /// <returns>The next passage, or null if the given passage is the last one.</returns>
        public Passage GetNextPassage(Passage passage)
        {
            var orderedPassages = Passages.OrderBy(x => x.PassageNumber).ToList();
            var index = orderedPassages.IndexOf(passage) + 1;
            return index > orderedPassages.Count - 1 ? null : orderedPassages[index];
        }

        public bool HasRetellBackTranslation()
        {
            return Passages.All(x =>
                x.HasAudio && x.CurrentDraftAudio.RetellBackTranslationAudio != null);
        }

        public bool HasSegmentBackTranslation()
        {
            var segBtAudios = Passages.SelectMany(x => x.CurrentDraftAudio.SegmentBackTranslationAudios).ToList();
            return Passages.All(x => x.HasAudio) && segBtAudios.Count > 0 && segBtAudios.All(x => x.HasAudio);
        }

        public bool HasAnyTranscriptions()
        {
            if (!HasRetellBackTranslation())
            {
                return false;
            }

            var hasSegmentBt = HasSegmentBackTranslation();
            foreach (var passage in Passages)
            {
                var transcription = passage.CurrentDraftAudio.RetellBackTranslationAudio.Transcription;
                if(!string.IsNullOrEmpty(transcription))
                {
                    HasPassageTranscriptions = true;
                    break;
                }
            }

            foreach (var passage in Passages)
            {
                if (!hasSegmentBt)
                {
                    return HasPassageTranscriptions;
                }

                if (passage.CurrentDraftAudio.SegmentBackTranslationAudios
                    .All(segment => string.IsNullOrEmpty(segment.Transcription)))
                {
                    continue;
                }
                HasSegmentTranscriptions = true;
            }

            return HasPassageTranscriptions || HasSegmentTranscriptions;
        }
        
        public List<Guid> GetAllNoteInterpretationIdsForSection()
        {
            var listOfIds = new List<Guid>();
            foreach (var passage in Passages.Where(passage => passage.HasAudio))
            {
                //Interpretations on Draft
                listOfIds.AddRange(from conversation in passage.CurrentDraftAudio.Conversations 
                    from message in conversation.Messages where message.InterpretationAudio != null 
                    select message.InterpretationAudio.Id);

                //Interpretations on Retell Back Translation
                if (passage.CurrentDraftAudio.RetellBackTranslationAudio != null)
                {
                    listOfIds.AddRange(from conversation in passage.CurrentDraftAudio.RetellBackTranslationAudio.Conversations 
                        from message in conversation.Messages where message.InterpretationAudio != null 
                        select message.InterpretationAudio.Id);
                }
                    
                //Interpretations on Segment Back Translation
                listOfIds.AddRange(from segment in passage.CurrentDraftAudio.SegmentBackTranslationAudios 
                    from conversation in segment.Conversations 
                    from message in conversation.Messages where message.InterpretationAudio != null 
                    select message.InterpretationAudio.Id);
            }
            
            return listOfIds;
        }
        
        public IEnumerable<SegmentBackTranslation> GetBackTranslationSegmentAudios()
        {
            var hasSegment = Passages.All(x => x.CurrentDraftAudio.SegmentBackTranslationAudios != null
                                               && x.CurrentDraftAudio.SegmentBackTranslationAudios.Count > 0);

            return hasSegment ? Passages.SelectMany(x => x.CurrentDraftAudio.SegmentBackTranslationAudios) : null;
        }
        
        public IEnumerable<RetellBackTranslation> GetBackTranslationRetellAudios()
        {
            var hasRetell = Passages.All(x => x.CurrentDraftAudio?.RetellBackTranslationAudio != null);
            return hasRetell ? Passages.Select(x => x.CurrentDraftAudio?.RetellBackTranslationAudio) : null;
        }

        public IEnumerable<RetellBackTranslation> GetSecondStepBackTranslationRetellAudios()
        {
            var hasRetell = CheckSecondStepRetellAudio();
            return hasRetell ? Passages.Select(x => x.CurrentDraftAudio?.RetellBackTranslationAudio?.RetellBackTranslationAudio) : null;
        }

        public IEnumerable<RetellBackTranslation> GetSecondStepBackTranslationSegmentAudios()
        {
            return CheckSecondStepSegmentAudio()
                ? Passages.SelectMany(x => x.CurrentDraftAudio
                    .SegmentBackTranslationAudios).Select(segmentAudio => segmentAudio.RetellBackTranslationAudio)
                : null;
        }

        public bool CheckSecondStepRetellAudio()
        {
            return Passages.All(x => x.CurrentDraftAudio?.RetellBackTranslationAudio?.RetellBackTranslationAudio != null);
        }

        public bool CheckRetellAudio()
        {
            return Passages.All(x => x.CurrentDraftAudio?.RetellBackTranslationAudio != null);
        }

        public bool CheckSegmentAudio()
        {
            return Passages.All(x => x.CurrentDraftAudio?.SegmentBackTranslationAudios != null
                                     && x.CurrentDraftAudio?.SegmentBackTranslationAudios.Count > 0);
        }

        public bool CheckSecondStepSegmentAudio()
        {
            var hasSegment = Passages.All(x => x.CurrentDraftAudio?.SegmentBackTranslationAudios != null
                                               && x.CurrentDraftAudio?.SegmentBackTranslationAudios.Count > 0);
            if (!hasSegment) return false;
            {
                foreach (var segment in from passage in Passages
                         from segment in passage.CurrentDraftAudio.SegmentBackTranslationAudios
                         where segment.RetellBackTranslationAudio == null
                         select segment)
                {
                    return false;
                }
            }
            return true;
        }
        
        #region FakeBuilding
        [JsonIgnore] private const string ResourcePrefix = "Render.Models.FakeModels.";
        [JsonIgnore] private const string TemptationCev = "Temptation_cev";
        [JsonIgnore] private const string TemptationNIRV = "Temptation_NewInternationalReadersVersion";
        [JsonIgnore] private const string TemptationNLT = "Temptation_nlt";
        [JsonIgnore] private const string TemptationNIV = "Temptation_niv";
        public static Section TheTemptationOfJesus(Guid projectId, List<Reference> references, int number = 999)
        {
            var passage1 = new Passage(new PassageNumber(1));
            passage1.ScriptureReferences.Add(new ScriptureReference("Luke", 4, 1, 6));
            var passage2 = new Passage(new PassageNumber(2));
            passage2.ScriptureReferences.Add(new ScriptureReference("Luke", 4, 7, 13));

            var sectionTitleAudio = GetFakeAudio("TheTemptationOfJesus_Name.opus", projectId, Guid.Empty);
            var section = new Section(
                new SectionTitle("The Temptation of Jesus", sectionTitleAudio.Id),
                Guid.Empty, 
                projectId,
                Guid.Empty, 
                number, "Luke, 4:1-6, 4:7-13",
                new List<Passage> { passage1, passage2 },
                sectionTitleAudio
            );
            var nivPassage1Reference = new PassageReference(new PassageNumber(1), new TimeMarkers(0, 45500));
            var nivPassage2Reference = new PassageReference(new PassageNumber(2), new TimeMarkers(45500, 73460));
            var niv = GetFakeReference(projectId,
                TemptationNIV,
                section.Id,
                references.SingleOrDefault(x => x.Name == "English NIV"),
                new List<PassageReference> { nivPassage1Reference, nivPassage2Reference });

            var cevPassage1Reference = new PassageReference(new PassageNumber(1), new TimeMarkers(0, 58500));
            var cevPassage2Reference = new PassageReference(new PassageNumber(2), new TimeMarkers(58500, 92640));
            var cev = GetFakeReference(projectId,
                TemptationCev,
                section.Id,
                references.SingleOrDefault(x => x.Name == "English CEV"),
                new List<PassageReference> { cevPassage1Reference, cevPassage2Reference });

            var nirvPassage1Reference = new PassageReference(new PassageNumber(1), new TimeMarkers(0, 69500));
            var nirvPassage2Reference =
                new PassageReference(new PassageNumber(2), new TimeMarkers(69500, 114899));
            var nirv = GetFakeReference(projectId,
                TemptationNIRV,
                section.Id,
                references.SingleOrDefault(x => x.Name == "English NIRV"),
                new List<PassageReference> { nirvPassage1Reference, nirvPassage2Reference });

            var nltPassage1Reference = new PassageReference(new PassageNumber(1), new TimeMarkers(0, 51300));
            var nltPassage2Reference = new PassageReference(new PassageNumber(2), new TimeMarkers(51300, 83399));
            var nlt = GetFakeReference(projectId,
                TemptationNLT,
                section.Id,
                references.SingleOrDefault(x => x.Name == "English NLT"),
                new List<PassageReference> { nltPassage1Reference, nltPassage2Reference });
            section.References = new List<SectionReferenceAudio> { niv, cev, nirv, nlt };
            
            section.SetSupplementaryMaterial(GetFakeSupplementaryMaterialForSection(projectId, section));
            passage1.SetSupplementaryMaterial(GetFakeSupplementaryMaterialForPassage(projectId, passage1));
            passage2.SetSupplementaryMaterial(GetFakeSupplementaryMaterialForPassage(projectId, passage2));
            
            return section;
        }

        private static SectionReferenceAudio GetFakeReference(Guid projectId, string sectionName, Guid sectionId,
            Reference reference, List<PassageReference> passageReferences)
        {
            var sectionReferenceAudio = new SectionReferenceAudio(projectId, reference.Id, sectionId, Guid.Empty);
            sectionReferenceAudio.SetPassageReferences(passageReferences);
            var audio = GetFakeAudio($"{sectionName}.opus", projectId, sectionReferenceAudio.Id);
            sectionReferenceAudio.SetAudio(audio.Data);
            sectionReferenceAudio.Reference = reference;
            return sectionReferenceAudio;
        }

        private static Audio.Audio GetFakeAudio(string audioName, Guid projectId, Guid parentId)
        {
            var assembly = typeof(Section).GetTypeInfo().Assembly;
            var audioDataStream = assembly.GetManifestResourceStream($"{ResourcePrefix}{audioName}");
            var numberOfBytes = (int)audioDataStream.Length;
            var audioData = new byte[numberOfBytes];
            audioDataStream.Read(audioData, 0, numberOfBytes);
            var audio = new Audio.Audio(Guid.Empty, projectId, parentId);
            audio.SetAudio(audioData);
            return audio;
        }
        
        public static SupplementaryMaterial GetFakeSupplementaryMaterial(string name, Guid parentId, Audio.Audio audio, string text = "")
        {
            var supplementaryMaterial = new SupplementaryMaterial(name, audio.Id, text)
            {
                Audio = audio
            };
            return supplementaryMaterial;
        }

        public static List<SupplementaryMaterial> GetFakeSupplementaryMaterialForPassage(Guid projectId, Passage passage)
        {
            if (passage.PassageNumber.Number == 1)
            {
                var supplementaryPassage1 = GetFakeSupplementaryMaterial("Passage 1", passage.Id, 
                    GetFakeAudio("TemptationOfJesus_Passage1SupplementaryMaterial.opus", projectId, passage.Id));
                return new List<SupplementaryMaterial>() { supplementaryPassage1 };
            }

            var supplementaryPassage2 = GetFakeSupplementaryMaterial("Passage 2",passage.Id,
                GetFakeAudio("TemptationOfJesus_Passage2SupplementaryMaterial.opus", projectId, passage.Id));
            return new List<SupplementaryMaterial>() { supplementaryPassage2 };
        }

        public static List<SupplementaryMaterial> GetFakeSupplementaryMaterialForSection(Guid projectId, Section section)
        {
            var supplementaryAudio =
                GetFakeAudio("TemptationOfJesus_SectionSupplementaryMaterial.opus", projectId, section.Id);
            var supplementarySection = GetFakeSupplementaryMaterial("Section", 
                section.Id, supplementaryAudio);
            
            return new List<SupplementaryMaterial>() { supplementarySection };
        }
        #endregion
        
        //Clone section except for section audio reference audio and reference, so changing the them will also change original section.
        public Section Clone()
        {
            var sectionJson = JsonConvert.SerializeObject(this);
            var cloneSection = JsonConvert.DeserializeObject<Section>(sectionJson);
            var sectRefJson = JsonConvert.SerializeObject(References);
            var sectionRefAudios = JsonConvert.DeserializeObject<List<SectionReferenceAudio>>(sectRefJson);
            var passagesJson = JsonConvert.SerializeObject(Passages);
            var passages = JsonConvert.DeserializeObject<List<Passage>>(passagesJson);
            
            if (cloneSection != null)
            {
                cloneSection.References = sectionRefAudios;
                cloneSection.Passages = passages;
                cloneSection.SetSupplementaryMaterial(SupplementaryMaterials.ToList());
            }
            
            var originalSectionRef = References;
           
            if (sectionRefAudios != null)
            {
                for (var i = 0; i < sectionRefAudios.Count; i++)
                {
                    sectionRefAudios[i].Reference = originalSectionRef.ElementAt(i).Reference;
                    sectionRefAudios[i].SetAudio(originalSectionRef.ElementAt(i).Data);
                    sectionRefAudios[i].LockedReferenceByPassageNumbersList =
                        originalSectionRef.ElementAt(i).LockedReferenceByPassageNumbersList;
                }
            }
            
            var originalPassages = Passages;
           
            if (passages != null)
            {
                for (var i = 0; i < passages.Count; i++)
                {
                    passages[i].ChangeCurrentDraftAudio(originalPassages.ElementAt(i).CurrentDraftAudio);
                }
            }

            if (cloneSection != null && Title?.Audio != null)
            {
                cloneSection.Title.SetAudio(Title?.Audio);
            }

            return cloneSection;
        }
    }
}