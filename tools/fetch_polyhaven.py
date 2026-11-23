"""Download CC0 assets from Poly Haven (https://polyhaven.com) with MD5 verification.

Uses the public API (api.polyhaven.com). Retries each file until its MD5 matches,
because this PC's connection drops long downloads. Writes a manifest with source
URLs and licence for docs/ASSET_REGISTER.md.

Usage: python -I tools/fetch_polyhaven.py <out_dir> <asset_id> [<asset_id> ...]
"""
import hashlib
import json
import os
import sys
import time
import urllib.request

UA = {"User-Agent": "AirDefenseAssetFetcher/1.0"}
RES_TEX = os.environ.get("PH_RES", "1k")  # e.g. PH_RES=2k for close-up surfaces
RES_HDRI = "2k"


def get_json(url):
    for attempt in range(6):
        try:
            with urllib.request.urlopen(urllib.request.Request(url, headers=UA), timeout=60) as r:
                return json.loads(r.read().decode("utf-8"))
        except Exception as e:  # noqa: BLE001
            print(f"    retry json ({e})")
            time.sleep(2 + attempt * 2)
    raise RuntimeError("failed: " + url)


def download(url, path, md5, size):
    os.makedirs(os.path.dirname(path), exist_ok=True)
    for attempt in range(1, 16):
        if os.path.exists(path) and os.path.getsize(path) == size:
            if hashlib.md5(open(path, "rb").read()).hexdigest() == md5:
                return True
        try:
            with urllib.request.urlopen(urllib.request.Request(url, headers=UA), timeout=120) as r, open(path, "wb") as f:
                while True:
                    chunk = r.read(1 << 16)
                    if not chunk:
                        break
                    f.write(chunk)
        except Exception as e:  # noqa: BLE001
            print(f"    retry {attempt} {os.path.basename(path)} ({type(e).__name__})")
            time.sleep(min(10, attempt))
    return False


def files_for(asset_type, files):
    """Returns [(url, relative_path, md5, size)] for the chosen resolution/format.
    asset_type from the API: 0 = HDRI, 1 = texture, 2 = model (models also list texture maps)."""
    out = []
    if asset_type == 0:
        f = files["hdri"][RES_HDRI]["hdr"]
        out.append((f["url"], os.path.basename(f["url"]), f["md5"], f["size"]))
    elif asset_type == 2:  # model
        g = files["gltf"][RES_TEX]["gltf"]
        out.append((g["url"], os.path.basename(g["url"]), g["md5"], g["size"]))
        for rel, inc in g.get("include", {}).items():
            out.append((inc["url"], rel, inc["md5"], inc["size"]))
    else:  # texture set
        for m in ("Diffuse", "nor_gl", "Rough", "AO", "Displacement"):
            if m == "Displacement":
                continue  # not used on mobile
            if m in files and RES_TEX in files[m]:
                fmt = "jpg" if "jpg" in files[m][RES_TEX] else next(iter(files[m][RES_TEX]))
                f = files[m][RES_TEX][fmt]
                out.append((f["url"], os.path.basename(f["url"]), f["md5"], f["size"]))
    return out


def main():
    out_dir, ids = sys.argv[1], sys.argv[2:]
    manifest = []
    failed = []
    for asset_id in ids:
        info = get_json(f"https://api.polyhaven.com/info/{asset_id}")
        files = get_json(f"https://api.polyhaven.com/files/{asset_id}")
        entries = files_for(info.get("type"), files)
        total = sum(e[3] for e in entries)
        print(f"{asset_id}: {len(entries)} files, {total / 1e6:.1f} MB")
        ok = True
        for url, rel, md5, size in entries:
            if not download(url, os.path.join(out_dir, asset_id, rel), md5, size):
                ok = False
                failed.append(f"{asset_id}/{rel}")
        manifest.append({
            "id": asset_id,
            "name": info.get("name"),
            "authors": list((info.get("authors") or {}).keys()),
            "licence": "CC0 1.0",
            "source": f"https://polyhaven.com/a/{asset_id}",
            "bytes": total,
            "verified_md5": ok,
        })
    with open(os.path.join(out_dir, "polyhaven_manifest.json"), "w", encoding="utf-8") as f:
        json.dump(manifest, f, indent=2, ensure_ascii=False)
    print("FAILED:" if failed else "ALL VERIFIED", failed or "")


if __name__ == "__main__":
    main()
