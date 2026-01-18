using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Effects;

namespace JL.Windows.GUI.CustomControls;

#pragma warning disable CA1812 // Internal class that is apparently never instantiated
internal sealed class EffectWrapper : ContentControl
{
#pragma warning restore CA1812 // Internal class that is apparently never instantiated
    public EffectMode EffectMode { get; set; } = EffectMode.DropShadow;
    public Color EffectColor { get; set; } = Colors.Black;
    public double EffectShadowDepth { get; set; } = 2;
    public double EffectBlurRadius { get; set; } = 8;
    public double EffectOpacity { get; set; } = 1.0;
    public double ShadowDirection { get; set; } = 315;

    private readonly Grid?[] _layers = new Grid?[8];
    private bool _templateInitialized;

    static EffectWrapper()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(EffectWrapper), new FrameworkPropertyMetadata(typeof(EffectWrapper)));
    }

    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        EnsureTemplateParts();
    }

    public void RebuildEffects()
    {
        EnsureTemplateParts();
        ApplyFrozenEffects();
    }

    private void EnsureTemplateParts()
    {
        if (_templateInitialized)
        {
            return;
        }

        for (int i = 0; i < 8; i++)
        {
            _layers[i] = (Grid?)GetTemplateChild($"DIRECTION_{i + 1}");
        }

        _templateInitialized = true;
    }

    private void ApplyFrozenEffects()
    {
        for (int i = 0; i < 8; i++)
        {
            Grid? layer = _layers[i];
            Debug.Assert(layer is not null);

            layer.Effect = CreateFrozenEffect(i + 1);
        }
    }

    private DropShadowEffect? CreateFrozenEffect(int layerIndex)
    {
        if (EffectMode is EffectMode.None)
        {
            return null;
        }

        double direction;
        if (EffectMode is EffectMode.DropShadow)
        {
            if (layerIndex > 1)
            {
                return null;
            }

            direction = ShadowDirection;
        }
        else // if (EffectMode is EffectMode.Outline)
        {
            direction = (layerIndex - 1) * 45;
        }

        DropShadowEffect dropShadowEffect = new()
        {
            Color = EffectColor,
            ShadowDepth = EffectShadowDepth,
            BlurRadius = EffectBlurRadius,
            Opacity = EffectOpacity,
            Direction = direction,
            RenderingBias = RenderingBias.Quality
        };

        dropShadowEffect.Freeze();
        return dropShadowEffect;
    }
}
