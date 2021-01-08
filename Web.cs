using System;
using System.Threading;
using System.Collections.Generic;
using Renci.SshNet;
class Web
{
    static SshClient client;
    public static string room;
    public static int[] index;
    /// <summary>
    /// Инициализация веб-клиента. Установка соединения с сервером.
    /// </summary>
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
            // Напиши "Проверьте подключение к интернету и нажмите ОК", после нажатия ОК:
            // return Init();
        }
    }
    static string RunCommand(string str)
    {
        try
        {
            string ans = client.RunCommand(str).Result;
            Thread.Sleep(220);
            return ans;
        }
        catch
        {
            // Напиши "Проверьте подключение к интернету и нажмите ОК", после нажатия ОК:
            // return RunCommand(str);
        }
        return "-1";
    }
    /// <summary>
    /// Создание комнаты.
    /// </summary>
    /// <param name="Room">Название комнаты.</param>
    /// <param name="RoomPlayers">Вместимость комнаты.</param>
    /// <returns></returns>
    public static string CreateRoom(string Room, int RoomPlayers)
    {
        string spec = "\\/:?\"*<>|";
        foreach (char c in spec)
            if (Room.Contains(c.ToString()))
                return $"Неверный формат ввода, не используйте знаки {spec}";

        if (RunCommand($"if [ -e rooms/{Room}/ ]; then echo -n 'Exists'; fi") == "Exists")
            return "Комната с таким названием уже существует";
        else
        {
            string
                cmd1 = $"mkdir rooms/{Room}/",
                cmd2 = $"echo -n '0' > rooms/{Room}/index",
                cmd3 = $"echo -n 'Free' > rooms/{Room}/roomStatus",
                cmd4 = $"echo -n '{RoomPlayers}' > rooms/{Room}/roomPlayers";

            RunCommand($"{cmd1} && {cmd2} && {cmd3} && {cmd4}");

            return "Комната успешно создана!";
        }
    }
    /// <summary>
    /// Инициализация устройства в комнату.
    /// </summary>
    /// <param name="Room">Название комнаты.</param>
    /// <param name="Players">Количество игроков с данного устройства.</param>
    /// <param name="Parameters">Начальные параметры игры.</param>
    /// <param name="Names">Имена людей.</param>
    /// <returns></returns>
    public static string InitRoom(string Room, int Players, string Parameters, string Names)
    {
        if (Players <= 0)
            return "Некорректное значение количества игроков";

        if (RunCommand($"if [ -e rooms/{Room}/ ]; then echo -n 'Exists'; fi") != "Exists")
            return "Комната не найдена";

        if (RunCommand($"cat rooms/{Room}/roomStatus") == "Running")
            return "В этой комнате идёт игра";

        int Playnow = PlayersNow(Room), Playroom = Convert.ToInt32(RunCommand($"cat rooms/{Room}/roomPlayers"));

        if (Playnow + Players > Playroom)
            return "Эта комната уже заполнена";

        room = Room;
        index = new int[Players];
        int x = Convert.ToInt32(RunCommand($"cat rooms/{Room}/index"));

        for (int i = 0; i < Players; ++i, ++x) index[i] = x;

        RunCommand($"echo -n '{x}' > rooms/{Room}/index");

        if (index[0] == 0) RunCommand($"echo -n '{Parameters}' > rooms/{Room}/parameters");

        RunCommand($"echo -n '{Names}' >> rooms/{Room}/names");

        if (Playnow + Players == Playroom)
            RunRoom(Room);

        return $"Вы присоединились к комнате {room}";
    }
    /// <summary>
    /// Передача строки для хранения на сервере, используется после инициализации устройства в комнату.
    /// </summary>
    /// <param name="str">Строка для сохранения на сервере.</param>
    public static void Write(string str)
    {
        RunCommand($"echo -n '{str}' > rooms/{room}/file");
    }
    /// <summary>
    /// Получение строки, соответсвующей последнему использованию Write().
    /// </summary>
    /// <returns></returns>
    public static string Read()
    {
        return RunCommand($"cat rooms/{room}/file");
    }
    public static bool IsRoomFull()
    {
        return RunCommand($"cat rooms/{room}/roomStatus") == "Running";
    }
    /// <summary>
    /// Очищение комнаты после её использования.
    /// </summary>
    public static void ClearRoom()
    {
        string
                cmd1 = $"echo -n '0' > rooms/{room}/index",
                cmd2 = $"rm rooms/{room}/names",
                cmd3 = $"rm rooms/{room}/parameters",
                cmd4 = $"echo -n 'Free' > rooms/{room}/roomStatus";

        RunCommand($"if [ -e rooms/{room}/ ]; then {cmd1} && {cmd2} && {cmd3} && {cmd4}; fi");
    }
    /// <summary>
    /// Удаление комнаты после её использования.
    /// </summary>
    public static void DeleteRoom()
    {
        if (room != "") RunCommand($"if [ -e rooms/{room}/ ]; then rm -r rooms/{room}/; fi");
    }
    /// <summary>
    /// Удаление комнаты по названию.
    /// </summary>
    public static void DeleteRoom(string Room)
    {
        if (room != "") RunCommand($"if [ -e rooms/{Room}/ ]; then rm -r rooms/{Room}/; fi");
    }
    /// <summary>
    /// Количество игроков в комнате.
    /// </summary>
    /// <param name="Room">Название комнаты.</param>
    /// <returns></returns>
    public static int PlayersNow(string Room)
    {
        return Convert.ToInt32(RunCommand($"cat rooms/{Room}/index"));
    }
    /// <summary>
    /// Вместимость комнаты.
    /// </summary>
    /// <param name="Room">Название комнаты.</param>
    /// <returns></returns>
    public static int RoomPlayers(string Room)
    {
        return Convert.ToInt32(RunCommand($"cat rooms/{Room}/roomPlayers")); ;
    }
    /// <summary>
    /// Параметры игры в комнате.
    /// </summary>
    /// <returns></returns>
    public static string Parameters()
    {
        return RunCommand($"cat rooms/{room}/parameters");
    }
    /// <summary>
    /// Имена людей в комнате.
    /// </summary>
    /// <returns></returns>
    public static string Names()
    {
        return RunCommand($"cat rooms/{room}/names");
    }
    static void RunRoom(string Room)
    {
        RunCommand($"echo -n 'Running' > rooms/{Room}/roomStatus");
    }
}