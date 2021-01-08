using System;
using System.Threading;
using System.Collections.Generic;
using Renci.SshNet;
class Web
{
    public static SshClient client;
    public static string room;
    public static int[] index;
    public static void Init()
    {
        try
        {
            string ip = "52.14.170.138", name = "ec2-user";
            var pk = new PrivateKeyFile("Assets/WebClient/key2.cer");
            var keyFiles = new[] { pk };

            var methods = new List<AuthenticationMethod>();
            methods.Add(new PrivateKeyAuthenticationMethod(name, keyFiles));

            var con = new ConnectionInfo(ip, name, methods.ToArray());
            client = new SshClient(con);
            client.Connect();
        }
        catch
        {
            // Напиши "Проверьте подключение к интернету и нажмите ОК", перезапусти Init() после нажатия ОК
        }
    }
    public static string CreateRoom(string Room="room", int RoomPlayers=1)
    {
        string spec = "\\/:?\"*<>|";
        foreach (char c in spec)
            if (Room.Contains(c.ToString()))
                return $"Неверный формат ввода, не используйте знаки {spec}";

        string res = client.RunCommand($"if [ -e rooms/{Room}/ ]; then echo 'Exists'; fi").Result;
        Thread.Sleep(220);
        if (res == "Exists\n")
            return "Комната с таким названием уже существует";
        else
        {
            string
                cmd1 = $"mkdir rooms/{Room}/",
                cmd2 = $"echo '0' > rooms/{Room}/index",
                cmd3 = $"echo 'Free' > rooms/{Room}/roomStatus",
                cmd4 = $"echo '{RoomPlayers}' > rooms/{Room}/roomPlayers";

            client.RunCommand($"{cmd1} && {cmd2} && {cmd3} && {cmd4}");
            Thread.Sleep(220);
            return "Комната успешно создана!";
        }
    }
    public static void InitRoom(string Room="room", int Players=1, string Parameters = "", string Names = "")
    {
        try
        {
            room = Room;

            string res = client.RunCommand($"if [ -e rooms/{Room}/ ]; then echo 'Exists'; fi").Result;
            Thread.Sleep(220);
            if (res != "Exists\n")
                throw new Exception("Комната не найдена");

            res = client.RunCommand($"cat rooms/{Room}/roomStatus").Result;
            Thread.Sleep(220);
            if (res == "Running\n")
                throw new Exception("В этой комнате идёт игра");

            res = client.RunCommand($"cat rooms/{Room}/roomPlayers").Result;
            Thread.Sleep(220);
            int pnow = PlayersNow(), rpl= Convert.ToInt32(res);
            if (pnow + Players > rpl)
                throw new Exception("В этой комнате не поместится столько игроков");

            if (Players <= 0)
                throw new Exception("Некорректное значение количества игроков");

            index = new int[Players];
            int x = Convert.ToInt32(client.RunCommand($"cat rooms/{room}/index").Result);
            for (int i = 0; i < Players; ++i)
            {
                index[i] = x;
                ++x;
            }
            client.RunCommand($"echo '{x}' > rooms/{room}/index");
            Thread.Sleep(220);

            if (index[0] == 0)
            {
                client.RunCommand($"echo '{Parameters}' > rooms/{room}/parameters");
                Thread.Sleep(220);
            }
            
            client.RunCommand($"echo -n '{Names}' >> rooms/{room}/names");
            Thread.Sleep(220);

            if (pnow + Players == rpl)
                RunRoom();
        }
        catch (Exception e)
        {
            if (e.Message == "Комната не найдена") ;
            else if (e.Message == "В этой комнате идёт игра") ;
            else if (e.Message == "В этой комнате не поместится столько игроков") ;
            else if (e.Message == "Некорректное значение количества игроков") ;
            else; // Напиши "Проверьте подключение к интернету и нажмите ОК", перезапусти InitRoom() после нажатия ОК
        }
    }
    public static void Write(string str)
    {
        try { client.RunCommand($"echo '{str}' > rooms/{room}/file"); Thread.Sleep(220); }
        catch
        {
            // Напиши "Проверьте подключение к интернету и нажмите ОК", перезапусти Write() после нажатия ОК
        }
    }
    public static string Read()
    {
        try
        {
            string str = client.RunCommand($"cat rooms/{room}/file").Result; Thread.Sleep(220);
            return str.Substring(0, str.Length - 1);
        }
        catch
        {
            // Напиши "Проверьте подключение к интернету и нажмите ОК", перезапусти Read() после нажатия ОК
        }
        return "\0";
    }
    public static bool IsRoomFull()
    {
        try { bool stat = client.RunCommand($"cat rooms/{room}/roomStatus").Result == "Running\n"; Thread.Sleep(220); return stat; }
        catch
        {
            // Напиши "Проверьте подключение к интернету и нажмите ОК", перезапусти IsRoomFull() после нажатия ОК
        }
        return false;
    }
    public static void ClearRoom()
    {
        string
                cmd1 = $"echo '0' > rooms/{room}/index",
                cmd2 = $"echo 'Free' > rooms/{room}/roomStatus",
                cmd3 = $"rm rooms/{room}/names";

        client.RunCommand($"if [ -e rooms/{room}/ ]; then {cmd1} && {cmd2} && {cmd3}; fi"); Thread.Sleep(220);
    }
    public static void DeleteRoom()
    {
        client.RunCommand($"if [ -e rooms/{room}/ ]; then rm -r rooms/{room}/; fi"); Thread.Sleep(220);
    }
    public static void DeleteRoom(string Room)
    {
        client.RunCommand($"if [ -e rooms/{Room}/ ]; then rm -r rooms/{Room}/; fi"); Thread.Sleep(220);
    }
    public static int PlayersNow()
    {
        try { 
            int x = Convert.ToInt32(client.RunCommand($"cat rooms/{room}/index").Result); Thread.Sleep(220); 
            return x; }
        catch { }
        return 0;
    }
    public static int RoomPlayers(string Room)
    {
        try { int x = Convert.ToInt32(client.RunCommand($"cat rooms/{Room}/roomPlayers").Result); Thread.Sleep(220); return x; }
        catch { }
        return 0;
    }
    public static void RunRoom()
    {
        client.RunCommand($"echo 'Running' > rooms/{room}/roomStatus");
        Thread.Sleep(220);
    }
    public static string Parameters()
    {
        try
        {
            string str = client.RunCommand($"cat rooms/{room}/parameters").Result;
            Thread.Sleep(220);
            return str.Substring(0, str.Length - 1);
        }
        catch
        {
            // Напиши "Проверьте подключение к интернету и нажмите ОК", перезапусти Parameters() после нажатия ОК
        }
        return "\0";
    }
    public static string Names()
    {
        try
        {
            string str = client.RunCommand($"cat rooms/{room}/names").Result;
            Thread.Sleep(220);
            return str.Substring(0, str.Length - 1);
        }
        catch
        {
            // Напиши "Проверьте подключение к интернету и нажмите ОК", перезапусти Names() после нажатия ОК
        }
        return "\0";
    }
    public static int _Main()
    {
        Init();
        Console.WriteLine(CreateRoom("m", 3));
        InitRoom("m", 3);
        Write("d|");
        Console.WriteLine(Read()=="d|");
        return 0;
    }
}