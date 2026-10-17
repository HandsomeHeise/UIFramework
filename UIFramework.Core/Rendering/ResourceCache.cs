using System;
using System.Collections.Generic;
using System.Drawing;
using UIFramework.Core.Dpi;
using UIFramework.Core.Skinning;

namespace UIFramework.Core.Rendering
{
    /// <summary>
    /// Hält GDI+-Ressourcen am Leben, die sonst pro Paint-Durchgang neu entstünden.
    /// Beim Grid läuft dieser Pfad später tausendfach pro Sekunde.
    ///
    /// Nicht threadsicher, und das ist Absicht: der Cache gehört dem UI-Thread,
    /// eine Sperre auf dem Zeichenpfad wäre teurer als ihr Nutzen.
    ///
    /// WICHTIG: Aufrufer dürfen die zurückgegebenen Objekte NIEMALS disposen.
    /// Sie gehören dem Cache und werden bei Clear() freigegeben.
    /// </summary>
    public sealed class ResourceCache : IDisposable
    {
        /// <summary>Die Instanz, die der SkinManager bei Skin-Wechsel leert.</summary>
        public static ResourceCache Shared { get; } = new ResourceCache();

        private struct PenKey : IEquatable<PenKey>
        {
            public readonly int Argb;
            public readonly int Width;

            public PenKey(Color color, int width)
            {
                Argb = color.ToArgb();
                Width = width;
            }

            public bool Equals(PenKey other)
            {
                return Argb == other.Argb && Width == other.Width;
            }

            public override bool Equals(object obj)
            {
                return obj is PenKey && Equals((PenKey)obj);
            }

            public override int GetHashCode()
            {
                unchecked { return (Argb * 397) ^ Width; }
            }
        }

        private struct FontKey : IEquatable<FontKey>
        {
            public readonly FontSpec Spec;
            public readonly int Dpi;

            public FontKey(FontSpec spec, int dpi)
            {
                Spec = spec;
                Dpi = dpi;
            }

            public bool Equals(FontKey other)
            {
                return Spec.Equals(other.Spec) && Dpi == other.Dpi;
            }

            public override bool Equals(object obj)
            {
                return obj is FontKey && Equals((FontKey)obj);
            }

            public override int GetHashCode()
            {
                unchecked { return (Spec.GetHashCode() * 397) ^ Dpi; }
            }
        }

        private readonly Dictionary<int, SolidBrush> _brushes = new Dictionary<int, SolidBrush>();
        private readonly Dictionary<PenKey, Pen> _pens = new Dictionary<PenKey, Pen>();
        private readonly Dictionary<FontKey, Font> _fonts = new Dictionary<FontKey, Font>();
        private bool _disposed;

        internal int Count
        {
            get { return _brushes.Count + _pens.Count + _fonts.Count; }
        }

        public SolidBrush GetBrush(Color color)
        {
            ThrowIfDisposed();

            int key = color.ToArgb();
            SolidBrush brush;
            if (!_brushes.TryGetValue(key, out brush))
            {
                brush = new SolidBrush(color);
                _brushes.Add(key, brush);
            }
            return brush;
        }

        public Pen GetPen(Color color, int width)
        {
            ThrowIfDisposed();
            if (width <= 0) throw new ArgumentOutOfRangeException(nameof(width), "Die Stiftbreite muss positiv sein.");

            var key = new PenKey(color, width);
            Pen pen;
            if (!_pens.TryGetValue(key, out pen))
            {
                pen = new Pen(color, width);
                _pens.Add(key, pen);
            }
            return pen;
        }

        public Font GetFont(FontSpec spec, int dpi)
        {
            ThrowIfDisposed();

            var key = new FontKey(spec, dpi);
            Font font;
            if (!_fonts.TryGetValue(key, out font))
            {
                // GraphicsUnit.Pixel ist entscheidend: die Umrechnung Punkt→Pixel
                // haben wir schon gemacht. Mit GraphicsUnit.Point würde GDI+ ein
                // zweites Mal skalieren — die Schrift wäre bei 144 DPI viel zu groß.
                float pixels = DpiScale.PointsToPixels(spec.SizeInPoints, dpi);
                font = new Font(spec.Family, pixels, spec.Style, GraphicsUnit.Pixel);
                _fonts.Add(key, font);
            }
            return font;
        }

        /// <summary>
        /// Gibt alle gehaltenen Ressourcen frei. Wird vom SkinManager bei jedem
        /// Skin-Wechsel gerufen — die alten Farben werden nie wieder gebraucht.
        /// </summary>
        public void Clear()
        {
            foreach (var brush in _brushes.Values) brush.Dispose();
            foreach (var pen in _pens.Values) pen.Dispose();
            foreach (var font in _fonts.Values) font.Dispose();

            _brushes.Clear();
            _pens.Clear();
            _fonts.Clear();
        }

        public void Dispose()
        {
            if (_disposed) return;
            Clear();
            _disposed = true;
        }

        private void ThrowIfDisposed()
        {
            if (_disposed) throw new ObjectDisposedException(nameof(ResourceCache));
        }
    }
}
