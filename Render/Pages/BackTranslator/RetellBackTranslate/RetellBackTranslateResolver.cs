﻿using Render.Kernel;
using Render.Models.Sections;
using Render.Models.Workflow;
using Render.Pages.AppStart.Home;

namespace Render.Pages.BackTranslator.RetellBackTranslate
{
    public static class RetellBackTranslateResolver
    {
        public static async Task<ViewModelBase> GetRetellPassageSelectViewModelAsync(
            Section section,
            Step step,
            IViewModelContextProvider viewModelContextProvider)
        {
            var idiom = viewModelContextProvider.GetCurrentDeviceIdiom();
            var workflowService = viewModelContextProvider.GetWorkflowService();
            var stage = workflowService.ProjectWorkflow.GetStage(step.Id);

            ViewModelBase viewModelToNavigateTo;

            if (idiom == DeviceIdiom.Tablet || idiom == DeviceIdiom.Desktop)
            {
                viewModelToNavigateTo = TabletRetellPassageSelectPageViewModel.Create(
                    viewModelContextProvider,
                    step,
                    section,
                    stage);
            }
            else
            {
                viewModelToNavigateTo = await HomeViewModel.CreateAsync(section.ProjectId, viewModelContextProvider);
            }

            return viewModelToNavigateTo;
        }

        public static async Task<ViewModelBase> GetRetellPassageTranslateViewModelAsync(
            Section section,
            Passage passage,
            RetellBackTranslation retellBackTranslation,
            Step step,
            IViewModelContextProvider viewModelContextProvider)
        {
            var idiom = viewModelContextProvider.GetCurrentDeviceIdiom();
            var workflowService = viewModelContextProvider.GetWorkflowService();
            var stage = workflowService.ProjectWorkflow.GetStage(step.Id);

            ViewModelBase viewModelToNavigateTo;

            if (idiom == DeviceIdiom.Tablet || idiom == DeviceIdiom.Desktop)
            {
                viewModelToNavigateTo = TabletRetellPassageTranslatePageViewModel.Create(
                    viewModelContextProvider,
                    step,
                    section,
                    passage,
                    retellBackTranslation,
                    stage);
            }
            else
            {
                viewModelToNavigateTo = await HomeViewModel.CreateAsync(section.ProjectId, viewModelContextProvider);
            }

            return viewModelToNavigateTo;
        }
    }
}