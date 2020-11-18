using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace YFonts.SHX
{
    public class ShapeFontFile : FontFile
    {
        public override FontType Type { get { return FontType.Shapes; } }

        internal override void Init(BinaryReader reader)
        {
            var startCode = reader.ReadBytes(2);
            var endCode = reader.ReadBytes(2);
            var count = BitConverter.ToInt16(reader.ReadBytes(2), 0);
            var items = new List<ShapeIndexItem>();
            for (int i = 0; i < count; i++)
            {
                var item = new ShapeIndexItem();
                item.Code = BitConverter.ToUInt16(reader.ReadBytes(2), 0);
                item.Length = BitConverter.ToUInt16(reader.ReadBytes(2), 0);
                items.Add(item);
            }

            foreach (var item in items)
                _datas[item.Code] = reader.ReadBytes(item.Length);

            var infoData = _datas[0];
            var index = Array.IndexOf(infoData, (byte)0x00);
            _info = Encoding.ASCII.GetString(infoData, 0, index);
            _baseUp = infoData[++index];
            _baseDown = infoData[++index];
            _showMode = (ShowMode)infoData[++index];
        }
    }
}