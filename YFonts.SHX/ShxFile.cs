using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace YFonts.SHX
{
    public sealed class ShxFile : IDisposable
    {
        public const double DEFAULTSIZE = 12;
        private const string ResourcePrefix = "YFonts.SHX.Resources.";

        public static ShxFile AsciiFile;
        public static ShxFile CHSFile;

        static ShxFile()
        {
            AsciiFile = Load(Resources.OpenStream(ResourcePrefix, "simplex.shx"));
            CHSFile = Load(Resources.OpenStream(ResourcePrefix, "hztxt.shx"));
        }

        public static ShxFile Load(string fileFullName)
        {
            using (var stream = File.OpenRead(fileFullName))
                return Load(stream);
        }

        public static ShxFile Load(Stream stream)
        {
            var shxFile = new ShxFile();

            try
            {
                stream.Position = 0;
                var reader = new BinaryReader(stream);
                var data = ShxParser.ReadBytes(reader, ShxParser.FileStopFlag);
                var headers = Encoding.ASCII.GetString(data).Split();
                shxFile._fileHeader = headers[0];
                shxFile._fileVersion = headers[2];
                var ifile = default(FontFile);
                switch (headers[1])
                {
                    case "shapes":
                        shxFile._type = FontType.Shapes;
                        ifile = new ShapeFontFile();
                        break;
                    case "bigfont":
                        shxFile._type = FontType.Bigfont;
                        ifile = new BigFontFile();
                        break;
                    case "unifont":
                        shxFile._type = FontType.Unifont;
                        ifile = new UniFontFile();
                        break;
                }
                if (ifile != null)
                {
                    ifile.Init(reader);
                    shxFile._fontFile = ifile;
                }
                else return null;
            }
            catch (Exception e)
            {
                return null;
            }

            return shxFile;
        }

        private ShxFile()
        {

        }

        public string FileHeader { get { return _fileHeader; } }
        private string _fileHeader;

        public string FileVersion { get { return _fileVersion; } }
        private string _fileVersion;

        public FontType Type { get { return _type; } }
        private FontType _type;

        public FontFile FontFile { get { return _fontFile; } }
        private FontFile _fontFile;

        public void Dispose()
        {
            _fontFile.Dispose();
            _fontFile = null;
        }

        internal TextShape? GetGraphicData(uint code, double size)
        {
            if (code == 0) return null;
            if (!_fontFile._graphicData.ContainsKey(code))
            {
                if (_fontFile._datas.ContainsKey(code))
                    ShxParser.GenerateGraphicData(this, code);
                else return null;
            }
            if (_fontFile._graphicData.ContainsKey(code))
            {
                var scale = size / DEFAULTSIZE;
                var transform = new Matrix();
                transform.Scale(scale, scale);
                return _fontFile._graphicData[code].Transform(transform);
            }
            return null;
        }

        internal TextShape? GetGraphicData(uint code, double size, Vector translate)
        {
            return GetGraphicData(code, size)?.Offset(translate);
        }

        internal TextShape? GetGraphicData(uint code, double size, Matrix transform)
        {
            return GetGraphicData(code, size)?.Transform(transform);
        }

        public TextShape? GetGraphicData(char c, double size)
        {
            var code = ShxParser.GetCode(_type, c);
            return GetGraphicData(code, size);
        }

        public TextShape? GetGraphicData(char c, double size, Vector translate)
        {
            return GetGraphicData(c, size)?.Offset(translate);
        }

        public TextShape? GetGraphicData(char c, double size, Matrix transform)
        {
            return GetGraphicData(c, size)?.Transform(transform);
        }

        public IEnumerable<TextShape> GetGraphicData(string str, double size, double lineHeight, double lineSpace, double wordSpace)
        {
            var textShapes = new List<TextShape>();
            var lines = str.Split(Environment.NewLine.ToCharArray());
            if (lines.Length == 0) yield break;

            var vOffset = (lines.Length - 1) * (lineSpace + lineHeight);
            var hOffset = 0.0;
            foreach (var line in lines)
            {
                foreach (var c in line)
                {
                    var shape = GetGraphicData(c, size);
                    if (shape == null)
                        hOffset += size;
                    else
                    {
                        yield return shape.Value.Offset(new Vector(hOffset, vOffset));
                        hOffset += shape.Value.Width + wordSpace;
                    }
                }
                hOffset = 0;
                vOffset -= lineSpace + lineHeight;
            }
        }

        public IEnumerable<TextShape> GetGraphicData(string str, double size, double lineHeight, double lineSpace, double wordSpace, Vector translate)
        {
            return GetGraphicData(str, size, lineHeight, lineSpace, wordSpace).Select(ts => ts.Offset(translate));
        }

        public IEnumerable<TextShape> GetGraphicData(string str, double size, double lineHeight, double lineSpace, double wordSpace, Matrix transform)
        {
            return GetGraphicData(str, size, lineHeight, lineSpace, wordSpace).Select(ts => ts.Transform(transform));
        }
    }
}