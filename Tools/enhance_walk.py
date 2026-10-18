"""从参考视频抽走路循环，抠白底，然后做锐化 + 2 倍放大，缓解"糊"。

糊的三个来源都在这处理：
1. 视频只有 720×720 且码率低（766kbps），本身边缘就软、有压缩块
2. 缩放会进一步糊
3. 透明区残留的白色像素会在放大/锐化时被吸到边缘，形成白边

做法：
- 先把透明区域的 RGB 用邻近不透明像素填满，再放大 → 不会出现白边
- 2× 双线性放大后做 unsharp mask（锐化）
- alpha 走一条对比曲线，让轮廓更干脆

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

START = 34
FRAMES = 31
SIZE = 720
SCALE = 2          # 输出放大倍数
WHITE = 232
PAD = 3
SHARPEN = 0.62     # unsharp 强度
EDGE = 1.15         # alpha 对比强度


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
        g = dilate(seed) & white
        if g.sum() == seed.sum():
            return seed
        seed = g


def box3(img):
    """3×3 均值模糊（用移位相加，比卷积快）。img: HxWxC float"""
    p = np.pad(img, ((1, 1), (1, 1), (0, 0)), mode="edge")
    acc = np.zeros_like(img)
    for dy in (0, 1, 2):
        for dx in (0, 1, 2):
            acc += p[dy:dy + img.shape[0], dx:dx + img.shape[1]]
    return acc / 9.0


def _cubic_w(f):
    f2 = f * f
    f3 = f2 * f
    w0 = -0.5 * f3 + f2 - 0.5 * f
    w1 = 1.5 * f3 - 2.5 * f2 + 1.0
    w2 = -1.5 * f3 + 2.0 * f2 + 0.5 * f
    w3 = 0.5 * f3 - 0.5 * f2
    return w0, w1, w2, w3


def resize_bicubic(img, out_h, out_w):
    """Catmull-Rom 双三次放大，比双线性锐利，放大后再锐化不容易出毛边。"""
    h, w = img.shape[:2]
    ys = np.linspace(0, h - 1, out_h)
    xs = np.linspace(0, w - 1, out_w)
    yi = np.floor(ys).astype(np.int32)
    xi = np.floor(xs).astype(np.int32)
    wy = _cubic_w(ys - yi)
    wx = _cubic_w(xs - xi)

    def clamp(v, lo, hi):
        return np.clip(v, lo, hi)

    # 先按行插值
    yidx = [clamp(yi + k, 0, h - 1) for k in (-1, 0, 1, 2)]
    rows = np.zeros((out_h, w) + img.shape[2:], dtype=np.float32)
    for k in range(4):
        rows += wy[k][:, None, None] * img[yidx[k]] if img.ndim == 3 else wy[k][:, None] * img[yidx[k]]

    # 再按列插值
    xidx = [clamp(xi + k, 0, w - 1) for k in (-1, 0, 1, 2)]
    out = np.zeros((out_h, out_w) + img.shape[2:], dtype=np.float32)
    for k in range(4):
        if img.ndim == 3:
            out += wx[k][None, :, None] * rows[:, xidx[k]]
        else:
            out += wx[k][None, :] * rows[:, xidx[k]]
    return np.clip(out, 0, 255) if img.max() > 1.5 else np.clip(out, 0, 1)


def premultiply(rgb, a):
    return rgb * a[..., None]


def unpremultiply(pm, a):
    return np.clip(pm / np.maximum(a[..., None], 0.004), 0, 255)


def main():
    os.makedirs(OUT_DIR, exist_ok=True)
    raw_path = os.path.join(os.path.dirname(OUT_DIR), "_walk.raw")

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
    rgbs = np.zeros((n, SIZE, SIZE, 3), dtype=np.float32)

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
        rgbs[i] = rgb

    ys, xs = np.where(alphas.max(axis=0) > 0.02)
    y0 = max(0, ys.min() - PAD); x0 = max(0, xs.min() - PAD)
    y1 = min(SIZE - 1, ys.max() + PAD); x1 = min(SIZE - 1, xs.max() + PAD)
    print("裁剪框 %dx%d" % (x1 - x0 + 1, y1 - y0 + 1))

    for i in range(n):
        rgb = rgbs[i, y0:y1 + 1, x0:x1 + 1]
        a = alphas[i, y0:y1 + 1, x0:x1 + 1]
        h, w = a.shape

        # 预乘 alpha 之后再放大，避免透明区的颜色被吸到边缘形成白边
        pm = premultiply(rgb, a)
        # 先轻压一下压缩噪点（视频码率低，直接锐化会把马赛克一起放大）
        pm = 0.6 * pm + 0.4 * box3(pm)
        up_pm = resize_bicubic(pm, h * SCALE, w * SCALE)
        up_a = resize_bicubic(a, h * SCALE, w * SCALE)

        # 在预乘图上做 unsharp，锐化不会带出杂色
        blur = box3(up_pm)
        sharpened = np.clip(up_pm + SHARPEN * (up_pm - blur), 0, 255)
        # 只在实心区域锐化，边缘留原样，避免出现白/亮边
        solid = (up_a > 0.92)[..., None]
        up_pm = np.where(solid, sharpened, up_pm)

        up_rgb = unpremultiply(up_pm, up_a)

        # alpha 对比曲线：边界更干脆，减少"发虚"
        up_a = np.clip((up_a - 0.5) * EDGE + 0.5, 0.0, 1.0)

        out = np.zeros((h * SCALE, w * SCALE, 4), dtype=np.uint8)
        out[..., :3] = up_rgb.astype(np.uint8)
        out[..., 3] = (up_a * 255).astype(np.uint8)
        write_png(os.path.join(OUT_DIR, "walk_%02d.png" % i), out)

    if os.path.exists(raw_path):
        os.remove(raw_path)
    print("输出 %d 帧 -> %s  (%dx%d)" % (n, OUT_DIR, w * SCALE, h * SCALE))


if __name__ == "__main__":
    main()
