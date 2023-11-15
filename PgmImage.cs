using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CW3_grafika
{
    public class PgmImage
    {
        public int Width { get; set; }
        public int Height { get; set; }
        public byte MaxGrayValue { get; set; }
        public byte[,] Pixels { get; set; }
    }

}
