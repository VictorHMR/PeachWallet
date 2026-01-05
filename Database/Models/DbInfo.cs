using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PeachWallet.Database.Models
{
    public class DbInfo:BaseSQLiteModel
    {
        public int Version { get; set; }

    }
}
