using PeachWallet.Database.Models;
using PeachWallet.Models;
using PeachWallet.Utils;
using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PeachWallet.Database.Repositories
{
    public class RelatorioRepository
    {
        private readonly SQLiteAsyncConnection _conn;
        public RelatorioRepository(LocalDbService db)
        {
            _conn = db.Connection; 
        }
        public async Task<ResumoMensalDTO> GetResumoMes(int Ano, int Mes)
        {
            var inicio = new DateTime(Ano, Mes, 1);
            var fim = inicio.AddMonths(1);

            string sql = @"
            SELECT
                SUM(CASE WHEN (TipoLancamento =" + (int)TiposLancamento.Entrada + @" AND FlLiquidado = 1) THEN Valor ELSE 0 END) as EntradasLiquidadas,
                SUM(CASE WHEN (TipoLancamento =" + (int)TiposLancamento.Entrada + @" AND FlLiquidado = 0) THEN Valor ELSE 0 END) as EntradasPendentes,
                SUM(CASE WHEN (TipoLancamento =" + (int)TiposLancamento.Saida + @" AND FlLiquidado = 1 AND FlCredito = 0) THEN Valor ELSE 0 END) as GastosLiquidados,
                SUM(CASE WHEN (TipoLancamento =" + (int)TiposLancamento.Saida + @" AND FlLiquidado = 0 AND FlCredito = 0) THEN Valor ELSE 0 END) as GastosPendentes,
                SUM(CASE WHEN (TipoLancamento =" + (int)TiposLancamento.Saida + @" AND FlLiquidado = 1 AND FlCredito = 1) THEN Valor ELSE 0 END) as GastosCreditoLiquidados,
                SUM(CASE WHEN (TipoLancamento =" + (int)TiposLancamento.Saida + @" AND FlLiquidado = 0 AND FlCredito = 1) THEN Valor ELSE 0 END) as GastosCreditoPendentes,
                SUM(CASE WHEN (TipoLancamento =" + (int)TiposLancamento.Investimento + @" AND FlLiquidado = 1) THEN Valor ELSE 0 END) as InvestimentoLiquidados,
                SUM(CASE WHEN (TipoLancamento =" + (int)TiposLancamento.Investimento + @" AND FlLiquidado = 0) THEN Valor ELSE 0 END) as InvestimentoPendentes
            FROM Lancamento
            WHERE DtLancamento >= ? AND DtLancamento < ?";


            var resumo = await _conn.QueryAsync<ResumoMensalDTO>(sql, inicio, fim);
            var dto = resumo.FirstOrDefault() ?? new ResumoMensalDTO();

            dto.ValorDisponivelMes = (dto.EntradasLiquidadas + dto.EntradasPendentes) - (dto.GastosLiquidados + dto.GastosPendentes + dto.GastosCreditoLiquidados + dto.GastosCreditoPendentes + dto.InvestimentoLiquidados + dto.InvestimentoPendentes);
            return dto;
        }
    }
}
