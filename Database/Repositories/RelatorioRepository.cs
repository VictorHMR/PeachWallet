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

            if (contaMov is null)
                return new List<ResumoMensalDTO>();

            List<ResumoMensalDTO> resumoAno = new List<ResumoMensalDTO>();
            double ValorDispMesPassado = 0;

            bool anoAFrente = Ano > DateTime.Now.Year;
            int mesAtual = !anoAFrente ? DateTime.Now.Month : 1;
            double Saldo = contaMov.Saldo;

            if (anoAFrente)
            {
                var ResumoAnoAnterior = await GetResumoAno(Ano - 1);
                Saldo = ResumoAnoAnterior.FirstOrDefault(x => x.NrMes == 12)!.ValorDisponivelTotal;
            }
            for (int mes = mesAtual; mes >= 1; mes--)
            {
                var resumoMes = await GetResumoMes(Ano, mes, config.NrDiaFechamentoFatura);

                if (mes == mesAtual)
                {
                    resumoMes.ValorDisponivelTotal = Saldo + resumoMes.EntradasPendentes - resumoMes.GastosPendentes - resumoMes.GastosCreditoPendentes - resumoMes.InvestimentoPendentes - resumoMes.Saida_ReservaPendentes;
                }
                else
                {
                    var SaldoMes = await _conn.GetAsync<SaldoMes>(x => x.Mes == mes && x.Ano == Ano);
                    if(SaldoMes is not null)
                        resumoMes.ValorDisponivelTotal = SaldoMes.Valor;
                }

                resumoAno.Add(resumoMes);
            }

            for (int mes = mesAtual; mes <= 12; mes++)
            {
                if(mes == mesAtual)
                {
                    ValorDispMesPassado = resumoAno.FirstOrDefault(x=> x.NrMes == mesAtual)!.ValorDisponivelTotal;
                    continue;
                }
                
                var resumoMes = await GetResumoMes(Ano, mes, config.NrDiaFechamentoFatura);
                resumoMes.ValorDisponivelTotal = ValorDispMesPassado + resumoMes.ValorDisponivelMes - resumoMes.Saida_ReservaPendentes;
                
                ValorDispMesPassado = resumoMes.ValorDisponivelTotal;
                resumoAno.Add(resumoMes);
            }

            return resumoAno.OrderBy(x=> x.NrMes).ToList();
        }


        public async Task<ResumoMensalDTO> GetResumoMes(int Ano, int Mes, int? DiaFechamento = 1)
        {
            var inicio = new DateTime(Ano, Mes, 1);
            var fim = inicio.AddMonths(1);


            DateTime dataFinalFatura = new DateTime(Ano, Mes, DiaFechamento ?? 1);
            DateTime dataInicialFatura = dataFinalFatura.AddMonths(-1);

            string sql = @"
            SELECT
                SUM(CASE WHEN (TipoLancamento = " + (int)TiposLancamento.Entrada + @" AND FlLiquidado = 1) THEN Valor ELSE 0 END) as EntradasLiquidadas,
                SUM(CASE WHEN (TipoLancamento = " + (int)TiposLancamento.Entrada + @" AND FlLiquidado = 0) THEN Valor ELSE 0 END) as EntradasPendentes,
            
                SUM(CASE WHEN (TipoLancamento = " + (int)TiposLancamento.Saida + @" AND FlLiquidado = 1 AND FlCredito = 0) THEN Valor ELSE 0 END) as GastosLiquidados,
                SUM(CASE WHEN (TipoLancamento = " + (int)TiposLancamento.Saida + @" AND FlLiquidado = 0 AND FlCredito = 0) THEN Valor ELSE 0 END) as GastosPendentes,
            
                SUM(CASE WHEN (TipoLancamento = " + (int)TiposLancamento.Saida + @" AND FlLiquidado = 1 AND FlCredito = 1) THEN Valor ELSE 0 END) as GastosCreditoLiquidados,
                SUM(CASE WHEN (TipoLancamento = " + (int)TiposLancamento.Saida + @" AND FlLiquidado = 0 AND FlCredito = 1) THEN Valor ELSE 0 END) as GastosCreditoPendentes,
            
                SUM(CASE WHEN (TipoLancamento = " + (int)TiposLancamento.Investimento + @" AND FlLiquidado = 1) THEN Valor ELSE 0 END) as InvestimentoLiquidados,
                SUM(CASE WHEN (TipoLancamento = " + (int)TiposLancamento.Investimento + @" AND FlLiquidado = 0) THEN Valor ELSE 0 END) as InvestimentoPendentes,

                SUM(CASE WHEN (TipoLancamento = " + (int)TiposLancamento.Saida_Reserva + @" AND FlLiquidado = 1) THEN Valor ELSE 0 END) as Saida_ReservaLiquidados,
                SUM(CASE WHEN (TipoLancamento = " + (int)TiposLancamento.Saida_Reserva + @" AND FlLiquidado = 0) THEN Valor ELSE 0 END) as Saida_ReservaPendentes
            FROM Lancamento
            WHERE
            (
                FlCredito = 0
                AND DtLancamento >= ?
                AND DtLancamento < ?
            )
            OR
            (
                FlCredito = 1
                AND DtLancamento >= ?
                AND DtLancamento < ?
            )";


            var resumo = await _conn.Connection.QueryAsync<ResumoMensalDTO>(sql, inicio, fim, dataInicialFatura, dataFinalFatura);
            var dto = resumo.FirstOrDefault() ?? new ResumoMensalDTO();

            dto.Mes = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(Mes));
            dto.NrMes = Mes;
            dto.ValorDisponivelMes = (dto.EntradasLiquidadas + dto.EntradasPendentes) - (dto.GastosLiquidados + dto.GastosPendentes + dto.GastosCreditoLiquidados + dto.GastosCreditoPendentes + dto.InvestimentoLiquidados + dto.InvestimentoPendentes);
            return dto;
        }

        public async Task<ResumoMensalDTO> GetResumoDia(int Ano, int Mes, int Dia)
        {
            var inicio = new DateTime(Ano, Mes, Dia);
            var fim = inicio.AddDays(1);


            DateTime dataFinalFatura = new DateTime(Ano, Mes, Dia);
            dataFinalFatura = dataFinalFatura.AddDays(1);
            DateTime dataInicialFatura = dataFinalFatura.AddDays(-1);

            string sql = @"
            SELECT
                SUM(CASE WHEN (TipoLancamento = " + (int)TiposLancamento.Entrada + @" AND FlLiquidado = 1) THEN Valor ELSE 0 END) as EntradasLiquidadas,
                SUM(CASE WHEN (TipoLancamento = " + (int)TiposLancamento.Entrada + @" AND FlLiquidado = 0) THEN Valor ELSE 0 END) as EntradasPendentes,
            
                SUM(CASE WHEN (TipoLancamento = " + (int)TiposLancamento.Saida + @" AND FlLiquidado = 1 AND FlCredito = 0) THEN Valor ELSE 0 END) as GastosLiquidados,
                SUM(CASE WHEN (TipoLancamento = " + (int)TiposLancamento.Saida + @" AND FlLiquidado = 0 AND FlCredito = 0) THEN Valor ELSE 0 END) as GastosPendentes,
            
                SUM(CASE WHEN (TipoLancamento = " + (int)TiposLancamento.Saida + @" AND FlLiquidado = 1 AND FlCredito = 1) THEN Valor ELSE 0 END) as GastosCreditoLiquidados,
                SUM(CASE WHEN (TipoLancamento = " + (int)TiposLancamento.Saida + @" AND FlLiquidado = 0 AND FlCredito = 1) THEN Valor ELSE 0 END) as GastosCreditoPendentes,
            
                SUM(CASE WHEN (TipoLancamento = " + (int)TiposLancamento.Investimento + @" AND FlLiquidado = 1) THEN Valor ELSE 0 END) as InvestimentoLiquidados,
                SUM(CASE WHEN (TipoLancamento = " + (int)TiposLancamento.Investimento + @" AND FlLiquidado = 0) THEN Valor ELSE 0 END) as InvestimentoPendentes,

                SUM(CASE WHEN (TipoLancamento = " + (int)TiposLancamento.Saida_Reserva + @" AND FlLiquidado = 1) THEN Valor ELSE 0 END) as Saida_ReservaLiquidados,
                SUM(CASE WHEN (TipoLancamento = " + (int)TiposLancamento.Saida_Reserva + @" AND FlLiquidado = 0) THEN Valor ELSE 0 END) as Saida_ReservaPendentes
            FROM Lancamento
            WHERE
            (
                FlCredito = 0
                AND DtLancamento >= ?
                AND DtLancamento < ?
            )
            OR
            (
                FlCredito = 1
                AND DtLancamento >= ?
                AND DtLancamento < ?
            )";


            var resumo = await _conn.Connection.QueryAsync<ResumoMensalDTO>(sql, inicio, fim, dataInicialFatura, dataFinalFatura);
            var dto = resumo.FirstOrDefault() ?? new ResumoMensalDTO();

            dto.Mes = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(Mes));
            dto.ValorDisponivelMes = (dto.EntradasLiquidadas + dto.EntradasPendentes) - (dto.GastosLiquidados + dto.GastosPendentes + dto.GastosCreditoLiquidados + dto.GastosCreditoPendentes + dto.InvestimentoLiquidados + dto.InvestimentoPendentes);
            return dto;
        }

        public async Task<List<int>> GetDiasComLancamentos(int ano, int mes)
        {
            var teste =  await _conn.Connection.QueryAsync<DiasComLancamentoDTO>(@"
                SELECT 
                    CAST(strftime('%d', datetime(((DtLancamento / 10000000) - 62135596800), 'unixepoch')) AS INTEGER) AS Dia
                FROM Lancamento
                WHERE 
                    CAST(strftime('%Y', datetime(((DtLancamento / 10000000) - 62135596800), 'unixepoch')) AS INTEGER) = ?
                    AND
                    CAST(strftime('%m', datetime(((DtLancamento / 10000000) - 62135596800), 'unixepoch')) AS INTEGER) = ?
                GROUP BY 
                    strftime('%d', (DtLancamento / 10000000) - 62135596800, 'unixepoch')
                ORDER BY Dia", ano, mes);

            return teste.Select(x => x.Dia).ToList();
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

        public async Task<List<MesAnoLancamentoDTO>> GetMesesAbertos(DateTime data)
        {
            var result = await _conn.Connection.QueryAsync<MesAnoLancamentoDTO>(@"
        SELECT 
            CAST(strftime('%Y', datetime(((DtLancamento / 10000000) - 62135596800), 'unixepoch')) AS INTEGER) AS Ano,
            CAST(strftime('%m', datetime(((DtLancamento / 10000000) - 62135596800), 'unixepoch')) AS INTEGER) AS Mes
        FROM Lancamento l
        WHERE 
            (
                CAST(strftime('%Y', datetime(((DtLancamento / 10000000) - 62135596800), 'unixepoch')) AS INTEGER) < ?
                OR
                (
                    CAST(strftime('%Y', datetime(((DtLancamento / 10000000) - 62135596800), 'unixepoch')) AS INTEGER) = ?
                    AND
                    CAST(strftime('%m', datetime(((DtLancamento / 10000000) - 62135596800), 'unixepoch')) AS INTEGER) < ?
                )
            )
            AND NOT EXISTS (
                SELECT 1
                FROM SaldoMes s
                WHERE 
                    s.Ano = CAST(strftime('%Y', datetime(((l.DtLancamento / 10000000) - 62135596800), 'unixepoch')) AS INTEGER)
                    AND
                    s.Mes = CAST(strftime('%m', datetime(((l.DtLancamento / 10000000) - 62135596800), 'unixepoch')) AS INTEGER)
            )
        GROUP BY 
            strftime('%Y', datetime(((DtLancamento / 10000000) - 62135596800), 'unixepoch')),
            strftime('%m', datetime(((DtLancamento / 10000000) - 62135596800), 'unixepoch'))
        ORDER BY Ano, Mes DESC
    ", data.Year, data.Year, data.Month);

            return result.ToList();
        }

        public class DiasComLancamentoDTO
        {
            public int Dia { get; set; }
        }
    }
}
