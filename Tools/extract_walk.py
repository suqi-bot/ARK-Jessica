"""从参考视频抽取走路循环 → 白底抠透明 → 输出等尺寸序列帧 PNG。

关键改进：
1. 起始帧用自相关搜出来的最佳接缝（start=34），不再从第 0 帧开始；
2. 用视频原始 720 分辨率抽取，不再缩到 480，细节更实；
3. 所有帧共用同一个裁剪框，保证播放时不抖。

用法： python extract_walk.py
"""
import os
import struct
import subprocess
import zlib

import numpy as np

FFMPEG = r"C:\Users\苏\.workbuddy\binaries\python\envs\default\Lib\site-packages\imageio_ffmpeg\binaries\ffmpeg-win-x86_64-v7.1.exe"
VIDEO = r"E:\unity\ARK-Jessica\Temp\walk.mp4"
OUT_DIR = r"E:\unity\ARK-Jessica\Assets\ArtRes\Jessica\VideoWalk"

START = 34          # 最佳接缝起点（analyze_walk.py 搜出来的）
FRAMES = 31         # 一个完整步幅
SIZE = 720          # 视频原生分辨率
WHITE = 232
PAD = 3


def write_png(path, rgba):
    h, w, _ = rgba.shape
    raw = b"".join(b"\x00" + rgba[y].tobytes() for y in range(h))

    def chunk(tag, data):
        return (struct.pack(">I", len(data)) + tag + data
                + struct.pack(">I", zlib.crc32(tag + data) & 0xFFFFFFFF))

    png = b"\x89PNG\r\n\x1a\n"
    png += chunk(b"IHDR", struct.pack(">IIBBBBB", w, h, 8, 6, 0, 0, 0))
    png += chunk(b"IDAT", zlib.compress(raw, 6))
    png += chunk(b"IEND", b"")
    with open(path, "wb") as f:
        f.write(png)


def dilate(m):
    d = m.copy()
    d[1:, :] |= m[:-1, :]
    d[:-1, :] |= m[1:, :]
    d[:, 1:] |= m[:, :-1]
    d[:, :-1] |= m[:, 1:]
    return d


def flood_bg(white):
    seed = np.zeros_like(white)
    seed[0, :] = white[0, :]
    seed[-1, :] = white[-1, :]
    seed[:, 0] = white[:, 0]
    seed[:, -1] = white[:, -1]
    while True:
        grown = dilate(seed) & white
        if grown.sum() == seed.sum():
            return seed
        seed = grown


def main():
    os.makedirs(OUT_DIR, exist_ok=True)
    raw_path = os.path.join(os.path.dirname(OUT_DIR), "_walk.raw")

    # 直接跳到 START 帧开始取，避免在 Python 侧再切片
    cmd = [FFMPEG, "-hide_banner", "-loglevel", "error", "-y",
           "-i", VIDEO,
           "-vf", "select='between(n\\,%d\\,%d)',scale=%d:%d" % (START, START + FRAMES - 1, SIZE, SIZE),
           "-vsync", "0",
           "-f", "rawvideo", "-pix_fmt", "rgb24", raw_path]
    subprocess.run(cmd, check=True)

    buf = np.fromfile(raw_path, dtype=np.uint8)
    n = buf.size // (SIZE * SIZE * 3)
    frames = buf[:n * SIZE * SIZE * 3].reshape(n, SIZE, SIZE, 3).astype(np.float32)
    print("取到帧数", n, "起始帧", START)

    alphas = np.zeros((n, SIZE, SIZE), dtype=np.float32)
    rgbs = np.zeros((n, SIZE, SIZE, 3), dtype=np.uint8)

    for i in range(n):
        f = frames[i]
        white = (f.min(axis=2) >= WHITE)
        bg = flood_bg(white)
        soft = np.clip(1.0 - f.min(axis=2) / 255.0, 0.0, 1.0)
        ring = dilate(dilate(bg)) & (~bg)
        a = np.ones((SIZE, SIZE), dtype=np.float32)
        a[ring] = np.clip(soft[ring] * 1.6, 0.15, 1.0)
        a[bg] = 0.0
        alphas[i] = a

        rgb = f.copy()
        m = (a > 0.02) & (a < 0.999)
        for c in range(3):
            ch = rgb[..., c]
            ch[m] = np.clip((ch[m] - (1.0 - a[m]) * 255.0) / np.maximum(a[m], 1e-3), 0, 255)
        rgbs[i] = rgb.astype(np.uint8)

    ys, xs = np.where(alphas.max(axis=0) > 0.02)
    y0 = max(0, ys.min() - PAD); x0 = max(0, xs.min() - PAD)
    y1 = min(SIZE - 1, ys.max() + PAD); x1 = min(SIZE - 1, xs.max() + PAD)
    h = y1 - y0 + 1
    w = x1 - x0 + 1
    print("裁剪框 %dx%d" % (w, h))

    for i in range(n):
        rgba = np.zeros((h, w, 4), dtype=np.uint8)
        rgba[..., :3] = rgbs[i, y0:y1 + 1, x0:x1 + 1]
        rgba[..., 3] = (alphas[i, y0:y1 + 1, x0:x1 + 1] * 255).astype(np.uint8)
        write_png(os.path.join(OUT_DIR, "walk_%02d.png" % i), rgba)

    if os.path.exists(raw_path):
        os.remove(raw_path)
    print("输出 %d 帧 -> %s  (高度 %d px)" % (n, OUT_DIR, h))


if __name__ == "__main__":
    main()
