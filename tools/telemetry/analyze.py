# Soul Saver 익명 플레이 데이터 분석
# 구글 시트 「파일 → 다운로드 → CSV」로 받은 파일(runs_28 시트)을 넣으면 요약을 보여 줌
# 사용법: python tools/telemetry/analyze.py runs.csv [--version 2.2.1]
import csv, sys
from collections import Counter, defaultdict

def main():
    args = sys.argv[1:]
    if not args:
        print("사용법: python tools/telemetry/analyze.py <시트에서 받은 CSV> [--version 2.2.1]")
        return
    path, version = args[0], None
    if "--version" in args:
        version = args[args.index("--version") + 1]

    with open(path, encoding="utf-8-sig", newline="") as f:
        rows = list(csv.DictReader(f))
    if version:
        rows = [r for r in rows if r.get("version") == version]
    if not rows:
        print("기록이 없습니다")
        return

    def num(r, k):
        try: return float(r.get(k) or 0)
        except ValueError: return 0.0

    print(f"판 {len(rows)}개 · 플레이어 {len({r['player'] for r in rows})}명")
    print("버전별:", dict(Counter(r["version"] for r in rows).most_common()))

    def table(title, key):
        print(f"\n== {title}")
        print(f"{'':16}{'판':>6}{'클리어':>8}{'죽음':>7}{'평균 분':>9}{'평균 레벨':>10}")
        groups = defaultdict(list)
        for r in rows: groups[key(r)].append(r)
        for k, g in sorted(groups.items()):
            clear = sum(r["result"] == "clear" for r in g) / len(g) * 100
            death = sum(r["result"] == "death" for r in g) / len(g) * 100
            mins = sum(num(r, "seconds") for r in g) / len(g) / 60
            lv = sum(num(r, "level") for r in g) / len(g)
            print(f"{str(k):16}{len(g):>6}{clear:>7.0f}%{death:>6.0f}%{mins:>9.1f}{lv:>10.1f}")

    table("난이도별", lambda r: r["difficulty"])
    table("캐릭터별", lambda r: r["character"])

    deaths = [r for r in rows if r["result"] == "death"]
    if deaths:
        print("\n== 어디서 죽나 (난이도 · 장)")
        for (d, s), n in sorted(Counter((r["difficulty"], r["stage"]) for r in deaths).items()):
            print(f"  {d:8} {s:>4}장  {n}번")
        print("\n== 무엇에 죽나 (상위 10)")
        for k, n in Counter(r["last_hit"] for r in deaths if r["last_hit"]).most_common(10):
            print(f"  {k}: {n}번")

    print("\n== 많이 고른 1차 진화 (상위 10)")
    for k, n in Counter(r["evo1"] for r in rows if r["evo1"]).most_common(10):
        print(f"  {k}: {n}번")

if __name__ == "__main__":
    main()
