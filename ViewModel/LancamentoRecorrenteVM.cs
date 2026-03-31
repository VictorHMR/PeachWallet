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
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using UraniumUI.Extensions;

namespace PeachWallet.ViewModel
{
    public partial class LancamentoRecorrenteVM: ObservableObject
    {
        private readonly LocalDbService _connection;

        public ObservableCollection<LancamentoRecorrenteDTO> LancamentosVisiveis { get; } = [];

        public ObservableCollection<LancamentoRecorrenteDTO> Lancamentos{ get; } = [];


        [ObservableProperty]
        private ObservableCollection<int> anos = [];

        [ObservableProperty]
        private int? anoSelecionado;

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

        private bool _dataCompleted;
        private int _pageSize = 10;

        ContaBancaria contaMov;
        Configs configs;
        ContaBancaria contaInvest;
        public LancamentoRecorrenteVM(LocalDbService connection)
        {
            _connection = connection;
        }

        public async Task InitializeVMAsync()
        {
            configs = await _connection.GetAsync<Configs>();
            if (configs.IdContaMovimentacao is not null)
                contaMov = await _connection.GetAsync<ContaBancaria>(x => x.Id == configs.IdContaMovimentacao);
            if (configs.IdContaInvestimento is not null)
                contaInvest = await _connection.GetAsync<ContaBancaria>(x => x.Id == configs.IdContaInvestimento);

            AnoSelecionado = DateTime.Now.Year;
            await ReloadLancamentoRecorrenteAsync();
            await LoadRelatorio();
            Anos = new ObservableCollection<int>(Lancamentos.OrderBy(x => x.DtLancamento).Select(x => x.DtLancamento.Year).Distinct());
        }

        #region COMMANDS

        [RelayCommand]
        public async Task GetLancamentoRecorrenteAsync()
        {
            if (_dataCompleted)
                return;


            var lstLancamentos = await _connection.SelectAsync<LancamentoRecorrente>();

            if (lstLancamentos.Count < _pageSize)
                _dataCompleted = true;

            foreach (var item in lstLancamentos.OrderByDescending(x=> x.DtLancamento))
            {
                int parcPagas = (await _connection.SelectAsync<Lancamento>(x => x.IdLancamentoRecorrente == item.Id && x.FlLiquidado)).Count();
                Lancamentos.Add(new LancamentoRecorrenteDTO
                {
                    IdLancamentoRecorrente = item.Id,
                    Descricao = item.Descricao,
                    DtLancamento = item.DtLancamento,
                    Valor = item.Valor,
                    TipoLancamento = (TiposLancamento)item.TipoLancamento,
                    CorTexto = LancamentoUtils.ObterCorTexto((TiposLancamento)item.TipoLancamento),
                    FlCredito = item.FlCredito,
                    NrMeses = item.NrMeses,
                    DisplayText = item.Descricao + " " + parcPagas + "/" + item.NrMeses,
                    DisplayPeriodoLancamento = item.DtLancamento.ToString("dd/MM/yyyy") + " a " + item.DtLancamento.AddMonths((item.NrMeses ?? 1) - 1).ToString("dd/MM/yyyy")
                });
            }
            FiltrarLancamentoRecorrente();
        }

        [RelayCommand]
        public async Task ReloadLancamentoRecorrenteAsync()
        {
            Lancamentos.Clear();
            _dataCompleted = false;
            await GetLancamentoRecorrenteAsync();
        }

        [RelayCommand]
        public async Task CriarLancamentoRecorrenteAsync()
        {
            await MopupService.Instance.PushAsync(
                    new LancamentoRecorrentePopup(PopupMode.Create, async (lancamentoRecorrente, mode) =>
                    {
                        if (mode == PopupMode.Create && lancamentoRecorrente != null)
                        {
                            int parcPagas = (await _connection.SelectAsync<Lancamento>(x => x.IdLancamentoRecorrente == lancamentoRecorrente.IdLancamentoRecorrente && x.FlLiquidado)).Count();

                            LancamentoRecorrente lancamentoRecorrenteDB = new LancamentoRecorrente
                            {
                                Descricao = lancamentoRecorrente.Descricao,
                                DtLancamento = lancamentoRecorrente.DtLancamento,
                                TipoLancamento = (int)lancamentoRecorrente.TipoLancamento,
                                Valor = lancamentoRecorrente.Valor,
                                FlCredito = lancamentoRecorrente.FlCredito,
                                NrMeses = lancamentoRecorrente.NrMeses
                            };

                            lancamentoRecorrente.IdLancamentoRecorrente = await _connection.CreateAsync(lancamentoRecorrenteDB);
                            lancamentoRecorrente.CorTexto = LancamentoUtils.ObterCorTexto(lancamentoRecorrente.TipoLancamento);
                            lancamentoRecorrente.DisplayText = lancamentoRecorrente.Descricao + " " + parcPagas + "/" + lancamentoRecorrente.NrMeses;
                            lancamentoRecorrente.DisplayPeriodoLancamento = lancamentoRecorrente.DtLancamento.ToString("dd/MM/yyyy") + " a " + lancamentoRecorrente.DtLancamento.AddMonths((lancamentoRecorrente.NrMeses ?? 1) - 1).ToString("dd/MM/yyyy");


                            await CriarLancamentos(lancamentoRecorrente);

                            var index = Lancamentos.TakeWhile(x => x.DtLancamento > lancamentoRecorrente.DtLancamento).Count();

                            Lancamentos.Insert(index, lancamentoRecorrente);
                            FiltrarLancamentoRecorrente();
                            await LoadRelatorio();

                        }

                    })
            );

        }
        [RelayCommand]
        public async Task AtualizarLancamentoRecorrenteAsync(LancamentoRecorrenteDTO lancamentoRecorrente)
        {
            await MopupService.Instance.PushAsync(
                    new LancamentoRecorrentePopup(PopupMode.Update, async (lancamentoRecorrente, mode) =>
                    {
                        if (mode == PopupMode.Update && lancamentoRecorrente != null)
                        {
                            int parcPagas = (await _connection.SelectAsync<Lancamento>(x => x.IdLancamentoRecorrente == lancamentoRecorrente.IdLancamentoRecorrente && x.FlLiquidado)).Count();
                            var lstLancamentos = await _connection.SelectAsync<Lancamento>(x => x.IdLancamentoRecorrente == lancamentoRecorrente.IdLancamentoRecorrente && !x.FlLiquidado);
                            foreach (var lancamento in lstLancamentos)
                                await _connection.DeleteAsync<Lancamento>(lancamento.Id);

                            await _connection.UpdateAsync(new LancamentoRecorrente
                            {
                                Id = lancamentoRecorrente.IdLancamentoRecorrente,
                                Descricao = lancamentoRecorrente.Descricao,
                                TipoLancamento = (int)lancamentoRecorrente.TipoLancamento,
                                Valor = lancamentoRecorrente.Valor,
                                DtLancamento = lancamentoRecorrente.DtLancamento,
                                FlCredito = lancamentoRecorrente.FlCredito,
                                NrMeses = lancamentoRecorrente.NrMeses,
                            });

                            lancamentoRecorrente.CorTexto = LancamentoUtils.ObterCorTexto(lancamentoRecorrente.TipoLancamento);
                            lancamentoRecorrente.DisplayText = lancamentoRecorrente.Descricao + " " + parcPagas + "/" + lancamentoRecorrente.NrMeses;
                            lancamentoRecorrente.DisplayPeriodoLancamento = lancamentoRecorrente.DtLancamento.ToString("dd/MM/yyyy") + " a " + lancamentoRecorrente.DtLancamento.AddMonths((lancamentoRecorrente.NrMeses ?? 1) - 1).ToString("dd/MM/yyyy");

                            await CriarLancamentos(lancamentoRecorrente);

                            var existente = Lancamentos.FirstOrDefault(x => x.IdLancamentoRecorrente == lancamentoRecorrente.IdLancamentoRecorrente);
                            if (existente == null)
                                return;

                            Lancamentos.Remove(existente);

                            var index = Lancamentos.TakeWhile(x => x.DtLancamento > lancamentoRecorrente.DtLancamento).Count();

                            Lancamentos.Insert(index, lancamentoRecorrente);

                        }

                    }, async dto => await RemoverLancamentoRecorrenteAsync(dto), lancamentoRecorrente)
            );
            FiltrarLancamentoRecorrente();
            await LoadRelatorio();
        }

        [RelayCommand]
        public async Task RemoverLancamentoRecorrenteAsync(LancamentoRecorrenteDTO lancamentoRecorrente)
        {
            var lstLancamentos = await _connection.SelectAsync<Lancamento>(x => x.IdLancamentoRecorrente == lancamentoRecorrente.IdLancamentoRecorrente && !x.FlLiquidado);

            foreach (var lancamento in lstLancamentos)
                await _connection.DeleteAsync<Lancamento>(lancamento.Id);

            await _connection.DeleteAsync<LancamentoRecorrente>(lancamentoRecorrente.IdLancamentoRecorrente);

            var item = Lancamentos.FirstOrDefault(x => x.IdLancamentoRecorrente == lancamentoRecorrente.IdLancamentoRecorrente);
            if (item != null)
                Lancamentos.Remove(item);

            FiltrarLancamentoRecorrente();
            await LoadRelatorio();
        }

        [RelayCommand]
        private async Task AnoSelecionadoAsync(int ano)
        {
            if (ano == null)
                return;

            FiltrarLancamentoRecorrente();
            await LoadRelatorio();
        }
        #endregion

        #region HELPERS 
        public async Task CriarLancamentos(LancamentoRecorrenteDTO lancamentoRecorrente)
        {
            int nrMesesAFrente = lancamentoRecorrente.NrMeses ?? 1;

            var dataBase = new DateTime(lancamentoRecorrente.DtLancamento.Year, 
                                        lancamentoRecorrente.DtLancamento.Month, 
                                        lancamentoRecorrente.DtLancamento.Day, 
                                        DateTime.Now.Hour, 
                                        DateTime.Now.Minute, 
                                        DateTime.Now.Second);

            for (int i = 0; i < nrMesesAFrente; i++)
            {
                var dataLancamento = dataBase.AddMonths(i);
                DateTime dtIni = new DateTime(dataLancamento.Year, dataLancamento.Month, 1);
                DateTime dtfim = dtIni.AddMonths(1);
                bool possuiLancamento = (await _connection.SelectAsync<Lancamento>(x => x.IdLancamentoRecorrente == lancamentoRecorrente.IdLancamentoRecorrente && x.DtLancamento >= dtIni && x.DtLancamento < dtfim)).Any();

                if (!possuiLancamento && dtfim > DateTime.Now.Date)
                {
                    var lancamentoDB = new Lancamento
                    {
                        Descricao = lancamentoRecorrente.Descricao + $" {i + 1}/{nrMesesAFrente}",
                        DtLancamento = dataLancamento,
                        TipoLancamento = (int)lancamentoRecorrente.TipoLancamento,
                        Valor = lancamentoRecorrente.Valor,
                        IdLancamentoRecorrente = lancamentoRecorrente.IdLancamentoRecorrente,
                        FlCredito = lancamentoRecorrente.FlCredito,
                    };
                    await _connection.CreateAsync(lancamentoDB);
                }

            }
        }

        public async Task LoadRelatorio()
        {
            GastosPeriodo = LancamentosVisiveis.Where(x => x.TipoLancamento == TiposLancamento.Saida && !x.FlCredito).Sum(x=> x.Valor);
            GastosCreditoPeriodo = LancamentosVisiveis.Where(x => x.TipoLancamento == TiposLancamento.Saida && x.FlCredito).Sum(x=> x.Valor);
            EntradasPeriodo = LancamentosVisiveis.Where(x => x.TipoLancamento == TiposLancamento.Entrada).Sum(x=> x.Valor);
            InvestidoPeriodo = LancamentosVisiveis.Where(x => x.TipoLancamento == TiposLancamento.Investimento).Sum(x=> x.Valor);

            DisponivelPeriodo = EntradasPeriodo - (GastosPeriodo + GastosCreditoPeriodo + InvestidoPeriodo);
        }

        public void FiltrarLancamentoRecorrente()
        {
            LancamentosVisiveis.Clear();
            var lstLancamentosAno = Lancamentos.Where(x => (x.DtLancamento.Year >= AnoSelecionado && x.DtLancamento.Year <= AnoSelecionado) || (x.DtLancamento.AddMonths((x.NrMeses ?? 1) - 1).Year >= AnoSelecionado && x.DtLancamento.AddMonths((x.NrMeses ?? 1) - 1).Year <= AnoSelecionado)).ToList();
            foreach (var item in lstLancamentosAno)
                LancamentosVisiveis.Add(item);

            Anos = new ObservableCollection<int>(Lancamentos.OrderBy(x => x.DtLancamento).Select(x => x.DtLancamento.Year).Distinct());
        }

        #endregion
    }
}
