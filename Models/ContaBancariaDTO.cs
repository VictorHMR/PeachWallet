using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PeachWallet.Models
{
    public partial class ContaBancariaDTO: ObservableObject
    {
        [ObservableProperty]
        private int idContaBancaria;
        [ObservableProperty]
        private string nome;
        [ObservableProperty]
        private double saldoAtual;
    }
}
