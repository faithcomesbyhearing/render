using Render.Models.Workflow;

namespace Render.Pages.Configurator.SectionAssignment.Factory;

internal record TeamAssignment(
    Team Team, 
    Models.Workflow.SectionAssignment Assignment);