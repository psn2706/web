using UnityEngine;
class Test
{
    static void wait()
    {
        while (Web.res == "") ;
    }
    public static void test_message()
    {
        Web.create(1);
        wait();
        Debug.Log(Web.res);

        Web.join(1, Web.room, "nam", "par");
        wait();
        Debug.Log(Web.res);

        Web.rwait();
        wait();
        Debug.Log(Web.res);

        string str = "Привет, Андрей!\nТебе нравится мой тест?\a";
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
    public static void delete_all(int count = 100)
    {
        for (int i = 0; i < count; ++i)
        {
            Web.delete(i);
            wait();
        }
    }
}