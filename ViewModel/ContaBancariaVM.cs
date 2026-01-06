using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Mopups.Services;
using PeachWallet.Database;
using PeachWallet.Database.Models;
using PeachWallet.Models;
using PeachWallet.Utils;
using PeachWallet.View.SaldoPages;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PeachWallet.ViewModel
{
    public partial class ContaBancariaVM:ObservableObject
    {
        private readonly LocalDbService _connection;

        [ObservableProperty]
        private ObservableCollection<ContaBancariaDTO> contasBancarias = [];

        private bool _dataCompleted;

        // =======================
        // Construtor
        // =======================

        public ContaBancariaVM(LocalDbService connection)
        {
            _connection = connection;
        }

        [RelayCommand]
        public async Task GetContaBancariaAsync()
        {
            if (_dataCompleted)
                return;

            var lstContas = await _connection.SelectAsync<ContaBancaria>();


            foreach (var item in lstContas)
            {
                ContasBancarias.Add(new ContaBancariaDTO
                {
                    IdContaBancaria = item.Id,
                    Nome = item.NomeConta,
                    SaldoAtual = item.Saldo
                });
            }
        }

        [RelayCommand]
        public async Task ReloadContaAsync()
        {
            ContasBancarias.Clear();
            _dataCompleted = false;
            await GetContaBancariaAsync();
        }

        [RelayCommand]
        private async Task EditarContaAsync(ContaBancariaDTO conta)
        {
            await MopupService.Instance.PushAsync(
                new ContaBancariaPopup(
                    PopupMode.Update,
                    async (result, mode) =>
                    {
                        if (result != null)
                        {
                            await _connection.UpdateAsync(new ContaBancaria
                            {
                                Id = result.IdContaBancaria,
                                NomeConta = result.Nome,
                                Saldo = result.SaldoAtual,
                            });

                            var existente = ContasBancarias.FirstOrDefault(x => x.IdContaBancaria == result.IdContaBancaria);
                            if (existente == null)
                                return;
                            existente.Nome = result.Nome;
                            existente.SaldoAtual = result.SaldoAtual;
                        }
                    },
                    conta)
            );
        }
        [RelayCommand]
        private async Task CriarContaAsync(ContaBancariaDTO conta)
        {
            await MopupService.Instance.PushAsync(
                new ContaBancariaPopup(PopupMode.Create, async (contaBancaria, mode) =>
                {
                    if (mode == PopupMode.Create && contaBancaria != null)
                    {
                        contaBancaria.IdContaBancaria = await _connection.CreateAsync(new ContaBancaria
                        {
                            NomeConta = contaBancaria.Nome,
                            Saldo = contaBancaria.SaldoAtual
                        });

                        ContasBancarias.Add(contaBancaria);
                    }
                })
            );
        }

        [RelayCommand]
        private async Task RemoverContaAsync(ContaBancariaDTO conta)
        {
            bool confirmar = await Application.Current.MainPage.DisplayAlert(
                                "Excluir conta bancária",
                                "Tem certeza que deseja excluir a conta ?",
                                "Excluir",
                                "Cancelar");

            if (!confirmar)
                return;

            await _connection.DeleteAsync<ContaBancaria>(conta.IdContaBancaria);

            var item = ContasBancarias.FirstOrDefault(x => x.IdContaBancaria == conta.IdContaBancaria);
            if (item != null)
                ContasBancarias.Remove(item);
        }
    }
}
