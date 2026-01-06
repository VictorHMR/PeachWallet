using CommunityToolkit.Maui;
using Microsoft.Extensions.Logging;
using Mopups.Hosting;
using PeachWallet.Database;
using UraniumUI;
using UraniumUI.Dialogs;
using static Microsoft.Maui.Controls.PlatformConfiguration.iOSSpecific.VisualElement;

namespace PeachWallet
{
    public static class MauiProgram
    {
        public static MauiApp CurrentApp { get; private set; }

        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .UseMauiCommunityToolkit()
                .UseUraniumUI()
                .UseUraniumUIMaterial()
                .ConfigureMopups()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                    fonts.AddFont("MaterialIcons-Regular.ttf", "MaterialIcons");
                    fonts.AddFontAwesomeIconFonts();
                });

#if DEBUG
            builder.Logging.AddDebug();
#endif
            builder.Services.AddMopupsDialogs();

            builder.Services.AddSingleton<LocalDbService>();
            CurrentApp = builder.Build();
            return CurrentApp;
        }
    }
}
