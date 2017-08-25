using System;
using System.Text;
using System.IO;
using System.Collections;
using Ratcow.Muscle.Support;

namespace Ratcow.Muscle.Message.Legacy
{
    using static Support.Constants.TypeConstants;
    using static MessageUtils;
    using Support.Constants;

    /// Represents a single array of like-typed objects
    class MessageField : Flattenable
    {
        TypeConstants type;
        int numItems;
        Array payload;

        public MessageField(TypeConstants type)
        {
            this.type = type;
        }
        public int Size
        {
            get
            {
                return numItems;
            }
        }
        public object Payload
        {
            get
            {
                return payload;
            }
        }

        public T GetData<T>()
        {
          return (T)((object)payload);            
        }

        public override bool IsFixedSize
        {
            get
            {
                return false;
            }
        }
        public override TypeConstants TypeCode
        {
            get
            {
                return type;
            }
        }

        /** Returns the number of bytes in a single flattened item, or 0 if our items are variable-size */
        public int FlattenedItemSize()
        {
            switch (type)
            {
                case B_BOOL_TYPE:
                case B_INT8_TYPE:
                    return sizeof(byte); //1

                case B_INT16_TYPE:
                    return sizeof(Int16); //2

                case B_FLOAT_TYPE:
                case B_INT32_TYPE:
                    return sizeof(Int32); //4

                case B_INT64_TYPE:
                case B_DOUBLE_TYPE:
                case B_POINT_TYPE:
                    return sizeof(Int64); // 8

                case B_RECT_TYPE:
                    return sizeof(Int64) * 2; //16 (2 * 8)

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
                var result = 0;
                switch (type)
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
                        result += numItems * FlattenedItemSize();
                        break;

                    case B_MESSAGE_TYPE:
                        {
                            // there is no number-of-items field for 
                            // B_MESSAGE_TYPE (for historical reasons, sigh)
                            for (var i = 0; i < numItems; i++)
                            {
                                result += 4 + ((Message[])payload)[i].FlattenedSize;
                            }
                            // 4 for the size int
                        }
                        break;

                    case B_STRING_TYPE:
                        {
                            result += 4;  // for the number-of-items field
                            for (var i = 0; i < numItems; i++)
                            {
                                result += 4;
                                result += Encoding.UTF8.GetByteCount(((string[])payload)[i]) + 1;
                                // 4 for the size int, 1 for the nul byte
                            }
                        }
                        break;

                    default:
                        {
                            result += 4;  // for the number-of-items field
                            for (var i = 0; i < numItems; i++)
                            {
                                result += 4 + ((byte[][])payload)[i].Length;
                            }
                            // 4 for the size int
                        }
                        break;
                }
                return result;
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
                        for (var i = 0; i < numItems; i++)
                        {
                            writer.Write((byte)(((bool[])payload)[i] ? 1 : 0));
                        }
                    }
                    break;

                case B_INT8_TYPE:
                    {
                        writer.Write((byte[])payload, 0, numItems);  // wow, easy!
                    }
                    break;

                case B_INT16_TYPE:
                    {
                        for (var i = 0; i < numItems; i++)
                        {
                            writer.Write((short)((short[])payload)[i]);
                        }
                    }
                    break;

                case B_FLOAT_TYPE:
                    {
                        for (var i = 0; i < numItems; i++)
                        {
                            writer.Write((float)((float[])payload)[i]);
                        }
                    }
                    break;

                case B_INT32_TYPE:
                    {
                        for (var i = 0; i < numItems; i++)
                        {
                            writer.Write((int)((int[])payload)[i]);
                        }
                    }
                    break;

                case B_INT64_TYPE:
                    {
                        for (var i = 0; i < numItems; i++)
                        {
                            writer.Write((long)((long[])payload)[i]);
                        }
                    }
                    break;

                case B_DOUBLE_TYPE:
                    {
                        for (var i = 0; i < numItems; i++)
                        {
                            writer.Write((double)((double[])payload)[i]);
                        }
                    }
                    break;

                case B_POINT_TYPE:
                    {
                        for (var i = 0; i < numItems; i++)
                        {
                            ((Point[])payload)[i].Flatten(writer);
                        }
                    }
                    break;

                case B_RECT_TYPE:
                    {
                        for (var i = 0; i < numItems; i++)
                        {
                            ((Rect[])payload)[i].Flatten(writer);
                        }
                    }
                    break;

                case B_MESSAGE_TYPE:
                    {
                        for (var i = 0; i < numItems; i++)
                        {
                            writer.Write((int)((Message[])payload)[i].FlattenedSize);
                            ((Message[])payload)[i].Flatten(writer);
                        }
                    }
                    break;

                case B_STRING_TYPE:
                    {
                        writer.Write((int)numItems);
                        for (int i = 0; i < numItems; i++)
                        {
                            byte[] utf8Bytes = Encoding.UTF8.GetBytes(((string[])payload)[i]);
                            writer.Write(utf8Bytes.Length + 1);
                            writer.Write(utf8Bytes);
                            writer.Write((byte)0);  // nul terminator
                        }
                    }
                    break;

                default:
                    {
                        writer.Write((int)numItems);
                        for (int i = 0; i < numItems; i++)
                        {
                            writer.Write(((byte[][])payload)[i].Length);
                            writer.Write(((byte[][])payload)[i]);
                        }
                    }
                    break;
            }
        }

        /// Returns true iff (code) equals our type code */
        public override bool AllowsTypeCode(TypeConstants code)
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
                numItems = numBytes / flattenedCount;

            switch (type)
            {
                case B_BOOL_TYPE:
                    {
                        var array = new bool[numItems];
                        for (var i = 0; i < numItems; i++)
                        {
                            array[i] = (reader.ReadByte() > 0) ? true : false;
                        }

                        payload = array;
                    }
                    break;

                case B_INT8_TYPE:
                    {
                        payload = reader.ReadBytes(numItems);
                    }
                    break;

                case B_INT16_TYPE:
                    {
                        var array = new short[numItems];
                        for (var i = 0; i < numItems; i++)
                        {
                            array[i] = reader.ReadInt16();
                        }

                        payload = array;
                    }
                    break;

                case B_FLOAT_TYPE:
                    {
                        var array = new float[numItems];
                        for (var i = 0; i < numItems; i++)
                        {
                            array[i] = reader.ReadSingle();
                        }

                        payload = array;
                    }
                    break;

                case B_INT32_TYPE:
                    {
                        var array = new int[numItems];
                        for (int i = 0; i < numItems; i++)
                        {
                            array[i] = reader.ReadInt32();
                        }

                        payload = array;
                    }
                    break;

                case B_INT64_TYPE:
                    {
                        var array = new long[numItems];
                        for (var i = 0; i < numItems; i++)
                        {
                            array[i] = reader.ReadInt64();
                        }

                        payload = array;
                    }
                    break;

                case B_DOUBLE_TYPE:
                    {
                        var array = new double[numItems];
                        for (var i = 0; i < numItems; i++)
                        {
                            array[i] = reader.ReadDouble();
                        }

                        payload = array;
                    }
                    break;

                case B_POINT_TYPE:
                    {
                        var array = new Point[numItems];
                        for (var i = 0; i < numItems; i++)
                        {
                            Point p = array[i] = new Point();
                            p.Unflatten(reader, p.FlattenedSize);
                        }
                        payload = array;
                    }
                    break;

                case B_RECT_TYPE:
                    {
                        var array = new Rect[numItems];
                        for (var i = 0; i < numItems; i++)
                        {
                            Rect r = array[i] = new Rect();
                            r.Unflatten(reader, r.FlattenedSize);
                        }
                        payload = array;
                    }
                    break;

                case B_MESSAGE_TYPE:
                    {
                        var temp = new ArrayList();
                        while (numBytes > 0)
                        {
                            var subMessage = new Message();
                            var subMessageSize = reader.ReadInt32();
                            subMessage.Unflatten(reader, subMessageSize);
                            temp.Add(subMessage);
                            numBytes -= (subMessageSize + 4);  // 4 for the size int
                        }
                        numItems = temp.Count;
                        var array = new Message[numItems];
                        for (var j = 0; j < numItems; j++)
                        {
                            array[j] = (Message)temp[j];
                        }

                        payload = array;
                    }

                    break;

                case B_STRING_TYPE:
                    {
                        var d = Encoding.UTF8.GetDecoder();

                        numItems = reader.ReadInt32();
                        var array = new string[numItems];

                        byte[] byteArray = null;
                        char[] charArray = null;
                        for (var i = 0; i < numItems; i++)
                        {
                            var nextStringLen = reader.ReadInt32();
                            byteArray = reader.ReadBytes(nextStringLen);

                            var charsRequired = d.GetCharCount(byteArray, 0, nextStringLen);

                            if (charArray == null || charArray.Length < charsRequired)
                            {
                                charArray = new char[charsRequired];
                            }

                            var charsDecoded = d.GetChars(byteArray, 0, byteArray.Length, charArray, 0);

                            array[i] = new string(charArray, 0, charsDecoded - 1);
                        }
                        payload = array;
                    }
                    break;

                default:
                    {
                        numItems = reader.ReadInt32();
                        var array = new byte[numItems][];
                        for (var i = 0; i < numItems; i++)
                        {
                            int length = reader.ReadInt32();
                            array[i] = new byte[length];
                            array[i] = reader.ReadBytes(length);
                        }
                        payload = array;
                    }
                    break;
            }
        }

        /// Prints some debug info about our state to (out)
        public override string ToString()
        {
            var result = $"  Type='{WhatString(type.ToInt32())}', {numItems} items: ";
            int pitems = (numItems < 10) ? numItems : 10;
            switch (type)
            {
                case B_BOOL_TYPE:
                    {
                        for (var i = 0; i < pitems; i++)
                        {
                            result += ((((byte[])payload)[i] != 0) ? "true " : "false ");
                        }
                    }
                    break;

                case B_INT8_TYPE:
                    {
                        for (var i = 0; i < pitems; i++)
                        {
                            result += (((byte[])payload)[i] + " ");
                        }
                    }
                    break;

                case B_INT16_TYPE:
                    {
                        for (var i = 0; i < pitems; i++)
                        {
                            result += (((short[])payload)[i] + " ");
                        }
                    }
                    break;

                case B_FLOAT_TYPE:
                    {
                        for (var i = 0; i < pitems; i++)
                        {
                            result += (((float[])payload)[i] + " ");
                        }
                    }
                    break;

                case B_INT32_TYPE:
                    {
                        for (var i = 0; i < pitems; i++)
                        {
                            result += (((int[])payload)[i] + " ");
                        }
                    }
                    break;

                case B_INT64_TYPE:
                    {
                        for (int i = 0; i < pitems; i++)
                        {
                            result += (((long[])payload)[i] + " ");
                        }
                    }
                    break;

                case B_DOUBLE_TYPE:
                    {
                        for (var i = 0; i < pitems; i++)
                        {
                            result += (((double[])payload)[i] + " ");
                        }
                    }
                    break;

                case B_POINT_TYPE:
                    {
                        for (var i = 0; i < pitems; i++)
                        {
                            result += ((Point[])payload)[i];
                        }
                    }
                    break;

                case B_RECT_TYPE:
                    {
                        for (int i = 0; i < pitems; i++)
                        {
                            result += ((Rect[])payload)[i];
                        }
                    }
                    break;

                case B_MESSAGE_TYPE:
                    {
                        for (var i = 0; i < pitems; i++)
                        {
                            result += ("[" + WhatString(((Message[])payload)[i].What) + ", "
                                + ((Message[])payload)[i].CountFields() + " fields] ");
                        }
                    }
                    break;

                case B_STRING_TYPE:
                    {
                        for (var i = 0; i < pitems; i++)
                        {
                            result += ("[" + ((string[])payload)[i] + "] ");
                        }
                    }
                    break;

                default:
                    {
                        for (var i = 0; i < pitems; i++)
                        {
                            result += ("[" + ((byte[][])payload)[i].Length + " bytes] ");
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
            var clone = new MessageField(type);
            System.Array newArray;  // this will be a copy of our data array
            switch (type)
            {
                case B_BOOL_TYPE:
                    newArray = new bool[numItems];
                    break;

                case B_INT8_TYPE:
                    newArray = new byte[numItems];
                    break;

                case B_INT16_TYPE:
                    newArray = new short[numItems];
                    break;
                case B_FLOAT_TYPE:
                    newArray = new float[numItems];
                    break;

                case B_INT32_TYPE:
                    newArray = new int[numItems];
                    break;

                case B_INT64_TYPE:
                    newArray = new long[numItems];
                    break;

                case B_DOUBLE_TYPE:
                    newArray = new double[numItems];
                    break;

                case B_STRING_TYPE:
                    newArray = new string[numItems];
                    break;

                case B_POINT_TYPE:
                    newArray = new Point[numItems];
                    break;

                case B_RECT_TYPE:
                    newArray = new Rect[numItems];
                    break;

                case B_MESSAGE_TYPE:
                    newArray = new Message[numItems];
                    break;

                default:
                    newArray = new byte[numItems][];
                    break;
            }

            newArray = (Array)payload.Clone();

            // If the contents of newArray are modifiable, we need to 
            // clone the contents also
            switch (type)
            {
                case B_POINT_TYPE:
                    {
                        for (int i = 0; i < numItems; i++)
                        {
                            ((Point[])newArray)[i] = (Point)((Point[])newArray)[i].Clone();
                        }
                    }
                    break;

                case B_RECT_TYPE:
                    {
                        for (int i = 0; i < numItems; i++)
                        {
                            ((Rect[])newArray)[i] = (Rect)((Rect[])newArray)[i].Clone();
                        }
                    }
                    break;

                case B_MESSAGE_TYPE:
                    {
                        for (int i = 0; i < numItems; i++)
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
                            for (int i = 0; i < numItems; i++)
                            {
                                byte[] newBuf = (byte[])array[i].Clone();
                                array[i] = newBuf;
                            }
                        }
                    }
                    break;
            }

            clone.SetPayload(newArray, numItems);
            return clone;
        }

        /// Sets our payload and numItems fields.
        public void SetPayload(Array payload, int numItems)
        {
            this.payload = payload;
            this.numItems = numItems;
        }

    }
}


