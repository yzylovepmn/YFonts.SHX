using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace YFonts.SHX
{
    public class UniFontFile : FontFile
    {
        public override FontType Type { get { return FontType.Unifont; } }

        public bool IsUniCode { get { return _isUniCode; } }
        private bool _isUniCode;

        public bool IsEmbedded { get { return _isEmbedded; } }
        private bool _isEmbedded;

        internal override void Init(BinaryReader reader)
        {
            var count = BitConverter.ToInt32(reader.ReadBytes(4), 0);
            var infoLength = BitConverter.ToInt16(reader.ReadBytes(2), 0);
            var infoData = reader.ReadBytes(infoLength);
            var index = Array.IndexOf(infoData, (byte)0x00);
            _info = Encoding.ASCII.GetString(infoData, 0, index);
            _baseUp = infoData[++index];
            _baseDown = infoData[++index];
            _showMode = (ShowMode)infoData[++index];
            _isUniCode = infoData[++index] == 0x00;
            _isEmbedded = infoData[++index] == 0x00;

            var items = new List<UniFontIndexItem>();
            for (int i = 0; i < count - 1; i++)
            {
                var item = new UniFontIndexItem();
                item.Code = BitConverter.ToUInt16(reader.ReadBytes(2), 0);
                item.Length = BitConverter.ToUInt16(reader.ReadBytes(2), 0);
                _datas[item.Code] = reader.ReadBytes(item.Length);
                items.Add(item);
            }
        }
    }
}