using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Mopups.Services;
using PeachWallet.Database;
using PeachWallet.Database.Models;
using PeachWallet.Database.Repositories;
using PeachWallet.Models;
using PeachWallet.Services;
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

        private readonly RelatorioRepository _relatorioRepository;

        private readonly LiquidacaoService _liquidacaoService;

        public ObservableCollection<LancamentoDTO> Lancamentos { get; } = [];
        [ObservableProperty]
        private ObservableCollection<MesAnoLancamentoDTO> mesesDisponiveis = [];

        [ObservableProperty]
        private MesAnoLancamentoDTO? mesSelecionado;

        [ObservableProperty]
        private double gastosPeriodo;
        [ObservableProperty]
        private double gastosCreditoPeriodo;
        [ObservableProperty]
        private double investidoPeriodo;
        [ObservableProperty]
        private double entradasPeriodo;
        [ObservableProperty]
        private double disponivelPeriodo;
        [ObservableProperty]
        private double sobrasPeriodo;

        private bool _dataCompleted;
        private int _pageSize = 10;
        private int _pageNumber = 1;

        ContaBancaria contaMov;
        Configs configs;
        ContaBancaria contaInvest;

        public LancamentoVM(LocalDbService connection, LiquidacaoService liquidacaoService)
        {
            _connection = connection;
            _relatorioRepository = new RelatorioRepository(_connection);
            _liquidacaoService = liquidacaoService;
        }

        public async Task InitializeVMAsync()
        {
            configs = await _connection.GetAsync<Configs>();
            if (configs.IdContaMovimentacao is not null)
                contaMov = await _connection.GetAsync<ContaBancaria>(x => x.Id == configs.IdContaMovimentacao);
            if (configs.IdContaInvestimento is not null)
                contaInvest = await _connection.GetAsync<ContaBancaria>(x => x.Id == configs.IdContaInvestimento);

            await LoadMesesAsync();
            await ReloadLancamentoAsync();
            await LoadRelatorio();
        }

        #region COMMANDS
        [RelayCommand]
        public async Task GetLancamentoAsync()
        {
            if (_dataCompleted)
                return;

            if (MesSelecionado == null)
                return;

            DateTime primeiroDiaMes = new DateTime(MesSelecionado.Ano, MesSelecionado.Mes, 1);
            DateTime primeiroDiaProximoMes = primeiroDiaMes.AddMonths(1);

            DateTime dataFinalFatura = new DateTime(MesSelecionado.Ano, MesSelecionado.Mes, configs.NrDiaFechamentoFatura ?? 1);
            DateTime dataInicialFatura = dataFinalFatura.AddMonths(-1);

            Expression<Func<Lancamento, bool>> predicate = x =>
                (x.DtLancamento >= primeiroDiaMes &&
                x.DtLancamento < primeiroDiaProximoMes && !x.FlCredito) ||
                (x.DtLancamento >= dataInicialFatura &&
                x.DtLancamento < dataFinalFatura && x.FlCredito);

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
                    CorTexto = LancamentoUtils.ObterCorTexto((TiposLancamento)item.TipoLancamento),
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
                            Lancamento lancamentoDB = new Lancamento
                            {
                                Descricao = lancamento.Descricao,
                                DtLancamento = lancamento.DtLancamento,
                                TipoLancamento = (int)lancamento.TipoLancamento,
                                Valor = lancamento.Valor,
                                IdLancamentoRecorrente = lancamento.IdLancamentoRecorrente,
                                FlCredito = lancamento.FlCredito,
                            };

                            lancamentoDB.FlLiquidado = await _liquidacaoService.AtualizarSaldoConta(lancamentoDB, contaMov, contaInvest, configs, DateTime.Now);
                            lancamento.IdLancamento = await _connection.CreateAsync(lancamentoDB);

                            lancamento.FlLiquidado = lancamentoDB.FlLiquidado;
                            lancamento.CorTexto = LancamentoUtils.ObterCorTexto(lancamento.TipoLancamento);

                            if (PertenceAoPeriodoSelecionado(lancamento))
                            {
                                var index = Lancamentos
                                    .TakeWhile(x => x.DtLancamento > lancamento.DtLancamento)
                                    .Count();

                                Lancamentos.Insert(index, lancamento);
                                await LoadRelatorio();
                            }

                            await LoadMesesAsync();
                        }
                            
                    }, null, new DateTime(MesSelecionado.Ano, MesSelecionado.Mes, DateTime.Now.Day))
            );

        }

        [RelayCommand]
        public async Task RemoverLancamentoAsync(LancamentoDTO lancamento)
        {
            if(lancamento.IdLancamentoRecorrente != null || lancamento.IdLancamentoRecorrente > 0)
            {
                await Application.Current.MainPage.DisplayAlert(
                    "Atenção",
                    "Lançamentos recorrentes não podem ser excluídos individualmente. Por favor, edite ou exclua a série de lançamentos recorrentes.",
                    "OK"
                );
                return;
            }

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
                if (configs.IdContaMovimentacao is not null)
                {
                    if (lancamento.TipoLancamento == TiposLancamento.Saida)
                        contaMov.Saldo += lancamento.Valor;
                    else if (lancamento.TipoLancamento == TiposLancamento.Entrada)
                        contaMov.Saldo -= lancamento.Valor;
                    else if (lancamento.TipoLancamento == TiposLancamento.Investimento && configs.IdContaInvestimento is not null)
                    {
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
            if (lancamento.DtLancamento.Year == MesSelecionado.Ano && lancamento.DtLancamento.Month == MesSelecionado.Mes)
                await LoadRelatorio();
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
        private async Task MesSelecionadoAsync(MesAnoLancamentoDTO mes)
        {
            if (mes == null)
                return;
            MesSelecionado = mes;
            await ReloadLancamentoAsync();
            await LoadRelatorio();
        }
        #endregion

        #region HELPERS
        class Temporario
        {
            public int Ano { get; set; }
        }
        public async Task LoadMesesAsync()
        {
            var today = DateTime.Now;

            var meses = await _relatorioRepository.GetMesesComLancamentos();

            if (meses.Count() < 1 || !meses.Any(x=> x.Ano == today.Year && x.Mes == today.Month))
                meses.Insert(0, new MesAnoLancamentoDTO { Mes = today.Month, Ano = today.Year });

            MesesDisponiveis = new ObservableCollection<MesAnoLancamentoDTO>(meses);

            var mesAtual = MesesDisponiveis.FirstOrDefault(x => x.Ano == today.Year && x.Mes == today.Month);
            if(MesSelecionado is null)
                MesSelecionado = mesAtual;
            else
                MesSelecionado = MesSelecionado == mesAtual ? mesAtual : MesSelecionado;
        }

        public async Task LoadRelatorio()
        {
            if (contaMov == null || contaInvest == null)
                return;
            bool PeriodoAtual = MesSelecionado.Ano == DateTime.Now.Year && MesSelecionado.Mes == DateTime.Now.Month;

            ResumoMensalDTO resumo = await _relatorioRepository.GetResumoMes(MesSelecionado.Ano, MesSelecionado.Mes);
            EntradasPeriodo = resumo.EntradaTotal;
            GastosPeriodo = resumo.GastosLiquidados + resumo.GastosPendentes;
            GastosCreditoPeriodo = resumo.GastosCreditoLiquidados + resumo.GastosCreditoPendentes;
            InvestidoPeriodo = resumo.InvestidoTotal;

            SobrasPeriodo = resumo.ValorDisponivelMes;
            if(PeriodoAtual)
                DisponivelPeriodo = contaMov.Saldo + resumo.EntradasPendentes - resumo.GastosPendentes - resumo.GastosCreditoPendentes - resumo.InvestimentoPendentes;
            else
            {
                double SobrasMesesAnteriores = 0;
                foreach (var mes in MesesDisponiveis)
                {
                    if (mes.Ano < MesSelecionado.Ano || (mes.Ano == MesSelecionado.Ano && mes.Mes < MesSelecionado.Mes))
                    {
                        ResumoMensalDTO resumoMesAnterior = await _relatorioRepository.GetResumoMes(mes.Ano, mes.Mes);
                        SobrasMesesAnteriores += resumoMesAnterior.EntradasPendentes - resumoMesAnterior.GastosPendentes - resumoMesAnterior.GastosCreditoPendentes - resumoMesAnterior.InvestimentoPendentes;
                    }
                }
                DisponivelPeriodo = contaMov.Saldo + SobrasMesesAnteriores + resumo.EntradasPendentes - resumo.GastosPendentes - resumo.GastosCreditoPendentes - resumo.InvestimentoPendentes;
            }
        }

        private bool PertenceAoPeriodoSelecionado(LancamentoDTO lancamento)
        {
            DateTime primeiroDiaMes = new DateTime(MesSelecionado.Ano, MesSelecionado.Mes, 1);
            DateTime primeiroDiaProximoMes = primeiroDiaMes.AddMonths(1);

            DateTime dataFinalFatura = new DateTime(
                MesSelecionado.Ano,
                MesSelecionado.Mes,
                configs.NrDiaFechamentoFatura ?? 1
            );

            DateTime dataInicialFatura = dataFinalFatura.AddMonths(-1);

            if (!lancamento.FlCredito)
            {
                return lancamento.DtLancamento >= primeiroDiaMes &&
                       lancamento.DtLancamento < primeiroDiaProximoMes;
            }

            return lancamento.DtLancamento >= dataInicialFatura &&
                   lancamento.DtLancamento < dataFinalFatura;
        }

        #endregion
    }
}
