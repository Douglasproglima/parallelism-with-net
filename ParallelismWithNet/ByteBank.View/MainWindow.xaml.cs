using ByteBank.Core.Model;
using ByteBank.Core.Repository;
using ByteBank.Core.Service;
using System;
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
        private CancellationTokenSource _cts;

        public MainWindow()
        {
            InitializeComponent();

            r_Repositorio = new ContaClienteRepository();
            r_Servico = new ContaClienteService();
        }

        private async void BtnProcessar_Click(object sender, RoutedEventArgs e)
        {
            BtnProcessar.IsEnabled = false;

            _cts = new CancellationTokenSource();

            var contas = r_Repositorio.GetContaClientes();

            var contasQtdePorThread = contas.Count() / 4;

            //PgsProgresso.Maximum = contas.Count();
            //LimparView();

            var contas_parte1 = contas.Take(contasQtdePorThread);
            var contas_parte2 = contas.Skip(contasQtdePorThread).Take(contasQtdePorThread);
            var contas_parte3 = contas.Skip(contasQtdePorThread*2).Take(contasQtdePorThread);
            var contas_parte4 = contas.Skip(contasQtdePorThread*3);

            var resultado = new List<string>();

            var inicio = DateTime.Now;

            Thread primeiraThread = new Thread(() =>
            {
                foreach (var conta in contas_parte1)
                {
                    var resultadoProcessamento = r_Servico.ConsolidarMovimentacao(conta);
                    resultado.Add(resultadoProcessamento);
                }
            });

            Thread SegundaThread = new Thread(() =>
            {
                foreach (var conta in contas_parte2)
                {
                    var resultadoProcessamento = r_Servico.ConsolidarMovimentacao(conta);
                    resultado.Add(resultadoProcessamento);
                }
            });

            Thread TerceiraThread = new Thread(() =>
            {
                foreach (var conta in contas_parte3)
                {
                    var resultadoProcessamento = r_Servico.ConsolidarMovimentacao(conta);
                    resultado.Add(resultadoProcessamento);
                }
            });

            Thread QuartaThread = new Thread(() =>
            {
                foreach (var conta in contas_parte4)
                {
                    var resultadoProcessamento = r_Servico.ConsolidarMovimentacao(conta);
                    resultado.Add(resultadoProcessamento);
                }
            });

            primeiraThread.Start();
            SegundaThread.Start();
            TerceiraThread.Start();
            QuartaThread.Start();

            while (primeiraThread.IsAlive || SegundaThread.IsAlive || TerceiraThread.IsAlive || QuartaThread.IsAlive) 
                Thread.Sleep(250);

            
            BtnCancelar.IsEnabled = true;
            var progress = new Progress<String>(str =>
                PgsProgresso.Value++
            );
            
            //var byteBankProgress = new ByteBankProgress<String>(str =>
            //  PgsProgresso.Value++);

            try
            {
                var _resultado = await ConsolidarContas(contas, progress, _cts.Token);

                var fim = DateTime.Now;
                AtualizarView(_resultado, fim - inicio);
            }
            catch (OperationCanceledException)
            {
                TxtTempo.Text = "Operação cancelada pelo usuário";
            } 
            finally
            {
                BtnProcessar.IsEnabled = true;
                BtnCancelar.IsEnabled = false;
            }
        }

        private void BtnCancelar_Click(object sender, RoutedEventArgs e)
        {
            BtnCancelar.IsEnabled = false;
            _cts.Cancel();
        }

        private async Task<string[]> ConsolidarContas(IEnumerable<ContaCliente> contas, IProgress<string> reportadorDeProgresso, CancellationToken ct)
        {
            var tasks = contas.Select(conta =>
                Task.Factory.StartNew(() =>
                {
                    ct.ThrowIfCancellationRequested();

                    var resultadoConsolidacao = r_Servico.ConsolidarMovimentacao(conta, ct);

                    reportadorDeProgresso.Report(resultadoConsolidacao);

                    ct.ThrowIfCancellationRequested();
                    return resultadoConsolidacao;
                }, ct)
            );

            return await Task.WhenAll(tasks);
        }

        private void LimparView()
        {
            LstResultados.ItemsSource = null;
            TxtTempo.Text = null;
            PgsProgresso.Value = 0;
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
