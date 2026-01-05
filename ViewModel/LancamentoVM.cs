using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PeachWallet.Database;
using PeachWallet.Database.Models;
using PeachWallet.Models;
using PeachWallet.Utils;
using System;
using System.Collections.ObjectModel;
using System.Linq.Expressions;
using System.Threading.Tasks;
using UraniumUI.Icons.FontAwesome;

namespace PeachWallet.ViewModel
{
    public partial class LancamentoVM : ObservableObject
    {
        private readonly LocalDbService _connection;

        public ObservableCollection<LancamentoDTO> Lancamentos { get; } = [];

        private bool _dataCompleted;
        private int _pageSize = 10;
        private int _pageNumber = 1;

        // =======================
        // Construtor
        // =======================

        public LancamentoVM(LocalDbService connection)
        {
            _connection = connection;
        }

        // =======================
        // Commands
        // =======================

        [RelayCommand]
        public async Task GetLancamentoAsync()
        {
            if (_dataCompleted)
                return;

            await Task.Delay(1000);

            var lstLancamentos = await _connection.SelectAsync<Lancamento>();

            if (lstLancamentos.Count < _pageSize)
                _dataCompleted = true;

            foreach (var item in lstLancamentos)
            {
                Lancamentos.Add(new LancamentoDTO
                {
                    IdLancamento = item.Id,
                    Descricao = item.Descricao,
                    DtLancamento = item.DtLancamento,
                    Valor = item.Valor,
                    TipoLancamento = (TiposLancamento)item.TipoLancamento,
                    IdLancamentoRecorrente = item.IdLancamentoRecorrente,
                    CorTexto = ObterCorTexto((TiposLancamento)item.TipoLancamento),
                });
            }

            _pageNumber++;
        }


        [RelayCommand]
        public async Task ReloadLancamentoAsync()
        {
            Lancamentos.Clear();
            _pageNumber = 1;
            _dataCompleted = false;
            await GetLancamentoAsync();
        }

        //Helpers
        public async Task CriarLancamentoAsync(LancamentoDTO lancamento)
        {
            lancamento.DtLancamento = DateTime.Now;

            await _connection.CreateAsync(new Lancamento
            {
                Descricao = lancamento.Descricao,
                DtLancamento = lancamento.DtLancamento,
                TipoLancamento = (int)lancamento.TipoLancamento,
                Valor = lancamento.Valor,
                IdLancamentoRecorrente = lancamento.IdLancamentoRecorrente
            });
            lancamento.CorTexto = ObterCorTexto(lancamento.TipoLancamento);

            var index = Lancamentos
                .TakeWhile(x => x.DtLancamento > lancamento.DtLancamento)
                .Count();

            Lancamentos.Insert(index, lancamento);
        }

        public async Task AtualizarLancamentoAsync(LancamentoDTO lancamento)
        {
            await _connection.UpdateAsync(new Lancamento
            {
                Id = lancamento.IdLancamento,
                Descricao = lancamento.Descricao,
                TipoLancamento = (int)lancamento.TipoLancamento,
                Valor = lancamento.Valor,
                DtLancamento = lancamento.DtLancamento,
            });

            var existente = Lancamentos.FirstOrDefault(x => x.IdLancamento == lancamento.IdLancamento);
            if (existente == null)
                return;

            Lancamentos.Remove(existente);

            lancamento.CorTexto = ObterCorTexto(lancamento.TipoLancamento);

            var index = Lancamentos
                .TakeWhile(x => x.DtLancamento > lancamento.DtLancamento)
                .Count();

            Lancamentos.Insert(index, lancamento);
        }

        public async Task RemoverLancamentoAsync(int id)
        {
            await _connection.DeleteAsync<Lancamento>(id);

            var item = Lancamentos.FirstOrDefault(x => x.IdLancamento == id);
            if (item != null)
                Lancamentos.Remove(item);
        }


        public Color ObterCorTexto(TiposLancamento tipoLancamento)
        {
            return tipoLancamento switch
            {
                TiposLancamento.Saida =>
                    (Color)Application.Current.Resources["Negative"],

                TiposLancamento.Entrada =>
                    (Color)Application.Current.Resources["Positive"],

                TiposLancamento.Investimento =>
                    (Color)Application.Current.Resources["Investment"],

                _ => Colors.White
            };
        }
        
        }
}
