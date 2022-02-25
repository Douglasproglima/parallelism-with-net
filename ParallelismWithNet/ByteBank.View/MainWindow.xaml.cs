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

        private void BtnProcessar_Click(object sender, RoutedEventArgs e)
        {
            BtnProcessar.IsEnabled = false;
            //Retorna a thread principal que está sendo executado no momento
            var taskScheduleUI = TaskScheduler.FromCurrentSynchronizationContext();
            var contas = r_Repositorio.GetContaClientes();

            AtualizarView(new List<string>(), TimeSpan.Zero);

            var inicio = DateTime.Now;



            /*Objetivo: Transformar a lista de contas em uma lista de tarefas(Consolidade a conta de um ou mais clientes)*/
            /*
            var contasTarefas = contas.Select(conta =>
            {
            */
            /*Task.Factory: Constroi as tarefas
             *TaskScheduler: Define quando/onde uma tarefa será executada nos cores
             * OBSERVACAO: Quando se usa o Task.Factory, automáticamente estamos usando o "TaskScheduler"
             */
            /*
                return Task.Factory.StartNew(() =>
                {
                    var retornoConta = r_Servico.ConsolidarMovimentacao(conta);
                    resultado.Add(retornoConta);
                });
            }).ToArray();
            */

            //Task.WaitAll: Aguarda a execução de todas as threads
            //Enquanto não executar todas as threads não passa para linha debaixo.
            //Task.WaitAll(contasTarefas);

            /*Diferente do WaitAll(), o WhenAll(): Ao invés de executar a thread que executa a função para travar e aguardar as outras
             * tarefas(threads), ele retornar uma outra threads, cujo a função é aguardar a execução das demais threads
             */

            RetonarConsolidarConta(contas)
                //Só executa quando a thread anterior terminar
                .ContinueWith((task) =>
                {
                    //Aqui, consigo pegar a thread que originou a execução, dessa forma tem info do retorno, dos erros etc...
                    var fim = DateTime.Now;

                    var resultado = task.Result; // Result vem do método RetonarConsolidarConta(conta)

                    AtualizarView(resultado, fim - inicio);
                }, taskScheduleUI /*Será executado de acordo com a demada da task principal*/)
                .ContinueWith((task) =>
                {
                    BtnProcessar.IsEnabled = true;
                }, taskScheduleUI);

            //Movi esse código para execução da thread .ContinueWith()
            //var fim = DateTime.Now;
            //AtualizarView(resultado, fim - inicio);
        }

        private Task<List<string>> RetonarConsolidarConta(IEnumerable<ContaCliente> contas)
        {
            var resultado = new List<string>();

            var tasks = contas.Select( conta => 
            {
                return Task.Factory.StartNew(() =>
               {
                    var resultadoConta = r_Servico.ConsolidarMovimentacao(conta);
                   resultado.Add(resultadoConta);
               });
            });

            return Task.WhenAll(tasks).ContinueWith( task => resultado);
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