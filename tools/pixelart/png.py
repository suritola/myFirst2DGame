# 외부 라이브러리 없이 PNG(RGBA 8비트, 비인터레이스)를 읽고 쓰는 작은 도구
# 이미지 = Img(w, h) · px[y][x] = (r, g, b, a)   (y = 0 이 맨 위)
import struct, zlib


class Img:
    def __init__(self, w, h, fill=(0, 0, 0, 0)):
        self.w, self.h = w, h
        self.px = [[fill for _ in range(w)] for _ in range(h)]

    def get(self, x, y):
        if 0 <= x < self.w and 0 <= y < self.h:
            return self.px[y][x]
        return (0, 0, 0, 0)

    def set(self, x, y, c):
        if 0 <= x < self.w and 0 <= y < self.h:
            if len(c) == 3:
                c = (c[0], c[1], c[2], 255)
            self.px[y][x] = c

    def blend(self, x, y, c):
        # c 의 알파만큼 덮어 칠함
        if not (0 <= x < self.w and 0 <= y < self.h):
            return
        r, g, b, a = self.px[y][x]
        cr, cg, cb, ca = c if len(c) == 4 else (c[0], c[1], c[2], 255)
        k = ca / 255.0
        na = max(a, ca)
        self.px[y][x] = (round(r + (cr - r) * k), round(g + (cg - g) * k), round(b + (cb - b) * k), na)

    def rect(self, x0, y0, w, h, c):
        for y in range(y0, y0 + h):
            for x in range(x0, x0 + w):
                self.set(x, y, c)

    def paste(self, other, ox, oy):
        for y in range(other.h):
            for x in range(other.w):
                c = other.px[y][x]
                if c[3] > 0:
                    self.blend(ox + x, oy + y, c)

    def from_rows(rows, palette):
        # 글자 한 칸 = 픽셀 한 칸, palette[글자] = 색 ('.' 은 투명)
        img = Img(len(rows[0]), len(rows))
        for y, row in enumerate(rows):
            for x, ch in enumerate(row):
                if ch in palette:
                    img.set(x, y, palette[ch])
        return img
    from_rows = staticmethod(from_rows)


def _paeth(a, b, c):
    p = a + b - c
    pa, pb, pc = abs(p - a), abs(p - b), abs(p - c)
    if pa <= pb and pa <= pc:
        return a
    return b if pb <= pc else c


def read(path):
    data = open(path, 'rb').read()
    assert data[:8] == b'\x89PNG\r\n\x1a\n', path
    pos, idat, w = 8, b'', 0
    plte, trns = None, None
    while pos < len(data):
        n, typ = struct.unpack('>I4s', data[pos:pos + 8])
        body = data[pos + 8:pos + 8 + n]
        pos += 12 + n
        if typ == b'IHDR':
            w, h, depth, ctype, _, _, inter = struct.unpack('>IIBBBBB', body)
            assert depth == 8 and inter == 0, (path, depth, inter)
        elif typ == b'PLTE':
            plte = [tuple(body[i:i + 3]) for i in range(0, len(body), 3)]
        elif typ == b'tRNS':
            trns = body
        elif typ == b'IDAT':
            idat += body
    ch = {6: 4, 2: 3, 0: 1, 4: 2, 3: 1}[ctype]
    raw = zlib.decompress(idat)
    stride = w * ch
    img = Img(w, h)
    prev = bytearray(stride)
    i = 0
    for y in range(h):
        f = raw[i]; i += 1
        line = bytearray(raw[i:i + stride]); i += stride
        for x in range(stride):
            a = line[x - ch] if x >= ch else 0
            b = prev[x]
            c = prev[x - ch] if x >= ch else 0
            if f == 1: line[x] = (line[x] + a) & 255
            elif f == 2: line[x] = (line[x] + b) & 255
            elif f == 3: line[x] = (line[x] + (a + b) // 2) & 255
            elif f == 4: line[x] = (line[x] + _paeth(a, b, c)) & 255
        for x in range(w):
            p = line[x * ch:(x + 1) * ch]
            if ctype == 6: c = tuple(p)
            elif ctype == 2: c = (p[0], p[1], p[2], 255)
            elif ctype == 0: c = (p[0], p[0], p[0], 255)
            elif ctype == 4: c = (p[0], p[0], p[0], p[1])
            else:
                r, g, bb = plte[p[0]]
                c = (r, g, bb, trns[p[0]] if trns and p[0] < len(trns) else 255)
            img.px[y][x] = c
        prev = line
    return img


def write(img, path):
    raw = bytearray()
    for y in range(img.h):
        raw.append(0)
        for x in range(img.w):
            raw.extend(bytes(img.px[y][x]))

    def chunk(t, b):
        return struct.pack('>I', len(b)) + t + b + struct.pack('>I', zlib.crc32(t + b) & 0xffffffff)

    out = b'\x89PNG\r\n\x1a\n'
    out += chunk(b'IHDR', struct.pack('>IIBBBBB', img.w, img.h, 8, 6, 0, 0, 0))
    out += chunk(b'IDAT', zlib.compress(bytes(raw), 9))
    out += chunk(b'IEND', b'')
    open(path, 'wb').write(out)
