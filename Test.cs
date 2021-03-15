using UnityEngine;
class Test
{
    static void wait()
    {
        var watch = System.Diagnostics.Stopwatch.StartNew();
        while (Web.res == "")
            if (watch.ElapsedMilliseconds >= 10000)
                Web.res = "-1";
        ;
    }
    public static void try_divide_by_zero()
    {
        try
        {
            int x = 0, y;
            y = 1 / x;
        }
        catch
        {
            Debug.Log("Divide by zero!");
        }
    }
    public static void test_message()
    {
        try
        {
            Web.create(1, 0);
            wait();
            Debug.Log(Web.res);

            Web.join(1, Web.room, "nam", "par");
            wait();
            Debug.Log(Web.res);

            Web.rwait();
            wait();
            Debug.Log(Web.res);

            string str = "Привет, Андрей!\nТебе нравится мой тест?";
            Web.set(str);
            wait();
            Debug.Log(Web.res);

            Web.get();
            wait();
            Debug.Log(Web.res);

            Web.delete();
            wait();
            Debug.Log(Web.res);
        }
        catch { Debug.Log("ops"); }
    }
    public static void delete_all(int count = 100)
    {
        try
        {
            for (int i = 0; i < count; ++i)
            {
                Web.delete(i);
                wait();
            }
        }
        catch { Debug.Log("ops"); }
    }
}