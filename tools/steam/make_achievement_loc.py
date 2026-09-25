# docs/steam/achievements.md 표에서 Steamworks 도전 과제 현지화 파일(docs/steam/achievement-loc/*.vdf)을 만듦
# 실행 (저장소 루트에서): python tools/steam/make_achievement_loc.py
import re, os
rows = []
for line in open('docs/steam/achievements.md', encoding='utf-8'):
    m = re.match(r'\| `(ACH_[A-Z0-9_]+)` \| (.+) \| (.+) \| (.+) \| (.+) \|$', line.strip())
    if m and '**' in m.group(2):
        rows.append(m.groups())
assert len(rows) == 26, len(rows)

def split(cell):
    m = re.match(r'\*\*(.+?)\*\* — (.+)', cell.strip())
    return m.group(1), m.group(2)

os.makedirs('docs/steam/achievement-loc', exist_ok=True)
for col, lang in [(1, 'koreana'), (3, 'japanese'), (4, 'schinese'), (2, 'english')]:
    out = ['"lang"', '{', '\t"Language"\t"%s"' % lang, '\t"Tokens"', '\t{']
    for i, r in enumerate(rows):
        name, desc = split(r[col])
        assert '"' not in name + desc
        out.append('\t\t"NEW_ACHIEVEMENT_1_%d_NAME"\t"%s"' % (i, name))
        out.append('\t\t"NEW_ACHIEVEMENT_1_%d_DESC"\t"%s"' % (i, desc))
    out += ['\t}', '}', '']
    open('docs/steam/achievement-loc/%s.vdf' % lang, 'w', encoding='utf-8', newline='\n').write('\n'.join(out))
    print(lang, rows[0][0], split(rows[0][col]), rows[19][0], split(rows[19][col]))
