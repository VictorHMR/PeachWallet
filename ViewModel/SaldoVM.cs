using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PeachWallet.Database;
using PeachWallet.Models;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PeachWallet.ViewModel
{
    public partial class SaldoVM: ObservableObject
    {
        public ContaBancariaVM ContaBancariaVM { get; }
        public ProjecaoVM ProjecaoVM { get; }
        public double SaldoTotal => ContaBancariaVM.ContasBancarias.Sum(x => x.SaldoAtual);
        private bool isLoading;
        public SaldoVM(LocalDbService _connection)
        {
            ContaBancariaVM = new ContaBancariaVM(_connection);
            ProjecaoVM = new ProjecaoVM(_connection);
        }
        public async Task LoadAsync()
        {
            isLoading = true;
            ContaBancariaVM.ContasBancarias.CollectionChanged -= OnContasChanged;
            ContaBancariaVM.ContasBancarias.CollectionChanged += OnContasChanged;

            await ContaBancariaVM.ReloadContaAsync();

            foreach (var conta in ContaBancariaVM.ContasBancarias)
            {
                conta.PropertyChanged -= OnContaPropertyChanged;
                conta.PropertyChanged += OnContaPropertyChanged;
            }
            ProjecaoVM.SaldoAtual = SaldoTotal;
            await ProjecaoVM.ReloadProjecoesAsync();
            isLoading = false;
        }

        private async void OnContasChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems != null)
            {
                foreach (ContaBancariaDTO conta in e.NewItems)
                {
                    conta.PropertyChanged -= OnContaPropertyChanged;
                    conta.PropertyChanged += OnContaPropertyChanged;
                }
            }

            if (e.OldItems != null)
            {
                foreach (ContaBancariaDTO conta in e.OldItems)
                {
                    conta.PropertyChanged -= OnContaPropertyChanged;
                }
            }

            OnPropertyChanged(nameof(SaldoTotal));
            if (!isLoading)
            {
                ProjecaoVM.SaldoAtual = SaldoTotal;
                await ProjecaoVM.ReloadProjecoesAsync();
            }
        }

        private async void OnContaPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ContaBancariaDTO.SaldoAtual))
            {
                OnPropertyChanged(nameof(SaldoTotal));
                if (!isLoading)
                {
                    ProjecaoVM.SaldoAtual = SaldoTotal;
                    await ProjecaoVM.ReloadProjecoesAsync();
                }
            }
        }

    }
}
