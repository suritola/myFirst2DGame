# 무한 모드 "불타는 사막" 배경음악 합성 (모노 16비트 32kHz, 32초 반복)
# 120 BPM · 16마디. 히자즈 음계 선율 + 낮은 지속음 + 다르부카풍 타악기
# 실행: python tools/audio/make_desert_bgm.py  →  Assets/Resources/Music/bgm_desert.wav
import math, os, random, struct, wave

RATE = 32000
BPM = 120
BEAT = 60.0 / BPM
BARS = 16
LEN = int(RATE * BEAT * 4 * BARS)
OUT = os.path.join(os.path.dirname(__file__), '..', '..', 'Assets', 'Resources', 'Music', 'bgm_desert.wav')

buf = [0.0] * LEN
random.seed(42)


def midi(n):
    return 440.0 * 2 ** ((n - 69) / 12.0)


def add(start, dur, fn, gain):
    i0 = int(start * RATE)
    n = int(dur * RATE)
    for i in range(n):
        j = (i0 + i) % LEN              # 끝을 넘으면 처음으로 (끊김 없는 반복)
        buf[j] += fn(i / RATE, dur) * gain


def env(t, dur, a=0.01, r=0.12):
    if t < a:
        return t / a
    return max(0.0, 1.0 - max(0.0, t - (dur - r)) / r) * math.exp(-t * 1.6)


def pluck(freq):
    # 우드(oud) 느낌: 배음이 빨리 사라지는 뜯는 소리
    def f(t, dur):
        e = env(t, dur, 0.004, 0.08)
        s = math.sin(2 * math.pi * freq * t) + 0.5 * math.sin(4 * math.pi * freq * t) * math.exp(-t * 6) \
            + 0.25 * math.sin(6 * math.pi * freq * t) * math.exp(-t * 10)
        return s * e
    return f


def reed(freq):
    # 피리(ney) 느낌: 숨소리 섞인 부드러운 소리 + 비브라토
    def f(t, dur):
        a = min(1.0, t / 0.06) * max(0.0, min(1.0, (dur - t) / 0.1))
        vib = 1.0 + 0.006 * math.sin(2 * math.pi * 5.5 * t) * min(1.0, t / 0.3)
        ph = 2 * math.pi * freq * vib * t
        return (math.sin(ph) * 0.8 + 0.2 * math.sin(2 * ph) + 0.05 * (random.random() - 0.5)) * a
    return f


def drone(freq):
    def f(t, dur):
        return (math.sin(2 * math.pi * freq * t) + 0.4 * math.sin(2 * math.pi * freq * 1.5 * t)) * 0.5
    return f


def doum(t, dur):
    # 낮은 북: 음높이가 떨어지는 사인 + 두툼한 몸통
    f = 90 * math.exp(-t * 18) + 55
    return math.sin(2 * math.pi * f * t) * math.exp(-t * 9)


def tek(t, dur):
    # 높은 테두리 소리: 짧은 잡음 + 높은 울림
    return ((random.random() * 2 - 1) * 0.7 + 0.5 * math.sin(2 * math.pi * 1800 * t)) * math.exp(-t * 55)


# 히자즈 (D 기준): D Eb F# G A Bb C D
SCALE = [62, 63, 66, 67, 69, 70, 72, 74]

# 지속음 (D + A), 두 마디마다 살짝 숨쉼
for bar in range(0, BARS, 2):
    add(bar * 4 * BEAT, 8 * BEAT, drone(midi(38)), 0.10)

# 타악기 패턴 (말수디 리듬): 둠 . 둠 텍 . 텍 둠 텍  (8분음표 기준)
PATTERN = ['D', '.', 'D', 'T', '.', 'T', 'D', 'T']
for bar in range(BARS):
    for k, p in enumerate(PATTERN):
        t = bar * 4 * BEAT + k * BEAT / 2
        if p == 'D':
            add(t, 0.35, doum, 0.55)
        elif p == 'T':
            add(t, 0.12, tek, 0.22)
    # 네 마디마다 잔 북 굴림
    if bar % 4 == 3:
        for k in range(6):
            add(bar * 4 * BEAT + 3 * BEAT + k * BEAT / 6, 0.08, tek, 0.12 + 0.02 * k)

# 우드 반주: 한 박에 두 번, 음계 아래쪽을 오르내림
ARP = [0, 2, 3, 2, 4, 3, 2, 1]
for bar in range(BARS):
    root = 0 if bar % 4 < 2 else (3 if bar % 4 == 2 else 1)
    for k in range(8):
        n = SCALE[(ARP[k] + root) % len(SCALE)] - 12
        add(bar * 4 * BEAT + k * BEAT / 2, BEAT / 2 * 0.95, pluck(midi(n)), 0.16)

# 피리 선율 (마디 5부터, 한 번 쉬고 되풀이)
MELODY = [  # (음계 번호, 박 수)
    (4, 1), (5, 0.5), (4, 0.5), (3, 1), (2, 1),
    (1, 1.5), (2, 0.5), (3, 2),
    (4, 1), (6, 0.5), (5, 0.5), (4, 1), (3, 1),
    (2, 1), (1, 1), (0, 2),
    (7, 1), (6, 0.5), (5, 0.5), (4, 2),
    (5, 1), (4, 0.5), (3, 0.5), (2, 2),
    (3, 1), (2, 0.5), (1, 0.5), (2, 1), (3, 1),
    (1, 2), (0, 2),
]
t = 4 * 4 * BEAT
for start_bar in (4, 12):
    t = start_bar * 4 * BEAT
    for deg, beats in MELODY[:len(MELODY) // (1 if start_bar == 4 else 2)]:
        add(t, beats * BEAT * 0.98, reed(midi(SCALE[deg])), 0.16)
        t += beats * BEAT

# 부드럽게 눌러서 저장
peak = max(abs(x) for x in buf) or 1.0
scale = 0.85 / peak
with wave.open(OUT, 'wb') as w:
    w.setnchannels(1)
    w.setsampwidth(2)
    w.setframerate(RATE)
    w.writeframes(b''.join(struct.pack('<h', int(max(-1.0, min(1.0, math.tanh(x * scale * 1.2))) * 32000)) for x in buf))
print('wrote', os.path.abspath(OUT), LEN / RATE, 's')
