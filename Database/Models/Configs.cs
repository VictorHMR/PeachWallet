using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PeachWallet.Database.Models
{
    public class Configs: BaseSQLiteModel
    {
        public int? IdContaMovimentacao { get; set; }
        public int? IdContaInvestimento { get; set; }
    }
}
