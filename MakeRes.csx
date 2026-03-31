using System;
using System.IO;

// .RES format: cada resource tiene header + data
// Win32 ICON resource ID=1
var icoBytes = File.ReadAllBytes(@"Assets\app.ico");

// Leer el ICO: extraer el primer image (256x256 PNG)
// ICO header: 6 bytes, entonces directory entries de 16 bytes cada una
int count = BitConverter.ToInt16(icoBytes, 4);

using var res = new FileStream(@"Assets\app.res", FileMode.Create);
using var bw = new BinaryWriter(res);

// Escribir el resource RT_ICON (type=3) group y luego RT_GROUP_ICON (type=14)
// Formato .RES: https://docs.microsoft.com/en-us/windows/win32/menurc/resource-file-formats

// Por cada imagen en el ICO, escribir como RT_ICON
for (int i = 0; i < count; i++) {
    int offset = 6 + i * 16;
    int dataSize = BitConverter.ToInt32(icoBytes, offset + 8);
    int dataOffset = BitConverter.ToInt32(icoBytes, offset + 12);
    
    // .RES resource header
    bw.Write((uint)dataSize);       // DataSize
    bw.Write((uint)32);             // HeaderSize
    bw.Write((ushort)0xFFFF);       // Type: RT_ICON=3
    bw.Write((ushort)3);
    bw.Write((ushort)0xFFFF);       // Name: ID = i+1
    bw.Write((ushort)(i+1));
    bw.Write((uint)0);              // DataVersion
    bw.Write((ushort)0x1010);       // MemoryFlags
    bw.Write((ushort)0);            // LanguageId
    bw.Write((uint)0);              // Version
    bw.Write((uint)0);              // Characteristics
    
    // Icon data
    bw.Write(icoBytes, dataOffset, dataSize);
    // Padding to DWORD boundary
    int pad = (4 - (dataSize % 4)) % 4;
    for (int p = 0; p < pad; p++) bw.Write((byte)0);
}

// RT_GROUP_ICON (type=14, ID=1)
int groupDataSize = 6 + count * 14; // GRPICONDIR header + entries
bw.Write((uint)groupDataSize);
bw.Write((uint)32);
bw.Write((ushort)0xFFFF);       // RT_GROUP_ICON=14
bw.Write((ushort)14);
bw.Write((ushort)0xFFFF);       // ID=1
bw.Write((ushort)1);
bw.Write((uint)0);
bw.Write((ushort)0x1010);
bw.Write((ushort)0);
bw.Write((uint)0);
bw.Write((uint)0);

// GRPICONDIR
bw.Write((ushort)0);    // reserved
bw.Write((ushort)1);    // type=1 (icon)
bw.Write((ushort)count);

for (int i = 0; i < count; i++) {
    int offset = 6 + i * 16;
    bw.Write(icoBytes[offset]);     // width
    bw.Write(icoBytes[offset+1]);   // height
    bw.Write(icoBytes[offset+2]);   // colorCount
    bw.Write((byte)0);              // reserved
    bw.Write(BitConverter.ToInt16(icoBytes, offset+4));  // planes
    bw.Write(BitConverter.ToInt16(icoBytes, offset+6));  // bitCount
    bw.Write(BitConverter.ToInt32(icoBytes, offset+8));  // bytesInRes
    bw.Write((ushort)(i+1));        // ID
}
int gppad = (4 - (groupDataSize % 4)) % 4;
for (int p = 0; p < gppad; p++) bw.Write((byte)0);

Console.WriteLine("app.res creado: " + new FileInfo(@"Assets\app.res").Length + " bytes");
