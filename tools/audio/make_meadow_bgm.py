# 3장 "초원" 배경음악 (2.1~): 무겁고 웅장하게 — 오래된 숲의 행진 (모노 16비트 32kHz, 반복)
# 84 BPM · 16마디 · D 단조(프리지안 내림 2도 섞음). 낮은 금관 화음 + 첼로 스타카토 반복음 + 합창 패드
# + 타이코 북 · 팀파니 연타 + 낮은 종. 예전 곡이 너무 밝아서 바꿈
# 실행: python tools/audio/make_meadow_bgm.py  →  Assets/Resources/Music/bgm_meadow.wav
import math, os, random, struct, wave

RATE = 32000
BPM = 84
BEAT = 60.0 / BPM
BARS = 16
LEN = int(RATE * BEAT * 4 * BARS)
OUT = os.path.join(os.path.dirname(__file__), '..', '..', 'Assets', 'Resources', 'Music', 'bgm_meadow.wav')

buf = [0.0] * LEN
random.seed(7)
TAU = 2 * math.pi


def midi(n):
    return 440.0 * 2 ** ((n - 69) / 12.0)


def add(start, dur, fn, gain):
    i0 = int(start * RATE)
    n = int(dur * RATE)
    for i in range(n):
        buf[(i0 + i) % LEN] += fn(i / RATE, dur) * gain       # 끝을 넘으면 처음으로 (끊김 없는 반복)


def adsr(t, dur, a, r):
    if t < a:
        return t / a
    return max(0.0, min(1.0, (dur - t) / r))


def brass(freq):
    # 낮은 금관: 톱니파 배음을 몇 개만 (부드럽게), 천천히 부풀었다 가라앉음
    def f(t, dur):
        e = adsr(t, dur, 0.35, 0.6) * (0.85 + 0.15 * math.sin(TAU * 0.5 * t))
        s = 0.0
        for k in range(1, 7):
            s += math.sin(TAU * freq * k * t + k * 0.3) / (k ** 1.15)
        return s * e
    return f


def choir(freq):
    # 합창 "아": 배음 중 몇 개를 키워 목소리처럼 + 살짝 어긋난 두 목소리
    form = {1: 1.0, 2: 0.6, 3: 0.5, 4: 0.35, 5: 0.12, 6: 0.08}
    def f(t, dur):
        e = adsr(t, dur, 0.8, 1.0)
        vib = 1.0 + 0.004 * math.sin(TAU * 4.8 * t)
        s = 0.0
        for k, g in form.items():
            s += g * (math.sin(TAU * freq * k * vib * t) + math.sin(TAU * freq * 1.004 * k * t + 1.1)) * 0.5
        return s * e
    return f


def cello(freq):
    # 첼로 스타카토: 활을 긋는 거친 시작 + 짧은 끝
    def f(t, dur):
        e = min(1.0, t / 0.015) * math.exp(-t * 7.5)
        ph = TAU * freq * t
        s = math.sin(ph) + 0.55 * math.sin(2 * ph + 0.4) + 0.3 * math.sin(3 * ph) + 0.15 * math.sin(4 * ph)
        return (s + 0.04 * (random.random() - 0.5)) * e
    return f


def taiko(t, dur):
    # 큰 북: 낮게 떨어지는 음 + 두꺼운 몸통 + 처음의 둔탁한 잡음
    f = 70 * math.exp(-t * 14) + 42
    body = math.sin(TAU * f * t) * math.exp(-t * 5.5)
    hit = (random.random() * 2 - 1) * math.exp(-t * 60) * 0.6
    return body + hit


def timpani(freq):
    def f(t, dur):
        return (math.sin(TAU * freq * t) + 0.4 * math.sin(TAU * freq * 1.5 * t)) * math.exp(-t * 3.2) * min(1.0, t / 0.004)
    return f


def bell(freq):
    # 낮은 종: 어긋난 배음들이 오래 울림
    parts = [(1.0, 1.0), (2.76, 0.5), (5.4, 0.25), (8.9, 0.12)]
    def f(t, dur):
        return sum(g * math.sin(TAU * freq * r * t) * math.exp(-t * (0.7 + r * 0.25)) for r, g in parts)
    return f


# 화음 진행 (두 마디씩): Dm - Bb - Gm - A(#) / Dm - Eb(프리지안) - C - A
CHORDS = [
    (38, [50, 53, 57]), (34, [46, 50, 53]), (31, [43, 46, 50]), (33, [45, 49, 52]),
    (38, [50, 53, 57]), (39, [51, 55, 58]), (36, [48, 52, 55]), (33, [45, 49, 52]),
]
for i, (root, chord) in enumerate(CHORDS):
    t0 = i * 8 * BEAT
    # 금관 화음 (한 옥타브 아래 뿌리음까지)
    add(t0, 8 * BEAT, brass(midi(root)), 0.07)
    for n in chord:
        add(t0, 8 * BEAT, brass(midi(n - 12)), 0.035)
    # 합창: 뒤 여덟 마디부터 (곡이 점점 커지게)
    if i >= 4:
        for n in chord:
            add(t0, 8 * BEAT, choir(midi(n)), 0.03)
    # 첼로 반복음: 8분음표로 뿌리음 · 5도 · 옥타브
    pattern = [0, 0, 7, 0, 12, 0, 7, 3]
    for b in range(16):
        n = root + 12 + pattern[b % 8]
        add(t0 + b * BEAT * 0.5, BEAT * 0.45, cello(midi(n)), 0.07)
    # 낮은 종: 화음이 바뀔 때마다
    add(t0, 6 * BEAT, bell(midi(root + 12)), 0.05)

# 북: 한 마디에 둥 . 둥둥 . / 네 마디마다 팀파니 연타로 몰아침
for bar in range(BARS):
    t0 = bar * 4 * BEAT
    for beat, g in [(0, 0.5), (2, 0.38), (2.5, 0.3)]:
        add(t0 + beat * BEAT, 1.2, taiko, g)
    if bar % 4 == 3:
        root = CHORDS[(bar // 2) % len(CHORDS)][0]
        for k in range(8):
            add(t0 + 2 * BEAT + k * BEAT / 4, 0.6, timpani(midi(root + 12)), 0.06 + 0.02 * k)

# 전체: 부드럽게 눌러 소리가 깨지지 않게 (tanh) 후 16비트
peak = max(abs(x) for x in buf) or 1.0
out = [math.tanh(x / peak * 1.6) * 0.82 for x in buf]
with wave.open(OUT, 'w') as w:
    w.setnchannels(1)
    w.setsampwidth(2)
    w.setframerate(RATE)
    w.writeframes(b''.join(struct.pack('<h', int(max(-1.0, min(1.0, s)) * 32767)) for s in out))
print('wrote', OUT, '%.1fs' % (LEN / RATE))
