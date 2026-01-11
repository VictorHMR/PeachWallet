using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PeachWallet.Database;
using PeachWallet.Database.Repositories;
using PeachWallet.Models;
using PeachWallet.Utils;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace PeachWallet.ViewModel
{
    public partial class RelatorioAnualVM: ObservableObject
    {
        private readonly LocalDbService _connection;

        private readonly RelatorioRepository _relatorioRepository;
        [ObservableProperty]
        private ObservableCollection<ResumoMensalDTO> relatorioMensal = [];

        [ObservableProperty]
        private ResumoMensalDTO relatorioTotal;

        [ObservableProperty]
        private ObservableCollection<MesAnoLancamentoDTO> anosDisponiveis = [];
        [ObservableProperty]
        private MesAnoLancamentoDTO? anoSelecionado;

        private bool _dataCompleted;

        public RelatorioAnualVM(LocalDbService connection)
        {
            _connection = connection;
            _relatorioRepository = new RelatorioRepository(_connection);
        }

        public async Task InitializeVMAsync()
        {
            RelatorioMensal.Clear();
            _dataCompleted = false;
            await LoadAnosAsync();
            await LoadRelatorioAsync();
        }

        #region COMMANDS
        [RelayCommand]
        public async Task LoadRelatorioAsync()
        {
            if (AnoSelecionado == null)
                return;
            if (_dataCompleted)
                return;

            var relatorioAno = await _relatorioRepository.GetResumoAno(AnoSelecionado.Ano);

            foreach (var mes in relatorioAno)
                RelatorioMensal.Add(mes);
            

            RelatorioTotal = new ResumoMensalDTO
            {
                EntradasLiquidadas = relatorioAno.Sum(x=> x.EntradasLiquidadas),
                EntradasPendentes = relatorioAno.Sum(x=> x.EntradasPendentes),
                GastosLiquidados = relatorioAno.Sum(x=> x.GastosLiquidados),
                GastosPendentes = relatorioAno.Sum(x=> x.GastosPendentes),
                GastosCreditoLiquidados = relatorioAno.Sum(x=> x.GastosCreditoLiquidados),
                GastosCreditoPendentes = relatorioAno.Sum(x=> x.GastosCreditoPendentes),
                InvestimentoLiquidados = relatorioAno.Sum(x=> x.InvestimentoLiquidados),
                InvestimentoPendentes = relatorioAno.Sum(x=> x.InvestimentoPendentes),
                ValorDisponivelMes = relatorioAno.Sum(x=> x.ValorDisponivelMes),
                ValorDisponivelTotal = relatorioAno.LastOrDefault()?.ValorDisponivelTotal ?? 0,
            };

            _dataCompleted = true;

        }

        [RelayCommand]
        private async Task AnoSelecionadoAsync(MesAnoLancamentoDTO ano)
        {
            if (ano == null)
                return;
            RelatorioMensal.Clear();
            AnoSelecionado = ano;
            _dataCompleted = false;
            await LoadRelatorioAsync();
        }

        #endregion

        public async Task LoadAnosAsync()
        {
            var today = DateTime.Now;

            var anos = await _relatorioRepository.GetAnosComLancamentos();

            if (anos.Count() < 1 || !anos.Any(x => x.Ano == today.Year))
                anos.Insert(0, new MesAnoLancamentoDTO { Ano = today.Year });

            AnosDisponiveis = new ObservableCollection<MesAnoLancamentoDTO>(anos);

            var anoAtual = AnosDisponiveis.FirstOrDefault(x => x.Ano == today.Year);
            if (AnoSelecionado is null)
                AnoSelecionado = anoAtual;
            else
                AnoSelecionado = AnoSelecionado == anoAtual ? anoAtual : AnoSelecionado;

        }


    }
}
