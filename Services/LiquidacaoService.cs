using PeachWallet.Database;
using PeachWallet.Database.Models;
using PeachWallet.Models;
using PeachWallet.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
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

            var lstLancamentos = await _connection.SelectAsync<Lancamento>(x=> !x.FlLiquidado && x.DtLancamento < data);

            foreach(var lancamento in lstLancamentos)
            {
                lancamento.FlLiquidado = await AtualizarSaldoConta(lancamento, contaMov, contaInvest, configs, data);   
                await _connection.UpdateAsync<Lancamento>(lancamento);
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
    }

}
