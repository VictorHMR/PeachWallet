using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PeachWallet.Models
{
    public partial class ConfigsDTO: ObservableObject
    {
        [ObservableProperty]
        private int idConfig;
        [ObservableProperty]
        private int? idContaMovimentacao;
        [ObservableProperty]
        private int? idContaInvestimento;
        [ObservableProperty]
        private int? nrDiaFechamentoFatura;
    }
}
