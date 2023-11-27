using GrimBuilder2.Views.Controls;

namespace GrimBuilder2.Contracts.Services;

public interface INavigationService
{
    event EventHandler<CustomFrameViewNavigatedArgs>? Navigated;

    CustomFrameView? Frame { get; set; }

    void NavigateTo(string pageKey);
}
