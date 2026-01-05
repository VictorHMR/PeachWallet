using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PeachWallet.Database
{
    public class BaseSQLiteModel
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
    }
}
