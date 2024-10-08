using System.ComponentModel.DataAnnotations;
using System.Windows.Media;
using YukkuriMovieMaker.Commons;
using YukkuriMovieMaker.Controls;
using YukkuriMovieMaker.Exo;
using YukkuriMovieMaker.Player.Video;
using YukkuriMovieMaker.Plugin.Effects;

namespace YMM_BrokenJpeg
{
    [VideoEffect("Broken Jpeg", ["ÁH"], [], IsAviUtlSupported = false)]
    public class BrokenJpeg : VideoEffectBase
    {
        [Display(Name = "³ki¿", Description = "JPEGÉ³k·éÛÌi¿")]
        [AnimationSlider("F0", "", 0.0, 100.0)]
        public Animation CompressQuality { get; } = new Animation(50.0, 0.0, 100.0);

        SubSamplingType subSamplingType = SubSamplingType.YCbCr444;
        [Display(Name = "TuTvO", Description = "³kÌÛÉs¤TuTvOÌíÞ")]
        [EnumComboBox]
        public SubSamplingType SubSamplingType
        {
            get => subSamplingType;
            set => Set(ref subSamplingType, value);
        }

        Color backgroundColor = Colors.Black;
        [Display(Name = "wiF", Description = "wiF")]
        [ColorPicker]
        public Color BackgroundColor
        {
            get => backgroundColor;
            set
            {
                Set(ref backgroundColor, value);
            }
        }

        [Display(GroupName = "æÌj¹", AutoGenerateField = true)]
        public ScanPropertyGroup Scan { get; } = new ScanPropertyGroup();

        [Display(GroupName = "Êq»e[u(Px)Ìj¹", AutoGenerateField = true)]
        public QuantizeTablePropertyGroup LuminanceQuantizeTable { get; } = new QuantizeTablePropertyGroup();

        [Display(GroupName = "Êq»e[u(F·)Ìj¹", AutoGenerateField = true)]
        public QuantizeTablePropertyGroup ChrominanceQuantizeTable { get; } = new QuantizeTablePropertyGroup();

        public override string Label => "Broken Jpeg";

        public override IEnumerable<string> CreateExoVideoFilters(int keyFrameIndex, ExoOutputDescription exoOutputDescription)
        {
            return [];
        }

        public override IVideoEffectProcessor CreateVideoEffect(IGraphicsDevicesAndContext devices)
        {
            return new BrokenJpegProcessor(devices, this);
        }

        protected override IEnumerable<IAnimatable> GetAnimatables()
        {
            return [CompressQuality, Scan, LuminanceQuantizeTable, ChrominanceQuantizeTable];
        }
    }
}
