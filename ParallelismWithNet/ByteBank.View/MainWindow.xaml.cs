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

        public MainWindow()
        {
            InitializeComponent();

            r_Repositorio = new ContaClienteRepository();
            r_Servico = new ContaClienteService();
        }

        private void BtnProcessar_Click(object sender, RoutedEventArgs e)
        {
            var contas = r_Repositorio.GetContaClientes();

            var resultado = new List<string>();

            AtualizarView(new List<string>(), TimeSpan.Zero);

            var inicio = DateTime.Now;

            /*Objetivo: Transformar a lista de contas em uma lista de tarefas(Consolidade a conta de um ou mais clientes)*/
            var contasTarefas = contas.Select(conta =>
            {
                /*Task.Factory: Constroi as tarefas
                 *TaskScheduler: Define quando/onde uma tarefa será executada nos cores
                 * OBSERVACAO: Quando se usa o Task.Factory, automáticamente estamos usando o "TaskScheduler"
                 */
                return Task.Factory.StartNew(() =>
                {
                    var retornoConta = r_Servico.ConsolidarMovimentacao(conta);
                    resultado.Add(retornoConta);
                });
            }).ToArray();

            //Task.WaitAll: Aguarda a execução de todas as threads
            //Enquanto não executar todas as threads não passa para linha debaixo.
            Task.WaitAll(contasTarefas);

            var fim = DateTime.Now;

            AtualizarView(resultado, fim - inicio);
        }

        private void AtualizarView(List<String> result, TimeSpan elapsedTime)
        {
            var tempoDecorrido = $"{ elapsedTime.Seconds }.{ elapsedTime.Milliseconds} segundos!";
            var mensagem = $"Processamento de {result.Count} clientes em {tempoDecorrido}";

            LstResultados.ItemsSource = result;
            TxtTempo.Text = mensagem;
        }
    }
}