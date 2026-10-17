using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using UIFramework.Core.Controls;
using UIFramework.Core.Rendering;
using UIFramework.Core.Skinning;

namespace UIFramework.Controls
{
    /// <summary>
    /// Reiterleiste (SkinButton-artige TabHeaderItem-Reihe) über einem
    /// Inhaltsbereich, der pro Reiter genau ein Control anzeigt und alle
    /// anderen versteckt statt sie neu zu erzeugen -- Zustand der Tab-Inhalte
    /// bleibt beim Wechsel erhalten.
    ///
    /// Kein Schließen-Button, kein Drag-Reorder: eine feste Tab-Menge reicht
    /// für die vorgesehenen Verwender.
    ///
    /// Enthält bewusst keinen einzigen Farbwert — alles Sichtbare kommt aus dem Skin.
    /// </summary>
    [ToolboxItem(true)]
    [DefaultEvent("SelectedIndexChanged")]
    public class SkinTabControl : SkinnedControl
    {
        /// <summary>
        /// Ein einzelner Reiter. Public statt private, damit Tests einen Klick
        /// simulieren können, ohne echte Mauskoordinaten zu treffen (siehe
        /// RaiseClickForTests) -- derselbe Grund, aus dem ProbeSkinButton in den
        /// Tests lebt, hier aber direkt am Typ selbst, weil TabHeaderItem sonst
        /// keinerlei öffentliche API hätte, an die ein Test andocken könnte.
        /// </summary>
        public sealed class TabHeaderItem : SkinnedControl
        {
            private readonly SkinTabControl _owner;

            internal TabHeaderItem(SkinTabControl owner, string title)
            {
                _owner = owner;
                Title = title;
                SetStyle(ControlStyles.Selectable, false);
                TabStop = false;
            }

            public string Title { get; }

            public bool Active { get; internal set; }

            protected override string ElementKey
            {
                get { return ElementKeys.Tab; }
            }

            protected override bool IsSelected
            {
                get { return Active; }
            }

            protected override bool ShowFocusRing
            {
                get { return false; }
            }

            protected override void PaintContent(Graphics g, ElementAppearance appearance)
            {
                if (!string.IsNullOrEmpty(Title))
                    SkinPainter.DrawPaddedText(g, Title, ClientRectangle, appearance, DeviceDpi, ContentAlignment.MiddleCenter);
            }

            public override Size GetPreferredSize(Size proposedSize)
            {
                var appearance = CurrentAppearance;
                using (var bitmap = new Bitmap(1, 1))
                using (var g = Graphics.FromImage(bitmap))
                {
                    var measured = SkinPainter.MeasureText(g, Title ?? "", appearance, DeviceDpi);
                    return SkinPainter.InflateByPadding(measured, appearance, DeviceDpi);
                }
            }

            protected override void OnClick(EventArgs e)
            {
                base.OnClick(e);
                _owner.ActivateTab(this);
            }

            /// <summary>Nur für Tests: ein echter Mausklick ist im Testhost nicht
            /// zuverlässig simulierbar (siehe SkinButtonTests-Kommentar zu
            /// Focused). OnClick ist protected und von hier aus aufrufbar, weil
            /// dieser Code innerhalb derselben Klasse steht.</summary>
            public void RaiseClickForTests()
            {
                OnClick(EventArgs.Empty);
            }
        }

        private readonly SkinPanel _headerStrip = new SkinPanel();
        private readonly SkinPanel _contentHost = new SkinPanel { Dock = DockStyle.Fill };
        private readonly List<TabHeaderItem> _headers = new List<TabHeaderItem>();
        private readonly List<Control> _pages = new List<Control>();
        private int _selectedIndex = -1;

        public SkinTabControl()
        {
            SetStyle(ControlStyles.Selectable, false);
            TabStop = false;
            Size = new Size(400, 300);

            _headerStrip.Dock = DockStyle.Top;
            _headerStrip.Height = 32;

            Controls.Add(_contentHost);
            Controls.Add(_headerStrip);
        }

        protected override string ElementKey
        {
            get { return ElementKeys.Panel; }
        }

        public event EventHandler SelectedIndexChanged;

        public void AddTab(string title, Control content)
        {
            if (content == null) throw new ArgumentNullException(nameof(content));

            var header = new TabHeaderItem(this, title);
            header.Size = header.GetPreferredSize(Size.Empty);
            header.Height = _headerStrip.Height;
            header.Location = new Point(HeaderRight(), 0);
            _headerStrip.Controls.Add(header);

            content.Visible = false;
            content.Dock = DockStyle.Fill;
            _contentHost.Controls.Add(content);

            _headers.Add(header);
            _pages.Add(content);

            if (_selectedIndex < 0) ActivateTab(header);
        }

        [Browsable(false)]
        public int SelectedIndex
        {
            get { return _selectedIndex; }
            set
            {
                if (value < 0 || value >= _headers.Count)
                    throw new ArgumentOutOfRangeException(nameof(value));
                ActivateTab(_headers[value]);
            }
        }

        private int HeaderRight()
        {
            int right = 0;
            foreach (var header in _headers) right = Math.Max(right, header.Right);
            return right;
        }

        private void ActivateTab(TabHeaderItem header)
        {
            int index = _headers.IndexOf(header);
            if (index < 0 || index == _selectedIndex) return;

            for (int i = 0; i < _headers.Count; i++)
            {
                _headers[i].Active = i == index;
                _headers[i].Invalidate();
                _pages[i].Visible = i == index;
            }

            _selectedIndex = index;

            var handler = SelectedIndexChanged;
            if (handler != null) handler(this, EventArgs.Empty);
        }
    }
}
