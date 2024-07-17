using Render.Models.Audio;
using Render.UnitTests.App.Kernel;

namespace Render.UnitTests.Domain.Audios
{
    public class AudioTests : TestBase
    {
        [Fact]
        public void WriteWavHeader_Succeeds()
        {
            //Arrange
            var data = new byte[]{1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1};
            var audio = new Audio(Guid.Empty, Guid.Empty, Guid.Empty);
            audio.SetAudio(data);
            
            //Act 1
            audio.WriteWavHeader();
            
            //Assert 1
            audio.Data.Length.Should().BeGreaterThan(data.Length);
            
            //Act 2
            var noHeader = audio.RemoveWavHeader();
            
            //Assert 2
            noHeader.Length.Should().Be(data.Length);
            noHeader.Should().BeEquivalentTo(data);
        }

        [Fact]
        public void AddRawAudioData_Succeeds()
        {
            //Arrange
            var data = new byte[]{1,2,3,4,5};
            var audio = new Audio(Guid.Empty, Guid.Empty, Guid.Empty);
            audio.SetAudio(data);
            
            //Act
            var data2 = new byte[]{1,2,3,4,5};
            audio.AddRawAudioData(data2);
            
            //Assert
            audio.Data.Length.Should().Be(data.Length + data2.Length);
            audio.Data.Should().BeEquivalentTo(new byte[]{1,2,3,4,5,1,2,3,4,5});
        }

        [Fact]
        public void RemoveWavHeader_AudioWithWavHeader_WavHeaderRemoved()
        {
            //Arrange
            var data = new byte[]{1,2,3,4,5};
            var audio = new Audio(default, default, default);
            audio.SetAudio(data);
            audio.WriteWavHeader();
            
            //Act
            var dataWithoutHeader = audio.RemoveWavHeader();
            
            //Assert
            dataWithoutHeader.Length.Should().Be(data.Length);
            dataWithoutHeader.Should().BeEquivalentTo(new byte[]{1,2,3,4,5});
        }
    }
}