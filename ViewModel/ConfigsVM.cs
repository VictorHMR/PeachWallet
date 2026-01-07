using CommunityToolkit.Mvvm.ComponentModel;
using PeachWallet.Database;
using PeachWallet.Database.Models;
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

        public ConfigsVM(LocalDbService connection)
        {
            _connection = connection;
        }

        public async Task LoadAsync()
        {
            _isLoading = true;

            var config = await _connection.GetAsync<Configs>();

            DiaFechamentoFatura = config.NrDiaFechamentoFatura ?? 1;

            _isLoading = false;
        }


        partial void OnDiaFechamentoFaturaChanged(int value)
        {
            if (_isLoading)
                return;

            _ = SaveDiaFechamentoAsync(value);
        }

        private async Task SaveDiaFechamentoAsync(int dia)
        {
            var config = await _connection.GetAsync<Configs>();

            if (config.NrDiaFechamentoFatura == dia)
                return;

            config.NrDiaFechamentoFatura = dia;
            await _connection.UpdateAsync(config);
        }
    }
}
