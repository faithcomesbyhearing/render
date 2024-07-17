using Render.Models.LocalOnlyData;
using Render.UnitTests.App.Kernel;

namespace Render.UnitTests.Domain.Services.SessionStateServiceTests
{
    public class UserProjectSessionTests : TestBase
    {
        private readonly UserProjectSession _session;

        public UserProjectSessionTests()
        {
            var userId = Guid.NewGuid();
            var projectId = Guid.NewGuid();
            var stepId = Guid.NewGuid();
            _session = new UserProjectSession(userId, projectId, stepId);
        }
        
        [Fact]
        public void StartStep_WhenClearRequirementsCompletedIsNotSpecified_RequirementsCompletedIsNotCleared()
        {
            //Arrange
            _session.AddRequirementCompletion(Guid.NewGuid());

            //Act
            _session.StartStep(Guid.NewGuid(), "Page");

            //Assert
            _session.RequirementsCompleted.Count.Should().Be(1);

        }
        
        [Fact]
        public void StartStep_WhenClearRequirementsCompletedIsTrue_RequirementsCompletedIsCleared()
        {
            //Arrange
            _session.AddRequirementCompletion(Guid.NewGuid());

            //Act
            _session.StartStep(Guid.NewGuid(), "Page", true);

            //Assert
            _session.RequirementsCompleted.Count.Should().Be(0);
        }
    }
}