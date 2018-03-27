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

    public class FieldType
    {
        public virtual type_fields gettype()
        {
            return type_fields.tf_binary;
        }

        public virtual Int32 getlength()
        {
            return 0;
        }

        public virtual Int32 getlen()
        {
            return 0;
        }

        public virtual Int32 getprecision()
        {
            return 0;
        }

        public virtual bool getcase_sensitive()
        {
            return true;
        }

        public virtual String get_presentation_type()
        {
            return " ";
        }

        public virtual String get_presentation(char[] rec, bool EmptyNull, char Delimiter, bool ignore_showGUID, bool detailed)
        {
            return " ";
        }

        public virtual bool get_binary_value(byte[] buf, String value)
        {
            return true;
        }

        public virtual String get_XML_presentation(char[] rec, v8Table parent, bool ignore_showGUID)
        {
            return " ";
        }

        public virtual UInt32 getSortKey(byte[] rec, byte[] SortKey, Int32 maxlen)
        {
            return 0;
        }
        
        public static FieldType create_type_manager(field_type_declaration type_declaration)
        {
            switch (type_declaration.type)
            {

                case type_fields.tf_binary:
                case type_fields.tf_varbinary:
                    return new BinaryFieldType(type_declaration);

                case type_fields.tf_numeric:
                    return new NumericFieldType(type_declaration);

                case type_fields.tf_datetime:
                    return new DatetimeFieldType(type_declaration);
            }
            return new CommonFieldType(type_declaration);
        }

        public static FieldType Version8()
        {
            field_type_declaration td;
            td.type = type_fields.tf_version8;
            td.case_sensitive = false;
            td.length = 0;
            td.null_exists = false;
            td.precision = 0;
            return new CommonFieldType(td);
        }

        // TODO: убрать это куда-нибудь
        public static bool showGUIDasMS; // Признак, что GUID надо преобразовывать по стилю MS (иначе по стилю 1С)
        public static bool showGUID;

    }

    public class CommonFieldType : FieldType
    {
        public type_fields type = type_fields.tf_binary;
        public int length = 0;
        public int precision = 0;
        public bool case_sensitive = false;

        public int len = 0;

        public CommonFieldType(field_type_declaration declaration) 
        {

            type           = declaration.type;
            length         = declaration.length;
            precision      = declaration.precision;
            case_sensitive = declaration.case_sensitive;
            
        }

        public override type_fields gettype()
        {
            return type;
        }

        public override int getlength()
        {
            return length;
        }

        public override int getprecision()
        {
            return precision;
        }

        public override bool getcase_sensitive()
        {
            return case_sensitive;
        }

        public override String get_presentation_type()
        {
            switch (type)
            {
                case type_fields.tf_binary:    return "binary";
                case type_fields.tf_bool:      return "bool";
                case type_fields.tf_numeric:   return "number";
                case type_fields.tf_char:      return "fixed string";
                case type_fields.tf_varchar:   return "string";
                case type_fields.tf_version:   return "version";
                case type_fields.tf_string:    return "memo";
                case type_fields.tf_text:      return "text";
                case type_fields.tf_image:     return "image";
                case type_fields.tf_datetime:  return "datetime";
                case type_fields.tf_version8:  return "hidden version";
                case type_fields.tf_varbinary: return "var binary";
            }
            return "{?}";
        }
        
        public override int getlen()
        {

            if (len != 0) return len;

            switch (type)
            {
                case type_fields.tf_binary:    len += length;            break;
                case type_fields.tf_bool:      len += 1;                 break;
                case type_fields.tf_numeric:   len += (length + 2) >> 1; break;
                case type_fields.tf_char:      len += length * 2;        break;
                case type_fields.tf_varchar:   len += length * 2 + 2;    break;
                case type_fields.tf_version:   len += 16;                break;
                case type_fields.tf_string:    len += 8;                 break;
                case type_fields.tf_text:      len += 8;                 break;
                case type_fields.tf_image:     len += 8;                 break;
                case type_fields.tf_datetime:  len += 7;                 break;
                case type_fields.tf_version8:  len += 8;                 break;
                case type_fields.tf_varbinary: len += length + 2;        break;
            }
            return len;
        }
        
        public override String get_presentation(char[] rec, bool EmptyNull, Char Delimiter, bool ignore_showGUID, bool detailed)
        {
            return "";
        }
                
        public virtual String get_fast_presentation(char[] rec)
        {
            return get_presentation(rec, false, '0', false, false); 
        }
                
        public override bool get_binary_value(byte[] buf, String value)
        {
            return true;
        }
        
        public override String get_XML_presentation(char[] rec, v8Table parent, bool ignore_showGUID)
        {
            Int32 i = 0;

            //MemoryStream in;
            //MemoryStream out;
            String s;

            char[] fr = rec;

            switch (type)
            {

                case type_fields.tf_bool:
                    
                        if (fr[0] != '0')
                            return "true";
                        return
                            "false";
                    
                case type_fields.tf_char:
                    
                    return toXML(fr.ToString());

                case type_fields.tf_varchar:
                    {
                        //i = *(int16_t*)fr;
                        //return toXML(String(((WCHART*)fr) + 1, i));
                        return toXML(fr.ToString());
                    }

                case type_fields.tf_version:
                    {
                        //int32_t* retyped = (int32_t*)fr;
                        //return String(*(int32_t*)fr) + ":" + retyped[1] + ":" + retyped[2] + ":" + retyped[3];
                        return toXML(fr.ToString());
                    }
                case type_fields.tf_version8:
                    {

                        //int32_t* retyped = (int32_t*)fr;
                        //return String(retyped[0] + ":" + retyped[1]);
                        return toXML(fr.ToString());
                    }
                case type_fields.tf_string:
                    {
                        /*
                        uint32_t* retyped = (uint32_t*)fr;
			            out = new MemoryStream();
                        parent.readBlob(out, retyped[0], retyped[1]);
                        s = toXML(String((WCHART*)(out->GetMemory()), out->GetSize() / 2));
                        delete out;
                        return s;
                        */
                        return toXML(fr.ToString());
                    }
                case type_fields.tf_text:
                    {
                        /*
                        uint32_t* retyped = (uint32_t*)fr;
			            out = new MemoryStream();
                        parent.readBlob(out, retyped[0], retyped[1]);
                        s = toXML(String((char*)(out->GetMemory()), out->GetSize()));
                        delete out;
                        return s;
                        */
                        return toXML(fr.ToString());
                    }
                case type_fields.tf_image:
                    {
                        /*
                        uint32_t* retyped = (uint32_t*)fr;
			            in = new MemoryStream();
			            out = new MemoryStream();
                        parent->readBlob(in, retyped[0], retyped[1]);
                        base64_encode(in, out, 72);
                        s = String((WCHART*)(out->GetMemory()), out->GetSize() / 2);
                        delete in;
                        delete out;
                        return s;
                        */
                        return toXML(fr.ToString());
                    }
            }

            return "{?}";
        }
        
        public override uint getSortKey(byte[] rec, byte[] SortKey, int maxlen)
        {
            UInt32 addlen = 0;

            //unsigned char* fr = (unsigned char*)rec;
            char[] fr = System.Text.Encoding.UTF8.GetString(rec).ToCharArray();
            byte[] fr_byte = new byte[rec.Length];
            Array.Copy(rec, fr_byte, rec.Length);


            if (maxlen == 0)
            {
                throw new Exception($"Ошибка получения ключа сортировки поля. Нулевая длина буфера. Значение поля {get_fast_presentation(fr)}");
            }

            switch (type)
            {
                case type_fields.tf_bool:
                    if (len > maxlen)
                    {
                        throw new Exception($"Ошибка получения ключа сортировки поля. Длина буфера меньше необходимой. Значение поля. Значение поля {get_fast_presentation(fr)}. Длина буфера {maxlen}. Необходимая длина буфера {len}");
                    }
                    //memcpy(SortKey, (void*)fr, len - addlen);
                    Array.Copy(fr_byte, SortKey, len - addlen);
                    return (UInt32)len;

                case type_fields.tf_char:
                    throw new Exception($"Ошибка получения ключа сортировки поля. Неизвестный код возврата. Значение поля {get_fast_presentation(fr)}");

                case type_fields.tf_varchar:
                    throw new Exception($"Ошибка получения ключа сортировки поля. Неизвестный код возврата. Значение поля {get_fast_presentation(fr)}");
            }

            throw new Exception($"Попытка получения ключа сортировки неподдерживаемого типа поля. Значение поля {get_fast_presentation(fr)}");
        }

    }

    public class BinaryFieldType : CommonFieldType
    {
        public BinaryFieldType(field_type_declaration declaration) : base(declaration)
        {

        }

        public override String get_presentation(char[] rec, bool EmptyNull, char Delimiter, bool ignore_showGUID, bool detailed)
        {
            char sym;
            Int32 i, m;

            //unsigned char* fr = (unsigned char*)rec;
            char[] fr = new char[rec.Length];
            Array.Copy(rec, fr, rec.Length);

            //char* buf = new char[(length + 1) * 2]; // TODO: адовый костыль с утечкой памяти
            char[] buf = new char[(length + 1) * 2];

            switch (type)
            {
                case type_fields.tf_binary:
                    if (length == 16 && (showGUID || ignore_showGUID))
                    {
                        if (showGUIDasMS)
                            return GUIDasMS(fr);
                        else
                            return GUIDas1C(fr);
                    }
                    else
                    {
                        for (i = 0; i < length; i++)
                        {
                            sym = (Char)((UInt32)'0' + ((UInt32)fr[i] >> 4));
                            if (sym > '9') sym += (Char)((UInt32)'a' - (UInt32)'9' - 1);
                            buf[ i << 1 ] = sym;
                            sym = (Char)((UInt32)'0' + ((UInt32)fr[i] & 0xf));
                            if (sym > '9') sym += (Char)((UInt32)'a' - (UInt32)'9' - 1);
                            buf[(i << 1) + 1] = sym;

                        }
                        //buf[length << 1] = 0;
                    }
                    return new String(buf);

                case type_fields.tf_varbinary:
                    //m = *(int16_t*)fr; // длина + смещение
                    m = fr.Length;
                    for (i = 0; i < m; i++)
                    {
                        sym = (Char)((UInt32)'0' + ((UInt32)fr[i] >> 4));
                        if (sym > '9') sym += (Char)((UInt32)'a' - (UInt32)'9' - 1);
                        buf[i << 1] = sym;
                        sym = (Char)((UInt32)'0' + ((UInt32)fr[i] & 0xf));
                        if (sym > '9') sym += (Char)((UInt32)'a' - (UInt32)'9' - 1);
                        buf[(i << 1) + 1] = sym;
                    }
                    //buf[m << 1] = 0;
                    return new String(buf);
            }

            return "{?}";
        }

        public override String get_XML_presentation(char[] rec, v8Table parent, bool ignore_showGUID)
        {
            Char sym;
            Int32 i, m;

            String s;

            char[] fr = new char[rec.Length];
            Array.Copy(rec, fr, rec.Length);

            //char* buf = new char[(length + 1) * 2]; // TODO: адовый костыль с утечкой памяти
            char[] buf = new char[(length + 1) * 2];  // TODO: нам к костылям не привыкать


            switch (type)
            {
                case type_fields.tf_binary:
                    if (length == 16 && (showGUID || ignore_showGUID))
                    {
                        if (showGUIDasMS)
                            return GUIDasMS(fr);
                        else
                            return GUIDas1C(fr);
                    }
                    else
                    {
                        for (i = 0; i < length; i++)
                        {
                            sym = (Char)((UInt32)'0' + ((UInt32)fr[i] >> 4));
                            if (sym > '9') sym += (Char)((UInt32)'a' - (UInt32)'9' - 1);
                            buf[i << 1] = sym;
                            sym = (Char)((UInt32)'0' + ((UInt32)fr[i] & 0xf));
                            if (sym > '9') sym += (Char)((UInt32)'a' - (UInt32)'9' - 1);
                            buf[(i << 1) + 1] = sym;
                        }

                    }
                    return new String(buf);

                case type_fields.tf_varbinary:
                    m = fr.Length; // длина + смещение
                    for (i = 0; i < m; i++)
                    {
                        sym = (Char)((UInt32)'0' + ((UInt32)fr[i] >> 4));
                        if (sym > '9') sym += (Char)((UInt32)'a' - (UInt32)'9' - 1);
                        buf[i << 1] = sym;
                        sym = (Char)((UInt32)'0' + ((UInt32)fr[i + 2] & 0xf));
                        if (sym > '9') sym += (Char)((UInt32)'a' - (UInt32)'9' - 1);
                        buf[(i << 1) + 1] = sym;
                    }

                    return new String(buf);
            }

            return "{?}";

            //return base.get_XML_presentation(rec, parent, ignore_showGUID);
        }

        public override uint getSortKey(byte[] rec, byte[] SortKey, int maxlen)
        {
            char[] fr = System.Text.Encoding.UTF8.GetString(rec).ToCharArray();
            byte[] fr_byte = new byte[rec.Length];
            Array.Copy(rec, fr_byte, rec.Length);

            if (maxlen == 0)
            {
                throw new Exception($"Ошибка получения ключа сортировки поля. Нулевая длина буфера. Значение поля { get_fast_presentation(fr) }");
            }

            switch (type)
            {
                case type_fields.tf_binary:
                    if (len > maxlen)
                    {
                        throw new Exception($"Ошибка получения ключа сортировки поля. Длина буфера меньше необходимой. Значение поля { get_fast_presentation(fr) }. Длина буфера {maxlen}. Необходимая длина буфера {len}");
                    }
                    Array.Copy(fr_byte, SortKey, len);
                    return (UInt32)len;

                case type_fields.tf_varbinary:
                    throw new Exception($"Попытка получения ключа сортировки неподдерживаемого типа поля. Значение поля { get_fast_presentation(fr) }");
            }
            return 0;
        }

    }

    public class NumericFieldType : CommonFieldType
    {
        public NumericFieldType(field_type_declaration declaration) : base(declaration)
        {

        }

        public override String get_presentation(char[] rec, bool EmptyNull, char Delimiter, bool ignore_showGUID, bool detailed)
        {
            char sym;
            Int32 i, m;

            //unsigned char* fr = (unsigned char*)rec;
            char[] fr = new char[rec.Length];
            Array.Copy(rec, fr, rec.Length);

            //char* buf = new char[(length + 1) * 2]; // TODO: адовый костыль с утечкой памяти
            char[] buf = new char[(length + 1) * 2];

            switch (type)
            {
                case type_fields.tf_binary:
                    if (length == 16 && (showGUID || ignore_showGUID))
                    {
                        if (showGUIDasMS)
                            return GUIDasMS(fr);
                        else
                            return GUIDas1C(fr);
                    }
                    else
                    {
                        for (i = 0; i < length; i++)
                        {
                            sym = (Char)((UInt32)'0' + ((UInt32)fr[i] >> 4));
                            if (sym > '9') sym += (Char)((UInt32)'a' - (UInt32)'9' - 1);
                            buf[i << 1] = sym;
                            sym = (Char)((UInt32)'0' + ((UInt32)fr[i] & 0xf));
                            if (sym > '9') sym += (Char)((UInt32)'a' - (UInt32)'9' - 1);
                            buf[(i << 1) + 1] = sym;

                        }
                        //buf[length << 1] = 0;
                    }
                    return new String(buf);

                case type_fields.tf_varbinary:
                    //m = *(int16_t*)fr; // длина + смещение
                    m = fr.Length;
                    for (i = 0; i < m; i++)
                    {
                        sym = (Char)((UInt32)'0' + ((UInt32)fr[i] >> 4));
                        if (sym > '9') sym += (Char)((UInt32)'a' - (UInt32)'9' - 1);
                        buf[i << 1] = sym;
                        sym = (Char)((UInt32)'0' + ((UInt32)fr[i] & 0xf));
                        if (sym > '9') sym += (Char)((UInt32)'a' - (UInt32)'9' - 1);
                        buf[(i << 1) + 1] = sym;
                    }
                    //buf[m << 1] = 0;
                    return new String(buf);
            }

            return "{?}";
        }

        public override String get_XML_presentation(char[] rec, v8Table parent, bool ignore_showGUID)
        {
            Char sym;
            Int32 i, m;

            String s;

            char[] fr = new char[rec.Length];
            Array.Copy(rec, fr, rec.Length);

            //char* buf = new char[(length + 1) * 2]; // TODO: адовый костыль с утечкой памяти
            char[] buf = new char[(length + 1) * 2];  // TODO: нам к костылям не привыкать


            switch (type)
            {
                case type_fields.tf_binary:
                    if (length == 16 && (showGUID || ignore_showGUID))
                    {
                        if (showGUIDasMS)
                            return GUIDasMS(fr);
                        else
                            return GUIDas1C(fr);
                    }
                    else
                    {
                        for (i = 0; i < length; i++)
                        {
                            sym = (Char)((UInt32)'0' + ((UInt32)fr[i] >> 4));
                            if (sym > '9') sym += (Char)((UInt32)'a' - (UInt32)'9' - 1);
                            buf[i << 1] = sym;
                            sym = (Char)((UInt32)'0' + ((UInt32)fr[i] & 0xf));
                            if (sym > '9') sym += (Char)((UInt32)'a' - (UInt32)'9' - 1);
                            buf[(i << 1) + 1] = sym;
                        }

                    }
                    return new String(buf);

                case type_fields.tf_varbinary:
                    m = fr.Length; // длина + смещение
                    for (i = 0; i < m; i++)
                    {
                        sym = (Char)((UInt32)'0' + ((UInt32)fr[i] >> 4));
                        if (sym > '9') sym += (Char)((UInt32)'a' - (UInt32)'9' - 1);
                        buf[i << 1] = sym;
                        sym = (Char)((UInt32)'0' + ((UInt32)fr[i + 2] & 0xf));
                        if (sym > '9') sym += (Char)((UInt32)'a' - (UInt32)'9' - 1);
                        buf[(i << 1) + 1] = sym;
                    }

                    return new String(buf);
            }

            return "{?}";

            //return base.get_XML_presentation(rec, parent, ignore_showGUID);
        }

        public override uint getSortKey(byte[] rec, byte[] SortKey, int maxlen)
        {
            char[] fr = System.Text.Encoding.UTF8.GetString(rec).ToCharArray();
            byte[] fr_byte = new byte[rec.Length];
            Array.Copy(rec, fr_byte, rec.Length);

            if (maxlen == 0)
            {
                throw new Exception($"Ошибка получения ключа сортировки поля. Нулевая длина буфера. Значение поля { get_fast_presentation(fr) }");
            }

            switch (type)
            {
                case type_fields.tf_binary:
                    if (len > maxlen)
                    {
                        throw new Exception($"Ошибка получения ключа сортировки поля. Длина буфера меньше необходимой. Значение поля { get_fast_presentation(fr) }. Длина буфера {maxlen}. Необходимая длина буфера {len}");
                    }
                    Array.Copy(fr_byte, SortKey, len);
                    return (UInt32)len;

                case type_fields.tf_varbinary:
                    throw new Exception($"Попытка получения ключа сортировки неподдерживаемого типа поля. Значение поля { get_fast_presentation(fr) }");
            }
            return 0;
        }

        public bool get_binary_value(char[] binary_value, String value)
        {

            Int32 l = value.Length;
	        if(l == 0)
            {
		        return true;
	        }

	        BinaryDecimalNumber bdn = new BinaryDecimalNumber(value, true, getlength(), getprecision());
            bdn.write_to(binary_value);

	        return true;
        }


}

public class DatetimeFieldType : CommonFieldType
    {
        public DatetimeFieldType(field_type_declaration declaration) : base(declaration)
        {

        }
    }


    


}
