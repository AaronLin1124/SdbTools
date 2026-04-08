using System.Text;
using DbcTools.CrossPlatform.Models;

namespace DbcTools.CrossPlatform.Writers;

public static class BinWriter
{
    private const string MagicNumber = "sbdc";
    private const byte HeaderLength = 32;
    private const byte SignalBodyLength = 64;

    public static void Write(string outputPath, List<DbcSignal> signals)
    {
        var validSignals = signals.Where(s => !s.IsInvalid).ToList();
        
        using var ms = new MemoryStream();
        using var bw = new BinaryWriter(ms);

        WriteString(bw, MagicNumber, 4);
        bw.Write(HeaderLength);
        bw.Write(SignalBodyLength);
        var reserved = new byte[22];
        bw.Write(reserved);
        bw.Write((ushort)0);
        bw.Write((ushort)validSignals.Count);

        foreach (var sig in validSignals)
        {
            bw.Write(sig.MessageId);
            bw.Write((ushort)sig.MessageDlc);
            WriteString(bw, sig.Name, 32);
            ushort packed = (ushort)((sig.Length << 9) | (sig.StartBit & 0x1FF));
            if (sig.ByteOrder == "Intel") packed |= 0x8000;
            bw.Write(packed);
            bw.Write(sig.Factor);
            bw.Write(sig.Offset);
            WriteString(bw, sig.Unit, 8);
            byte valueType = sig.ValueType switch
            {
                "signed" => 1,
                "IEEE float" => 2,
                "IEEE double" => 3,
                _ => 0
            };
            byte flags = valueType;
            if (sig.IsExtendedFrame) flags |= 0x04;
            bw.Write(flags);
            var sigReserved = new byte[7];
            bw.Write(sigReserved);
        }

        bw.Flush();
        var data = ms.ToArray();

        ushort crc = Crc16(data, 32, data.Length - 32);
        data[28] = (byte)(crc & 0xFF);
        data[29] = (byte)((crc >> 8) & 0xFF);

        File.WriteAllBytes(outputPath, data);
    }

    private static ushort Crc16(byte[] data, int offset, int length)
    {
        ushort crc = 0xFFFF;
        for (int i = offset; i < offset + length; i++)
        {
            crc ^= data[i];
            for (int j = 0; j < 8; j++)
            {
                if ((crc & 0x0001) != 0)
                    crc = (ushort)((crc >> 1) ^ 0xA001);
                else
                    crc >>= 1;
            }
        }
        return crc;
    }

    private static void WriteString(BinaryWriter bw, string value, int fixedLength)
    {
        var bytes = Encoding.UTF8.GetBytes(value ?? string.Empty);
        var buffer = new byte[fixedLength];
        int copyLen = Math.Min(bytes.Length, fixedLength);
        Array.Copy(bytes, buffer, copyLen);
        bw.Write(buffer);
    }
}
