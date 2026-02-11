using System.Collections.Generic;
using UnityEngine;

namespace Cube.Network.KcpMovement
{
    /// <summary>
    /// 快照插值时间戳数据
    /// 用于在两个快照之间进行平滑插值
    /// </summary>
    public struct Snapshot
    {
        public double Timestamp;         // 本地接收时间 (Time.timeAsDouble)
        public uint ServerTick;          // 服务端 tick
        public Vector3 Position;
        public Quaternion Rotation;
        public Vector3 Velocity;
        public bool IsGrounded;
    }

    /// <summary>
    /// 快照插值器
    /// 实现基于时间的快照插值，用于平滑显示远程玩家的位置
    ///
    /// 原理：
    /// 1. 缓存最近 N 个快照
    /// 2. 在渲染时，取当前时间 - 插值延迟 作为目标时间
    /// 3. 找到目标时间前后的两个快照，进行线性插值
    /// 4. 这样即使有网络抖动，显示效果也是平滑的
    ///
    /// 插值延迟：默认 2 帧快照间隔（100ms @ 20Hz），可配置
    /// </summary>
    public class SnapshotInterpolation
    {
        // 快照缓冲区
        private readonly List<Snapshot> _buffer = new List<Snapshot>();

        // 配置
        private readonly int _bufferSize;
        private readonly double _interpolationDelay; // 插值延迟（秒）

        // 当前插值结果
        public Vector3 CurrentPosition { get; private set; }
        public Quaternion CurrentRotation { get; private set; }
        public Vector3 CurrentVelocity { get; private set; }
        public bool CurrentIsGrounded { get; private set; }

        /// <summary>缓冲区中的快照数量</summary>
        public int BufferCount => _buffer.Count;

        /// <summary>
        /// 创建快照插值器
        /// </summary>
        /// <param name="interpolationDelaySeconds">插值延迟（秒），默认0.1s = 2帧@20Hz</param>
        /// <param name="bufferSize">快照缓冲区大小，默认30</param>
        public SnapshotInterpolation(double interpolationDelaySeconds = 0.1, int bufferSize = 30)
        {
            _interpolationDelay = interpolationDelaySeconds;
            _bufferSize = bufferSize;
        }

        /// <summary>
        /// 添加新快照到缓冲区
        /// </summary>
        public void AddSnapshot(Snapshot snapshot)
        {
            // 保持按时间排序
            _buffer.Add(snapshot);

            // 限制缓冲区大小，移除最旧的
            while (_buffer.Count > _bufferSize)
            {
                _buffer.RemoveAt(0);
            }
        }

        /// <summary>
        /// 更新插值（每帧调用）
        /// </summary>
        /// <param name="currentTime">当前时间 (Time.timeAsDouble)</param>
        public void Update(double currentTime)
        {
            if (_buffer.Count == 0) return;

            // 目标渲染时间 = 当前时间 - 插值延迟
            // 这样我们总是在过去的时间点上渲染，有足够的缓冲来处理抖动
            double renderTime = currentTime - _interpolationDelay;

            // 只有一个快照：直接使用
            if (_buffer.Count == 1)
            {
                var snap = _buffer[0];
                CurrentPosition = snap.Position;
                CurrentRotation = snap.Rotation;
                CurrentVelocity = snap.Velocity;
                CurrentIsGrounded = snap.IsGrounded;
                return;
            }

            // 找到 renderTime 前后的两个快照进行插值
            // 快照按时间排序（添加顺序即时间顺序）
            Snapshot? from = null;
            Snapshot? to = null;

            for (int i = 0; i < _buffer.Count - 1; i++)
            {
                if (_buffer[i].Timestamp <= renderTime && _buffer[i + 1].Timestamp >= renderTime)
                {
                    from = _buffer[i];
                    to = _buffer[i + 1];
                    break;
                }
            }

            // 如果 renderTime 在所有快照之前，使用最早的快照
            if (from == null && renderTime < _buffer[0].Timestamp)
            {
                var snap = _buffer[0];
                CurrentPosition = snap.Position;
                CurrentRotation = snap.Rotation;
                CurrentVelocity = snap.Velocity;
                CurrentIsGrounded = snap.IsGrounded;
                return;
            }

            // 如果 renderTime 在所有快照之后，使用最新的快照 + 外推
            if (from == null)
            {
                var latest = _buffer[_buffer.Count - 1];
                double timeSinceLatest = renderTime - latest.Timestamp;

                // 简单外推：位置 + 速度 * 时间差（最多外推0.2秒）
                if (timeSinceLatest < 0.2)
                {
                    CurrentPosition = latest.Position + latest.Velocity * (float)timeSinceLatest;
                }
                else
                {
                    CurrentPosition = latest.Position;
                }
                CurrentRotation = latest.Rotation;
                CurrentVelocity = latest.Velocity;
                CurrentIsGrounded = latest.IsGrounded;
                return;
            }

            // 正常插值
            double duration = to.Value.Timestamp - from.Value.Timestamp;
            float t = duration > 0 ? (float)((renderTime - from.Value.Timestamp) / duration) : 0f;
            t = Mathf.Clamp01(t);

            CurrentPosition = Vector3.Lerp(from.Value.Position, to.Value.Position, t);
            CurrentRotation = Quaternion.Slerp(from.Value.Rotation, to.Value.Rotation, t);
            CurrentVelocity = Vector3.Lerp(from.Value.Velocity, to.Value.Velocity, t);
            CurrentIsGrounded = t < 0.5f ? from.Value.IsGrounded : to.Value.IsGrounded;
        }

        /// <summary>
        /// 清空缓冲区
        /// </summary>
        public void Clear()
        {
            _buffer.Clear();
        }
    }
}
