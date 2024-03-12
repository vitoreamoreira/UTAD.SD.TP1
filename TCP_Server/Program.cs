using System;
using System.Net;
using System.Net.Sockets;
using System.Text;

class Server
{
    static void Main()
    {
        TcpListener server = new TcpListener(IPAddress.Any, 13000);
        server.Start();
        Console.WriteLine("Servidor iniciado...");

        while (true)
        {
            TcpClient client = server.AcceptTcpClient();
            NetworkStream stream = client.GetStream();

            byte[] buffer = new byte[client.ReceiveBufferSize];
            int data = stream.Read(buffer, 0, client.ReceiveBufferSize);
            string clientMessage = Encoding.ASCII.GetString(buffer, 0, data);
            Console.WriteLine("Mensagem recebida: " + clientMessage);

            if (clientMessage.StartsWith("START"))
            {
                string idCliente = clientMessage.Split('|')[1];
                Console.WriteLine("ID do cliente: " + idCliente);
                byte[] message = Encoding.ASCII.GetBytes("100 OK");
                stream.Write(message, 0, message.Length);
            }
            else if (clientMessage == "QUIT")
            {
                byte[] message = Encoding.ASCII.GetBytes("400 BYE");
                stream.Write(message, 0, message.Length);
                client.Close();
                break;
            }
        }

        server.Stop();
    }
}
