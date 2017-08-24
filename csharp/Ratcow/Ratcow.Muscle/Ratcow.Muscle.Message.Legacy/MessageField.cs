using System;
using System.Text;
using System.IO;
using System.Collections;
using Ratcow.Muscle.Support;

namespace Ratcow.Muscle.Message.Legacy
{
    using static TypeConstants;
    using static MessageUtils;

    /// Represents a single array of like-typed objects
    class MessageField : Flattenable
    {
        public MessageField(int type)
        {
            _type = type;
        }
        public int Size()
        {
            return _numItems;
        }
        public object GetData()
        {
            return _payload;
        }
        public override bool IsFixedSize
        {
           get { return false; }
        }
        public override int TypeCode
        {
            get{ return _type; }
        }

        /** Returns the number of bytes in a single flattened item, or 0 if our items are variable-size */
        public int FlattenedItemSize()
        {
            switch (_type)
            {
                case B_BOOL_TYPE:
                case B_INT8_TYPE:
                    return 1;
                case B_INT16_TYPE:
                    return 2;
                case B_FLOAT_TYPE:
                case B_INT32_TYPE:
                    return 4;
                case B_INT64_TYPE:
                case B_DOUBLE_TYPE:
                case B_POINT_TYPE:
                    return 8;
                case B_RECT_TYPE:
                    return 16;
                default:
                    return 0;
            }
        }

        /// Returns the number of bytes the MessageField will take 
        /// up when it is flattened.
        public override int FlattenedSize
        {
            get
            {
                int ret = 0;
                switch (_type)
                {
                    case B_BOOL_TYPE:
                    case B_INT8_TYPE:
                    case B_INT16_TYPE:
                    case B_FLOAT_TYPE:
                    case B_INT32_TYPE:
                    case B_INT64_TYPE:
                    case B_DOUBLE_TYPE:
                    case B_POINT_TYPE:
                    case B_RECT_TYPE:
                        ret += _numItems * FlattenedItemSize();
                        break;
                    case B_MESSAGE_TYPE:
                        {
                            // there is no number-of-items field for 
                            // B_MESSAGE_TYPE (for historical reasons, sigh)
                            Message[] array = (Message[])_payload;
                            for (int i = 0; i < _numItems; i++)
                                ret += 4 + array[i].FlattenedSize;
                            // 4 for the size int
                        }
                        break;

                    case B_STRING_TYPE:
                        {
                            ret += 4;  // for the number-of-items field
                            string[] array = (string[])_payload;
                            for (int i = 0; i < _numItems; i++)
                            {
                                ret += 4;
                                ret += Encoding.UTF8.GetByteCount(array[i]) + 1;
                                // 4 for the size int, 1 for the nul byte
                            }
                        }
                        break;

                    default:
                        {
                            ret += 4;  // for the number-of-items field
                            byte[][] array = (byte[][])_payload;
                            for (int i = 0; i < _numItems; i++)
                                ret += 4 + array[i].Length;
                            // 4 for the size int
                        }
                        break;
                }
                return ret;
            }
        }

        /// Unimplemented, throws a ClassCastException exception
        public override void SetEqualTo(Flattenable setFromMe)
        {
            throw new InvalidCastException("MessageField.SetEqualTo() not supported");
        }


        /// Flattens our data into the given stream
        /// <exception cref="IOException"/>
        public override void Flatten(BinaryWriter writer)
        {
            switch (TypeCode)
            {
                case B_BOOL_TYPE:
                    {
                        bool[] array = (bool[])_payload;
                        for (int i = 0; i < _numItems; i++)
                            writer.Write((byte)(array[i] ? 1 : 0));
                    }
                    break;

                case B_INT8_TYPE:
                    {
                        byte[] array = (byte[])_payload;
                        writer.Write(array, 0, _numItems);  // wow, easy!
                    }
                    break;

                case B_INT16_TYPE:
                    {
                        short[] array = (short[])_payload;
                        for (int i = 0; i < _numItems; i++)
                            writer.Write((short)array[i]);
                    }
                    break;

                case B_FLOAT_TYPE:
                    {
                        float[] array = (float[])_payload;
                        for (int i = 0; i < _numItems; i++)
                            writer.Write((float)array[i]);
                    }
                    break;

                case B_INT32_TYPE:
                    {
                        int[] array = (int[])_payload;
                        for (int i = 0; i < _numItems; i++)
                            writer.Write((int)array[i]);
                    }
                    break;

                case B_INT64_TYPE:
                    {
                        long[] array = (long[])_payload;
                        for (int i = 0; i < _numItems; i++)
                            writer.Write((long)array[i]);
                    }
                    break;

                case B_DOUBLE_TYPE:
                    {
                        double[] array = (double[])_payload;
                        for (int i = 0; i < _numItems; i++)
                            writer.Write((double)array[i]);
                    }
                    break;

                case B_POINT_TYPE:
                    {
                        Point[] array = (Point[])_payload;
                        for (int i = 0; i < _numItems; i++)
                            array[i].Flatten(writer);
                    }
                    break;

                case B_RECT_TYPE:
                    {
                        Rect[] array = (Rect[])_payload;
                        for (int i = 0; i < _numItems; i++)
                            array[i].Flatten(writer);
                    }
                    break;

                case B_MESSAGE_TYPE:
                    {
                        Message[] array = (Message[])_payload;
                        for (int i = 0; i < _numItems; i++)
                        {
                            writer.Write((int)array[i].FlattenedSize);
                            array[i].Flatten(writer);
                        }
                    }
                    break;

                case B_STRING_TYPE:
                    {
                        string[] array = (string[])_payload;
                        writer.Write((int)_numItems);

                        for (int i = 0; i < _numItems; i++)
                        {
                            byte[] utf8Bytes = Encoding.UTF8.GetBytes(array[i]);
                            writer.Write(utf8Bytes.Length + 1);
                            writer.Write(utf8Bytes);
                            writer.Write((byte)0);  // nul terminator
                        }
                    }
                    break;

                default:
                    {
                        byte[][] array = (byte[][])_payload;
                        writer.Write((int)_numItems);

                        for (int i = 0; i < _numItems; i++)
                        {
                            writer.Write(array[i].Length);
                            writer.Write(array[i]);
                        }
                    }
                    break;
            }
        }

        /// Returns true iff (code) equals our type code */
        public override bool AllowsTypeCode(int code)
        {
            return (code == TypeCode);
        }

        /// Restores our state from the given stream.
        /// Assumes that our _type is already set correctly.
        /// <exception cref="IOException"/>
        ///
        public override void Unflatten(BinaryReader reader, int numBytes)
        {
            // For fixed-size types, calculating the number of items 
            // to read is easy...
            int flattenedCount = FlattenedItemSize();

            if (flattenedCount > 0)
                _numItems = numBytes / flattenedCount;

            switch (_type)
            {
                case B_BOOL_TYPE:
                    {
                        bool[] array = new bool[_numItems];
                        for (int i = 0; i < _numItems; i++)
                            array[i] = (reader.ReadByte() > 0) ? true : false;
                        _payload = array;
                    }
                    break;

                case B_INT8_TYPE:
                    {
                        byte[] array = reader.ReadBytes(_numItems);
                        _payload = array;
                    }
                    break;

                case B_INT16_TYPE:
                    {
                        short[] array = new short[_numItems];
                        for (int i = 0; i < _numItems; i++)
                            array[i] = reader.ReadInt16();
                        _payload = array;
                    }
                    break;

                case B_FLOAT_TYPE:
                    {
                        float[] array = new float[_numItems];
                        for (int i = 0; i < _numItems; i++)
                            array[i] = reader.ReadSingle();
                        _payload = array;
                    }
                    break;

                case B_INT32_TYPE:
                    {
                        int[] array = new int[_numItems];
                        for (int i = 0; i < _numItems; i++)
                            array[i] = reader.ReadInt32();
                        _payload = array;
                    }
                    break;

                case B_INT64_TYPE:
                    {
                        long[] array = new long[_numItems];
                        for (int i = 0; i < _numItems; i++)
                            array[i] = reader.ReadInt64();
                        _payload = array;
                    }
                    break;

                case B_DOUBLE_TYPE:
                    {
                        double[] array = new double[_numItems];
                        for (int i = 0; i < _numItems; i++)
                            array[i] = reader.ReadDouble();
                        _payload = array;
                    }
                    break;

                case B_POINT_TYPE:
                    {
                        Point[] array = new Point[_numItems];
                        for (int i = 0; i < _numItems; i++)
                        {
                            Point p = array[i] = new Point();
                            p.Unflatten(reader, p.FlattenedSize);
                        }
                        _payload = array;
                    }
                    break;

                case B_RECT_TYPE:
                    {
                        Rect[] array = new Rect[_numItems];
                        for (int i = 0; i < _numItems; i++)
                        {
                            Rect r = array[i] = new Rect();
                            r.Unflatten(reader, r.FlattenedSize);
                        }
                        _payload = array;
                    }
                    break;

                case B_MESSAGE_TYPE:
                    {
                        ArrayList temp = new ArrayList();
                        while (numBytes > 0)
                        {
                            Message subMessage = new Message();
                            int subMessageSize = reader.ReadInt32();
                            subMessage.Unflatten(reader, subMessageSize);
                            temp.Add(subMessage);
                            numBytes -= (subMessageSize + 4);  // 4 for the size int
                        }
                        _numItems = temp.Count;
                        Message[] array = new Message[_numItems];
                        for (int j = 0; j < _numItems; j++)
                            array[j] = (Message)temp[j];
                        _payload = array;
                    }

                    break;

                case B_STRING_TYPE:
                    {
                        Decoder d = Encoding.UTF8.GetDecoder();

                        _numItems = reader.ReadInt32();
                        string[] array = new string[_numItems];

                        byte[] byteArray = null;
                        char[] charArray = null;
                        for (int i = 0; i < _numItems; i++)
                        {
                            int nextStringLen = reader.ReadInt32();
                            byteArray = reader.ReadBytes(nextStringLen);

                            int charsRequired = d.GetCharCount(byteArray,
                                               0,
                                               nextStringLen);

                            if (charArray == null || charArray.Length < charsRequired)
                                charArray = new char[charsRequired];

                            int charsDecoded = d.GetChars(byteArray,
                                          0,
                                          byteArray.Length,
                                          charArray,
                                          0);

                            array[i] = new string(charArray, 0, charsDecoded - 1);
                        }
                        _payload = array;
                    }
                    break;

                default:
                    {
                        _numItems = reader.ReadInt32();
                        byte[][] array = new byte[_numItems][];
                        for (int i = 0; i < _numItems; i++)
                        {
                            int length = reader.ReadInt32();
                            array[i] = new byte[length];
                            array[i] = reader.ReadBytes(length);
                        }
                        _payload = array;
                    }
                    break;
            }
        }

        /// Prints some debug info about our state to (out)
        public override string ToString()
        {
            string result = "  Type='" + WhatString(_type) + "', " + _numItems + " items: ";
            int pitems = (_numItems < 10) ? _numItems : 10;
            switch (_type)
            {
                case B_BOOL_TYPE:
                    {
                        for (int i = 0; i < pitems; i++)
                        {
                            result += ((((byte[])_payload)[i] != 0) ? "true " : "false ");
                        }
                    }
                    break;

                case B_INT8_TYPE:
                    {
                        for (int i = 0; i < pitems; i++)
                        {
                            result += (((byte[])_payload)[i] + " ");
                        }
                    }
                    break;

                case B_INT16_TYPE:
                    {
                        for (int i = 0; i < pitems; i++)
                        {
                            result += (((short[])_payload)[i] + " ");
                        }
                    }
                    break;

                case B_FLOAT_TYPE:
                    {
                        for (int i = 0; i < pitems; i++)
                        {
                            result += (((float[])_payload)[i] + " ");
                        }
                    }
                    break;

                case B_INT32_TYPE:
                    {
                        for (int i = 0; i < pitems; i++)
                        {
                            result += (((int[])_payload)[i] + " ");
                        }
                    }
                    break;

                case B_INT64_TYPE:
                    {
                        for (int i = 0; i < pitems; i++)
                        {
                            result += (((long[])_payload)[i] + " ");
                        }
                    }
                    break;

                case B_DOUBLE_TYPE:
                    {
                        for (int i = 0; i < pitems; i++)
                        {
                            result += (((double[])_payload)[i] + " ");
                        }
                    }
                    break;

                case B_POINT_TYPE:
                    {
                        for (int i = 0; i < pitems; i++)
                        {
                            result += ((Point[])_payload)[i];
                        }
                    }
                    break;

                case B_RECT_TYPE:
                    {
                        for (int i = 0; i < pitems; i++)
                        {
                            result += ((Rect[])_payload)[i];
                        }
                    }
                    break;

                case B_MESSAGE_TYPE:
                    {
                        for (int i = 0; i < pitems; i++)
                        {
                            result += ("[" + WhatString(((Message[])_payload)[i].What) + ", "
                                + ((Message[])_payload)[i].CountFields() + " fields] ");
                        }
                    }
                    break;

                case B_STRING_TYPE:
                    {
                        for (int i = 0; i < pitems; i++)
                        {
                            result += ("[" + ((string[])_payload)[i] + "] ");
                        }
                    }
                    break;

                default:
                    {
                        for (int i = 0; i < pitems; i++)
                        {
                            result += ("[" + ((byte[][])_payload)[i].Length + " bytes] ");
                        }
                    }
                    break;
            }
            return result;
        }

        /// Makes an independent clone of this field object 
        /// (a bit expensive)
        public override Flattenable Clone()
        {
            MessageField clone = new MessageField(_type);
            System.Array newArray;  // this will be a copy of our data array
            switch (_type)
            {
                case B_BOOL_TYPE: newArray = new bool[_numItems]; break;
                case B_INT8_TYPE: newArray = new byte[_numItems]; break;
                case B_INT16_TYPE: newArray = new short[_numItems]; break;
                case B_FLOAT_TYPE: newArray = new float[_numItems]; break;
                case B_INT32_TYPE: newArray = new int[_numItems]; break;
                case B_INT64_TYPE: newArray = new long[_numItems]; break;
                case B_DOUBLE_TYPE: newArray = new double[_numItems]; break;
                case B_STRING_TYPE: newArray = new string[_numItems]; break;
                case B_POINT_TYPE: newArray = new Point[_numItems]; break;
                case B_RECT_TYPE: newArray = new Rect[_numItems]; break;
                case B_MESSAGE_TYPE: newArray = new Message[_numItems]; break;
                default: newArray = new byte[_numItems][]; break;
            }

            newArray = (Array)_payload.Clone();

            // If the contents of newArray are modifiable, we need to 
            // clone the contents also
            switch (_type)
            {
                case B_POINT_TYPE:
                    {
                        for (int i = 0; i < _numItems; i++)
                        {
                            ((Point[])newArray)[i] = (Point)((Point[])newArray)[i].Clone();
                        }
                    }
                    break;

                case B_RECT_TYPE:
                    {
                        for (int i = 0; i < _numItems; i++)
                        {
                            ((Rect[])newArray)[i] = (Rect)((Rect[])newArray)[i].Clone();
                        }
                    }
                    break;

                case B_MESSAGE_TYPE:
                    {
                        for (int i = 0; i < _numItems; i++)
                        {
                            ((Message[])newArray)[i] = (Message)((Message[])newArray)[i].Clone();
                        }
                    }
                    break;

                default:
                    {
                        // Clone the byte arrays, since they are modifiable
                        if (newArray is byte[][] array)
                        {
                            for (int i = 0; i < _numItems; i++)
                            {
                                byte[] newBuf = (byte[])array[i].Clone();
                                array[i] = newBuf;
                            }
                        }
                    }
                    break;
            }

            clone.SetPayload(newArray, _numItems);
            return clone;
        }

        /// Sets our payload and numItems fields.
        public void SetPayload(System.Array payload, int numItems)
        {
            _payload = payload;
            _numItems = numItems;
        }

        private int _type;
        private int _numItems;
        private Array _payload;
    }
}


