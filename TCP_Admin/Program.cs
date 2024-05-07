using System;
using System.IO;
using System.Net.Sockets;
using System.Text;

class Admin
{
    // Método para iniciar o administrador e gerenciar a comunicação com o servidor
    public void StartAdmin(string serverIP, int port)
    {
        // Estabelece uma conexão TCP com o servidor no endereço IP e porta especificados
        using TcpClient client = new TcpClient(serverIP, port);
        // Cria um fluxo para ler e escrever dados na conexão
        using NetworkStream stream = client.GetStream();
        // Leitor para facilitar a leitura de strings do fluxo
        using StreamReader reader = new StreamReader(stream, Encoding.ASCII);
        // Escritor para facilitar a escrita de strings no fluxo
        using StreamWriter writer = new StreamWriter(stream, Encoding.ASCII) { AutoFlush = true };

        // Aguarda a resposta inicial do servidor após a conexão
        string serverResponse = reader.ReadLine();
        Console.WriteLine($"SERVIDOR DIZ: {serverResponse}");

        // Confirmação de conexão do servidor
        if (serverResponse.StartsWith("100 OK"))
        {
            // Envia o ID após confirmar que a conexão foi estabelecida
            Console.Write("Introduza o seu ID de cliente: ");
            string clientId = Console.ReadLine();
            writer.WriteLine($"ID {clientId}");

            // Aguarda a resposta do servidor quanto ao registo do cliente
            serverResponse = reader.ReadLine();
            Console.WriteLine($"SERVIDOR DIZ: {serverResponse}");
        }

        // Loop principal para interação do cliente com o menu de comandos
        string userInput;
        string command = string.Empty;
        do
        {
            Console.WriteLine("\nMenu:");
            Console.WriteLine("1 - Serviço");
            Console.WriteLine("2 - Criar Tarefa");
            Console.WriteLine("9 - Sair");
            Console.Write("Introduza o valor ou comando: ");
            userInput = Console.ReadLine();

            // Normaliza a entrada do utulizador para permitir a comparação insensível a maiúsculas e minúsculas
            string upperInput = userInput.ToUpper();

            // Decodifica a entrada do utilizador, seja um número de menu ou um comando completo
            switch (upperInput)
            {
                case "1":
                case "SERVICO":
                    command = "SERVICO";
                    break;
                case "2":
                case "CRIAR TAREFA":
                    command = "CRIAR TAREFA";
                    break;
                case "9":
                case "SAIR":
                    command = "SAIR";
                    break;
                default:
                    Console.WriteLine("Valor ou comando inválido. Por favor tente novamente.");
                    continue;
            }
            // Envia o comando ao servidor
            writer.WriteLine(command);
            // Se o comando não for para sair, espera e imprime a resposta do servidor
            if (!command.Equals("SAIR", StringComparison.OrdinalIgnoreCase))
            {
                serverResponse = reader.ReadLine();
                Console.WriteLine($"SERVIDOR DIZ: {serverResponse}");
            }
        } while (!command.Equals("SAIR", StringComparison.OrdinalIgnoreCase));

        // Após receber o comando "SAIR", aguarda a mensagem final de confirmação do servidor
        if (command.Equals("SAIR", StringComparison.OrdinalIgnoreCase))
        {
            serverResponse = reader.ReadLine();
            Console.WriteLine($"SERVIDOR DIZ: {serverResponse}");
            Console.WriteLine("Conexao Terminada - Pressione qualquer tecla para encerrar...");
            Console.ReadKey();  // Pausa antes de fechar, permitindo ao utilizador ver a mensagem antes de fechar
        }
    }

    // Ponto de entrada principal do cliente
    static void Main(string[] args)
    {
        // Solicita ao utilizador o endereço IP do servidor para conexão
        Console.Write("Introduza o endereco IP do servidor: ");
        string serverIP = Console.ReadLine();
        int port = 12345; // A mesma porta usada no servidor

        // Cria uma instância do cliente e inicia a interação com o servidor
        Admin administrador = new Admin();
        administrador.StartAdmin(serverIP, port);
    }
}
