from pathlib import Path
from PIL import Image, ImageFilter

REPO = Path(__file__).resolve().parents[1]
PROJECT = REPO / "SKY_LAND_ATUALIZADO" / "SkyLand"
if not PROJECT.is_dir():
    PROJECT = REPO / "SkyLand"  # permite testar o script a partir do ZIP extraído

REPLACEMENTS = {
    "#8B5CF6": "#8B17C0",
    "#7C3AED": "#8B17C0",
    "#D946EF": "#C45BE0",
    "#A855F7": "#A64ACF",
    "#A78BFA": "#D6A0F0",
    "#C4B5FD": "#E9D5F5",
    "#4C1D95": "#351044",
    "#8A2FB8": "#8B17C0",
    "#4A8B5CF6": "#4A8B17C0",
    "#208B5CF6": "#208B17C0",
}


def update_text(path: Path, replacements: dict[str, str]) -> None:
    text = path.read_text(encoding="utf-8-sig")
    original = text
    for old, new in replacements.items():
        text = text.replace(old, new)
    if text != original:
        path.write_text(text, encoding="utf-8", newline="\n")


def main() -> None:
    if not PROJECT.is_dir():
        raise FileNotFoundError(f"Pasta do código-fonte não encontrada: {PROJECT}")

    # Apply the requested purple identity to every XAML page/control/theme.
    for xaml in PROJECT.rglob("*.xaml"):
        update_text(xaml, REPLACEMENTS)

    # Keep the theme names and the default palette consistent with the new identity.
    deep_blue = PROJECT / "Themes" / "Theme.DeepBlue.xaml"
    update_text(deep_blue, {"TEMA: AZUL PROFUNDO (padrão)": "TEMA: ROXO ESCURO (padrão)"})
    black_theme = PROJECT / "Themes" / "Theme.Black.xaml"
    update_text(black_theme, {"#A955F7": "#A64ACF", "#2D1F5E": "#351044"})

    # Explicit WPF window icon (the project file already embeds the same ICO as its app icon).
    main_window = PROJECT / "MainWindow.xaml"
    content = main_window.read_text(encoding="utf-8-sig")
    if 'Icon="pack://application:,,,/Assets/app.ico"' not in content:
        content = content.replace(
            '        Title="SKY LAND"\n',
            '        Title="SKY LAND"\n        Icon="pack://application:,,,/Assets/app.ico"\n',
            1,
        )
        main_window.write_text(content, encoding="utf-8", newline="\n")

    # Version the source and Windows manifest consistently for this build.
    for relative in ("App.xaml.cs", "SkyLand.csproj"):
        path = PROJECT / relative
        update_text(path, {"1.0.0": "1.0.2", "1.0.1": "1.0.2"})
    update_text(PROJECT / "app.manifest", {'version="1.0.0.0"': 'version="1.0.2.0"', 'version="1.0.1.0"': 'version="1.0.2.0"'})

    # Generate a real multi-resolution ICO from the original SKY LAND artwork.
    logo_path = PROJECT / "Assets" / "logo.png"
    icon_path = PROJECT / "Assets" / "app.ico"
    if not logo_path.is_file():
        raise FileNotFoundError(f"Logotipo original não encontrado: {logo_path}")
    image = Image.open(logo_path).convert("RGBA")
    image = image.filter(ImageFilter.UnsharpMask(radius=1.0, percent=115, threshold=3))
    # ICO encoders need a source at least as large as the maximum requested frame.
    image = image.resize((256, 256), Image.Resampling.LANCZOS)
    image.save(
        icon_path,
        format="ICO",
        sizes=[(16, 16), (32, 32), (48, 48), (64, 64), (128, 128), (256, 256)],
    )
    sizes = Image.open(icon_path).ico.sizes()
    expected = {(16, 16), (32, 32), (48, 48), (64, 64), (128, 128), (256, 256)}
    if not expected.issubset(sizes):
        raise RuntimeError(f"O ICO não contém todas as resoluções. Encontradas: {sorted(sizes)}")

    # Refresh the root README with the actual source and release locations.
    readme = """# SKY LAND

Aplicativo Windows x64 para gerenciamento dos executores e ferramentas do SKY LAND.

## Código-fonte

O projeto WPF/.NET 8 está em `SKY_LAND_ATUALIZADO/SkyLand`.

## Download

Baixe o executável mais recente em [Releases](https://github.com/Joaovictor18383/Sky-land-/releases).

## Identidade visual

- Cor principal: `#8B17C0`
- Tema escuro com acentos roxos
- Ícone ICO com resoluções de 16, 32, 48, 64, 128 e 256 pixels

## Créditos

Joaovictor36484
"""
    (REPO / "README.md").write_text(readme, encoding="utf-8", newline="\n")

    print(f"Branding atualizado. Ícone contém: {sorted(sizes)}")


if __name__ == "__main__":
    main()
