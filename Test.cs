using UnityEngine;
class Test
{
    static void wait()
    {
        while (Web.res == "") ;
    }
    static void clear()
    {
        Web.res = "";
    }
    public static void test_message()
    {

        Web.room = 0;

        Web.create(1, Web.room);
        wait(); clear();

        Web.join(1, Web.room, "nam", "par");
        wait(); clear();

        Web.rwait();
        wait(); clear();

        Web.set("hi!");
        wait(); clear();

        Web.get();
        wait();
        Debug.Log(Web.res);
        clear();

        Web.delete();
        wait(); clear();

    }

    /// <summary>
    /// It takes 50 sec
    /// </summary>
    /// <param name="count"></param>
    public static void delete_all(int count = 100)
    {
        for (int i=0; i<count; ++i)
        {
            Web.delete(i);
            wait(); clear();
        }
    }
}