using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Text;

// State object for receiving data from remote device.  
public class StateObject
{
    // Client socket.  
    public Socket workSocket = null;
    // Size of receive buffer.  
    public const int BufferSize = 1024 * 1024;
    // Receive buffer.  
    public byte[] buffer = new byte[BufferSize];
    // Received data string.  
    public StringBuilder sb = new StringBuilder();
}

public class Web
{
    // The ip and port number for the remote device. 
    private static string ip = "3.16.165.168";
    private static int port = 8000;

    // ManualResetEvent instances signal completion.  
    private static ManualResetEvent connectDone =
        new ManualResetEvent(false);
    private static ManualResetEvent sendDone =
        new ManualResetEvent(false);
    private static ManualResetEvent receiveDone =
        new ManualResetEvent(false);

    // The response from the remote device.  
    private static string response = string.Empty;

    private static string r;
    public static string create(int n) => getResponse($"CREATE {n}");
    public static string create(int n, int x) => getResponse($"CREATEX {n},{x}");
    public static string join(int k, int x, string names, string par)
    {
        string ans = getResponse($"JOIN {k},{x},{names},{par}");
        int s = ans.Length - 1;
        if (ans[s]=='>')
        {
            int m = ans.IndexOf('<');
            r = ans.Substring(m + 1, s - m - 1);
        }
        return ans;
    }
    public static string rwait() => getResponse($"RWAIT {r}");
    public static string nam() => getResponse($"NAM {r}");
    public static string par() => getResponse($"PAR {r}");
    public static string set(string str) => getResponse($"SET {str},{r}");
    public static string get() => getResponse($"GET {r}");
    public static string wait() => getResponse($"WAIT {r}");
    public static string delete() => getResponse($"DELETE {r}");
    public static string clear() => getResponse($"CLEAR {r}");

    private static string getResponse(string str)
    {
        // Connect to a remote device.  
        try
        {
            // Establish the remote endpoint for the socket.
            //IPHostEntry ipHostInfo = Dns.GetHostEntry(Dns.GetHostName());
            //IPAddress ipAddress = ipHostInfo.AddressList[1];
            IPHostEntry ipHostInfo = Dns.GetHostEntry(ip);
            IPAddress ipAddress = ipHostInfo.AddressList[0];
            IPEndPoint remoteEP = new IPEndPoint(ipAddress, port);

            // Create a TCP/IP socket.  
            Socket client = new Socket(ipAddress.AddressFamily,
                SocketType.Stream, ProtocolType.Tcp);

            // Connect to the remote endpoint.  
            client.BeginConnect(remoteEP,
                new AsyncCallback(ConnectCallback), client);
            connectDone.WaitOne();

            // Send test data to the remote device.  
            Send(client, str);
            sendDone.WaitOne();

            // Receive the response from the remote device.  
            Receive(client);
            receiveDone.WaitOne();

            // Release the socket.  
            client.Shutdown(SocketShutdown.Both);
            client.Close();

            return response;
        }
        catch
        {
            return "-1";
        }
    }

    private static void ConnectCallback(IAsyncResult ar)
    {
        // Retrieve the socket from the state object.  
        Socket client = (Socket)ar.AsyncState;

        // Complete the connection.  
        client.EndConnect(ar);

        // Signal that the connection has been made.  
        connectDone.Set();
    }

    private static void Receive(Socket client)
    {
        // Create the state object.  
        StateObject state = new StateObject();
        state.workSocket = client;

        // Begin receiving the data from the remote device.  
        client.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,
            new AsyncCallback(ReceiveCallback), state);
    }

    private static void ReceiveCallback(IAsyncResult ar)
    {
        // Retrieve the state object and the client socket   
        // from the asynchronous state object.  
        StateObject state = (StateObject)ar.AsyncState;
        Socket client = state.workSocket;

        // Read data from the remote device.  
        int bytesRead = client.EndReceive(ar);

        if (bytesRead > 0)
        {
            // There might be more data, so store the data received so far.  
            state.sb.Append(Encoding.UTF8.GetString(state.buffer, 0, bytesRead));

            // Get the rest of the data.  
            client.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,
                new AsyncCallback(ReceiveCallback), state);
        }
        else
        {
            // All the data has arrived; put it in response.  
            if (state.sb.Length > 1)
            {
                response = state.sb.ToString();
            }
            // Signal that all bytes have been received.  
            receiveDone.Set();
        }
    }

    private static void Send(Socket client, String data)
    {
        // Convert the string data to byte data using ASCII encoding.  
        byte[] byteData = Encoding.UTF8.GetBytes(data);

        // Begin sending the data to the remote device.  
        client.BeginSend(byteData, 0, byteData.Length, 0,
            new AsyncCallback(SendCallback), client);
    }

    private static void SendCallback(IAsyncResult ar)
    {
        // Retrieve the socket from the state object.  
        Socket client = (Socket)ar.AsyncState;

        // Complete sending the data to the remote device.  
        int bytesSent = client.EndSend(ar);
        // Console.WriteLine("Sent {0} bytes to server.", bytesSent);

        // Signal that all bytes have been sent.  
        sendDone.Set();
    }

    public static int Main()
    {
        Console.WriteLine(create(2));
        Console.ReadLine();
        return 0;
    }
}