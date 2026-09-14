#!/usr/bin/env python3
"""Publish only explicitly inspected CC0 render outputs to the existing feature branch.
This script never merges, force-pushes, downloads viewer geometry, or runs asset code.
Credentials are handled by GitHub CLI; they are never embedded in URLs or printed.
"""
from __future__ import annotations
import argparse
import hashlib
import json
from pathlib import Path, PurePosixPath
import shutil
import subprocess
import tempfile
import zipfile

REPO = 'joshua051/test_20241124_helloworld'
BRANCH = 'feat/arena-prototype-v0.1.0'
RUNTIME = ('Gladiator.json', 'skin.png', 'metal2.png', 'asset_manifest.json')
EXPORTS = ('IronSand_Gladiator.fbx', 'IronSand_Gladiator.glb', 'IronSand_Gladiator_Stage.blend', 'gladiator_guard.png', 'gladiator_rear.png', 'gladiator_strike.png', 'gladiator_arena.png', 'render_manifest.json')
SCRIPTS = ('tools/art/inspect_sources.py', 'tools/art/build_model_data.py', 'tools/art/render_gladiator.py')

def run(*args: str) -> str:
    return subprocess.check_output(args, text=True).strip()

def api(path: str) -> dict:
    return json.loads(run('gh', 'api', f'repos/{REPO}/{path}'))

def digest(data: bytes) -> str:
    return hashlib.sha256(data).hexdigest()

def require(condition: bool, message: str) -> None:
    if not condition:
        raise RuntimeError(message)

def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument('--run-id', type=int, required=True)
    parser.add_argument('--artifact-id', type=int, required=True)
    parser.add_argument('--artifact-sha256', required=True)
    parser.add_argument('--model-sha256', required=True)
    args = parser.parse_args()
    require(run('git', 'branch', '--show-current') == BRANCH, 'Refusing a non-feature-branch checkout')
    require(not run('git', 'status', '--porcelain'), 'Publication requires a clean checkout')
    head_before = run('git', 'rev-parse', 'HEAD')
    artifact = api(f'actions/artifacts/{args.artifact_id}')
    job = api(f'actions/runs/{args.run_id}')
    require(artifact['name'] == 'gladiator-integrated-assets' and not artifact['expired'], 'Wrong or expired artifact')
    require(artifact['workflow_run']['id'] == args.run_id, 'Artifact/run mismatch')
    require(job['status'] == 'completed' and job['conclusion'] == 'success', 'Rendering must actually succeed first')
    require(job['head_branch'] == BRANCH and job['path'] == '.github/workflows/gladiator-asset-pipeline.yml', 'Unexpected render producer')
    source_sha = job['head_sha']
    require(artifact['workflow_run']['head_sha'] == source_sha, 'Render revision mismatch')
    require(artifact.get('digest') == 'sha256:' + args.artifact_sha256, 'Unexpected archive digest')
    subprocess.run(['git', 'merge-base', '--is-ancestor', source_sha, 'HEAD'], check=True)
    for script in SCRIPTS:
        prior = subprocess.check_output(['git', 'show', f'{source_sha}:{script}'])
        require(prior == Path(script).read_bytes(), 'Render source changed since inspection: ' + script)
    require(artifact['size_in_bytes'] < 32 * 1024 * 1024, 'Oversized artifact')
    selected = {f'Assets/Resources/Gladiators/{name}': f'Assets/Resources/Gladiators/{name}' for name in RUNTIME}
    selected.update({f'ArtExports/{name}': f'Art/Gladiator/{name}' for name in EXPORTS})
    receipt = {'schema': 1, 'status': 'ACTUAL_BLENDER_RENDER_PUBLISHED_UNITY_NOT_RUN', 'repository': REPO, 'branch': BRANCH, 'parent_commit': head_before, 'render_source_commit': source_sha, 'render_run_id': args.run_id, 'artifact_id': args.artifact_id, 'artifact_sha256': args.artifact_sha256, 'model_sha256': args.model_sha256, 'files': []}
    with tempfile.TemporaryDirectory(prefix='gladiator-publication-') as temp:
        archive_path = Path(temp) / 'artifact.zip'
        with archive_path.open('wb') as output:
            subprocess.run(['gh', 'api', f'repos/{REPO}/actions/artifacts/{args.artifact_id}/zip'], stdout=output, check=True)
        require(digest(archive_path.read_bytes()) == args.artifact_sha256, 'Downloaded archive hash differs')
        with zipfile.ZipFile(archive_path) as archive:
            infos = archive.infolist()
            require(sum(item.file_size for item in infos) < 100 * 1024 * 1024, 'Archive expansion limit exceeded')
            require(len({item.filename for item in infos}) == len(infos), 'Duplicate archive entries')
            for item in infos:
                path = PurePosixPath(item.filename.replace('\\', '/'))
                require(not path.is_absolute() and '..' not in path.parts, 'Unsafe archive member')
            render = json.loads(archive.read('ArtExports/render_manifest.json'))
            manifest = json.loads(archive.read('Assets/Resources/Gladiators/asset_manifest.json'))
            require(render['status'] == 'BLENDER_RENDERED_NOT_UNITY_VALIDATED', 'Unexpected render status')
            require(render['source_data_sha256'] == manifest['model_sha256'] == args.model_sha256, 'Model data provenance mismatch')
            require(digest(archive.read('Assets/Resources/Gladiators/Gladiator.json')) == args.model_sha256, 'Model byte mismatch')
            for item in render['files']:
                raw = archive.read('ArtExports/' + item['name'])
                require(len(raw) == item['bytes'] and digest(raw) == item['sha256'], 'Corrupt render/export: ' + item['name'])
            for source, destination in selected.items():
                raw = archive.read(source)
                require(len(raw) < 20 * 1024 * 1024, 'File exceeds repository size budget')
                path = Path(destination); path.parent.mkdir(parents=True, exist_ok=True); path.write_bytes(raw)
                receipt['files'].append({'path': destination, 'bytes': len(raw), 'sha256': digest(raw)})
    # Keep the authors' provenance next to the standard exports as well as runtime data.
    credits = Path('Art/Gladiator/CREDITS.md')
    shutil.copyfile('Assets/Resources/Gladiators/CREDITS.md', credits)
    receipt_path = Path('docs/GLADIATOR_ASSET_RECEIPT.json')
    receipt_path.write_text(json.dumps(receipt, indent=2) + '\n', encoding='utf-8')
    paths = list(selected.values()) + [str(credits), str(receipt_path)]
    subprocess.run(['git', 'add', '--', *paths], check=True)
    subprocess.run(['git', 'diff', '--cached', '--check'], check=True)
    subprocess.run(['python3', 'tools/repository_guard.py'], check=True)
    remote_head = run('git', 'ls-remote', 'origin', f'refs/heads/{BRANCH}').split()[0]
    require(remote_head == head_before, 'Feature branch changed during publication; refusing overwrite')
    subprocess.run(['git', '-c', 'user.name=github-actions[bot]', '-c', 'user.email=41898282+github-actions[bot]@users.noreply.github.com', 'commit', '-m', 'art: publish inspected CC0 gladiator skin, exports and real Blender renders'], check=True)
    subprocess.run(['git', 'push', 'origin', f'HEAD:refs/heads/{BRANCH}'], check=True)
    published_sha = run('git', 'rev-parse', 'HEAD')
    require(run('git', 'ls-remote', 'origin', f'refs/heads/{BRANCH}').split()[0] == published_sha, 'Remote commit verification failed')
    out = Path(tempfile.gettempdir()) / 'iron-sand-art-delivery'; out.mkdir(exist_ok=True)
    subprocess.run(['git', 'archive', '--format=zip', '--prefix=iron-sand-arena/', '-o', str(out / 'iron-sand-arena-with-gladiator.zip'), 'HEAD'], check=True)
    (out / 'publication.json').write_text(json.dumps({'published_commit': published_sha, 'parent_commit': head_before, 'render_run_id': args.run_id, 'render_artifact_id': args.artifact_id, 'model_sha256': args.model_sha256, 'Unity_U1_U7': 'NOT_RUN'}, indent=2) + '\n', encoding='utf-8')
    print('PUBLISHED_COMMIT=' + published_sha)
    print('DELIVERY_DIRECTORY=' + str(out))

if __name__ == '__main__':
    main()
