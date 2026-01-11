using PeachWallet.Database;
using PeachWallet.Database.Models;
using PeachWallet.Models;
using PeachWallet.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace PeachWallet.Services
{
    public class LiquidacaoService
    {
        private readonly LocalDbService _connection;

        public LiquidacaoService(LocalDbService connection)
        {
            _connection = connection;
        }

        public async Task LiquidarLancamentosVencidosAsync(DateTime data)
        {
            Configs configs = await _connection.GetAsync<Configs>();
            ContaBancaria contaMov = await _connection.GetAsync<ContaBancaria>(x => x.Id == configs.IdContaMovimentacao);
            ContaBancaria contaInvest = await _connection.GetAsync<ContaBancaria>(x => x.Id == configs.IdContaInvestimento);


            DateTime dataFinalFatura = new DateTime(data.Year, data.Month, configs.NrDiaFechamentoFatura ?? 1);
            DateTime dataInicialFatura = dataFinalFatura.AddMonths(-1);

            Expression<Func<Lancamento, bool>> predicate = x =>
                (!x.FlLiquidado && !x.FlCredito && x.DtLancamento < data) ||
                (x.DtLancamento >= dataInicialFatura &&
                x.DtLancamento < dataFinalFatura && x.FlCredito && !x.FlLiquidado);

            var lstLancamentos = await _connection.SelectAsync<Lancamento>(predicate);

            foreach (var lancamento in lstLancamentos)
            {
                lancamento.FlLiquidado = await AtualizarSaldoConta(lancamento, contaMov, contaInvest, configs, data);   
                await _connection.UpdateAsync<Lancamento>(lancamento);

                if(lancamento.FlLiquidado && lancamento.IdLancamentoRecorrente != null && lancamento.IdLancamentoRecorrente != 0)
                {
                    LancamentoRecorrente lancamentoRecorrente = await _connection.GetAsync<LancamentoRecorrente>(x=> x.Id == lancamento.IdLancamentoRecorrente);
                    lancamentoRecorrente.NrMeses = lancamentoRecorrente.NrMeses != null ? lancamentoRecorrente.NrMeses - 1 : null;
                    await _connection.UpdateAsync<LancamentoRecorrente>(lancamentoRecorrente);
                }
            }
        }
        public async Task<bool> AtualizarSaldoConta(Lancamento lancamento, ContaBancaria contaMov, ContaBancaria contaInvest, Configs configs, DateTime data)
        {
            bool atualizado = false;
            bool execute = false;

            if (configs.IdContaMovimentacao is null)
                return atualizado;
            if (configs.IdContaInvestimento is null)
                return atualizado;

            if (lancamento.FlCredito && lancamento.DtLancamento.Date < data.Date)
                execute = true;
            if (!lancamento.FlCredito && !lancamento.FlLiquidado && lancamento.DtLancamento.Date <= data.Date)
                execute = true;

            if (execute)
            {
                if (lancamento.TipoLancamento == (int)TiposLancamento.Entrada)
                    contaMov.Saldo += lancamento.Valor;
                else
                    contaMov.Saldo -= lancamento.Valor;

                await _connection.UpdateAsync(contaMov);

                if (lancamento.TipoLancamento == (int)TiposLancamento.Investimento)
                {
                    contaInvest.Saldo += lancamento.Valor;
                    await _connection.UpdateAsync(contaInvest);
                }

                atualizado = true;
            }
            return atualizado;
        }

        public async Task CriarLancamentoRecorrenteProxMes()
        {
            var lstLancamentoRecorrente = await _connection.SelectAsync<LancamentoRecorrente>(x => x.NrMeses == null);

            DateTime database = DateTime.Now.AddMonths(1);

            DateTime primeiroDiaMes = new DateTime(database.Year, database.Month, 1);
            DateTime primeiroDiaProxMes = primeiroDiaMes.AddMonths(1);
            foreach (var lancamentoRecorrente in lstLancamentoRecorrente)
            {
                var existeLanc = await _connection.GetAsync<Lancamento>(x => x.IdLancamentoRecorrente == lancamentoRecorrente.Id && x.DtLancamento >= primeiroDiaMes && x.DtLancamento < primeiroDiaProxMes);
                if(existeLanc == null)
                {
                    var lancamentoDB = new Lancamento
                    {
                        Descricao = lancamentoRecorrente.Descricao,
                        DtLancamento = database.Date,
                        TipoLancamento = (int)lancamentoRecorrente.TipoLancamento,
                        Valor = lancamentoRecorrente.Valor,
                        IdLancamentoRecorrente = lancamentoRecorrente.Id,
                        FlCredito = lancamentoRecorrente.FlCredito,
                    };
                }
            }


        }

    }

}
