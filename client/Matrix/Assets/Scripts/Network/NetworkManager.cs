using UnityEngine;
using System;

namespace Cube.Network
{
    /// <summary>
    /// 网络管理器
    /// 负责与服务器通信、消息处理、连接管理
    /// </summary>
    public class NetworkManager : MonoBehaviour
    {
        private static NetworkManager _instance;
        public static NetworkManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<NetworkManager>();
                }
                return _instance;
            }
        }

        [Header("Network Settings")]
        public string serverAddress = "localhost";
        public int serverPort = 8080;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }

        /// <summary>
        /// 连接到服务器
        /// </summary>
        public void ConnectToServer()
        {
            Debug.Log($"Connecting to server: {serverAddress}:{serverPort}");
            // 连接逻辑将在后续实现
        }

        /// <summary>
        /// 断开连接
        /// </summary>
        public void Disconnect()
        {
            Debug.Log("Disconnecting from server");
            // 断开逻辑将在后续实现
        }

        /// <summary>
        /// 发送消息
        /// </summary>
        public void SendMessage(byte[] data)
        {
            // 消息发送逻辑将在后续实现
        }
    }
}
