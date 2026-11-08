#!/usr/bin/env python3
"""EBO 設計畫布的本機工作台：預覽、截圖、拼圖、點擊測試、素材對照。

工作資料夾（預設 <repo>/.design-work，可用 EBO_DESIGN_WORK 換）：
  canvas/   從畫布讀回來的板子（project/*.dc.html、canvas.json）
  runtime/  畫布的 dc-runtime.js
  assets/   上傳過的素材本機副本，檔名＝blob id（/_blob/<id> 直接對到 assets/<id>.<副檔名>）
  raw/      產圖原圖；play/ 建好的預覽頁；shots/ 截圖

用法：
  canvas.py serve                         在 8770 開本機伺服器（已開就跳過）
  canvas.py asset ID FILE                 上傳後記下本機副本（ID 是 /_blob/ 後面那串）
  canvas.py import DIR                    把 Artifact read 下載的素材（檔名＝blob id）收進 assets/
  canvas.py play BOARD [-s 舊=>新]... [-o 名稱]
                                          建預覽頁：blob 換本機檔、換 runtime、依 -s 改初始狀態
  canvas.py shot 名稱 [-o out.png] [--size 390x844] [--wait 4000]
  canvas.py strip OUT IN... [--crop x,y,w,h] [--zoom N]
                                          多張截圖並排（可先裁切、放大），方便一次看
  canvas.py check OUT IMG...              去背圖疊在淺色、深色底上，檢查邊緣
  canvas.py test 名稱 SCRIPT.js [--wait 15000]
                                          把測試腳本注入預覽頁，用無頭 Chrome 跑，印出 log
"""
import argparse, glob, importlib.util, os, re, shutil, socket, subprocess, sys, html as htmlmod

REPO = os.path.abspath(os.path.join(os.path.dirname(__file__), '..', '..', '..', '..'))
WORK = os.environ.get('EBO_DESIGN_WORK', os.path.join(REPO, '.design-work'))
PORT = 8770  # 專用的埠，不跟其他預覽伺服器撞
CHROME = '/Applications/Google Chrome.app/Contents/MacOS/Google Chrome'
PROFILE = os.path.join(WORK, '.chrome')
MA_PATH = os.path.expanduser('~/.claude/skills/preparing-ai-generated-ui-assets/scripts/magenta_assets.py')


def ma():
    spec = importlib.util.spec_from_file_location('ma', MA_PATH)
    m = importlib.util.module_from_spec(spec); spec.loader.exec_module(m)
    return m


def d(*p):
    path = os.path.join(WORK, *p)
    os.makedirs(os.path.dirname(path) if os.path.splitext(path)[1] else path, exist_ok=True)
    return path


def serving():
    with socket.socket() as s:
        return s.connect_ex(('127.0.0.1', PORT)) == 0


def cmd_serve(a):
    if serving():
        print('already serving', f'http://localhost:{PORT}/'); return
    subprocess.Popen([sys.executable, '-I', '-m', 'http.server', str(PORT), '--bind', '127.0.0.1'], cwd=WORK,
                     stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL, start_new_session=True)
    print('serving', WORK, f'at http://localhost:{PORT}/')


def cmd_asset(a):
    ext = os.path.splitext(a.file)[1].lower() or '.png'
    for old in glob.glob(d('assets', a.id + '.*')): os.remove(old)
    dst = d('assets', a.id + ext); shutil.copyfile(a.file, dst); print(dst)


def cmd_import(a):
    n = 0
    for f in os.listdir(a.dir):
        bid, ext = os.path.splitext(f)
        if re.fullmatch(r'[0-9a-f]{32}', bid):
            shutil.copyfile(os.path.join(a.dir, f), d('assets', bid + ext.lower())); n += 1
    print('imported', n)


def asset_for(bid):
    hit = glob.glob(os.path.join(WORK, 'assets', bid + '.*'))
    return '../assets/' + os.path.basename(hit[0]) if hit else None


def cmd_play(a):
    src = os.path.join(WORK, 'canvas', 'project', a.board + '.dc.html')
    s = open(src, encoding='utf-8').read()
    missing = set()
    def rep(m):
        p = asset_for(m.group(1))
        if not p: missing.add(m.group(1)); return m.group(0)
        return p
    s = re.sub(r'/_blob/([0-9a-f]{32})', rep, s)
    s = s.replace('./support.js', '../runtime/dc-runtime.js')
    for pair in a.set or []:
        old, new = pair.split('=>', 1)
        n = s.count(old)
        if n != 1: sys.exit(f'-s 要剛好比對到 1 處，"{old[:60]}" 比對到 {n} 處')
        s = s.replace(old, new)
    name = a.out or a.board.lower()
    out = d('play', name + '.html'); open(out, 'w', encoding='utf-8').write(s)
    if missing: print('沒有本機副本的素材（用 canvas.py asset 補）：', ' '.join(sorted(missing)))
    print(out, f'http://localhost:{PORT}/play/{name}.html')


def chrome(args, budget):
    if not serving(): cmd_serve(None)
    return subprocess.run([CHROME, '--headless=new', f'--user-data-dir={PROFILE}', f'--virtual-time-budget={budget}', *args],
                          capture_output=True, text=True, timeout=180)


def cmd_shot(a):
    w, h = a.size.split('x')
    out = a.out or d('shots', a.name + '.png')
    chrome([f'--window-size={w},{h}', f'--screenshot={out}', f'http://localhost:{PORT}/play/{a.name}.html'], a.wait)
    print(out)


def rgba_rows(path):
    m = ma(); w, h, r = m.read_png(path); k = len(r[0]) // w
    return w, h, [bytearray(x for i in range(w) for x in (*row[i*k:i*k+3], row[i*k+3] if k == 4 else 255)) for row in r]


def cmd_strip(a):
    m = ma(); frames = []
    for p in a.inputs:
        w, h, r = rgba_rows(p)
        if a.crop:
            x, y, cw, ch = map(int, a.crop.split(','))
            r = [row[x*4:(x+cw)*4] for row in r[y:y+ch]]; w, h = cw, len(r)
        z = a.zoom
        if z > 1:
            r = [bytes(b for i in range(w) for b in bytes(row[i*4:i*4+4]) * z) for row in r for _ in range(z)]; w, h = w*z, h*z
        frames.append((w, h, r))
    H = max(h for w, h, r in frames); gap = bytes([255]*4*10)
    rows = [gap.join(bytes(r[y]) if y < h else bytes([255]*4*w) for w, h, r in frames) for y in range(H)]
    m.write_png(a.out, sum(w for w, h, r in frames) + 10*(len(frames)-1), H, rows); print(a.out)


def cmd_check(a):
    m = ma(); rows = []
    imgs = [rgba_rows(p) for p in a.inputs]
    H = max(h for w, h, r in imgs)
    for bg in [(240, 228, 205), (50, 35, 22)]:
        for y in range(H):
            line = bytearray()
            for w, h, r in imgs:
                for x in range(w):
                    if y < h:
                        p = r[y][x*4:x*4+4]; al = p[3]/255
                        line += bytes(round(p[c]*al + bg[c]*(1-al)) for c in range(3)) + b'\xff'
                    else: line += bytes([*bg, 255])
                line += bytes([*bg, 255]) * 20
            rows.append(bytes(line))
    m.write_png(a.out, len(rows[0])//4, len(rows), rows); print(a.out)


HARNESS = r"""<script>
(function(){
  const out=document.createElement('pre'); out.id='__lg';
  const L=[]; window.log=(...x)=>{L.push(x.join(' '));out.textContent=L.join('\n');};
  window.q=(s,root)=>(root||document).querySelector(s); window.qa=(s,root)=>[...(root||document).querySelectorAll(s)];
  // 模擬真的手指：在元素中心找實際會點到的東西，送 pointerdown／up 再 click；回傳點到的元素
  window.tap=(el)=>{const r=el.getBoundingClientRect(),x=r.left+r.width/2,y=r.top+r.height/2,hit=document.elementFromPoint(x,y)||el;
    for(const t of ['pointerdown','pointerup']) hit.dispatchEvent(new PointerEvent(t,{bubbles:true,clientX:x,clientY:y,pointerId:1,isPrimary:true}));
    hit.click(); return hit;};
  window.swipe=(el,dx)=>{const r=el.getBoundingClientRect(),y=r.top+r.height/2,x=r.left+r.width/2;
    el.dispatchEvent(new PointerEvent('pointerdown',{bubbles:true,clientX:x,clientY:y,pointerId:1,isPrimary:true}));
    el.dispatchEvent(new PointerEvent('pointerup',{bubbles:true,clientX:x+dx,clientY:y,pointerId:1,isPrimary:true}));};
  window.addEventListener('error',e=>log('PAGE ERROR',e.message));
  window.addEventListener('load',()=>{document.body.appendChild(out);
    const steps=window.STEPS||[]; let i=0;
    const next=()=>{ if(i>=steps.length) return; const [wait,fn]=steps[i++]; setTimeout(()=>{try{fn()}catch(e){log('ERR step',i,e.message)} next();},wait); };
    next();});
})();
</script>"""


def cmd_test(a):
    src = d('play', a.name + '.html')
    s = open(src, encoding='utf-8').read(); t = open(a.script, encoding='utf-8').read()
    i = s.rindex('</body>')
    test = d('play', a.name + '.test.html')
    open(test, 'w', encoding='utf-8').write(s[:i] + HARNESS + '<script>' + t + '</script>' + s[i:])
    r = chrome(['--window-size=390,844', '--dump-dom', f'http://localhost:{PORT}/play/{a.name}.test.html'], a.wait)
    m = re.search(r'<pre id="__lg">(.*?)</pre>', r.stdout, re.S)
    print(htmlmod.unescape(m.group(1)) if m else '（沒有 log：腳本沒跑起來，或等待時間不夠）')


def main():
    p = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    sp = p.add_subparsers(dest='cmd', required=True)
    sp.add_parser('serve').set_defaults(f=cmd_serve)
    x = sp.add_parser('asset'); x.add_argument('id'); x.add_argument('file'); x.set_defaults(f=cmd_asset)
    x = sp.add_parser('import'); x.add_argument('dir'); x.set_defaults(f=cmd_import)
    x = sp.add_parser('play'); x.add_argument('board'); x.add_argument('-s', '--set', action='append'); x.add_argument('-o', '--out'); x.set_defaults(f=cmd_play)
    x = sp.add_parser('shot'); x.add_argument('name'); x.add_argument('-o', '--out'); x.add_argument('--size', default='390x844'); x.add_argument('--wait', type=int, default=4000); x.set_defaults(f=cmd_shot)
    x = sp.add_parser('strip'); x.add_argument('out'); x.add_argument('inputs', nargs='+'); x.add_argument('--crop'); x.add_argument('--zoom', type=int, default=1); x.set_defaults(f=cmd_strip)
    x = sp.add_parser('check'); x.add_argument('out'); x.add_argument('inputs', nargs='+'); x.set_defaults(f=cmd_check)
    x = sp.add_parser('test'); x.add_argument('name'); x.add_argument('script'); x.add_argument('--wait', type=int, default=15000); x.set_defaults(f=cmd_test)
    a = p.parse_args(); a.f(a)


if __name__ == '__main__':
    main()
