using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Read1CD
{
    /// <summary>
    /// Класс строитель
    /// </summary>
    public class BinaryDecimalBuilder
    {
        List<Int32> data;

        public BinaryDecimalBuilder(List<Int32> data)
        {
            this.data = data;
        }

        public void push1(Byte lo)
        {
            data.Add(lo);
        }

        public void push2(Byte hilo)
        {
            push1((Byte)(hilo >> 4));
            push1((Byte)(hilo & 0x0f));
        }

    }

    public class BinaryDecimalNumber
    {
        #region Конструкторский отдел
        public BinaryDecimalNumber()
        {
            has_sing_flag = false;
            precision = 0;
            sign = 1;
        }

        public BinaryDecimalNumber(byte[] raw_data, int length, int precision, bool has_sign_flag)
        {
            data.Clear();
            BinaryDecimalBuilder builder = new BinaryDecimalBuilder(data);

            //UInt16 byte_data = (uint8_t*)raw_data;
            //UInt16 first_byte = *byte_data;
            Byte byte_data = 0;
            Byte first_byte = 0;
            if (has_sign_flag)
            {
                if (first_byte >> 4 !=0 )
                {
                    sign = 1;
                }
                else
                {
                    sign = -1;
                }
                builder.push1((Byte)(first_byte & 0x0f));
            }
            else
            {
                builder.push2(first_byte);
            }
            ++byte_data;
            int i = 1;
            while (i < length)
            {
                if (i + 1 < length)
                {
                    //builder.push2(*byte_data);
                    i += 2;
                }
                else
                {
                    //builder.push1(*byte_data >> 4);
                    i++;
                }
                byte_data++;
            }

        }

        public BinaryDecimalNumber(String presentation, bool has_sign = false, int length = 0, int precision = 0)
        {
            Int32 INT_PART = 0;
            Int32 FRAC_PART = 1;
            //std::array < std::vector<int>, 2 > parts;

            List<List<Int32>> parts = new List<List<int>>();

            int part_no = 0;

            foreach (var c in presentation)
            {
                if (c == '.')
                {
                    part_no++;
                }
                else
                {
                    //parts[part_no].push_back(c - '0');
                    parts[part_no].Add((Int32)(c - '0'));
                }

            }

            foreach (var c in presentation)
            {
                if (c == '.')
                {
                    part_no++;
                }
                else
                {
                    //parts[part_no].push_back(c - '0');
                    parts[part_no].Add((Int32)(c - '0'));
                }
            }

            if (precision != 0)
            {
                //parts[FRAC_PART].resize(precision);
                parts[FRAC_PART].Capacity = precision;
            }
            int add_length = 0;
            if (length != 0)
            {
                add_length = length - parts[INT_PART].Capacity - parts[FRAC_PART].Capacity;
                if (add_length < 0)
                {
                    parts[INT_PART].Clear();
                    //parts[INT_PART].resize(length - precision, 9); // забиваем девяточками при превышении размера
                    parts[INT_PART].Capacity = (length - precision);
                    add_length = 0;
                }
            }
            while (add_length-- != 0)
            {
                //data.push_back(0);
                data.Add(0);
            }

            
            foreach (var part in parts)
                {
                foreach (var num in part)
                {
                    //data.push_back(num);
                    data.Add(num);
                }
            }

        }
        #endregion

        public void write_to(char[] raw_data)
        {

        }

        public virtual String get_presentation()
        {
            String result = "";
            if (has_sing_flag)
            {
                if (sign == -1)
                {
                    result += "-";
                }
            }
            int int_size = data.Capacity - precision;
            {
                int i = 0;
                while (i < int_size && data[i] == 0)
                {
                    i++;
                }
                if (i < int_size)
                {
                    while (i < int_size)
                    {
                        result += '0' + data[i];
                        i++;
                    }
                }
                else
                {
                    result += '0';
                }
            }
            if (precision != 0)
            {
                String frac = ".";
                bool has_significant_digits = false;
                int max_significant_size = data.Capacity;
                while (max_significant_size > int_size)
                {
                    if (data[max_significant_size - 1] == 0)
                    {
                        max_significant_size--;
                    }
                    else
                    {
                        break;
                    }
                }
                for (int i = int_size; i < max_significant_size; i++)
                {
                    if (data[i] != 0)
                    {
                        has_significant_digits = true;
                    }
                    frac += '0' + data[i];
                }
                if (has_significant_digits)
                {
                    result += frac;
                }
            }
            return result;
        }

        public String get_part(int startIndex, int count)
        {
            String result = "";
            for (int i = startIndex; i < data.Capacity && count != 0; count--, i++)
            {
                result += (data[i] + '0');
            }
            return result;
        }

        public List<Int32> get_int()
        {
            List<Int32> result = new List<int>();
            result.AddRange(data.GetRange(0, data.Count - precision));
            return result;
        }

        public List<Int32> get_frac()
        {
            List<Int32> result = new List<int>();
            result.AddRange(data.GetRange(data.Count - precision, precision));
            return result;
        }

        public List<Int32> data;

        public bool has_sing_flag  = false;
        public Int32 precision = 0;
        
        Int32 sign;

    }

    public class BinaryDecimalDate : BinaryDecimalNumber
    {
        public BinaryDecimalDate(byte[] raw_data) : base(raw_data, 19, 0, false)
        {
            
        }

        public BinaryDecimalDate(String presentation, String format)
        {

            //std::map<char, std::vector<int>> indexes;
            SortedDictionary<Char, List<Int32>> indexes = new SortedDictionary<char, List<int>>();
            for (int i = 0; i < format.Length; i++)
            {
                //indexes.insert(std::make_pair(format[i], std::vector<int>()));
                indexes.Add(format[i], new List<int>());
            }
            for (int i = 0; i < format.Length; i++)
            {
                //indexes.at(format[i]).push_back(i);
                indexes[format[i]].Add(i);
            }
            
            foreach (var part in  "yMdhms") 
            {
                foreach (var i in indexes[part])
                {
                    if (i < presentation.Length)
                    {
                        //data.push_back(presentation[i] - '0');
                        data.Add(presentation[i] - '0');
                    }
                    else
                    {
                        //data.push_back(0);
                        data.Add(0);
                    }
                }
            }

        }

        public override String get_presentation()
        {
            String result = "";
            result += get_part(6, 2);
            result += ".";
            result += get_part(4, 2);
            result += ".";
            result += get_part(0, 4);
            result += " ";
            result += get_part(8, 2);
            result += ":";
            result += get_part(10, 2);
            result += ":";
            result += get_part(12, 2);

            return result;
        }

        public int get_year()
        {
            return data[0] * 1000 + data[1] * 100 + data[2] * 10 + data[3]; 
        }

        public int get_month()
        {
            return data[4] * 10 + data[5];
        }

        public int get_day()
        {
            return data[6] * 10 + data[7];
        }

        public int get_hour()
        {
            return data[8] * 10 + data[9];
        }

        public int get_minute()
        {
            return data[10] * 10 + data[11];
        }

        public int get_second()
        {
            return data[12] * 10 + data[13];
        }
    }

}
