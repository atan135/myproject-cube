using UnityEngine;

namespace Cube.Game.Maze
{
    /// <summary>
    /// 迷宫生成器
    /// 负责程序化生成立方体迷宫
    /// </summary>
    public class MazeGenerator : MonoBehaviour
    {
        [Header("Maze Settings")]
        public int mazeSize = 10;
        public GameObject roomPrefab;

        private void Start()
        {
            GenerateMaze();
        }

        /// <summary>
        /// 生成迷宫
        /// </summary>
        public void GenerateMaze()
        {
            // 迷宫生成逻辑将在后续实现
            Debug.Log("Maze generation started");
        }
    }
}
