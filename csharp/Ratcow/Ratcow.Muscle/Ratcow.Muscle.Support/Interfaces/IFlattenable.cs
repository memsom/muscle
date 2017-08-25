using System.IO;

namespace Ratcow.Muscle.Support.Interfaces
{
    using Constants;

    public interface IFlattenable
    {
        int FlattenedSize { get; }
        bool IsFixedSize { get; }
        TypeConstants TypeCode { get; }

        bool AllowsTypeCode(TypeConstants code);
        Flattenable Clone();
        void Flatten(BinaryWriter writer);
        void SetEqualTo(Flattenable setFromMe);
        void Unflatten(BinaryReader reader, int numBytes);
    }
}