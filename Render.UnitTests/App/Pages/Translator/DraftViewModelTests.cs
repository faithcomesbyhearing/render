using FluentAssertions;
using Render.Models.Audio;
using Render.Pages.Translator.DraftingPage;
using Render.UnitTests.App.Kernel;

namespace Render.UnitTests.App.Pages.Translator
{
    public class DraftViewModelTests : ViewModelTestBase
    {
        private readonly DraftViewModel _vm;
        
        public DraftViewModelTests()
        {
            _vm = new DraftViewModel(1, MockContextProvider.Object);
            var audio = new Audio(Guid.Empty, Guid.Empty, Guid.Empty);
            _vm.SetAudio(audio);
        }
        
        [Fact]
        public void Select_TemporaryDelete_True()
        {
            // Arrange
            _vm.Audio.TemporaryDeleted = true;
            
            // Act
            _vm.Select();
            
            //Assert 
            _vm.DraftState.Should().Be(DraftState.Selected);
        }
        
        [Fact]
        public void Select_TemporaryDelete_False_HasAudio()
        {
            // Arrange
            _vm.Audio.TemporaryDeleted = false;
            _vm.Audio.SetAudio(new byte[] {0,1,2});
            
            // Act
            _vm.Select();
            
            //Assert 
            _vm.DraftState.Should().Be(DraftState.HasAudio);
        }
        
        [Fact]
        public void Select_TemporaryDelete_False_Selected()
        {
            // Arrange
            _vm.Audio.TemporaryDeleted = false;
            _vm.DraftState = DraftState.Selected;
            
            // Act
            _vm.Select();
            
            //Assert 
            _vm.DraftState.Should().Be(DraftState.Selected);
        }
    }
}