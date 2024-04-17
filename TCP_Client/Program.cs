using System;
using System.IO;
using System.Net.Sockets;
using System.Text;

class Client
{
    public void StartClient(string serverIP, int port)
    {
        Console.Write("Enter your Client ID: ");
        string clientId = Console.ReadLine();

        using TcpClient client = new TcpClient(serverIP, port);
        using NetworkStream stream = client.GetStream();
        using StreamReader reader = new StreamReader(stream, Encoding.ASCII);
        using StreamWriter writer = new StreamWriter(stream, Encoding.ASCII) { AutoFlush = true };

        try
        {
            // Receiving the initial server response
            string serverResponse = reader.ReadLine();
            Console.WriteLine($"Server says: {serverResponse}");

            // Sending client ID to server
            writer.WriteLine($"ID {clientId}");

            // Main client loop
            string userInput;
            do
            {
                Console.WriteLine("Enter command (COMPLETE TASK, REQUEST TASK, QUIT to exit): ");
                userInput = Console.ReadLine();
                writer.WriteLine(userInput);

                if (userInput != "QUIT")
                {
                    serverResponse = reader.ReadLine();
                    Console.WriteLine($"Server says: {serverResponse}");
                }
            } while (userInput != "QUIT");

            // Expecting the server's final response
            serverResponse = reader.ReadLine();
            Console.WriteLine($"Server says: {serverResponse}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Exception: {ex.Message}");
        }
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
