using System;
using System.Collections.Generic;
using System.Windows.Forms;
using UIFramework.Core.Rendering;
using UIFramework.Core.Skinning.Skins;

namespace UIFramework.Core.Skinning
{
    /// <summary>
    /// Hält den aktiven Skin und benachrichtigt alle Controls, wenn er wechselt.
    ///
    /// Die Registrierung läuft über SCHWACHE Referenzen. Der übliche Weg — Controls
    /// abonnieren ein statisches Event — hält jedes jemals erzeugte Control am Leben,
    /// bis die App endet. Bei einer App, die Fenster öffnet und schließt, ist das
    /// ein Leck mit Ansage. SkinnedControl.Dispose meldet zusätzlich sauber ab;
    /// die schwachen Referenzen sind das Netz darunter.
    ///
    /// Der Setter von Current gehört auf den UI-Thread: er leert den ResourceCache
    /// und ruft Invalidate auf Controls.
    /// </summary>
    public static class SkinManager
    {
        private static readonly List<WeakReference> Registrations = new List<WeakReference>();
        private static ISkin _current = new LightSkin();

        /// <summary>Wird nach jedem Skin-Wechsel ausgelöst, nachdem der Cache geleert wurde.</summary>
        public static event EventHandler SkinChanged;

        public static ISkin Current
        {
            get { return _current; }
            set
            {
                if (value == null) throw new ArgumentNullException(nameof(value));
                if (ReferenceEquals(value, _current)) return;

                _current = value;

                // Die alten Farben werden nie wieder gebraucht.
                ResourceCache.Shared.Clear();

                InvalidateAll();

                var handler = SkinChanged;
                if (handler != null) handler(null, EventArgs.Empty);
            }
        }

        public static void Register(Control control)
        {
            if (control == null) throw new ArgumentNullException(nameof(control));

            Prune();

            foreach (var reference in Registrations)
            {
                if (ReferenceEquals(reference.Target, control)) return;
            }

            Registrations.Add(new WeakReference(control));
        }

        public static void Unregister(Control control)
        {
            if (control == null) throw new ArgumentNullException(nameof(control));

            for (int i = Registrations.Count - 1; i >= 0; i--)
            {
                var target = Registrations[i].Target;
                if (target == null || ReferenceEquals(target, control))
                    Registrations.RemoveAt(i);
            }
        }

        internal static int RegisteredCount
        {
            get
            {
                Prune();
                return Registrations.Count;
            }
        }

        internal static void ResetForTests()
        {
            Registrations.Clear();
            SkinChanged = null;
            _current = new LightSkin();
            ResourceCache.Shared.Clear();
        }

        private static void InvalidateAll()
        {
            Prune();

            foreach (var reference in Registrations.ToArray())
            {
                var control = reference.Target as Control;
                if (control == null || control.IsDisposed) continue;

                control.Invalidate();
            }
        }

        private static void Prune()
        {
            for (int i = Registrations.Count - 1; i >= 0; i--)
            {
                if (!Registrations[i].IsAlive)
                    Registrations.RemoveAt(i);
            }
        }
    }
}
