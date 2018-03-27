using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using static Read1CD.Constant;

namespace Read1CD
{
    class Program
    {
        static void Main(string[] args)
        {

            String FileName = args[0];

            int CountPages = 0;

            v8Base1CD Data1c = new v8Base1CD(FileName);

            var res = Data1c.Page0.sig.SequenceEqual(SIG_CON);

            if (res)
                Console.WriteLine($"Файл {FileName} является базой данных 1С");

            //String verDB = Data1c.Page0.getver();
            String verDB = Data1c.Page0.Version;

            Console.WriteLine($"В базе {FileName}, версия базы {verDB} и количество таблиц в файле: {Data1c.num_tables}");
        }


    }
}

