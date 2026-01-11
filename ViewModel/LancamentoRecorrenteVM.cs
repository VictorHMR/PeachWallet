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

namespace PeachWallet.ViewModel
{
    public partial class LancamentoRecorrenteVM: ObservableObject
    {
        private readonly LocalDbService _connection;

        private readonly RelatorioRepository _relatorioRepository;

        private readonly LiquidacaoService _liquidacaoService;

        public ObservableCollection<LancamentoRecorrenteDTO> Lancamentos { get; } = [];

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
        public LancamentoRecorrenteVM(LocalDbService connection, LiquidacaoService liquidacaoService)
        {
            _connection = connection;
            _liquidacaoService = liquidacaoService;
            _relatorioRepository = new RelatorioRepository(_connection);
        }

        public async Task InitializeVMAsync()
        {
            configs = await _connection.GetAsync<Configs>();
            if (configs.IdContaMovimentacao is not null)
                contaMov = await _connection.GetAsync<ContaBancaria>(x => x.Id == configs.IdContaMovimentacao);
            if (configs.IdContaInvestimento is not null)
                contaInvest = await _connection.GetAsync<ContaBancaria>(x => x.Id == configs.IdContaInvestimento);

            await ReloadLancamentoRecorrenteAsync();
            await LoadRelatorio();
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

            foreach (var item in lstLancamentos)
            {
                Lancamentos.Add(new LancamentoRecorrenteDTO
                {
                    IdLancamentoRecorrente = item.Id,
                    Descricao = item.Descricao,
                    DtLancamento = item.DtLancamento,
                    Valor = item.Valor,
                    TipoLancamento = (TiposLancamento)item.TipoLancamento,
                    CorTexto = LancamentoUtils.ObterCorTexto((TiposLancamento)item.TipoLancamento),
                    FlCredito = item.FlCredito,
                    NrMeses = item.NrMeses
                });
            }

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

                            await CriarLancamentos(lancamentoRecorrente);

                            var index = Lancamentos.TakeWhile(x => x.DtLancamento > lancamentoRecorrente.DtLancamento).Count();

                            Lancamentos.Insert(index, lancamentoRecorrente);
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

                            await CriarLancamentos(lancamentoRecorrente);

                            var existente = Lancamentos.FirstOrDefault(x => x.IdLancamentoRecorrente == lancamentoRecorrente.IdLancamentoRecorrente);
                            if (existente == null)
                                return;

                            Lancamentos.Remove(existente);

                            var index = Lancamentos.TakeWhile(x => x.DtLancamento > lancamentoRecorrente.DtLancamento).Count();

                            Lancamentos.Insert(index, lancamentoRecorrente);

                        }

                    }, lancamentoRecorrente)
            );

        }

        [RelayCommand]
        public async Task RemoverLancamentoRecorrenteAsync(LancamentoRecorrenteDTO lancamentoRecorrente)
        {
            bool confirmar = await Application.Current.MainPage.DisplayAlert(
                "Excluir lançamento mensal",
                "Tem certeza que deseja excluir este lançamento? Essa operação irá excluir todos os futuros lançamentos relacionados e este lançamento mensal",
                "Excluir",
                "Cancelar"
            );
            if (!confirmar)
                return;

            var lstLancamentos = await _connection.SelectAsync<Lancamento>(x => x.IdLancamentoRecorrente == lancamentoRecorrente.IdLancamentoRecorrente && !x.FlLiquidado);

            foreach (var lancamento in lstLancamentos)
                await _connection.DeleteAsync<Lancamento>(lancamento.Id);

            await _connection.DeleteAsync<LancamentoRecorrente>(lancamentoRecorrente.IdLancamentoRecorrente);

            var item = Lancamentos.FirstOrDefault(x => x.IdLancamentoRecorrente == lancamentoRecorrente.IdLancamentoRecorrente);
            if (item != null)
                Lancamentos.Remove(item);

            await LoadRelatorio();
        }

        #endregion

        #region HELPERS 
        public async Task CriarLancamentos(LancamentoRecorrenteDTO lancamentoRecorrente)
        {
            int nrMesesAFrente = lancamentoRecorrente.NrMeses ?? 1;

            var dataBase = lancamentoRecorrente.DtLancamento.Date;

            for (int i = 1; i <= nrMesesAFrente; i++)
            {
                var dataLancamento = dataBase.AddMonths(i);

                var lancamentoDB = new Lancamento
                {
                    Descricao = lancamentoRecorrente.Descricao,
                    DtLancamento = dataLancamento,
                    TipoLancamento = (int)lancamentoRecorrente.TipoLancamento,
                    Valor = lancamentoRecorrente.Valor,
                    IdLancamentoRecorrente = lancamentoRecorrente.IdLancamentoRecorrente,
                    FlCredito = lancamentoRecorrente.FlCredito,
                };

                await _connection.CreateAsync(lancamentoDB);
            }
        }





        public async Task LoadRelatorio()
        {
            GastosPeriodo = await _connection.SumValueAsync<LancamentoRecorrente>(x => x.TipoLancamento == (int)TiposLancamento.Saida && !x.FlCredito, x => x.Valor);
            GastosCreditoPeriodo = await _connection.SumValueAsync<LancamentoRecorrente>(x => x.TipoLancamento == (int)TiposLancamento.Saida && x.FlCredito, x => x.Valor);
            EntradasPeriodo = await _connection.SumValueAsync<LancamentoRecorrente>(x => x.TipoLancamento == (int)TiposLancamento.Entrada , x => x.Valor);
            InvestidoPeriodo = await _connection.SumValueAsync<LancamentoRecorrente>(x => x.TipoLancamento == (int)TiposLancamento.Investimento , x => x.Valor);

            DisponivelPeriodo = EntradasPeriodo - (GastosPeriodo + GastosCreditoPeriodo + InvestidoPeriodo);
        }

        #endregion
    }
}
