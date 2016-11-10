using System;
using System.Net.Sockets;

namespace SnakeWPF
{
    class Client
    {
        public Client()
        {
            runTcpClient("localhost", 25566);
        }

        public Client(string IP, int Port)
        {
            runTcpClient(IP, Port);
        }

        public Client(string IP)
        {
            runTcpClient(IP, 25566);
        }

        public void StopClient()
        {
            if (clientStream != null)
            {
                clientStream.Flush();
                clientStream.Close();
            }
            client.Close();
        }

        NetworkStream clientStream;
        TcpClient client;

        private async void runTcpClient(string IP, int PORT)
        {
            try
            {
                client = new TcpClient();
                await client.ConnectAsync(IP, PORT);
                Console.WriteLine("Connected to:" + IP + ":" + PORT.ToString());
                HandleClientComm(client);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        public void SendMessage(byte[] message)
        {
            if (clientStream != null && clientStream.CanWrite)
            {
                clientStream.Write(message, 0, message.Length);
                clientStream.Flush();
            }
        }

        private async void HandleClientComm(object client)
        {
            Console.WriteLine("HandleClientComm(" + client.ToString() + ")");

            Console.WriteLine("Client connected");
            OnConnectionChange(this, true);

            TcpClient tcpClient = (TcpClient)client;
            clientStream = tcpClient.GetStream();
            
            byte[] message;

            int bytesRead;

            while (true)
            {
                try
                {
                    message = new byte[4];
                    bytesRead = await clientStream.ReadAsync(message, 0, message.Length);
                    clientStream.Flush();
                    if (bytesRead > 0)
                        OnDataReceived(this, message);
                    else break;
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                    break;
                }
            }

            tcpClient.Close();
            OnConnectionChange(this, false);
            Console.WriteLine("Client closed");
            Console.Write("\n");

            clientStream.Flush();
            clientStream.Close();
        }

        public delegate void DataReceivedHandler(object sender, byte[] data);
        public event DataReceivedHandler OnDataReceived;

        public delegate void OnConnectionChangedHandler(object sender, bool connected);
        public event OnConnectionChangedHandler OnConnectionChange;
    }
}
