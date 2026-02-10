import os
import shutil

# 配置路径（请根据你的实际目录修改）
SERVER_SHARED_PATH = r"./server/Shared/Data"
UNITY_SHARED_PATH = r"./client/Matrix/Assets/Scripts/Data"

def sync():
    if not os.path.exists(UNITY_SHARED_PATH):
        os.makedirs(UNITY_SHARED_PATH)

    for filename in os.listdir(SERVER_SHARED_PATH):
        # 只同步 .cs 文件
        if filename.endswith(".cs"):
            src = os.path.join(SERVER_SHARED_PATH, filename)
            dst = os.path.join(UNITY_SHARED_PATH, filename)
            shutil.copy2(src, dst)
            print(f"Synced: {filename}\nsrc {src} dst {dst}")

if __name__ == "__main__":
    sync()
    print("Entity 同步完成！")
