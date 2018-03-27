using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace Read1CD
{
    public class v8File
    {
        public enum FileIsCatalog { unknown, yes, no }

        /// <summary>
        /// Вложенный класс...
        /// </summary>
        public class TV8FileStream : MemoryStream
        {
            #region public
            /// <summary>
            /// Основной конструктор
            /// </summary>
            /// <param name="f"></param>
            /// <param name="ownfile"></param>
            public TV8FileStream(v8File f, bool ownfile = false)
            {
                pos = 0;
                f.streams.Add(this);
            }

            public virtual Int64 Read(byte[] Buffer, Int64 Count)
            {
                int r = (int)file.Read(Buffer, (int)pos, (int)Count);
                pos += r;
                return r;
            }

            public override int Read(byte[] Buffer, int Offset, int Count)
            {
                // int r = (int)file.Read(Buffer, (int)Offset, (int)Count); - возможно надо так ????
                int r = (int)file.Read(Buffer, (int)pos, (int)Count);
                pos += r;
                return r;
            }

            /*
            public virtual Int64 Write(byte[] Buffer, Int64 Count)
            {
                int r = (int)file.Write(Buffer, (int)pos, (int)Count);
                pos += r;
                return r;
            }


            public override void Write(byte[] Buffer, int Offset, int Count)
            {
                // int r = (int)file.Write(Buffer, (int)Offset, (int)Count); - Возможно должно быть так ????
                int r = (int)file.Write(Buffer, (int)pos, (int)Count);
                pos += r;
            }
            */
            public override Int64 Seek(Int64 Offset, SeekOrigin Origin)
            {
                Int64 len = file.GetFileLength();
                switch (Origin)
                {
                    case SeekOrigin.Begin:
                        if (Offset >= 0)
                        {
                            if (Offset <= len)
                            {
                                pos = Offset;
                            }
                            else
                            {
                                pos = len;
                            }
                        }
                        break;
                    case SeekOrigin.Current:
                        if (pos + Offset < len)
                        {
                            pos += Offset;
                        }
                        else
                        {
                            pos = len;
                        }
                        break;
                    case SeekOrigin.End:
                        if (Offset <= 0)
                        {
                            if (Offset <= len)
                            {
                                pos = len - Offset;
                            }
                            else
                            {
                                pos = 0;
                            }
                        }
                        break;
                }
                return pos;
            }
            #endregion

            #region protected

            protected v8File file;
            protected bool own;
            protected Int64 pos;
            //protected int pos;

            #endregion

        }


        /// <summary>
        /// Конструктор
        /// </summary>
        public v8File()
        {

        }

        public String name;
        public MemoryStream data;
        public FileIsCatalog iscatalog;

        public bool is_opened;   // признак открытого файла (инициализирован поток data)

        public bool selfzipped;  // Признак, что файл является запакованным независимо от признака zipped каталога

        public int start_data;   // начало блока данных файла в каталоге (0 означает, что файл в каталоге не записан)
        public int start_header; // начало блока заголовка файла в каталоге
        
        private SortedSet<TV8FileStream> streams;



        //public v8catalog parent; // не очень понятно пока для чего нужен



        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////

        public Int64 GetFileLength()
        {
            Int64 ret = 0;

            if (!try_open())
            {
                return ret;
            }

            ret = data.Length;

            return ret;

        }


        public bool try_open()
        {
            //return (is_opened ? true : Open());
            return true;
        }

        public Int64 Read(byte[] Buffer, int Start, int Length)
        {

            Int64 ret = 0;

            if (!try_open())
            {
                return ret;
            }

            data.Seek(Start, SeekOrigin.Begin);
            data.Read(Buffer, Start, Length);

            return ret;

        }

    }
}
