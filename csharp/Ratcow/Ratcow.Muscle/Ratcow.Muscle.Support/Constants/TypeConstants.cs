using System;

namespace Ratcow.Muscle.Support.Constants
{
    public class TypeConstantAttribute : Attribute
    {
        public TypeConstants Type { get; set; }
    }

    public static class TypeConstantsHelper
    {
        public static int ToInt32(this TypeConstants value)
        {
            return (int)value;
        }
    }

    public enum TypeConstants : int
    {
        // 'ANYT',  // wild card
        B_ANY_TYPE = 1095653716,

        // 'BOOL',
        B_BOOL_TYPE = 1112493900,

        // 'DBLE',
        B_DOUBLE_TYPE = 1145195589,

        // 'FLOT',
        B_FLOAT_TYPE = 1179406164,

        // 'LLNG',  // a.k.a. long in C#
        B_INT64_TYPE = 1280069191,

        // 'LONG',  // a.k.a. int in C#
        B_INT32_TYPE = 1280265799,

        // 'SHRT',  // a.k.a. short in C# 
        B_INT16_TYPE = 1397248596,

        // 'BYTE',  // a.k.a. byte in C#
        B_INT8_TYPE = 1113150533,

        // 'MSGG',
        B_MESSAGE_TYPE = 1297303367,

        // 'PNTR',  // parsed as int in C# (but not very useful)
        B_POINTER_TYPE = 1347310674,

        // 'BPNT',  // muscle.support.Point in C#
        B_POINT_TYPE = 1112559188,

        // 'RECT',  // muscle.support.Rect in C#
        B_RECT_TYPE = 1380270932,

        // 'CSTR',  // C# string
        B_STRING_TYPE = 1129534546,

        // 'OPTR',
        B_OBJECT_TYPE = 1330664530,

        // 'RAWT',  
        B_RAW_TYPE = 1380013908,

        // 'MIME',  
        B_MIME_TYPE = 1296649541
    }


}
