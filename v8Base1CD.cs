using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using static Read1CD.Constant;
using static Read1CD.Structures;

namespace Read1CD
{
    public class v8Base1CD
    {

        // https://infostart.ru/public/187832/


        public v8Base1CD(String fname)
        {
            //FileStream f1CD = new FileStream(fname, FileMode.Open);

            byte[] b;
            root_81 root81;
            root81.blocks = new UInt32[1];
            root81.numblocks = 0;

            //MemoryStream tstr;

            Int32 i, j;

            UInt32[] table_blocks;

            data1CD = new FileStream(fname, FileMode.Open);

            ReadPage0();

            //String verDB = Page0.getver();
            String verDB = Page0.Version;

            if (verDB == "8.2.14.0")
            {
                version = db_ver.ver8_2_14_0;
            }
            else if (verDB == "8.3.8.0")
            {
                version = db_ver.ver8_3_8_0;
                pagesize = Page0.pagesize;
            }

            Int64 lf = data1CD.Length;
            length = (UInt32)lf / pagesize;
            if (length * pagesize != data1CD.Length)
            {
                Console.WriteLine($"Длина файла базы не кратна длине страницы ({pagesize}). Длина файла: {lf}");
            }

            //v8MemBlock MB = new v8MemBlock((FileStream)data1CD, length, false, true);
            v8MemBlock.pagesize = pagesize;

            v8MemBlock.maxcount = ONE_GB / pagesize;

            v8MemBlock.create_memblocks(length);

            if (length != Page0.length)
            {
                Console.WriteLine($"Длина файла в блоках и количество блоков в заголовке не равны. Длина файла в блоках: {length}. Блоков в заголовке {Page0.length}");
            }

            free_blocks = new V8Object(this, 1);    // таблица свободных блоков
            root_object = new V8Object(this, 2);    // корневой объект

            if (version == db_ver.ver8_0_3_0 || version == db_ver.ver8_0_5_0)
            {
            }
            else
            {
                if (version >= db_ver.ver8_3_8_0)
                {
                    MemoryStream tstr = new MemoryStream();

                    root_object.readBlob(tstr, 1);
                    b = new byte[tstr.Length];
                    Array.Copy(tstr.ToArray(), 0, b, 0, tstr.Length);

                    root81 = ByteArrayToRoot81(b);

                }
                else
                {
                    root81 = ByteArrayToRoot81(root_object.getdata());
                }
            }

            num_tables = (Int32)root81.numblocks;

            table_blocks = new UInt32[root81.numblocks];
            Array.Copy(root81.blocks, table_blocks, root81.blocks.Length);
            //table_blocks = root81.blocks;

            tables = new v8Table[num_tables];
            for (i = 0, j = 0; i < num_tables; i++)
            {
                if (version < db_ver.ver8_3_8_0)
                {
                    tables[j] = new v8Table(this, (Int32)table_blocks[i]);
                }
                else
                {
                    MemoryStream tstr = new MemoryStream();
                    root_object.readBlob(tstr, table_blocks[i]);
                    //tables[j] = new v8Table(this, tstr.ToString(), (Int32)table_blocks[i]);

                    String ttt = Encoding.UTF8.GetString(tstr.ToArray());

                    tables[j] = new v8Table(this, ttt, (Int32)table_blocks[i]);
                }

                if (tables[j].bad)
                {
                    tables[j] = null;
                    continue;
                }

                if (tables[j].getname().CompareTo("CONFIG")          == 0) table_config          = tables[j];
                if (tables[j].getname().CompareTo("CONFIGSAVE")      == 0) table_configsave      = tables[j];
                if (tables[j].getname().CompareTo("PARAMS")          == 0) table_params          = tables[j];
                if (tables[j].getname().CompareTo("FILES")           == 0) table_files           = tables[j];
                if (tables[j].getname().CompareTo("DBSCHEMA")        == 0) table_dbschema        = tables[j];
                if (tables[j].getname().CompareTo("CONFIGCAS")       == 0) table_configcas       = tables[j];
                if (tables[j].getname().CompareTo("CONFIGCASSAVE")   == 0) table_configcassave   = tables[j];
                if (tables[j].getname().CompareTo("_EXTENSIONSINFO") == 0) table__extensionsinfo = tables[j];
                if (tables[j].getname().CompareTo("DEPOT")           == 0) table_depot           = tables[j];
                if (tables[j].getname().CompareTo("USERS")           == 0) table_users           = tables[j];
                if (tables[j].getname().CompareTo("OBJECTS")         == 0) table_objects         = tables[j];
                if (tables[j].getname().CompareTo("VERSIONS")        == 0) table_versions        = tables[j];
                if (tables[j].getname().CompareTo("LABELS")          == 0) table_labels          = tables[j];
                if (tables[j].getname().CompareTo("HISTORY")         == 0) table_history         = tables[j];
                if (tables[j].getname().CompareTo("LASTESTVERSIONS") == 0) table_lastestversions = tables[j];
                if (tables[j].getname().CompareTo("EXTERNALS")       == 0) table_externals       = tables[j];
                if (tables[j].getname().CompareTo("SELFREFS")        == 0) table_selfrefs        = tables[j];
                if (tables[j].getname().CompareTo("OUTREFS")         == 0) table_outrefs         = tables[j];

                j++;

            }
            num_tables = j;

            if (table_config == null && table_configsave == null && table_params == null && table_files == null && table_dbschema == null)
            {
                if (table_depot == null && table_users == null && table_objects == null && table_versions == null && table_labels == null && table_history == null && table_lastestversions == null && table_externals == null && table_selfrefs == null && table_outrefs == null)
                {
                    Console.WriteLine("База не является информационной базой 1С");
                }
                else
                {
                    is_depot = true;   // это хранилище

                    if (table_depot           == null) Console.WriteLine("Отсутствует таблица DEPOT");
                    if (table_users           == null) Console.WriteLine("Отсутствует таблица USERS");
                    if (table_objects         == null) Console.WriteLine("Отсутствует таблица OBJECTS");
                    if (table_versions        == null) Console.WriteLine("Отсутствует таблица VERSIONS");
                    if (table_labels          == null) Console.WriteLine("Отсутствует таблица LABELS");
                    if (table_history         == null) Console.WriteLine("Отсутствует таблица HISTORY");
                    if (table_lastestversions == null) Console.WriteLine("Отсутствует таблица LASTESTVERSIONS");
                    if (table_externals       == null) Console.WriteLine("Отсутствует таблица EXTERNALS");
                    if (table_selfrefs        == null) Console.WriteLine("Отсутствует таблица SELFREFS");
                    if (table_outrefs         == null) Console.WriteLine("Отсутствует таблица OUTREFS");

                    FieldType.showGUIDasMS = true; // TODO: wat??
                }
            }
            else
            {
                is_infobase = true;    // это информационная база

                if (table_config     == null) Console.WriteLine("Отсутствует таблица CONFIG");
                if (table_configsave == null) Console.WriteLine("Отсутствует таблица CONFIGSAVE");
                if (table_params     == null) Console.WriteLine("Отсутствует таблица PARAMS");
                if (table_files      == null) Console.WriteLine("Отсутствует таблица FILES");
                if (table_dbschema   == null) Console.WriteLine("Отсутствует таблица DBSCHEMA");
            }

        }



        public void ReadPage0()
        {
            
            BinaryReader br = new BinaryReader(data1CD);
            byte[] buf = new byte[100];

            buf = br.ReadBytes(8);

            Page0.sig = Encoding.UTF8.GetChars(buf, 0, 8);

            Page0.ver1 = br.ReadByte();
            Page0.ver2 = br.ReadByte();
            Page0.ver3 = br.ReadByte();
            Page0.ver4 = br.ReadByte();

            Page0.length = br.ReadUInt32();
            Page0.firstblock = br.ReadUInt32();
            Page0.pagesize = br.ReadUInt32();

        }

        public void ReadPage1()
        {
            BinaryReader br = new BinaryReader(data1CD);
            br.BaseStream.Seek(pagesize, SeekOrigin.Begin);
            byte[] buf = new byte[100];


        }

        public void ReadPage2()
        {

        }

        public v8Table[] tables;

        //=======================================================================

        v8Table table_config;
        v8Table table_configsave;
        v8Table table_params;
        v8Table table_files;
        v8Table table_dbschema;
        v8Table table_configcas;
        v8Table table_configcassave;
        v8Table table__extensionsinfo;

        // таблицы - хранилища файлов
        //ConfigStorageTableConfig cs_config;
        //ConfigStorageTableConfigSave cs_configsave;

        // Таблицы хранилища конфигураций
        v8Table table_depot;
        v8Table table_users;
        v8Table table_objects;
        v8Table table_versions;
        v8Table table_labels;
        v8Table table_history;
        v8Table table_lastestversions;
        v8Table table_externals;
        v8Table table_selfrefs;
        v8Table table_outrefs;

        //=======================================================================

        public v8con   Page0;
        public objtab  Page1;
        public root_81 Page2;

        public db_ver version;           // версия базы
        public UInt32 pagesize = 0x1000; // размер одной страницы (до версии 8.2.14 всегда 0x1000 (4K), начиная с версии 8.3.8 от 0x1000 (4K) до 0x10000 (64K))
        public UInt32 length;            // длина базы в блоках (v8File)

        public V8Object free_blocks; // свободные блоки
        public V8Object root_object; // корневой объект

        public Int32 num_tables; // количество таблиц

        public bool is_open; // признак того что базу удалось открыть
        public bool is_read; // признак того что базу удалось прочитать, т.е. прочитаны все страницы
        public bool is_infobase; // признак информационной базы
        public bool is_depot; // признак хранилища конфигурации

        public Stream data1CD;

        private bool _ReadOnly;

        /// <summary>
        /// Свойство, показывающее в каком режиме открыт файл БД
        /// </summary>
        public bool ReadOnly
        {
            get
            {
                return get_readonly();
            }
            set
            {
                _ReadOnly = value;
            }
        }


        public bool get_readonly()
        {
            return _ReadOnly;
        }


        public byte[] getblock(UInt32 block_number)
        {
            if (data1CD == null)
                return null;
            if (block_number >= length)
            {
                Console.WriteLine($"Попытка чтения блока за пределами файла. Индекс блока {block_number}. Всего блоков {length}");
                return null;
            }

            new v8MemBlock((FileStream)data1CD, block_number, false, true);
            return v8MemBlock.getblock((FileStream)data1CD, block_number);
        }

        public bool getblock(ref byte[] buf, UInt32 block_number, Int32 blocklen = -1) // буфер принадлежит вызывающей процедуре
        {
            if (data1CD == null)
                return false;

            if (blocklen < 0)
                blocklen = (Int32)pagesize;

            if (block_number >= length)
            {
                Console.WriteLine($"Попытка чтения блока за пределами файла. Индекс блока {block_number}, всего блоков {length}");
                return false;
            }

            //memcpy(buf, MemBlock::getblock(fs, block_number), blocklen);


            v8MemBlock tmp_mem_block = new v8MemBlock((FileStream)data1CD, block_number, false, true);
            byte[] tmp_buf = v8MemBlock.getblock((FileStream)data1CD, block_number);
            Array.Copy(tmp_buf, buf, blocklen);
            return true;

        }

        public byte[] getblock_for_write(UInt32 block_number, bool read)
        {
            v8con bc;

            if ((FileStream)data1CD == null)
                return null;

            if (block_number > length)
            {
                Console.WriteLine($"Попытка получения блока за пределами файла базы. Индекс блока {block_number}. Всего блоков {length}");
                return null;
            }

            return new byte[10];
        }

        public UInt32 get_free_block()
        {
            return free_blocks.get_free_block();
        }

        public void set_block_as_free(UInt32 block_number)
        {
            free_blocks.set_block_as_free(block_number);
        }

        public void ReadHeader()
        {

        }


    }
}
