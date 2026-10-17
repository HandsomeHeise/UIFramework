using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using UIFramework.Core.Controls;
using UIFramework.Core.Rendering;
using UIFramework.Core.Skinning;

namespace UIFramework.Controls
{
    /// <summary>
    /// Zweizustands-Schalter: Control-Fläche wie ein Label (ElementKeys.CheckBox,
    /// ForeColor ist die Textfarbe, kein Rahmen ums Ganze) plus gezeichnete Box
    /// (ElementKeys.CheckBoxIndicator, ihr ForeColor ist die Hakenfarbe) mit Text
    /// rechts daneben. Eigenständig neben ButtonEditBase — es gibt kein Textfeld.
    /// Umschalten per Klick und Leertaste; fokussierbar mit Fokusring.
    ///
    /// Enthält bewusst keinen einzigen Farbwert — alles Sichtbare kommt aus dem Skin.
    /// </summary>
    [ToolboxItem(true)]
    [DefaultEvent("CheckedChanged")]
    public class CheckEdit : SkinnedControl, IGridCellEditor
    {
        private bool _checked;

        public CheckEdit()
        {
            SetStyle(ControlStyles.Selectable, true);
            TabStop = true;
            Size = new Size(120, 24);
            AutoSize = true;
        }

        protected override string ElementKey
        {
            get { return ElementKeys.CheckBox; }
        }

        /// <summary>
        /// Wenn gesetzt (Standard), richtet sich die Größe nach dem tatsächlichen
        /// Textbedarf (siehe <see cref="GetPreferredSize"/>) statt der festen
        /// 120×24-Vorgabe aus dem Konstruktor. Ohne das schneidet der Text ab,
        /// sobald er breiter ist als die feste Größe.
        /// </summary>
        [Category("Layout")]
        [DefaultValue(true)]
        public override bool AutoSize
        {
            get { return base.AutoSize; }
            set
            {
                if (base.AutoSize == value) return;
                base.AutoSize = value;
                if (value) AdjustSize();
            }
        }

        [Category("Behavior")]
        [DefaultValue(false)]
        public bool Checked
        {
            get { return _checked; }
            set
            {
                if (_checked == value) return;
                _checked = value;
                Invalidate();
                OnCheckedChanged(EventArgs.Empty);
            }
        }

        public event EventHandler CheckedChanged;

        protected virtual void OnCheckedChanged(EventArgs e)
        {
            var handler = CheckedChanged;
            if (handler != null) handler(this, e);
        }

        /// <summary>Bestätigen erbeten (Enter) — Gegenstück zu ButtonEditBase,
        /// ohne gemeinsame Basis: CheckEdit hat kein Textfeld (Spec 3a/3b).</summary>
        public event EventHandler EditConfirmed;

        /// <summary>Verwerfen erbeten (Escape).</summary>
        public event EventHandler EditCancelled;

        Control IGridCellEditor.EditorControl
        {
            get { return this; }
        }

        object IGridCellEditor.EditValue
        {
            get { return Checked; }
            set
            {
                bool parsed;
                Checked = value is bool b ? b : bool.TryParse(value == null ? "" : value.ToString(), out parsed) && parsed;
            }
        }

        void IGridCellEditor.BeginWith(string text)
        {
            // Kein Textkern — Lostippen verpufft hier bewusst (Plan-Entscheidung 4).
        }

        void IGridCellEditor.FocusEditor()
        {
            Focus();
        }

        event EventHandler IGridCellEditor.ConfirmRequested
        {
            add { EditConfirmed += value; }
            remove { EditConfirmed -= value; }
        }

        event EventHandler IGridCellEditor.CancelRequested
        {
            add { EditCancelled += value; }
            remove { EditCancelled -= value; }
        }

        protected override void PaintContent(Graphics g, ElementAppearance appearance)
        {
            var content = SkinPainter.GetContentRectangle(ClientRectangle, appearance, DeviceDpi);
            int side = Math.Min(content.Height, content.Width);
            var box = new Rectangle(content.Left, content.Top + (content.Height - side) / 2, side, side);

            // Fläche und Rahmen der Box malt die Basis NICHT — sie hat den
            // Control-Hintergrund gemalt (jetzt rahmenlos, wie ein Label). Die
            // Box ist hier Inhalt und hat ihre EIGENE Erscheinung
            // (CheckBoxIndicator): sie bleibt die klassische Checkbox-Optik
            // (Fläche/Rahmen/Haken je Zustand), unabhängig davon, wie das
            // Control drumherum aussieht.
            var indicator = SkinManager.Current.GetAppearance(ElementKeys.CheckBoxIndicator, State);

            SkinPainter.DrawBackground(g, box, indicator, DeviceDpi);
            SkinPainter.DrawBorder(g, box, indicator, DeviceDpi);

            if (_checked)
            {
                // Haken als Polylinie im inneren Drittel der Box.
                var pen = ResourceCache.Shared.GetPen(indicator.ForeColor, Math.Max(2, side / 8));
                int x0 = box.Left + side / 4;
                int y0 = box.Top + side / 2;
                int x1 = box.Left + side * 2 / 5;
                int y1 = box.Top + side * 7 / 10;
                int x2 = box.Left + side * 3 / 4;
                int y2 = box.Top + side * 3 / 10;
                g.DrawLines(pen, new[] { new Point(x0, y0), new Point(x1, y1), new Point(x2, y2) });
            }

            if (!string.IsNullOrEmpty(Text))
            {
                var textRect = new Rectangle(box.Right + box.Width / 3, content.Top,
                    Math.Max(0, content.Right - box.Right - box.Width / 3), content.Height);
                SkinPainter.DrawText(g, Text, textRect, appearance, DeviceDpi, ContentAlignment.MiddleLeft);
            }
        }

        protected override void OnMouseClick(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left && Enabled)
            {
                if (CanFocus) Focus();
                Checked = !Checked;
            }
            base.OnMouseClick(e);
        }

        protected override bool IsInputKey(Keys keyData)
        {
            if (keyData == Keys.Space || keyData == Keys.Enter || keyData == Keys.Escape) return true;
            return base.IsInputKey(keyData);
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            PerformKey(e.KeyCode);
            if (e.KeyCode == Keys.Space || e.KeyCode == Keys.Enter || e.KeyCode == Keys.Escape) e.Handled = true;
            base.OnKeyDown(e);
        }

        /// <summary>Tastenlogik separat, damit Tests sie ohne Fokus-Maschinerie
        /// treiben können (Muster: GridControl.PerformKey).</summary>
        internal void PerformKey(Keys key)
        {
            // Symmetrie (Befund F4): vorher prüfte nur Space auf Enabled — Enter
            // und Escape feuerten auch an einem deaktivierten Editor.
            if (!Enabled) return;

            if (key == Keys.Space) Checked = !Checked;
            else if (key == Keys.Enter) RaiseEditConfirmed();
            else if (key == Keys.Escape) RaiseEditCancelled();
        }

        /// <summary>Gegenstück zu ButtonEditBase.RaiseEditConfirmed — CheckEdit hat
        /// keine gemeinsame Basis dafür (kein Textkern).</summary>
        private void RaiseEditConfirmed()
        {
            var handler = EditConfirmed;
            if (handler != null) handler(this, EventArgs.Empty);
        }

        private void RaiseEditCancelled()
        {
            var handler = EditCancelled;
            if (handler != null) handler(this, EventArgs.Empty);
        }

        /// <summary>Bestätigen bei Fokusverlust (Befund F1) — CheckEdit hat kein
        /// natives Textkern-LostFocus wie ButtonEditBase._inner; das eigene
        /// OnLostFocus ist hier der einzige Anknüpfungspunkt.</summary>
        protected override void OnLostFocus(EventArgs e)
        {
            RaiseEditConfirmed();
            base.OnLostFocus(e);
        }

        // ---- Nur für Tests --------------------------------------------------

        /// <summary>Simuliert Fokusverlust — Fokus selbst ist kopflos nicht
        /// auslösbar (Muster: ButtonEditBase.RaiseLostFocusForTests).</summary>
        internal void RaiseLostFocusForTests()
        {
            OnLostFocus(EventArgs.Empty);
        }

        public override Size GetPreferredSize(Size proposedSize)
        {
            var appearance = CurrentAppearance;

            using (var bitmap = new Bitmap(1, 1))
            using (var g = Graphics.FromImage(bitmap))
            {
                var textSize = SkinPainter.MeasureText(g, string.IsNullOrEmpty(Text) ? "Xg" : Text,
                    appearance, DeviceDpi);
                // Box (quadratisch = Texthöhe) + Lücke (Drittel) + Text.
                int box = textSize.Height;
                var content = new Size(box + box / 3 + textSize.Width, Math.Max(box, textSize.Height));
                return SkinPainter.InflateByPadding(content, appearance, DeviceDpi);
            }
        }

        protected override void OnTextChanged(EventArgs e)
        {
            if (AutoSize) AdjustSize();
            Invalidate();
            base.OnTextChanged(e);
        }

        protected override void OnDpiChangedAfterParent(EventArgs e)
        {
            if (AutoSize) AdjustSize();
            base.OnDpiChangedAfterParent(e);
        }

        private void AdjustSize()
        {
            Size = GetPreferredSize(Size.Empty);
        }
    }
}
