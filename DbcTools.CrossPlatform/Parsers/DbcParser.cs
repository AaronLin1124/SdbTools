using System.Text;
using System.Text.RegularExpressions;
using DbcTools.CrossPlatform.Models;

namespace DbcTools.CrossPlatform.Parsers;

public static class DbcParser
{
    private static uint ParseMessageId(string raw)
    {
        raw = raw.Trim();
        if (raw.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            return Convert.ToUInt32(raw[2..], 16);
        
        if (uint.TryParse(raw, out uint result))
            return result;
        
        return Convert.ToUInt32(raw, 16);
    }

    private static string[] ReadLinesWithEncoding(string filePath)
    {
        var rawBytes = File.ReadAllBytes(filePath);
        var utf8 = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);
        try
        {
            var text = utf8.GetString(rawBytes);
            return text.Split(["\r\n", "\r", "\n"], StringSplitOptions.None);
        }
        catch (DecoderFallbackException)
        {
            var gbk = Encoding.GetEncoding("GBK");
            var text = gbk.GetString(rawBytes);
            return text.Split(["\r\n", "\r", "\n"], StringSplitOptions.None);
        }
    }

    public static List<DbcSignal> Parse(string filePath)
    {
        var signals = new List<DbcSignal>();
        var lines = ReadLinesWithEncoding(filePath);

        uint currentMsgId = 0;
        string currentMsgName = string.Empty;
        int currentMsgDlc = 0;
        bool currentIsExtended = false;

        var boPattern = new Regex(@"^BO_\s+([0-9a-fA-FxX]+)\s+(\w+)\s*:\s*(\d+)\s+\w+");
        var sgPattern = new Regex(
            @"^\s*SG_\s+(\w+)\s*:\s*(\d+)\|(\d+)@([01])([+-])\s*\(\s*([^,]+)\s*,\s*([^)]+)\s*\)\s*\[[^\]]*\]\s*""([^""]*)""");

        foreach (var line in lines)
        {
            var boMatch = boPattern.Match(line);
            if (boMatch.Success)
            {
                currentMsgId = ParseMessageId(boMatch.Groups[1].Value.Trim());
                currentIsExtended = (currentMsgId & 0x80000000) != 0;
                currentMsgId &= 0x1FFFFFFF;
                currentMsgName = boMatch.Groups[2].Value;
                currentMsgDlc = int.Parse(boMatch.Groups[3].Value);
                continue;
            }

            var sgMatch = sgPattern.Match(line);
            if (sgMatch.Success)
            {
                var isInvalid = (currentMsgId & 0xC0000000) == 0xC0000000 || currentMsgId == 0;
                var signal = new DbcSignal
                {
                    Name = sgMatch.Groups[1].Value,
                    StartBit = int.Parse(sgMatch.Groups[2].Value),
                    Length = int.Parse(sgMatch.Groups[3].Value),
                    ByteOrder = sgMatch.Groups[4].Value == "0" ? "Motorola" : "Intel",
                    ValueType = sgMatch.Groups[5].Value == "+" ? "unsigned" : "signed",
                    Factor = double.Parse(sgMatch.Groups[6].Value.Trim(), System.Globalization.CultureInfo.InvariantCulture),
                    Offset = double.Parse(sgMatch.Groups[7].Value.Trim(), System.Globalization.CultureInfo.InvariantCulture),
                    Unit = sgMatch.Groups[8].Value,
                    MessageId = currentMsgId,
                    MessageName = currentMsgName,
                    MessageDlc = currentMsgDlc,
                    IsExtendedFrame = currentIsExtended,
                    IsInvalid = isInvalid
                };
                signals.Add(signal);
                continue;
            }

            var vtMatch = Regex.Match(line, @"^VALTYPE_\s+\d+\s+(\w+)\s*:\s*(\d)");
            if (vtMatch.Success)
            {
                string sigName = vtMatch.Groups[1].Value;
                int vtCode = int.Parse(vtMatch.Groups[2].Value);
                string vtName = vtCode switch
                {
                    1 => "signed",
                    2 => "IEEE float",
                    3 => "IEEE double",
                    _ => "unsigned"
                };
                var target = signals.FindLast(s => s.Name == sigName);
                if (target != null) target.ValueType = vtName;
            }
        }

        return signals;
    }
}
