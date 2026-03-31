using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PeachWallet.Database;
using PeachWallet.Database.Models;
using PeachWallet.Utils;
using System.Collections.ObjectModel;

namespace PeachWallet.ViewModel
{
    public partial class ConfigsVM : ObservableObject
    {
        private readonly LocalDbService _connection;
        private bool _isLoading;

        public ObservableCollection<int> DiasFechamento { get; } =
            new(Enumerable.Range(1, 30));

        [ObservableProperty]
        private int diaFechamentoFatura;

        [ObservableProperty]
        private TipoDeducaoSaldoDisp tipoDeducaoSaldoDisp;
        public ConfigsVM(LocalDbService connection)
        {
            _connection = connection;
        }

        [RelayCommand]
        public async Task ImportarDadosAsync()
        {
            bool confirmar = await Application.Current.MainPage.DisplayAlert(
                "Importar Dados",
                "Tem certeza que deseja prosseguir com a importação ? Essa ação irá sobrescrever os dados existentes",
                "Importar",
                "Cancelar"
            );
            if (!confirmar)
                return;
            await _connection.ImportDatabaseAsync();
        }
        [RelayCommand]
        public async Task ExportarDadosAsync()
        {
            await _connection.ExportDatabaseAsync();
        }

        public async Task LoadAsync()
        {
            _isLoading = true;

            var config = await _connection.GetAsync<Configs>();

            DiaFechamentoFatura = config.NrDiaFechamentoFatura ?? 1;
            TipoDeducaoSaldoDisp = (TipoDeducaoSaldoDisp)config.TipoDeducaoSaldoDisp;
            _isLoading = false;
        }


        partial void OnDiaFechamentoFaturaChanged(int value)
        {
            if (_isLoading)
                return;

            _ = SaveDiaFechamentoAsync(value);
        }

        [RelayCommand]
        public async Task DeduzirDispAsync(TiposDeducaoSaldoDispPickerItem value)
        {
            if (_isLoading)
                return;

            if(value.Id != TipoDeducaoSaldoDisp)
                await SaveDeduzirDispAsync(value.Id);
        }

        private async Task SaveDiaFechamentoAsync(int dia)
        {
            var config = await _connection.GetAsync<Configs>();

            if (config.NrDiaFechamentoFatura == dia)
                return;

            config.NrDiaFechamentoFatura = dia;
            await _connection.UpdateAsync(config);
        }

        private async Task SaveDeduzirDispAsync(TipoDeducaoSaldoDisp value)
        {
            var config = await _connection.GetAsync<Configs>();

            if (config.TipoDeducaoSaldoDisp == (int)value)
                return;

            config.TipoDeducaoSaldoDisp = (int)value;
            await _connection.UpdateAsync(config);
        }
    }

    public class TiposDeducaoSaldoDispPickerItem
    {
        public TipoDeducaoSaldoDisp Id { get; set; }
        public string Display { get; set; }
    }
}
