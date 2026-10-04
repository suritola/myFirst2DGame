# 보스 필살기 "결계" 소리 (2.1~): 무섭고 웅장하게 (모노 16비트 32kHz)
#   Music/bgm_domain.wav       결계 안 배경음 (반복): 낮은 삼전음 지속음 + 심장 박동 + 불협 합창 + 금속 긁힘 + 종
#   Sounds/domain_shout.wav    보스가 외칠 때: 금관 불협화음 타격 + 큰 북 + 으르렁
#   Sounds/domain_open.wav     결계가 펼쳐질 때: 거꾸로 차오르는 합창 → 거대한 충격 → 긴 울림
#   Sounds/domain_break.wav    결계가 깨질 때: 유리 파열 + 낮은 충격
# 실행: python tools/audio/make_domain_audio.py
import math, os, random, struct, wave

RATE = 32000
TAU = 2 * math.pi
ROOT = os.path.join(os.path.dirname(__file__), '..', '..', 'Assets', 'Resources')
random.seed(13)


def midi(n):
    return 440.0 * 2 ** ((n - 69) / 12.0)


def save(path, data, norm=0.9, drive=1.4):
    peak = max(abs(x) for x in data) or 1.0
    out = [math.tanh(x / peak * drive) / math.tanh(drive) * norm for x in data]
    os.makedirs(os.path.dirname(path), exist_ok=True)
    with wave.open(path, 'w') as w:
        w.setnchannels(1)
        w.setsampwidth(2)
        w.setframerate(RATE)
        w.writeframes(b''.join(struct.pack('<h', int(max(-1.0, min(1.0, s)) * 32767)) for s in out))
    print('wrote', os.path.normpath(path), '%.1fs' % (len(data) / RATE))


class Track:
    def __init__(self, seconds, loop=False):
        self.n = int(seconds * RATE)
        self.buf = [0.0] * self.n
        self.loop = loop

    def add(self, start, dur, fn, gain):
        i0 = int(start * RATE)
        for i in range(int(dur * RATE)):
            j = i0 + i
            if self.loop: j %= self.n
            elif j >= self.n: break
            self.buf[j] += fn(i / RATE, dur) * gain


def choir_cluster(notes, attack=1.2):
    # 불협 합창: 반음 · 삼전음으로 부딪히는 목소리들, 천천히 숨 쉬듯
    form = [(1, 1.0), (2, 0.55), (3, 0.42), (4, 0.25), (5, 0.1)]
    fr = [midi(n) for n in notes]
    def f(t, dur):
        e = min(1.0, t / attack) * max(0.0, min(1.0, (dur - t) / 1.2))
        s = 0.0
        for j, base in enumerate(fr):
            vib = 1.0 + 0.005 * math.sin(TAU * (4.3 + j * 0.37) * t + j)
            for k, g in form:
                s += g * math.sin(TAU * base * k * vib * t + j * 0.7)
        return s / len(fr) * e
    return f


def drone(freq):
    def f(t, dur):
        wob = 0.6 + 0.4 * math.sin(TAU * 0.13 * t)
        return (math.sin(TAU * freq * t) + 0.5 * math.sin(TAU * freq * 2.01 * t) * wob + 0.25 * math.sin(TAU * freq * 3.0 * t)) * 0.6
    return f


def heartbeat(t, dur):
    # 두 번 뛰는 심장 (쿵-쿵)
    def thump(x):
        if x < 0: return 0.0
        fq = 55 * math.exp(-x * 10) + 38
        return math.sin(TAU * fq * x) * math.exp(-x * 9)
    return thump(t) + 0.7 * thump(t - 0.28)


def scrape(t, dur):
    # 금속 긁힘: 좁은 대역의 잡음이 떨림
    f0 = 2400 + 900 * math.sin(TAU * 0.7 * t)
    return (random.random() * 2 - 1) * 0.3 * math.sin(TAU * f0 * t) * math.sin(math.pi * t / dur)


def bell(freq):
    parts = [(1.0, 1.0), (2.4, 0.55), (4.1, 0.3), (6.7, 0.15)]
    def f(t, dur):
        return sum(g * math.sin(TAU * freq * r * t) * math.exp(-t * (0.5 + r * 0.3)) for r, g in parts)
    return f


def taiko(t, dur):
    fq = 68 * math.exp(-t * 13) + 40
    return math.sin(TAU * fq * t) * math.exp(-t * 4.5) + (random.random() * 2 - 1) * math.exp(-t * 50) * 0.7


def brass_stab(notes):
    fr = [midi(n) for n in notes]
    def f(t, dur):
        e = min(1.0, t / 0.02) * math.exp(-t * 1.6)
        s = 0.0
        for base in fr:
            for k in range(1, 9):
                s += math.sin(TAU * base * k * t + k) / k
        return s / len(fr) * e
    return f


def growl(t, dur):
    fq = 70 + 18 * math.sin(TAU * 7 * t)
    saw = 2 * (t * fq - math.floor(t * fq + 0.5))
    return (saw * 0.6 + (random.random() * 2 - 1) * 0.35) * math.sin(math.pi * min(1.0, t / dur))


def boom(t, dur):
    fq = 45 * math.exp(-t * 4) + 28
    return math.sin(TAU * fq * t) * math.exp(-t * 1.6) + (random.random() * 2 - 1) * math.exp(-t * 14) * 0.8


def glass(t, dur):
    s = 0.0
    for fr in (2350, 3120, 4410, 5230, 6870):
        s += math.sin(TAU * fr * t + fr) * math.exp(-t * (6 + fr / 900))
    return s * 0.35 + (random.random() * 2 - 1) * math.exp(-t * 30) * 0.6


# ---------------------------------------------------------------- 결계 배경음 (24초 반복, 70 BPM 느낌)
bgm = Track(24.0, loop=True)
bgm.add(0, 24, drone(midi(26)), 0.35)          # D1
bgm.add(0, 24, drone(midi(32)), 0.22)          # G#1 (삼전음)
for i, notes in enumerate([[50, 51, 56], [49, 50, 55], [50, 53, 56], [48, 49, 54]]):
    bgm.add(i * 6, 7, choir_cluster(notes), 0.22)
for k in range(28):                            # 심장 박동 (점점 몰아치는 느낌은 반복이 맡음)
    bgm.add(k * 0.857, 0.8, heartbeat, 0.55)
for k in range(4):
    bgm.add(k * 6 + 3, 2.2, scrape, 0.25)
    bgm.add(k * 6, 5, bell(midi(38 + (k % 2))), 0.18)
for k in range(8):
    bgm.add(k * 3 + 1.5, 1.4, taiko, 0.35)
save(os.path.join(ROOT, 'Music', 'bgm_domain.wav'), bgm.buf, 0.85, 1.6)

# ---------------------------------------------------------------- 외침 스팅어 (1.8초)
shout = Track(1.8)
shout.add(0, 1.8, brass_stab([38, 39, 44, 50]), 0.6)
shout.add(0, 1.4, taiko, 0.8)
shout.add(0.18, 1.2, taiko, 0.6)
shout.add(0, 1.4, growl, 0.45)
shout.add(0, 1.8, boom, 0.5)
save(os.path.join(ROOT, 'Sounds', 'domain_shout.wav'), shout.buf, 0.95, 1.8)

# ---------------------------------------------------------------- 결계 전개 (4초): 거꾸로 차오르는 합창 → 충격 → 울림
op = Track(4.2)
swell = choir_cluster([50, 51, 56, 57], attack=1.3)
op.add(0, 1.4, lambda t, d: swell(t, 4.0) * (t / d) ** 2, 0.7)
op.add(1.3, 2.9, boom, 1.0)
op.add(1.3, 2.9, bell(midi(26)), 0.6)
op.add(1.3, 2.9, bell(midi(33)), 0.4)
op.add(1.35, 1.5, glass, 0.35)
op.add(1.3, 2.8, choir_cluster([38, 44, 50, 51], attack=0.05), 0.45)
save(os.path.join(ROOT, 'Sounds', 'domain_open.wav'), op.buf, 0.95, 1.7)

# ---------------------------------------------------------------- 결계 파열 (1.6초)
br = Track(1.6)
br.add(0, 1.6, glass, 0.9)
br.add(0, 1.6, boom, 0.6)
save(os.path.join(ROOT, 'Sounds', 'domain_break.wav'), br.buf, 0.9, 1.5)
