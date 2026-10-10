// Soul Saver 익명 플레이 데이터 수집 (Google Apps Script 웹 앱)
// 게임(Assets/Scripts/Telemetry.cs)이 판이 끝날 때마다 balance_log.csv 와 같은 한 줄을 보냄 → 이 시트에 한 줄씩 쌓임
// 설치 방법: tools/telemetry/README.md

const TOKEN = 'soulsaver-telemetry-v1';   // Telemetry.cs 의 Token 과 같게
const MAX_COLS = 60, MAX_LEN = 4000;

function doPost(e) {
  try {
    const d = JSON.parse(e.postData.contents);
    if (d.token !== TOKEN) return out('bad token');
    const header = String(d.header || ''), row = String(d.row || '');
    if (!header || !row || row.length > MAX_LEN) return out('bad row');
    const cols = header.split(','), vals = row.split(',');
    if (cols.length !== vals.length || cols.length > MAX_COLS) return out('bad row');

    // 칸 수가 바뀌면(게임 업데이트) 새 시트에 따로 쌓음
    const ss = SpreadsheetApp.getActiveSpreadsheet();
    const name = 'runs_' + cols.length;
    const lock = LockService.getScriptLock();
    lock.waitLock(10000);
    try {
      let sh = ss.getSheetByName(name);
      if (!sh) {
        sh = ss.insertSheet(name);
        sh.appendRow(['received', 'player'].concat(cols));
        sh.setFrozenRows(1);
      }
      sh.appendRow([new Date(), String(d.id || '').slice(0, 32)].concat(vals.map(safe)));
    } finally {
      lock.releaseLock();
    }
    return out('ok');
  } catch (err) {
    return out('error');
  }
}

// 시트가 수식으로 읽지 않게 (=, +, @ 로 시작하는 값)
function safe(v) {
  return /^[=+@]/.test(v) ? "'" + v : v;
}

function out(s) {
  return ContentService.createTextOutput(s);
}
