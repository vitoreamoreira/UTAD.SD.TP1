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
        Console.WriteLine($"SERVIDOR DIZ: {serverResponse}");

        if (serverResponse.StartsWith("100 OK"))
        {
            // Enviar ID após confirmar que a conexão foi estabelecida
            Console.Write("Introduza o seu ID de cliente: ");
            string clientId = Console.ReadLine();
            writer.WriteLine($"ID {clientId}");

            // Espera a resposta do servidor sobre o estado do cliente
            serverResponse = reader.ReadLine();
            Console.WriteLine($"SERVIDOR DIZ: {serverResponse}");
        }

        // Main loop cliente
        string userInput;
        string command = string.Empty;
        do
        {
            Console.WriteLine("\nMenu:");
            Console.WriteLine("1 - Requisitar Tarefa");
            Console.WriteLine("2 - Completar Tarefa");
            Console.WriteLine("9 - Sair");
            Console.Write("Introduza o valor ou comando: ");
            userInput = Console.ReadLine();

            // Converte a entrada para maiúsculas para a comparação
            string upperInput = userInput.ToUpper();

            // Traduzir números para comandos ou usar entrada diretamente se for um comando.
            switch (upperInput)
            {
                case "1":
                case "REQUISITAR TAREFA":
                    command = "REQUISITAR TAREFA";
                    break;
                case "2":
                case "COMPLETAR TAREFA":
                    command = "COMPLETAR TAREFA";
                    break;
                case "9":
                case "SAIR":
                    command = "SAIR";
                    break;
                default:
                    Console.WriteLine("Valor ou comando inválido. Por favor tente novamente.");
                    continue;
            }

            writer.WriteLine(command);

            if (!command.Equals("SAIR", StringComparison.OrdinalIgnoreCase))
            {
                serverResponse = reader.ReadLine();
                Console.WriteLine($"SERVIDOR DIZ: {serverResponse}");
            }
        } while (!command.Equals("SAIR", StringComparison.OrdinalIgnoreCase));

        // Resposta final do servidor após "QUIT"
        if (command.Equals("SAIR", StringComparison.OrdinalIgnoreCase))
        {
            serverResponse = reader.ReadLine();
            Console.WriteLine($"SERVIDOR DIZ: {serverResponse}");
            Console.WriteLine("Conexao Terminada - Pressione qualquer tecla para encerrar...");
            Console.ReadKey();  // Pausa antes de fechar, permitindo ao utilizador ver a mensagem
        }
    }

    static void Main(string[] args)
    {
        Console.Write("Introduza o endereco IP do servidor: ");
        string serverIP = Console.ReadLine();
        int port = 12345; // A mesma porta usada no servidor

        Client client = new Client();
        client.StartClient(serverIP, port);
    }
}
