using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Mopups.Services;
using PeachWallet.Database;
using PeachWallet.Database.Models;
using PeachWallet.Models;
using PeachWallet.Utils;
using PeachWallet.View;
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
        [ObservableProperty]
        private ObservableCollection<MesLancamentoDTO> mesesDisponiveis = [];

        [ObservableProperty]
        private MesLancamentoDTO? mesSelecionado;

        private bool _dataCompleted;
        private int _pageSize = 10;
        private int _pageNumber = 1;


        public LancamentoVM(LocalDbService connection)
        {
            _connection = connection;
        }

        #region COMMANDS
        [RelayCommand]
        public async Task GetLancamentoAsync()
        {
            if (_dataCompleted)
                return;

            if (MesSelecionado == null)
                return;

            var firstDayOfMonth = new DateTime(MesSelecionado.Ano, MesSelecionado.Mes, 1);
            var firstDayOfNextMonth = firstDayOfMonth.AddMonths(1);

            Expression<Func<Lancamento, bool>> predicate = x =>
                x.DtLancamento >= firstDayOfMonth &&
                x.DtLancamento < firstDayOfNextMonth;

            var lstLancamentos = await _connection.SelectPagedAsync<Lancamento>(
                  pageNumber: _pageNumber,
                  pageSize: _pageSize,
                  predicate: predicate,
                  orderBy: x => x.DtLancamento,
                  ascending: false 
              );

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
        public async Task CriarLancamentoAsync()
        {
            await MopupService.Instance.PushAsync(
                    new LancamentoPopup(PopupMode.Create, async (lancamento, mode) =>
                    {
                        if (mode == PopupMode.Create && lancamento != null)
                        {
                            lancamento.DtLancamento = DateTime.Now;

                            lancamento.IdLancamento = await _connection.CreateAsync(new Lancamento
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

                            await LoadMesesAsync();
                        }
                            
                    })
            );


        }

        [RelayCommand]
        public async Task AbrirPopupLancamentoAsync(LancamentoDTO lancamento)
        {
            await MopupService.Instance.PushAsync(
                    new LancamentoPopup(PopupMode.Update, async (lancamento, mode) =>
                    {
                        if (mode == PopupMode.Update && lancamento != null)
                        {
                            await AtualizarLancamentoAsync(lancamento);
                        }
                        else if (mode == PopupMode.Delete && lancamento != null)
                        {
                            await RemoverLancamentoAsync(lancamento.IdLancamento);
                        }

                    }, lancamento)
            );

        }

        [RelayCommand]
        public async Task ReloadLancamentoAsync()
        {
            Lancamentos.Clear();
            _pageNumber = 1;
            _dataCompleted = false;
            await GetLancamentoAsync();
        }

        [RelayCommand]
        private async Task MesSelecionadoAsync(MesLancamentoDTO mes)
        {
            if (mes == null)
                return;
            MesSelecionado = mes;
            await ReloadLancamentoAsync();
        }
        #endregion

        #region HELPERS
        private async Task AtualizarLancamentoAsync(LancamentoDTO lancamento)
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

        public async Task LoadMesesAsync()
        {
            var lancamentos = await _connection.SelectAsync<Lancamento>();
            var today = DateTime.Now;

            var meses = lancamentos
                .Select(x => new { x.DtLancamento.Year, x.DtLancamento.Month })
                .Distinct()
                .OrderBy(x => x.Year)
                .ThenBy(x => x.Month)
                .Select(x => new MesLancamentoDTO
                {
                    Ano = x.Year,
                    Mes = x.Month
                }).ToList();

            if (meses.Count() < 1)
                meses.Add(new MesLancamentoDTO { Mes = today.Month, Ano = today.Year });

            MesesDisponiveis = new ObservableCollection<MesLancamentoDTO>(meses);
            MesSelecionado = MesesDisponiveis.FirstOrDefault(x=> x.Ano == today.Year && x.Mes == today.Month);
        }
        #endregion
    }
}
