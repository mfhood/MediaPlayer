using MaterialDesignThemes.Wpf;
using MaterialDesignThemes.Wpf.Converters;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Windows.Data;

namespace MediaPlayer
{
    public class BooleanToVolumePackIconConverter : BooleanConverter<PackIconKind>
    {
        public BooleanToVolumePackIconConverter() : base(PackIconKind.VolumeHigh, PackIconKind.VolumeOff)
        {
        }
    }
}
