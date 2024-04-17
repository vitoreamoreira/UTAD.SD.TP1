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

                    // Inicia a thread do cliente para lidar com interações subsequentes
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

            try
            {
                // Envia "100 OK" assim que a conexão for aceite
                writer.WriteLine("100 OK");

                string clientId = null;
                string line;
                while ((line = reader.ReadLine()) != null && !line.Equals("QUIT", StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine($"Received: {line}");

                    if (line.StartsWith("ID"))
                    {
                        clientId = line.Split(' ')[1];
                        string response = EnsureClientRegistered(clientId);
                        writer.WriteLine(response);
                    }
                    else if (line.ToLower().Equals("complete task"))
                    {
                        if (clientId == null)
                        {
                            writer.WriteLine("Erro: ID de cliente nao informado.");
                        }
                        else
                        {
                            string response = CompleteTask(clientId);
                            writer.WriteLine(response);
                        }
                    }
                    else if (line.ToLower().Equals("request task"))
                    {
                        if (clientId == null)
                        {
                            writer.WriteLine("Erro: ID de cliente nao informado.");
                        }
                        else
                        {
                            string response = AllocateTask(clientId);
                            writer.WriteLine(response);
                        }
                    }
                }
                writer.WriteLine("400 BYE");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception: {ex.Message}");
                writer.WriteLine("Erro: Ocorreu um problema no servidor.");
            }
            finally
            {
                // Certifica-se de que todos os recursos sejam libertados
                reader.Close();
                writer.Close();
                stream.Close();
                client.Close();
            }
        }

        private string EnsureClientRegistered(string clientId)
        {
            mutex.WaitOne();
            string response = "";
            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    string sql = "SELECT ID_Servico FROM Cliente WHERE ID_Cliente = @clientId";
                    using (SqlCommand cmd = new SqlCommand(sql, connection))
                    {
                        cmd.Parameters.AddWithValue("@clientId", clientId);
                        var serviceId = cmd.ExecuteScalar();

                        if (serviceId != null)
                        {
                            // O cliente já está registrado
                            response = $"O Cliente esta registado no Servico {serviceId}.";
                        }
                        else
                        {
                            // O cliente é novo, então regista e associa a um serviço
                            response = RegisterNewClient(clientId);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao verificar/registar cliente: {ex.Message}");
                response = "Erro ao tentar verificar/registar cliente.";
            }
            finally
            {
                mutex.ReleaseMutex();
            }
            return response;
        }


        private string RegisterNewClient(string clientId)
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                string serviceQuery = @"
                    SELECT TOP 1 ID_Servico 
                    FROM Servicos 
                    WHERE Estado = 'Nao alocado' 
                    GROUP BY ID_Servico 
                    ORDER BY COUNT(*) DESC";
                using (SqlCommand command = new SqlCommand(serviceQuery, connection))
                {
                    object serviceId = command.ExecuteScalar();
                    if (serviceId != null)
                    {
                        string insertQuery = @"
                            INSERT INTO Cliente (ID_Cliente, ID_Servico) 
                            VALUES (@clientId, @serviceId)";
                        using (SqlCommand insertCommand = new SqlCommand(insertQuery, connection))
                        {
                            insertCommand.Parameters.AddWithValue("@clientId", clientId);
                            insertCommand.Parameters.AddWithValue("@serviceId", serviceId);
                            insertCommand.ExecuteNonQuery();
                            return $"Novo cliente {clientId} registado no serviço {serviceId}.";
                        }
                    }
                    else
                    {
                        return "Nenhum servico disponivel para alocacao de novos clientes.";
                    }
                }
            }
        }
        private string CompleteTask(string clientId)
        {
            mutex.WaitOne();
            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    string sqlUpdate = @"
                UPDATE Servicos 
                SET Estado = 'Concluido', ID_Cliente = NULL 
                WHERE ID_Cliente = @clientId AND Estado = 'Em curso'";
                    using (SqlCommand command = new SqlCommand(sqlUpdate, connection))
                    {
                        command.Parameters.AddWithValue("@clientId", clientId);
                        int result = command.ExecuteNonQuery();
                        if (result > 0)
                        {
                            return "Sua tarefa foi concluida com sucesso.";
                        }
                        else
                        {
                            return "Voce nao tem tarefas em curso ou ja foram concluidas.";
                        }
                    }
                }
            }
            finally
            {
                mutex.ReleaseMutex();
            }
        }

        private string AllocateTask(string clientId)
        {
            mutex.WaitOne();
            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    // Verificar se o cliente já tem uma tarefa em curso
                    string checkTaskQuery = "SELECT ID_Tarefa FROM Servicos WHERE ID_Cliente = @clientId AND Estado = 'Em curso'";
                    using (SqlCommand checkTaskCommand = new SqlCommand(checkTaskQuery, connection))
                    {
                        checkTaskCommand.Parameters.AddWithValue("@clientId", clientId);
                        object existingTaskId = checkTaskCommand.ExecuteScalar();
                        if (existingTaskId != null)
                        {
                            return $"Voce ja esta alocado na tarefa {existingTaskId} e deve conclui-la antes de receber uma nova.";
                        }
                    }

                    // Continua a lógica para alocar uma nova tarefa ao cliente que não tem tarefa em curso.
                    // Primeiro, obtemos o serviço do cliente.
                    string getServiceQuery = "SELECT ID_Servico FROM Cliente WHERE ID_Cliente = @clientId";
                    using (SqlCommand getServiceCommand = new SqlCommand(getServiceQuery, connection))
                    {
                        getServiceCommand.Parameters.AddWithValue("@clientId", clientId);
                        var serviceId = getServiceCommand.ExecuteScalar();
                        if (serviceId == null)
                        {
                            return "Erro: Cliente nao registado em nenhum serviço.";
                        }

                        // Buscamos uma tarefa não alocada nesse serviço específico.
                        string getTaskQuery = @"
                    SELECT TOP 1 ID_Tarefa, Descricao 
                    FROM Servicos 
                    WHERE Estado = 'Nao alocado' AND ID_Servico = @serviceId
                    ORDER BY NEWID()";
                        using (SqlCommand getTaskCommand = new SqlCommand(getTaskQuery, connection))
                        {
                            getTaskCommand.Parameters.AddWithValue("@serviceId", serviceId);
                            using (SqlDataReader taskReader = getTaskCommand.ExecuteReader())
                            {
                                if (taskReader.Read())
                                {
                                    string taskId = taskReader["ID_Tarefa"].ToString();
                                    string taskDescription = taskReader["Descricao"].ToString();

                                    taskReader.Close();
                                    string updateTaskQuery = @"
                                UPDATE Servicos 
                                SET Estado = 'Em curso', ID_Cliente = @clientId 
                                WHERE ID_Tarefa = @taskId";
                                    using (SqlCommand updateTaskCommand = new SqlCommand(updateTaskQuery, connection))
                                    {
                                        updateTaskCommand.Parameters.AddWithValue("@clientId", clientId);
                                        updateTaskCommand.Parameters.AddWithValue("@taskId", taskId);
                                        updateTaskCommand.ExecuteNonQuery();
                                    }
                                    return $"Tarefa {taskId} - {taskDescription} foi alocada para você.";
                                }
                                else
                                {
                                    return "Nenhuma tarefa disponível para o seu serviço.";
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return $"Erro ao alocar tarefa: {ex.Message}";
            }
            finally
            {
                mutex.ReleaseMutex();
            }
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            int port = 12345;
            Server server = new Server();
            server.StartServer(port);
        }
    }
}