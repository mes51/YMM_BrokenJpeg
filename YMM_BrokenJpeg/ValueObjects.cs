using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace YMM_BrokenJpeg
{
    record ScanPropertyValue(int BrokenCount, double BrokenRangeBegin, double BrokenRangeEnd, uint RandomSeed);

    record QuantizeTablePropertyValue(bool Enabled, int BrokenPosition, byte ReplaceValue, int BrokenCount, int MaxValue, uint RandomSeed);
}
