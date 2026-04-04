namespace DbcTools.CrossPlatform.Models;

public class DbcSignal
{
    public string Name { get; set; } = string.Empty;
    public int StartBit { get; set; }
    public int Length { get; set; }
    public double Factor { get; set; }
    public double Offset { get; set; }
    public string Unit { get; set; } = string.Empty;
    public string ByteOrder { get; set; } = string.Empty;
    public string ValueType { get; set; } = string.Empty;
    public string MessageName { get; set; } = string.Empty;
    public uint MessageId { get; set; }
    public string MessageIdHex => IsInvalid ? "Invalid" : (MessageId == 0 ? "0x0" : $"0x{MessageId:X}");
    public int MessageDlc { get; set; }
    public bool IsExtendedFrame { get; set; }
    public bool IsInvalid { get; set; }
}
