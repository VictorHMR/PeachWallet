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
                await liquidacaoService.CriarLancamentoRecorrenteProxMes();
                await liquidacaoService.FecharSaldoMesPassado(DateTime.Now);
            });
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            return new Window(new AppShell());
        }
    }
}