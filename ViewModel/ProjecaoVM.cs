using CommunityToolkit.Maui.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Mopups.Services;
using PeachWallet.Database;
using PeachWallet.Database.Models;
using PeachWallet.Database.Repositories;
using PeachWallet.Models;
using PeachWallet.Utils;
using PeachWallet.View.SaldoPages;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using UraniumUI.Dialogs.Mopups;

namespace PeachWallet.ViewModel
{
    public partial class ProjecaoVM: ObservableObject
    {
        private readonly LocalDbService _connection;

        [ObservableProperty]
        private ObservableCollection<ProjecaoDTO> projecoes = [];

        [ObservableProperty]
        private double saldoAtual = 0;

        [ObservableProperty]
        private double saldoAtualSemDisp = 0;

        private bool _dataCompleted;


        // =======================
        // Construtor
        // =======================

        public ProjecaoVM(LocalDbService connection)
        {
            _connection = connection;        }

        [RelayCommand]
        public async Task GetProjecoesAsync()
        {
            if (_dataCompleted)
                return;

            var relatorioRepository = new RelatorioRepository(_connection);
            var configs = await _connection.GetAsync<Configs>();

            int anoAtual = DateTime.Now.Year;
            double saldoDispAnoPassado = 0;

            while (true)
            {
                DateTime inicio = new DateTime(anoAtual, 1, 1);
                DateTime fim = new DateTime(anoAtual, 12, 31);
                var lancamentosAno = await _connection.SelectAsync<Lancamento>(x => x.DtLancamento >= inicio && x.DtLancamento <= fim && x.TipoLancamento == (int)TiposLancamento.Investimento);

                if (!lancamentosAno.Any())
                    break;

                double investidoTotal = lancamentosAno.Sum(x => x.Valor);
                double investimentoNaoLiquidado = lancamentosAno.Where(x => !x.FlLiquidado).Sum(x => x.Valor);

                var resumoAno = await relatorioRepository.GetResumoAno(anoAtual);
                double saldoFinal = resumoAno.FirstOrDefault(x => x.NrMes == 12)?.ValorDisponivelTotal ?? 0;

                double saldoAtual = saldoFinal - saldoDispAnoPassado;

                double valorBase = Projecoes.FirstOrDefault(x => x.Ano == anoAtual - 1)?.ValorTotal ?? SaldoAtualSemDisp;

                double valorTotal = 0;

                switch ((TipoDeducaoSaldoDisp)configs.TipoDeducaoSaldoDisp)
                {
                    case TipoDeducaoSaldoDisp.Nao_Deduzir:
                    default:
                        valorTotal = valorBase + investimentoNaoLiquidado;
                        break;
                    case TipoDeducaoSaldoDisp.Ano_Atual:
                        if(anoAtual == DateTime.Now.Year)
                            valorTotal = valorBase + saldoAtual + investimentoNaoLiquidado;
                        else
                            valorTotal = valorBase + investimentoNaoLiquidado;
                        break;
                    case TipoDeducaoSaldoDisp.Todos_Anos:
                        valorTotal = valorBase + saldoAtual + investimentoNaoLiquidado;
                        break;
                }

                Projecoes.Add(new ProjecaoDTO
                {
                    Ano = anoAtual,
                    Investido = investidoTotal,
                    ValorTotal = valorTotal
                });

                saldoDispAnoPassado = saldoFinal;
                anoAtual++;
            }
        }

        [RelayCommand]
        public async Task ReloadProjecoesAsync()
        {
            Projecoes.Clear();
            _dataCompleted = false;
            await GetProjecoesAsync();
            _dataCompleted = true;
        }

    }
}
