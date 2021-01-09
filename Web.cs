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
    public static string Init()
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
            return "0";
        }
        catch { return "!con"; }
    }
    static string RunCommand(string str)
    {
        try
        {
            string ans = client.RunCommand(str).Result;
            Thread.Sleep(220);
            return ans;
        }
        catch { return "!con"; }
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

        string res = RunCommand($"if [ -e rooms/{Room}/ ]; then echo -n 'Exists'; fi"); if (res == "!con") return res;
        if (res == "Exists")
            return "Комната с таким названием уже существует";
        else
        {
            string
                cmd1 = $"mkdir rooms/{Room}/",
                cmd2 = $"echo -n '0' > rooms/{Room}/index",
                cmd3 = $"echo -n 'Free' > rooms/{Room}/roomStatus",
                cmd4 = $"echo -n '{RoomPlayers}' > rooms/{Room}/roomPlayers";

            res = RunCommand($"{cmd1} && {cmd2} && {cmd3} && {cmd4}"); if (res == "!con") return res;

            return "Комната успешно создана!";
        }
    }
    static int Playnow = 0, Playroom = 0;
    /// <summary>
    /// Инициализация устройства в комнату.
    /// </summary>
    /// <param name="Room">Название комнаты.</param>
    /// <param name="Players">Количество игроков с данного устройства.</param>
    /// <param name="Parameters">Начальные параметры игры.</param>
    /// <param name="Names">Имена людей.</param>
    /// <returns></returns>
    public static string InitRoom(string Room, int Players, string Parameters, string Names, string error = "0")
    {
        switch (error)
        {
            case "0":
                string res, res1, res2;
                if (Players <= 0)
                    return "Некорректное значение количества игроков";

                goto case "!exists";
            case "!exists":

                res = RunCommand($"if [ -e rooms/{Room}/ ]; then echo -n 'Exists'; fi"); if (res == "!con") return "!exists";
                if (res != "Exists")
                    return "Комната не найдена";

                goto case "!running";
            case "!running":

                res = RunCommand($"cat rooms/{Room}/roomStatus"); if (res == "!con") return "!running";
                if (res == "Running")
                    return "В этой комнате идёт игра";

                goto case "!roomplayers";
            case "!roomplayers":

                res1 = PlayersNow(Room);
                res2 = RunCommand($"cat rooms/{Room}/roomPlayers");
                if (res1 == "!con" || res2 == "!con") return "!roomplayers";
                Playnow = Convert.ToInt32(res1); Playroom = Convert.ToInt32(res2);

                if (Playnow + Players > Playroom)
                    return "Эта комната уже заполнена";

                goto case "!getindex";
            case "!getindex":

                index = new int[Players];
                res = RunCommand($"cat rooms/{Room}/index"); if (res == "!con") return "!getindex";
                int x = Convert.ToInt32(res);
                for (int i = 0; i < Players; ++i, ++x) index[i] = x;

                goto case "!newindex";
            case "!newindex":

                res = RunCommand($"echo -n '{index[Players - 1] + 1}' > rooms/{Room}/index"); if (res == "!con") return "!newindex";

                goto case "!parameters";
            case "!parameters":

                if (index[0] == 0)
                {
                    res = RunCommand($"echo -n '{Parameters}' > rooms/{Room}/parameters"); if (res == "!con") return "!parameters";
                }

                goto case "!names";
            case "!names":

                res = RunCommand($"echo -n '{Names}' >> rooms/{Room}/names"); if (res == "!con") return "!names";

                goto case "!run";
            case "!run":

                if (Playnow + Players == Playroom)
                {
                    res = RunRoom(Room); if (res == "!con") return "!run";
                }

                goto default;
            default:
                room = Room;
                return $"Вы присоединились к комнате {room}";
        }



    }
    /// <summary>
    /// Передача строки для хранения на сервере, используется после инициализации устройства в комнату.
    /// </summary>
    /// <param name="str">Строка для сохранения на сервере.</param>
    public static string Write(string str)
    {
        return RunCommand($"echo -n '{str}' > rooms/{room}/file");
    }
    /// <summary>
    /// Получение строки, соответсвующей последнему использованию Write().
    /// </summary>
    /// <returns></returns>
    public static string Read()
    {
        return RunCommand($"cat rooms/{room}/file");
    }
    /// <summary>
    /// Возвращает "true", если комната полна, "false", если нет; "!con", если нет интернета.
    /// </summary>
    /// <returns></returns>
    public static string IsRoomFull()
    {
        string res = RunCommand($"cat rooms/{room}/roomStatus"); if (res == "!con") return res;
        return RunCommand($"cat rooms/{room}/roomStatus") == "Running" ? "true" : "false";
    }
    /// <summary>
    /// Очищение комнаты после её использования.
    /// </summary>
    public static string ClearRoom()
    {
        string
                cmd1 = $"echo -n '0' > rooms/{room}/index",
                cmd2 = $"rm rooms/{room}/names",
                cmd3 = $"rm rooms/{room}/parameters",
                cmd4 = $"echo -n 'Free' > rooms/{room}/roomStatus";

        return RunCommand($"if [ -e rooms/{room}/ ]; then {cmd1} && {cmd2} && {cmd3} && {cmd4}; fi");
    }
    /// <summary>
    /// Удаление комнаты после её использования.
    /// </summary>
    public static string DeleteRoom()
    {
        return room != "" ? RunCommand($"if [ -e rooms/{room}/ ]; then rm -r rooms/{room}/; fi") : "Не указана комната";
    }
    /// <summary>
    /// Удаление комнаты по названию.
    /// </summary>
    public static string DeleteRoom(string Room)
    {
        return room != "" ? RunCommand($"if [ -e rooms/{room}/ ]; then rm -r rooms/{Room}/; fi") : "Не указана комната";
    }
    /// <summary>
    /// Количество игроков в комнате.
    /// </summary>
    /// <param name="Room">Название комнаты.</param>
    /// <returns></returns>
    public static string PlayersNow(string Room)
    {
        return RunCommand($"cat rooms/{Room}/index");
    }
    /// <summary>
    /// Вместимость комнаты.
    /// </summary>
    /// <param name="Room">Название комнаты.</param>
    /// <returns></returns>
    public static string RoomPlayers(string Room)
    {
        return RunCommand($"cat rooms/{Room}/roomPlayers");
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
    static string RunRoom(string Room)
    {
        return RunCommand($"echo -n 'Running' > rooms/{Room}/roomStatus");
    }
}