using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PeachWallet.Models
{
    public class ResumoMensalDTO
    {
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
    }
}
