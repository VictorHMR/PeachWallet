using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PeachWallet.Utils
{
    public static class LancamentoUtils
    {
        public static Color ObterCorTexto(TiposLancamento tipoLancamento)
        {
            return tipoLancamento switch
            {
                TiposLancamento.Saida =>
                    (Color)Application.Current.Resources["Negative"],

                TiposLancamento.Entrada =>
                    (Color)Application.Current.Resources["Positive"],

                TiposLancamento.Investimento =>
                    (Color)Application.Current.Resources["Investment"],

                _ => Colors.White
            };
        }
    }
    public class TipoLancamentoPickerItem
    {
        public TiposLancamento Id { get; set; }
        public string Display { get; set; }
    }
    public enum TiposLancamento
    {
        Saida = 0,
        Entrada = 1,
        Investimento =2
    }

}
