#!/usr/bin/env python3
"""Fetch author-published CC0 candidates; never execute downloaded code."""
from __future__ import annotations
import hashlib
import json
from pathlib import Path, PurePosixPath
import urllib.request
import zipfile

OUT = Path('ArtInspection')
SOURCES = [
    ('gladiator-pack', 'https://opengameart.org/sites/default/files/gladiatorpack.zip', 'Astarribadebirra / Nando', 'https://opengameart.org/content/gladiator-pack'),
    ('low-poly-warrior', 'https://opengameart.org/sites/default/files/basechar_0.zip', 'BlackScorp', 'https://opengameart.org/content/low-poly-warrior'),
]
MAX_BYTES = 20 * 1024 * 1024
ALLOWED = {'.obj', '.mtl', '.png', '.jpg', '.jpeg', '.txt', '.md', '.blend'}

def main() -> None:
    OUT.mkdir(exist_ok=True)
    manifest = []
    for name, url, author, page in SOURCES:
        request = urllib.request.Request(url, headers={'User-Agent': 'IronSandArena-licensed-asset-validation/1.0'})
        with urllib.request.urlopen(request, timeout=45) as response:
            if not response.url.startswith('https://opengameart.org/'):
                raise RuntimeError('Unexpected download redirect')
            data = response.read(MAX_BYTES + 1)
        if len(data) > MAX_BYTES:
            raise RuntimeError('Source archive too large')
        archive = OUT / (name + '.zip')
        archive.write_bytes(data)
        item = {'id': name, 'author': author, 'page': page, 'url': url, 'license': 'CC0-1.0', 'sha256': hashlib.sha256(data).hexdigest(), 'bytes': len(data), 'files': []}
        with zipfile.ZipFile(archive) as package:
            if sum(info.file_size for info in package.infolist()) > MAX_BYTES * 4:
                raise RuntimeError('Archive expansion limit exceeded')
            for info in package.infolist():
                relative = PurePosixPath(info.filename.replace('\\', '/'))
                if relative.is_absolute() or '..' in relative.parts:
                    raise RuntimeError('Unsafe source path')
                if info.is_dir() or relative.suffix.lower() not in ALLOWED:
                    continue
                dest = OUT / name / Path(*relative.parts)
                dest.parent.mkdir(parents=True, exist_ok=True)
                content = package.read(info)
                dest.write_bytes(content)
                entry = {'path': str(relative), 'bytes': len(content), 'sha256': hashlib.sha256(content).hexdigest()}
                if relative.suffix.lower() == '.obj':
                    text = content.decode('utf-8', errors='replace').splitlines()
                    vertices = [[float(value) for value in line.split()[1:4]] for line in text if line.startswith('v ')]
                    entry.update(vertices=len(vertices), faces=sum(line.startswith('f ') for line in text), objects=[line for line in text if line.startswith(('o ', 'g ', 'usemtl ', 'mtllib '))][:100])
                    if vertices:
                        entry['bounds_min'] = [min(v[i] for v in vertices) for i in range(3)]
                        entry['bounds_max'] = [max(v[i] for v in vertices) for i in range(3)]
                item['files'].append(entry)
        manifest.append(item)
    (OUT / 'manifest.json').write_text(json.dumps(manifest, indent=2) + '\n', encoding='utf-8')
    print(json.dumps(manifest, indent=2))

if __name__ == '__main__':
    main()
