﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CW3_grafika
{
    public class PgmImage : ImageBase
    {
        public int MaxValue { get; set; }
        public byte[,] Pixels { get; set; }
    }
}
