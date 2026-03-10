using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PeachWallet.Database.Models
{
    public class SaldoMes : BaseSQLiteModel
    {
        public int Ano { get; set; }
        public int Mes { get; set; }
        public double Valor { get; set; }
    }
}
