"""Repairs a Bannerlord save whose strings section was broken by a string longer than 32767 bytes.

The save system stores each string entry's size as a 16-bit short (SaveContext.SaveStringTo), so a long string
corrupts the size field. This script shrinks the Calradic Exchange state string (drops market.guilds, which refills
in 28 days) and rewrites the size fields. Usage: repair_save.py <in.sav> <out.sav> [--check]
"""
import base64
import json
import struct
import sys
import zlib


def read_i32(b, o):
    return struct.unpack_from('<i', b, o)[0], o + 4


def parse_game_data(raw):
    o = 0
    hlen, o = read_i32(raw, o)
    header = raw[o:o + hlen]; o += hlen
    n, o = read_i32(raw, o)
    objs = []
    for _ in range(n):
        l, o = read_i32(raw, o)
        objs.append(raw[o:o + l]); o += l
    n, o = read_i32(raw, o)
    conts = []
    for _ in range(n):
        l, o = read_i32(raw, o)
        conts.append(raw[o:o + l]); o += l
    slen, o = read_i32(raw, o)
    strings = raw[o:o + slen]; o += slen
    assert o == len(raw), (o, len(raw))
    return header, objs, conts, strings


def parse_strings(s):
    prefix = s[:18]
    count = struct.unpack_from('<i', s, 14)[0]
    o = 18
    entries = []
    for i in range(count):
        head = s[o:o + 7]
        size = struct.unpack_from('<h', s, o + 7)[0]
        blen = struct.unpack_from('<i', s, o + 9)[0]
        text = s[o + 13:o + 13 + blen].decode('utf-8')
        entries.append((head, size, text))
        o += 13 + blen
    assert o == len(s), (o, len(s))
    return prefix, entries


def build_strings(prefix, entries):
    out = bytearray(prefix)
    for head, _, text in entries:
        data = text.encode('utf-8')
        size = 4 + len(data)
        assert size <= 32767, size
        out += head + struct.pack('<h', size) + struct.pack('<i', len(data)) + data
    return bytes(out)


def build_game_data(header, objs, conts, strings):
    out = bytearray(struct.pack('<i', len(header)) + header)
    out += struct.pack('<i', len(objs))
    for b in objs:
        out += struct.pack('<i', len(b)) + b
    out += struct.pack('<i', len(conts))
    for b in conts:
        out += struct.pack('<i', len(b)) + b
    out += struct.pack('<i', len(strings)) + strings
    return bytes(out)


def main():
    src, dst = sys.argv[1], sys.argv[2]
    check = '--check' in sys.argv
    d = open(src, 'rb').read()
    mlen = struct.unpack_from('<i', d, 0)[0]
    meta = d[:4 + mlen]
    raw = zlib.decompressobj(-15).decompress(d[4 + mlen:])
    header, objs, conts, strings = parse_game_data(raw)
    prefix, entries = parse_strings(strings)
    bad = [(i, e[1], len(e[2].encode('utf-8')) + 4) for i, e in enumerate(entries) if e[1] != len(e[2].encode('utf-8')) + 4]
    print(f'{src}: {len(entries)} strings, {len(bad)} with a wrong size field: {bad[:5]}')
    if check:
        return
    fixed = []
    for head, size, text in entries:
        if '"schemaVersion"' in text and len(text.encode('utf-8')) + 4 > 32767:
            state = json.loads(text)
            before = len(text)
            state.get('market', {}).pop('guilds', None)
            plain = json.dumps(state, separators=(',', ':'), ensure_ascii=False)
            # Same format as SaveChunks.Pack (ADR-028): "CXZ1|0|1|" + base64(raw deflate(utf8 json)).
            comp = zlib.compressobj(9, zlib.DEFLATED, -15)
            text = 'CXZ1|0|1|' + base64.b64encode(comp.compress(plain.encode('utf-8')) + comp.flush()).decode('ascii')
            print(f'  exchange state: {before} chars -> {len(plain)} without market.guilds -> one {len(text)}-char chunk, schema {state.get("schemaVersion")}')
        fixed.append((head, size, text))
    new_strings = build_strings(prefix, fixed)
    body = build_game_data(header, objs, conts, new_strings)
    comp = zlib.compressobj(6, zlib.DEFLATED, -15)
    packed = comp.compress(body) + comp.flush()
    open(dst, 'wb').write(meta + packed)
    # Verify the result parses cleanly.
    d2 = open(dst, 'rb').read()
    raw2 = zlib.decompressobj(-15).decompress(d2[4 + mlen:])
    h2, o2, c2, s2 = parse_game_data(raw2)
    _, e2 = parse_strings(s2)
    assert all(e[1] == len(e[2].encode('utf-8')) + 4 for e in e2)
    assert h2 == header and o2 == objs and c2 == conts
    print(f'  wrote {dst}: {len(d2)} bytes, all {len(e2)} string sizes valid')


main()
