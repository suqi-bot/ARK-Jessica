"""用 Real-ESRGAN（animevideov3）把走路序列帧做 2 倍超分，再补回透明通道。

流程：
1. 从视频抽 31 帧（起始帧 34），裁出统一包围盒 → 白底 RGB 小图 (600×649)
2. 交给 realesrgan-ncnn-vulkan 做 2× 超分 → 1200×1298
3. 用原图算出背景遮罩（flood fill 在 600 尺寸做，再最近邻放大），
   在放大图上只在边缘环里按"离白色多远"算 alpha，避免白边
4. 输出 RGBA 到 Assets
"""
import os
import struct
import subprocess
import zlib

import numpy as np

FFMPEG = r"C:\Users\苏\.workbuddy\binaries\python\envs\default\Lib\site-packages\imageio_ffmpeg\binaries\ffmpeg-win-x86_64-v7.1.exe"
ESRGAN = r"E:\unity\ARK-Jessica\Tools\esrgan\realesrgan-ncnn-vulkan.exe"
VIDEO = r"E:\unity\ARK-Jessica\Temp\walk.mp4"
TMP = r"E:\unity\ARK-Jessica\Temp\walk_esrgan"
OUT_DIR = r"E:\unity\ARK-Jessica\Assets\ArtRes\Jessica\VideoWalk"

START = 34
FRAMES = 31
SIZE = 720
WHITE = 232
PAD = 3
ESRGAN_SCALE = 4     # 先用 x4plus-anime 放大 4 倍，再缩回 2 倍，细节比直接 2 倍好
OUT_SCALE = 2


def write_png(path, arr):
    """arr: HxWx3 (RGB) 或 HxWx4 (RGBA), uint8"""
    h, w, c = arr.shape
    color_type = 6 if c == 4 else 2
    raw = b"".join(b"\x00" + arr[y].tobytes() for y in range(h))

    def chunk(tag, data):
        return (struct.pack(">I", len(data)) + tag + data
                + struct.pack(">I", zlib.crc32(tag + data) & 0xFFFFFFFF))

    png = b"\x89PNG\r\n\x1a\n"
    png += chunk(b"IHDR", struct.pack(">IIBBBBB", w, h, 8, color_type, 0, 0, 0))
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
        g = dilate(seed) & white
        if g.sum() == seed.sum():
            return seed
        seed = g


def main():
    if not os.path.exists(ESRGAN):
        raise SystemExit("找不到 realesrgan-ncnn-vulkan.exe")
    small_dir = os.path.join(TMP, "in")
    big_dir = os.path.join(TMP, "out")
    os.makedirs(small_dir, exist_ok=True)
    os.makedirs(big_dir, exist_ok=True)
    os.makedirs(OUT_DIR, exist_ok=True)

    # 1) 抽帧
    raw_path = os.path.join(TMP, "_walk.raw")
    subprocess.run([FFMPEG, "-hide_banner", "-loglevel", "error", "-y", "-i", VIDEO,
                    "-vf", "select='between(n\\,%d\\,%d)',scale=%d:%d" % (START, START + FRAMES - 1, SIZE, SIZE),
                    "-vsync", "0", "-f", "rawvideo", "-pix_fmt", "rgb24", raw_path], check=True)
    buf = np.fromfile(raw_path, dtype=np.uint8)
    n = buf.size // (SIZE * SIZE * 3)
    frames = buf[:n * SIZE * SIZE * 3].reshape(n, SIZE, SIZE, 3)

    # 2) 背景遮罩 + 统一裁剪
    masks = []
    for i in range(n):
        masks.append(~flood_bg(frames[i].min(axis=2) >= WHITE))
    masks = np.array(masks)
    ys, xs = np.where(masks.any(axis=0))
    y0 = max(0, ys.min() - PAD); x0 = max(0, xs.min() - PAD)
    y1 = min(SIZE - 1, ys.max() + PAD); x1 = min(SIZE - 1, xs.max() + PAD)
    h, w = y1 - y0 + 1, x1 - x0 + 1
    print("裁剪 %dx%d, 帧数 %d" % (w, h, n))

    for i in range(n):
        crop = frames[i, y0:y1 + 1, x0:x1 + 1]
        write_png(os.path.join(small_dir, "walk_%02d.png" % i), crop)

    # 3) ESRGAN 2×
    print("跑 Real-ESRGAN ...")
    r = subprocess.run([ESRGAN, "-i", small_dir, "-o", big_dir,
                        "-n", "realesrgan-x4plus-anime", "-s", str(ESRGAN_SCALE), "-f", "png"],
                       capture_output=True, text=True)
    if r.returncode != 0:
        print("ESRGAN 失败:", r.stderr[-800:])
        return
    print("ESRGAN 完成")

    # 4) 读回放大图（走 ffmpeg → raw，避免自己解 PNG）
    H, W = h * OUT_SCALE, w * OUT_SCALE
    big_raw = os.path.join(TMP, "_big.raw")
    subprocess.run([FFMPEG, "-hide_banner", "-loglevel", "error", "-y",
                    "-framerate", "24", "-i", os.path.join(big_dir, "walk_%02d.png"),
                    "-vf", "scale=%d:%d:flags=lanczos" % (W, H),
                    "-f", "rawvideo", "-pix_fmt", "rgb24", big_raw], check=True)
    bbuf = np.fromfile(big_raw, dtype=np.uint8)
    bn = bbuf.size // (H * W * 3)
    big = bbuf[:bn * H * W * 3].reshape(bn, H, W, 3).astype(np.float32)

    for i in range(min(n, bn)):
        m = masks[i, y0:y1 + 1, x0:x1 + 1]
        bg_big = np.repeat(np.repeat(~m, OUT_SCALE, axis=0), OUT_SCALE, axis=1)   # True=背景
        f = big[i]

        a = np.ones((H, W), dtype=np.float32)
        a[bg_big] = 0.0
        ring = dilate(dilate(bg_big)) & (~bg_big)
        soft = np.clip(1.0 - f.min(axis=2) / 255.0, 0.0, 1.0)
        a[ring] = np.clip(soft[ring] * 1.6, 0.15, 1.0)

        rgb = f.copy()
        m2 = (a > 0.02) & (a < 0.999)
        for c in range(3):
            ch = rgb[..., c]
            ch[m2] = np.clip((ch[m2] - (1.0 - a[m2]) * 255.0) / np.maximum(a[m2], 1e-3), 0, 255)

        # 收一圈：ESRGAN 在原本白底的位置会生成浅色描边，不收边会看到一圈白边
        for _ in range(2):
            a = np.minimum(np.minimum(np.roll(a, 1, 0), np.roll(a, -1, 0)),
                           np.minimum(np.roll(a, 1, 1), np.roll(a, -1, 1)))

        out = np.zeros((H, W, 4), dtype=np.uint8)
        out[..., :3] = rgb.astype(np.uint8)
        out[..., 3] = (np.clip((a - 0.5) * 1.2 + 0.5, 0, 1) * 255).astype(np.uint8)
        write_png(os.path.join(OUT_DIR, "walk_%02d.png" % i), out)

    print("输出 %d 帧 → %s (%dx%d)" % (min(n, bn), OUT_DIR, W, H))


if __name__ == "__main__":
    main()
