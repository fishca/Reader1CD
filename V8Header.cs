using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Read1CD
{

    public class Page0
    {
        private Char[] sig; // сигнатура SIG_CON // сигнатура “1CDBMSV8”

        private byte ver1;
        private byte ver2;
        private byte ver3;
        private byte ver4;

        private UInt32 length;
        private UInt32 firstblock;
        private UInt32 pagesize;

        public Char[] Sig
        {
            get
            {
                return sig;
            }
        }

        public byte Ver1
        {
            get
            {
                return ver1;
            }
        }

        public byte Ver2
        {
            get
            {
                return ver2;
            }
        }

        public byte Ver3
        {
            get
            {
                return ver3;
            }
        }

        public byte Ver4
        {
            get
            {
                return ver4;
            }
        }

        public UInt32 Length
        {
            get
            {
                return length;
            }
        }

        public UInt32 FirstBlock
        {
            get
            {
                return firstblock;
            }
        }

        public UInt32 PageSize
        {
            get
            {
                return pagesize;
            }
        }

        public Page0()
        {

        }

    }

    public class Page1
    {
        public Page1()
        {
        }
    }

    public class Page2
    {
        public Page2()
        {
        }
    }


    /// <summary>
    /// Класс который содержит 3 первых страницы базы 1CD
    /// </summary>
    public class V8Header
    {
    }
}
