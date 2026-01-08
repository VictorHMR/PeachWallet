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
                    FlCredito = item.FlCredito,
                    FlLiquidado = item.FlLiquidado
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
                            lancamento.FlLiquidado = await AtualizarSaldoConta(lancamento);

                            lancamento.IdLancamento = await _connection.CreateAsync(new Lancamento
                            {
                                Descricao = lancamento.Descricao,
                                DtLancamento = lancamento.DtLancamento,
                                TipoLancamento = (int)lancamento.TipoLancamento,
                                Valor = lancamento.Valor,
                                IdLancamentoRecorrente = lancamento.IdLancamentoRecorrente,
                                FlCredito = lancamento.FlCredito,
                                FlLiquidado = lancamento.FlLiquidado,
                            });
                            lancamento.CorTexto = ObterCorTexto(lancamento.TipoLancamento);

                            
                            if(lancamento.DtLancamento.Year == MesSelecionado.Ano && lancamento.DtLancamento.Month == MesSelecionado.Mes)
                            {
                                var index = Lancamentos
                                    .TakeWhile(x => x.DtLancamento > lancamento.DtLancamento)
                                    .Count();

                                Lancamentos.Insert(index, lancamento);
                            }

                            await LoadMesesAsync();
                        }
                            
                    })
            );

        }

        [RelayCommand]
        public async Task RemoverLancamentoAsync(LancamentoDTO lancamento)
        {
            bool confirmar = await Application.Current.MainPage.DisplayAlert(
                "Excluir lançamento",
                "Tem certeza que deseja excluir este lançamento?",
                "Excluir",
                "Cancelar"
            );
            if (!confirmar)
                return;
            if (lancamento.FlLiquidado)
            {
                Configs configs = await _connection.GetAsync<Configs>();
                if (configs.IdContaMovimentacao is not null)
                {
                    ContaBancaria contaMov = await _connection.GetAsync<ContaBancaria>(x => x.Id == configs.IdContaMovimentacao);

                    if (lancamento.TipoLancamento == TiposLancamento.Saida)
                        contaMov.Saldo += lancamento.Valor;
                    else if (lancamento.TipoLancamento == TiposLancamento.Entrada)
                        contaMov.Saldo -= lancamento.Valor;
                    else if (lancamento.TipoLancamento == TiposLancamento.Investimento && configs.IdContaInvestimento is not null)
                    {
                        ContaBancaria contaInvest = await _connection.GetAsync<ContaBancaria>(x => x.Id == configs.IdContaInvestimento);
                        contaMov.Saldo += lancamento.Valor;
                        contaInvest.Saldo -= lancamento.Valor;
                        await _connection.UpdateAsync(contaInvest);
                    }

                    await _connection.UpdateAsync(contaMov);

                }
            }


            await _connection.DeleteAsync<Lancamento>(lancamento.IdLancamento);

            var item = Lancamentos.FirstOrDefault(x => x.IdLancamento == lancamento.IdLancamento);
            if (item != null)
                Lancamentos.Remove(item);
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

            if (meses.Count() < 1 || !meses.Any(x=> x.Ano == today.Year && x.Mes == today.Month))
                meses.Insert(0, new MesLancamentoDTO { Mes = today.Month, Ano = today.Year });

            MesesDisponiveis = new ObservableCollection<MesLancamentoDTO>(meses);

            var mesAtual = MesesDisponiveis.FirstOrDefault(x => x.Ano == today.Year && x.Mes == today.Month);
            if(MesSelecionado is null)
                MesSelecionado = mesAtual;
            else
                MesSelecionado = MesSelecionado == mesAtual ? mesAtual : MesSelecionado;
        }

        public async Task<bool> AtualizarSaldoConta(LancamentoDTO lancamento)
        {
            bool atualizado = false;
            Configs configs = await _connection.GetAsync<Configs>();

            if(configs.IdContaMovimentacao is null)
                return atualizado;
            if(configs.IdContaInvestimento is null)
                return atualizado;

            ContaBancaria contaMov = await _connection.GetAsync<ContaBancaria>(x => x.Id == configs.IdContaMovimentacao);
            ContaBancaria contaInvest = await _connection.GetAsync<ContaBancaria>(x => x.Id == configs.IdContaInvestimento);

            if (!lancamento.FlCredito && !lancamento.FlLiquidado && lancamento.DtLancamento.Date <= DateTime.Now.Date)
            {
                if(lancamento.TipoLancamento == TiposLancamento.Entrada)
                    contaMov.Saldo += lancamento.Valor;
                else
                    contaMov.Saldo -= lancamento.Valor;

                await _connection.UpdateAsync(contaMov);

                if(lancamento.TipoLancamento == TiposLancamento.Investimento)
                {
                    contaInvest.Saldo += lancamento.Valor;
                    await _connection.UpdateAsync(contaInvest);
                }

                atualizado = true;
            }

                return atualizado;
        }
        #endregion
    }
}
