using UnityEngine;
class Test
{
    public static void test_message()
    {
        Web.room = 0;
        Web.create(1, Web.room);
        Web.join(1, Web.room, "nam", "par");
        Web.rwait();
        Web.set("hi!");
        Debug.Log(Web.get());
        Web.delete();
    }
}