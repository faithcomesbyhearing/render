using Render.Kernel;
using Render.Models.Scope;
using Render.Models.Workflow;
using Render.Resources.Localization;
using Render.TempFromVessel.Project;
using Render.Pages.Configurator.SectionAssignment.Cards.Section;
using Render.Pages.Configurator.SectionAssignment.Cards.Team;
using Render.Models.Sections;
using Render.Extensions;

namespace Render.Pages.Configurator.SectionAssignment.Factory;

public static class SectionAssignmentCardsFactory
{
    private static class Keys
    {
        public const string ProjectId = nameof(ProjectId);
        public const string Active = nameof(Active);
    }

    public static async Task<SectionAssignmentCards> CreateCards(
        IViewModelContextProvider provider,
        RenderWorkflow workflow,
        Guid projectId)
    {
        var sectionRepository = provider.GetSectionRepository();
        var allSections = await sectionRepository.GetSectionsForProjectAsync(projectId);
        if (allSections.Count is 0)
        {
            return SectionAssignmentCards.Empty;
        }

        var projectRepository = provider.GetPersistence<Project>();
        var project = await projectRepository.GetAsync(projectId);
        var scopePersistence = provider.GetPersistence<Scope>();
        var projectScopes = await scopePersistence.QueryOnFieldAsync(Keys.ProjectId, projectId.ToString(), 0);
        var userRepository = provider.GetUserRepository();

        var teamCards = new List<TeamCardViewModel>();
        var sectionCards = new List<SectionCardViewModel>();

        var allTeams = workflow.GetTeams();
        var teamsAssignments = allTeams
            .SelectMany(t => t.SectionAssignments, (t, a) => new TeamAssignment(t, a))
            .ToList();

        foreach (var section in allSections)
        {
            if (projectScopes.IsSectionIsInActiveScope(section) is false)
            {
                continue;
            }

            var teamAssignment = teamsAssignments.FirstOrDefault(team => team.Assignment.SectionId == section.Id);
            var sectionCard = CreateSectionCard(provider, section, teamAssignment?.Assignment?.Priority);

            if (teamAssignment is not null)
            {
                var teamCard = teamCards.FirstOrDefault(card => card.TeamId == teamAssignment.Team.Id);
                if (teamCard is null)
                {
                    teamCard = CreateTeamCard(provider, teamAssignment.Team);
                    teamCards.Add(teamCard);
                }

                teamCard.AssignSection(sectionCard);
                sectionCard.AssignTeam(teamCard);
            }

            sectionCards.Add(sectionCard);
        }
        
        teamCards.AddUnassignedTeamCards(allTeams, provider);
        return new (teamCards, sectionCards);
    }

    private static bool IsSectionIsInActiveScope(this List<Scope> scopes, Section section)
    {
        return scopes.Any(scope =>
            scope.Status == Keys.Active &&
            scope.Id == section.ScopeId);
    }

    private static SectionCardViewModel CreateSectionCard(
        IViewModelContextProvider contextProvider,
        Section section,
        int? priority)
    {
        return new SectionCardViewModel(contextProvider)
        {
            SectionId = section.Id,
            Title = section.Title.Text,
            Number = section.Number,
            ScriptureReference = section.ScriptureReference,
            Priority = priority ?? 999,
        };
    }

    private static TeamCardViewModel CreateTeamCard(
        IViewModelContextProvider provider, 
        Team team)
    {
        return new TeamCardViewModel(provider)
        {
            TeamId = team.Id,
            TeamNumber = team.TeamNumber,
            Name = string.Format(AppResources.TeamTitle, team.TeamNumber)
        };
    }

    private static void AddUnassignedTeamCards(
        this List<TeamCardViewModel> teamCards, 
        IReadOnlyList<Team> allTeams,
        IViewModelContextProvider provider)
    {
        allTeams
            .ExceptBy(teamCards.Select(card => card.TeamId), team => team.Id)
            .ForEach(team => teamCards.Add(CreateTeamCard(provider, team)));
    }
}