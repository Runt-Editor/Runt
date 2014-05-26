using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Runt
{
    public static partial class Icons
    {
        private static ImageSource GetImage(string name)
        {
            var decoder = BitmapDecoder.Create(new Uri("pack://application:,,,/Resources/Icons/" + name),
                BitmapCreateOptions.DelayCreation | BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnDemand);

            //var triples = decoder.Frames
            //    .GroupBy(f => new Tuple<int, int>(f.PixelWidth, f.PixelHeight))
            //    .Where(g => g.Count() == 3)
            //    .ToDictionary(k => k.Key, v => v.ToArray());


            return decoder.Frames[0];
        }
    }
}
