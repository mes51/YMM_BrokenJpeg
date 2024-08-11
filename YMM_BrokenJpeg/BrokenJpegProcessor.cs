using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Vortice.DCommon;
using Vortice.Direct2D1;
using Vortice.Direct2D1.Effects;
using Vortice.Mathematics;
using Vortice.WIC;
using YukkuriMovieMaker.Commons;
using YukkuriMovieMaker.Player.Video;

namespace YMM_BrokenJpeg
{
    class BrokenJpegProcessor : IVideoEffectProcessor
    {
        public ID2D1Image Output { get; }

        ID2D1Image? Input { get; set; }

        ID2D1Bitmap? InputBitmap { get; set; }

        ID2D1Bitmap1? TransferBitmap { get; set; }

        ID2D1Bitmap? LastBrokenImage { get; set; }

        BrokenJpeg Item { get; }

        IGraphicsDevicesAndContext Devices { get; }

        AffineTransform2D Transform { get; }

        public BrokenJpegProcessor(IGraphicsDevicesAndContext devices, BrokenJpeg item)
        {
            Devices = devices;
            Item = item;
            Transform = new AffineTransform2D(devices.DeviceContext);
            Output = Transform.Output;
        }

        public void ClearInput()
        {
            Transform.SetInput(0, null, true);
            LastBrokenImage?.Dispose();
            LastBrokenImage = null;
            TransferBitmap?.Dispose();
            TransferBitmap = null;
            InputBitmap?.Dispose();
            InputBitmap = null;
        }

        public void SetInput(ID2D1Image? input)
        {
            ClearInput();

            Input = input;
        }

        public DrawDescription Update(EffectDescription effectDescription)
        {
            if (Input == null)
            {
                return effectDescription.DrawDescription;
            }

            var rect = Devices.DeviceContext.GetImageLocalBounds(Input);
            var width = Math.Min((int)MathF.Ceiling(rect.Right - rect.Left), short.MaxValue / 2);
            var height = Math.Min((int)MathF.Ceiling(rect.Bottom - rect.Top), short.MaxValue / 2);
            if (InputBitmap == null || TransferBitmap == null || InputBitmap.Size.Width != width || InputBitmap.Size.Height != height)
            {
                InputBitmap?.Dispose();
                TransferBitmap?.Dispose();

                try
                {
                    InputBitmap = CreateBitmap(Devices.DeviceContext, width, height, BitmapOptions.Target);
                    TransferBitmap = CreateBitmap(Devices.DeviceContext, width, height, BitmapOptions.CannotDraw | BitmapOptions.CpuRead);
                }
                catch
                {
                    return effectDescription.DrawDescription;
                }
            }

            Devices.DeviceContext.Target = InputBitmap;
            Devices.DeviceContext.BeginDraw();
            Devices.DeviceContext.Clear(Item.BackgroundColor.ToColor4());
            Devices.DeviceContext.DrawImage(Input, new Vector2(-rect.Left, -rect.Top));
            Devices.DeviceContext.EndDraw();

            TransferBitmap.CopyFromBitmap(InputBitmap);

            var frame = effectDescription.ItemPosition.Frame;
            var length = effectDescription.ItemDuration.Frame;
            var fps = effectDescription.FPS;

            var quality = (float)Item.CompressQuality.GetValue(frame, length, fps) * 0.01F;
            var subSamplingType = Item.SubSamplingType;
            var scanProperty = Item.Scan.GetPropertyValue(frame, length, fps);
            var qtlProperty = Item.LuminanceQuantizeTable.GetPropertyValue(frame, length, fps);
            var qtcProperty = Item.ChrominanceQuantizeTable.GetPropertyValue(frame, length, fps);

            Transform.SetInput(0, null, true);
            LastBrokenImage?.Dispose();
            // NOTE* おそらく例外は発生しないはずだが、念のためtry-catchする
            try
            {
                LastBrokenImage = ProcessImage(Devices.DeviceContext, TransferBitmap, frame, quality, subSamplingType, scanProperty, qtlProperty, qtcProperty);
                Transform.SetInput(0, LastBrokenImage, true);
                Transform.TransformMatrix = Matrix3x2.CreateTranslation(rect.Left, rect.Top);
            }
            catch
            {
                LastBrokenImage = null;
            }

            return effectDescription.DrawDescription;
        }

        public void Dispose()
        {
            Transform.SetInput(0, null, true);
            Output.Dispose();
            Transform.Dispose();
            LastBrokenImage?.Dispose();
            TransferBitmap?.Dispose();
            InputBitmap?.Dispose();
        }

        static ID2D1Bitmap1 CreateBitmap(ID2D1DeviceContext6 deviceContext, int width, int height, BitmapOptions flag)
        {
            var bitmapProperty = new BitmapProperties1
            {
                BitmapOptions = flag,
                PixelFormat = new Vortice.DCommon.PixelFormat
                {
                    AlphaMode = AlphaMode.Premultiplied,
                    Format = Vortice.DXGI.Format.B8G8R8A8_UNorm
                }
            };

            return deviceContext.CreateBitmap(new SizeI(width, height), bitmapProperty);
        }

        static ID2D1Bitmap1? ProcessImage(ID2D1DeviceContext6 deviceContext, ID2D1Bitmap1 image, int time, float quality, SubSamplingType subSamplingType, ScanPropertyValue scanProperty, QuantizeTablePropertyValue qtlProperty, QuantizeTablePropertyValue qtcProperty)
        {
            using var ms = new MemoryStream();

            var width = image.PixelSize.Width;
            var height = image.PixelSize.Height;
            var map = image.Map(MapOptions.Read);
            using var wicFactory = new IWICImagingFactory2();
            using (var stream = wicFactory.CreateStream(ms))
            using (var encoder = wicFactory.CreateEncoder(ContainerFormat.Jpeg))
            using (var wicBitmap = wicFactory.CreateBitmapFromMemory(width, height, Vortice.WIC.PixelFormat.Format32bppBGR, map.Pitch, map.Pitch * height, map.Bits))
            {
                encoder.Initialize(stream);
                using var frame = encoder.CreateNewFrame(out var props);
                props.Set("ImageQuality", quality);
                switch (subSamplingType)
                {
                    case SubSamplingType.YCbCr444:
                        props.Set("JpegYCrCbSubsampling", 3);
                        break;
                    case SubSamplingType.YCbCr422:
                        props.Set("JpegYCrCbSubsampling", 2);
                        break;
                    default:
                        props.Set("JpegYCrCbSubsampling", 1);
                        break;
                }
                frame.Initialize(props);
                frame.WriteSource(wicBitmap);

                frame.Commit();
                encoder.Commit();
                stream.Commit(SharpGen.Runtime.Win32.CommitFlags.Default);
            }
            image.Unmap();

            ms.Position = 0;
            BreakJpegScan(ms, time, scanProperty);
            if (qtlProperty.Enabled)
            {
                BreakQuantizeTable(ms, time, qtlProperty, 0);
            }
            if (qtcProperty.Enabled)
            {
                BreakQuantizeTable(ms, time, qtcProperty, 1);
            }

            using var jpegStream = wicFactory.CreateStream(ms);
            using var decoder = wicFactory.CreateDecoderFromStream(ms);
            using var decodedFrame = decoder.GetFrame(0);
            using var converter = wicFactory.CreateFormatConverter();
            converter.Initialize(decodedFrame, Vortice.WIC.PixelFormat.Format32bppPBGRA, BitmapDitherType.None, null, 0, BitmapPaletteType.Custom);

            return deviceContext.CreateBitmapFromWicBitmap(converter);
        }

        static void BreakJpegScan(MemoryStream ms, int time, ScanPropertyValue scanProperty)
        {
            var (brokenCount, brokenRangeBegin, brokenRangeEnd, seed) = scanProperty;
            if (brokenCount < 1)
            {
                return;
            }
            if (brokenRangeBegin > brokenRangeEnd)
            {
                (brokenRangeBegin, brokenRangeEnd) = (brokenRangeEnd, brokenRangeBegin);
            }

            var imageStarted = false;
            while (!imageStarted)
            {
                var marker = ms.ReadUInt16BE();
                switch (marker)
                {
                    case 0xFFD8: // SOI
                        break;
                    case 0xFFE0: // APP0
                    case 0xFFDB: // DQT
                    case 0xFFC0: // SOF
                    case 0xFFC4: // DHT
                        ms.Position += ms.ReadUInt16BE();
                        break;
                    case 0xFFDA: // SOS
                        ms.Position += ms.ReadUInt16BE();
                        imageStarted = true;
                        break;
                }
            }

            var imageRange = ms.Length - ms.Position - 2; // EOI
            var brokenBeginPos = ms.Position + (long)(imageRange * brokenRangeBegin);
            var brokenEndPos = ms.Position + (long)(imageRange * brokenRangeEnd);
            if (brokenBeginPos == brokenEndPos)
            {
                ms.Position = 0;
                return;
            }

            brokenCount = (int)Math.Min(brokenCount, brokenEndPos - brokenBeginPos);
            var rangePerBreak = (int)((brokenEndPos - brokenBeginPos) / brokenCount);
            for (var i = 0; i < brokenCount; i++)
            {
                var (pos, data, _) = Pcg3D((uint)i, (uint)time, 0, seed);
                ms.Position = brokenBeginPos + rangePerBreak * i + (int)((pos / (double)uint.MaxValue) * rangePerBreak);
                ms.WriteByte((byte)data);
            }

            ms.Position = 0;
        }

        static void BreakQuantizeTable(MemoryStream ms, int time, QuantizeTablePropertyValue qtProperty, int destination)
        {
            var (_, brokenPosition, replaceValue, brokenCount, maxValue, seed) = qtProperty;

            var dqtStarted = false;
            while (!dqtStarted)
            {
                var marker = ms.ReadUInt16BE();
                switch (marker)
                {
                    case 0xFFD8: // SOI
                        break;
                    case 0xFFDB: // DQT
                        if (ms.ReadUInt16BE() != 67)
                        {
                            // NOTE: 量子化テーブルの定義自体が破損している
                            ms.Position = 0;
                            return;
                        }
                        dqtStarted = ms.ReadByte() == destination;
                        break;
                    case 0xFFE0: // APP0
                    case 0xFFC0: // SOF
                    case 0xFFC4: // DHT
                        ms.Position += ms.ReadUInt16BE();
                        break;
                    case 0xFFDA: // SOS
                                 // NOTE: これ以降画像データが始まるので何もしない
                        ms.Position = 0;
                        return;
                }
            }

            var dcElementPosition = ms.Position;
            ms.Position += brokenPosition;
            ms.WriteByte(replaceValue);

            for (var i = 0; i < brokenCount; i++)
            {
                var (pos, data, _) = Pcg3D((uint)i, (uint)time, (uint)(1 + destination), seed);
                ms.Position = dcElementPosition + (int)((pos / (double)uint.MaxValue) * 63.0);

                ms.WriteByte((byte)((data / (double)uint.MaxValue) * maxValue));
            }

            ms.Position = 0;
        }

        // from https://jcgt.org/published/0009/03/02/paper.pdf
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static (uint, uint, uint) Pcg3D(uint x, uint y, uint z, uint seed)
        {
            const uint Multiplyer = 1664525U;

            const uint Increment = 1013904223U;

            var vx = (x + seed) * Multiplyer + Increment;
            var vy = (y + seed) * Multiplyer + Increment;
            var vz = (z + seed) * Multiplyer + Increment;

            vx += vy * vz;
            vy += vz * vx;
            vz += vx * vy;
            vx ^= vx >> 16;
            vy ^= vy >> 16;
            vz ^= vz >> 16;
            vx += vy * vz;
            vy += vz * vx;
            vz += vx * vy;

            return (vx, vy, vz);
        }
    }

    file static class MemoryStreamExtension
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ushort ReadUInt16BE(this MemoryStream ms)
        {
            Span<byte> data = stackalloc byte[sizeof(ushort)];
            ms.Read(data);
            return BinaryPrimitives.ReadUInt16BigEndian(data);
        }
    }
}
