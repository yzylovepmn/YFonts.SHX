using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace YFonts.SHX
{
    public enum ShowMode
    {
        Horizontal = 0x00,
        Vertical = 0x01,
        All = 0x02,
    }

    public abstract class FontFile : IDisposable
    {
        internal FontFile()
        {
            _datas = new Dictionary<uint, byte[]>();
            _graphicData = new Dictionary<uint, TextShape>();
        }

        public abstract FontType Type { get; }

        public string Info { get { return _info; } }
        protected string _info;

        public ShowMode ShowMode { get { return _showMode; } }
        protected ShowMode _showMode;

        public byte BaseUp { get { return _baseUp; } }
        protected byte _baseUp;

        public byte BaseDown { get { return _baseDown; } }
        protected byte _baseDown;

        internal Dictionary<uint, byte[]> _datas;
        internal Dictionary<uint, TextShape> _graphicData;

        internal abstract void Init(BinaryReader reader);

        public void Dispose()
        {
            _datas = null;
            _graphicData = null;
        }
    }
}