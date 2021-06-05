using System;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace mySocket
{
    class Program
    {
        public class ClientObj
        {
            public string ip;
            public TcpClient client;
            public int summ;
            public ushort index;
            public ClientObj(TcpClient tcpClient, ushort indexClient)
            {
                client = tcpClient;
                ip = ((IPEndPoint)tcpClient.Client.RemoteEndPoint).Address.ToString();
                summ = 0;
                index = indexClient;
            }

            public void Process()
            {
                NetworkStream stream = null;
                try
                {
                    stream = client.GetStream();
                    byte[] data = new byte[128]; // буфер для получения и отправки данных
                    string message = null;

                    data = Encoding.UTF8.GetBytes("Hallo, Client!\r\n"); // отправляем приветствие
                    stream.Write(data, 0, data.Length);

                    while (true)
                    {
                        do
                        {
                            StringBuilder builder = new StringBuilder();
                            int bytes = 0;
                            do
                            {
                                bytes = stream.Read(data, 0, data.Length);
                                builder.Append(Encoding.UTF8.GetString(data, 0, bytes));

                            }
                            while (stream.DataAvailable);
                            message += builder.ToString();
                        }
                        while (!message.Contains('\r'));

                        Console.WriteLine("Client (" + index + ") message - " + message);

                        int number;
                        if (int.TryParse(message, out number))
                        {
                            summ += number;
                            message = summ.ToString() + "\r\n";
                        }
                        else
                        {
                            if(message.Contains("exit"))
                            {
                                break;
                            }
                            else
                            {
                                if (message.Contains("list"))
                                    message = clientslist();
                                else
                                    message = "Error! (Enter a number or one of the commands: list, exit)\r\n";
                            }
                                
                        }

                        data = Encoding.UTF8.GetBytes(message);
                        message = null;
                        stream.Write(data, 0, data.Length);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
                finally
                {
                    
                    if (stream != null)
                        stream.Close();
                    if (client != null)
                        client.Close();
                    remClients(index); // удаление клиента
                    Console.WriteLine("Client (" + index + ") has been disconnected");
                }
            }

            ~ClientObj()
            {
                Console.WriteLine("Destructor for Client (" + index + ")!");
            }
        }

        static public void remClients (ushort id)
        {
            for(ushort i=id; i < count-1; i++)
            {
                Clients[i] = Clients[i + 1];
                Clients[i].index = i;
            }

            Clients[count] = null;
            GC.Collect();
            count--;
        }

        static public string clientslist()
        {
            string str = null;

            for (ushort i = 0; i < count; i++)
            {
                str += " IP - " + Clients[i].ip + "  summ = " + Clients[i].summ.ToString() + "\r\n";
            }

            return str;
        }

        static TcpListener listener;
        static ushort count;
        static ClientObj[] Clients = new ClientObj[16];
        static void Main(string[] args)
        {

            if (args.Length == 0)
            {
                Console.WriteLine("Argument needed!");
                Console.ReadLine();
                return;
            }

            ushort port;

            if (!ushort.TryParse(args[0], out port))
            {
                Console.WriteLine("Invalid argument!");
                return;
            }

            count = 0;
            try
            {
                listener = new TcpListener(IPAddress.Parse("127.0.0.1"), port);
                listener.Start();
                Console.WriteLine("Waiting for connections...");

                while (true)
                {
                    TcpClient client = listener.AcceptTcpClient();
                    Clients[count] = new ClientObj(client,count);

                    //новый поток для обслуживания нового клиента
                    Thread clientThread = new Thread(new ThreadStart(Clients[count].Process));

                    clientThread.Start();
                    
                    Console.WriteLine("New client (" + count + ")");
                    count++;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            finally
            {
                if (listener != null)
                    listener.Stop();
            }
        }
    }
}
