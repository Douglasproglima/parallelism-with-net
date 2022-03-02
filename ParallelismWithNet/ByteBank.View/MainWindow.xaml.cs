using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using ByteBank.Core.Model;
using ByteBank.Core.Repository;
using ByteBank.Core.Service;
using ByteBank.View.Utils;

namespace ByteBank.View
{
    public partial class MainWindow : Window
    {
        private readonly ContaClienteRepository r_Repositorio;
        private readonly ContaClienteService r_Servico;
        private CancellationTokenSource _cancellationTokenSource;

        public MainWindow()
        {
            InitializeComponent();

            r_Repositorio = new ContaClienteRepository();
            r_Servico = new ContaClienteService();
        }

        private async void BtnProcessar_Click(object sender, RoutedEventArgs e)
        {
            HabilitarDesabilitarBtnProcessar(false);

            _cancellationTokenSource = new CancellationTokenSource();

            var contas = r_Repositorio.GetContaClientes();

            LimparView();

            PgsProgressoProcessamento.Maximum = contas.Count();

            var inicio = DateTime.Now;

            HabilitarDesabilitarBtnCancelar(true);

            //var progressDotnet = new Progress<string>((param) => PgsProgressoProcessamento.Value++);
            //var resultadoConsolidadeConta = await RetonarConsolidarConta(contas, progressDotnet);

            var barraProgresso = new GenericProgressBar<string>((param) => PgsProgressoProcessamento.Value++);

            try
            {
                var resultadoConsolidadeConta = await RetonarConsolidarConta(contas, barraProgresso, _cancellationTokenSource.Token);
                var fim = DateTime.Now;
                AtualizarView(resultadoConsolidadeConta, fim - inicio);
            }
            catch (OperationCanceledException erro)
            {
                TxtTempo.Text = "Operação Cancelada!";
            }
            finally
            { 
                HabilitarDesabilitarBtnProcessar(true);
                HabilitarDesabilitarBtnCancelar(false);
            }
        }

        private void BtnCancelar_Click(object sender, RoutedEventArgs e)
        {
            HabilitarDesabilitarBtnCancelar(false);
            _cancellationTokenSource.Cancel();
        }

        private void HabilitarDesabilitarBtnProcessar(bool isHabilitar)
        {
            BtnProcessar.IsEnabled = isHabilitar;
        }

        private void HabilitarDesabilitarBtnCancelar(bool isHabilitar)
        {
            BtnCancelar.IsEnabled = isHabilitar;
        }

        private async Task<string[]> RetonarConsolidarConta(IEnumerable<ContaCliente> contas, IProgress<string> paramProgresso, CancellationToken cancellationToken)
        {
            var tasks = contas.Select(conta =>
                Task.Factory.StartNew(() => 
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    //if(cancellationToken.IsCancellationRequested) throw new OperationCanceledException(cancellationToken);

                    var retornoConsolidacao = r_Servico.ConsolidarMovimentacao(conta, cancellationToken);

                    paramProgresso.Report(retornoConsolidacao);

                    cancellationToken.ThrowIfCancellationRequested();
                    //if (cancellationToken.IsCancellationRequested) throw new OperationCanceledException(cancellationToken);

                    return retornoConsolidacao;
                }, cancellationToken)
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