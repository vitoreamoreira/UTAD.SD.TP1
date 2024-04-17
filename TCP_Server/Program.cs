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
                            // O cliente já está registrado.
                            response = $"O Cliente esta registado no Servico {serviceId}.";
                        }
                        else
                        {
                            // O cliente é novo, então registre e associe a um serviço.
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
                            return $"Novo cliente {clientId} registrado no serviço {serviceId}.";
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
                    string checkTask = "SELECT ID_Tarefa FROM Servicos WHERE ID_Cliente = @clientId AND Estado = 'Em curso'";
                    using (SqlCommand cmdCheck = new SqlCommand(checkTask, connection))
                    {
                        cmdCheck.Parameters.AddWithValue("@clientId", clientId);
                        object existingTaskId = cmdCheck.ExecuteScalar();
                        if (existingTaskId != null)
                        {
                            return $"Voce ja esta alocado a tarefa {existingTaskId} e deve conclui-la antes de receber uma nova.";
                        }
                    }

                    // Alocar uma nova tarefa
                    string sql = @"
                SELECT TOP 1 ID_Tarefa, Descricao 
                FROM Servicos 
                WHERE Estado = 'Nao alocado' 
                ORDER BY NEWID()";
                    using (SqlCommand command = new SqlCommand(sql, connection))
                    {
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                string taskId = reader["ID_Tarefa"].ToString();
                                string description = reader["Descricao"].ToString();

                                // Marcar a tarefa como em curso
                                reader.Close();
                                string updateTask = @"
                            UPDATE Servicos 
                            SET Estado = 'Em curso', ID_Cliente = @clientId 
                            WHERE ID_Tarefa = @taskId";
                                using (SqlCommand updateCommand = new SqlCommand(updateTask, connection))
                                {
                                    updateCommand.Parameters.AddWithValue("@clientId", clientId);
                                    updateCommand.Parameters.AddWithValue("@taskId", taskId);
                                    updateCommand.ExecuteNonQuery();
                                }
                                return $"Tarefa alocada: {taskId} - {description}";
                            }
                            else
                            {
                                return "Nenhuma tarefa disponivel para alocacao.";
                            }
                        }
                    }
                }
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
            int port = 12345; // Ajuste a porta conforme necessário
            Server server = new Server();
            server.StartServer(port);
        }
    }
}