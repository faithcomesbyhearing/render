using ReactiveUI;
using Render.Kernel.WrappersAndExtensions;
using Render.Resources;

namespace Render.Components.CommunityTestFlagLibraryModal;

public partial class QuestionCard 
{
    public QuestionCard()
    {
         InitializeComponent();
            this.WhenActivated(d =>
            {
                d(this.OneWayBind(ViewModel, vm => vm.AddToLibraryButtonGlyph, v => v.AddToLibraryButton.Text));
                d(this.OneWayBind(ViewModel, vm => vm.AddToQuestionListButtonGlyph, v => v.AddToQuestionListButton.Text));
                
                d(this.OneWayBind(ViewModel, vm => vm.BarPlayerViewModel, 
                    v => v.BarPlayer.BindingContext));

                d(this.WhenAnyValue(v => v.ViewModel.IsDeleted, v=> v.ViewModel.IsLibraryQuestion)
                    .Subscribe(((bool IsDeleted, bool IsLibraryQuestion) options) =>
                    {
                        if (options.IsLibraryQuestion)
                        {
                            DeleteFromQuestionListButton.IsVisible = false;
                            UndoDeleteFromQuestionListButton.IsVisible = false;
                            
                            DeleteFromLibraryButton.IsVisible = !options.IsDeleted;
                            UndoDeleteFromLibraryButton.IsVisible = options.IsDeleted;
                        }
                        else // Question
                        {
                            DeleteFromLibraryButton.IsVisible = false;
                            UndoDeleteFromLibraryButton.IsVisible = false;

                            DeleteFromQuestionListButton.IsVisible = !options.IsDeleted;
                            UndoDeleteFromQuestionListButton.IsVisible = options.IsDeleted;
                        }

                        BarPlayerDisabledOverlay.IsVisible = options.IsDeleted;
                    }));
                
                d(this.WhenAnyValue(v => v.ViewModel.IsLibraryQuestion)
                    .Subscribe(isLibraryQuestion =>
                    {
                        AddToLibraryButton.IsVisible = !isLibraryQuestion;
                        AddToQuestionListButton.IsVisible = isLibraryQuestion;
                    }));
                
                d(this.WhenAnyValue(v => v.ViewModel.IsLibraryQuestion, v => v.ViewModel.Include)
                    .Subscribe(((bool Include, bool IsLibraryQuestion) options) =>
                    {
                        BarPlayer.MainStackBackgroundColor = ResourceExtensions.GetColor(
                            options.IsLibraryQuestion || options.Include
                            ? "CommunityTestQuestionLibraryPlayerBackgroundColor"
                            : "CommunityTestQuestionPlayerBackgroundColor");
                    }));
                
                d(this.WhenAnyValue(v => v.ViewModel.Include, v => v.ViewModel.IsDeleted)
                    .Subscribe(((bool Include, bool IsDeleted) options) =>
                    {
                        var color = ResourceExtensions.GetColor(options.Include || options.IsDeleted
                            ? "InactiveIconColor"
                            : "Option");
                        
                        AddToLibraryButton.IsEnabled = !options.Include && !options.IsDeleted;
                        AddToLibraryButton.TextColor = color;
                        AddToQuestionListButton.IsEnabled = !options.Include && !options.IsDeleted;
                        AddToQuestionListButton.TextColor = color;
                    }));
                
                d(this.BindCommandCustom(AddToLibraryGesture, v => v.ViewModel.AddToLibraryCommand));
                d(this.BindCommandCustom(DeleteFromQuestionListGesture, v => v.ViewModel.DeleteFromQuestionListCommand));
                d(this.BindCommandCustom(UndoDeleteFromQuestionListGesture, v => v.ViewModel.UndoDeleteFromQuestionCommand));
                d(this.BindCommandCustom(DeleteFromLibraryGesture, v => v.ViewModel.DeleteFromLibraryCommand));
                d(this.BindCommandCustom(UndoDeleteFromLibraryGesture, v => v.ViewModel.UndoDeleteFromLibraryCommand));
                d(this.BindCommandCustom(AddToQuestionListGesture, v => v.ViewModel.AddToQuestionListCommand));
            });
    }
}