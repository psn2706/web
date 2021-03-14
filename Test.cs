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

        Web.join(1, Web.room, "nam", "par");
        wait();

        Web.rwait();
        wait();

        Web.set("hi!");
        wait();

        Web.get();
        wait();
        Debug.Log(Web.res);
        
        Web.delete();
        wait();
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