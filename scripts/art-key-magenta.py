# 用於 agy 產的 JPEG：桃紅底有雜訊，先用色相判斷去底，再清掉孤立小斑點，最後裁邊。
import sys, struct, zlib
def readpng(p):
    d=open(p,'rb').read(); pos=8; idat=b''
    while pos<len(d):
        l=struct.unpack('>I',d[pos:pos+4])[0]; t=d[pos+4:pos+8]; c=d[pos+8:pos+8+l]
        if t==b'IHDR': w,h,bd,ct=struct.unpack('>IIBB',c[:10])
        if t==b'IDAT': idat+=c
        pos+=12+l
    bpp={2:3,6:4}[ct]
    raw=zlib.decompress(idat); stride=w*bpp; rows=[]; prev=bytearray(stride); i=0
    for y in range(h):
        f=raw[i]; i+=1; line=bytearray(raw[i:i+stride]); i+=stride
        for x in range(stride):
            a=line[x-bpp] if x>=bpp else 0; b=prev[x]; c=prev[x-bpp] if x>=bpp else 0
            if f==1: line[x]=(line[x]+a)&255
            elif f==2: line[x]=(line[x]+b)&255
            elif f==3: line[x]=(line[x]+(a+b)//2)&255
            elif f==4:
                p=a+b-c; pa,pb,pc=abs(p-a),abs(p-b),abs(p-c)
                line[x]=(line[x]+(a if pa<=pb and pa<=pc else b if pb<=pc else c))&255
        rows.append(line); prev=line
    if bpp==3: rows=[bytearray(b''.join(bytes((r[x*3],r[x*3+1],r[x*3+2],255)) for x in range(w))) for r in rows]
    return w,h,rows
def writepng(p,w,h,rows):
    def chunk(t,d): return struct.pack('>I',len(d))+t+d+struct.pack('>I',zlib.crc32(t+d)&0xffffffff)
    raw=b''.join(b'\x00'+bytes(r) for r in rows)
    open(p,'wb').write(b'\x89PNG\r\n\x1a\n'+chunk(b'IHDR',struct.pack('>IIBBBBB',w,h,8,6,0,0,0))+chunk(b'IDAT',zlib.compress(raw,9))+chunk(b'IEND',b''))
src,dst=sys.argv[1],sys.argv[2]
w,h,rows=readpng(src)
# 1. 色相去底：桃紅 = R、B 都高且 G 明顯低
for r in rows:
    for i in range(0,w*4,4):
        R,G,B=r[i],r[i+1],r[i+2]
        m=min(R,B)-G          # 桃紅程度
        if m>90 and min(R,B)>110: r[i+3]=0
        elif m>50 and min(R,B)>90:
            r[i+3]=max(0,min(255,int(255*(90-m)/40)))
            r[i+1]=min(255,G+(m-50))   # 去溢色
# 2. 清孤立斑點：alpha>0 但 5×5 鄰域內不透明像素太少的視為雜訊（跑兩輪）
for _ in range(2):
    A=[[rows[y][x*4+3] for x in range(w)] for y in range(h)]
    for y in range(h):
        for x in range(w):
            if A[y][x]==0: continue
            n=0
            for dy in range(-2,3):
                yy=y+dy
                if yy<0 or yy>=h: continue
                row=A[yy]
                for dx in range(-2,3):
                    xx=x+dx
                    if 0<=xx<w and row[xx]>0: n+=1
            if n<12: rows[y][x*4+3]=0
# 3. 裁到不透明邊界（alpha>24）
xs=[x for x in range(w) if any(rows[y][x*4+3]>24 for y in range(h))]
ys=[y for y in range(h) if any(rows[y][x*4+3]>24 for x in range(w))]
x0,x1,y0,y1=xs[0],xs[-1],ys[0],ys[-1]
out=[rows[y][x0*4:(x1+1)*4] for y in range(y0,y1+1)]
writepng(dst,x1-x0+1,y1-y0+1,out); print(dst,x1-x0+1,'x',y1-y0+1)
