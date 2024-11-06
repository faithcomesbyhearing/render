using System;
using System.Reactive.Linq;
using ReactiveUI;

namespace Render.Components.StageSettings.ConsultantApprovalStageSettings
{
    public partial class ConsultantApprovalStageSettings
    {
        public ConsultantApprovalStageSettings()
        {
            InitializeComponent();

            this.WhenActivated(d =>
            {
                d(this.Bind(ViewModel, vm => vm.StageName,
                    v => v.StageName.Text));
                d(this.Bind(ViewModel, vm => vm.ConsultantApprovalStepName.StepName,
                    v => v.ConsultantApprovalStepName.Text));
                
                d(this.WhenAnyValue(x => x.ViewModel.FlowDirection)
                    .ObserveOn(RxApp.MainThreadScheduler)
                    .Subscribe(flowDirection =>
                    {
                        TopLevelElement.SetValue(FlowDirectionProperty, flowDirection);
                    }));
            });
        }
    }
}