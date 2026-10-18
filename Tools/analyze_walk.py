"""分析参考视频的走路循环：找最佳接缝、量角色漂移。"""
import subprocess

import numpy as np

FFMPEG = r"C:\Users\苏\.workbuddy\binaries\python\envs\default\Lib\site-packages\imageio_ffmpeg\binaries\ffmpeg-win-x86_64-v7.1.exe"
VIDEO = r"E:\unity\ARK-Jessica\Temp\walk.mp4"
SIZE = 320
WHITE = 232


def dilate(m):
    d = m.copy()
    d[1:, :] |= m[:-1, :]
    d[:-1, :] |= m[1:, :]
    d[:, 1:] |= m[:, :-1]
    d[:, :-1] |= m[:, 1:]
    return d


def flood(white):
    seed = np.zeros_like(white)
    seed[0, :] = white[0, :]
    seed[-1, :] = white[-1, :]
    seed[:, 0] = white[:, 0]
    seed[:, -1] = white[:, -1]
    while True:
        g = dilate(seed) & white
        if g.sum() == seed.sum():
            return seed
        seed = g


def main():
    raw = r"E:\unity\ARK-Jessica\Temp\_walk_analysis.raw"
    cmd = [FFMPEG, "-hide_banner", "-loglevel", "error", "-y", "-i", VIDEO,
           "-vf", "scale=%d:%d" % (SIZE, SIZE), "-f", "rawvideo", "-pix_fmt", "rgb24", raw]
    subprocess.run(cmd, check=True)
    buf = np.fromfile(raw, dtype=np.uint8)
    n = buf.size // (SIZE * SIZE * 3)
    f = buf[:n * SIZE * SIZE * 3].reshape(n, SIZE, SIZE, 3).astype(np.float32)
    print("帧数", n)

    masks = []
    for i in range(n):
        bg = flood(f[i].min(axis=2) >= WHITE)
        masks.append(~bg)
    masks = np.array(masks)

    # 每帧不透明区域的包围盒 + 脚底位置（最下面一行不透明像素）
    info = []
    for i in range(n):
        ys, xs = np.where(masks[i])
        info.append((xs.min(), xs.max(), ys.min(), ys.max(), xs.mean(), ys.max()))
    info = np.array(info)
    print("脚底 y: min=%.1f max=%.1f 漂移=%.1f px" % (info[:, 5].min(), info[:, 5].max(), info[:, 5].max() - info[:, 5].min()))
    print("水平中心 x: min=%.1f max=%.1f 漂移=%.1f px" % (info[:, 4].min(), info[:, 4].max(), info[:, 4].max() - info[:, 4].min()))

    # 找最佳接缝：f[s] 与 f[s+L] 的像素差（只比人物遮罩区域）
    print("\n最佳循环段（差值越小越接近无缝）:")
    cands = []
    for L in range(22, 41):
        for s in range(0, n - L - 1):
            a, b = f[s], f[s + L]
            m = masks[s] | masks[s + L]
            d = np.abs(a - b)[m].mean()
            cands.append((d, s, L))
    cands.sort()
    for d, s, L in cands[:10]:
        print("  start=%3d  len=%2d  diff=%.2f" % (s, L, d))


if __name__ == "__main__":
    main()
