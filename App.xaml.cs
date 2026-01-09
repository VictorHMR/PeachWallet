using PeachWallet.Services;

namespace PeachWallet
{
    public partial class App : Application
    {
        public App(LiquidacaoService liquidacaoService)
        {
            InitializeComponent();
            UserAppTheme = AppTheme.Dark;

            Task.Run(async () =>
            {
                await liquidacaoService.LiquidarLancamentosVencidosAsync(DateTime.Today);
            });
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            return new Window(new AppShell());
        }
    }
}