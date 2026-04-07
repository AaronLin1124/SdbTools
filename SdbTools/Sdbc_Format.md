# Sdbc 文件格式说明

## 1 文件概述

Sdbc (Signal Database for CAN) 是一种用于 CAN/串口通信的二进制信号定义文件格式。

## 2 文件结构

### 2.1 文件头 (Header)

| 偏移 | 长度 | 字段 | 说明 |
|------|------|------|------|
| 0 | 4 | Magic | 固定值 `"sbdc"` (4字节) |
| 4 | 1 | HeaderLength | 文件头总长 = 32 (1字节) |
| 5 | 1 | SignalBodyLength | 单个信号体长度 = 64 (1字节) |
| 6 | 22 | Reserved | 预留字节 (22字节) |
| 28 | 2 | CRC16 | 信号体数据校验 (2字节) |
| 30 | 2 | SignalCount | 信号总数 (2字节) |

**总长度：32字节**

### 2.2 信号体 (Signal Body)

每个信号占用 **64字节**，结构如下：

| 偏移 | 长度 | 字段 | 说明 |
|------|------|------|------|
| 0 | 4 | MessageId | 报文ID (4字节，unsigned int) |
| 4 | 1 | MessageDlc | 数据长度 DLC (1字节) |
| 5 | 32 | Name | 信号名称 (32字节，UTF-8) |
| 37 | 2 | Packed | 起始位+长度+字节序 (2字节) |
| 39 | 4 | Factor | 精度/因子 (4字节，float) |
| 43 | 4 | Offset | 偏移量 (4字节，float) |
| 47 | 16 | Unit | 单位 (16字节，UTF-8) |
| 63 | 1 | Flags | 值类型标志 (1字节) |
| 64 | 1 | Reserved | 预留 (1字节) |

**每个信号：64字节**

### 2.3 Packed 字段结构

```
bit 15: 字节序 (0=Motorola, 1=Intel)
bit 14-9: 预留
bit 8-0: 起始位 (0-511)
```

计算公式：
```csharp
packed = (length << 9) | (startBit & 0x1FF)
if (byteOrder == "Intel") packed |= 0x8000
```

### 2.4 Flags 字段结构

```
bit 7-2: 预留
bit 1: IEEE double
bit 0: 值类型 (0=unsigned, 1=signed, 2=float)
```

## 3 文件示例

### 示例：2个信号的文件

```
总文件大小 = 32 (header) + 64 * 2 (signals) = 160 字节
```

### 头部数据示例

```
Offset 00: 73 62 64 63          # "sbdc" Magic
Offset 04: 20                   # HeaderLength = 32
Offset 05: 40                   # SignalBodyLength = 64
Offset 06: 00 00 00 00 00 00    # 22 bytes reserved
         00 00 00 00 00 00
         00 00 00 00 00 00
         00 00 00 00
Offset 28: XX XX                # CRC16
Offset 30: 00 02                # 2 signals
```

## 4 CRC16 校验

校验范围：从偏移 32 (信号体开始) 到文件末尾

```csharp
ushort Crc16(byte[] data, int offset, int length)
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
```

## 5 与 DBC 文件对比

| 特性 | DBC | Sdbc |
|------|-----|------|
| 文件格式 | 文本 | 二进制 |
| 可读性 | 高 | 低 |
| 解析速度 | 慢 | 快 |
| 文件大小 | 大 | 小 |
| 适用场景 | 开发/调试 | 运行时 |

## 6 版本历史

| 版本 | 日期 | 说明 |
|------|------|------|
| v1.0.0 | 2026-04-04 | 初始版本，支持 CAN 信号 |