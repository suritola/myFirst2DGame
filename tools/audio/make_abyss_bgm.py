# 4장 "영혼의 심연" 배경음악 (2.1.1~): 고요하고 서늘하게 — 영혼이 가라앉은 깊은 곳 (모노 16비트 32kHz, 반복)
# 70 BPM · 16마디 · 올림다 단조. 낮은 합창 지속음 + 유리 종 아르페지오 + 심장 박동 같은 북 + 높은 영혼빛 패드
# 실행: python tools/audio/make_abyss_bgm.py  →  Assets/Resources/Music/bgm_abyss.wav
import math, os, random, re, struct, uuid, wave

RATE = 32000
BPM = 70
BEAT = 60.0 / BPM
BARS = 16
LEN = int(RATE * BEAT * 4 * BARS)
MUSIC = os.path.join(os.path.dirname(__file__), '..', '..', 'Assets', 'Resources', 'Music')
OUT = os.path.join(MUSIC, 'bgm_abyss.wav')

buf = [0.0] * LEN
random.seed(11)
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


def choir(freq):
    # 낮은 합창 "우": 낮은 배음 위주 + 살짝 어긋난 두 목소리, 아주 천천히 부풀었다 가라앉음
    form = {1: 1.0, 2: 0.45, 3: 0.2, 4: 0.08}
    def f(t, dur):
        e = adsr(t, dur, 1.6, 1.8)
        vib = 1.0 + 0.003 * math.sin(TAU * 4.2 * t)
        s = 0.0
        for k, g in form.items():
            s += g * (math.sin(TAU * freq * k * vib * t) + math.sin(TAU * freq * 1.005 * k * t + 0.9)) * 0.5
        return s * e
    return f


def glass(freq):
    # 유리 종: 맑은 배음이 짧게 울리고 길게 사라짐
    parts = [(1.0, 1.0), (2.0, 0.35), (3.01, 0.2), (4.2, 0.08)]
    def f(t, dur):
        return sum(g * math.sin(TAU * freq * r * t) * math.exp(-t * (1.6 + r * 0.6)) for r, g in parts) * min(1.0, t / 0.003)
    return f


def shimmer(freq):
    # 높은 영혼빛 패드: 떨리는 사인 두 개
    def f(t, dur):
        e = adsr(t, dur, 2.0, 2.0) * (0.6 + 0.4 * math.sin(TAU * 0.35 * t))
        return (math.sin(TAU * freq * t) + 0.6 * math.sin(TAU * freq * 1.502 * t + 0.7)) * e
    return f


def heart(t, dur):
    # 심장 박동 북: 낮고 둔하게
    f = 55 * math.exp(-t * 18) + 38
    return math.sin(TAU * f * t) * math.exp(-t * 7) + (random.random() * 2 - 1) * math.exp(-t * 80) * 0.25


# 화음 진행 (두 마디씩): C#m - A - E - G#(sus) / C#m - F#m - D(나폴리) - G#
CHORDS = [
    (37, [49, 52, 56]), (33, [45, 49, 52]), (40, [52, 56, 59]), (44, [56, 61, 63]),
    (37, [49, 52, 56]), (42, [54, 57, 61]), (38, [50, 54, 57]), (44, [56, 60, 63]),
]
for i, (root, chord) in enumerate(CHORDS):
    t0 = i * 8 * BEAT
    add(t0, 8 * BEAT, choir(midi(root)), 0.09)
    for n in chord:
        add(t0, 8 * BEAT, choir(midi(n - 12)), 0.04)
    # 유리 종 아르페지오: 8분음표로 화음을 오르내림 (뒤 여덟 마디는 한 옥타브 위를 섞음)
    arp = chord + [chord[1] + 12, chord[2] + 12, chord[1] + 12, chord[2]]
    for b in range(16):
        n = arp[b % len(arp)] + (12 if i >= 4 and b % 4 == 3 else 0)
        add(t0 + b * BEAT * 0.5, BEAT * 1.6, glass(midi(n + 12)), 0.05 if b % 2 == 0 else 0.035)
    # 영혼빛 패드: 뒤 여덟 마디부터
    if i >= 4:
        add(t0, 8 * BEAT, shimmer(midi(chord[2] + 24)), 0.018)

# 심장 박동: 한 마디에 둥-둥 . . (뒤 여덟 마디는 조금 더 촘촘히)
for bar in range(BARS):
    t0 = bar * 4 * BEAT
    hits = [(0, 0.55), (0.45, 0.4)] + ([(2, 0.35), (2.45, 0.28)] if bar >= 8 else [])
    for beat, g in hits:
        add(t0 + beat * BEAT, 1.0, heart, g)

peak = max(abs(x) for x in buf) or 1.0
out = [math.tanh(x / peak * 1.5) * 0.8 for x in buf]
with wave.open(OUT, 'w') as w:
    w.setnchannels(1)
    w.setsampwidth(2)
    w.setframerate(RATE)
    w.writeframes(b''.join(struct.pack('<h', int(max(-1.0, min(1.0, s)) * 32767)) for s in out))
# 메타 (처음 한 번): 초원 곡 설정 그대로, guid 만 새로
if not os.path.exists(OUT + '.meta'):
    tpl = open(os.path.join(MUSIC, 'bgm_meadow.wav.meta'), encoding='utf-8').read()
    open(OUT + '.meta', 'w', encoding='utf-8', newline='\n').write(re.sub(r'guid: [0-9a-f]{32}', 'guid: ' + uuid.uuid4().hex, tpl, count=1))
print('wrote', OUT, '%.1fs' % (LEN / RATE))
