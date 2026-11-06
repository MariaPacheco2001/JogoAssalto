"""Package existing Windows and macOS builds; verify bytes and Unix permissions.

Usage: python Tools/PackageRelease.py --repo . --output /path/to/packages
"""
import argparse
import hashlib
import json
import os
from pathlib import Path
import plistlib
import stat
import struct
import zipfile

MACHO = {b'\xfe\xed\xfa\xce', b'\xce\xfa\xed\xfe', b'\xfe\xed\xfa\xcf',
         b'\xcf\xfa\xed\xfe', b'\xca\xfe\xba\xbe', b'\xbe\xba\xfe\xca'}


def universal_architectures(path):
    data = path.read_bytes()
    assert data[:4] == b'\xca\xfe\xba\xbe', f'Not a universal Mach-O: {path}'
    count = struct.unpack_from('>I', data, 4)[0]
    ids = {struct.unpack_from('>I', data, 8 + i * 20)[0] for i in range(count)}
    assert ids == {0x01000007, 0x0100000C}, f'Unexpected architectures: {ids}'
    return ['x86_64', 'arm64']


def package(build, readme, archive_path, root, mac=False):
    source_files = sorted(p for p in build.rglob('*') if p.is_file() or p.is_symlink())
    assert source_files, f'Empty build: {build}'
    records = {}
    with zipfile.ZipFile(archive_path, 'w', zipfile.ZIP_DEFLATED, compresslevel=6) as z:
        entries = [(root + '/LEIA-ME.txt', readme.read_bytes(), 0o100644)]
        for path in source_files:
            relative = path.relative_to(build).as_posix()
            name = root + '/' + (build.name + '/' if mac else '') + relative
            if path.is_symlink():
                data, mode = os.readlink(path).encode('utf-8'), stat.S_IFLNK | 0o777
            else:
                data = path.read_bytes()
                executable = mac and (data[:4] in MACHO or relative.startswith('Contents/MacOS/'))
                mode = stat.S_IFREG | (0o755 if executable else 0o644)
            entries.append((name, data, mode))
        for name, data, mode in entries:
            info = zipfile.ZipInfo(name, (2026, 10, 9, 12, 0, 0))
            info.create_system = 3
            info.external_attr = mode << 16
            info.compress_type = zipfile.ZIP_DEFLATED
            z.writestr(info, data)
            records[name] = (hashlib.sha256(data).digest(), mode)
    with zipfile.ZipFile(archive_path) as z:
        assert z.testzip() is None
        assert len(z.infolist()) == len(records)
        for info in z.infolist():
            digest, mode = records[info.filename]
            assert hashlib.sha256(z.read(info)).digest() == digest, info.filename
            assert info.external_attr >> 16 == mode, info.filename
            assert info.create_system == 3
    return {'archive': archive_path.name, 'files': len(records),
            'bytes': archive_path.stat().st_size,
            'sha256': hashlib.sha256(archive_path.read_bytes()).hexdigest(),
            'content_and_permissions_verified': True}


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('--repo', type=Path, required=True)
    parser.add_argument('--output', type=Path, required=True)
    args = parser.parse_args()
    args.output.mkdir(parents=True, exist_ok=True)
    win = args.repo / 'Builds/AssaltoWindows'
    mac = args.repo / 'Builds/AssaltoMacOS/GolpeCalculado.app'
    for required in ['GolpeCalculado.exe', 'UnityPlayer.dll', 'GolpeCalculado_Data', 'MonoBleedingEdge']:
        assert (win / required).exists(), required
    plist = plistlib.loads((mac / 'Contents/Info.plist').read_bytes())
    executable = mac / 'Contents/MacOS' / plist['CFBundleExecutable']
    architectures = universal_architectures(executable)
    report = {'mac_architectures': architectures,
              'mac_minimum_os': plist.get('LSMinimumSystemVersion'),
              'mac_execution_tested': False, 'packages': []}
    for platform, build, root in [('Windows', win, 'Golpe Calculado'), ('macOS-Universal', mac, 'Golpe Calculado macOS')]:
        result = package(build, args.repo / 'Docs/Distribuicao' / ('LEIA-ME-' + platform + '.txt'),
                         args.output / ('Golpe-Calculado-' + platform + '.zip'), root,
                         mac=platform.startswith('macOS'))
        report['packages'].append(result)
    (args.output / 'SHA256SUMS.txt').write_text(''.join(
        p['sha256'] + '  ' + p['archive'] + '\n' for p in report['packages']), encoding='utf-8')
    (args.output / 'VerificacaoPacotes.json').write_text(json.dumps(report, indent=2) + '\n', encoding='utf-8')
    print(json.dumps(report, indent=2))


if __name__ == '__main__':
    main()
