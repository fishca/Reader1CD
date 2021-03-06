﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using static Read1CD.Constant;
using static Read1CD.Structures;

namespace Read1CD
{
    
    /// <summary>
    /// Класс - объекты - файлы базы
    /// 
    /// Объекты.
    ///    Вся остальная информация в базе хранится в объектах(файлах базы). 
    ///    Каждый объект состоит из одного или более блоков.
    ///    У каждого объекта есть один заголовочный блок, блоки с таблицей размещения, и собственно данные.
    ///    Второе и третье может отсутствовать.
    /// 
    /// 
    /// 
    /// 
    /// 
    /// </summary>
    public class V8Object
    {
        
        public v8Base1CD _base;
        
        public UInt64 len;                 // длина объекта. Для типа таблицы свободных страниц - количество свободных блоков
        public _version version;           // текущая версия объекта
        public _version_rec version_rec;   // текущая версия записи
        public bool new_version_recorded;  // признак, что новая версия объекта записана
        public v8objtype type;             // тип и формат файла
        public Int32 fatlevel;             // Количество промежуточных уровней в таблице размещения
        public UInt64 numblocks;           // кол-во страниц в корневой таблице размещения объекта
        public UInt32 real_numblocks;      // реальное кол-во страниц в корневой таблице (только для файлов свободных страниц, может быть больше numblocks)

        //uint32_t* blocks;        

        // struct {
        //
        //        char lang[N];                Для версии базы «8.0.5.0» эта структура выглядит так: char lang[8];   
        //                                     Для версии базы «8.1.0.0» эта структура выглядит так: char lang[32]; 
        //                                       - код языка базы
        //
        //        int numblocks;                 - содержится количество элементов в массиве tableblocks
        //
        //        int tableblocks[numblocks];    - в массиве же tableblocks содержатся индексы объектов, 
        //                                            содержащих все таблицы данных. Т.е. таблиц в базе ровно numblocks.
        //
        //       }



        // наверное пока так               // номера страниц в массиве                                    
        public UInt32[] blocks;            // таблица страниц корневой таблицы размещения объекта (т.е. уровня 0)
                                           


        public UInt32 block;               // номер блока объекта
        public byte[] data;                // данные, представляемые объектом, NULL если не прочитаны или len = 0   //char* data;                      

        public static V8Object first;      
        public static V8Object last;

        public V8Object next;
        public V8Object prev;

        public UInt32 lastdataget;          // время (Windows time, в миллисекундах) последнего обращения к данным объекта (data)
        public bool lockinmemory;

        /// <summary>
        /// Установка новой длины объекта
        /// </summary>
        /// <param name="_len"></param>
        public void set_len(UInt64 _len) // установка новой длины объекта
        {
            UInt32 num_data_blocks;
            UInt32 num_blocks;
            UInt32 cur_data_blocks;
            UInt32 bl;
            UInt32 i;
            v8ob b;
            v838ob_data bd;
            objtab838 bb;
            UInt32 offsperpage;
            UInt64 maxlen;
            Int32 newfatlevel;

            if (len == _len) return;

            if (type == v8objtype.free80 || type == v8objtype.free838)
            {
                // Таблица свободных блоков
                Console.WriteLine("Попытка установки длины в файле свободных страниц");
                return;
            }

            data = null;

            if (type == v8objtype.data80)
            {

                b = ByteArrayToV8ob(_base.getblock_for_write(block, true));
                b.len = (UInt32)_len;

                num_data_blocks = (UInt32)(_len + 0xfff) >> 12;
                num_blocks = (num_data_blocks + 1022) / 1023;
                cur_data_blocks = (UInt32)(len + 0xfff) >> 12;

                if (numblocks != num_blocks)
                {
                    blocks = null;
                    if (num_blocks != 0)
                        blocks = new UInt32[num_blocks];
                    else
                        blocks = null;
                }
                if (num_data_blocks > cur_data_blocks)
                {
                    objtab ot;
                    // Увеличение длины объекта
                    if (numblocks != 0)
                        ot = ByteArrayToObjtab(_base.getblock_for_write(b.blocks[numblocks - 1], true));
                    for (; cur_data_blocks < num_data_blocks; cur_data_blocks++)
                    {
                        i = cur_data_blocks % 1023;
                        if (i == 0)
                        {
                            bl = _base.get_free_block();
                            b.blocks[numblocks++] = bl;

                            //ot = (objtab*)base->getblock_for_write(bl, false);
                            ot = ByteArrayToObjtab(_base.getblock_for_write(bl, false));

                            ot.numblocks = 0;
                        }
                        bl = _base.get_free_block();
                        _base.getblock_for_write(bl, false); // получаем блок без чтения, на случай, если блок вдруг в конце файла
                        //ot.blocks[i] = bl;  // TODO: надо доработать
                        ot.numblocks = (Int32)i + 1;
                    }
                }
                else if (num_data_blocks < cur_data_blocks)
                {
                    // Уменьшение длины объекта
                    objtab ot = ByteArrayToObjtab(_base.getblock_for_write(b.blocks[numblocks - 1], true));

                    for (cur_data_blocks--; cur_data_blocks >= num_data_blocks; cur_data_blocks--)
                    {
                        i = cur_data_blocks % 1023;
                        _base.set_block_as_free(ot.blocks[i]);
                        ot.blocks[i] = 0;
                        ot.numblocks = (Int32)i;
                        if (i == 0)
                        {
                            _base.set_block_as_free(b.blocks[--numblocks]);
                            b.blocks[numblocks] = 0;
                            if (numblocks != 0)
                                ot = ByteArrayToObjtab(_base.getblock_for_write(b.blocks[numblocks - 1], true));
                        }
                    }

                }
                len = _len;
                if (numblocks != 0)
                {
                    //memcpy(blocks, b->blocks, numblocks * 4);
                    Array.Copy(b.blocks, blocks, (Int32)numblocks * 4);
                }

                write_new_version();

            }
            else if (type == v8objtype.data838)
            {
                offsperpage = _base.pagesize / 4;
                maxlen = _base.pagesize * offsperpage * (offsperpage - 6);
                if (_len > maxlen)
                {
                    Console.WriteLine($"Попытка установки длины файла больше максимальной. Номер страницы файла {block}. Максимальная длина файла {maxlen}. Запрошенная длина файла {_len}");
                    _len = maxlen;
                }

                //bd = (v838ob_data*)base->getblock_for_write(block, true);
                bd = ByteArrayTov838ob(_base.getblock_for_write(block, true));
                bd.len = _len;

                num_data_blocks = (UInt32)(_len + _base.pagesize - 1) / _base.pagesize;
                if (num_data_blocks > offsperpage - 6)
                {
                    num_blocks = (num_data_blocks + offsperpage - 1) / offsperpage;
                    newfatlevel = 1;
                }
                else
                {
                    num_blocks = num_data_blocks;
                    newfatlevel = 0;
                }
                cur_data_blocks = (UInt32)(len + _base.pagesize - 1) / _base.pagesize;

                if (numblocks != num_blocks)
                {
                    blocks = null;
                    if (num_blocks != 0)
                        blocks = new UInt32[num_blocks];
                    else
                        blocks = null;
                }

                if (num_data_blocks > cur_data_blocks)
                {
                    // Увеличение длины объекта
                    if (fatlevel == 0 && newfatlevel != 0)
                    {
                        bl = _base.get_free_block();
                        //bb = (objtab838*)base->getblock_for_write(bl, false);
                        bb = ByteArrayToObjtab838(_base.getblock_for_write(bl, false));
                        //memcpy(bb->blocks, bd->blocks, numblocks * 4);
                        Array.Copy(bd.blocks, bb.blocks, (Int32)numblocks * 4);
                        fatlevel = newfatlevel;
                        bd.fatlevel = (Int16)newfatlevel;
                        bd.blocks[0] = bl;
                        numblocks = 1;
                    }
                    else
                    { 
                        bb = ByteArrayToObjtab838(_base.getblock_for_write(bd.blocks[numblocks - 1], true));
                    }

                    if (fatlevel != 0)
                    {
                        for (; cur_data_blocks < num_data_blocks; cur_data_blocks++)
                        {
                            i = cur_data_blocks % offsperpage;
                            if (i == 0)
                            {
                                bl = _base.get_free_block();
                                bd.blocks[numblocks++] = bl;
                                bb = ByteArrayToObjtab838(_base.getblock_for_write(bl, false));
                            }
                            bl = _base.get_free_block();
                            _base.getblock_for_write(bl, false); // получаем блок без чтения, на случай, если блок вдруг в конце файла
                            bb.blocks[i] = bl;
                        }
                    }
                    else
                    {
                        for (; cur_data_blocks < num_data_blocks; cur_data_blocks++)
                        {
                            bl = _base.get_free_block();
                            _base.getblock_for_write(bl, false); // получаем блок без чтения, на случай, если блок вдруг в конце файла
                            bd.blocks[cur_data_blocks] = bl;
                        }
                    }
                }
                else if (num_data_blocks < cur_data_blocks)
                {
                    // Уменьшение длины объекта
                    if (fatlevel != 0)
                    {
                        //bb = (objtab838*)base->getblock_for_write(b->blocks[numblocks - 1], true);
                        //bb = ByteArrayToObjtab838(_base.getblock_for_write(b.blocks[numblocks - 1], true));
                        bb = ByteArrayToObjtab838(_base.getblock_for_write((UInt32)numblocks - 1, true)); // TODO: надо доработать
                        for (cur_data_blocks--; cur_data_blocks >= num_data_blocks; cur_data_blocks--)
                        {
                            i = cur_data_blocks % offsperpage;
                            _base.set_block_as_free(bb.blocks[i]);
                            bb.blocks[i] = 0;
                            if (i == 0)
                            {
                                _base.set_block_as_free(bd.blocks[--numblocks]);
                                bd.blocks[numblocks] = 0;
                                if (numblocks != 0)
                                {
                                    //bb = ByteArrayToObjtab838(_base.getblock_for_write(b.blocks[numblocks - 1], true));
                                    bb = ByteArrayToObjtab838(_base.getblock_for_write( (UInt32)numblocks - 1, true)); // TODO: надо доработать
                                }
                            }
                        }
                    }
                    else
                    {
                        for (cur_data_blocks--; cur_data_blocks >= num_data_blocks; cur_data_blocks--)
                        {
                            _base.set_block_as_free(bd.blocks[cur_data_blocks]);
                            bd.blocks[cur_data_blocks] = 0;
                        }
                        numblocks = num_data_blocks;
                    }

                    if (fatlevel != 0 && newfatlevel == 0)
                    {
                        if (numblocks != 0)
                        {
                            bl = bd.blocks[0];
                            //memcpy(bd->blocks, bb->blocks, num_data_blocks * 4);
                            //Array.Copy(bb.blocks, bd.blocks, num_data_blocks * 4);  // TODO: надо доработать
                            _base.set_block_as_free(bl);
                        }
                        fatlevel = 0;
                        bd.fatlevel = 0;
                    }

                }

                len = _len;
                if (numblocks != 0)
                {
                    //memcpy(blocks, bd->blocks, numblocks * 4);
                    Array.Copy(bd.blocks, blocks, (Int32)numblocks * 4);
                }
                write_new_version();

            }

        }

        /// <summary>
        /// Конструктор нового (еще не существующего) объекта
        /// </summary>
        /// <param name="_base"></param>
        public V8Object(v8Base1CD _base)
        {
            UInt32 blockNum;
            byte[] b;

            blockNum = _base.get_free_block();
            b = _base.getblock_for_write(blockNum, false);

            //memset(b, 0, _base->pagesize);

            if (_base.version < db_ver.ver8_3_8_0)
            {
                //memcpy(((v8ob*)b)->sig, SIG_OBJ, 8);

                Array.Copy(SIG_OBJ, ByteArrayToV8ob(b).sig, 8);
            }
            else
            {
                b[0] = 0x1c;
                b[1] = 0xfd;
            }
            init(_base, (Int32)blockNum);

        }

        /// <summary>
        /// Конструктор существующего объекта
        /// </summary>
        /// <param name="_base"></param>
        /// <param name="blockNum"></param>
        public V8Object(v8Base1CD _base, Int32 blockNum)
        {
            init(_base, blockNum);
        }

        /// <summary>
        /// Инициализация с параметрами
        /// </summary>
        /// <param name="_base"></param>
        /// <param name="blockNum"></param>
        public void init(v8Base1CD _base, Int32 blockNum)
        {
            this._base = _base;
            prev = last;
            next = null;

            if (last != null)
                last.next = this;
            else
                first = this;
            last = this;
            if (blockNum == 1)
            {
                if ((int)this._base.version < (int)db_ver.ver8_3_8_0)
                    type = v8objtype.free80;
                else
                    type = v8objtype.free838;

            }
            else
            {
                if ((int)this._base.version < (int)db_ver.ver8_3_8_0)
                    type = v8objtype.data80;
                else
                    type = v8objtype.data838;
            }

            if (type == v8objtype.data80 || type == v8objtype.free80)
            {
                fatlevel = 1;
                v8ob t = new v8ob();
                Byte[] buf = new Byte[0x1000];

                //this._base.getblock(t, (UInt32)blockNum);
                this._base.getblock(ref buf, (UInt32)blockNum);

                t = ByteArrayToV8ob(buf);
                if (!t.sig.SequenceEqual(SIG_OBJ))
                {
                    t = new v8ob();
                    init();
                    Console.WriteLine($"Ошибка получения объекта из блока. Блок не является объектом. Блок {blockNum}");
                }

                len = t.len;
                version.version_1 = t.version.version_1;
                version.version_2 = t.version.version_2;
                version.version_3 = t.version.version_3;
                version_rec.version_1 = version.version_1 + 1;
                version_rec.version_2 = 0;
                new_version_recorded = false;
                block = (UInt32)blockNum;
                real_numblocks = 0;
                data = null;

                if (type == v8objtype.free80)
                {
                    if (len != 0)
                        numblocks = (len - 1) / 0x400 + 1;
                    else
                        numblocks = 0;

                    // в таблице свободных блоков в разделе blocks может быть больше блоков, чем numblocks
                    // numblocks - кол-во блоков с реальными данными
                    // оставшиеся real_numblocks - numblocks блоки принадлежат объекту, но не содержат данных
                    while (t.blocks[real_numblocks] != 0)
                        real_numblocks++;
                    if (real_numblocks != 0)
                    {
                        blocks = new UInt32[real_numblocks];
                        //memcpy(blocks, t->blocks, real_numblocks * sizeof(*blocks));
                        Array.Copy(t.blocks, 0, blocks, 0, real_numblocks * 4);
                    }
                    else
                        blocks = null;

                }
                else
                {
                    if (len != 0)
                        numblocks = (len - 1) / 0x3ff000 + 1;
                    else
                        numblocks = 0;
                    if (numblocks != 0)
                    {
                        blocks = new UInt32[numblocks];
                        //memcpy(blocks, t->blocks, numblocks * sizeof(*blocks));
                        Array.Copy(t.blocks, 0, blocks, 0, real_numblocks * 4);
                    }
                    else
                        blocks = null;
                }

            }
            else if (type == v8objtype.data838)
            {
                byte[] b = new byte[this._base.pagesize];
                this._base.getblock(ref b, (UInt32)blockNum);
                v838ob_data t = ByteArrayTov838ob(b);
                if (t.sig[0] != 0x1c || t.sig[1] != 0xfd)
                {
                    b = null;
                    init();
                    Console.WriteLine($"Ошибка получения файла из страницы. Страница не является заголовочной страницей файла данных. Блок {blockNum}");
                    return;
                }
                len = t.len;
                fatlevel = t.fatlevel;
                if (fatlevel == 0 && len > ((this._base.pagesize / 4 - 6) * this._base.pagesize))
                {
                    b = null;
                    init();
                    Console.WriteLine($"Ошибка получения файла из страницы. Длина файла больше допустимой при одноуровневой таблице размещения. Блок {blockNum}. Длина файла {len}");
                    return;
                }
                version.version_1 = t.version.version_1;
                version.version_2 = t.version.version_2;
                version.version_3 = t.version.version_3;
                version_rec.version_1 = version.version_1 + 1;
                version_rec.version_2 = 0;
                new_version_recorded = false;
                block = (UInt32)blockNum;
                real_numblocks = 0;
                data = null;

                if (len != 0)
                {
                    if (fatlevel == 0)
                    {
                        numblocks = (len - 1) / this._base.pagesize + 1;
                    }
                    else
                    {
                        numblocks = (len - 1) / (this._base.pagesize / 4 * this._base.pagesize) + 1;
                    }
                }
                else
                    numblocks = 0;

                if ( numblocks !=0 )
                {
                    blocks = new UInt32[numblocks];
                    
                    //memcpy(blocks, t->blocks, numblocks * sizeof(*blocks));
                    //Array.Copy(t.blocks, 0, blocks, 0, (int)numblocks);
                    Array.Copy(t.blocks, 0, blocks, 0, t.blocks.Length);
                }
                else
                    blocks = null;

                b = null;
            }
            else
            {
                byte[] b = new byte[this._base.pagesize];
                
                this._base.getblock(ref b, (UInt32)blockNum);

                v838ob_free t = ByteArrayTov838ob_free(b);

                if (t.sig[0] != 0x1c || t.sig[1] != 0xff)
                {
                    b = null;
                    init();
                    Console.WriteLine($"Ошибка получения файла из страницы. Страница не является заголовочной страницей файла свободных блоков. Блок {blockNum}");
                    return;
                }

                len = 0; // ВРЕМЕННО! Пока не понятна структура файла свободных страниц

                version.version_1 = t.version;
                version_rec.version_1 = version.version_1 + 1;
                version_rec.version_2 = 0;
                new_version_recorded = false;
                block = (UInt32)blockNum;
                real_numblocks = 0;
                data = null;

                if (len != 0)
                    numblocks = (len - 1) / 0x400 + 1;
                else
                    numblocks = 0;

                // в таблице свободных блоков в разделе blocks может быть больше блоков, чем numblocks
                // numblocks - кол-во блоков с реальными данными
                // оставшиеся real_numblocks - numblocks блоки принадлежат объекту, но не содержат данных
                while (t.blocks[real_numblocks] != 0)
                    real_numblocks++;
                if (real_numblocks != 0)
                {
                    blocks = new UInt32[real_numblocks];
                    //memcpy(blocks, t->blocks, real_numblocks * sizeof(*blocks));
                    Array.Copy(t.blocks, 0, blocks, 0, real_numblocks * 4);
                }
                else
                    blocks = null;

                b = null;

            }

        }

        /// <summary>
        /// Инициализация без параметров
        /// </summary>
        public void init()
        {
            len = 0;
            version.version_1 = 0;
            version.version_2 = 0;
            version_rec.version_1 = 0;
            version_rec.version_2 = 0;
            new_version_recorded = false;
            numblocks = 0;
            real_numblocks = 0;
            blocks = null;
            block = 999999;
            data = null;
            lockinmemory = false;
            type = v8objtype.unknown;
            fatlevel = 0;
        }

        /// <summary>
        /// чтение всего объекта целиком, поддерживает кеширование объектов. Буфер принадлежит объекту
        /// </summary>
        /// <returns></returns>
        public byte[] getdata() 
        {

            byte[] tt;
            objtab b;
            objtab838 bb;
            UInt32 i, l;
            Int32 j, pagesize, blocksperpage;
            UInt64 ll;
            UInt32 curlen = 0;

            //lastdataget = GetTickCount();
            if (len == 0)
                return null;

            if (data != null)
                return data;

            if (type == v8objtype.free80)
            {
                l = (UInt32)len * 4;
                data = new byte[l];
                tt = data;
                i = 0;
                while (l > PAGE4K)
                {
                    _base.getblock(ref tt, blocks[i++]);
                    // tt += PAGE4K; TODO: Надо понять что с этим сделать
                    l -= (UInt32)PAGE4K;
                }
                _base.getblock(ref tt, blocks[i], (Int32)l);
            }
            else if (type == v8objtype.data80)
            {
                l = (UInt32)len;
                data = new byte[l];
                tt = data;
                for (i = 0; i < numblocks; i++)
                {
                    //b = (objtab*)base->getblock(blocks[i]);
                    b = ByteArrayToObjtab(_base.getblock(blocks[i]));


                    for (j = 0; j < b.numblocks; j++)
                    {
                        //curlen = std::min(DEFAULT_PAGE_SIZE, l);
                        curlen = (UInt32)Math.Min(PAGE4K, l);
                        _base.getblock(ref tt, b.blocks[j], (Int32)curlen);
                        if (l <= curlen)
                            break;
                        l -= curlen;
                        // tt += PAGE4K; TODO: Надо понять что с этим сделать
                    }
                    if (l <= curlen) break;
                }
            }
            else if (type == v8objtype.data838)
            {
                pagesize = (Int32)_base.pagesize;
                blocksperpage = pagesize / 4;
                ll = len;
                data = new byte[ll];
                tt = data;
                if (fatlevel != 0)
                {
                    for (i = 0; i < numblocks; i++)
                    {
                        //bb = (objtab838*)base->getblock(blocks[i]);
                        bb = ByteArrayToObjtab838(_base.getblock(blocks[i]));
                        for (j = 0; j < blocksperpage; j++)
                        {
                            curlen = ll > (UInt32)pagesize ? (UInt32)pagesize : (UInt32)ll;
                            _base.getblock(ref tt, bb.blocks[j], (Int32)curlen);
                            if (ll <= curlen) break;
                            ll -= curlen;
                            
                            // tt += pagesize; TODO: Надо понять что с этим сделать
                        }
                        if (ll <= curlen) break;
                    }
                }
                else
                {
                    for (i = 0; i < numblocks; i++)
                    {
                        //curlen = ll > pagesize ? pagesize : ll;
                        curlen = ll > (UInt32)pagesize ? (UInt32)pagesize : (UInt32)ll;
                        _base.getblock(ref tt, blocks[i], (Int32)curlen);
                        if (ll <= curlen)
                            break;
                        ll -= curlen;
                        // tt += pagesize; TODO: Надо понять что с этим сделать
                    }
                }
            }
            else if (type == v8objtype.free838)
            {
                // TODO: реализовать v8object::getdata() для файла свободных страниц формата 8.3.8
            }
            return data;

            //return new byte[100];

        }

        /// <summary>
        /// чтение кусочка объекта, поддерживает кеширование блоков. Буфер не принадлежит объекту
        /// </summary>
        /// <param name="buf"></param>
        /// <param name="_start"></param>
        /// <param name="_length"></param>
        /// <returns></returns>
        public byte[] getdata(byte[] buf, UInt64 _start, UInt64 _length) 
        {
            UInt32 curblock;
            UInt32 curoffblock;
            //byte[] _buf = new byte[0x1000];
            byte[] _buf = new byte[PAGE8K];
            byte[] _bu;
            UInt32 curlen = 0;
            UInt32 destIndex = 0;
            UInt32 offsperpage = 0;

            objtab b;
            objtab838 bb;
            UInt32 curobjblock = 0;
            UInt32 curoffobjblock = 0;

            if (data != null)
            {
                //memcpy(buf, data + _start, _length);
                Array.Copy(data, (int)_start, buf, 0, (int)_length);
            }
            else
            {
                if (type == v8objtype.free80)
                {
                    if (_start + _length > len * 4)
                    {
                        Console.WriteLine($"Попытка чтения данных за пределами объекта. Номер блока объекта, {block}. Длина объекта, {len * 4}. Начало читаемых данных, {_start}. Длина читаемых данных {_length}");
                        return null;
                    }
                    curblock = (UInt32)_start >> 12;
                    //_buf = (char*)buf;
                    Array.Copy(buf, _buf, buf.Length);
                    curoffblock = (UInt32)_start - (curblock << 12);
                    // curlen = std::min(static_cast<uint64_t>(DEFAULT_PAGE_SIZE - curoffblock), _length);
                    curlen = Math.Min((UInt32)(0x1000 - curoffblock), (UInt32)_length);

                    while (_length != 0)
                    {
                        _bu = _base.getblock(blocks[curblock++]);
                        if (_bu == null)
                            return null;

                        Array.Copy(_bu, curoffblock, _buf, destIndex, curlen);
                        // _buf += curlen; пока не понятно что с этим делать
                        destIndex += curlen;
                        _length -= curlen;
                        curoffblock = 0;
                        curlen = Math.Min((UInt32)(0x1000 - curoffblock), (UInt32)_length);
                    }

                }
                else if (type == v8objtype.data80)
                {
                    if (_start + _length > len)
                    {
                        Console.WriteLine($"Попытка чтения данных за пределами объекта. Номер блока объекта, {block}. Длина объекта, {len * 4}. Начало читаемых данных, {_start}. Длина читаемых данных {_length}");
                        return null;
                    }

                    curblock = (UInt32)_start >> 12;
                    Array.Copy(_buf, buf, buf.Length);
                    curoffblock = (UInt32)_start - (curblock << 12);
                    curlen = Math.Min((UInt32)(0x1000 - curoffblock), (UInt32)_length);

                    curobjblock = curblock / 1023;
                    curoffobjblock = curblock - curobjblock * 1023;
                    b = ByteArrayToObjtab(_base.getblock(blocks[curobjblock++]));
                    /*
                    if (!b)
                    {
                        return nullptr;
                    }
                    */
                    while (_length != 0)
                    {
                        _bu = _base.getblock(b.blocks[curoffobjblock++]);
                        if (_bu == null)
                        {
                            return null;
                        }
                        //memcpy(_buf, _bu + curoffblock, curlen);
                        Array.Copy(_bu, curoffblock, buf, destIndex, curlen);

                        //_buf += curlen; пока не понятно что делать
                        destIndex += curlen;
                        _length -= curlen;
                        curoffblock = 0;
                        curlen = Math.Min((UInt32)(0x1000 - curoffblock), (UInt32)_length);
                        if (_length > 0)
                        {
                            if (curoffobjblock >= 1023)
                            {
                                curoffobjblock = 0;
                                b = ByteArrayToObjtab(_base.getblock(blocks[curobjblock++]));
                            }
                        }
                    }
                }
                else if (type == v8objtype.data838)
                {
                    if (_start + _length > len)
                    {
                        Console.WriteLine($"Попытка чтения данных за пределами объекта. Номер блока объекта, {block}. Длина объекта, {len * 4}. Начало читаемых данных, {_start}. Длина читаемых данных {_length}");
                        return null;
                    }

                    curblock = (UInt32)_start / _base.pagesize;

                    //_buf = (char*)buf;
                    Array.Copy(_buf, buf, buf.Length);
                    offsperpage = _base.pagesize / 4;
                    curoffblock = (UInt32)_start - (curblock * _base.pagesize);
                    //curlen = std::min(static_cast<uint64_t>(base->pagesize - curoffblock), _length);
                    curlen = Math.Min((UInt32)(_base.pagesize - curoffblock), (UInt32)_length);
                    if (fatlevel != 0)
                    {
                        curobjblock = curblock / offsperpage;
                        curoffobjblock = curblock - curobjblock * offsperpage;

                        bb = ByteArrayToObjtab838(_base.getblock(blocks[curobjblock++]));
                        /*
                        if (!bb)
                        {
                            return nullptr;
                        }
                        */
                        while (_length != 0)
                        {
                            _bu = _base.getblock(bb.blocks[curoffobjblock++]);
                            if (_bu == null)
                            {
                                return null;
                            }
                            //memcpy(_buf, _bu + curoffblock, curlen);
                            Array.Copy(_bu, curoffblock, buf, destIndex, curlen);
                            //_buf += curlen;
                            destIndex += curlen;
                            _length -= curlen;
                            curoffblock = 0;
                            //curlen = std::min(static_cast<uint64_t>(base->pagesize), _length);
                            curlen = Math.Min((UInt32)(_base.pagesize - curoffblock), (UInt32)_length);
                            if (_length > 0)
                            {
                                if (curoffobjblock >= offsperpage)
                                {
                                    curoffobjblock = 0;
                                    bb = ByteArrayToObjtab838(_base.getblock(blocks[curobjblock++]));
                                }
                            }
                        }
                    }
                    else
                    {
                        destIndex = 0;
                        while (_length != 0)
                        {
                            _bu = _base.getblock(blocks[curblock++]);
                            if (_bu == null)
                            {
                                return null;
                            }
                            //memcpy(_buf, _bu + curoffblock, curlen);
                            Array.Copy(_bu, curoffblock, buf, destIndex, curlen);
                            //_buf += curlen;
                            destIndex += curlen;
                            _length -= curlen;
                            curoffblock = 0;
                            //curlen = std::min(static_cast<uint64_t>(base->pagesize), _length);
                            curlen = Math.Min((UInt32)(_base.pagesize - curoffblock), (UInt32)_length);
                        }

                    }

                }
                else if (type == v8objtype.free838)
                {
                    // TODO: реализовать V8Object::getdata для файла свободных страниц формата 8.3.8
                }
            }
            return buf;
        }

        /// <summary>
        /// запись кусочка объекта, поддерживает кеширование блоков.
        /// </summary>
        /// <param name="buf"></param>
        /// <param name="_start"></param>
        /// <param name="_length"></param>
        /// <returns></returns>
        public bool setdata(byte[] buf, UInt64 _start, UInt64 _length) 
        {
            UInt32 curblock;
            UInt32 curoffblock;
            //char* _buf;
            byte[] _buf;
            UInt32 curlen;

            UInt32 curobjblock;
            UInt32 curoffobjblock;
            UInt32 offsperpage;
            UInt32 destIndex = 0;

            if (_base.ReadOnly)
            {
                Console.WriteLine($"Попытка записи в файл в режиме \"Только чтение\". Номер страницы файла {block}");
                return false;
            }

            if (type == v8objtype.free80 || type == v8objtype.free838)
            {
                Console.WriteLine($"Попытка прямой записи в файл свободных страниц. Номер страницы файла {block}");
                return false;
            }

            //lastdataget = GetTickCount();

            
            data = null;
            if (_start + _length > len)
            {
                set_len(_start + _length);
            }

            if (type == v8objtype.data80)
            {
                curblock = (UInt32)_start >> 12;

                _buf = buf;

                curoffblock = (UInt32)_start - (curblock << 12);

                //curlen = std::min(static_cast<uint64_t>(DEFAULT_PAGE_SIZE - curoffblock), _length);
                curlen = (UInt32)Math.Min((UInt64)(PAGE4K - curoffblock), _length);

                curobjblock = curblock / 1023;
                curoffobjblock = curblock - curobjblock * 1023;

                //objtab* b = (objtab*)base->getblock(blocks[curobjblock++]);
                objtab b = ByteArrayToObjtab(_base.getblock(blocks[curobjblock++]));
                while (_length != 0)
                {
                    //memcpy((char*)(base->getblock_for_write(b->blocks[curoffobjblock++], curlen != DEFAULT_PAGE_SIZE)) + curoffblock, _buf, curlen);
                    Array.Copy(_buf, destIndex, _base.getblock_for_write(b.blocks[curoffobjblock++], curlen != PAGE4K), curoffblock, curlen);

                    //_buf += curlen; TODO : Надо что-то с этим делать
                    destIndex += curlen;
                    _length -= curlen;
                    curoffblock = 0;

                    //curlen = std::min(static_cast<uint64_t>(DEFAULT_PAGE_SIZE), _length);
                    curlen = (UInt32)Math.Min((UInt64)PAGE4K, _length);

                    if (_length > 0)
                    {
                        if (curoffobjblock >= 1023)
                        {
                            curoffobjblock = 0;
                            //b = (objtab*)base->getblock(blocks[curobjblock++]);
                            b = ByteArrayToObjtab(_base.getblock(blocks[curobjblock++]));
                        }
                    }
                }

                write_new_version();
                return true;
            }
            else if (type == v8objtype.data838)
            {
                curblock = (UInt32)_start / _base.pagesize;

                //_buf = (char*)buf;
                _buf = buf;

                curoffblock = (UInt32)_start - (curblock * _base.pagesize);

                //curlen = std::min(static_cast<uint64_t>(base->pagesize - curoffblock), _length);
                curlen = (UInt32)Math.Min((UInt64)_base.pagesize-curoffblock, _length);

                if (fatlevel != 0)
                {
                    offsperpage = _base.pagesize / 4;
                    curobjblock = curblock / offsperpage;
                    curoffobjblock = curblock - curobjblock * offsperpage;

                    //objtab838* bb = (objtab838*)base->getblock(blocks[curobjblock++]);
                    objtab838 bb = ByteArrayToObjtab838(_base.getblock(blocks[curobjblock++]));
                    destIndex = 0;
                    while (_length != 0)
                    {
                        //memcpy((char*)(base->getblock_for_write(bb->blocks[curoffobjblock++], curlen != base->pagesize)) + curoffblock, _buf, curlen);

                        Array.Copy(_buf, destIndex, _base.getblock_for_write(bb.blocks[curoffobjblock++], curlen != _base.pagesize), curoffblock, curlen);
                        // _buf += curlen; TODO : Надо что-то с этим делать
                        destIndex += curlen;
                        _length -= curlen;
                        curoffblock = 0;

                        //curlen = std::min(static_cast<uint64_t>(base->pagesize), _length);
                        curlen = (UInt32)Math.Min((UInt64)_base.pagesize, _length);
                        if (_length > 0)
                        {
                            if (curoffobjblock >= offsperpage)
                            {
                                curoffobjblock = 0;
                                //bb = (objtab838*)base->getblock(blocks[curobjblock++]);
                                bb = ByteArrayToObjtab838(_base.getblock(blocks[curobjblock++]));
                            }
                        }
                    }
                }
                else
                {
                    destIndex = 0;
                    while (_length != 0)
                    {
                        //memcpy((char*)(base->getblock_for_write(blocks[curblock++], curlen != base->pagesize)) + curoffblock, _buf, curlen);
                        Array.Copy(_buf, destIndex, _base.getblock_for_write(blocks[curblock++], curlen != _base.pagesize), curoffblock, curlen);
                        //_buf += curlen;
                        destIndex += curlen;
                        _length -= curlen;
                        curoffblock = 0;
                        //curlen = std::min(static_cast<uint64_t>(base->pagesize), _length);
                        curlen = (UInt32)Math.Min((UInt64)_base.pagesize, _length);
                    }
                }

                write_new_version();
                return true;
            }

            return false;
        }

        /// <summary>
        /// запись объекта целиком, поддерживает кеширование блоков.
        /// </summary>
        /// <param name="buf"></param>
        /// <param name="_length"></param>
        /// <returns></returns>
        public bool setdata(byte[] buf, UInt64 _length) 
        {
            byte[] _buf;
            
            UInt32 curlen = 0;
            UInt32 offsperpage;
            UInt32 curblock;
            UInt32 curobjblock;
            UInt32 curoffobjblock;

            UInt32 srcIndex = 0;

            if (_base.ReadOnly)
            {
                Console.WriteLine($"Попытка записи в файл в режиме \"Только чтение\". Номер страницы файла {block}");
                return false;
            }

            if (type == v8objtype.free80 || type == v8objtype.free838)
            {
                Console.WriteLine($"Попытка прямой записи в файл свободных страниц. Номер страницы файла {block}");
                return false;
            }

            data = null;
            set_len(_length);

            _buf = buf;

            if (type == v8objtype.data80)
            {
                for (UInt32 i = 0; i < numblocks; i++)
                {
                    //objtab* b = (objtab*)base->getblock(blocks[i]);
                    objtab b = ByteArrayToObjtab(_base.getblock(blocks[i]));

                    for (UInt32 j = 0; j < b.numblocks; j++)
                    {

                        //curlen = std::min(static_cast<uint64_t>(DEFAULT_PAGE_SIZE), _length);
                        curlen = (UInt32)Math.Min((UInt64)PAGE4K, _length);

                        //char* tt = base->getblock_for_write(b->blocks[j], false);
                        byte[] tt = _base.getblock_for_write(b.blocks[j], false);

                        //memcpy(tt, buf, curlen);
                        Array.Copy(_buf, srcIndex, tt, 0, curlen);

                        srcIndex += (UInt32)PAGE4K;

                        if (_length <= curlen)
                        {
                            break;
                        }

                        _length -= curlen;

                    }
                    if (_length <= curlen)
                    {
                        break;
                    }
                }

                write_new_version();
                return true;
            }
            else if (type == v8objtype.data838)
            {
                curblock = 0;
                srcIndex = 0;
                //curlen = std::min(static_cast<uint64_t>(base->pagesize), _length);
                curlen = (UInt32)Math.Min((UInt64)_base.pagesize, _length);

                if (fatlevel != 0)
                {
                    offsperpage = _base.pagesize / 4;
                    curobjblock = 0;
                    curoffobjblock = 0;

                    //objtab838* bb = (objtab838*)base->getblock(blocks[curobjblock++]);
                    objtab838 bb = ByteArrayToObjtab838(_base.getblock(blocks[curobjblock++]));

                    while (_length != 0)
                    {
                        //memcpy((char*)(base->getblock_for_write(bb->blocks[curoffobjblock++], false)), buf, curlen);
                        Array.Copy(_buf, srcIndex, _base.getblock_for_write(bb.blocks[curoffobjblock++], false), 0, curlen);
                        srcIndex += curlen;
                        //buf += curlen;
                        _length -= curlen;
                        //curlen = std::min(static_cast<uint64_t>(base->pagesize), _length);
                        curlen = (UInt32)Math.Min((UInt64)_base.pagesize, _length);
                        if (_length > 0)
                        {
                            if (curoffobjblock >= offsperpage)
                            {
                                curoffobjblock = 0;
                                //bb = (objtab838*)base->getblock(blocks[curobjblock++]);
                                bb = ByteArrayToObjtab838(_base.getblock(blocks[curobjblock++]));
                            }
                        }
                    }
                }
                else
                {
                    srcIndex = 0;
                    while (_length != 0)
                    {
                        //memcpy((char*)(base->getblock_for_write(blocks[curblock++], false)), buf, curlen);
                        Array.Copy(_buf, srcIndex, _base.getblock_for_write(blocks[curblock++], false), 0, curlen);
                        srcIndex += curlen;
                        _length -= curlen;
                        //curlen = std::min(static_cast<uint64_t>(base->pagesize), _length);
                        curlen = (UInt32)Math.Min((UInt64)_base.pagesize, _length);
                    }
                }

                write_new_version();
                return true;
            }

            return false;
        }

        /// <summary>
        /// записывает поток целиком в объект, поддерживает кеширование блоков.
        /// </summary>
        /// <param name="stream"></param>
        /// <returns></returns>
        public bool setdata(Stream stream) 
        {
            
            UInt32 curlen = 0;
            UInt32 offsperpage;
            UInt32 curblock;
            UInt32 curobjblock;
            UInt32 curoffobjblock;

            UInt64 _length;

            if (_base.ReadOnly)
            {
                Console.WriteLine($"Попытка записи в файл в режиме \"Только чтение\". Номер страницы файла {block}");
                return false;
            }

            if (type == v8objtype.free80 || type == v8objtype.free838)
            {
                Console.WriteLine($"Попытка прямой записи в файл свободных страниц. Номер страницы файла {block}");
                return false;
            }

            data = null;

            //_length = stream->GetSize();
            _length = (UInt64)stream.Length;

            set_len(_length);

            //stream->Seek(0, soFromBeginning);
            stream.Seek(0, SeekOrigin.Begin);

            if (type == v8objtype.data80)
            {
                for (UInt32 i = 0; i < numblocks; i++)
                {
                    objtab b = ByteArrayToObjtab(_base.getblock(blocks[i]));

                    for (UInt32 j = 0; j < b.numblocks; j++)
                    {
                        curlen = (UInt32)Math.Min((UInt64)PAGE4K, _length);

                        //char* tt = base->getblock_for_write(b->blocks[j], false);
                        byte[] tt = _base.getblock_for_write(b.blocks[j], false);
                        stream.Read(tt, 0, (Int32)curlen);
                        if (_length <= curlen)
                        {
                            break;
                        }
                        _length -= curlen;
                    }
                    if (_length <= (UInt64)curlen)
                    {
                        break;
                    }
                }

                write_new_version();
                return true;
            }
            else if (type == v8objtype.data838)
            {
                curblock = 0;

                curlen = (UInt32)Math.Min((UInt64)_base.pagesize, _length);

                if (fatlevel != 0)
                {
                    offsperpage = _base.pagesize / 4;
                    curobjblock = 0;
                    curoffobjblock = 0;

                    objtab838 bb = ByteArrayToObjtab838(_base.getblock(blocks[curobjblock++]));

                    while (_length != 0)
                    {
                        stream.Read(_base.getblock_for_write(bb.blocks[curoffobjblock++], false), 0, (Int32)curlen);

                        _length -= curlen;
                        curlen = (UInt32)Math.Min((UInt64)_base.pagesize, _length);
                        if (_length > 0)
                        {
                            if (curoffobjblock >= offsperpage)
                            {
                                curoffobjblock = 0;
                                bb = ByteArrayToObjtab838(_base.getblock(blocks[curobjblock++]));
                            }
                        }
                    }
                }
                else
                {
                    while (_length != 0)
                    {
                        stream.Read(_base.getblock_for_write(blocks[curblock++], false), 0, (Int32)curlen);
                        _length -= curlen;
                        curlen = (UInt32)Math.Min((UInt64)_base.pagesize, _length);
                    }
                }

                write_new_version();
                return true;
            }

            return false;
        }

        /// <summary>
        /// запись части потока в объект, поддерживает кеширование блоков.
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="_start"></param>
        /// <param name="_length"></param>
        /// <returns></returns>
        public bool setdata(Stream stream, UInt64 _start, UInt64 _length) 
        {
            UInt32 curblock;
            UInt32 curoffblock;
            UInt32 curlen;
            
            UInt32 curobjblock;
            UInt32 curoffobjblock;
            UInt32 offsperpage;

            if (_base.ReadOnly)
            {
                Console.WriteLine($"Попытка записи в файл в режиме \"Только чтение\". Номер страницы файла {block}");
                return false;
            }

            if (type == v8objtype.free80 || type == v8objtype.free838)
            {
                Console.WriteLine($"Попытка прямой записи в файл свободных страниц. Номер страницы файла {block}");
                return false;
            }

            

            
            data = null;

            if (_start + _length > len)
                set_len(_start + _length);

            if (type == v8objtype.data80)
            {
                curblock    = (UInt32)_start >> 12;
                curoffblock = (UInt32)_start - (curblock << 12);
                curlen      = (UInt32)Math.Min( (UInt64)(PAGE4K - curoffblock), _length);

                curobjblock = curblock / 1023;
                curoffobjblock = curblock - curobjblock * 1023;

                //objtab* b = (objtab*)base->getblock(blocks[curobjblock++]);
                objtab b = ByteArrayToObjtab(_base.getblock(blocks[curobjblock++]));

                while (_length != 0)
                {
                    stream.Read(_base.getblock_for_write(b.blocks[curoffobjblock++], curlen != PAGE4K), (Int32)curoffblock, (Int32)curlen);
                    _length -= curlen;
                    curoffblock = 0;
                    curlen = (UInt32)Math.Min((UInt64)PAGE4K, _length);
                    if (_length > 0)
                    {
                        if (curoffobjblock >= 1023)
                        {
                            curoffobjblock = 0;
                            b = ByteArrayToObjtab(_base.getblock(blocks[curobjblock++]));
                        }
                    }
                }

                write_new_version();
                return true;
            }
            else if (type == v8objtype.data838)
            {
                curblock = (UInt32)_start / _base.pagesize;
                curoffblock = (UInt32)_start - (curblock * _base.pagesize);
                curlen = (UInt32)Math.Min((UInt64)(_base.pagesize - curoffblock), _length);

                if (fatlevel != 0)
                {
                    offsperpage = _base.pagesize / 4;
                    curobjblock = curblock / offsperpage;
                    curoffobjblock = curblock - curobjblock * offsperpage;

                    objtab838 bb = ByteArrayToObjtab838(_base.getblock(blocks[curobjblock++]));

                    while (_length != 0)
                    {
                        stream.Read(_base.getblock_for_write(bb.blocks[curoffobjblock++], curlen != _base.pagesize), (Int32)curoffblock, (Int32)curlen);
                        _length -= curlen;
                        curoffblock = 0;
                        curlen = (UInt32)Math.Min((UInt64)(_base.pagesize), _length);
                        if (_length > 0)
                        {
                            if (curoffobjblock >= offsperpage)
                            {
                                curoffobjblock = 0;
                                bb = ByteArrayToObjtab838(_base.getblock(blocks[curobjblock++]));
                            }
                        }
                    }
                }
                else
                {
                    while (_length != 0)
                    {
                        stream.Read(_base.getblock_for_write(blocks[curblock++], curlen != _base.pagesize), (Int32)curoffblock, (Int32)curlen);
                        _length -= curlen;
                        curoffblock = 0;
                        curlen = (UInt32)Math.Min((UInt64)(_base.pagesize), _length);
                    }
                }

                write_new_version();
                return true;
            }

            return false;

        }

        /// <summary>
        /// Возвращает длину объекта
        /// </summary>
        /// <returns></returns>
        public UInt64 getlen()
        {
            return (type == v8objtype.free80) ? (len * 4) : len;
        }

        /// <summary>
        /// Сохранение в файл
        /// </summary>
        /// <param name="_filename"></param>
        public void SaveToFile(String _filename)
        {
            UInt64 pagesize = _base.pagesize;
            FileStream fs = new FileStream(_filename, FileMode.CreateNew);

            //char* buf = new char[pagesize];
            byte[] buf = new byte[pagesize];

            UInt64 total_size = getlen();
            UInt64 remain_size = total_size;
            for (UInt64 offset = 0; offset < total_size; offset += pagesize)
            {
                UInt32 size_of_block = Math.Min((UInt32)remain_size, (UInt32)pagesize);

                getdata(buf, offset, size_of_block);

                fs.Write(buf, 0, (Int32)size_of_block);
                remain_size -= pagesize;
            }
            buf = null;
        }

        #region Не используемые пока
        public void set_lockinmemory(bool _lock)
        {
        }

        public static void garbage()
        {
        }
        #endregion

        /// <summary>
        /// получить физическое смещение в файле по смещению в объекте
        /// </summary>
        /// <param name="offset"></param>
        /// <returns></returns>
        public UInt64 get_fileoffset(UInt64 offset) 
        {
            UInt32 _start = (UInt32)offset;
            objtab b;
            objtab838 bb;
            UInt32 curblock;
            UInt32 curoffblock;
            UInt32 curobjblock;
            UInt32 curoffobjblock;
            UInt32 offsperpage;

            if (type == v8objtype.free80)
            {
                curblock = _start >> 12;
                curoffblock = _start - (curblock << 12);
                return (((UInt64)(blocks[curblock])) << 12) + curoffblock;
            }
            else if (type == v8objtype.data80)
            {
                curblock = _start >> 12;
                curoffblock = _start - (curblock << 12);

                curobjblock = curblock / 1023;
                curoffobjblock = curblock - curobjblock * 1023;

                //b = (objtab*)base->getblock(blocks[curobjblock]);
                b = ByteArrayToObjtab(_base.getblock(blocks[curobjblock]));

                return (((UInt64)(b.blocks[curoffobjblock])) << 12) + curoffblock;
            }
            else if (type == v8objtype.data838)
            {
                curblock = _start / _base.pagesize;
                curoffblock = _start - (curblock * _base.pagesize);
                if (fatlevel != 0)
                {
                    offsperpage = _base.pagesize / 4;
                    curobjblock = curblock / offsperpage;
                    curoffobjblock = curblock - curobjblock * offsperpage;
                    //bb = (objtab838*)base->getblock(blocks[curobjblock]);
                    bb = ByteArrayToObjtab838(_base.getblock(blocks[curobjblock]));
                    return (((UInt64)(bb.blocks[curoffobjblock])) * _base.pagesize) + curoffblock;
                }
                else
                {
                    return (((UInt64)(blocks[curblock])) * _base.pagesize) + curoffblock;
                }
            }

            else if (type == v8objtype.free838)
            {
                // TODO: реализовать v8object::get_fileoffset для файла свободных страниц формата 8.3.8
                return 0;
            }

            return 0;
        }

        /// <summary>
        /// пометить блок как свободный
        /// </summary>
        /// <param name="block_number"></param>
        public void set_block_as_free(UInt32 block_number) 
        {
            if (block != 1)
            {
                // Таблица свободных блоков
                Console.WriteLine($"Попытка установки свободного блока в объекте, не являющимся таблицей свободных блоков. Блок объекта {block}");
                return;
            }

            Int32 j = (Int32)len >> 10;   // length / 1024
            Int32 i = (Int32)len & 0x3ff; // length % 1024

            //v8ob ob = (v8ob*)base->getblock_for_write(block, true);
            v8ob ob = ByteArrayToV8ob(_base.getblock_for_write(block, true));

            if (real_numblocks > j)
            {
                len++;
                ob.len = (UInt32)len;
                //uint32_t* b = (uint32_t*)base->getblock_for_write(blocks[j], true);
                byte[] b = _base.getblock_for_write(blocks[j], true);
                b[i] = (byte)block_number;
                if (numblocks <= (UInt32)j)
                    numblocks = (UInt32)j + 1;
            }
            else
            {
                ob.blocks[real_numblocks] = block_number;
                blocks = null;
                real_numblocks++;
                blocks = new UInt32[real_numblocks];
                //memcpy(blocks, ob->blocks, real_numblocks * 4);
                Array.Copy(ob.blocks, blocks, real_numblocks * 4);
            }

        }

        /// <summary>
        /// получить номер свободного блока (и пометить как занятый)
        /// </summary>
        /// <returns></returns>
        public UInt32 get_free_block() 
        {
            if (block != 1)
            {
                // Таблица свободных блоков
                Console.WriteLine($"Попытка получения свободного блока в объекте, не являющимся таблицей свободных блоков. Блок объекта {block}");
                return 0;
            }

            if (len != 0)
            {
                len--;
                UInt32 j = (UInt32)len >> 10;    // length / 1024
                UInt32 i = (UInt32)len & 0x3ff;  // length % 1024
                byte[] b = _base.getblock_for_write(blocks[j], true);

                UInt32 k = b[i];
                b[i] = 0;

                //v8ob* ob = (v8ob*)base->getblock_for_write(block, true);
                v8ob ob = ByteArrayToV8ob(_base.getblock_for_write(block, true));

                ob.len = (UInt32)len;

                return k;
            }
            else
            {
                //unsigned i = MemBlock::get_numblocks();
                UInt32 i = 10000000; // TODO: надо доработать
                _base.getblock_for_write(i, false);

                return i;
            }

        }

        /// <summary>
        /// получает версию очередной записи и увеличивает сохраненную версию объекта
        /// </summary>
        /// <param name="ver"></param>
        public void get_version_rec_and_increase(_version ver) 
        {
            ver.version_1 = version_rec.version_1;
            ver.version_2 = version_rec.version_2;

            version_rec.version_2++;
        }

        /// <summary>
        /// получает сохраненную версию объекта
        /// </summary>
        /// <param name="ver"></param>
        public void get_version(_version ver) 
        {
            ver.version_1 = version.version_1;
            ver.version_2 = version.version_2;
        }

        /// <summary>
        /// записывает новую версию объекта
        /// </summary>
        public void write_new_version() 
        {
            _version new_ver;
            if (new_version_recorded) return;
            Int32 veroffset = type == v8objtype.data80 || type == v8objtype.free80 ? 12 : 4;

            new_ver.version_1 = version.version_1 + 1;
            new_ver.version_2 = version.version_2;
            //memcpy(base->getblock_for_write(block, true) + veroffset, &new_ver, 8);
            // TODO: надо доработать
            //Array.Copy();
            new_version_recorded = true;
        }

        /// <summary>
        /// Возвращает "первый" объект
        /// </summary>
        /// <returns></returns>
        public static V8Object get_first()
        {
            return first;
        }

        /// <summary>
        /// Возвращает "последний" объект
        /// </summary>
        /// <returns></returns>
        public static V8Object get_last()
        {
            return last;
        }

        /// <summary>
        /// Возвращает "следующий" объект
        /// </summary>
        /// <returns></returns>
        public V8Object get_next()
        {
            return next;
        }

        /// <summary>
        /// Возвращает номер блока
        /// </summary>
        /// <returns></returns>
        public UInt32 get_block_number()
        {
            return block;
        }

        /// <summary>
        /// rewrite - перезаписывать поток _str. Истина - перезаписывать (по умолчанию), Ложь - дописывать
        /// </summary>
        /// <param name="_str"></param>
        /// <param name="_startblock"></param>
        /// <param name="_length"></param>
        /// <param name="rewrite"></param>
        /// <returns></returns>
        public Stream readBlob(Stream _str, UInt32 _startblock, UInt32 _length = UInt32.MaxValue, bool rewrite = true)
        {

            UInt32 _curblock = 0;
            byte[] _curb;

            UInt32 _numblock = 0;
            UInt32 startlen = 0;

            if (rewrite)
                _str.SetLength(0);

            startlen = (UInt32)_str.Position;
            if (_startblock == 0 && _length != 0)
            {
                Console.WriteLine($"Попытка чтения нулевого блока файла Blob. Номер страницы файла {block}");
                return _str;
            }

            _numblock = (UInt32)len >> 8;
            if (_numblock << 8 != len)
            {
                Console.WriteLine($"Длина файла Blob не кратна 0x100. Номер страницы файла {block}. Длина файла {len}");
            }

            _curb = new byte[0x100];
            _curblock = _startblock;

            while (_curblock != 0)
            {
                if (_curblock >= _numblock)
                {
                    Console.WriteLine($"Попытка чтения блока файла Blob за пределами файла. Номер страницы файла {block}. Всего блоков {_numblock}. Читаемый блок {_curblock}");
                    return _str;
                }
                getdata(_curb, _curblock << 8, 0x100);

                _curblock = (UInt32)_curb[0];

                UInt16 _curlen = _curb[4];

                if (_curlen > 0xfa)
                {
                    Console.WriteLine($"Попытка чтения из блока файла Blob более 0xfa байт. Номер страницы файла {block}. Индекс блока {_curblock}. Читаемый байт {_curlen}");
                    return _str;
                }
                _str.Write(_curb, 6, _curlen);

                if (_str.Length - startlen > _length)
                    break; // аварийный выход из возможного ошибочного зацикливания

            }

            _curb = null;

            if (_length != UInt32.MaxValue)
                if (_str.Length - startlen != _length)
                {
                    Console.WriteLine($"Несовпадение длины Blob-поля, указанного в записи, с длиной практически прочитанных данных. Номер страницы файла {block}. Длина поля {_length}. Прочитано {_str.Length - startlen}");
                }

            return _str;
        }

    }
}
