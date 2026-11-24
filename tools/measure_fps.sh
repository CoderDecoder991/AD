#!/usr/bin/env bash
# Frame pacing of the running game on the connected Android device, from SurfaceFlinger's present times
# (works on review builds that have no on-screen readout). SurfaceFlinger keeps ~127 frames (~2 s), so this
# takes several short samples.  Usage: tools/measure_fps.sh [samples=5] [gap_seconds=3]
ADB="${ADB:-/c/Program Files/Unity/Hub/Editor/6000.6.10f1/Editor/Data/PlaybackEngines/AndroidPlayer/SDK/platform-tools/adb.exe}"
N="${1:-5}"; GAP="${2:-3}"
L=$("$ADB" shell dumpsys SurfaceFlinger --list | tr -d '\r' | grep -oE "[0-9a-f]+ SurfaceView\[com.placeholder.airdefense/com.unity3d.player.UnityPlayerGameActivity\](@0)?\(BLAST\)#[0-9]+" | head -1)
[ -z "$L" ] && { echo "game surface not found"; exit 1; }
TMP="$(mktemp)"; command -v cygpath >/dev/null && TMP_W="$(cygpath -w "$TMP")" || TMP_W="$TMP"
for i in $(seq 1 "$N"); do
  "$ADB" shell "dumpsys SurfaceFlinger --latency-clear" >/dev/null
  sleep 2.2
  "$ADB" shell "dumpsys SurfaceFlinger --latency \"$L\"" | tr -d '\r' > "$TMP"
  python -I - "$TMP_W" <<'PYEOF'
import sys, statistics
rows=[l.split() for l in open(sys.argv[1]).read().splitlines()[1:] if len(l.split())==3]
t=sorted(int(r[1]) for r in rows if 0<int(r[1])<9e18)
d=[(b-a)/1e6 for a,b in zip(t,t[1:])]
if len(d)>5:
    span=(t[-1]-t[0])/1e9
    print(f"  {len(t)/span:5.1f} fps  median {statistics.median(d):4.1f} ms  worst {max(d):5.1f} ms  >20ms {sum(1 for x in d if x>20):3d}/{len(d)}")
else:
    print("  no frames (paused or backgrounded?)")
PYEOF
  sleep "$GAP"
done
rm -f "$TMP"
