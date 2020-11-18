using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace YFonts.SHX
{
    internal class ShxParser
    {
        internal static byte[] StopFlag = new byte[] { 0x00 };
        internal static byte[] FileStopFlag = new byte[] { 0x0d, 0x0a, 0x1a };
        internal const double CICLESPAN = Math.PI / 18;

        internal static byte[] ReadBytes(BinaryReader reader, byte[] stopMatch)
        {
            var header = new List<byte>();
            var flag = false;
            var index = 0;
            while (true)
            {
                var data = reader.ReadByte();
                if (flag)
                {
                    if (data == stopMatch[index])
                        index++;
                    else
                    {
                        flag = false;
                        index = 0;
                    }
                }
                else
                {
                    if (data == stopMatch[index])
                    {
                        flag = true;
                        index++;
                        continue;
                    }
                    header.Add(data);
                }
                if (flag && index == stopMatch.Length)
                    break;
            }

            return header.ToArray();
        }

        internal static uint GetCode(FontType type, char c)
        {
            var cArray = new char[] { c };
            var encode = default(Encoding);
            switch (type)
            {
                case FontType.Shapes:
                    encode = Encoding.ASCII;
                    break;
                case FontType.Bigfont:
                    encode = Encoding.Default;
                    break;
                case FontType.Unifont:
                    encode = Encoding.Unicode;
                    break;
            }
            if (encode != null)
            {
                var data = encode.GetBytes(cArray);
                if (data.Length < 2)
                    return data[0];
                if (data.Length < 4)
                    return BitConverter.ToUInt16(encode.GetBytes(cArray), 0);
                return BitConverter.ToUInt32(encode.GetBytes(cArray), 0);
            }
            return 0;
        }

        internal static void GenerateGraphicData(ShxFile file, uint code)
        {
            var data = file.FontFile._datas[code];
            var scale = ShxFile.DEFAULTSIZE / file.FontFile.BaseUp;
            file.FontFile._graphicData[code] = _ParseCode(file, data, scale);
        }

        private static TextShape _ParseCode(ShxFile file, byte[] data, double scale)
        {
            var currentP = new Point();
            var polyLines = new List<PolyLine>();
            var currentPolyLine = new List<Point>();
            var sp = new Stack<Point>();
            var isPenDown = false;

            for (int i = 0; i < data.Length; i++)
            {
                var cb = data[i];
                switch (cb)
                {
                    case 0x00:
                        break;
                    case 0x01:
                        {
                            isPenDown = true;
                            currentPolyLine.Add(currentP);
                        }
                        break;
                    case 0x02:
                        {
                            isPenDown = false;
                            if (currentPolyLine.Count > 1)
                            {
                                polyLines.Add(new PolyLine(currentPolyLine));
                                currentPolyLine = new List<Point>();
                            }
                        }
                        break;
                    case 0x03:
                        {
                            i++;
                            scale /= data[i];
                        }
                        break;
                    case 0x04:
                        {
                            i++;
                            scale *= data[i];
                        }
                        break;
                    case 0x05:
                        {
                            if (sp.Count == 4) throw new InvalidOperationException("The position stack is only four locations deep");
                            sp.Push(currentP);
                        }
                        break;
                    case 0x06:
                        {
                            currentP = sp.Pop();
                            if (currentPolyLine.Count > 1)
                            {
                                polyLines.Add(new PolyLine(currentPolyLine));
                                currentPolyLine = new List<Point>();
                            }
                            if (isPenDown)
                                currentPolyLine.Add(currentP);
                        }
                        break;
                    case 0x07:
                        {
                            var subCode = 0u;
                            var shape = default(TextShape?);
                            var size = scale * file.FontFile.BaseUp;
                            var origin = currentP;
                            if (currentPolyLine.Count > 1)
                            {
                                polyLines.Add(new PolyLine(currentPolyLine));
                                currentPolyLine = new List<Point>();
                            }
                            switch (file.Type)
                            {
                                case FontType.Shapes:
                                    {
                                        i++;
                                        subCode = data[i];
                                    }
                                    break;
                                case FontType.Bigfont:
                                    {
                                        i++;
                                        subCode = data[i];
                                        if (subCode == 0)
                                        {
                                            i++;
                                            subCode = BitConverter.ToUInt16(new byte[] { data[i++], data[i++] }, 0);
                                            origin.X = data[i++] * scale;
                                            origin.Y = data[i++] * scale;
                                            var width = data[i++] * scale;
                                            var height = data[i] * scale;
                                            size = height;
                                        }
                                    }
                                    break;
                                case FontType.Unifont:
                                    {
                                        i += 2;
                                        subCode = BitConverter.ToUInt16(new byte[] { data[i - 1], data[i] }, 0);
                                    }
                                    break;
                            }
                            if (subCode != 0)
                            {
                                shape = file.GetGraphicData(subCode, size, (Vector)origin);
                                if (shape.HasValue)
                                {
                                    polyLines.AddRange(shape.Value.PolyLines);
                                    currentP = shape.Value.LastPoint;
                                }
                            }
                        }
                        break;
                    case 0x08:
                        {
                            var vec = new Vector();
                            vec.X = (sbyte)data[++i];
                            vec.Y = (sbyte)data[++i];
                            currentP += vec * scale;
                            if (isPenDown)
                                currentPolyLine.Add(currentP);
                        }
                        break;
                    case 0x09:
                        {
                            while (true)
                            {
                                var vec = new Vector();
                                vec.X = (sbyte)data[++i];
                                vec.Y = (sbyte)data[++i];
                                if (vec.X == 0 && vec.Y == 0) break;
                                currentP += vec * scale;
                                if (isPenDown)
                                    currentPolyLine.Add(currentP);
                            }
                        }
                        break;
                    case 0x0a:
                        {
                            var r = data[++i] * scale;
                            var flag = (sbyte)data[++i];
                            var n1 = (flag & 0x70) >> 4;
                            var n2 = flag & 0x07;
                            if (n2 == 0)
                                n2 = 8;
                            var pi_4 = Math.PI / 4;
                            var span = pi_4 * n2;
                            var delta = CICLESPAN;
                            if (flag < 0)
                            {
                                delta = -delta;
                                span = -span;
                            }
                            var startRadian = pi_4 * n1;
                            var endRadian = startRadian + span;
                            var center = currentP - r * new Vector(Math.Cos(startRadian), Math.Sin(startRadian));
                            currentP = center + r * new Vector(Math.Cos(endRadian), Math.Sin(endRadian));
                            if (isPenDown)
                            {
                                var currentRadian = startRadian;
                                while (true)
                                {
                                    currentRadian += delta;
                                    if ((flag > 0 && currentRadian < endRadian) || (flag < 0 && currentRadian > endRadian))
                                        currentPolyLine.Add(center + r * new Vector(Math.Cos(currentRadian), Math.Sin(currentRadian)));
                                    else break;
                                }
                                currentPolyLine.Add(currentP);
                            }
                        }
                        break;
                    case 0x0b:
                        {
                            int startOffset = data[++i];
                            int endOffset = data[++i];
                            var hr = data[++i];
                            var lr = data[++i];
                            var r = (hr * 255 + lr) * scale;
                            var flag = (sbyte)data[++i];
                            var n1 = (flag & 0x70) >> 4;
                            var n2 = flag & 0x07;
                            if (n2 == 0)
                                n2 = 8;
                            if (endOffset != 0)
                                n2--;
                            var pi_4 = Math.PI / 4;
                            var span = pi_4 * n2;
                            var delta = CICLESPAN;
                            var sign = 1;
                            if (flag < 0)
                            {
                                delta = -delta;
                                span = -span;
                                sign = -1;
                            }
                            var startRadian = pi_4 * n1;
                            var endRadian = startRadian + span;
                            startRadian += pi_4 * startOffset / 256 * sign;
                            endRadian += pi_4 * endOffset / 256 * sign;
                            var center = currentP - r * new Vector(Math.Cos(startRadian), Math.Sin(startRadian));
                            currentP = center + r * new Vector(Math.Cos(endRadian), Math.Sin(endRadian));
                            if (isPenDown)
                            {
                                var currentRadian = startRadian;
                                while (true)
                                {
                                    currentRadian += delta;
                                    if ((flag > 0 && currentRadian < endRadian) || (flag < 0 && currentRadian > endRadian))
                                        currentPolyLine.Add(center + r * new Vector(Math.Cos(currentRadian), Math.Sin(currentRadian)));
                                    else break;
                                }
                                currentPolyLine.Add(currentP);
                            }
                        }
                        break;
                    case 0x0c:
                        {
                            var vec = new Vector();
                            vec.X = (sbyte)data[++i] * scale;
                            vec.Y = (sbyte)data[++i] * scale;
                            var bulge = (sbyte)data[++i];
                            if (bulge < -127)
                                bulge = -127;
                            if (isPenDown)
                            {
                                if (bulge == 0)
                                    currentPolyLine.Add(currentP + vec);
                                else currentPolyLine.AddRange(_GenerateArcPoints(currentP, vec, bulge / 127.0));
                            }
                            currentP += vec;
                        }
                        break;
                    case 0x0d:
                        {
                            while (true)
                            {
                                var vec = new Vector();
                                vec.X = (sbyte)data[++i] * scale;
                                vec.Y = (sbyte)data[++i] * scale;
                                if (vec.X == 0 && vec.Y == 0) break;
                                var bulge = (sbyte)data[++i];
                                if (bulge < -127)
                                    bulge = -127;
                                if (isPenDown)
                                {
                                    if (bulge == 0)
                                        currentPolyLine.Add(currentP + vec);
                                    else currentPolyLine.AddRange(_GenerateArcPoints(currentP, vec, bulge / 127.0));
                                }
                                currentP += vec;
                            }
                        }
                        break;
                    case 0x0e:
                        {
                            i = _SkipCode(file, data, ++i);
                        }
                        break;
                    default:
                        if (cb > 0x0f)
                        {
                            var len = (cb & 0xf0) >> 4;
                            var dir = cb & 0x0f;
                            var vec = new Vector();
                            switch (dir)
                            {
                                case 0:
                                    vec.X = 1;
                                    break;
                                case 1:
                                    vec.X = 1;
                                    vec.Y = 0.5;
                                    break;
                                case 2:
                                    vec.X = 1;
                                    vec.Y = 1;
                                    break;
                                case 3:
                                    vec.X = 0.5;
                                    vec.Y = 1;
                                    break;
                                case 4:
                                    vec.Y = 1;
                                    break;
                                case 5:
                                    vec.X = -0.5;
                                    vec.Y = 1;
                                    break;
                                case 6:
                                    vec.X = -1;
                                    vec.Y = 1;
                                    break;
                                case 7:
                                    vec.X = -1;
                                    vec.Y = 0.5;
                                    break;
                                case 8:
                                    vec.X = -1;
                                    break;
                                case 9:
                                    vec.X = -1;
                                    vec.Y = -0.5;
                                    break;
                                case 10:
                                    vec.X = -1;
                                    vec.Y = -1;
                                    break;
                                case 11:
                                    vec.X = -0.5;
                                    vec.Y = -1;
                                    break;
                                case 12:
                                    vec.Y = -1;
                                    break;
                                case 13:
                                    vec.X = 0.5;
                                    vec.Y = -1;
                                    break;
                                case 14:
                                    vec.X = 1;
                                    vec.Y = -1;
                                    break;
                                case 15:
                                    vec.X = 1;
                                    vec.Y = -0.5;
                                    break;
                            }
                            currentP += vec * len * scale;
                            if (isPenDown)
                                currentPolyLine.Add(currentP);
                        }
                        break;
                }
            }

            return new TextShape(currentP, polyLines);
        }

        private static int _SkipCode(ShxFile file, byte[] data, int index)
        {
            var cb = data[index];
            switch (cb)
            {
                case 0x00:
                    break;
                case 0x01:
                    break;
                case 0x02:
                    break;
                case 0x03:
                case 0x04:
                    index++;
                    break;
                case 0x05:
                    break;
                case 0x06:
                    break;
                case 0x07:
                    switch (file.Type)
                    {
                        case FontType.Shapes:
                            index++;
                            break;
                        case FontType.Bigfont:
                            index++;
                            var subCode = data[index];
                            if (subCode == 0)
                                index += 6;
                            break;
                        case FontType.Unifont:
                            index += 2;
                            break;
                    }
                    break;
                case 0x08:
                    index += 2;
                    break;
                case 0x09:
                    while (true)
                    {
                        var x = data[++index];
                        var y = data[++index];
                        if (x == 0 && y == 0)
                            break;
                    }
                    break;
                case 0x0a:
                    index += 2;
                    break;
                case 0x0b:
                    index += 5;
                    break;
                case 0x0c:
                    index += 3;
                    break;
                case 0x0d:
                    while (true)
                    {
                        var x = data[++index];
                        var y = data[++index];
                        if (x == 0 && y == 0)
                            break;
                        var bulge = data[++index];
                    }
                    break;
                case 0x0e:
                    break;
                default:
                    break;
            }
            return index;
        }

        private static IEnumerable<Point> _GenerateArcPoints(Point start, Vector distance, double bulge)
        {
            var end = start + distance;
            var isClockwise = bulge < 0;
            var isLargeAngle = false;
            bulge = Math.Abs(bulge);
            var halfLength = distance.Length / 2;
            var h = halfLength * bulge;
            var radian = 2 * Math.Atan(1 / bulge);
            var normal = new Vector(distance.Y, -distance.X);
            normal.Normalize();
            normal *= h;

            var radius = Math.Abs(halfLength / Math.Sin(radian / 2));
            var center = start + distance / 2;
            if (isLargeAngle ^ isClockwise)
                center += normal;
            else center -= normal;

            var svec = start - center;
            var evec = end - center;
            var startRadian = Math.Atan2(svec.Y, svec.X);
            var endRadian = Math.Atan2(evec.Y, evec.X);
            var delta = CICLESPAN;
            if (isClockwise)
            {
                delta = -delta;
                if (startRadian < endRadian)
                    startRadian += 2 * Math.PI;
            }
            else
            {
                if (startRadian > endRadian)
                    startRadian -= 2 * Math.PI;
            }
            var currentRadian = startRadian;
            while (true)
            {
                currentRadian += delta;
                if ((!isClockwise && currentRadian < endRadian) || (isClockwise && currentRadian > endRadian))
                    yield return center + radius * new Vector(Math.Cos(currentRadian), Math.Sin(currentRadian));
                else break;
            }
            yield return end;
        }
    }
}