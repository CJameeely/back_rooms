"""Original, deterministic ambient and interaction sounds; no downloaded music."""
from pathlib import Path
import math,random,wave,struct
out=Path(__file__).resolve().parents[1]/'Assets/Resources/Audio'
out.mkdir(parents=True,exist_ok=True)
def save(name,duration,fn):
 rng=random.Random(103);frames=bytearray();state=0
 for i in range(int(44100*duration)):
  t=i/44100;state=.985*state+.015*rng.uniform(-1,1)
  v=fn(t,state,rng)
  frames.extend(struct.pack('<h',round(max(-1,min(1,v))*32767)))
 with wave.open(str(out/(name+'.wav')),'wb') as w:w.setnchannels(1);w.setsampwidth(2);w.setframerate(44100);w.writeframes(frames)
save('RoomTone',24,lambda t,n,r: .10*math.sin(2*math.pi*50*t)+.035*math.sin(2*math.pi*100*t)+.025*math.sin(2*math.pi*(125/3)*t)+.30*n*min(1,t/.2,(24-t)/.2))
save('Footstep',.19,lambda t,n,r: .45*math.exp(-t*35)*math.sin(2*math.pi*(105-180*t)*t)+.14*r.uniform(-1,1)*math.exp(-t*30))
save('Button',.10,lambda t,n,r: .20*math.exp(-t*65)*math.sin(2*math.pi*650*t)+.12*r.uniform(-1,1)*math.exp(-t*100))
