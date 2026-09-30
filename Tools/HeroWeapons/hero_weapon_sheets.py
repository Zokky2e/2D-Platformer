"""Hero weapon sheets: builds per-weapon copies of the Hero Knight sprite sheet and their Unity assets.

The vendor sheet has the sword drawn into all 90 frames. For each frame this script finds the sword (the gold
guard plus the straight lavender blade from it), erases it, and draws another weapon at the same grip point and
angle. Only pixels that were transparent or part of the old sword are painted, so body parts that covered the
sword still cover the new weapon. Swing trails are kept (Dagger), turned magic blue (Staff, Wand) or removed (Bow).

Outputs (GUIDs in existing .meta files are kept, so regenerating doesn't break references):
  Assets/Sprites/Hero/HeroKnight_<Weapon>.png (+ .meta sliced like the original sheet)
  Assets/Animations/Hero/<Weapon>/HeroKnight_<Weapon>_<Clip>.anim (the hero clips, pointed at the new sheet)
  Assets/Animations/Hero/<Weapon>/HeroKnight_<Weapon>.overrideController (of HeroKnight_AnimController)

Usage, from the repository root:   python Tools/HeroWeapons/hero_weapon_sheets.py [--preview <folder>]
--preview also writes zoomed contact sheets of every frame for checking the result.
Standard library only. Weapon shapes are in WEAPONS; per-frame fixes for frames the detection misreads are in
OVERRIDES.
"""
import math, os, re, struct, sys, uuid, zlib

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), '..', '..'))
VENDOR = os.path.join(ROOT, 'Assets', 'Hero Knight - Pixel Art')
SOURCE_SHEET = os.path.join(VENDOR, 'Sprites', 'HeroKnight.png')
SOURCE_GUID = 'ae297123f091bbf4e8f1835eb98fb293'          # HeroKnight.png
SHEET_DIR = os.path.join(ROOT, 'Assets', 'Sprites', 'Hero')
ANIM_DIR = os.path.join(ROOT, 'Assets', 'Animations', 'Hero')
FW, FH, COLS, FRAMES = 100, 55, 10, 90


# ---- PNG reading and writing (8-bit RGBA, as the vendor sheet is) ----
def read_png(path):
    data = open(path, 'rb').read()
    assert data[:8] == b'\x89PNG\r\n\x1a\n', path
    pos, idat, w, h = 8, b'', 0, 0
    while pos < len(data):
        length, ctype = struct.unpack('>I4s', data[pos:pos + 8])
        chunk = data[pos + 8:pos + 8 + length]
        if ctype == b'IHDR':
            w, h, depth, color, _, _, interlace = struct.unpack('>IIBBBBB', chunk)
            assert depth == 8 and color == 6 and interlace == 0, 'expected 8-bit RGBA, not interlaced'
        elif ctype == b'IDAT':
            idat += chunk
        pos += 12 + length
    raw = zlib.decompress(idat)
    stride, rows, prev, i = w * 4, [], bytearray(w * 4), 0
    for _ in range(h):
        f = raw[i]
        line = bytearray(raw[i + 1:i + 1 + stride])
        i += 1 + stride
        for x in range(stride):
            a = line[x - 4] if x >= 4 else 0
            b = prev[x]
            c = prev[x - 4] if x >= 4 else 0
            if f == 1:
                line[x] = (line[x] + a) & 255
            elif f == 2:
                line[x] = (line[x] + b) & 255
            elif f == 3:
                line[x] = (line[x] + ((a + b) >> 1)) & 255
            elif f == 4:
                p = a + b - c
                pa, pb, pc = abs(p - a), abs(p - b), abs(p - c)
                line[x] = (line[x] + (a if pa <= pb and pa <= pc else b if pb <= pc else c)) & 255
        rows.append([tuple(line[x * 4:x * 4 + 4]) for x in range(w)])
        prev = line
    return rows


def write_png(path, pixels):
    h, w = len(pixels), len(pixels[0])
    raw = b''.join(b'\x00' + bytes(c for px in row for c in px) for row in pixels)

    def chunk(t, d):
        return struct.pack('>I', len(d)) + t + d + struct.pack('>I', zlib.crc32(t + d) & 0xffffffff)

    data = b'\x89PNG\r\n\x1a\n' + chunk(b'IHDR', struct.pack('>IIBBBBB', w, h, 8, 6, 0, 0, 0))
    data += chunk(b'IDAT', zlib.compress(raw, 9)) + chunk(b'IEND', b'')
    open(path, 'wb').write(data)


def get_frame(sheet, i):
    fx, fy = (i % COLS) * FW, (i // COLS) * FH
    return [row[fx:fx + FW] for row in sheet[fy:fy + FH]]


def zoom(frames, scale=2, cols=10, bg=(90, 90, 100, 255), gap=2):
    fh, fw = len(frames[0]), len(frames[0][0])
    rows_n = (len(frames) + cols - 1) // cols
    W, H = cols * (fw * scale + gap), rows_n * (fh * scale + gap)
    canvas = [[bg] * W for _ in range(H)]
    for n, f in enumerate(frames):
        ox, oy = (n % cols) * (fw * scale + gap), (n // cols) * (fh * scale + gap)
        for y in range(fh):
            for x in range(fw):
                if f[y][x][3]:
                    for dy in range(scale):
                        row = canvas[oy + y * scale + dy]
                        for dx in range(scale):
                            row[ox + x * scale + dx] = f[y][x]
    return canvas


BLADE = {(190, 194, 236), (127, 136, 171), (241, 241, 241), (215, 211, 214), (174, 212, 229)}
GOLD = {(231, 169, 51), (178, 102, 17)}
FLASH = (241, 241, 241)
N8 = [(-1, -1), (0, -1), (1, -1), (-1, 0), (1, 0), (-1, 1), (0, 1), (1, 1)]
ATTACK_FRAMES = range(18, 38)
FLASH_FRAMES = {45: 46, 48: 49}  # White hit-flash silhouette -> the frame with the same pose
# Frames the detection can't read: the blade runs behind the body and merges with the shoulder pad.
# grip = the gold guard in the hand, tip = the blade's far end, start = where the visible blade begins
OVERRIDES = {24: dict(grip=(60.0, 20.5), tip=(36.3, 27.6), start=16)}


def rgb(p):
    return p[:3] if p[3] else None


def components(frame, colors):
    h, w = len(frame), len(frame[0])
    seen = set()
    comps = []
    for y in range(h):
        for x in range(w):
            if (x, y) in seen or rgb(frame[y][x]) not in colors:
                continue
            stack, comp = [(x, y)], []
            seen.add((x, y))
            while stack:
                cx, cy = stack.pop()
                comp.append((cx, cy))
                for dx, dy in N8:
                    nx, ny = cx + dx, cy + dy
                    if 0 <= nx < w and 0 <= ny < h and (nx, ny) not in seen and rgb(frame[ny][nx]) in colors:
                        seen.add((nx, ny))
                        stack.append((nx, ny))
            comps.append(comp)
    return comps


def min_dist(a, b):
    return min(math.dist(p, q) for p in a for q in b)


def find_sword(frame, override=None):
    """Returns (guard pixels, blade pixels, grip point, unit direction, length) or None."""
    if override:
        return sword_from_override(frame, **override)
    golds = [g for g in components(frame, GOLD) if len(g) >= 2]
    blades = [b for b in components(frame, BLADE) if len(b) >= 6]
    best = None
    for g in golds:
        gc = (sum(p[0] for p in g) / len(g), sum(p[1] for p in g) / len(g))
        for b in blades:
            if min_dist(g, b) > 3.5:
                continue
            # Direction from the blade pixels near the guard, where the blade dominates any trail
            near = [p for p in b if 3 <= math.dist(p, gc) <= 16]
            if len(near) < 5:
                continue
            mx = sum(p[0] for p in near) / len(near) - gc[0]
            my = sum(p[1] for p in near) / len(near) - gc[1]
            n = math.hypot(mx, my)
            if n == 0:
                continue
            d = (mx / n, my / n)
            # Blade = pixels of this blob along that ray, within a narrow band and sword length
            band = [p for p in b if abs((p[0] - gc[0]) * d[1] - (p[1] - gc[1]) * d[0]) <= 2.2
                    and -1 <= (p[0] - gc[0]) * d[0] + (p[1] - gc[1]) * d[1] <= 30]
            length = max((p[0] - gc[0]) * d[0] + (p[1] - gc[1]) * d[1] for p in band)
            if length < 12:
                continue  # A shoulder pad or other highlight next to the hand, not a blade
            score = len(band)
            if best is None or score > best[0]:
                best = (score, g, band, gc, d, length)
    # A blade that passes behind the body: a stroke sticking out from it, lined up with the guard
    for g in golds:
        gc = (sum(p[0] for p in g) / len(g), sum(p[1] for p in g) / len(g))
        for b in blades:
            a, c = max(((p, q) for p in b for q in b), key=lambda pq: math.dist(*pq))
            span = math.dist(a, c)
            if span < 8 or body_contact(frame, b) >= 0.5:
                continue
            near_end, far_end = (a, c) if math.dist(a, gc) <= math.dist(c, gc) else (c, a)
            if math.dist(near_end, gc) > 22:
                continue
            L = math.dist(far_end, gc)
            d = ((far_end[0] + 0.5 - gc[0]) / L, (far_end[1] + 0.5 - gc[1]) / L)
            off = max(abs((p[0] + 0.5 - gc[0]) * d[1] - (p[1] + 0.5 - gc[1]) * d[0]) for p in b)
            if off > 2.5:
                continue
            score = len(b)
            if best is None or score > best[0]:
                best = (score, g, list(b), gc, d, L)
    if best is None:
        return find_sword_without_guard(frame, blades)
    _, g, band, gc, d, length = best
    return g, band, gc, d, length


def sword_from_override(frame, grip, tip, start):
    length = math.dist(grip, tip)
    d = ((tip[0] - grip[0]) / length, (tip[1] - grip[1]) / length)
    blade = []
    for y in range(len(frame)):
        for x in range(len(frame[0])):
            if rgb(frame[y][x]) not in BLADE:
                continue
            px, py = x + 0.5 - grip[0], y + 0.5 - grip[1]
            if abs(px * d[1] - py * d[0]) <= 2.0 and start <= px * d[0] + py * d[1] <= length + 1.5:
                blade.append((x, y))
    guard = [(x, y) for y in range(len(frame)) for x in range(len(frame[0]))
             if rgb(frame[y][x]) in GOLD and math.dist((x + 0.5, y + 0.5), grip) <= 3]
    return guard, blade, grip, d, length


def find_sword_without_guard(frame, blades):
    """When the hand hides the guard: the longest straight blade-coloured stroke, gripped at the end that
    touches the body."""
    best = None
    for b in blades:
        a, c = max(((p, q) for p in b for q in b), key=lambda pq: math.dist(*pq))
        length = math.dist(a, c)
        if length < 12 or len(b) > 3 * length:  # Not long and thin enough to be a blade
            continue
        score = length * (1 - body_contact(frame, b))  # Blades stick out; the shield rim hugs the shield
        if best is None or score > best[0]:
            best = (score, b, a, c)
    if best is None:
        return None
    _, b, a, c = best
    length = math.dist(a, c)
    h, w = len(frame), len(frame[0])
    def touches_body(p):
        return sum(1 for dx, dy in N8 if 0 <= p[0] + dx < w and 0 <= p[1] + dy < h
                   and frame[p[1] + dy][p[0] + dx][3] and rgb(frame[p[1] + dy][p[0] + dx]) not in BLADE)
    grip_end, tip = (a, c) if touches_body(a) >= touches_body(c) else (c, a)
    d = ((tip[0] - grip_end[0]) / length, (tip[1] - grip_end[1]) / length)
    grip = (grip_end[0] + 0.5 - 2 * d[0], grip_end[1] + 0.5 - 2 * d[1])  # The guard sits just inside the hand
    return [], b, grip, d, length


def body_contact(frame, comp):
    """Share of a blob's outside neighbours that are body (opaque, not blade-coloured) pixels."""
    cs = set(comp)
    h, w = len(frame), len(frame[0])
    total = body = 0
    for x, y in comp:
        for dx, dy in N8:
            nx, ny = x + dx, y + dy
            if (nx, ny) in cs:
                continue
            total += 1
            if 0 <= nx < w and 0 <= ny < h and frame[ny][nx][3] and rgb(frame[ny][nx]) not in BLADE:
                body += 1
    return body / total if total else 1


def trails(frame):
    """Swing-trail pixels (attack frames only): blade-coloured blobs barely touching the body."""
    out = []
    for comp in components(frame, BLADE):
        if len(comp) < 3:
            if body_contact(frame, comp) == 0:
                out += comp  # Loose trail dashes
            continue
        xs, ys = [p[0] for p in comp], [p[1] for p in comp]
        span = math.hypot(max(xs) - min(xs), max(ys) - min(ys))
        contact = body_contact(frame, comp)
        # Armor highlights are small; the shield rim is long but borders the shield's face
        if contact < 0.25 or (span >= 9 and contact < 0.45):
            out += comp
    return out


# ---- Weapons, drawn as vector shapes in weapon space: x runs from the grip (0) toward the tip along the old
# blade, y across it. Each shape is (kind, params, colour); later shapes paint over earlier ones.
LAVENDER, LAV_SHADE, WHITE = (190, 194, 236), (127, 136, 171), (241, 241, 241)
WOOD, WOOD_DARK, WOOD_LIGHT = (122, 65, 19), (88, 43, 3), (178, 102, 17)
GOLDC = (231, 169, 51)
CYAN, CYAN_LIGHT, STRING = (90, 190, 235), (190, 240, 255), (215, 211, 214)

WEAPONS = {
    'Dagger': [
        ('seg', (-3, 0, 0, 0, 1.6), WOOD_DARK),          # handle, mostly inside the fist
        ('seg', (1, 0, 10, 0, 2.2), LAV_SHADE),           # blade
        ('seg', (1, -0.5, 9, -0.5, 1.1), LAVENDER),
        ('seg', (10, 0, 12, 0, 1.0), LAVENDER),           # point
        ('seg', (0.5, -2.5, 0.5, 2.5, 1.2), GOLDC),       # crossguard
    ],
    'Wand': [
        ('seg', (-3, 0, 10, 0, 1.6), WOOD_DARK),
        ('seg', (-2, -0.4, 9, -0.4, 0.8), WOOD),
        ('circle', (11.2, 0, 1.7), CYAN),
        ('circle', (11.0, -0.4, 0.8), CYAN_LIGHT),
    ],
    'Staff': [
        ('seg', (-9, 0, 22, 0, 2.2), WOOD_DARK),
        ('seg', (-8, -0.5, 21, -0.5, 1.0), WOOD),
        ('seg', (21, -2.2, 21, 2.2, 1.2), GOLDC),         # collar under the orb
        ('circle', (24.2, 0, 2.3), CYAN),
        ('circle', (23.8, -0.6, 1.0), CYAN_LIGHT),
    ],
    'Bow': [  # Limbs across the old blade line, bowing forward along it; the string on the grip side
        ('curve', ((-2, -12), (5, 0), (-2, 12), 1.8), WOOD_DARK),
        ('curve', ((-1.5, -11), (4.4, 0), (-1.5, 11), 0.9), WOOD),
        ('seg', (-2.4, -12, -2.4, 12, 0.8), STRING),
        ('seg', (0, -1.5, 0, 1.5, 1.8), WOOD_LIGHT),      # grip wrap
    ],
}


def seg_dist(px, py, x1, y1, x2, y2):
    vx, vy = x2 - x1, y2 - y1
    L = vx * vx + vy * vy
    t = 0 if L == 0 else max(0, min(1, ((px - x1) * vx + (py - y1) * vy) / L))
    return math.hypot(px - (x1 + t * vx), py - (y1 + t * vy))


def curve_points(p0, p1, p2, steps=24):
    return [((1 - t) ** 2 * p0[0] + 2 * (1 - t) * t * p1[0] + t * t * p2[0],
             (1 - t) ** 2 * p0[1] + 2 * (1 - t) * t * p1[1] + t * t * p2[1]) for t in (i / steps for i in range(steps + 1))]


def shape_hit(shape, lx, ly):
    kind, params, _ = shape
    if kind == 'seg':
        x1, y1, x2, y2, width = params
        return seg_dist(lx, ly, x1, y1, x2, y2) <= width / 2
    if kind == 'circle':
        cx, cy, r = params
        return math.hypot(lx - cx, ly - cy) <= r
    if kind == 'curve':
        p0, p1, p2, width = params
        pts = curve_points(p0, p1, p2)
        return min(seg_dist(lx, ly, *a, *b) for a, b in zip(pts, pts[1:])) <= width / 2
    raise ValueError(kind)


def draw_weapon(frame, name, grip, d, paintable, flash=False):
    """Rasterises a weapon at the grip along direction d. Only paints pixels in `paintable` or transparent
    ones, so body parts that were in front of the old sword stay in front of the new weapon."""
    h, w = len(frame), len(frame[0])
    shapes = WEAPONS[name]
    gx, gy = grip
    nx, ny = -d[1], d[0]  # weapon-space y axis
    for y in range(max(0, int(gy) - 30), min(h, int(gy) + 31)):
        for x in range(max(0, int(gx) - 30), min(w, int(gx) + 31)):
            if frame[y][x][3] and (x, y) not in paintable:
                continue
            px, py = x + 0.5 - gx, y + 0.5 - gy
            lx, ly = px * d[0] + py * d[1], px * nx + py * ny
            colour = None
            for shape in shapes:
                if shape_hit(shape, lx, ly):
                    colour = shape[2]
            if colour:
                frame[y][x] = (FLASH if flash else colour) + (255,)


MAGIC = {(190, 194, 236): (120, 215, 250), (127, 136, 171): (60, 140, 210), (241, 241, 241): (215, 250, 255),
         (215, 211, 214): (170, 235, 255), (174, 212, 229): (140, 225, 255)}


def build(sheet, weapon, report=None):
    out = [row[:] for row in sheet]
    for i in range(90):
        fx, fy = (i % COLS) * FW, (i // COLS) * FH
        frame = [row[fx:fx + FW] for row in sheet[fy:fy + FH]]
        flash = i in FLASH_FRAMES
        source = frame
        if flash:
            j = FLASH_FRAMES[i]
            sx, sy = (j % COLS) * FW, (j // COLS) * FH
            source = [row[sx:sx + FW] for row in sheet[sy:sy + FH]]
        found = find_sword(source, OVERRIDES.get(FLASH_FRAMES.get(i, i)))
        if found is None:
            if report is not None:
                report.append(i)
            continue
        guard, blade, grip, d, length = found
        removed = set(guard) | set(blade)
        if flash:  # Erase the silhouette's sword where the matching frame had it (plus a pixel around)
            removed = {(x + dx, y + dy) for x, y in removed for dx in (-1, 0, 1) for dy in (-1, 0, 1)
                       if 0 <= x + dx < FW and 0 <= y + dy < FH and rgb(frame[y + dy][x + dx]) == FLASH}
            removed -= body_core(frame, removed)
        new = [row[:] for row in frame]
        for x, y in removed:
            new[y][x] = (0, 0, 0, 0)
        # Loose blade-coloured specks just past the old tip, which the blade band missed
        for comp in components(new, BLADE):
            if len(comp) <= 4 and body_contact(new, comp) == 0 and all(
                    abs((x + 0.5 - grip[0]) * d[1] - (y + 0.5 - grip[1]) * d[0]) <= 3.5
                    and length - 3 <= (x + 0.5 - grip[0]) * d[0] + (y + 0.5 - grip[1]) * d[1] <= length + 8
                    for x, y in comp):
                for x, y in comp:
                    new[y][x] = (0, 0, 0, 0)
                    removed.add((x, y))
        if i in ATTACK_FRAMES and not flash:
            trail = trails(new)
            for x, y in trail:
                if weapon == 'Bow':
                    new[y][x] = (0, 0, 0, 0)
                elif weapon in ('Staff', 'Wand'):
                    new[y][x] = MAGIC[rgb(new[y][x])] + (255,)
        draw_weapon(new, weapon, grip, d, removed, flash)
        for y in range(FH):
            out[fy + y][fx:fx + FW] = new[y]
    return out


def body_core(frame, removed):
    """Silhouette pixels to keep in a flash frame: those with a solid body around them (not the thin blade)."""
    keep = set()
    for x, y in removed:
        solid = sum(1 for dx in range(-2, 3) for dy in range(-2, 3)
                    if 0 <= x + dx < FW and 0 <= y + dy < FH and frame[y + dy][x + dx][3])
        if solid >= 20:
            keep.add((x, y))
    return keep


# ---- Unity assets ----
def existing_guid(meta_path):
    if os.path.exists(meta_path):
        found = re.search(r'^guid: (\w+)', open(meta_path, encoding='utf-8').read(), re.M)
        if found:
            return found.group(1)
    return uuid.uuid4().hex


def write_text(path, text):
    os.makedirs(os.path.dirname(path), exist_ok=True)
    open(path, 'w', encoding='utf-8', newline='\n').write(text)


def folder_meta(path):
    if not os.path.exists(path + '.meta'):
        write_text(path + '.meta', f"fileFormatVersion: 2\nguid: {uuid.uuid4().hex}\nfolderAsset: yes\n"
                                   "DefaultImporter:\n  externalObjects: {}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n")


def native_meta(path, main_id):
    guid = existing_guid(path + '.meta')
    write_text(path + '.meta', f"fileFormatVersion: 2\nguid: {guid}\nNativeFormatImporter:\n  externalObjects: {{}}\n"
                               f"  mainObjectFileID: {main_id}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n")
    return guid


def hero_clips():
    """The vendor hero clips that show frames of HeroKnight.png (the no-blood and no-effect variants use
    other sheets and keep the sword)."""
    folder = os.path.join(VENDOR, 'Animations')
    clips = []
    for name in sorted(os.listdir(folder)):
        path = os.path.join(folder, name)
        if name.startswith('HeroKnight_') and name.endswith('.anim') and SOURCE_GUID in open(path, encoding='utf-8').read():
            clips.append(path)
    return clips


def write_unity_assets(weapon, sheet_pixels, controller_guid):
    folder_meta(os.path.dirname(SHEET_DIR))
    folder_meta(SHEET_DIR)
    png = os.path.join(SHEET_DIR, f'HeroKnight_{weapon}.png')
    os.makedirs(SHEET_DIR, exist_ok=True)
    write_png(png, sheet_pixels)
    # Same import settings and slicing as the vendor sheet, so frame n has the same name and id
    source_meta = open(SOURCE_SHEET + '.meta', encoding='utf-8').read()
    source_meta = re.sub(r'^AssetOrigin:\n(?:  .*\n)*', '', source_meta, flags=re.M)  # The vendor file's store origin
    sheet_guid = existing_guid(png + '.meta')
    write_text(png + '.meta', re.sub(r'^guid: \w+', f'guid: {sheet_guid}', source_meta, count=1, flags=re.M))

    out_dir = os.path.join(ANIM_DIR, weapon)
    folder_meta(ANIM_DIR)
    folder_meta(out_dir)
    overrides = []
    for clip_path in hero_clips():
        original_guid = existing_guid(clip_path + '.meta')
        clip_name = os.path.basename(clip_path)[len('HeroKnight_'):-len('.anim')]
        new_name = f'HeroKnight_{weapon}_{clip_name}'
        text = open(clip_path, encoding='utf-8').read().replace(SOURCE_GUID, sheet_guid)
        text = re.sub(r'^  m_Name: .*$', f'  m_Name: {new_name}', text, count=1, flags=re.M)
        new_path = os.path.join(out_dir, new_name + '.anim')
        write_text(new_path, text)
        overrides.append((original_guid, native_meta(new_path, 7400000)))

    entries = ''.join(f"  - m_OriginalClip: {{fileID: 7400000, guid: {o}, type: 2}}\n"
                      f"    m_OverrideClip: {{fileID: 7400000, guid: {n}, type: 2}}\n" for o, n in overrides)
    controller = os.path.join(out_dir, f'HeroKnight_{weapon}.overrideController')
    write_text(controller, f"""%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!221 &22100000
AnimatorOverrideController:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_Name: HeroKnight_{weapon}
  m_Controller: {{fileID: 9100000, guid: {controller_guid}, type: 2}}
  m_Clips:
{entries}""")
    return native_meta(controller, 22100000)


def main():
    preview = sys.argv[sys.argv.index('--preview') + 1] if '--preview' in sys.argv else None
    controller_guid = existing_guid(os.path.join(VENDOR, 'Animations', 'HeroKnight_AnimController.controller.meta'))
    sheet = read_png(SOURCE_SHEET)
    for weapon in WEAPONS:
        missing = []
        result = build(sheet, weapon, missing)
        guid = write_unity_assets(weapon, result, controller_guid)
        print(f'{weapon}: controller {guid}' + (f', no sword found in frames {missing}' if missing else ''))
        if preview:
            os.makedirs(preview, exist_ok=True)
            for part in range(3):
                frames = [get_frame(result, i) for i in range(part * 30, part * 30 + 30)]
                write_png(os.path.join(preview, f'{weapon}_{part}.png'), zoom(frames))


if __name__ == '__main__':
    main()
