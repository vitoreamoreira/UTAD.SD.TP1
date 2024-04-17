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
        string command = string.Empty;

        do
        {
            Console.WriteLine("\nMenu:");
            Console.WriteLine("1 - Request Task");
            Console.WriteLine("2 - Complete Task");
            Console.WriteLine("9 - Quit");
            Console.Write("Enter your choice or command: ");
            userInput = Console.ReadLine();

            // Converte a entrada para maiúsculas para a comparação
            string upperInput = userInput.ToUpper();

            // Traduzir números para comandos ou usar entrada diretamente se for um comando.
            switch (upperInput)
            {
                case "1":
                    command = "REQUEST TASK";
                    break;
                case "2":
                    command = "COMPLETE TASK";
                    break;
                case "9":
                case "QUIT":
                    command = "QUIT";
                    break;
                case "REQUEST TASK":
                case "COMPLETE TASK":
                    command = upperInput;
                    break;
                default:
                    Console.WriteLine("Escolha ou comando invalido. Por favor tente novamente.");
                    continue;
            }

            writer.WriteLine(command);

            if (!command.Equals("QUIT", StringComparison.OrdinalIgnoreCase))
            {
                serverResponse = reader.ReadLine();
                Console.WriteLine($"Server says: {serverResponse}");
            }
        } while (!command.Equals("QUIT", StringComparison.OrdinalIgnoreCase));

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
