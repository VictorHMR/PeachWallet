using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PeachWallet.Database.Models
{
    public class ContaBancaria: BaseSQLiteModel
    {
        public string NomeConta { get; set; }
        public double Saldo { get; set; }
    }
}
