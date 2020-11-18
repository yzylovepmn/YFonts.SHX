using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace YFonts.SHX
{
    public class BigFontFile : FontFile
    {
        public bool IsExtend { get { return _isExtend; } }
        private bool _isExtend;

        public override FontType Type { get { return FontType.Bigfont; } }

        internal override void Init(BinaryReader reader)
        {
            var itemLength = BitConverter.ToInt16(reader.ReadBytes(2), 0);
            var count = BitConverter.ToInt16(reader.ReadBytes(2), 0);
            var changeNumber = BitConverter.ToInt16(reader.ReadBytes(2), 0);

            for (int i = 0; i < changeNumber; i++)
            {
                var startCode = reader.ReadBytes(2);
                var endCode = reader.ReadBytes(2);
            }

            var items = new List<BigFontIndexItem>();
            for (int i = 0; i < count; i++)
            {
                var item = new BigFontIndexItem();
                var data = reader.ReadBytes(8);
                item.Code = BitConverter.ToUInt16(new byte[] { data[1], data[0] }, 0);
                item.Length = BitConverter.ToUInt16(data, 2);
                item.Offset = BitConverter.ToUInt32(data, 4);
                if (item.Code == 0 && item.Length == 0 && item.Offset == 0) continue;
                items.Add(item);
            }

            foreach (var item in items)
            {
                reader.BaseStream.Position = item.Offset;
                _datas[item.Code] = reader.ReadBytes(item.Length);
            }

            var infoData = _datas[0];
            var index = Array.IndexOf(infoData, (byte)0x00);
            _info = Encoding.ASCII.GetString(infoData, 0, index++);

            if (infoData.Length - index == 4)
            {
                _baseUp = infoData[index++];
                _baseDown = infoData[index++];
                _showMode = (ShowMode)infoData[index++];
            }
            else
            {
                _baseUp = infoData[index++]; // font height
                index++;
                _showMode = (ShowMode)infoData[index++];
                _baseDown = infoData[index++]; // font width
                _isExtend = true;
            }
        }
    }
}