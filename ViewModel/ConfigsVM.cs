using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PeachWallet.Database;
using PeachWallet.Database.Models;
using PeachWallet.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PeachWallet.ViewModel
{
    public partial class ConfigsVM : ObservableObject
    {
        private readonly LocalDbService _connection;
        public ContaBancariaVM ContaBancariaVM { get; }

        private bool _isLoading;

        [ObservableProperty]
        private ConfigsDTO configs;

        [ObservableProperty]
        private ContaBancariaDTO? contaMovimentacao;

        [ObservableProperty]
        private ContaBancariaDTO? contaInvestimento;

        public ConfigsVM(LocalDbService connection)
        {
            _connection = connection;
            ContaBancariaVM = new ContaBancariaVM(_connection);
        }

        public async Task LoadAsync()
        {
            _isLoading = true;

            await ContaBancariaVM.ReloadContaAsync();
            var config = await _connection.GetAsync<Configs>();

            Configs = new ConfigsDTO
            {
                IdConfig = config.Id,
                IdContaMovimentacao = config.IdContaMovimentacao,
                IdContaInvestimento = config.IdContaInvestimento
            };

            ContaMovimentacao = ContaBancariaVM.ContasBancarias
                .FirstOrDefault(x => x.IdContaBancaria == Configs.IdContaMovimentacao);

            ContaInvestimento = ContaBancariaVM.ContasBancarias
                .FirstOrDefault(x => x.IdContaBancaria == Configs.IdContaInvestimento);

            _isLoading = false;
        }

        partial void OnContaMovimentacaoChanged(ContaBancariaDTO? value)
        {
            AtualizarConfig(
                () => Configs.IdContaMovimentacao = value?.IdContaBancaria ?? 0
            );
        }

        partial void OnContaInvestimentoChanged(ContaBancariaDTO? value)
        {
            AtualizarConfig(
                () => Configs.IdContaInvestimento = value?.IdContaBancaria ?? 0
            );
        }

        private void AtualizarConfig(Action atualizar)
        {
            if (_isLoading || Configs == null)
                return;

            atualizar();
            _ = SaveConfigsAsync();
        }

        private async Task SaveConfigsAsync()
        {
            await _connection.UpdateAsync(new Configs
            {
                Id = Configs.IdConfig,
                IdContaMovimentacao = Configs.IdContaMovimentacao,
                IdContaInvestimento = Configs.IdContaInvestimento
            });
        }
    }
}
