using GrimBuilder2.Contracts.Services;
using GrimBuilder2.ViewModels;

using Microsoft.UI.Xaml;

namespace GrimBuilder2.Activation;

public class DefaultActivationHandler(INavigationService navigationService) : ActivationHandler<LaunchActivatedEventArgs>
{
    protected override bool CanHandleInternal(LaunchActivatedEventArgs args)
    {
        // None of the ActivationHandlers has handled the activation.
        //return _navigationService.Frame?.Content == null;
        return true;
    }

    protected async override Task HandleInternalAsync(LaunchActivatedEventArgs args)
    {
        navigationService.NavigateTo(typeof(MainViewModel).FullName!);

        await Task.CompletedTask;
    }
}
