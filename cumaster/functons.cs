using System;

namespace global
{
    public static class funcs
    {
        public static Boolean IsInRange(int x,int l,int h)
        {
            return x >= l && x <= h;
        }

        public static Boolean IsInRange(float x, float l, float h)
        {
            return x >= l && x <= h;
        }

        public static String WhichWebsite(String url)
        {
            if (url.IndexOf("forum.gamer.com.tw") >= 0)
                return "BH";
            else if (url.IndexOf("docs.google.com") >= 0)
                return "GO";
            else
                return "?";
        }
    }
}