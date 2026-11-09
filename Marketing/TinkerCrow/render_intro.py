"""Reconstruct the intro from original assets; does not alter game files."""
from pathlib import Path
import math, subprocess, wave, json
import numpy as np
from PIL import Image, ImageDraw, ImageFont, ImageFilter

ROOT = Path(__file__).resolve().parents[2]
OUT = Path(__file__).resolve().parent
ASSETS = ROOT / 'Echoes/Assets'
FF = ROOT / 'tmp/tinker-crow-tools/ffmpeg.exe'
W, H, FPS = 3840, 2160, 30
LAND = .6 + 1.1
TYPE = LAND + 2/7 + .35
HOLD = TYPE + .8
FADE = HOLD + 1.6
END = FADE + .9 + .3
N = math.ceil(END * FPS)
S = round(H * .62)
WIRE = H * .62

def sprite(name, size):
    return Image.open(ASSETS / 'Textures/Intro' / name).convert('RGBA').resize((size,size), Image.Resampling.LANCZOS)

frames = [sprite(n,S) for n in ['corvo_chegando_1.png','corvo_chegando_2.png','corvo_chegando_3.png','corvo_pousado_1.png','corvo_pousado_3.png']]
beaks = [(.81,.4),(.846,.336),(.881,.387),(.928,.484),(.861,.113)]
wires = [.821,.84,.787,.755,.752]
key = sprite('red_key.png',round(S*.2))
font = ImageFont.truetype(str(ASSETS/'Fonts/B/atwriter.ttf'),round(H*.055))
rng = np.random.default_rng(7)
# Fixed textured paper, independently reconstructed rather than captured from Unity.
noise = rng.normal(0,1.1,(H,W,1))
yy,xx = np.mgrid[0:H,0:W]
edge = np.clip((np.sqrt(((xx/W-.5)**2)*1.1+((yy/H-.5)**2)*2.4)-.42)/.36,0,1)
base = np.clip(np.array([236,230,218])[None,None,:]+noise-edge[:,:,None]*12,0,255).astype('uint8')
paper = Image.fromarray(base).convert('RGBA')
pd = ImageDraw.Draw(paper)
for offset in range(-H,W+H,26):
    # Fine pencil hatching confined to corners.
    for x0,y0,x1,y1 in [(offset,0,offset+90,90),(offset,H,offset-90,H-90)]:
        pd.line((x0,y0,x1,y1),fill=(130,119,101,35),width=1)

def stroke(draw, y, left=0, right=W, width=2):
    for k in range(3):
        points=[(x,y+math.sin(x*.004+k*2)*2+k-1) for x in range(int(left),int(right)+1,4)]
        if len(points)>1: draw.line(points,fill=(40,36,31,175),width=width)

def render(t):
    im=paper.copy(); d=ImageDraw.Draw(im)
    shake=H*.006*math.exp(-7*(t-LAND))*math.sin((t-LAND)*24) if t>=LAND else 0
    stroke(d,WIRE+shake,width=2)
    if t>=.6:
        if t<LAND:
            p=t-.6; idx=int(p*9)%2; u=min(1,p/1.1); e=1-(1-u)**2
            x=(-S*1.1)*(1-e)+(W/2-S/2)*e
            y=WIRE-wires[idx]*S-H*.5*(1-e)+math.sin(p*14)*H*.006*(1-u)
        else:
            p=t-LAND; idx=2+min(1,int(p*7)) if p<2/7 else 4
            x=W/2-S/2; y=WIRE-wires[idx]*S+shake
        im.alpha_composite(frames[idx],(round(x),round(y)))
        # Ring anchor follows the original normalized beak coordinates.
        angle=12*math.exp(-3.5*(t-LAND))*math.sin((t-LAND)*7) if t>=LAND else math.sin((t-.6)*14)*6
        anchor=(key.width*.555,key.height*.075)
        canvas=Image.new('RGBA',(key.width*3,key.height*3))
        canvas.alpha_composite(key,(round(canvas.width/2-anchor[0]),round(canvas.height/2-anchor[1])))
        canvas=canvas.rotate(-angle,resample=Image.Resampling.BICUBIC)
        bx=x+beaks[idx][0]*S; by=y+beaks[idx][1]*S
        im.alpha_composite(canvas,(round(bx-canvas.width/2),round(by-canvas.height/2)))
    d=ImageDraw.Draw(im)
    text='TINKER CROW'
    count=min(len(text),max(0,int((t-TYPE)/.08)+1)) if t>=TYPE else 0
    spacing=H*.012
    widths=[d.textlength(ch,font=font) for ch in text]
    tx=(W-sum(widths)-spacing*(len(text)-1))/2
    for j,ch in enumerate(text):
        if j<count: d.text((tx,WIRE+H*.08),ch,font=font,fill=(26,24,23),anchor='lt')
        tx+=widths[j]+spacing
    if t>=HOLD:
        u=min(1,(t-HOLD)/.35); length=H*.42*(1-(1-u)**2)
        stroke(d,WIRE+H*.155,W/2-H*.21,W/2-H*.21+length,width=3)
    brightness=min(1,t/.6) if t<.6 else max(0,1-(t-FADE)/.9) if t>=FADE else 1
    rgb=im.convert('RGB')
    if brightness<1: rgb=Image.blend(Image.new('RGB',(W,H)),rgb,brightness)
    return rgb

def main():
    sample=OUT/'typewriter-source.wav'
    source=Path('C:/Users/loren/Documents/FMOD Studio/Echoes v2/Assets/Menu/typewriter 1.mp3')
    subprocess.run([str(FF),'-hide_banner','-loglevel','error','-y','-i',str(source),'-ac','1','-ar','48000',str(sample)],check=True)
    with wave.open(str(sample),'rb') as f: audio=np.frombuffer(f.readframes(f.getnframes()),dtype='<i2').astype(np.float64)/32768
    mixed=np.zeros(round(N/FPS*48000))
    for j,ch in enumerate('TINKER CROW'):
        if ch==' ': continue
        start=round((TYPE+j*.08)*48000); clip=audio[:round(.313469*48000)]*10**(-2/20)
        n=min(len(clip),len(mixed)-start); mixed[start:start+n]+=clip[:n]
    peak=np.max(np.abs(mixed)); mixed*=min(1,.92/max(peak,.001))
    wav=OUT/'tinker-crow-intro-audio.wav'
    with wave.open(str(wav),'wb') as f:
        f.setparams((1,2,48000,0,'NONE','not compressed')); f.writeframes((mixed*32767).astype('<i2').tobytes())
    target=OUT/'tinker-crow-abertura-4k.mp4'
    cmd=[str(FF),'-hide_banner','-loglevel','error','-y','-f','rawvideo','-pix_fmt','rgb24','-s',f'{W}x{H}','-r',str(FPS),'-i','pipe:0','-i',str(wav),'-c:v','libx264','-preset','medium','-crf','16','-pix_fmt','yuv420p','-c:a','aac','-b:a','256k','-movflags','+faststart','-shortest',str(target)]
    p=subprocess.Popen(cmd,stdin=subprocess.PIPE)
    for i in range(N):
        frame=render(i/FPS)
        if i==105: frame.save(OUT/'tinker-crow-video-preview.jpg',quality=95)
        p.stdin.write(frame.tobytes())
        if i%30==0: print(f'{i}/{N} frames',flush=True)
    p.stdin.close()
    if p.wait()!=0: raise RuntimeError('Encoding failed')
    print(json.dumps({'file':str(target),'frames':N,'seconds':N/FPS,'resolution':[W,H],'fps':FPS}),flush=True)

if __name__=='__main__': main()
