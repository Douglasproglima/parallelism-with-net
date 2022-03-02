using ByteBank.Core.Model;
using ByteBank.Core.Repository;
using ByteBank.Core.Service;
using ByteBank.View.Utils;
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

            LimparView();

            PgsProgressoProcessamento.Maximum = contas.Count();

            var inicio = DateTime.Now;

            //Acessa a thread principal para atualizar a barra de progresso
            var taskScheduleGui = TaskScheduler.FromCurrentSynchronizationContext();

            //var progressDotnet = new Progress<string>((param) => PgsProgressoProcessamento.Value++);
            //var resultadoConsolidadeConta = await RetonarConsolidarConta(contas, progressDotnet);

            var barraProgresso = new GenericProgressBar<string>((param) => PgsProgressoProcessamento.Value++);
            var resultadoConsolidadeConta = await RetonarConsolidarConta(contas, barraProgresso);

            var fim = DateTime.Now;
            
            AtualizarView(resultadoConsolidadeConta, fim - inicio);
            
            HabilitarBotaoProcessar(true);
        }

        private void HabilitarBotaoProcessar(bool isHabilitar)
        {
            BtnProcessar.IsEnabled = isHabilitar;
        }

        private async Task<string[]> RetonarConsolidarConta(IEnumerable<ContaCliente> contas, IProgress<string> paramProgresso)
        {
            var tasks = contas.Select(conta =>
                Task.Factory.StartNew(() => 
                {
                    var retornoConsolidacao = r_Servico.ConsolidarMovimentacao(conta);

                    paramProgresso.Report(retornoConsolidacao);

                    return retornoConsolidacao;
                })
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

        private void LimparView()
        {
            LstResultados.ItemsSource = null;
            TxtTempo.Text = String.Empty;
            PgsProgressoProcessamento.Value = 0;
        }
    }
}