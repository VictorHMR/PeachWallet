using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PeachWallet.Utils
{
    public static class Configuration
    {
        #region Database
        public static string DATABASE_NAME= "PeachWalletDB";
        public static SQLite.SQLiteOpenFlags DATABASE_FLAGS = SQLite.SQLiteOpenFlags.ReadWrite | SQLite.SQLiteOpenFlags.Create | SQLite.SQLiteOpenFlags.SharedCache;
        public static string DATABASE_PATH = Path.Combine(FileSystem.AppDataDirectory, DATABASE_NAME);
        #endregion


    }

    public enum TipoDeducaoSaldoDisp
    {
        Nao_Deduzir=0,
        Ano_Atual=1,
        Todos_Anos= 2
    }
}
