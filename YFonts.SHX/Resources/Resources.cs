using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace YFonts.SHX
{
    internal class Resources
    {
        public static Stream OpenStream(string prefix, string name)
        {
            var ass = typeof(Resources).Assembly;
            var stream = ass.GetManifestResourceStream(prefix + name);
            if (stream == null)
                throw new FileNotFoundException();
            return stream;
        }
    }
}