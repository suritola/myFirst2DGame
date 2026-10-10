# 하이라이트 영상(record.ps1 -Mode highlight)용 음악: 긴박하고 웅장한 오케스트라풍 (직접 합성 → 저작권 걱정 없음)
# 140 BPM · 라단조 (Dm - Bb - Gm - A), 타이코 북 · 현악 스타카토 · 금관 화음 · 합창 · 낮은 지속음
# 영상 장면이 바뀌는 순간 · 필살기가 터지는 순간 · 결계가 깨지는 순간 · 로고에 충격음을 맞춤 (GameplayCapture.HighlightReel 의 프레임 수 기준)
# 실행: py tools/trailer/make_trailer_music.py <출력 wav>   (48kHz 스테레오 16비트)
import sys, wave
import numpy as np

SR = 48000
DUR = 44.5
N = int(SR * DUR)
rng = np.random.default_rng(7)
L = np.zeros(N)
R = np.zeros(N)

BPM = 140.0
BEAT = 60.0 / BPM
BAR = BEAT * 4

# 영상 타이밍 (초): 장면이 바뀌는 순간 · 필살기가 터지는 순간 · 결계가 깨지는 순간 · 로고
CUTS = [2.87, 6.37, 9.77, 15.47, 19.27, 22.57, 26.27, 31.97, 35.47]
ULTS = [0.6, 2.87 + 1.27, 6.37 + 1.17, 15.47 + 1.57, 19.27 + 1.07, 22.57 + 1.47, 31.97 + 1.27, 35.47 + 1.27]
BREAKS = [9.77 + 3.47, 26.27 + 3.47]
LOGO = 39.3


def midi(n):
    return 440.0 * 2 ** ((n - 69) / 12.0)


def put(sig, start, gain=1.0, pan=0.0):
    i0 = int(start * SR)
    if i0 >= N or i0 + len(sig) <= 0:
        return
    s = sig[: max(0, N - i0)] * gain
    gl, gr = np.sqrt(0.5 * (1 - pan)), np.sqrt(0.5 * (1 + pan))
    L[i0:i0 + len(s)] += s * gl
    R[i0:i0 + len(s)] += s * gr


def tt(sec):
    return np.arange(int(sec * SR)) / SR


def env(sec, a, r, sustain=1.0):
    t = tt(sec)
    e = np.minimum(1.0, t / max(a, 1e-4)) * sustain
    tail = np.clip((sec - t) / max(r, 1e-4), 0.0, 1.0)
    return e * tail


def lowpass(x, cutoff):
    # FFT 로 간단히 깎음 (짧은 소리용)
    X = np.fft.rfft(x)
    f = np.fft.rfftfreq(len(x), 1 / SR)
    X *= 1.0 / (1.0 + (f / cutoff) ** 4)
    return np.fft.irfft(X, len(x))


def highpass(x, cutoff):
    return x - lowpass(x, cutoff)


def saw(freq, sec, harm=12, rolloff=1.0, detune=0.0):
    t = tt(sec)
    s = np.zeros_like(t)
    for k in range(1, harm + 1):
        if freq * k > SR * 0.45:
            break
        s += np.sin(2 * np.pi * freq * k * (1 + detune) * t + k * 0.3) / k ** rolloff
    return s


# ---------------- 악기
def taiko(big=1.0):
    sec = 0.9
    t = tt(sec)
    f = 46 + 70 * np.exp(-t * 18)
    body = np.sin(2 * np.pi * np.cumsum(f) / SR) * np.exp(-t * 4.5)
    skin = lowpass(rng.standard_normal(len(t)), 900) * np.exp(-t * 28) * 0.8
    return (body * 1.4 + skin) * big


def tom(pitch=1.0):
    sec = 0.35
    t = tt(sec)
    f = (120 + 90 * np.exp(-t * 25)) * pitch
    body = np.sin(2 * np.pi * np.cumsum(f) / SR) * np.exp(-t * 11)
    hit = lowpass(rng.standard_normal(len(t)), 2500) * np.exp(-t * 40) * 0.5
    return body + hit


def snare():
    sec = 0.25
    t = tt(sec)
    n = highpass(rng.standard_normal(len(t)), 1500) * np.exp(-t * 22)
    return n * 0.7 + np.sin(2 * np.pi * 190 * t) * np.exp(-t * 30) * 0.4


def crash(sec=2.6):
    t = tt(sec)
    n = highpass(rng.standard_normal(len(t)), 4000)
    return n * np.exp(-t * 2.2) * 0.55


def boom(size=1.0):
    # 트레일러 충격음: 아주 낮게 떨어지는 사인 + 묵직한 타격 + 심벌
    sec = 3.2
    t = tt(sec)
    f = 28 + 60 * np.exp(-t * 6)
    sub = np.sin(2 * np.pi * np.cumsum(f) / SR) * np.exp(-t * 1.4)
    thud = lowpass(rng.standard_normal(len(t)), 300) * np.exp(-t * 9)
    out = np.tanh((sub * 1.6 + thud * 1.2) * 1.5) * size
    c = crash(sec)
    out[: len(c)] += c * 0.9 * size
    return out


def riser(sec):
    # 장면 앞에 차오르는 소리: 거꾸로 커지는 바람 + 올라가는 음
    t = tt(sec)
    k = (t / sec) ** 2.2
    n = lowpass(rng.standard_normal(len(t)), 6000) * k * 0.6
    f = 200 + 1400 * k
    tone = np.sin(2 * np.pi * np.cumsum(f) / SR) * k * 0.25
    return n + tone


def string_note(note, sec=BEAT / 4 * 0.9):
    s = saw(midi(note), sec, harm=14, rolloff=1.1) + saw(midi(note), sec, harm=14, rolloff=1.1, detune=0.004)
    return s * env(sec, 0.006, 0.04) * 0.35


def brass(notes, sec):
    out = np.zeros(int(sec * SR))
    for n in notes:
        for d in (-0.003, 0.003):
            out += saw(midi(n), sec, harm=16, rolloff=1.25, detune=d)
    e = env(sec, 0.035, 0.25)
    # 금관다운 숨: 앞이 살짝 부풀었다 가라앉음
    e *= 1.0 + 0.4 * np.exp(-tt(sec) * 8)
    return out * e / len(notes) * 0.9


def choir(notes, sec):
    t = tt(sec)
    out = np.zeros_like(t)
    for j, n in enumerate(notes):
        for k, g in ((1, 1.0), (2, 0.5), (3, 0.3), (4, 0.15)):
            vib = 1 + 0.004 * np.sin(2 * np.pi * (4.5 + j * 0.3) * t + j)
            out += g * np.sin(2 * np.pi * midi(n) * k * vib * t + j)
    return out * env(sec, 0.6, 0.8) / len(notes) * 0.5


def drone(note, sec):
    t = tt(sec)
    return (np.sin(2 * np.pi * midi(note) * t) + 0.5 * np.sin(2 * np.pi * midi(note + 12) * t)) * env(sec, 1.5, 2.0) * 0.5


# ---------------- 화성 (한 마디에 하나): Dm - Bb - Gm - A
CHORDS = [[50, 53, 57, 62], [46, 50, 53, 58], [43, 50, 55, 58], [45, 49, 52, 57]]
ROOTS = [38, 34, 31, 33]
OSTINATO = [0, 0, 12, 0, 7, 0, 3, 0, 0, 0, 12, 0, 7, 3, 2, 0]


def section(t0, t1):
    return lambda x: t0 <= x < t1


intro = section(0.0, 6.37)
main = section(6.37, 26.27)
calm = section(26.27, BREAKS[1])
climax = section(BREAKS[1], LOGO)

# 낮은 지속음 (처음부터 로고 뒤까지)
put(drone(26, LOGO + 5.0), 0.0, 0.8)

bar = 0
tb = 0.0
while tb < LOGO:
    ci = bar % 4
    root = ROOTS[ci]
    chord = CHORDS[ci]
    # 현악 스타카토 16분음표
    for i, step in enumerate(OSTINATO):
        x = tb + i * BEAT / 4
        if x >= LOGO:
            break
        lvl = 0.45 + 0.55 * min(1.0, x / 6.37) if intro(x) else (0.6 if calm(x) else 1.0)
        put(string_note(root + 12 + step), x, lvl * (1.15 if i % 4 == 0 else 0.85), pan=-0.35)
        if climax(x) or main(x):
            put(string_note(root + 24 + step), x, lvl * 0.45, pan=0.35)
    # 타이코 · 톰
    for b in range(4):
        x = tb + b * BEAT
        if x >= LOGO:
            break
        if intro(x):
            if b in (0, 2):
                put(taiko(0.8), x, 0.9)
        elif calm(x):
            if b == 0:
                put(taiko(0.9), x, 0.8)
        else:
            put(taiko(1.0), x, 1.0)
            if b in (1, 3):
                put(snare(), x, 0.6, pan=0.1)
            put(tom(1.0), x + BEAT * 0.5, 0.55, pan=-0.2)
            if climax(x):
                put(tom(1.3), x + BEAT * 0.25, 0.45, pan=0.25)
                put(tom(0.8), x + BEAT * 0.75, 0.5, pan=-0.25)
    # 금관 화음 · 합창
    if main(tb) or climax(tb):
        put(brass(chord, BEAT * 1.5), tb, 1.0, pan=0.15)
        put(brass(chord, BEAT * 0.6), tb + BEAT * 2.5, 0.7, pan=0.15)
        if climax(tb):
            put(brass([n + 12 for n in chord[1:]], BEAT * 1.0), tb + BEAT * 2, 0.6, pan=-0.15)
    if not intro(tb):
        put(choir([n + 12 for n in chord], BAR + 0.4), tb, 0.9 if climax(tb) else 0.6)
    tb += BAR
    bar += 1

# ---------------- 영상에 맞춘 충격음
put(riser(2.8), 6.37 - 2.8, 0.9)                       # 본편 진입
for c in CUTS:
    put(boom(0.75), c, 0.9)
for u in ULTS:
    put(taiko(1.3), u, 1.0)
    put(crash(1.6), u, 0.5, pan=0.3)
for b in BREAKS:
    put(riser(3.0), b - 3.0, 1.0)
    put(boom(1.2), b, 1.2)
    put(brass([38, 45, 50, 57, 62], 2.5), b, 1.1)
put(riser(3.5), LOGO - 3.5, 1.1)
put(boom(1.4), LOGO, 1.3)
put(brass([38, 45, 50, 53, 57, 62], 4.5), LOGO, 1.0)
put(choir([62, 65, 69, 74], 5.0), LOGO, 1.2)

# ---------------- 잔향 (짧은 홀) · 마스터
ir_t = tt(1.6)
ir = rng.standard_normal(len(ir_t)) * np.exp(-ir_t * 3.2)
ir = lowpass(ir, 5000)
ir /= np.sqrt(np.sum(ir ** 2))


def reverb(x):
    n = len(x) + len(ir)
    size = 1 << (n - 1).bit_length()
    y = np.fft.irfft(np.fft.rfft(x, size) * np.fft.rfft(ir, size), size)[: len(x)]
    return x + y * 0.22


L, R = reverb(L), reverb(R)
fade = np.clip((DUR - tt(DUR)) / 3.0, 0.0, 1.0)
L, R = L * fade, R * fade
peak = max(np.abs(L).max(), np.abs(R).max()) or 1.0
drive = 1.6
L = np.tanh(L / peak * drive) / np.tanh(drive) * 0.93
R = np.tanh(R / peak * drive) / np.tanh(drive) * 0.93

out = sys.argv[1] if len(sys.argv) > 1 else "TrailerMusic.wav"
pcm = (np.stack([L, R], axis=1) * 32767).astype("<i2")
with wave.open(out, "w") as w:
    w.setnchannels(2)
    w.setsampwidth(2)
    w.setframerate(SR)
    w.writeframes(pcm.tobytes())
print("음악 저장: %s (%.1f초)" % (out, DUR))
