using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace YMM_BrokenJpeg
{
    public enum SubSamplingType
    {
        [Display(Name = "YCbCr 4:4:4")]
        YCbCr444,
        [Display(Name = "YCbCr 4:2:2")]
        YCbCr422,
        [Display(Name = "YCbCr 4:2:0")]
        YCbCr420,
    }
}
