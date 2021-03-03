// State object for receiving data from remote device.
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
    private static string ip = "3.16.165.168";
    private static int port = 8000;

    // ManualResetEvent instances signal completion.  
    private static System.Threading.ManualResetEvent connectDone;
    private static System.Threading.ManualResetEvent sendDone;
    private static System.Threading.ManualResetEvent receiveDone;

    // The response from the remote device.  
    private static string response = string.Empty;

    public static int room;
    public static string create(int n) => getResponse($"CREATE {n}");
    public static string create(int n, int x) => getResponse($"CREATEX {n},{x}");
    public static string join(int k, int x, string nam, string par)
    {
        string ans=getResponse($"JOIN {k},{x},{nam},{par}");
        if (ans[0] >= '0' && ans[0] <= '9') room = x;
        return ans;
    }
    public static string rwait() => getResponse($"RWAIT {room}");
    public static string nam() => getResponse($"NAM {room}");
    public static string par() => getResponse($"PAR {room}");
    public static string set(string str) => getResponse($"SET {str},{room}");
    public static string get() => getResponse($"GET {room}");
    public static string wait() => getResponse($"WAIT {room}");
    public static string delete() => getResponse($"DELETE {room}");
    public static string delete(int x) => getResponse($"DELETE {x}");
    public static string clear() => getResponse($"CLEAR {room}");
    public static string clear(int x) => getResponse($"CLEAR {x}");

    private static string getResponse(string str)
    {
        connectDone = new System.Threading.ManualResetEvent(false);
        sendDone = new System.Threading.ManualResetEvent(false);
        receiveDone = new System.Threading.ManualResetEvent(false);
        // Connect to a remote device.  
        try
        {
            // Establish the remote endpoint for the socket.
            //IPHostEntry ipHostInfo = Dns.GetHostEntry(Dns.GetHostName());
            //IPAddress ipAddress = ipHostInfo.AddressList[1];
            //IPEndPoint remoteEP = new IPEndPoint(ipAddress, port);
            System.Net.IPHostEntry ipHostInfo = System.Net.Dns.GetHostEntry(ip);
            System.Net.IPAddress ipAddress = ipHostInfo.AddressList[0];
            System.Net.IPEndPoint remoteEP = new System.Net.IPEndPoint(ipAddress, port);

            // Create a TCP/IP socket.  
            System.Net.Sockets.Socket client = new System.Net.Sockets.Socket(ipAddress.AddressFamily,
                System.Net.Sockets.SocketType.Stream, System.Net.Sockets.ProtocolType.Tcp);

            // Connect to the remote endpoint.  
            client.BeginConnect(remoteEP,
                new System.AsyncCallback(ConnectCallback), client);
            connectDone.WaitOne();

            // Send test data to the remote device.  
            Send(client, str);
            sendDone.WaitOne();

            // Receive the response from the remote device.  
            Receive(client);
            receiveDone.WaitOne();

            // Release the socket.  
            client.Shutdown(System.Net.Sockets.SocketShutdown.Both);
            client.Close();

            return response;
        }
        catch { return "-1"; }
    }

    private static void ConnectCallback(System.IAsyncResult ar)
    {
        // Retrieve the socket from the state object.  
        System.Net.Sockets.Socket client = (System.Net.Sockets.Socket)ar.AsyncState;

        // Complete the connection.  
        client.EndConnect(ar);

        // Signal that the connection has been made.  
        connectDone.Set();
    }

    private static void Receive(System.Net.Sockets.Socket client)
    {
        // Create the state object.  
        StateObject state = new StateObject();
        state.workSocket = client;

        // Begin receiving the data from the remote device.  
        client.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,
            new System.AsyncCallback(ReceiveCallback), state);
    }

    private static void ReceiveCallback(System.IAsyncResult ar)
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

    private static void Send(System.Net.Sockets.Socket client, string data)
    {
        // Convert the string data to byte data using ASCII encoding.  
        byte[] byteData = System.Text.Encoding.UTF8.GetBytes(data);

        // Begin sending the data to the remote device.  
        client.BeginSend(byteData, 0, byteData.Length, 0,
            new System.AsyncCallback(SendCallback), client);
    }

    private static void SendCallback(System.IAsyncResult ar)
    {
        // Retrieve the socket from the state object.  
        System.Net.Sockets.Socket client = (System.Net.Sockets.Socket)ar.AsyncState;

        // Complete sending the data to the remote device.  
        int bytesSent = client.EndSend(ar);

        // Signal that all bytes have been sent.  
        sendDone.Set();
    }
}