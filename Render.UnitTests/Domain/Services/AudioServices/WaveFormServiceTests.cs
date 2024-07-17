using Render.Models.Audio;
using Render.Services.AudioServices;
using Render.Services.WaveformService;
using Render.UnitTests.App.Kernel;

namespace Render.UnitTests.Domain.Services.AudioServices
{
    public class WaveFormServiceTests : ViewModelTestBase
    {
        private const int DefaultWavHeaderSize = AudioEncodingService.DefaultWavHeaderSize;
        private const int WavBodySize = 5000000;

        private readonly IWaveFormService _service;

        public WaveFormServiceTests()
        {
            _service = new WaveFormService(GetLogger());
        }

        #region GetMiniWaveformBars

        [Theory]
        [InlineData(WavBodySize + DefaultWavHeaderSize)]
        [InlineData(WavBodySize + DefaultWavHeaderSize + 1)]
        [InlineData(WavBodySize + DefaultWavHeaderSize - 1)]
        public void GetMiniWaveformBars_WhenStreamHasData_Returns100Bars(int audioDataSize)
        {
            //Arrange
            var stream = new MemoryStream(new byte[audioDataSize]);
            Audio.WriteWavHeaderOnStream(stream, 1);
            stream.Position = 0;

            //Act
            var bars = _service.GetMiniWaveformBars(stream);

            //Assert
            bars.Should().NotBeNull();
            bars.Length.Should().Be(100);
        }

        [Fact]
        public void GetMiniWaveformBars_WhenEmptyStream_ReturnsNotNullResult()
        {
            //Arrange
            var stream = new MemoryStream();

            //Act
            var bars = _service.GetMiniWaveformBars(stream);

            //Assert
            bars.Should().NotBeNull();
        }

        [Fact]
        public void GetMiniWaveformBars_WhenNullStream_ThrowsException()
        {
            //Arrange
            //Act
            //Assert
            Assert.ThrowsAny<Exception>(() => { _service.GetMiniWaveformBars(null); });
        }

        [Fact]
        public void GetMiniWaveformBars_WhenStreamWithoutWavHeader_ThrowsException()
        {
            //Arrange
            var stream = new MemoryStream(new byte[1]);

            //Act
            //Assert
            Assert.ThrowsAny<Exception>(() => { _service.GetMiniWaveformBars(stream); });
        }

        #endregion
    }
}