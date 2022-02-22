using System.Text;

namespace digit_console;

public class Display
{
    public static void GetImagesAsString(StringBuilder res, List<int> image1, List<int> image2)
    {
        var first = new ImageLineStringer(image1);
        var second = new ImageLineStringer(image2);

        var hasMore = true;
        while (hasMore)
        {
            hasMore &= first.WriteNextLine(res);
            res.Append(" | ");
            hasMore &= second.WriteNextLine(res);
            res.Append("\n");
        }
    }

    private struct ImageLineStringer
    {
        private int _pixel;
        private List<int> _image;
        public ImageLineStringer(List<int> image)
        {
            _pixel = 0;
            _image = image;
        }

        public bool WriteNextLine(StringBuilder res)
        {
            while (_pixel < _image.Count)
            {
                var output_char = GetDisplayCharForPixel(_image[_pixel]);
                res.Append(output_char);
                res.Append(output_char);
                ++_pixel;

                if (_pixel % 28 == 0)
                {
                    return true;
                }
            }

            return false;
        }
    }

    private static char GetDisplayCharForPixel(int pixel)
    {
        return pixel switch
        {
            var low when low > 16 && low < 32 => '.',
            var mid when mid >= 32 && mid < 64 => ':',
            var high when high >= 64 && high < 160 => 'o',
            var reallyHigh when reallyHigh >= 160 && reallyHigh < 224 => 'O',
            var reallyReallyHigh when reallyReallyHigh >= 224 => '@',
            _ => ' ',
        };
    }
}
