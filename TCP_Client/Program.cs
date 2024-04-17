using System;
using System.IO;
using System.Net.Sockets;
using System.Text;

class Client
{
    public void StartClient(string serverIP, int port)
    {
        using TcpClient client = new TcpClient(serverIP, port);
        using NetworkStream stream = client.GetStream();
        using StreamReader reader = new StreamReader(stream, Encoding.ASCII);
        using StreamWriter writer = new StreamWriter(stream, Encoding.ASCII) { AutoFlush = true };

        // Recebe resposta inicial do servidor
        string serverResponse = reader.ReadLine();
        Console.WriteLine($"Server says: {serverResponse}");

        if (serverResponse.StartsWith("100 OK"))
        {
            // Enviar ID após confirmar que a conexão foi estabelecida
            Console.Write("Enter your Client ID: ");
            string clientId = Console.ReadLine();
            writer.WriteLine($"ID {clientId}");

            // Espera a resposta do servidor sobre o estado do cliente
            serverResponse = reader.ReadLine();
            Console.WriteLine($"Server says: {serverResponse}");
        }

        // Main loop cliente
        string userInput;
        do
        {
            Console.Write("Enter command (COMPLETE TASK, REQUEST TASK, QUIT to exit): ");
            userInput = Console.ReadLine();
            writer.WriteLine(userInput);

            if (!userInput.Equals("QUIT", StringComparison.OrdinalIgnoreCase))
            {
                serverResponse = reader.ReadLine();
                Console.WriteLine($"Server says: {serverResponse}");
            }
        } while (!userInput.Equals("QUIT", StringComparison.OrdinalIgnoreCase));

        // Resposta final do servidor
        serverResponse = reader.ReadLine();
        Console.WriteLine($"Server says: {serverResponse}");
    }

    static void Main(string[] args)
    {
        Console.Write("Enter the server IP address: ");
        string serverIP = Console.ReadLine();
        int port = 12345; // A mesma porta usada no servidor

        Client client = new Client();
        client.StartClient(serverIP, port);
    }
}
