using AutoMapper;
using GrimBuilder2.Activation;
using GrimBuilder2.Contracts.Services;
using GrimBuilder2.Core.Contracts.Services;
using GrimBuilder2.Core.Models;
using GrimBuilder2.Core.Services;
using GrimBuilder2.Models;
using GrimBuilder2.Services;
using GrimBuilder2.ViewModels;
using GrimBuilder2.Views;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;

namespace GrimBuilder2;

public partial class App : Application
{
    public IHost Host { get; }

    public static T GetService<T>() where T : class
    {
        if ((App.Current as App)!.Host.Services.GetService(typeof(T)) is not T service)
            throw new ArgumentException($"{typeof(T)} needs to be registered in ConfigureServices within App.xaml.cs.");

        return service;
    }

    public static WindowEx MainWindow { get; } = new MainWindow();
    public static DispatcherQueue MainDispatcherQueue { get; } = DispatcherQueue.GetForCurrentThread();

    public static readonly IMapper Mapper = new MapperConfiguration(cfg =>
    {
        cfg.CreateMap<GdSkill, GdAssignableSkill>();
        cfg.CreateMap<GdClass, GdAssignableClass>()
            .ForMember(nameof(GdAssignableClass.AssignableSkills), x => x.MapFrom(src => src.Skills));
    }).CreateMapper();

    public static UIElement? AppTitlebar { get; set; }

    public App()
    {
        InitializeComponent();

        Host = Microsoft.Extensions.Hosting.Host
            .CreateDefaultBuilder()
            .UseContentRoot(AppContext.BaseDirectory)
            .ConfigureServices((context, services) =>
            {
                // Default Activation Handler
                services.AddSingleton<ActivationHandler<LaunchActivatedEventArgs>, DefaultActivationHandler>();

                // Other Activation Handlers

                // Services
                services.AddSingleton<ILocalSettingsService, LocalSettingsService>();
                services.AddSingleton<IThemeSelectorService, ThemeSelectorService>();
                services.AddSingleton<INavigationViewService, NavigationViewService>();

                services.AddSingleton<IActivationService, ActivationService>();
                services.AddSingleton<IPageService, PageService>();
                services.AddSingleton<INavigationService, NavigationService>();
                services.AddSingleton<DialogService>();

                // Core Services
                services.AddSingleton<IFileService, FileService>();

                services.AddSingleton<ArzParserService>();
                services.AddSingleton<GdService>();

                // Views and ViewModels
                services.AddSingleton<CommonViewModel>();
                services.AddSingleton<SettingsViewModel>();
                services.AddSingleton<SettingsPage>();
                services.AddSingleton<MasteriesViewModel>();
                services.AddSingleton<MasteriesPage>();
                services.AddSingleton<ShellPage>();
                services.AddSingleton<ShellViewModel>();
                services.AddSingleton<DevotionsViewModel>();
                services.AddSingleton<DevotionsPage>();

                // Configuration
                services.Configure<LocalSettingsOptions>(context.Configuration.GetSection(nameof(LocalSettingsOptions)));
            })
            .Build();

        UnhandledException += App_UnhandledException;
    }

    private void App_UnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
    {
        // TODO: Log and handle exceptions as appropriate.
        // https://docs.microsoft.com/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.application.unhandledexception.
    }

    protected async override void OnLaunched(LaunchActivatedEventArgs args)
    {
        base.OnLaunched(args);

        await App.GetService<IActivationService>().ActivateAsync(args);
    }
}
