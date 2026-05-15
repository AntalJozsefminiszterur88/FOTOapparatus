# FOTOapparatus

A projekt most ket reszbol all:

- `src/FOTOapparatus.Avalonia/` - az uj, .NET 8 + Avalonia desktop alkalmazas Linuxra
- a gyokerben levo Python/PySide6 kod - a regi Windowsos referencia-verzio

Az uj Avalonia alkalmazas a regi GUI elrendezeset es mukodeset koveti: van tesztgomb, felveteli mod valasztas, teljes kepernyo vagy egyedi terulet, programablak-kep, Discord mod, datum-pozicio, idozitok, mappavalasztas, autostart, idle-check, talcaikon es egypeldanyos futas.

## Uj Linuxos Avalonia verzio

### Build

```bash
dotnet build src/FOTOapparatus.Avalonia/FOTOapparatus.Avalonia.csproj --ignore-failed-sources
```

### Futtatas

```bash
dotnet run --project src/FOTOapparatus.Avalonia/FOTOapparatus.Avalonia.csproj
```

Rejtett inditashoz:

```bash
dotnet run --project src/FOTOapparatus.Avalonia/FOTOapparatus.Avalonia.csproj -- --start-hidden
```

### Linux fuggosegek

Az Avalonia app a platformfunkciok egy reszet kulso Linux eszkozokre tamasztja:

- `gnome-screenshot` - teljes kepernyo vagy aktiv ablak mentese
- `wmctrl` - futo ablakok listazasa es fokuszba hozasa
- X11 - a Discord hotkey kuldeshez es az idle-idomereshez

Fontos: a teljes funkcionalitas jelenleg X11 alatt a legerosebb. Wayland alatt a programkep, a Discord hotkey es az inaktivitasi figyeles korlatozott lehet.

### Konfiguracio

Az uj app ugyanoda menti a beallitasokat, ahova a regi verzio is:

- config: `~/Documents/UMKGL Solutions/FOTOapp/fotoapp_config.json`
- kepek: `~/Pictures/FOTOapp_Screenshots`

## Regi Python referencia

A gyokerben levo Python kod megmaradt referencianak, hogy az eredeti mukodes es kinezet visszanezhato legyen.

### Futtatas

```bash
pip install -r requirements.txt
python main.py
```

## Mappak

- `src/FOTOapparatus.Avalonia/` - uj Avalonia desktop alkalmazas
- `core/` - regi Python logika
- `gui/` - regi PySide6 felulet
- `assets/` - kozos ikonok es statikus allomanyok
