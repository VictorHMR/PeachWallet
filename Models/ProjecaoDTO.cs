using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PeachWallet.Models
{
    public partial class ProjecaoDTO: ObservableObject
    {
        [ObservableProperty]
        public int ano;
        [ObservableProperty]
        public double investidoMensal;
        [ObservableProperty]
        public double investido;
        [ObservableProperty]
        public double valorTotal;
        [ObservableProperty]
        public bool podeDeletar;
    }
}
