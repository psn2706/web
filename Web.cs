﻿// State object for receiving data from remote device.
public class StateObject
{
    // Client socket.  
    public System.Net.Sockets.Socket workSocket = null;
    // Size of receive buffer.  
    public const int BufferSize = 1024 * 1024;
    // Receive buffer.  
    public byte[] buffer = new byte[BufferSize];
    // Received data string.  
    public System.Text.StringBuilder sb = new System.Text.StringBuilder();
}
public class Web
{
    // The ip and port number for the remote device. 
    private const string ip = "3.16.165.168";
    private const int port = 8000;
    private const bool online = true;

    // ManualResetEvent instances signal completion.  
    private static System.Threading.ManualResetEvent connectDone;
    private static System.Threading.ManualResetEvent sendDone;
    private static System.Threading.ManualResetEvent receiveDone;

    // Establish the remote endpoint for the socket.
    private static System.Net.IPHostEntry ipHostInfo;
    private static System.Net.IPAddress ipAddress;
    private static System.Net.IPEndPoint remoteEP;

    // The response from the remote device.  
    private static string response = string.Empty;

    // Sync threads
    private static object locker = new object();

    private static System.Threading.Thread thread =
        new System.Threading.Thread(new System.Threading.ParameterizedThreadStart(get));

    private static System.Threading.Thread thread_timer =
        new System.Threading.Thread(new System.Threading.ParameterizedThreadStart(timer));

    private const int tick = 5000;

    // Separate requests
    private const string sep = "#";

    public static string res = string.Empty;
    public static int room = 0;
    public static int index = 0;
    public static void create(int n) => getResponse($"CREATE{sep}{n}");
    public static void create(int n, int x)
    {
        getResponse($"CREATEX{sep}{n}{sep}{x}");
        room = x;
    }
    public static void join(int k, int x, string nam, string par)
    {
        getResponse($"JOIN{sep}{k}{sep}{x}{sep}{nam}{sep}{par}");
        room = x;
    }
    public static void rwait() => getResponse($"RWAIT{sep}{room}");
    public static void names() => getResponse($"NAMES{sep}{room}");
    public static void parms() => getResponse($"PARMS{sep}{room}");
    public static void delete() => getResponse($"DELETE{sep}{room}");
    public static void delete(int x) => getResponse($"DELETE{sep}{x}");
    public static void clear() => getResponse($"CLEAR{sep}{room}");
    public static void clear(int x) => getResponse($"CLEAR{sep}{x}");
    public static void set(string str) => getResponse($"SET{sep}{room}{sep}{str}");
    public static void get() => getResponse($"GET{sep}{room}");
    public static void wait() => getResponse($"WAIT{sep}{room}{sep}{index}");
    private static void getResponse(string str)
    {
        try
        {
            res = "";
            if (thread_timer.IsAlive) thread_timer.Abort();
            thread = new System.Threading.Thread(new System.Threading.ParameterizedThreadStart(get));
            thread_timer = new System.Threading.Thread(new System.Threading.ParameterizedThreadStart(timer));
            thread.Start(str);
            thread_timer.Start(thread);
        }
        catch
        {
            res = "thread problem";
        }
    }
    private static void timer(object th)
    {
        System.Threading.Thread.Sleep(tick);
        if (thread.IsAlive && thread.Equals(th))
        {
            thread.Abort();
            res = "-1";
        }
    }
    private static void get(object str)
    {
        lock (locker)
        {
            try
            {
                response = string.Empty;

                // Connect to a remote device. 
                connectDone = new System.Threading.ManualResetEvent(false);
                sendDone = new System.Threading.ManualResetEvent(false);
                receiveDone = new System.Threading.ManualResetEvent(false);

                // Establish the remote endpoint for the socket.
                #pragma warning disable CS0162 // Обнаружен недостижимый код
                if (online)
                {
                    ipHostInfo = System.Net.Dns.GetHostEntry(ip);
                    ipAddress = ipHostInfo.AddressList[0];
                    remoteEP = new System.Net.IPEndPoint(ipAddress, port);
                }
                else
                {
                    ipHostInfo = System.Net.Dns.GetHostEntry(System.Net.Dns.GetHostName());
                    ipAddress = ipHostInfo.AddressList[1];
                    remoteEP = new System.Net.IPEndPoint(ipAddress, port);
                }
                #pragma warning restore CS0162 // Обнаружен недостижимый код


                // Create a TCP/IP socket.  
                System.Net.Sockets.Socket client = new System.Net.Sockets.Socket(ipAddress.AddressFamily,
                    System.Net.Sockets.SocketType.Stream, System.Net.Sockets.ProtocolType.Tcp);

                // Connect to the remote endpoint.  
                client.BeginConnect(remoteEP,
                    new System.AsyncCallback(ConnectCallback), client);
                connectDone.WaitOne();
                if (res == "-1")
                    return;

                // Send test data to the remote device.  
                Send(client, (string)str);
                sendDone.WaitOne();

                // Receive the response from the remote device.  
                Receive(client);
                receiveDone.WaitOne();

                // Release the socket.  
                client.Shutdown(System.Net.Sockets.SocketShutdown.Both);
                client.Close();

                if (response == string.Empty)
                {
                    res = "-1";
                    return;
                }

                string s = (string)str;

                if (s.StartsWith($"CREATE{sep}"))
                {
                    string[] split = response.Split();
                    int len = split.Length;
                    int.TryParse(split[len - 1], out room);
                }
                else if (s.StartsWith($"JOIN{sep}"))
                    int.TryParse(response, out index);

                res = response;
            }
            catch { res = "-1"; }
        }
    }
    private static void ConnectCallback(System.IAsyncResult ar)
    {
        try
        {
            // Retrieve the socket from the state object.  
            System.Net.Sockets.Socket client = (System.Net.Sockets.Socket)ar.AsyncState;

            // Complete the connection.  
            client.EndConnect(ar);

            // Signal that the connection has been made.  
            connectDone.Set();
        }
        catch { res = "-1"; }
    }

    private static void Receive(System.Net.Sockets.Socket client)
    {
        try
        {
            // Create the state object.  
            StateObject state = new StateObject();
            state.workSocket = client;

            // Begin receiving the data from the remote device.  
            client.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,
                new System.AsyncCallback(ReceiveCallback), state);
        }
        catch { res = "-1"; }
    }

    private static void ReceiveCallback(System.IAsyncResult ar)
    {
        try
        {
            // Retrieve the state object and the client socket   
            // from the asynchronous state object.  
            StateObject state = (StateObject)ar.AsyncState;
            System.Net.Sockets.Socket client = state.workSocket;

            // Read data from the remote device.  
            int bytesRead = client.EndReceive(ar);

            if (bytesRead > 0)
            {
                // There might be more data, so store the data received so far.  
                state.sb.Append(System.Text.Encoding.UTF8.GetString(state.buffer, 0, bytesRead));

                // Get the rest of the data.  
                client.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,
                    new System.AsyncCallback(ReceiveCallback), state);
            }
            else
            {
                // All the data has arrived; put it in response.  
                response = state.sb.ToString();

                // Signal that all bytes have been received.  
                receiveDone.Set();
            }
        }
        catch { res = "-1"; }
    }

    private static void Send(System.Net.Sockets.Socket client, string data)
    {
        try
        {
            // Convert the string data to byte data using ASCII encoding.  
            byte[] byteData = System.Text.Encoding.UTF8.GetBytes(data);

            // Begin sending the data to the remote device.  
            client.BeginSend(byteData, 0, byteData.Length, 0,
                new System.AsyncCallback(SendCallback), client);
        }
        catch { res = "-1"; }
    }

    private static void SendCallback(System.IAsyncResult ar)
    {
        try
        {
            // Retrieve the socket from the state object.  
            System.Net.Sockets.Socket client = (System.Net.Sockets.Socket)ar.AsyncState;

            // Complete sending the data to the remote device.  
            int bytesSent = client.EndSend(ar);

            // Signal that all bytes have been sent.  
            sendDone.Set();
        }
        catch { res = "-1"; }
    }
}