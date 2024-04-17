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
        // Ouvinte TCP para aceitar conexões de clientes
        private TcpListener listener;
        // Mutex para controlar o acesso a recursos compartilhados entre threads
        private static Mutex mutex = new Mutex();
        // String de conexão para a base de dados SQL Server
        private string connectionString = "Server=MEEP_P\\SQLEXPRESS;Database=Sistemas_Distribuidos;Integrated Security=True;";

        // Método que inicia o servidor para escutar por conexões entrantes
        public void StartServer(int port)
        {
            // Inicializa o TcpListener para escutar em qualquer endereço IP e na porta especificada
            listener = new TcpListener(IPAddress.Any, port);
            listener.Start();
            Console.WriteLine("Servidor iniciado...");

            try
            {
                // Loop infinito para aceitar e processar conexões de clientes
                while (true)
                {
                    // Aceita uma conexão de cliente pendente
                    TcpClient client = listener.AcceptTcpClient();

                    // Inicia uma thread separada para cada cliente, permitindo múltiplas conexões
                    Thread clientThread = new Thread(new ParameterizedThreadStart(HandleClient));
                    clientThread.Start(client);
                }
            }
            finally
            {
                listener.Stop(); // Fecha o ouvinte quando o servidor termina
            }
        }

        // Método que gerencia a comunicação com o cliente conectado
        private void HandleClient(object clientObj)
        {
            // Converte o objeto passado para TcpClient
            TcpClient client = (TcpClient)clientObj;
            // Obtém o fluxo de rede associado ao cliente para enviar e receber dados
            NetworkStream stream = client.GetStream();
            // Lê dados do fluxo de rede
            StreamReader reader = new StreamReader(stream, Encoding.ASCII);
            // Escreve dados no fluxo de rede
            StreamWriter writer = new StreamWriter(stream, Encoding.ASCII) { AutoFlush = true };

            try
            {
                writer.WriteLine("100 OK");  // Confirmação inicial de conexão

                string clientId = null; // Declara a variável que armazenará o ID do cliente
                string line; // Declara a variável que armazenará as linhas de texto recebidas

                // Loop para ler linhas de texto enviadas pelo cliente
                while ((line = reader.ReadLine()) != null && !line.Equals("SAIR", StringComparison.OrdinalIgnoreCase))
                {
                    // Imprime a linha recebida na consola do servidor
                    Console.WriteLine($"Recebeu: {line}");

                    // Trata a linha de acordo com o seu conteúdo (comandos)
                    // Se começar com "ID", tratar a identificação do cliente
                    if (line.StartsWith("ID"))
                    {
                        // Extrai o ID do cliente da linha
                        clientId = line.Split(' ')[1];
                        // Verifica e/ou regista o cliente, retornando uma resposta apropriada
                        string response = EnsureClientRegistered(clientId);
                        writer.WriteLine(response);// Envia a resposta ao cliente
                    }
                    // Se for "completar tarefa", invoca a lógica de completar a tarefa
                    else if (line.ToLower().Equals("completar tarefa"))
                    {
                        if (clientId == null)
                        {
                            writer.WriteLine("Erro: ID de cliente não informado.");
                        }
                        else
                        {
                            // Completa a tarefa do cliente e retorna uma resposta
                            string response = CompleteTask(clientId);
                            writer.WriteLine(response);
                        }
                    }
                    // Se for "requisitar tarefa", invoca a lógica de alocar uma nova tarefa
                    else if (line.ToLower().Equals("requisitar tarefa"))
                    {
                        if (clientId == null)
                        {
                            writer.WriteLine("Erro: ID de cliente não informado.");
                        }
                        else
                        {
                            // Aloca uma nova tarefa ao cliente e retorna uma resposta
                            string response = AllocateTask(clientId);
                            writer.WriteLine(response);
                        }
                    }
                }
                // Se o comando for "SAIR", imprime na consola do servidor e envia resposta ao cliente
                if (line != null && line.Equals("SAIR", StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine($"Recebeu: QUIT");
                    Console.WriteLine($"O cliente {clientId} solicitou desconexão.");
                    writer.WriteLine("400 BYE");  // Resposta ao comando QUIT
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception: {ex.Message}");
                writer.WriteLine("Erro: Ocorreu um problema no servidor.");
            }
            finally
            {
                // Fecha todos os recursos de rede associados ao cliente
                reader.Close();
                writer.Close();
                stream.Close();
                client.Close();
            }
        }

        // Método que verifica se o cliente está registado e, se não, regista-o
        // Retorna uma string que contém o resultado da verificação ou do registo
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
                            response = $"O cliente esta registado no Servico {serviceId}.";
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

        // Método que regista um novo cliente na base de dados
        // Retorna uma string que contém o resultado do registo
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
                            return $"Novo cliente ID:{clientId} registado no servico {serviceId}.";
                        }
                    }
                    else
                    {
                        return "Nenhum servico disponivel para alocacao de novos clientes.";
                    }
                }
            }
        }

        // Método que completa a tarefa do cliente e atualiza o seu estado na base de dados
        // Retorna uma string que contém o resultado da operação
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
                            return "O cliente nao tem tarefas em curso ou ja foram concluidas.";
                        }
                    }
                }
            }
            finally
            {
                mutex.ReleaseMutex();
            }
        }

        // Método que aloca uma nova tarefa ao cliente
        // Retorna uma string que contém o resultado da alocação
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
                            return $"O cliente ja esta alocado na tarefa {existingTaskId} e deve conclui-la antes de receber uma nova.";
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
                            return "Erro: Cliente nao registado em nenhum servico.";
                        }

                        // Procuramos uma tarefa não alocada nesse serviço específico.
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
                                    return $"A tarefa {taskId} - {taskDescription} foi alocada ao cliente.";
                                }
                                else
                                {
                                    return "Nenhuma tarefa disponivel para o seu serviço.";
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

    // Ponto de entrada principal do programa
    class Program
    {
        static void Main(string[] args)
        {
            int port = 12345; // Porta definida para a comunicação TCP
            // Instanciação do servidor e início da escuta por conexões
            Server server = new Server();
            server.StartServer(port);
        }
    }
}