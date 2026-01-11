using CommunityToolkit.Mvvm.ComponentModel;
using PeachWallet.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PeachWallet.Models
{
    public class ResumoMensalDTO
    {
        public string Mes { get; set; }
        public double EntradasLiquidadas { get; set; }
        public double EntradasPendentes { get; set; }
        public double GastosLiquidados { get; set; }
        public double GastosPendentes { get; set; }
        public double GastosCreditoLiquidados { get; set; }
        public double GastosCreditoPendentes { get; set; }
        public double InvestimentoLiquidados { get; set; }
        public double InvestimentoPendentes { get; set; }

        public double ValorDisponivelMes { get; set; }
        public double ValorDisponivelTotal { get; set; }

        public string MesAbreviado => string.IsNullOrWhiteSpace(Mes) ? string.Empty : Mes.Length <= 3 ? Mes : Mes[..3];
        public double EntradaTotal => EntradasLiquidadas + EntradasPendentes;
        public double InvestidoTotal => InvestimentoLiquidados + InvestimentoPendentes;
        public double GastoTotal => GastosLiquidados + GastosPendentes + GastosCreditoLiquidados + GastosCreditoPendentes;

        public Color CorTextoValorDisponivelMes => LancamentoUtils.ObterCorTexto(ValorDisponivelMes >= 0 ? TiposLancamento.Entrada : TiposLancamento.Saida);
        public Color CorTextoTotal => LancamentoUtils.ObterCorTexto(ValorDisponivelTotal >= 0 ? TiposLancamento.Entrada : TiposLancamento.Saida);
    }
}
