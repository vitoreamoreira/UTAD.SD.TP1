using System;
using System.Data.SqlClient;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace TCP_Server
{
    class Server
    {
        private TcpListener listener;
        private static Mutex mutex = new Mutex();
        private string connectionString = "Server=MEEP_P\\SQLEXPRESS;Database=Sistemas_Distribuidos;Integrated Security=True;";

        public void StartServer(int port)
        {
            listener = new TcpListener(IPAddress.Any, port);
            listener.Start();
            Console.WriteLine("Servidor iniciado...");

            try
            {
                while (true)
                {
                    TcpClient client = listener.AcceptTcpClient();
                    Thread clientThread = new Thread(new ParameterizedThreadStart(HandleClient));
                    clientThread.Start(client);
                }
            }
            finally
            {
                listener.Stop();
            }
        }

        private void HandleClient(object clientObj)
        {
            TcpClient client = (TcpClient)clientObj;
            NetworkStream stream = client.GetStream();
            StreamReader reader = new StreamReader(stream, Encoding.ASCII);
            StreamWriter writer = new StreamWriter(stream, Encoding.ASCII) { AutoFlush = true };

            string clientId = null;

            writer.WriteLine("100 OK");
            try
            {
                string line;
                while ((line = reader.ReadLine()) != null && line != "QUIT")
                {
                    Console.WriteLine($"Received: {line}");

                    if (line.StartsWith("ID"))
                    {
                        clientId = line.Split(' ')[1];
                        writer.WriteLine($"ID {clientId} acknowledged.");
                    }
                    else if (line.StartsWith("COMPLETE TASK"))
                    {
                        string taskId = line.Split(' ')[2];
                        CompleteTask(taskId);
                        writer.WriteLine($"Task {taskId} marked as completed.");
                    }
                    else if (line.StartsWith("REQUEST TASK"))
                    {
                        string newTask = AllocateTask(clientId);
                        writer.WriteLine($"Task allocated: {newTask}");
                    }
                }
                writer.WriteLine("400 BYE");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception: {ex.Message}");
            }
            finally
            {
                client.Close();
            }
        }

        private void CompleteTask(string taskId)
        {
            mutex.WaitOne();
            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    string sqlUpdate = "UPDATE Servicos SET Estado = 'Concluido', ID_Cliente = NULL WHERE ID_Tarefa = @taskId";
                    using (SqlCommand command = new SqlCommand(sqlUpdate, connection))
                    {
                        command.Parameters.AddWithValue("@taskId", taskId);
                        int result = command.ExecuteNonQuery();
                        // Verifica se o comando afetou alguma linha
                        if (result == 0)
                        {
                            Console.WriteLine("Tarefa não encontrada ou já estava concluída.");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao completar a tarefa: {ex.Message}");
            }
            finally
            {
                mutex.ReleaseMutex();
            }
        }

        private string AllocateTask(string clientId)
        {
            mutex.WaitOne();
            string allocatedTaskId = null;
            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    // Primeiro, descobre qual é o serviço do cliente
                    string sqlSelectService = "SELECT ID_Servico FROM Cliente WHERE ID_Cliente = @clientId";
                    using (SqlCommand commandService = new SqlCommand(sqlSelectService, connection))
                    {
                        commandService.Parameters.AddWithValue("@clientId", clientId);
                        string serviceId = (string)commandService.ExecuteScalar();

                        if (!string.IsNullOrEmpty(serviceId))
                        {
                            // Agora, aloca a primeira tarefa não alocada do serviço encontrado
                            string sqlAllocateTask = @"
                        SELECT TOP 1 ID_Tarefa 
                        FROM Servicos 
                        WHERE Estado = 'Nao alocado' AND ID_Servico = @serviceId
                        ORDER BY NEWID()"; // Usando NEWID() para alocação aleatória

                            using (SqlCommand commandTask = new SqlCommand(sqlAllocateTask, connection))
                            {
                                commandTask.Parameters.AddWithValue("@serviceId", serviceId);
                                allocatedTaskId = (string)commandTask.ExecuteScalar();

                                if (!string.IsNullOrEmpty(allocatedTaskId))
                                {
                                    // Atualiza a tarefa com o ID do cliente para marcá-la como em curso
                                    string sqlUpdateTask = @"
                                UPDATE Servicos 
                                SET Estado = 'Em curso', ID_Cliente = @clientId 
                                WHERE ID_Tarefa = @taskId";

                                    using (SqlCommand commandUpdate = new SqlCommand(sqlUpdateTask, connection))
                                    {
                                        commandUpdate.Parameters.AddWithValue("@clientId", clientId);
                                        commandUpdate.Parameters.AddWithValue("@taskId", allocatedTaskId);
                                        commandUpdate.ExecuteNonQuery();
                                    }
                                }
                                else
                                {
                                    Console.WriteLine("Não há tarefas disponíveis para alocação.");
                                }
                            }
                        }
                        else
                        {
                            Console.WriteLine($"Nenhum serviço encontrado para o cliente: {clientId}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao alocar tarefa: {ex.Message}");
            }
            finally
            {
                mutex.ReleaseMutex();
            }
            return allocatedTaskId ?? "Erro ao alocar tarefa.";
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            int port = 12345; // Use the port you prefer
            Server server = new Server();
            server.StartServer(port);
        }
    }
}
