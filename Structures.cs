using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Read1CD
{
    #region Структуры файла базы данных 1С (0, 1, 2 - предопределенные страницы)
    // Нулевая страница
    //
    //    Нулевая страница(страница с адресом 0) располагается в самом начале файла базы.
    //    Нулевая страница не принадлежит никакому файлу или таблице.
    //    Она содержит сигнатуру базы, версию структуры базы и длину базы в страницах

    /// <summary>
    /// Cтруктура первой страницы контейнера
    /// </summary>
    public struct v8con
    {
        // восемь символов
        public Char[] sig; // сигнатура SIG_CON // сигнатура “1CDBMSV8”
        public byte ver1;
        public byte ver2;
        public byte ver3;
        public byte ver4;
        public UInt32 length;
        public UInt32 firstblock;
        public UInt32 pagesize;

        /*
        public String getver()
        {
            String ss = ver1.ToString();
            ss += ".";
            ss += ver2;
            ss += ".";
            ss += ver3;
            ss += ".";
            ss += ver4;
            return ss;
        }
        */

        public String Version
        {
            get
            {
                return ver1.ToString() + "." + ver2.ToString() + "." + ver3.ToString() + "." + ver4.ToString();
            }
        }

    }

    // Первая страница
    //
    // Предопределенный объект с адресом 1 – это специальный файл, содержащий все свободные страницы базы. 
    // Файл свободных страниц не принадлежит никакой таблице


    /*
    /// <summary>
    /// Структура страницы размещения уровня 1 версий от 8.0 до 8.2.14
    /// Все свободные страницы
    /// </summary>
    
    public struct objtab
    {
        public UInt32 numblocks;
        public UInt32[] blocks;
    }
    */

    // Вторая страница
    // И наконец, предопределенный объект с адресом 2 – это корневой файл. 
    // В корневом файле содержатся код локализации, количество таблиц в базе и список адресов всех таблиц. 
    // Корневой файл также не принадлежит никакой таблице

    /// <summary>
    /// структура версии
    /// </summary>
    public struct _version_rec
    {
        public UInt32 version_1; // версия реструктуризации
        public UInt32 version_2; // версия изменения
    }

    /// <summary>
    /// структура версии
    /// </summary>
    public struct _version
    {
        public UInt32 version_1; // версия реструктуризации
        public UInt32 version_2; // версия изменения
        public UInt32 version_3; // версия изменения 2
    }

    // Структура страницы размещения уровня 1 версий от 8.0 до 8.2.14
    public struct objtab
    {
        public Int32 numblocks;
        public UInt32[] blocks; // 1023
        public objtab(Int32 _numblocks, UInt32[] _blocks)
        {
            numblocks = _numblocks;
            blocks = _blocks;
        }

    }
    /// <summary>
    /// Структура страницы размещения уровня 1 версий от 8.3.8 
    /// </summary>
    public struct objtab838
    {
        public UInt32[] blocks; // реальное количество блоков зависит от размера страницы (pagesize) uint32_t blocks[1];
    }

    /// <summary>
    /// структура заголовочной страницы файла данных или файла свободных страниц 
    /// </summary>
    public struct v8ob
    {
        public char[] sig; // сигнатура SIG_OBJ
        public UInt32 len; // длина файла
        public _version version;
        public UInt32[] blocks; // 1018
    }

    /// <summary>
    /// Структура заголовочной страницы файла данных начиная с версии 8.3.8 
    /// </summary>
    public struct v838ob_data
    {
        //public char[] sig;       // сигнатура 0x1C 0xFD (1C File Data?)  sig[2];
        public byte[] sig;       // сигнатура 0x1C 0xFD (1C File Data?)  sig[2];

        // Уровень таблицы размещения 
        //   (0x0000 - в таблице blocks номера страниц с данными, 
        //    0x0001 - в таблице blocks номера страниц с таблицами 
        //             размещения второго уровня, в которых уже, в свою очередь, 
        //             находятся номера страниц с данными)
        public Int16 fatlevel;
        public _version version;
        public UInt64 len;       // длина файла
        public UInt32[] blocks;  // Реальная длина массива зависит от размера страницы и равна pagesize/4-6 (от это 1018 для 4К до 16378 для 64К)  blocks[1];
    }

    /// <summary>
    /// структура заголовочной страницы файла свободных страниц начиная с версии 8.3.8 
    /// </summary>
    public struct v838ob_free
    {
        //public char[] sig;     // сигнатура 0x1C 0xFF (1C File Free?)
        public byte[] sig;     // сигнатура 0x1C 0xFF (1C File Free?)
        public Int16 fatlevel; // 0x0000 пока! но может ... уровень таблицы размещения (0x0000 - в таблице blocks номера страниц с данными, 0x0001 - в таблице blocks номера страниц с таблицами размещения второго уровня, в которых уже, в свою очередь, находятся номера страниц с данными)
        public UInt32 version;        // ??? предположительно...
        public UInt32[] blocks;       // Реальная длина массива зависит от размера страницы и равна pagesize/4-6 (от это 1018 для 4К до 16378 для 64К)
    }

    /// <summary>
    /// типы внутренних файлов
    /// </summary>
    public enum v8objtype
    {
        unknown = 0, // тип неизвестен
        data80 = 1, // файл данных формата 8.0 (до 8.2.14 включительно)
        free80 = 2, // файл свободных страниц формата 8.0 (до 8.2.14 включительно)
        data838 = 3, // файл данных формата 8.3.8
        free838 = 4  // файл свободных страниц формата 8.3.8
    }


    /// <summary>
    /// Корневая страница
    /// </summary>
    public struct root_80
    {
        public char[] lang; // 8
        public UInt32 numblocks;
        public UInt32[] blocks;
    }

    /// <summary>
    /// Корневая страница
    /// </summary>
    public struct root_81
    {
        public byte[] lang; //32
        public UInt32 numblocks;
        public UInt32[] blocks;
    }

    // Объекты.
    // Вся остальная информация в базе хранится в объектах(файлах базы (v8File)). 
    // Каждый объект состоит из одного или более блоков.
    // У каждого объекта есть один заголовочный блок, блоки с таблицей размещения, и собственно данные.
    // Второе и третье может отсутствовать.

    //Структура первого блока каждого объекта такова:
    /*
    public struct v8ob
    {
        char[] sig;   // сигнатура “1CDBOBV8”
        int length;   // длина содержимого объекта
        int version1;
        int version2;

        uint version;
        uint[] blocks;
        //uint blocks[1018];

    }
    */

    /// <summary>
    /// Типы страниц
    /// </summary>
    public enum pagetype
    {
        lost,          // потерянная страница (не относится ни к одному объекту)
        root,          // корневая страница (страница 0)
        freeroot,      // корневая страница таблицы свободных блоков (страница 1)
        freealloc,     // страница размещения таблицы свободных блоков
        free,          // свободная страница
        rootfileroot,  // корневая страница корневого файла (страница 2)
        rootfilealloc, // страница размещения корневого файла
        rootfile,      // страница данных корневого файла
        descrroot,     // корневая страница файла descr таблицы
        descralloc,    // страница размещения файла descr таблицы
        descr,         // страница данных файла descr таблицы
        dataroot,      // корневая страница файла data таблицы
        dataalloc,     // страница размещения файла data таблицы
        data,          // страница данных файла data таблицы
        indexroot,     // корневая страница файла index таблицы
        indexalloc,    // страница размещения файла index таблицы
        index,         // страница данных файла index таблицы
        blobroot,      // корневая страница файла blob таблицы
        bloballoc,     // страница размещения файла blob таблицы
        blob           // страница данных файла blob таблицы
    };

    /// <summary>
    /// Структура принадлежности страницы
    /// </summary>
    public struct pagemaprec
    {
        public Int32 tab;     // Индекс в T_1CD::tables, -1 - страница не относится к таблицам
        public pagetype type; // тип страницы
        public UInt32 number; // номер страницы в своем типе
        public pagemaprec(Int32 _tab = -1, pagetype _type = pagetype.lost, UInt32 _number = 0)
        {
            tab = -1;
            type = _type;
            number = 0;
        }
    }

    public enum db_ver
    {
        ver8_0_3_0 = 1,
        ver8_0_5_0 = 2,
        ver8_1_0_0 = 3,
        ver8_2_0_0 = 4,
        ver8_2_14_0 = 5,
        ver8_3_8_0 = 6
    }

    public struct field_type_declaration
    {
        
        public type_fields type;
        public bool null_exists;
        public Int32 length;
        public Int32 precision;
        public bool case_sensitive;

        public static field_type_declaration parse_tree(tree field_tree)
        {

            field_type_declaration type_declaration;

            if (field_tree.get_type() != node_type.nd_string)
            {
                throw new Exception("Ошибка получения типа поля таблицы. Узел не является строкой.");
            }

            String sFieldType = field_tree.get_value();

            if (sFieldType == "B")
                type_declaration.type = type_fields.tf_binary;
            else if (sFieldType == "L")
                type_declaration.type = type_fields.tf_bool;
            else if (sFieldType == "N")
                type_declaration.type = type_fields.tf_numeric;
            else if (sFieldType == "NC")
                type_declaration.type = type_fields.tf_char;
            else if (sFieldType == "NVC")
                type_declaration.type = type_fields.tf_varchar;
            else if (sFieldType == "RV")
                type_declaration.type = type_fields.tf_version;
            else if (sFieldType == "NT")
                type_declaration.type = type_fields.tf_string;
            else if (sFieldType == "T")
                type_declaration.type = type_fields.tf_text;
            else if (sFieldType == "I")
                type_declaration.type = type_fields.tf_image;
            else if (sFieldType == "DT")
                type_declaration.type = type_fields.tf_datetime;
            else if (sFieldType == "VB")
                type_declaration.type = type_fields.tf_varbinary;
            else
            {
                throw new Exception($"Неизвестный тип поля таблицы. Тип поля {sFieldType}");
            }

            field_tree = field_tree.get_next();
            if (field_tree.get_type() != node_type.nd_number)
            {
                throw new Exception($"Ошибка получения признака NULL поля таблицы. Узел не является числом. Тип поля {sFieldType}");
            }

            String sNullExists = field_tree.get_value();
            if (sNullExists == "0")
                type_declaration.null_exists = false;
            else if (sNullExists == "1")
                type_declaration.null_exists = true;
            else
            {
                throw new Exception($"Неизвестное значение признака NULL поля таблицы. Признак NUL {sNullExists}");
            }

            field_tree = field_tree.get_next();
            if (field_tree.get_type() != node_type.nd_number)
            {
                throw new Exception($"Ошибка получения длины поля таблицы. Узел не является числом.");
            }

            type_declaration.length = Convert.ToInt32(field_tree.get_value());

            field_tree = field_tree.get_next();
            if (field_tree.get_type() != node_type.nd_number)
            {
                throw new Exception($"Ошибка получения точности поля таблицы. Узел не является числом.");
            }
            type_declaration.precision = Convert.ToInt32(field_tree.get_value());

            field_tree = field_tree.get_next();
            if (field_tree.get_type() != node_type.nd_string)
            {
                throw new Exception($"Ошибка получения регистрочувствительности поля таблицы. Узел не является строкой.");
            }

            String sCaseSensitive = field_tree.get_value();
            if (sCaseSensitive == "CS")
                type_declaration.case_sensitive = true;
            else if (sCaseSensitive == "CI")
                type_declaration.case_sensitive = false;
            else
            {
                throw new Exception($"Неизвестное значение регистрочувствительности поля таблицы. Регистрочувствительность {sFieldType}.");
            }

            return type_declaration;
        }
    }

    public struct index_record
    {
        public v8Field field;
        public Int32 len;
    }

    struct unpack_index_record
    {
        UInt32 _record_number; // номер (индекс) записи в таблице записей
        //unsigned char _index[1]; // значение индекса записи. Реальная длина значения определяется полем length класса index
        public byte[] _index;
    }

    // структура заголовка страницы-ветки индексов
    public struct branch_page_header
    {
        public UInt16 flags; // offset 0
        public UInt16 number_indexes; // offset 2
        public UInt32 prev_page; // offset 4 // для 8.3.8 - это номер страницы (реальное смещение = prev_page * pagesize), до 8.3.8 - это реальное смещение
        public UInt32 next_page; // offset 8 // для 8.3.8 - это номер страницы (реальное смещение = next_page * pagesize), до 8.3.8 - это реальное смещение
    }

    // структура заголовка страницы-листа индексов
    public struct leaf_page_header
    {
        public Int16 flags; // offset 0
        public UInt16 number_indexes; // offset 2
        public UInt32 prev_page; // offset 4 // для 8.3.8 - это номер страницы (реальное смещение = prev_page * pagesize), до 8.3.8 - это реальное смещение
        public UInt32 next_page; // offset 8 // для 8.3.8 - это номер страницы (реальное смещение = next_page * pagesize), до 8.3.8 - это реальное смещение
        public UInt16 freebytes; // offset 12
        public UInt32 numrecmask; // offset 14
        public UInt16 leftmask; // offset 18
        public UInt16 rightmask; // offset 20
        public UInt16 numrecbits; // offset 22
        public UInt16 leftbits; // offset 24
        public UInt16 rightbits; // offset 26
        public UInt16 recbytes; // offset 28
    }

    // Вспомогательная структура для упаковки индексов на странице-листе
    public struct _pack_index_record
    {
        public UInt32 numrec;
        public UInt32 left;
        public UInt32 right;
    }


    #endregion



    public static class Structures
    {
        public static root_81 ByteArrayToRoot81(byte[] src)
        {

            root_81 Res;

            Res.lang = new byte[32];
            Array.Copy(src, 0, Res.lang, 0, 32);
            Res.numblocks = BitConverter.ToUInt32(src, 32);

            Res.blocks = new UInt32[Res.numblocks];
            //Res.blocks = new UInt32[1];
            Array.Clear(Res.blocks, 0, Res.blocks.Length);
            Array.Copy(src, 36, Res.blocks, 0, Res.blocks.Length);

            return Res;

        }

        public static leaf_page_header ByteArrayToLeafPageHeader(byte[] src)
        {

            leaf_page_header Res;

            Res.flags          = BitConverter.ToInt16(src, 0);
            Res.number_indexes = BitConverter.ToUInt16(src, 2);
            Res.prev_page      = BitConverter.ToUInt32(src, 4);
            Res.next_page      = BitConverter.ToUInt32(src, 8);
            Res.freebytes      = BitConverter.ToUInt16(src, 12);
            Res.numrecmask     = BitConverter.ToUInt32(src, 14);
            Res.leftmask       = BitConverter.ToUInt16(src, 18);
            Res.rightmask      = BitConverter.ToUInt16(src, 20);
            Res.numrecbits     = BitConverter.ToUInt16(src, 22);
            Res.leftbits       = BitConverter.ToUInt16(src, 24);
            Res.rightbits      = BitConverter.ToUInt16(src, 26);
            Res.recbytes       = BitConverter.ToUInt16(src, 28);

            return Res;

        }

        public static objtab ByteArrayToObjtab(byte[] src)
        {
            objtab Res = new objtab(0, new UInt32[1023]);

            Res.numblocks = BitConverter.ToInt32(src, 0);
            Array.Copy(src, 4, Res.blocks, 0, Res.numblocks);

            return Res;
        }

        public static objtab838 ByteArrayToObjtab838(byte[] src)
        {
            objtab838 Res;

            Res.blocks = new UInt32[1023];
            Array.Clear(Res.blocks, 0, Res.blocks.Length);
            Array.Copy(src, 0, Res.blocks, 0, src.Length);

            return Res;
        }

        public static v8ob ByteArrayToV8ob(byte[] src)
        {
            // public char[] sig; // сигнатура SIG_OBJ
            // public UInt32 len; // длина файла
            // public _version version;
            // public UInt32[] blocks; // 1018

            v8ob Res;

            Res.sig = Encoding.UTF8.GetChars(src, 0, 8);
            Res.len = BitConverter.ToUInt32(src, 8);
            Res.version.version_1 = BitConverter.ToUInt32(src, 12);
            Res.version.version_2 = BitConverter.ToUInt32(src, 16);
            Res.version.version_3 = BitConverter.ToUInt32(src, 20);
            Res.blocks = new UInt32[1018];
            Array.Clear(Res.blocks, 0, Res.blocks.Length);
            Array.Copy(src, 24, Res.blocks, 0, src.Length - 24);

            return Res;
        }

        public static v838ob_data ByteArrayTov838ob(byte[] src)
        {
            // public char[] sig;       // сигнатура 0x1C 0xFD (1C File Data?)  sig[2];
            // public Int16 fatlevel;   // уровень таблицы размещения (0x0000 - в таблице blocks номера страниц с данными, 0x0001 - в таблице blocks номера страниц с таблицами размещения второго уровня, в которых уже, в свою очередь, находятся номера страниц с данными)
            // public _version version;
            // public UInt64 len;       // длина файла
            // public UInt32[] blocks;  // Реальная длина массива зависит от размера страницы и равна pagesize/4-6 (от это 1018 для 4К до 16378 для 64К)  blocks[1];

            v838ob_data Res;

            //Res.sig = Encoding.UTF8.GetChars(src, 0, 2);

            Res.sig = new byte[2];
            Array.Copy(src, 0, Res.sig, 0, 2);

            Res.fatlevel = BitConverter.ToInt16(src, 2);
            Res.version.version_1 = BitConverter.ToUInt32(src, 4);
            Res.version.version_2 = BitConverter.ToUInt32(src, 8);
            Res.version.version_3 = BitConverter.ToUInt32(src, 12);
            Res.len = BitConverter.ToUInt64(src, 16);
            //Res.blocks = new UInt32[16378];
            Res.blocks = new UInt32[1];
            Array.Clear(Res.blocks, 0, Res.blocks.Length);
            //Array.Copy(src, 24, Res.blocks, 0, src.Length - 20);
            Array.Copy(src, 24, Res.blocks, 0, 1);

            return Res;

        }

        public static v838ob_free ByteArrayTov838ob_free(byte[] src)
        {
            // public char[] sig;     // сигнатура 0x1C 0xFF (1C File Free?)
            // public Int16 fatlevel; // 0x0000 пока! но может ... уровень таблицы размещения (0x0000 - в таблице blocks номера страниц с данными, 0x0001 - в таблице blocks номера страниц с таблицами размещения второго уровня, в которых уже, в свою очередь, находятся номера страниц с данными)
            // public UInt32 version;        // ??? предположительно...
            // public UInt32[] blocks;       // Реальная длина массива зависит от размера страницы и равна pagesize/4-6 (от это 1018 для 4К до 16378 для 64К)

            v838ob_free Res;

            //Res.sig = Encoding.UTF8.GetChars(src, 0, 2);
            Res.sig = new byte[2];
            Array.Copy(src, 0, Res.sig, 0, 2);
            Res.fatlevel = BitConverter.ToInt16(src, 2);
            Res.version = BitConverter.ToUInt32(src, 4);
            //Res.blocks = new UInt32[16378];
            Res.blocks = new UInt32[1];
            Array.Clear(Res.blocks, 0, Res.blocks.Length);
            //Array.Copy(src, 8, Res.blocks, 0, src.Length - 4);
            Array.Copy(src, 8, Res.blocks, 0, 1);

            return Res;

        }

        public static String toXML(String _in_)
        {
            return new StringBuilder(_in_).Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("'", "&apos;").Replace("\"", "&quot;").ToString();
        }

        public static String GUIDasMS(char[] fr)
        {

            Int32 i, j;
            Char[] buf = new char[Constant.GUID_LEN + 1];
            Char sym;

            j = 0;
	        for(i = 3; i >= 0; i--)
        	{
                sym = (Char)((UInt32)'0' + ((UInt32)fr[i] >> 4));
                if (sym > '9') sym += (Char)((UInt32)'a' - (UInt32)'9' - 1);
                buf[j++] = sym;
                sym = (Char)((UInt32)'0' + ((UInt32)fr[i] & 0xf));
                if (sym > '9') sym += (Char)((UInt32)'a' - (UInt32)'9' - 1);
                buf[j++] = sym;
            }

            buf[j++] = '-';

            for (i = 5; i >= 4; i--)
        	{
                sym = (Char)((UInt32)'0' + ((UInt32)fr[i] >> 4));
                if (sym > '9') sym += (Char)((UInt32)'a' - (UInt32)'9' - 1);
                buf[j++] = sym;
                sym = (Char)((UInt32)'0' + ((UInt32)fr[i] & 0xf));
                if (sym > '9') sym += (Char)((UInt32)'a' - (UInt32)'9' - 1);
                buf[j++] = sym;
            }

            buf[j++] = '-';

            for (i = 7; i >= 6; i--)
        	{
                sym = (Char)((UInt32)'0' + ((UInt32)fr[i] >> 4));
                if (sym > '9') sym += (Char)((UInt32)'a' - (UInt32)'9' - 1);
                buf[j++] = sym;
                sym = (Char)((UInt32)'0' + ((UInt32)fr[i] & 0xf));
                if (sym > '9') sym += (Char)((UInt32)'a' - (UInt32)'9' - 1);
                buf[j++] = sym;
        	}

            buf[j++] = '-';

            for (i = 8; i< 10; i++)
        	{
                sym = (Char)((UInt32)'0' + ((UInt32)fr[i] >> 4));
                if (sym > '9') sym += (Char)((UInt32)'a' - (UInt32)'9' - 1);
                buf[j++] = sym;
                sym = (Char)((UInt32)'0' + ((UInt32)fr[i] & 0xf));
                if (sym > '9') sym += (Char)((UInt32)'a' - (UInt32)'9' - 1);
                buf[j++] = sym;
            }
            buf[j++] = '-';
        	for(i = 10; i< 16; i++)
        	{
                sym = (Char)((UInt32)'0' + ((UInt32)fr[i] >> 4));
                if (sym > '9') sym += (Char)((UInt32)'a' - (UInt32)'9' - 1);
                buf[j++] = sym;
                sym = (Char)((UInt32)'0' + ((UInt32)fr[i] & 0xf));
                if (sym > '9') sym += (Char)((UInt32)'a' - (UInt32)'9' - 1);
                buf[j++] = sym;
            }
            //buf[j] = '0';
        
        	return new String(buf);
        }

        public static String GUIDas1C(char[] fr)
        {
            Int32 i, j;
            Char[] buf = new char[Constant.GUID_LEN + 1];
            Char sym;

            j = 0;
            for (i = 12; i < 16; i++)
            {
                sym = (Char)((UInt32)'0' + ((UInt32)fr[i] >> 4));
                if (sym > '9') sym += (Char)((UInt32)'a' - (UInt32)'9' - 1);
                buf[j++] = sym;
                sym = (Char)((UInt32)'0' + ((UInt32)fr[i] & 0xf));
                if (sym > '9') sym += (Char)((UInt32)'a' - (UInt32)'9' - 1);
                buf[j++] = sym;
            }

            buf[j++] = '-';
	        for(i = 10; i < 12; i++)
        	{
                sym = (Char)((UInt32)'0' + ((UInt32)fr[i] >> 4));
                if (sym > '9') sym += (Char)((UInt32)'a' - (UInt32)'9' - 1);
                buf[j++] = sym;
                sym = (Char)((UInt32)'0' + ((UInt32)fr[i] & 0xf));
                if (sym > '9') sym += (Char)((UInt32)'a' - (UInt32)'9' - 1);
                buf[j++] = sym;
            }

            buf[j++] = '-';
	        for(i = 8; i < 10; i++)
	        {
                sym = (Char)((UInt32)'0' + ((UInt32)fr[i] >> 4));
                if (sym > '9') sym += (Char)((UInt32)'a' - (UInt32)'9' - 1);
                buf[j++] = sym;
                sym = (Char)((UInt32)'0' + ((UInt32)fr[i] & 0xf));
                if (sym > '9') sym += (Char)((UInt32)'a' - (UInt32)'9' - 1);
                buf[j++] = sym;
            }

            buf[j++] = '-';
	        for(i = 0; i < 2; i++)
	        {
                sym = (Char)((UInt32)'0' + ((UInt32)fr[i] >> 4));
                if (sym > '9') sym += (Char)((UInt32)'a' - (UInt32)'9' - 1);
                buf[j++] = sym;
                sym = (Char)((UInt32)'0' + ((UInt32)fr[i] & 0xf));
                if (sym > '9') sym += (Char)((UInt32)'a' - (UInt32)'9' - 1);
                buf[j++] = sym;
            }

            buf[j++] = '-';
	        for(i = 2; i < 8; i++)
	        {
                sym = (Char)((UInt32)'0' + ((UInt32)fr[i] >> 4));
                if (sym > '9') sym += (Char)((UInt32)'a' - (UInt32)'9' - 1);
                buf[j++] = sym;
                sym = (Char)((UInt32)'0' + ((UInt32)fr[i] & 0xf));
                if (sym > '9') sym += (Char)((UInt32)'a' - (UInt32)'9' - 1);
                buf[j++] = sym;
            }
            return new String(buf);
        }












    }

    public static class Constant
    {
        // сигнатура файла базы 1С
        public static readonly char[] SIG_CON = { '1', 'C', 'D', 'B', 'M', 'S', 'V', '8' };
        public static readonly char[] SIG_OBJ = { '1', 'C', 'D', 'B', 'O', 'B', 'V', '8' };

        // Значения битовых флагов в заголовке страницы индекса
        public static readonly Int16 indexpage_is_root = 1; // Установленный флаг означает, что страница является корневой
        public static readonly Int16 indexpage_is_leaf = 2; // Установленный флаг означает, что страница является листом, иначе веткой

        //public static readonly UInt32 ONE_GB = 1073741824;
        public static readonly UInt32 ONE_GB = 0x40000000; // 1024 * 1024 * 1024, 0x400 * 0x400 * 0x400

        public static readonly UInt32 GUID_LEN = 36;

        public static readonly Int32 PAGE4K  = 0x1000;
        public static readonly Int32 PAGE8K  = 0x2000;
        public static readonly Int32 PAGE16K = 0x4000;
        public static readonly Int32 PAGE32K = 0x8000;
        public static readonly Int32 PAGE64K = 0x10000;

    }
}
