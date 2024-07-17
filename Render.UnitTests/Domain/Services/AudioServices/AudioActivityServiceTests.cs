using Moq;
using Render.Interfaces.AudioServices;
using Render.Services.AudioServices;
using Render.UnitTests.App.Kernel;

namespace Render.UnitTests.Domain.Services.AudioServices
{
    public class AudioActivityServiceTests : ViewModelTestBase
    {
        private IAudioActivityService _service;

        public AudioActivityServiceTests()
        {
            _service = new AudioActivityService(MockContextProvider.Object.GetLogger(GetType()));
        }

        [Fact]
        public void SetStopCommand_PassNullCommand_ThrowsArgumentException()
        {
            //Arrange
            //Act
            //Assert
            Assert.Throws<ArgumentException>(() => _service.SetStopCommand((Func<Task>)default, false));
        }

        [Fact]
        public void SetStopCommand_PassTwoCommands_PreviousCommandIsInvoked()
        {
            //Arrange
            var previousCommand = new Mock<Action>();
            var command = new Mock<Action>();
            _service.SetStopCommand(previousCommand.Object, false);

            //Act
            _service.SetStopCommand(command.Object, false);

            //Assert
            previousCommand.Verify(x => x.Invoke(), Times.Once);
            command.Verify(x => x.Invoke(), Times.Never);
        }

        [Fact]
        public void SetStopCommand_PassTwoCommandsTheSameInstance_PreviousCommandIsNotInvoked()
        {
            //Arrange
            var previousCommand = new Mock<Action>();
            var command = previousCommand;
            _service.SetStopCommand(previousCommand.Object, false);

            //Act
            _service.SetStopCommand(command.Object, false);

            //Assert
            previousCommand.Verify(x => x.Invoke(), Times.Never);
        }

        [Fact]
        public void Stop_PassCommand_CommandIsInvoked()
        {
            //Arrange
            var command = new Mock<Action>();
            _service.SetStopCommand(command.Object, false);

            //Act
            _service.Stop();

            //Assert
            command.Verify(x => x.Invoke(), Times.Once);
        }

        [Fact]
        public void Stop_PassAsyncCommand_CommandIsInvoked()
        {
            //Arrange
            var command = new Mock<Func<Task>>();
            _service.SetStopCommand(command.Object, true);

            //Act
            _service.Stop();

            //Assert
            command.Verify(x => x.Invoke(), Times.Once);
        }

        [Fact]
        public void StopRecording_IsAudioRecordingTrue_CommandIsInvoked()
        {
            //Arrange
            var command = new Mock<Action>();
            _service.SetStopCommand(command.Object, true);

            //Act
            _service.StopRecording();

            //Assert
            command.Verify(x => x.Invoke(), Times.Once);
        }

        [Fact]
        public void StopRecording_IsAudioRecordingFalse_CommandIsNotInvoked()
        {
            //Arrange
            var command = new Mock<Action>();
            _service.SetStopCommand(command.Object, false);

            //Act
            _service.StopRecording();

            //Assert
            command.Verify(x => x.Invoke(), Times.Never);
        }
    }
}