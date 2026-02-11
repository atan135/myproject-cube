using System;

namespace Cube.Network.KcpMovement
{
    /// <summary>
    /// KCP 位移同步消息类型
    /// 与服务端 KcpTransport/MovementMessage.cs 保持一致
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
    /// 3D 向量数据，用于网络传输
    /// </summary>
    public struct Vector3Data
    {
        public float X;
        public float Y;
        public float Z;

        public Vector3Data(float x, float y, float z) { X = x; Y = y; Z = z; }

        public int WriteTo(byte[] buffer, int offset)
        {
            Buffer.BlockCopy(BitConverter.GetBytes(X), 0, buffer, offset, 4);
            Buffer.BlockCopy(BitConverter.GetBytes(Y), 0, buffer, offset + 4, 4);
            Buffer.BlockCopy(BitConverter.GetBytes(Z), 0, buffer, offset + 8, 4);
            return 12;
        }

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

        public UnityEngine.Vector3 ToUnityVector3() => new UnityEngine.Vector3(X, Y, Z);

        public static Vector3Data FromUnityVector3(UnityEngine.Vector3 v) =>
            new Vector3Data(v.x, v.y, v.z);

        public override string ToString() => $"({X:F2}, {Y:F2}, {Z:F2})";
    }

    /// <summary>
    /// 四元数数据，用于网络传输
    /// </summary>
    public struct QuaternionData
    {
        public float X;
        public float Y;
        public float Z;
        public float W;

        public QuaternionData(float x, float y, float z, float w) { X = x; Y = y; Z = z; W = w; }

        public static QuaternionData Identity => new QuaternionData(0, 0, 0, 1);

        public int WriteTo(byte[] buffer, int offset)
        {
            Buffer.BlockCopy(BitConverter.GetBytes(X), 0, buffer, offset, 4);
            Buffer.BlockCopy(BitConverter.GetBytes(Y), 0, buffer, offset + 4, 4);
            Buffer.BlockCopy(BitConverter.GetBytes(Z), 0, buffer, offset + 8, 4);
            Buffer.BlockCopy(BitConverter.GetBytes(W), 0, buffer, offset + 12, 4);
            return 16;
        }

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

        public UnityEngine.Quaternion ToUnityQuaternion() => new UnityEngine.Quaternion(X, Y, Z, W);

        public static QuaternionData FromUnityQuaternion(UnityEngine.Quaternion q) =>
            new QuaternionData(q.x, q.y, q.z, q.w);
    }

    /// <summary>
    /// 玩家加入消息 (客户端→服务端)
    /// </summary>
    public struct PlayerJoinMessage
    {
        public const int SIZE = 1 + 4;
        public int PlayerId;

        public int WriteTo(byte[] buffer, int offset)
        {
            buffer[offset] = (byte)KcpMessageType.PlayerJoin;
            Buffer.BlockCopy(BitConverter.GetBytes(PlayerId), 0, buffer, offset + 1, 4);
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
    /// </summary>
    public struct PlayerJoinAckMessage
    {
        public const int SIZE = 1 + 4 + 4;
        public int PlayerId;
        public uint ServerTick;

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
    /// </summary>
    public struct MovementInputMessage
    {
        public const int SIZE = 1 + 4 + 4 + 12 + 4 + 1 + 4; // 30 bytes
        public int PlayerId;
        public uint InputSequence;
        public Vector3Data Direction;
        public float MoveSpeed;
        public bool IsJumping;
        public float YawAngle;

        public int WriteTo(byte[] buffer, int offset)
        {
            int start = offset;
            buffer[offset] = (byte)KcpMessageType.MovementInput;
            offset += 1;
            Buffer.BlockCopy(BitConverter.GetBytes(PlayerId), 0, buffer, offset, 4);
            offset += 4;
            Buffer.BlockCopy(BitConverter.GetBytes(InputSequence), 0, buffer, offset, 4);
            offset += 4;
            offset += Direction.WriteTo(buffer, offset);
            Buffer.BlockCopy(BitConverter.GetBytes(MoveSpeed), 0, buffer, offset, 4);
            offset += 4;
            buffer[offset] = IsJumping ? (byte)1 : (byte)0;
            offset += 1;
            Buffer.BlockCopy(BitConverter.GetBytes(YawAngle), 0, buffer, offset, 4);
            offset += 4;
            return offset - start;
        }
    }

    /// <summary>
    /// 单个玩家的快照状态
    /// </summary>
    public struct PlayerSnapshotData
    {
        public const int SIZE = 4 + 12 + 16 + 12 + 1; // 45 bytes
        public int PlayerId;
        public Vector3Data Position;
        public QuaternionData Rotation;
        public Vector3Data Velocity;
        public bool IsGrounded;

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
    /// </summary>
    public struct WorldSnapshotMessage
    {
        public const int HEADER_SIZE = 1 + 4 + 8 + 2;
        public uint ServerTick;
        public long Timestamp;
        public PlayerSnapshotData[] Players;

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
    }

    /// <summary>
    /// 玩家离开消息 (服务端→客户端)
    /// </summary>
    public struct PlayerLeaveMessage
    {
        public const int SIZE = 1 + 4;
        public int PlayerId;

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
        public static KcpMessageType ParseType(byte[] buffer, int offset)
        {
            return (KcpMessageType)buffer[offset];
        }

        public static KcpMessageType ParseType(ArraySegment<byte> segment)
        {
            return (KcpMessageType)segment.Array[segment.Offset];
        }
    }
}
