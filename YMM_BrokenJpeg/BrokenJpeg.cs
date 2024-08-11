using System.ComponentModel.DataAnnotations;
using System.Windows.Media;
using YukkuriMovieMaker.Commons;
using YukkuriMovieMaker.Controls;
using YukkuriMovieMaker.Exo;
using YukkuriMovieMaker.Player.Video;
using YukkuriMovieMaker.Plugin.Effects;

namespace YMM_BrokenJpeg
{
    [VideoEffect("Broken Jpeg", ["‰ÁH"], [], IsAviUtlSupported = false)]
    public class BrokenJpeg : VideoEffectBase
    {
        [Display(Name = "ˆ³k•iŽ¿", Description = "JPEG‚Éˆ³k‚·‚éÛ‚Ì•iŽ¿")]
        [AnimationSlider("F0", "", 0.0, 100.0)]
        public Animation CompressQuality { get; } = new Animation(50.0, 0.0, 100.0);

        SubSamplingType subSamplingType = SubSamplingType.YCbCr444;
        [Display(Name = "ƒTƒuƒTƒ“ƒvƒŠƒ“ƒO", Description = "ˆ³k‚ÌÛ‚És‚¤ƒTƒuƒTƒ“ƒvƒŠƒ“ƒO‚ÌŽí—Þ")]
        [EnumComboBox]
        public SubSamplingType SubSamplingType
        {
            get => subSamplingType;
            set => Set(ref subSamplingType, value);
        }

        Color backgroundColor = Colors.Black;
        [Display(Name = "”wŒiF", Description = "”wŒiF")]
        [ColorPicker]
        public Color BackgroundColor
        {
            get => backgroundColor;
            set
            {
                Set(ref backgroundColor, value);
            }
        }

        [Display(GroupName = "‰æ‘œ‚Ì”j‘¹", AutoGenerateField = true)]
        public ScanPropertyGroup Scan { get; } = new ScanPropertyGroup();

        [Display(GroupName = "—ÊŽq‰»ƒe[ƒuƒ‹(‹P“x)‚Ì”j‘¹", AutoGenerateField = true)]
        public QuantizeTablePropertyGroup LuminanceQuantizeTable { get; } = new QuantizeTablePropertyGroup();

        [Display(GroupName = "—ÊŽq‰»ƒe[ƒuƒ‹(F·)‚Ì”j‘¹", AutoGenerateField = true)]
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
