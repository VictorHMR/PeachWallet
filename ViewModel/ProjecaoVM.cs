using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Mopups.Services;
using PeachWallet.Database;
using PeachWallet.Database.Models;
using PeachWallet.Models;
using PeachWallet.Utils;
using PeachWallet.View.SaldoPages;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using UraniumUI.Dialogs.Mopups;

namespace PeachWallet.ViewModel
{
    public partial class ProjecaoVM: ObservableObject
    {
        private readonly LocalDbService _connection;

        [ObservableProperty]
        private ObservableCollection<ProjecaoDTO> projecoes = [];

        [ObservableProperty]
        private double saldoAtual = 0;

        private bool _dataCompleted;

        // =======================
        // Construtor
        // =======================

        public ProjecaoVM(LocalDbService connection)
        {
            _connection = connection;
        }

        [RelayCommand]
        public async Task GetProjecoesAsync()
        {
            if (_dataCompleted)
                return;

            var lstProjecoes = await _connection.SelectAsync<Projecao>();

            foreach (var item in lstProjecoes)
            {
                DateTime fimDoAno = new DateTime(item.Ano, 12, 31);
                DateTime inicioDoAno = new DateTime(item.Ano, 1, 1);
                Expression<Func<Lancamento, bool>> predicate = x => (x.DtLancamento <= fimDoAno && x.DtLancamento >= inicioDoAno && x.TipoLancamento == (int)TiposLancamento.Investimento && x.FlLiquidado);

                double SaltoTotal = Projecoes.FirstOrDefault(x => x.Ano == item.Ano - 1)?.ValorTotal ?? SaldoAtual;
                double investidoTotal = await _connection.SumValueAsync<Lancamento>(predicate, x => x.Valor);
                Projecoes.Add(new ProjecaoDTO
                {
                    IdProjecao = item.Id,
                    Ano = item.Ano,
                    InvestidoMensal = item.InvestidoMensal,
                    Investido = item.InvestidoMensal * 12,
                    ValorTotal = item.InvestidoMensal * 12 + SaltoTotal - investidoTotal
                });
                AtualizarUltimoItem();
            }
        }

        [RelayCommand]
        public async Task ReloadProjecoesAsync()
        {
            Projecoes.Clear();
            _dataCompleted = false;
            await GetProjecoesAsync();
            _dataCompleted = true;
        }
        [RelayCommand]
        public async Task CriarProjecaoAsync()
        {
            var ano = Projecoes.Any() ? Projecoes.Max(x => x.Ano) + 1 : DateTime.Now.Date.Year;
            var result = await PromptService.ShowTextPromptAsync(
                title: "Nova Projeção",
                fieldTitle: $"Quanto pretende investir mensalmente em {ano}?",
                keyboard: Keyboard.Numeric
            );

            if (string.IsNullOrEmpty(result))
                return;
            int meses = DateTime.Now.Date.Year == ano ? 12 - DateTime.Now.Month + 1 : 12;
            double SaltoTotal = Projecoes.FirstOrDefault(x => x.Ano == ano - 1)?.ValorTotal ?? SaldoAtual;

            ProjecaoDTO projecao = new ProjecaoDTO
            {
                Ano = ano,
                InvestidoMensal = double.Parse(result),
                ValorTotal = double.Parse(result) * meses + SaltoTotal,
                Investido = double.Parse(result) * meses
            };

            projecao.IdProjecao = await _connection.CreateAsync(new Projecao
            {
                Ano = projecao.Ano,
                InvestidoMensal = projecao.InvestidoMensal,
            });

            Projecoes.Add(projecao);
            AtualizarUltimoItem();
        }
        [RelayCommand]
        public async Task EditarProjecaoAsync(ProjecaoDTO projecao)
        {

            var result = await PromptService.ShowTextPromptAsync(
                title: "Editar Projeção",
                fieldTitle: $"Quanto pretende investir mensalmente em {projecao.Ano}?",
                initialValue: projecao.InvestidoMensal.ToString(),
                keyboard: Keyboard.Numeric
            );

            if (string.IsNullOrEmpty(result))
                return;
            projecao.InvestidoMensal = double.Parse(result);

            await EditarProjecao(projecao);

            var projecoesFuturas = Projecoes.Where(x => x.Ano > projecao.Ano).ToList();
            foreach (var item in projecoesFuturas)
                await EditarProjecao(item);
        }

        [RelayCommand]
        private async Task RemoverProjecaoAsync(ProjecaoDTO projecao)
        {
            bool confirmar = await Application.Current.MainPage.DisplayAlert(
                                "Excluir Projeção",
                                "Tem certeza que deseja excluir a projeção ?",
                                "Excluir",
                                "Cancelar");

            if (!confirmar)
                return;

            await _connection.DeleteAsync<Projecao>(projecao.IdProjecao);

            var item = Projecoes.FirstOrDefault(x => x.IdProjecao == projecao.IdProjecao);
            if (item != null)
            {
                Projecoes.Remove(item);
                AtualizarUltimoItem();
            }
        }

        private async Task EditarProjecao(ProjecaoDTO projecao)
        {
            int meses = DateTime.Now.Date.Year == projecao.Ano ? 12 - DateTime.Now.Month + 1 : 12;
            double SaltoTotal = Projecoes.FirstOrDefault(x => x.Ano == projecao.Ano - 1)?.ValorTotal ?? SaldoAtual;

            projecao.ValorTotal = projecao.InvestidoMensal * meses + SaltoTotal;

            await _connection.UpdateAsync(new Projecao
            {
                Id = projecao.IdProjecao,
                Ano = projecao.Ano,
                InvestidoMensal = projecao.InvestidoMensal,
            });

            var existente = Projecoes.FirstOrDefault(x => x.IdProjecao == projecao.IdProjecao);
            if (existente == null)
                return;
            existente.Ano = projecao.Ano;
            existente.InvestidoMensal = projecao.InvestidoMensal;
            existente.Investido = projecao.InvestidoMensal * meses;
            existente.ValorTotal = projecao.ValorTotal;
        }

        private void AtualizarUltimoItem()
        {
            if (Projecoes == null || Projecoes.Count == 0)
                return;

            foreach (var p in Projecoes)
                p.PodeDeletar = false;

            Projecoes.Last().PodeDeletar = true;
        }

    }
}
