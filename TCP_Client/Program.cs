using System;
using System.Net.Sockets;
using System.Text;

class Client
{
    static void Main()
    {
        Console.Write("Insira o endereço IP do servidor: ");
        string serverIP = Console.ReadLine();

        TcpClient client = new TcpClient(serverIP, 13000);
        NetworkStream stream = client.GetStream();

        Console.Write("Insira o seu ID: ");
        string id = Console.ReadLine();
        byte[] message = Encoding.ASCII.GetBytes("START|" + id);
        stream.Write(message, 0, message.Length);

        byte[] buffer = new byte[client.ReceiveBufferSize];
        int data = stream.Read(buffer, 0, client.ReceiveBufferSize);
        string serverMessage = Encoding.ASCII.GetString(buffer, 0, data);
        Console.WriteLine("Mensagem recebida: " + serverMessage);

        if (serverMessage == "100 OK")
        {
            Console.WriteLine("Comunicação iniciada com sucesso!");
        }

        while (true)
        {
            Console.WriteLine("Pressione 'Q' para sair ou 'C' para continuar...");
            var key = Console.ReadKey();
            if (key.KeyChar == 'Q' || key.KeyChar == 'q')
            {
                message = Encoding.ASCII.GetBytes("QUIT");
                stream.Write(message, 0, message.Length);
                break;
            }
        }

        client.Close();
    }
}