using System;
using System.Net.Sockets;
using System.Text;

class Client
{
    static void Main()
    {
        Console.Write("Por favor, insira o endereço IP do servidor: ");
        string serverIP = Console.ReadLine();

        TcpClient client = new TcpClient(serverIP, 8888);
        NetworkStream stream = client.GetStream();

        string message = "conectado";
        byte[] data = Encoding.ASCII.GetBytes(message);
        stream.Write(data, 0, data.Length);

        data = new byte[1024];
        int bytes = stream.Read(data, 0, data.Length);
        string responseMessage = Encoding.ASCII.GetString(data, 0, bytes);
        Console.WriteLine("Resposta do Servidor: " + responseMessage);

        string clientID = "";
        bool hasID = false;

        while (true)
        {
            if (!hasID)
            {
                Console.Write("Por favor, insira o seu ID: ");
                clientID = Console.ReadLine();
                byte[] idData = Encoding.ASCII.GetBytes(clientID);
                stream.Write(idData, 0, idData.Length);
                hasID = true;
            }
            else
            {
                Console.Write("Digite 'QUIT' para sair: ");
                string input = Console.ReadLine();
                byte[] inputData = Encoding.ASCII.GetBytes(input);
                stream.Write(inputData, 0, inputData.Length);

                if (input.ToUpper() == "QUIT")
                {
                    // Receber e exibir a resposta do servidor
                    data = new byte[1024];
                    bytes = stream.Read(data, 0, data.Length);
                    string quitResponse = Encoding.ASCII.GetString(data, 0, bytes);
                    Console.WriteLine("Resposta do servidor: " + quitResponse);
                    break;
                }
            }
        }

        stream.Close();
        client.Close();
    }
}
