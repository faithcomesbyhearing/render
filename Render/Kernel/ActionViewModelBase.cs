using System.Reactive.Linq;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace Render.Kernel
{
    public enum ActionState
    {
        Optional,
        Required,
        Inactive
    }

    public class ActionViewModelBase : ViewModelBase, IActionViewModelBase
    {
        private bool _requirementCompletionHandlerExists;

        protected Guid? RequirementIdToListen { get; private set; }

        [Reactive] public ActionState ActionState { get; set; } = ActionState.Required;

        public ActionViewModelBase(
            ActionState actionState,
            string urlPathSegment,
            IViewModelContextProvider viewModelContextProvider) :
            base(urlPathSegment, viewModelContextProvider)
        {
            SetState(actionState);
        }

        public ActionViewModelBase(
            ActionState actionState,
            Guid requirementIdToListen,
            string urlPathSegment,
            IViewModelContextProvider viewModelContextProvider) :
            base(urlPathSegment, viewModelContextProvider)
        {
            SetState(actionState, requirementIdToListen);
        }

        protected void SetState(ActionState actionState, Guid requirementIdToListen)
        {
            switch (actionState)
            {
                case ActionState.Required:
                    RequirementIdToListen = requirementIdToListen;

                    if (SessionStateService.RequirementMetInSession(requirementIdToListen))
                    {
                        SetState(ActionState.Optional);
                    }
                    else
                    {
                        SetState(ActionState.Required);

                        if (!_requirementCompletionHandlerExists)
                        {
                            // complete Session Requirement when ActionState is set to 'Optional' (audio has been listened) 
                            Disposables.Add(this
                                .WhenAnyValue(x => x.ActionState)
                                .Where(x => x != ActionState.Required)
                                .ObserveOn(RxApp.MainThreadScheduler)
                                .Subscribe(state =>
                                {
                                    if (RequirementIdToListen != default)
                                    {
                                        SessionStateService.AddRequirementCompletion(RequirementIdToListen.Value);
                                    }
                                }));

                            _requirementCompletionHandlerExists = true;
                        }
                    }
                    break;
                case ActionState.Optional:
                case ActionState.Inactive:
                    SetState(actionState);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        protected void SetState(ActionState actionState)
        {
            ActionState = actionState;
        }
    }
}