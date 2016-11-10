using System;
using System.Net;
using System.Net.Sockets;

namespace SnakeWPF
{
    class Server
    {
        private TcpListener tcpListener;
        private bool serverrunning = false;

        public Server()
        {
            Console.WriteLine("Server()");
            tcpListener = new TcpListener(IPAddress.Any, 25566);
            Listen();
        }

        public Server(int port)
        {
            Console.WriteLine("Server()");
            tcpListener = new TcpListener(IPAddress.Any, port);
            Listen();
        }

        public void StopServer()
        {
            serverrunning = false;
            tcpListener.Stop();
            if (client != null)
            {
                clientStream.Flush();
                clientStream.Close();
                client.Close();
            }
        }

        private async void Listen()
        {
            Console.WriteLine("Listen()");
            tcpListener.Start();
            serverrunning = true;
            while (serverrunning)
            {
                try
                {
                    client = await tcpListener.AcceptTcpClientAsync();
                    HandleClientComm(client);
                }
                catch (Exception e) { Console.WriteLine(e); }
            }
        }

        NetworkStream clientStream;
        TcpClient client;

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
