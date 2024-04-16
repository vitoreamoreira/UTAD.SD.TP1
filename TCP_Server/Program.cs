using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using ExcelDataReader;

class Server
{
    static Mutex mutex = new Mutex();
    //static string excelFilePath = @"Alocacao_Cliente_Servico.csv"; //arquivo CSV com as tarefas

    static void Main()
    {
        TcpListener server = new TcpListener(IPAddress.Parse("127.0.0.1"), 8888);
        server.Start();
        Console.WriteLine("Servidor iniciado...");

        while (true)
        {
            TcpClient client = server.AcceptTcpClient();
            Thread clientThread = new Thread(() => HandleClient(client));
            clientThread.Start();
        }
    }

    static void HandleClient(TcpClient client)
    {
        NetworkStream stream = client.GetStream();
        StreamReader reader = new StreamReader(stream);
        StreamWriter writer = new StreamWriter(stream);

        byte[] data = new byte[1024];
        int bytes = stream.Read(data, 0, data.Length);
        string message = Encoding.ASCII.GetString(data, 0, bytes);
        Console.WriteLine("Cliente " + message);

        if (message == "conectado")
        {
            string responseMessage = "100 OK";
            byte[] responseData = Encoding.ASCII.GetBytes(responseMessage);
            stream.Write(responseData, 0, responseData.Length);

            // Recebendo o ID do cliente
            bytes = stream.Read(data, 0, data.Length);
            string clientID = Encoding.ASCII.GetString(data, 0, bytes);
            Console.WriteLine("ID do Cliente: " + clientID);

            // Loop para ler os dados até que a conexão seja fechada pelo cliente
            while (true)
            {
                try
                {
                    bytes = stream.Read(data, 0, data.Length);
                    if (bytes == 0)
                    {
                        // Se não houver mais dados para ler, encerra o loop
                        break;
                    }
                    message = Encoding.ASCII.GetString(data, 0, bytes);

                    //if (message.ToUpper() == "ALOCACAO TAREFA")
                    //{
                    //    Console.WriteLine("Cliente solicitou uma tarefa.");
                    //    // Ler a tarefa correspondente do arquivo csv
                    //    string taskResponse = GetTaskFromCSV(clientID, excelFilePath);
                    //    if (taskResponse != null)
                    //    {
                    //        // Enviar a tarefa para o cliente
                    //        byte[] taskData = Encoding.ASCII.GetBytes(taskResponse);
                    //        stream.Write(taskData, 0, taskData.Length);
                    //}
                        //else
                        //{
                        //    // Se não houver tarefa correspondente, envie uma mensagem indicando isso
                        //    string noTaskMessage = "Não há tarefa atribuída para este cliente.";
                        //    byte[] noTaskData = Encoding.ASCII.GetBytes(noTaskMessage);
                        //    stream.Write(noTaskData, 0, noTaskData.Length);
                        //}
                    ////}

                    if (message.ToUpper() == "QUIT")
                    {
                        Console.WriteLine("Cliente solicitou encerramento da conexão.");
                        string quitResponse = "400 BYE";
                        byte[] quitData = Encoding.ASCII.GetBytes(quitResponse);
                        stream.Write(quitData, 0, quitData.Length);
                        break; // Encerra o loop e fecha a conexão
                    }
                }
                catch (IOException)
                {
                    // Se ocorrer uma exceção de E/S, encerra o loop
                    break;
                }
            }
        }

        stream.Close();
        client.Close();
    }

    static string GetTaskFromCSV(string clientID, string filePath)
    {
        try
        {
            using (var reader = new StreamReader(filePath))
            {
                while (!reader.EndOfStream)
                {
                    string line = reader.ReadLine();
                    string[] fields = line.Split(',');
                    if (fields.Length >= 2 && fields[0] == clientID)
                    {
                        return fields[1];
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Erro ao ler o arquivo CSV: " + ex.Message);
        }

        return null;
    }
}
