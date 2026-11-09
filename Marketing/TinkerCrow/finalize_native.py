"""Package original Unity-rendered frames, retaining capture timestamps."""
from pathlib import Path
import json, subprocess, wave
import numpy as np

ROOT=Path(__file__).resolve().parents[2]
OUT=Path(__file__).resolve().parent/'Native'
FF=ROOT/'tmp/tinker-crow-tools/ffmpeg.exe'
meta=json.loads((OUT/'capture.json').read_text())
times=meta['timestamps']
frames=ROOT/'tmp/tinker-native-frames'
frames.mkdir(exist_ok=True)
subprocess.run([str(FF),'-hide_banner','-loglevel','error','-y','-i',str(OUT/'native-frames.mkv'),'-compression_level','1',str(frames/'frame-%04d.png')],check=True)
# Skip the first extra layout-wait black frame. No animation frame is removed.
first=1
origin=times[first]
listing=['ffconcat version 1.0']
for i in range(first,len(times)):
    listing.extend([f"file '{(frames/f'frame-{i+1:04d}.png').as_posix()}'",'option framerate 1000',f'duration {(times[i+1]-times[i]) if i+1<len(times) else 1/30:.6f}'])
(OUT/'native-timing.ffconcat').write_text('\n'.join(listing)+'\n')
# Find the first typed glyph and first audible key to remove audio initialization
# lead-in. Both image and audio come from the actual StudioIntro/FMOD execution.
raw=subprocess.check_output([str(FF),'-v','error','-i',str(OUT/'native-frames.mkv'),'-vf','scale=480:270','-f','rawvideo','-pix_fmt','gray','pipe:1'])
images=np.frombuffer(raw,np.uint8).reshape(-1,270,480)
first_letter=next(i for i in range(35,len(times)) if (images[i,190:215,150:330]<65).sum()>15)
audio=ROOT/'Echoes/fmodoutput.wav'
with wave.open(str(audio)) as w:
    sr=w.getframerate(); ch=w.getnchannels()
    samples=np.frombuffer(w.readframes(w.getnframes()),'<i2').reshape(-1,ch)
audible=np.flatnonzero(np.max(abs(samples.astype('int32')),axis=1)>100)[0]/sr
audio_origin=origin+audible-times[first_letter]
duration=times[-1]-origin+1/30
target=OUT/'tinker-crow-abertura-original-4k.mp4'
subprocess.run([str(FF),'-hide_banner','-loglevel','error','-y','-safe','0','-f','concat','-i',str(OUT/'native-timing.ffconcat'),'-ss',f'{audio_origin:.6f}','-i',str(audio),'-map','0:v:0','-map','1:a:0','-t',f'{duration:.6f}','-r','30','-fps_mode','cfr','-c:v','libx264','-preset','medium','-crf','15','-pix_fmt','yuv420p','-c:a','aac','-b:a','256k','-movflags','+faststart',str(target)],check=True)
meta.update({'packaged_duration':duration,'audio_leadin_removed':audio_origin,'first_keystroke_frame':first_letter,'first_frame':first})
(OUT/'export-details.json').write_text(json.dumps(meta,indent=2))
print(str(target),flush=True)
