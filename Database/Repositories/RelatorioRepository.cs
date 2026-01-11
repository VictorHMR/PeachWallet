using PeachWallet.Database.Models;
using PeachWallet.Models;
using PeachWallet.Utils;
using System.Globalization;

namespace PeachWallet.Database.Repositories
{
    public class RelatorioRepository
    {
        private readonly LocalDbService _conn;
        public RelatorioRepository(LocalDbService db)
        {
            _conn = db; 
        }
        public async Task<List<ResumoMensalDTO>> GetResumoAno(int Ano)
        {
            Configs config = await _conn.GetAsync<Configs>();
            ContaBancaria contaMov = await _conn.GetAsync<ContaBancaria>(x => x.Id == config.IdContaMovimentacao);

            List<ResumoMensalDTO> resumoAno = new List<ResumoMensalDTO>();
            double ValorDispMesPassado = 0;
            for (int mes = 1; mes <= 12; mes++)
            {
                var resumoMes = await GetResumoMes(Ano, mes);

                if(mes == 1)
                {
                    resumoMes.ValorDisponivelTotal = contaMov.Saldo + resumoMes.EntradasPendentes - resumoMes.GastosPendentes - resumoMes.GastosCreditoPendentes - resumoMes.InvestimentoPendentes;

                }
                else
                {
                    resumoMes.ValorDisponivelTotal = ValorDispMesPassado + resumoMes.ValorDisponivelMes;
                }

                ValorDispMesPassado = resumoMes.ValorDisponivelTotal;
                resumoAno.Add(resumoMes);
            }

            return resumoAno;
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


            var resumo = await _conn.Connection.QueryAsync<ResumoMensalDTO>(sql, inicio, fim);
            var dto = resumo.FirstOrDefault() ?? new ResumoMensalDTO();

            dto.Mes = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(Mes));
            dto.ValorDisponivelMes = (dto.EntradasLiquidadas + dto.EntradasPendentes) - (dto.GastosLiquidados + dto.GastosPendentes + dto.GastosCreditoLiquidados + dto.GastosCreditoPendentes + dto.InvestimentoLiquidados + dto.InvestimentoPendentes);
            return dto;
        }

        public async Task<List<MesAnoLancamentoDTO>> GetMesesComLancamentos()
        {
            return await _conn.Connection.QueryAsync<MesAnoLancamentoDTO>(@"
            SELECT 
                CAST(strftime('%Y', (DtLancamento / 10000000) - 62135596800, 'unixepoch') AS INTEGER) AS Ano,
                CAST(strftime('%m', (DtLancamento / 10000000) - 62135596800, 'unixepoch') AS INTEGER) AS Mes
            FROM Lancamento
            GROUP BY 
                strftime('%Y', (DtLancamento / 10000000) - 62135596800, 'unixepoch'),
                strftime('%m', (DtLancamento / 10000000) - 62135596800, 'unixepoch')
            ORDER BY Ano, Mes");
        }


        public async Task<List<MesAnoLancamentoDTO>> GetAnosComLancamentos()
        {
            return await _conn.Connection.QueryAsync<MesAnoLancamentoDTO>(@"
            SELECT 
                CAST(strftime('%Y', (DtLancamento / 10000000) - 62135596800, 'unixepoch') AS INTEGER) AS Ano
            FROM Lancamento
            GROUP BY 
                strftime('%Y', (DtLancamento / 10000000) - 62135596800, 'unixepoch')
            ORDER BY Ano");
        }

    }
}
