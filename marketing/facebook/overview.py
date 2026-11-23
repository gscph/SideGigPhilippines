# Makes out/overview.png: every feed image, carousel slide and "how it works" image on one sheet, plus the cover.
#   python marketing/facebook/overview.py   (after build.py)
import pathlib

from PIL import Image, ImageDraw, ImageFont

OUT = pathlib.Path(__file__).resolve().parent / "out"
TILE, COLS, GAP, HEAD = 320, 6, 16, 56


def section(title, files):
    rows = (len(files) + COLS - 1) // COLS
    img = Image.new("RGB", (COLS * (TILE + GAP) + GAP, HEAD + rows * (TILE + GAP)), "white")
    ImageDraw.Draw(img).text((GAP, 14), title, fill="#5B2A86", font=font())
    for i, f in enumerate(files):
        img.paste(Image.open(f).convert("RGB").resize((TILE, TILE)),
                  (GAP + (i % COLS) * (TILE + GAP), HEAD + (i // COLS) * (TILE + GAP)))
    return img


def font():
    for name in ("arialbd.ttf", "DejaVuSans-Bold.ttf"):
        try:
            return ImageFont.truetype(name, 30)
        except OSError:
            continue
    return ImageFont.load_default()


def main():
    parts = [
        section("Posts for households (feed)", sorted(OUT.glob("h*-feed.png"), key=lambda p: int(p.name[1:].split("-")[0]))),
        section("Posts for skilled workers (feed)", sorted(OUT.glob("w*-feed.png"))),
        section("Carousels and how it works", sorted(OUT.glob("carousel-*.png")) + sorted(OUT.glob("how-*.png"))),
    ]
    width = parts[0].width
    cover = Image.open(OUT / "page-cover.png").convert("RGB")
    cover = cover.resize((width - 2 * GAP, round(cover.height * (width - 2 * GAP) / cover.width)))
    cov = Image.new("RGB", (width, HEAD + cover.height + GAP), "white")
    ImageDraw.Draw(cov).text((GAP, 14), "Page cover", fill="#5B2A86", font=font())
    cov.paste(cover, (GAP, HEAD))
    parts.append(cov)
    sheet = Image.new("RGB", (width, sum(p.height for p in parts)), "white")
    y = 0
    for p in parts:
        sheet.paste(p, (0, y))
        y += p.height
    sheet.save(OUT / "overview.png")
    print("wrote out/overview.png", sheet.size)


if __name__ == "__main__":
    main()
