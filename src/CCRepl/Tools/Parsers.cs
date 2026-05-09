using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Threading.Tasks;

namespace CCRepl.Tools
{
    public static class Parsers
    {
        public static bool Parse(string txt, out int v) => int.TryParse(txt, out v);
        public static bool Parse(string txt, out double v) => double.TryParse(txt, out v);
        public static bool Parse(string txt, out string v)
        {
            v = txt;
            return true;
        }
        public static bool Parse(string txt, out DateTime v) => DateTime.TryParse(txt, out v);
    }
}
