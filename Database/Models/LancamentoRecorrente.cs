using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PeachWallet.Database.Models
{
    public class LancamentoRecorrente: BaseSQLiteModel
    {
        public string Descricao { get; set; }
        public double Valor { get; set; }
        public int TipoLancamento { get; set; }
        public bool FlCredito { get; set; }
        public DateTime DtLancamento { get; set; }
        public int? NrMeses { get; set; }
    }
}
