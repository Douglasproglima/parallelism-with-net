using ByteBank.Core.Model;
using ByteBank.Core.Repository;
using ByteBank.Core.Service;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace ByteBank.View
{
    public partial class MainWindow : Window
    {
        private readonly ContaClienteRepository r_Repositorio;
        private readonly ContaClienteService r_Servico;

        public MainWindow()
        {
            InitializeComponent();

            r_Repositorio = new ContaClienteRepository();
            r_Servico = new ContaClienteService();
        }

        private async void BtnProcessar_Click(object sender, RoutedEventArgs e)
        {
            HabilitarBotaoProcessar(false);

            var contas = r_Repositorio.GetContaClientes();

            AtualizarView(new List<string>(), TimeSpan.Zero);

            var inicio = DateTime.Now;
            var resultadoConsolidadeConta = await RetonarConsolidarConta(contas);
            var fim = DateTime.Now;
            
            AtualizarView(resultadoConsolidadeConta, fim - inicio);
            
            HabilitarBotaoProcessar(true);
        }

        private void HabilitarBotaoProcessar(bool isHabilitar)
        {
            BtnProcessar.IsEnabled = isHabilitar;
        }

        private async Task<string[]> RetonarConsolidarConta(IEnumerable<ContaCliente> contas)
        {
            var tasks = contas.Select(conta =>
                Task.Factory.StartNew(() => r_Servico.ConsolidarMovimentacao(conta))
            );

            var resultado = await Task.WhenAll(tasks);

            return resultado;
        }

        private void AtualizarView(IEnumerable<String> result, TimeSpan elapsedTime)
        {
            var tempoDecorrido = $"{ elapsedTime.Seconds }.{ elapsedTime.Milliseconds} segundos!";
            var mensagem = $"Processamento de {result.Count()} clientes em {tempoDecorrido}";

            LstResultados.ItemsSource = result;
            TxtTempo.Text = mensagem;
        }
    }
}