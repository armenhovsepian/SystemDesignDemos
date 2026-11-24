using System.Text;

namespace UrlShortenerDemo.Services
{
    public static class Base62
    {
        private const string Chars = "0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ";

        public static string Encode(long id)
        {
            if (id == 0) return "0";

            var sb = new StringBuilder();
            while (id > 0)
            {
                sb.Insert(0, Chars[(int)(id % 62)]);
                id /= 62;
            }
            return sb.ToString();
        }

        public static long Decode(string code)
            => code.Aggregate(0L, (acc, c) => acc * 62 + Chars.IndexOf(c));
    }
}
