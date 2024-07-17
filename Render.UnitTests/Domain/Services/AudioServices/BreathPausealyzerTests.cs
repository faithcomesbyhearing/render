using Moq;
using Render.Services.AudioPlugins.AudioPlayer;
using Render.Services.AudioServices;
using Render.UnitTests.App.Kernel;
using Xunit.Abstractions;

namespace Render.UnitTests.Domain.Services.AudioServices
{
    public class BreathPauseAnalyzerTests : ViewModelTestBase
    {
        private readonly ITestOutputHelper _output;
        private readonly Mock<IAudioPlayer> _mockSimpleAudioPlayer;
        private readonly AudioEncodingService _audioEncodingService;

        public BreathPauseAnalyzerTests(ITestOutputHelper output)
        {
            _output = output;
            _mockSimpleAudioPlayer = new Mock<IAudioPlayer>();
            _mockSimpleAudioPlayer.Setup(x => x.Load(It.IsAny<Stream>(), false)).Returns(true);

            _audioEncodingService = new AudioEncodingService(GetLogger());
        }

        [Theory]
        [InlineData("BPTest.opus", 4, 3)]
        public void BreathPauseLogic_DividesAudioCorrectly(string audioFileName, int audioLengthInSeconds, int expectedBreathPauseCount)
        {
            //Arrange
            _mockSimpleAudioPlayer.Setup(x => x.Duration).Returns(audioLengthInSeconds);
            var opus = File.ReadAllBytes(GetPathToTestAudioFile(audioFileName));

            var audio = new Render.Models.Audio.Audio(default, default, default);
            using (var msWav = new MemoryStream())
            {
                _audioEncodingService.ConvertOpusToWav(opus, 48000, 1, msWav);
                audio.SetAudio(msWav.ToArray());
                audio.WriteWavHeader();
            }

            var bp = new UnitTestBreathPauseAnalyzer(_mockSimpleAudioPlayer.Object);

            //Act
            bp.LoadAudioAndFindBreathPauses(audio);

            //Assert
            WriteTestOutput(bp.Division);
            bp.Division.Count.Should().Be(expectedBreathPauseCount);
        }

        [Theory]
        [InlineData("BPTest.opus", 4, 3)]
        public void StreamBreathPauseLogic_DividesAudioCorrectly(string audioFileName, int audioLengthInSeconds, int expectedBreathPauseCount)
        {
            //Arrange
            _mockSimpleAudioPlayer.Setup(x => x.Duration).Returns(audioLengthInSeconds);
			var opus = File.ReadAllBytes(GetPathToTestAudioFile(audioFileName));

            var audio = new Render.Models.Audio.Audio(default, default, default);
            using (var msWav = new MemoryStream())
            {
                _audioEncodingService.ConvertOpusToWav(opus, 48000, 1, msWav);
                audio.SetAudio(msWav.ToArray());
                //audio.WriteWavHeader();
            }

            var bp = new StreamBreathPauseAnalyzer(_mockSimpleAudioPlayer.Object);

            var mockTempAudioService = new Mock<ITempAudioService>();
            mockTempAudioService.Setup(x => x.OpenAudioStream()).Returns(new MemoryStream(audio.Data));

            //Act
            bp.LoadAudioAndFindBreathPauses(mockTempAudioService.Object);

            //Assert
            WriteTestOutput(bp.Division);
            bp.Division.Count.Should().Be(expectedBreathPauseCount);
        }

		[Theory]
		[InlineData("BPTest.opus", 4, 2)]
		public void StreamBreathPauseLogic_RemoveLastExtraDivision(string audioFileName, int audioLengthInSeconds, int expectedBreathPauseCount)
		{
			//Arrange
			_mockSimpleAudioPlayer.Setup(x => x.Duration).Returns(audioLengthInSeconds);
			var opus = File.ReadAllBytes(GetPathToTestAudioFile(audioFileName));

			var audio = new Render.Models.Audio.Audio(default, default, default);
			using (var msWav = new MemoryStream())
			{
				_audioEncodingService.ConvertOpusToWav(opus, 48000, 1, msWav);
				audio.SetAudio(msWav.ToArray());
			}

			var bp = new StreamBreathPauseAnalyzer(_mockSimpleAudioPlayer.Object);

			var mockTempAudioService = new Mock<ITempAudioService>();
			mockTempAudioService.Setup(x => x.OpenAudioStream()).Returns(new MemoryStream(audio.Data));
			bp.LoadAudioAndFindBreathPauses(mockTempAudioService.Object);

            //Act
            bp.RemoveLastExtraDivision();

			//Assert
			WriteTestOutput(bp.Division);
			bp.Division.Count.Should().Be(expectedBreathPauseCount);
		}

		private void WriteTestOutput(IEnumerable<int> bpDivision)
        {
            foreach (var division in bpDivision)
            {
                _output.WriteLine(division.ToString());
            }
        }

        private string GetPathToTestAudioFile(string audioFileName)
        {
            return $"./TestData/{audioFileName}";
        }
    }

    internal class UnitTestBreathPauseAnalyzer : BreathPauseAnalyzer
    {
        internal UnitTestBreathPauseAnalyzer(IAudioPlayer simpleAudioPlayer) : base(simpleAudioPlayer)
        {
        }
    }
}