using System;
using System.Text;
using System.IO;
using System.Collections;
using System.Collections.Specialized;
using Ratcow.Muscle.Support;


/// <summary>
/// This is part of the legacy implementation of MUSCLE C# API.
/// 
/// I've basically taken the code and made it in to modern C#.
/// The old API was a bit funky. Partly as it was based on Java
/// and partly because it broke a few rules and used inheritence 
/// in a very odd way. This code is targettin 4.5.2 and uses
/// C# 7 features. But it should compile in earlier versions
/// of .Net framework.. you'd just need at least VS2017 to compile
/// it successfully.
/// </summary>
namespace Ratcow.Muscle.Message.Legacy
{
    using static TypeConstants;
    using static MessageUtils;

    /// <summary> 
    /// This class is sort of similar to Be's BMessage class.  When flattened,
    /// the resulting byte stream is compatible with the flattened
    /// buffers of MUSCLE's C++ Message class.
    /// It only acts as a serializable data container; it does not 
    /// include any threading capabilities.  
    /// </summary>

    public class Message : Flattenable
    {
        /// Oldest serialization protocol version parsable by this code's 
        /// unflatten() methods
        public const int OLDEST_SUPPORTED_PROTOCOL_VERSION = 1347235888; // 'PM00'

        /// Newest serialization protocol version parsable by this code's 
        /// unflatten() methods, as well as the version of the protocol 
        /// produce by this code's flatten() methods.
        public const int CURRENT_PROTOCOL_VERSION = 1347235888; // 'PM00'

        /// 32 bit 'what' code, for quick identification of message types.  
        /// Set this however you like.
        public int What { get; set; }

        /// static constructor
        static Message()
        {
            empty = new HybridDictionary();
        }

        /// Default Constructor.
        public Message()
        {
            What = 0;
        }

        /// Constructor.
        /// <param name="what">
        /// The 'what' member variable will be set to the value you specify here
        /// </param>
        ///
        public Message(int what)
        {
            What = what;
        }

        /// Copy Constructor.
        /// <param name="copyMe">
        /// the Message to make us a (wholly independant) copy.
        /// </param>
        public Message(Message copyMe)
        {
            SetEqualTo(copyMe);
        }

        /// Returns an independent copy of this Message
        public override Flattenable Clone()
        {
            var clone = new Message();
            clone.SetEqualTo(this);
            return clone;
        }

        /// Sets this Message equal to (c)
        /// <param name="c">What to clone.</param>
        /// <exception cref="System.InvalidCastException"/>
        public override void SetEqualTo(Flattenable c)
        {
            Clear();
            if (c is Message cloneMe)
            {
                What = cloneMe.What;
                IEnumerator fields = cloneMe.FieldNames();
                while (fields.MoveNext())
                {
                    cloneMe.CopyField((string)fields.Current, this);
                }
            }
        }

        /// Returns an Enumeration of Strings that are the
        /// field names present in this Message
        public IEnumerator FieldNames()
        {
            return (fieldTable != null) ? fieldTable.Keys.GetEnumerator() : empty.Keys.GetEnumerator();
        }

        /// Returns the number of field names of the given type that 
        /// are present in the Message.
        /// <param name="type">
        /// The type of field to count, or B_ANY_TYPE to 
        /// count all field types.
        /// </param>
        /// <returns>The number of matching fields, or zero if there are 
        ///  no fields of the appropriate type.</returns>
        ///
        public int CountFields(int type)
        {
            if (fieldTable == null)
            {
                return 0;
            }

            if (type == B_ANY_TYPE)
            {
                return fieldTable.Count;
            }

            int count = 0;
            IEnumerator e = fieldTable.Values.GetEnumerator();
            while (e.MoveNext())
            {
                if (((MessageField)e.Current).TypeCode == type)
                {
                    count++;
                }
            }
            return count;
        }

        /// Returns the total number of fields in this Message.
        ///
        public int CountFields()
        {
            return CountFields(B_ANY_TYPE);
        }

        /// Returns true iff there are no fields in this Message.
        ///
        public bool IsEmpty()
        {
            return (CountFields() == 0);
        }


        /// Returns a string that is a summary of the contents of this Message.
        /// Good for debugging.
        ///
        public override string ToString()
        {
            var result = $"Message: what='{ WhatString(What)}' ({What}), countField={CountFields()} flattenedSize={FlattenedSize}\n";

            IEnumerator e = this.FieldNames();

            while (e.MoveNext())
            {
                string fieldName = (string)e.Current;
                result += $"   {fieldName}: {(MessageField)fieldTable[fieldName]}\n";
            }

            return result;
        }

        /// Renames a field.
        /// If a field with this name already exists, it will be replaced.
        /// <param name="old_entry">Old field name to rename</param>
        /// <param name="new_entry">New field name to rename</param>
        /// <exception cref="FieldNotFoundException"/>
        ///
        public void RenameField(string old_entry, string new_entry)
        {
            var field = GetField(old_entry);
            fieldTable.Remove(old_entry);
            fieldTable.Add(new_entry, field);
        }

        /// <returns>false as messages can be of varying size.</returns>
        public override bool IsFixedSize
        {
            get { return false; }
        }

        /// <returns>Returns B_MESSAGE_TYPE</returns>
        public override int TypeCode
        {
            get { return B_MESSAGE_TYPE; }
        }

        /// Returns The number of bytes it would take to flatten this 
        /// Message into a byte buffer.
        public override int FlattenedSize
        {
            get
            {
                // 4 bytes for the protocol revision #, 4 bytes for the 
                // number-of-entries field, 4 bytes for what code
                var result = 4 + 4 + 4;
                if (fieldTable != null)
                {
                    IEnumerator e = FieldNames();
                    while (e.MoveNext())
                    {
                        var fieldName = (string)e.Current;
                        var field = (MessageField)fieldTable[fieldName];

                        // 4 bytes for the name length, name data, 
                        // 4 bytes for entry type code, 
                        /// 4 bytes for entry data length, entry data
                        result += 4 + (fieldName.Length + 1) + 4 + 4 + field.FlattenedSize;
                    }
                }
                return result;
            }
        }

        /// Returns true iff (code) is B_MESSAGE_TYPE
        public override bool AllowsTypeCode(int code)
        {
            return (code == B_MESSAGE_TYPE);
        }

        public override void Flatten(BinaryWriter writer)
        {
            // Format:  0. Protocol revision number 
            //             (4 bytes, always set to CURRENT_PROTOCOL_VERSION)
            //          1. 'what' code (4 bytes)
            //          2. Number of entries (4 bytes)
            //          3. Entry name length (4 bytes)
            //          4. Entry name string (flattened String)
            //          5. Entry type code (4 bytes)
            //          6. Entry data length (4 bytes)
            //          7. Entry data (n bytes)
            //          8. loop to 3 as necessary         
            writer.Write((int)CURRENT_PROTOCOL_VERSION);
            writer.Write((int)What);
            writer.Write((int)CountFields());
            IEnumerator e = FieldNames();
            while (e.MoveNext())
            {
                string name = (string)e.Current;
                MessageField field = (MessageField)fieldTable[name];

                byte[] byteArray = Encoding.UTF8.GetBytes(name);

                writer.Write((int)(byteArray.Length + 1));
                writer.Write(byteArray);
                writer.Write((byte)0);  // terminating NUL byte

                writer.Write(field.TypeCode);
                writer.Write(field.FlattenedSize);
                field.Flatten(writer);
            }
        }

        public override void Unflatten(BinaryReader reader, int numBytes)
        {
            Clear();
            int protocolVersion = reader.ReadInt32();

            if ((protocolVersion > CURRENT_PROTOCOL_VERSION) ||
                (protocolVersion < OLDEST_SUPPORTED_PROTOCOL_VERSION))
                throw new UnflattenFormatException("Version mismatch error");

            What = reader.ReadInt32();
            int numEntries = reader.ReadInt32();

            if (numEntries > 0)
                EnsureFieldTableAllocated();

            char[] charArray = null;

            Decoder d = Encoding.UTF8.GetDecoder();

            for (int i = 0; i < numEntries; i++)
            {
                int fieldNameLength = reader.ReadInt32();
                byte[] byteArray = reader.ReadBytes(fieldNameLength);

                int charArrayLen = d.GetCharCount(byteArray, 0, fieldNameLength);

                if (charArray == null || charArray.Length < charArrayLen)
                    charArray = new char[charArrayLen];

                int charsDecoded = d.GetChars(byteArray, 0, fieldNameLength,
                              charArray, 0);

                string f = new string(charArray, 0, charsDecoded - 1);
                MessageField field =
                  GetCreateOrReplaceField(f, reader.ReadInt32());

                field.Unflatten(reader, reader.ReadInt32());

                fieldTable.Remove(f);
                fieldTable.Add(f, field);
            }
        }

        /// Sets the given field name to contain a single boolean value.  
        /// Any previous field contents are replaced. 
        /// <param name="Name"/>
        /// <param name="val"/>
        ///
        public void SetBoolean(string name, bool val)
        {
            MessageField field = GetCreateOrReplaceField(name, B_BOOL_TYPE);
            bool[] array = (bool[])field.GetData();
            if ((array == null) || (field.Size() != 1))
                array = new bool[1];
            array[0] = val;
            field.SetPayload(array, 1);
        }

        /// Sets the given field name to contain the given boolean values.
        /// Any previous field contents are replaced. 
        /// Note that the array is not copied; rather the passed-in array 
        /// becomes part of the Message.
        /// <param name="name"/>
        /// <param name="val"/>
        public void SetBooleans(string name, bool[] vals)
        {
            SetObjects(name, B_BOOL_TYPE, vals, vals.Length);
        }

        /// Returns the first boolean value in the given field. 
        /// <param name="name"/>
        /// <returns>The first boolean value in the field.</returns>
        /// <exception cref="FieldNotFoundException"/>
        /// <exception cref="FieldTypeMismatchException">if the field
        /// with the given name is not a B_BOOL_TYPE field</exception>
        ///
        public bool GetBoolean(string name)
        {
            return GetBooleans(name)[0];
        }

        public bool GetBoolean(string name, bool def)
        {
            try
            {
                return GetBoolean(name);
            }
            catch (MessageException)
            {
                return def;
            }
        }

        public bool[] GetBooleans(string name)
        {
            return (bool[])GetData(name, B_BOOL_TYPE);
        }

        public void SetByte(string name, byte val)
        {
            MessageField field = GetCreateOrReplaceField(name, B_INT8_TYPE);
            byte[] array = (byte[])field.GetData();
            if ((array == null) || (field.Size() != 1))
                array = new byte[1];
            array[0] = val;
            field.SetPayload(array, 1);
        }

        public void SetBytes(string name, byte[] vals)
        {
            SetObjects(name, B_INT8_TYPE, vals, vals.Length);
        }

        public byte GetByte(string name)
        {
            return GetBytes(name)[0];
        }

        public byte GetByte(string name, byte def)
        {
            try
            {
                return GetByte(name);
            }
            catch (MessageException)
            {
                return def;
            }
        }

        public byte[] GetBytes(string name)
        {
            return (byte[])GetData(name, B_INT8_TYPE);
        }

        public void SetShort(string name, short val)
        {
            MessageField field = GetCreateOrReplaceField(name, B_INT16_TYPE);
            short[] array = (short[])field.GetData();
            if ((array == null) || (field.Size() != 1))
                array = new short[1];
            array[0] = val;
            field.SetPayload(array, 1);
        }

        public void SetShorts(string name, short[] vals)
        {
            SetObjects(name, B_INT16_TYPE, vals, vals.Length);
        }

        public short GetShort(string name)
        {
            return GetShorts(name)[0];
        }

        public short GetShort(string name, short def)
        {
            try
            {
                return GetShort(name);
            }
            catch (MessageException)
            {
                return def;
            }
        }

        public short[] GetShorts(string name)
        {
            return (short[])GetData(name, B_INT16_TYPE);
        }

        public void SetInt(string name, int val)
        {
            MessageField field = GetCreateOrReplaceField(name, B_INT32_TYPE);
            int[] array = (int[])field.GetData();
            if ((array == null) || (field.Size() != 1))
                array = new int[1];
            array[0] = val;
            field.SetPayload(array, 1);
        }

        public void SetInts(string name, int[] vals)
        {
            SetObjects(name, B_INT32_TYPE, vals, vals.Length);
        }

        public int GetInt(string name)
        {
            return GetInts(name)[0];
        }

        public int GetInt(string name, int def)
        {
            try
            {
                return GetInt(name);
            }
            catch (MessageException)
            {
                return def;
            }
        }

        public int[] GetInts(string name)
        {
            return (int[])GetData(name, B_INT32_TYPE);
        }

        public int[] GetInts(string name, int[] defs)
        {
            try
            {
                return GetInts(name);
            }
            catch (MessageException)
            {
                return defs;
            }
        }

        public void SetLong(string name, long val)
        {
            MessageField field = GetCreateOrReplaceField(name, B_INT64_TYPE);
            long[] array = (long[])field.GetData();
            if ((array == null) || (field.Size() != 1))
                array = new long[1];
            array[0] = val;
            field.SetPayload(array, 1);
        }

        public void SetLongs(string name, long[] vals)
        {
            SetObjects(name, B_INT64_TYPE, vals, vals.Length);
        }

        public long GetLong(string name)
        {
            return GetLongs(name)[0];
        }

        public long GetLong(string name, long def)
        {
            try
            {
                return GetLong(name);
            }
            catch (MessageException)
            {
                return def;
            }
        }

        public long[] GetLongs(string name)
        {
            return (long[])GetData(name, B_INT64_TYPE);
        }

        public void SetFloat(string name, float val)
        {
            MessageField field = GetCreateOrReplaceField(name, B_FLOAT_TYPE);
            float[] array = (float[])field.GetData();
            if ((array == null) || (field.Size() != 1))
                array = new float[1];
            array[0] = val;
            field.SetPayload(array, 1);
        }

        public void SetFloats(string name, float[] vals)
        {
            SetObjects(name, B_FLOAT_TYPE, vals, vals.Length);
        }

        public float GetFloat(string name)
        {
            return GetFloats(name)[0];
        }

        public float GetFloat(string name, float def)
        {
            try
            {
                return GetFloat(name);
            }
            catch (MessageException)
            {
                return def;
            }
        }

        public float[] GetFloats(string name)
        {
            return (float[])GetData(name, B_FLOAT_TYPE);
        }

        public void SetDouble(string name, double val)
        {
            MessageField field = GetCreateOrReplaceField(name, B_DOUBLE_TYPE);
            double[] array = (double[])field.GetData();
            if ((array == null) || (field.Size() != 1))
                array = new double[1];
            array[0] = val;
            field.SetPayload(array, 1);
        }

        public void SetDoubles(string name, double[] vals)
        {
            SetObjects(name, B_DOUBLE_TYPE, vals, vals.Length);
        }

        public double GetDouble(string name)
        {
            return GetDoubles(name)[0];
        }

        public double GetDouble(string name, double def)
        {
            try
            {
                return GetDouble(name);
            }
            catch (MessageException)
            {
                return def;
            }
        }

        public double[] GetDoubles(string name)
        {
            return (double[])GetData(name, B_DOUBLE_TYPE);
        }

        public void SetString(string name, string val)
        {
            MessageField field = GetCreateOrReplaceField(name, B_STRING_TYPE);
            string[] array = (string[])field.GetData();
            if ((array == null) || (field.Size() != 1))
                array = new string[1];
            array[0] = val;
            field.SetPayload(array, 1);
        }

        public void SetStrings(string name, string[] vals)
        {
            SetObjects(name, B_STRING_TYPE, vals, vals.Length);
        }

        public string GetString(string name)
        {
            return GetStrings(name)[0];
        }

        public string GetString(string name, string def)
        {
            try
            {
                return GetString(name);
            }
            catch (MessageException)
            {
                return def;
            }
        }

        public string[] GetStrings(string name)
        {
            return (string[])GetData(name, B_STRING_TYPE);
        }

        public string[] GetStrings(string name, string[] def)
        {
            try
            {
                return (string[])GetData(name, B_STRING_TYPE);
            }
            catch (MessageException)
            {
                return def;
            }
        }

        /// Sets the given field name to contain a single Message value.  
        /// Any previous field contents are replaced. 
        /// Note that a copy of (val) is NOT made; the passed-in mesage 
        /// object becomes part of this Message.
        ///
        /// <param name="name"/>
        /// <param name="val/>
        public void SetMessage(string name, Message val)
        {
            MessageField field = GetCreateOrReplaceField(name, B_MESSAGE_TYPE);
            Message[] array = (Message[])field.GetData();
            if ((array == null) || (field.Size() != 1))
                array = new Message[1];
            array[0] = val;
            field.SetPayload(array, 1);
        }

        /// Sets the given field name to contain a single Message value.  
        /// Any previous field contents are replaced. 
        /// Note that the neither the array nor the Message objects are copied;
        /// rather both the array and the Messages become part of this Message.
        /// <param name="name"/>
        /// <param name="val/>
        public void SetMessages(string name, Message[] vals)
        {
            SetObjects(name, B_MESSAGE_TYPE, vals, vals.Length);
        }

        public Message GetMessage(string name)
        {
            return GetMessages(name)[0];
        }

        public Message[] GetMessages(string name)
        {
            return (Message[])GetData(name, B_MESSAGE_TYPE);
        }

        public void SetPoint(string name, Point val)
        {
            MessageField field = GetCreateOrReplaceField(name, B_POINT_TYPE);
            Point[] array = (Point[])field.GetData();
            if ((array == null) || (field.Size() != 1))
                array = new Point[1];
            array[0] = val;
            field.SetPayload(array, 1);
        }

        public void SetPoints(string name, Point[] vals)
        {
            SetObjects(name, B_POINT_TYPE, vals, vals.Length);
        }

        public Point GetPoint(string name)
        {
            return GetPoints(name)[0];
        }

        public Point GetPoint(string name, Point def)
        {
            try
            {
                return GetPoint(name);
            }
            catch (MessageException)
            {
                return def;
            }
        }

        public Point[] GetPoints(string name)
        {
            return (Point[])GetData(name, B_POINT_TYPE);
        }

        public void SetRect(string name, Rect val)
        {
            MessageField field = GetCreateOrReplaceField(name, B_RECT_TYPE);
            Rect[] array = (Rect[])field.GetData();
            if ((array == null) || (field.Size() != 1))
                array = new Rect[1];
            array[0] = val;
            field.SetPayload(array, 1);
        }

        public void SetRects(string name, Rect[] vals)
        {
            SetObjects(name, B_RECT_TYPE, vals, vals.Length);
        }

        public Rect GetRect(string name)
        {
            return GetRects(name)[0];
        }

        public Rect GetRect(string name, Rect def)
        {
            try
            {
                return GetRect(name);
            }
            catch (MessageException)
            {
                return def;
            }
        }

        public Rect[] GetRects(string name)
        {
            return (Rect[])GetData(name, B_RECT_TYPE);
        }

        /// Sets the given field name to contain the flattened bytes of 
        /// the single given Flattenable object. Any previous field contents
        /// are replaced.  The type code of the field is determined by 
        /// calling val.typeCode().
        /// (val) will be flattened and the resulting bytes kept.
        /// (val) does not become part of the Message object.
        /// <param name="name"/>
        /// <param name="val">
        /// the object whose bytes are to be flattened out and put into 
        /// this field.</param>

        public void SetFlat(string name, Flattenable val)
        {
            int type = val.TypeCode;
            MessageField field = GetCreateOrReplaceField(name, type);

            object payload = field.GetData();

            switch (type)
            {
                // For these types, we have explicit support for holding 
                // the objects in memory, so we'll just clone them
                case B_MESSAGE_TYPE:
                    {
                        Message[] array =
                          ((payload != null) && (((Message[])payload).Length == 1)) ? ((Message[])payload) : new Message[1];

                        array[1] = (Message)val.Clone();
                        field.SetPayload(array, 1);
                    }
                    break;

                case B_POINT_TYPE:
                    {
                        Point[] array =
                      ((payload != null) && (((Point[])payload).Length == 1)) ?
                      ((Point[])payload) : new Point[1];
                        array[1] = (Point)val.Clone();
                        field.SetPayload(array, 1);
                    }
                    break;

                case B_RECT_TYPE:
                    {
                        Rect[] array =
                      ((payload != null) && (((Rect[])payload).Length == 1)) ?
                      ((Rect[])payload) : new Rect[1];
                        array[1] = (Rect)val.Clone();
                        field.SetPayload(array, 1);
                    }
                    break;

                // For everything else, we have to store the objects as byte buffers
                default:
                    {
                        byte[][] array =
                      ((payload != null) && (((byte[][])payload).Length == 1)) ?
                      ((byte[][])payload) : new byte[1][];
                        array[0] = FlattenToArray(val, array[0]);
                        field.SetPayload(array, 1);
                    }
                    break;
            }
        }


        /// Sets the given field name to contain the given Flattenable values.
        /// Any previous field contents are replaced.
        /// Note that if the objects are Messages, Points, or Rects, 
        /// they will be cloned rather than flattened.
        /// <param name="name"/>
        /// <param name="val">
        /// Array of Flattenable objects to assign to the field.  
        /// The objects are all flattened and
        /// the flattened data is put into the Message; 
        /// the objects themselves do not become part of the message.
        /// </param>
        public void SetFlats(string name, Flattenable[] vals)
        {
            int type = vals[0].TypeCode;
            int len = vals.Length;
            MessageField field = GetCreateOrReplaceField(name, type);

            // For these types, we have explicit support for holding 
            // the objects in memory, so we'll just clone them
            switch (type)
            {
                case B_MESSAGE_TYPE:
                    {
                        Message[] array = new Message[len];
                        for (int i = 0; i < len; i++)
                            array[i] = (Message)vals[i].Clone();
                        field.SetPayload(array, len);
                    }
                    break;

                case B_POINT_TYPE:
                    {
                        Point[] array = new Point[len];
                        for (int i = 0; i < len; i++)
                            array[i] = (Point)vals[i].Clone();
                        field.SetPayload(array, len);

                    }
                    break;

                case B_RECT_TYPE:
                    {
                        Rect[] array = new Rect[len];
                        for (int i = 0; i < len; i++)
                            array[i] = (Rect)vals[i].Clone();
                        field.SetPayload(array, len);
                    }
                    break;

                default:
                    {
                        byte[][] array = (byte[][])field.GetData();
                        if ((array == null) || (field.Size() != len))
                            array = new byte[len][];

                        for (int i = 0; i < len; i++) array[i] =
                              FlattenToArray(vals[i], array[i]);
                        field.SetPayload(array, len);
                    }
                    break;

            }
        }

        /// Retrieves the first Flattenable value in the given field. 
        /// <param name="name"/>
        /// <param name="returnObject"> 
        /// A Flattenable object that, on success, will be set to reflect 
        /// the value held in this field. This object will not be referenced 
        /// by this Message.
        /// </param>
        public void getFlat(string name, Flattenable returnObject)
        {
            MessageField field = GetField(name);
            if (returnObject.AllowsTypeCode(field.TypeCode))
            {
                object o = field.GetData();
                if (o is byte[][])
                    UnflattenFromArray(returnObject, ((byte[][])o)[0]);
                else if (o is Message[])
                    returnObject.SetEqualTo(((Message[])o)[0]);
                else if (o is Point[])
                    returnObject.SetEqualTo(((Point[])o)[0]);
                else if (o is Rect[])
                    returnObject.SetEqualTo(((Rect[])o)[0]);
                else
                    throw new FieldTypeMismatchException($"{name} isn't a flattened-data field");
            }
            else
            {
                throw new FieldTypeMismatchException($"Passed-in object doesn't like typeCode {WhatString(field.TypeCode)}");
            }
        }

        /// Retrieves the contents of the given field as an array of 
        /// Flattenable values. 
        /// <param name="name">Name of the field to look for 
        /// </param> Flattenable values in.
        /// <param name="returnObjects">Should be an array of pre-allocated 
        /// Flattenable objects of the correct type.  On success, this 
        /// array's objects will be set to the proper states as determined by 
        /// the held data in this Message.  All the objects should be of the 
        /// same type.  This method will unflatten as many objects as exist or 
        /// can fit in the array.  These objects will not be referenced by 
        /// this Message.</param>
        /// <returns>The number of objects in (returnObjects) that were 
        /// actually unflattened.  May be less than (returnObjects.length).
        /// </returns>
        /// <exception cref="FieldNotFoundException"/>
        /// <exception cref="FieldTypeMismatchException"/>
        /// <exception cref="UnflattenFormatException"/>
        /// <exception cref="InvalidCastException"/>
        ///
        public int GetFlats(string name, Flattenable[] returnObjects)
        {
            MessageField field = GetField(name);
            if (returnObjects[0].AllowsTypeCode(field.TypeCode))
            {
                object objs = field.GetData();
                int num;
                if (objs is byte[][] bufs)
                {
                    num = (bufs.Length < returnObjects.Length) ? bufs.Length : returnObjects.Length;
                    for (int i = 0; i < num; i++)
                    {
                        UnflattenFromArray(returnObjects[i], bufs[i]);
                    }
                }
                else if (objs is Message[] messages)
                {
                    num = (messages.Length < returnObjects.Length) ? messages.Length : returnObjects.Length;
                    for (int i = 0; i < num; i++)
                    {
                        returnObjects[i].SetEqualTo(messages[i]);
                    }
                }
                else if (objs is Point[] points)
                {
                    num = (points.Length < returnObjects.Length) ? points.Length : returnObjects.Length;
                    for (int i = 0; i < num; i++)
                    {
                        returnObjects[i].SetEqualTo(points[i]);
                    }
                }
                else if (objs is Rect[] rects)
                {
                    num = (rects.Length < returnObjects.Length) ? rects.Length : returnObjects.Length;
                    for (int i = 0; i < num; i++)
                    {
                        returnObjects[i].SetEqualTo(rects[i]);
                    }
                }
                else throw new FieldTypeMismatchException($"{name} wasn't an unflattenable data field");

                return num;
            }
            else throw new FieldTypeMismatchException($"Passed-in objects doen't like typeCode {WhatString(field.TypeCode)}");
        }

        /// Sets the given field name to contain a single byte buffer value.  Any previous field contents are replaced. 
        /// <param name="name"/>
        /// <param name="type">The type code to give the field.  
        /// May not be a B_*_TYPE that contains non-byte-buffer 
        /// data (e.g. B_STRING_TYPE or B_INT32_TYPE)
        /// </param>
        /// <param name="val">Value that will become the sole value in the 
        /// specified field.</param>
        /// <exception cref="FieldTypeMismatchException"/>
        ///
        public void SetByteBuffer(string name, int type, byte[] val)
        {
            CheckByteBuffersOkay(type);
            var field = GetCreateOrReplaceField(name, type);
            var array = (byte[][])field.GetData();
            if ((array == null) || (field.Size() != 1)) array = new byte[1][];
            array[0] = val;
            field.SetPayload(array, 1);
        }

        /// Sets the given field name to contain the given byte buffer 
        /// values.  Any previous field contents are replaced.
        /// <param name="name"/>
        /// <param name="type">The type code to file the byte buffers under.  
        /// May not be any a B_*_TYPE that contains non-byte-buffer 
        /// data (e.g. B_STRING_TYPE or B_INT32_TYPE).</param>
        /// <param name="vals">Array of byte buffers to assign to the 
        /// field.  Note that the neither the array nor the buffers it 
        /// contains are copied; rather the all the passed-in data becomes 
        /// part of the Message.</param>
        /// <exception cref="FieldTypeMismatchException"/>
        ///
        public void SetByteBuffers(string name, int type, byte[][] vals)
        {
            CheckByteBuffersOkay(type);
            SetObjects(name, type, vals, vals.Length);
        }

        /// Returns the first byte buffer value in the given field. 
        /// Note that the returned data is still held by this Message object.
        public byte[] GetBuffer(string name, int type)
        {
            return GetBuffers(name, type)[0];
        }

        /** Returns the first byte buffer value in the given field.
         * Note that the returned data is still held by this Message object.
         * @param def The Default value to return if the field is not found or has the wrong type.
         * @param name Name of the field to look for a byte buffer value in.
         * @return The first byte buffer value in the field.
         */
        public byte[] GetBuffer(string name, int type, byte[] def)
        {
            try
            {
                return GetBuffer(name, type);
            }
            catch (MessageException)
            {
                return def;
            }
        }

        /// Returns the contents of the given field as an array of byte 
        /// buffer values.
        public byte[][] GetBuffers(string name, int type)
        {
            object ret = ((object[])GetData(name, type))[0];
            if (ret is byte[][])
            {
                return (byte[][])ret;
            }

            throw new FieldTypeMismatchException();
        }

        //// Utility method to get the data of a field 
        /// (any type acceptable) with must-be-there semantics
        public object GetData(string name)
        {
            return GetField(name).GetData();
        }

        /// Gets the data of a field, returns def if any exceptions occur.
        public object GetData(string name, object def)
        {
            try
            {
                return GetData(name);
            }
            catch (MessageException)
            {
                return def;
            }
        }

        /// Utility method to get the data of a field 
        /// (of a given type, or B_ANY_TYPE) using standard must-be-there 
        /// semantics
        public object GetData(string name, int type)
        {
            return GetField(name, type).GetData();
        }

        /// Removes the specified field and its contents from the Message.  
        /// Has no effect if the field doesn't exist.
        public void RemoveField(string name)
        {
            if (fieldTable != null)
            {
                fieldTable.Remove(name);
            }
        }

        /// Clears all fields from the Message. */
        public void Clear()
        {
            if (fieldTable != null)
            {
                fieldTable.Clear();
            }
        }


        /// Returns true iff there is a field with the given name and 
        /// type present in the Message
        public bool HasField(string fieldName, int type)
        {
            return (GetFieldIfExists(fieldName, type) != null);
        }

        /// Returns true iff this Message contains a field named (fieldName).
        public bool HasField(string fieldName)
        {
            return HasField(fieldName, B_ANY_TYPE);
        }

        /// Returns the number of items present in the given field, or 
        /// zero if the field isn't present or is of the wrong type.
        /// <exception cref="FieldNotFound"/>
        public int CountItemsInField(string name, int type)
        {
            var field = GetFieldIfExists(name, type);
            return (field != null) ? field.Size() : 0;
        }

        /// Returns the number of items present in the given field, 
        /// or zero if the field isn't present
        /// <exception cref="FieldNotFound"/>
        public int CountItemsInField(string fieldName)
        {
            return CountItemsInField(fieldName, B_ANY_TYPE);
        }

        /// Returns the B_*_TYPE type code of the given field. 
        /// <exception cref="FieldNotFound"/>
        public int GetFieldTypeCode(string name)
        {
            return GetField(name).TypeCode;
        }

        /// Take the data under (name) in this message, and moves it 
        /// into (moveTo).
        /// <exception cref="FieldNotFound"/>
        public void MoveField(string name, Message moveTo)
        {
            var field = GetField(name);
            fieldTable.Remove(name);
            moveTo.EnsureFieldTableAllocated();
            moveTo.fieldTable.Add(name, field);
        }

        /// Take the data under (name) in this message, and copies it 
        /// into (moveTo). 
        /// <exception cref="FieldNotFound"/>
        public void CopyField(string name, Message copyTo)
        {
            var field = GetField(name);
            copyTo.EnsureFieldTableAllocated();
            copyTo.fieldTable.Add(name, field.Clone());
        }

        /// Flattens the given flattenable object into an array and 
        /// returns it.  Pass in an old array to use if you like; 
        /// it may be reused, or not. */
        private byte[] FlattenToArray(Flattenable flat, byte[] optOldBuf)
        {
            var fs = flat.FlattenedSize;
            if ((optOldBuf == null) || (optOldBuf.Length < fs))
            {
                optOldBuf = new byte[fs];
            }

            using (var memStream = new MemoryStream(optOldBuf))
            {
                var writer = new BinaryWriter(memStream);
                flat.Flatten(writer);
            }

            return optOldBuf;
        }

        /// Unflattens the given array into the given object.  
        /// <exception cref="UnflattenFormatException"/>
        ///
        private void UnflattenFromArray(Flattenable flat, byte[] buf)
        {
            using (var memStream = new MemoryStream(buf))
            {
                var reader = new BinaryReader(memStream);
                flat.Unflatten(reader, buf.Length);
            }
        }

        /// Convenience method:  when it returns, _fieldTable is guaranteed 
        /// to be non-null.
        private void EnsureFieldTableAllocated()
        {
            if (fieldTable == null)
            {
                fieldTable = new HybridDictionary();
            }
        }

        /// Sets the contents of a field
        private void SetObjects(string name,int type, Array values, int numValues)
        {
            GetCreateOrReplaceField(name, type).SetPayload(values, numValues);
        }

        /// Utility method to get a field (if it exists), create it if it 
        /// doesn't, or replace it if it exists but is the wrong type.
        /// <param name="name"/>
        /// <param name="type">B_*_TYPE of the field to get or create.  
        /// B_ANY_TYPE should not be used.</param>
        /// <returns>The (possibly new, possible pre-existing) 
        /// MessageField object.</returns>
        private MessageField GetCreateOrReplaceField(string name, int type)
        {
            EnsureFieldTableAllocated();
            MessageField field = (MessageField)fieldTable[name];
            if (field != null)
            {
                if (field.TypeCode != type)
                {
                    fieldTable.Remove(name);
                    return GetCreateOrReplaceField(name, type);
                }
            }
            else
            {
                field = new MessageField(type);
                fieldTable.Add(name, field);
            }
            return field;
        }

        /// Utility method to get a field (any type acceptable) with 
        /// must-be-there semantics
        private MessageField GetField(string name)
        {
            MessageField field;
            if ((fieldTable != null) && ((field = (MessageField)fieldTable[name]) != null))
            {
                return field;
            }

            throw new FieldNotFoundException($"Field {name} not found.");
        }

        /// Utility method to get a field (of a given type) using standard 
        /// must-be-there semantics
        private MessageField GetField(string name, int type)
        {
            MessageField field;
            if ((fieldTable != null) && ((field = (MessageField)fieldTable[name]) != null))
            {
                if ((type == B_ANY_TYPE) || (type == field.TypeCode))
                    return field;
                throw new FieldTypeMismatchException($"Field type mismatch in entry {name}. Requested type code: {type} but got type code {field.TypeCode}");
            }
            else throw new FieldNotFoundException($"Field {name} not found.");
        }

        /// Utility method to get a field using standard 
        /// may-or-may-not-be-there semantics
        private MessageField GetFieldIfExists(string name, int type)
        {
            MessageField field;
            return ((fieldTable != null) && ((field = (MessageField)fieldTable[name]) != null) && ((type == B_ANY_TYPE) || (type == field.TypeCode))) ? field : null;
        }

        /// Throws an exception iff byte-buffers aren't allowed as type (type)
        private void CheckByteBuffersOkay(int type)
        {
            switch (type)
            {
                case B_BOOL_TYPE:
                case B_DOUBLE_TYPE:
                case B_FLOAT_TYPE:
                case B_INT64_TYPE:
                case B_INT32_TYPE:
                case B_INT16_TYPE:
                case B_MESSAGE_TYPE:
                case B_POINT_TYPE:
                case B_RECT_TYPE:
                case B_STRING_TYPE:
                    throw new FieldTypeMismatchException();

                default:
                    /* do nothing */
                    break;
            }
        }

        private IDictionary fieldTable = null;
        private static IDictionary empty = null;
    }
}


