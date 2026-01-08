using CommunityToolkit.Mvvm.ComponentModel;
using PeachWallet.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UraniumUI.Icons.FontAwesome;

namespace PeachWallet.Models
{
    public partial class LancamentoDTO : ObservableObject
    {
        [ObservableProperty]
        private int idLancamento;
        [ObservableProperty]
        private string descricao;
        [ObservableProperty]
        private double valor;
        [ObservableProperty]
        private TiposLancamento tipoLancamento;
        [ObservableProperty]
        private DateTime dtLancamento;
        [ObservableProperty]
        private int? idLancamentoRecorrente;
        [ObservableProperty]
        private Color corTexto;
        [ObservableProperty]
        private bool flCredito;
        [ObservableProperty]
        private bool flLiquidado;
        public string IconeLancamento
        {
            get
            {
                return tipoLancamento switch
                {
                    TiposLancamento.Saida =>
                        Solid.MoneyBillTransfer,

                    TiposLancamento.Entrada =>
                        Solid.MoneyBill,

                    TiposLancamento.Investimento =>
                        Solid.MoneyBillTrendUp,

                    _ =>
                        Solid.Question
                };
            }
        }

        public string ValorFormatado
        {
            get
            {
                var sinal = tipoLancamento switch
                {
                    TiposLancamento.Saida => "-",
                    TiposLancamento.Entrada => "+",
                    TiposLancamento.Investimento => "+",
                    _ => string.Empty
                };

                return $"{sinal}{valor:C}";
            }
        }
    }
}
