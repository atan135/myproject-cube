using System;

namespace Cube.GameServer.KcpTransport;

/// <summary>
/// KCP 位移同步消息类型
/// </summary>
public enum KcpMessageType : byte
{
    /// <summary>客户端→服务端：玩家加入（携带玩家ID绑定KCP连接）</summary>
    PlayerJoin = 1,

    /// <summary>服务端→客户端：加入确认</summary>
    PlayerJoinAck = 2,

    /// <summary>客户端→服务端：位移输入</summary>
    MovementInput = 10,

    /// <summary>服务端→客户端：世界快照（所有玩家位置）</summary>
    WorldSnapshot = 20,

    /// <summary>服务端→客户端：单个玩家状态更新</summary>
    PlayerStateUpdate = 21,

    /// <summary>服务端→客户端：玩家离开通知</summary>
    PlayerLeave = 30,
}

/// <summary>
/// 3D 向量，用于位置和方向
/// 使用 float 精度，12 字节
/// </summary>
public struct Vector3Data
{
    public float X;
    public float Y;
    public float Z;

    public Vector3Data(float x, float y, float z)
    {
        X = x;
        Y = y;
        Z = z;
    }

    /// <summary>写入到 buffer，返回写入字节数 (12)</summary>
    public int WriteTo(byte[] buffer, int offset)
    {
        BitConverter.TryWriteBytes(new Span<byte>(buffer, offset, 4), X);
        BitConverter.TryWriteBytes(new Span<byte>(buffer, offset + 4, 4), Y);
        BitConverter.TryWriteBytes(new Span<byte>(buffer, offset + 8, 4), Z);
        return 12;
    }

    /// <summary>从 buffer 读取，返回读取字节数 (12)</summary>
    public static int ReadFrom(byte[] buffer, int offset, out Vector3Data result)
    {
        result = new Vector3Data
        {
            X = BitConverter.ToSingle(buffer, offset),
            Y = BitConverter.ToSingle(buffer, offset + 4),
            Z = BitConverter.ToSingle(buffer, offset + 8)
        };
        return 12;
    }

    public override string ToString() => $"({X:F2}, {Y:F2}, {Z:F2})";
}

/// <summary>
/// 四元数，用于旋转
/// 使用 float 精度，16 字节
/// </summary>
public struct QuaternionData
{
    public float X;
    public float Y;
    public float Z;
    public float W;

    public QuaternionData(float x, float y, float z, float w)
    {
        X = x;
        Y = y;
        Z = z;
        W = w;
    }

    /// <summary>单位四元数</summary>
    public static QuaternionData Identity => new(0, 0, 0, 1);

    /// <summary>写入到 buffer，返回写入字节数 (16)</summary>
    public int WriteTo(byte[] buffer, int offset)
    {
        BitConverter.TryWriteBytes(new Span<byte>(buffer, offset, 4), X);
        BitConverter.TryWriteBytes(new Span<byte>(buffer, offset + 4, 4), Y);
        BitConverter.TryWriteBytes(new Span<byte>(buffer, offset + 8, 4), Z);
        BitConverter.TryWriteBytes(new Span<byte>(buffer, offset + 12, 4), W);
        return 16;
    }

    /// <summary>从 buffer 读取，返回读取字节数 (16)</summary>
    public static int ReadFrom(byte[] buffer, int offset, out QuaternionData result)
    {
        result = new QuaternionData
        {
            X = BitConverter.ToSingle(buffer, offset),
            Y = BitConverter.ToSingle(buffer, offset + 4),
            Z = BitConverter.ToSingle(buffer, offset + 8),
            W = BitConverter.ToSingle(buffer, offset + 12)
        };
        return 16;
    }

    public override string ToString() => $"({X:F2}, {Y:F2}, {Z:F2}, {W:F2})";
}

/// <summary>
/// 玩家加入消息 (客户端→服务端)
/// 格式: [MsgType:1] [PlayerId:4]
/// </summary>
public struct PlayerJoinMessage
{
    public const int SIZE = 1 + 4; // 5 bytes
    public int PlayerId;

    public int WriteTo(byte[] buffer, int offset)
    {
        buffer[offset] = (byte)KcpMessageType.PlayerJoin;
        BitConverter.TryWriteBytes(new Span<byte>(buffer, offset + 1, 4), PlayerId);
        return SIZE;
    }

    public static PlayerJoinMessage ReadFrom(byte[] buffer, int offset)
    {
        return new PlayerJoinMessage
        {
            PlayerId = BitConverter.ToInt32(buffer, offset + 1)
        };
    }
}

/// <summary>
/// 玩家加入确认消息 (服务端→客户端)
/// 格式: [MsgType:1] [PlayerId:4] [ServerTick:4]
/// </summary>
public struct PlayerJoinAckMessage
{
    public const int SIZE = 1 + 4 + 4; // 9 bytes
    public int PlayerId;
    public uint ServerTick;

    public int WriteTo(byte[] buffer, int offset)
    {
        buffer[offset] = (byte)KcpMessageType.PlayerJoinAck;
        BitConverter.TryWriteBytes(new Span<byte>(buffer, offset + 1, 4), PlayerId);
        BitConverter.TryWriteBytes(new Span<byte>(buffer, offset + 5, 4), ServerTick);
        return SIZE;
    }

    public static PlayerJoinAckMessage ReadFrom(byte[] buffer, int offset)
    {
        return new PlayerJoinAckMessage
        {
            PlayerId = BitConverter.ToInt32(buffer, offset + 1),
            ServerTick = BitConverter.ToUInt32(buffer, offset + 5)
        };
    }
}

/// <summary>
/// 位移输入消息 (客户端→服务端)
/// 格式: [MsgType:1] [PlayerId:4] [InputSeq:4] [Direction:12] [MoveSpeed:4] [IsJumping:1] [YawAngle:4]
/// 总计 30 字节，非常小，适合高频发送
/// </summary>
public struct MovementInputMessage
{
    public const int SIZE = 1 + 4 + 4 + 12 + 4 + 1 + 4; // 30 bytes
    public int PlayerId;
    public uint InputSequence;     // 客户端输入序列号，用于服务端回滚
    public Vector3Data Direction;  // 移动方向 (归一化)
    public float MoveSpeed;        // 移动速度
    public bool IsJumping;         // 是否跳跃
    public float YawAngle;         // Y轴旋转角度 (弧度)

    public int WriteTo(byte[] buffer, int offset)
    {
        int start = offset;
        buffer[offset] = (byte)KcpMessageType.MovementInput;
        offset += 1;
        BitConverter.TryWriteBytes(new Span<byte>(buffer, offset, 4), PlayerId);
        offset += 4;
        BitConverter.TryWriteBytes(new Span<byte>(buffer, offset, 4), InputSequence);
        offset += 4;
        offset += Direction.WriteTo(buffer, offset);
        BitConverter.TryWriteBytes(new Span<byte>(buffer, offset, 4), MoveSpeed);
        offset += 4;
        buffer[offset] = IsJumping ? (byte)1 : (byte)0;
        offset += 1;
        BitConverter.TryWriteBytes(new Span<byte>(buffer, offset, 4), YawAngle);
        offset += 4;
        return offset - start;
    }

    public static MovementInputMessage ReadFrom(byte[] buffer, int offset)
    {
        var msg = new MovementInputMessage();
        offset += 1; // skip MsgType
        msg.PlayerId = BitConverter.ToInt32(buffer, offset); offset += 4;
        msg.InputSequence = BitConverter.ToUInt32(buffer, offset); offset += 4;
        Vector3Data.ReadFrom(buffer, offset, out msg.Direction); offset += 12;
        msg.MoveSpeed = BitConverter.ToSingle(buffer, offset); offset += 4;
        msg.IsJumping = buffer[offset] != 0; offset += 1;
        msg.YawAngle = BitConverter.ToSingle(buffer, offset);
        return msg;
    }
}

/// <summary>
/// 单个玩家的快照状态
/// 格式: [PlayerId:4] [Position:12] [Rotation:16] [Velocity:12] [IsGrounded:1]
/// 总计 45 字节
/// </summary>
public struct PlayerSnapshotData
{
    public const int SIZE = 4 + 12 + 16 + 12 + 1; // 45 bytes
    public int PlayerId;
    public Vector3Data Position;
    public QuaternionData Rotation;
    public Vector3Data Velocity;
    public bool IsGrounded;

    public int WriteTo(byte[] buffer, int offset)
    {
        int start = offset;
        BitConverter.TryWriteBytes(new Span<byte>(buffer, offset, 4), PlayerId);
        offset += 4;
        offset += Position.WriteTo(buffer, offset);
        offset += Rotation.WriteTo(buffer, offset);
        offset += Velocity.WriteTo(buffer, offset);
        buffer[offset] = IsGrounded ? (byte)1 : (byte)0;
        offset += 1;
        return offset - start;
    }

    public static int ReadFrom(byte[] buffer, int offset, out PlayerSnapshotData result)
    {
        int start = offset;
        result = new PlayerSnapshotData();
        result.PlayerId = BitConverter.ToInt32(buffer, offset); offset += 4;
        Vector3Data.ReadFrom(buffer, offset, out result.Position); offset += 12;
        QuaternionData.ReadFrom(buffer, offset, out result.Rotation); offset += 16;
        Vector3Data.ReadFrom(buffer, offset, out result.Velocity); offset += 12;
        result.IsGrounded = buffer[offset] != 0; offset += 1;
        return offset - start;
    }
}

/// <summary>
/// 世界快照消息 (服务端→客户端)
/// 格式: [MsgType:1] [ServerTick:4] [Timestamp:8] [PlayerCount:2] [PlayerSnapshot * N]
/// 使用 unreliable KCP 通道发送，丢包无所谓（快照插值会自动处理）
/// </summary>
public struct WorldSnapshotMessage
{
    public const int HEADER_SIZE = 1 + 4 + 8 + 2; // 15 bytes header
    public uint ServerTick;           // 服务端 tick 序号
    public long Timestamp;            // 服务端时间戳 (ms)
    public PlayerSnapshotData[] Players;

    public int WriteTo(byte[] buffer, int offset)
    {
        int start = offset;
        buffer[offset] = (byte)KcpMessageType.WorldSnapshot;
        offset += 1;
        BitConverter.TryWriteBytes(new Span<byte>(buffer, offset, 4), ServerTick);
        offset += 4;
        BitConverter.TryWriteBytes(new Span<byte>(buffer, offset, 8), Timestamp);
        offset += 8;

        ushort count = (ushort)(Players?.Length ?? 0);
        BitConverter.TryWriteBytes(new Span<byte>(buffer, offset, 2), count);
        offset += 2;

        for (int i = 0; i < count; i++)
        {
            offset += Players[i].WriteTo(buffer, offset);
        }

        return offset - start;
    }

    public static WorldSnapshotMessage ReadFrom(byte[] buffer, int offset)
    {
        var msg = new WorldSnapshotMessage();
        offset += 1; // skip MsgType
        msg.ServerTick = BitConverter.ToUInt32(buffer, offset); offset += 4;
        msg.Timestamp = BitConverter.ToInt64(buffer, offset); offset += 8;

        ushort count = BitConverter.ToUInt16(buffer, offset); offset += 2;
        msg.Players = new PlayerSnapshotData[count];
        for (int i = 0; i < count; i++)
        {
            offset += PlayerSnapshotData.ReadFrom(buffer, offset, out msg.Players[i]);
        }

        return msg;
    }

    /// <summary>计算整个消息的大小</summary>
    public int CalculateSize()
    {
        return HEADER_SIZE + (Players?.Length ?? 0) * PlayerSnapshotData.SIZE;
    }
}

/// <summary>
/// 玩家离开消息 (服务端→客户端)
/// 格式: [MsgType:1] [PlayerId:4]
/// </summary>
public struct PlayerLeaveMessage
{
    public const int SIZE = 1 + 4; // 5 bytes
    public int PlayerId;

    public int WriteTo(byte[] buffer, int offset)
    {
        buffer[offset] = (byte)KcpMessageType.PlayerLeave;
        BitConverter.TryWriteBytes(new Span<byte>(buffer, offset + 1, 4), PlayerId);
        return SIZE;
    }

    public static PlayerLeaveMessage ReadFrom(byte[] buffer, int offset)
    {
        return new PlayerLeaveMessage
        {
            PlayerId = BitConverter.ToInt32(buffer, offset + 1)
        };
    }
}

/// <summary>
/// 消息解析工具
/// </summary>
public static class MessageParser
{
    /// <summary>从原始字节解析消息类型</summary>
    public static KcpMessageType ParseType(byte[] buffer, int offset)
    {
        return (KcpMessageType)buffer[offset];
    }

    /// <summary>从 ArraySegment 解析消息类型</summary>
    public static KcpMessageType ParseType(ArraySegment<byte> segment)
    {
        return (KcpMessageType)segment.Array![segment.Offset];
    }
}
