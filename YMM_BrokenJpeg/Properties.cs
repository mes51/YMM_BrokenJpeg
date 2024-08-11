using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YukkuriMovieMaker.Commons;
using YukkuriMovieMaker.Controls;

namespace YMM_BrokenJpeg
{
    public class ScanPropertyGroup : Animatable
    {
        [Display(Name = "破損箇所数", Description = "データを壊す箇所の数")]
        [AnimationSlider("F0", "", 0.0, 1000.0)]
        public Animation BrokenCount { get; } = new Animation(10.0, 0.0, short.MaxValue);

        [Display(Name = "破損範囲開始", Description = "データを壊す範囲の開始位置")]
        [AnimationSlider("F0", "%", 0.0, 100.0)]
        public Animation BrokenRangeBegin { get; } = new Animation(0.0, 0.0, 100.0);

        [Display(Name = "破損範囲終了", Description = "データを壊す範囲の終了位置")]
        [AnimationSlider("F0", "%", 0.0, 100.0)]
        public Animation BrokenRangeEnd { get; } = new Animation(100.0, 0.0, 100.0);

        [Display(Name = "ランダムシード", Description = "ランダムシード")]
        [AnimationSlider("F0", "", 0.0, 1000.0)]
        public Animation RandomSeed { get; } = new Animation(438.0, 0.0, int.MaxValue);

        protected override IEnumerable<IAnimatable> GetAnimatables()
        {
            return [BrokenCount, BrokenRangeBegin, BrokenRangeEnd, RandomSeed];
        }

        internal ScanPropertyValue GetPropertyValue(long frame, long totalFrame, int fps)
        {
            return new ScanPropertyValue(
                (int)BrokenCount.GetValue(frame, totalFrame, fps),
                BrokenRangeBegin.GetValue(frame, totalFrame, fps) * 0.01,
                BrokenRangeEnd.GetValue(frame, totalFrame, fps) * 0.01,
                (uint)RandomSeed.GetValue(frame, totalFrame, fps)
            );
        }
    }

    public class QuantizeTablePropertyGroup : Animatable
    {
        bool enabled = false;
        [Display(Name = "有効/無効", Description = "量子化テーブルの破損を行うかどうか")]
        [ToggleSlider]
        public bool Enabled
        {
            get => enabled;
            set => Set(ref enabled, value);
        }

        [Display(Name = "破損箇所", Description = "壊すテーブルの位置")]
        [AnimationSlider("F0", "", 0.0, 64.0)]
        public Animation BrokenPosition { get; } = new Animation(3.0, 1.0, 64.0);

        [Display(Name = "値", Description = "量子化テーブルの値")]
        [AnimationSlider("F0", "", 0.0, 255.0)]
        public Animation ReplaceValue { get; } = new Animation(100.0, 0.0, 255.0);

        [Display(Name = "破損箇所数", Description = "テーブルを壊す箇所の数")]
        [AnimationSlider("F0", "", 0.0, 63.0)]
        public Animation BrokenCount { get; } = new Animation(0.0, 0.0, 63);

        [Display(Name = "最大値", Description = "テーブルの値を壊す際の最大値")]
        [AnimationSlider("F0", "", 0.0, 255.0)]
        public Animation MaxValue { get; } = new Animation(0.0, 0.0, 255.0);

        [Display(Name = "ランダムシード", Description = "ランダムシード")]
        [AnimationSlider("F0", "", 0.0, 1000.0)]
        public Animation RandomSeed { get; } = new Animation(33.0, 0.0, int.MaxValue);

        protected override IEnumerable<IAnimatable> GetAnimatables()
        {
            return [BrokenPosition, ReplaceValue, BrokenCount, MaxValue, RandomSeed];
        }

        internal QuantizeTablePropertyValue GetPropertyValue(long frame, long totalFrame, int fps)
        {
            return new QuantizeTablePropertyValue(
                Enabled,
                (int)BrokenPosition.GetValue(frame, totalFrame, fps) - 1,
                (byte)ReplaceValue.GetValue(frame, totalFrame, fps),
                (int)BrokenCount.GetValue(frame, totalFrame, fps),
                (int)MaxValue.GetValue(frame, totalFrame, fps),
                (uint)RandomSeed.GetValue(frame, totalFrame, fps)
            );
        }
    }
}
