using CommunityToolkit.Maui;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.Handlers;
using Mopups.Hosting;
using PeachWallet.Database;
using UraniumUI;

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
            Microsoft.Maui.Handlers.EntryHandler.Mapper.AppendToMapping("MyCustomization", (handler, view) =>
            {
                if (view is Picker)
                {
                    handler.PlatformView.Background = null;
                }
            });
            builder.Services.AddMopupsDialogs();
            builder.Services.AddSingleton<LocalDbService>();
            CurrentApp = builder.Build();
            return CurrentApp;
        }
    }
}
