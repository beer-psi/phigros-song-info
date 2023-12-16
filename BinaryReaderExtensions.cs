using System.Text;

namespace phigros_song_info;

public static class BinaryReaderExtensions
{
    public static string ReadAlignedString(this BinaryReader reader)
    {
        var len = reader.ReadInt32();
        var buf = reader.ReadBytes(len);
        var pos = (double)reader.BaseStream.Position;
        var off = (long)(-pos - Math.Floor(-pos / 4d) * 4);
        reader.BaseStream.Seek(off, SeekOrigin.Current);
        return Encoding.UTF8.GetString(buf);
    }
}