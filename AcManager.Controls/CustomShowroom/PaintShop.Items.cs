using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Media;
using AcTools.Render.Kn5SpecificForward;
using AcTools.Utils;
using FirstFloor.ModernUI.Helpers;
using FirstFloor.ModernUI.Presentation;
using JetBrains.Annotations;

namespace AcManager.Controls.CustomShowroom {
    public static partial class PaintShop {
        public abstract class PaintableItem : Displayable, IDisposable {
            protected PaintableItem(bool enabledByDefault) {
                _enabled = enabledByDefault;
            }
            
            [CanBeNull]
            private IPaintShopRenderer _renderer;

            public void SetRenderer(IPaintShopRenderer renderer) {
                _renderer = renderer;
            }

            private bool _enabled;

            public bool Enabled {
                get { return _enabled; }
                set {
                    if (Equals(value, _enabled)) return;
                    _enabled = value;
                    OnEnabledChanged();
                    OnPropertyChanged();
                }
            }

            private bool _guessed;

            public bool Guessed {
                get { return _guessed; }
                internal set {
                    if (Equals(value, _guessed)) return;
                    _guessed = value;
                    OnPropertyChanged();
                }
            }

            protected virtual void OnEnabledChanged() {}

            private bool _updating;

            protected async void Update() {
                if (_updating) return;

                try {
                    _updating = true;
                    await Task.Delay(20);
                    if (_updating && !_disposed) {
                        UpdateOverride();
                    }
                } finally {
                    _updating = false;
                }
            }

            protected override void OnPropertyChanged(string propertyName = null) {
                base.OnPropertyChanged(propertyName);
                Update();
            }

            protected virtual bool IsActive() {
                return Enabled;
            }

            protected void UpdateOverride() {
                var renderer = _renderer;
                if (renderer == null) return;

                if (IsActive()) {
                    ApplyOverride(renderer);
                } else {
                    ResetOverride(renderer);
                }
            }

            protected abstract void ApplyOverride([NotNull] IPaintShopRenderer renderer);

            protected abstract void ResetOverride([NotNull] IPaintShopRenderer renderer);

            [NotNull]
            protected abstract Task SaveOverrideAsync([NotNull] IPaintShopRenderer renderer, string location);

            [NotNull]
            public Task SaveAsync(string location) {
                var renderer = _renderer;
                if (renderer == null) return Task.Delay(0);

                return IsActive() ? SaveOverrideAsync(renderer, location) : Task.Delay(0);
            }

            private bool _disposed;

            public virtual void Dispose() {
                if (_renderer != null) {
                    ResetOverride(_renderer);
                }

                _disposed = true;
            }
        }

        public class SolidColorIfFlagged : PaintableItem {
            public SolidColorIfFlagged(string diffuseTexture, bool inverse, Color color, double opacity = 1d) : base(false) {
                _diffuseTexture = diffuseTexture;
                _inverse = inverse;
                _color = color;
                _opacity = opacity;
            }

            private readonly string _diffuseTexture;
            private readonly bool _inverse;
            private readonly Color _color;
            private readonly double _opacity;

            public override string DisplayName { get; set; } = "Colored If Enabled";

            protected override bool IsActive() {
                return Enabled ^ _inverse;
            }

            protected override void ApplyOverride(IPaintShopRenderer renderer) {
                renderer.OverrideTexture(_diffuseTexture, _color.ToColor(), _opacity);
            }

            protected override void ResetOverride(IPaintShopRenderer renderer) {
                renderer.OverrideTexture(_diffuseTexture, null);
            }

            protected override Task SaveOverrideAsync(IPaintShopRenderer renderer, string location) {
                return renderer.SaveTextureAsync(Path.Combine(location, _diffuseTexture), _color.ToColor(), _opacity);
            }
        }

        public class TransparentIfFlagged : SolidColorIfFlagged {
            public TransparentIfFlagged(string diffuseTexture, bool inverse) : base(diffuseTexture, inverse, Colors.Black, 0d) { }

            public override string DisplayName { get; set; } = "Transparent If Enabled";
        }

        public class ReplacedIfFlagged : PaintableItem {
            public ReplacedIfFlagged(bool inverse, Dictionary<string, byte[]> replacements) : base(false) {
                _inverse = inverse;
                _replacements = replacements;
            }

            public override string DisplayName { get; set; } = "Replaced If Enabled";
            
            private readonly bool _inverse;
            private readonly Dictionary<string, byte[]> _replacements;

            protected override bool IsActive() {
                return Enabled ^ _inverse;
            }

            protected override void ApplyOverride(IPaintShopRenderer renderer) {
                foreach (var replacement in _replacements) {
                    renderer.OverrideTexture(replacement.Key, replacement.Value);
                }
            }

            protected override void ResetOverride(IPaintShopRenderer renderer) {
                foreach (var replacement in _replacements) {
                    renderer.OverrideTexture(replacement.Key, null);
                }
            }

            protected override async Task SaveOverrideAsync(IPaintShopRenderer renderer, string location) {
                foreach (var replacement in _replacements) {
                    await FileUtils.WriteAllBytesAsync(Path.Combine(location, replacement.Key), replacement.Value);
                }
            }
        }

        public class ColoredItem : PaintableItem {
            public ColoredItem([Localizable(false)] string diffuseTexture, Color defaultColor) : base(false) {
                DiffuseTexture = diffuseTexture;
                DefaultColor = defaultColor;
            }

            public string DiffuseTexture { get; }

            public Color DefaultColor { get; }

            public override string DisplayName { get; set; } = "Colored item";

            private Color? _color;

            public Color Color {
                get { return _color ?? DefaultColor; }
                set {
                    if (Equals(value, _color)) return;
                    _color = value;
                    OnPropertyChanged();
                }
            }

            protected override void ApplyOverride(IPaintShopRenderer renderer) {
                renderer.OverrideTexture(DiffuseTexture, Color.ToColor());
            }

            protected override void ResetOverride(IPaintShopRenderer renderer) {
                renderer.OverrideTexture(DiffuseTexture, null);
            }

            protected override Task SaveOverrideAsync(IPaintShopRenderer renderer, string location) {
                return renderer.SaveTextureAsync(Path.Combine(location, DiffuseTexture), Color.ToColor());
            }
        }

        public class TintedWindows : ColoredItem {
            private readonly bool _tintBase;

            public double DefaultAlpha { get; set; }

            public TintedWindows([Localizable(false)] string diffuseTexture, double defaultAlpha = 0.23, Color? defaultColor = null, bool tintBase = false)
                    : base(diffuseTexture, defaultColor ?? Color.FromRgb(41, 52, 55)) {
                _tintBase = tintBase;
                DefaultAlpha = defaultAlpha;
            }

            public override string DisplayName { get; set; } = "Windows";

            private double? _alpha;

            public double Alpha {
                get { return _alpha ?? DefaultAlpha; }
                set {
                    if (Equals(value, _alpha)) return;
                    _alpha = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(Transparency));
                }
            }

            public double Transparency {
                get { return 1d - Alpha; }
                set { Alpha = 1d - value; }
            }

            protected override void ApplyOverride(IPaintShopRenderer renderer) {
                if (_tintBase) {
                    renderer.OverrideTextureTint(DiffuseTexture, Color.ToColor(), Alpha, null);
                } else {
                    renderer.OverrideTexture(DiffuseTexture, Color.ToColor(), Alpha);
                }
            }

            protected override Task SaveOverrideAsync(IPaintShopRenderer renderer, string location) {
                return _tintBase ? renderer.SaveTextureTintAsync(Path.Combine(location, DiffuseTexture), Color.ToColor(), Alpha, DiffuseTexture) :
                        renderer.SaveTextureAsync(Path.Combine(location, DiffuseTexture), Color.ToColor(), Alpha);
            }
        }

        public class CarPaint : ColoredItem {
            public CarPaint(string detailsTexture, Color? defaultColor = null)
                    : base(detailsTexture, defaultColor ?? Color.FromRgb(255, 255, 255)) {
                Enabled = true;
            }

            public bool SupportsFlakes { get; internal set; } = true;

            public override string DisplayName { get; set; } = "Car paint";

            private bool _flakes = true;

            public bool Flakes {
                get { return _flakes; }
                set {
                    if (Equals(value, _flakes)) return;
                    _flakes = value;
                    OnPropertyChanged();
                }
            }

            private Color? _previousColor;
            private bool _previousFlakes;

            protected override void OnEnabledChanged() {
                _previousColor = null;
            }

            protected override void ApplyOverride(IPaintShopRenderer renderer) {
                if (_previousColor == Color && _previousFlakes == Flakes) return;
                _previousColor = Color;
                _previousFlakes = Flakes;

                if (SupportsFlakes && Flakes) {
                    renderer.OverrideTextureFlakes(DiffuseTexture, Color.ToColor());
                } else {
                    renderer.OverrideTexture(DiffuseTexture, Color.ToColor());
                }
            }

            protected override Task SaveOverrideAsync(IPaintShopRenderer renderer, string location) {
                return SupportsFlakes && Flakes ?
                        renderer.SaveTextureFlakesAsync(Path.Combine(location, DiffuseTexture), Color.ToColor()) :
                        renderer.SaveTextureAsync(Path.Combine(location, DiffuseTexture), Color.ToColor());
            }
        }

        public class ComplexCarPaint : CarPaint {
            public string MapsTexture { get; }

            [CanBeNull, Localizable(false)]
            public string MapsDefaultTexture { get; internal set; }

            public bool AutoAdjustLevels { get; internal set; }

            public ComplexCarPaint([Localizable(false)] string detailsTexture, [Localizable(false)] string mapsTexture, Color? defaultColor = null)
                    : base(detailsTexture, defaultColor) {
                MapsTexture = mapsTexture;
            }

            private double _reflection = 1d;
            private double _previousReflection = -1d;

            public double Reflection {
                get { return _reflection; }
                set {
                    if (Equals(value, _reflection)) return;
                    _reflection = value;
                    OnPropertyChanged();
                }
            }

            private double _gloss = 1d;
            private double _previousGloss = -1d;

            public double Gloss {
                get { return _gloss; }
                set {
                    if (Equals(value, _gloss)) return;
                    _gloss = value;
                    OnPropertyChanged();
                }
            }

            private double _specular = 1d;
            private double _previousSpecular = -1d;

            public double Specular {
                get { return _specular; }
                set {
                    if (Equals(value, _specular)) return;
                    _specular = value;
                    OnPropertyChanged();
                }
            }

            protected override void OnEnabledChanged() {
                base.OnEnabledChanged();
                _previousGloss = _previousReflection = _previousSpecular = -1d;
            }

            protected override void ApplyOverride(IPaintShopRenderer renderer) {
                base.ApplyOverride(renderer);
                if (Math.Abs(_previousReflection - Reflection) > 0.001 || Math.Abs(_previousGloss - Gloss) > 0.001 ||
                        Math.Abs(_previousSpecular - Specular) > 0.001) {
                    renderer.OverrideTextureMaps(MapsTexture, Reflection, Gloss, Specular, AutoAdjustLevels, MapsDefaultTexture);
                    _previousReflection = Reflection;
                    _previousGloss = Gloss;
                    _previousSpecular = Specular;
                }
            }

            protected override void ResetOverride(IPaintShopRenderer renderer) {
                base.ResetOverride(renderer);
                renderer.OverrideTexture(MapsTexture, null);
            }

            protected override async Task SaveOverrideAsync(IPaintShopRenderer renderer, string location) {
                await base.SaveOverrideAsync(renderer, location);
                await renderer.SaveTextureMaps(Path.Combine(location, MapsTexture), Reflection, Gloss, Specular, AutoAdjustLevels,
                    MapsDefaultTexture ?? MapsTexture);
            }
        }
    }
}