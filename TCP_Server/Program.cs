using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

class Server
{
    static Mutex mutex = new Mutex();

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

        byte[] data = new byte[1024];
        int bytes = stream.Read(data, 0, data.Length);
        string message = Encoding.ASCII.GetString(data, 0, bytes);
        Console.WriteLine("Cliente " + message);

        if (message == "conectado")
        {
            string responseMessage = "100 OK";
            byte[] responseData = Encoding.ASCII.GetBytes(responseMessage);
            stream.Write(responseData, 0, responseData.Length);

            data = new byte[1024];
            bytes = 0;

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
                    Console.WriteLine("ID do Cliente: " + message);

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
}
