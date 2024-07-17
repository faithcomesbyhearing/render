using Moq;
using Render.Models.Sections;
using Render.Services.AudioPlugins.AudioPlayer;
using Render.Services.AudioServices;
using Render.UnitTests.App.Kernel;

namespace Render.UnitTests.Domain.Services.AudioServices
{
    public class AudioPlayerServiceTests : TestBase
    {
        private readonly IAudioPlayerService _testAudioPlayerService;
        private readonly Mock<IAudioPlayer> _mockAudioPlayer;

        public AudioPlayerServiceTests()
        {
            _mockAudioPlayer = new Mock<IAudioPlayer>();
            _mockAudioPlayer.Setup(x => x.LoadAsync(It.IsAny<Stream>(), false)).Returns(Task.FromResult(true));

            _testAudioPlayerService = new AudioPlayerService(_mockAudioPlayer.Object);
        }

        [Theory]
        [InlineData(127.14)]
        [InlineData(127.16)]
        [InlineData(127.2)]
        [InlineData(127)]
        [InlineData(17)]
        public void PassageDuration_SectionWithOnePassage_EqualsSectionDuration(double sectionDuration)
        {
            //arrange
            _mockAudioPlayer.Setup(x => x.Duration).Returns(sectionDuration);

            //act
            _testAudioPlayerService.LoadAsync(It.IsAny<Stream>(), null);

            //assert
            var expected = sectionDuration;
            var actual = _testAudioPlayerService.Duration;
            Assert.Equal(expected, actual);
        }

        [Theory]
        [InlineData(0, 21350, 127.16)]
        [InlineData(21350, 86550, 127.16)]
        [InlineData(86550, 127164, 127.16)]
        [InlineData(0, 21350, 127.2)]
        [InlineData(21350, 86550, 127.2)]
        [InlineData(86550, 127144, 127.1)]
        [InlineData(86550, 127144, 127.14)]
        [InlineData(86550, 127139, 127.14)]
        [InlineData(86550, 127000, 127)]
        [InlineData(86550, 126999, 127)]
        [InlineData(86550, 127049, 127)]
        public void PassageDuration_SectionWithMorePassages_EqualsMarkersDifference(int startMarkerTime, int endMarkerTime, double sectionDuration)
        {
            //arrange
            _mockAudioPlayer.Setup(x => x.Duration).Returns(sectionDuration);
            var timeMarkers = new TimeMarkers(startMarkerTime, endMarkerTime);

            //act
            _testAudioPlayerService.LoadAsync(It.IsAny<Stream>(), timeMarkers);

            //assert
            var expected = GetMarkersDifference(timeMarkers);
            var actual = _testAudioPlayerService.Duration;
            Assert.Equal(expected, actual);
        }

        private double GetMarkersDifference(TimeMarkers timeMarkers)
        {
            return ((double)timeMarkers.EndMarkerTime) / 1000 - ((double)timeMarkers.StartMarkerTime) / 1000;
        }
    }
}
