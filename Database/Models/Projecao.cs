using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PeachWallet.Database.Models
{
    public class Projecao: BaseSQLiteModel
    {
        public int Ano { get; set; }
        public double InvestidoMensal { get; set; }
    }
}
