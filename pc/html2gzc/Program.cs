using System.IO.Compression;

namespace html2gzc
{
    internal class Program
    {
        private static byte[] Decompress(byte[] compressedData)
        {
            if (compressedData == null || compressedData.Length == 0)
                throw new ArgumentNullException(nameof(compressedData));

            try
            {
                using (var compressedStream = new MemoryStream(compressedData))
                using (var decompressionStream = new GZipStream(compressedStream, CompressionMode.Decompress))
                using (var resultStream = new MemoryStream())
                {
                    decompressionStream.CopyTo(resultStream);
                    return resultStream.ToArray();
                }
            }
            catch (InvalidDataException ex)
            {
                throw new InvalidDataException("压缩数据格式错误或已损坏", ex);
            }
        }

        private static byte[] Compress(byte[] data)
        {
            if (data == null || data.Length == 0)
                throw new ArgumentNullException(nameof(data));

            using (var outputStream = new MemoryStream())
            {
                // 使用CompressionLevel.Optimal获取最佳压缩率
                using (var gzipStream = new GZipStream(outputStream, CompressionLevel.SmallestSize))
                {
                    gzipStream.Write(data, 0, data.Length);
                }
                return outputStream.ToArray();
            }
        }

        private static void Cpp2Html(string inPath)
        {
            var outPath = Path.GetDirectoryName(inPath) + "\\" + Path.GetFileNameWithoutExtension(inPath);
            var inTxt = File.ReadAllText(inPath);
            RmAnnotate nt = new();
            inTxt = nt.Convert(inTxt);
            inTxt = inTxt.Substring(inTxt.LastIndexOf('{') + 1);
            inTxt = inTxt.Substring(0, inTxt.LastIndexOf('}'));
            inTxt = inTxt.Replace(@" ", "");
            inTxt = inTxt.Replace("\t", "");
            inTxt = inTxt.Replace("\r", ",");
            inTxt = inTxt.Replace("\n", ",");
            inTxt = inTxt.Replace(@"{", ",");
            inTxt = inTxt.Replace(@"}", ",");
            inTxt = inTxt.Replace(",,,,", ",");
            inTxt = inTxt.Replace(",,,", ",");
            inTxt = inTxt.Replace(",,", ",");
            var nbTxts = inTxt.Split(',');
            List<byte> gzBuf = [];
            foreach (var nb in nbTxts)
            {
                if (!String.IsNullOrWhiteSpace(nb))
                {
                    var tx = nb.ToLower();
                    byte dt;
                    if (tx.StartsWith("0x"))
                    {
                        tx = tx.Substring(2);
                        dt = Convert.ToByte(tx, 16);
                    }
                    else
                        dt = Convert.ToByte(tx);
                    gzBuf.Add(dt);
                }
            }
            try
            {
                var outHtml = Decompress(gzBuf.ToArray());
                File.WriteAllBytes(outPath + ".html", outHtml);
            }
            catch {
                var outHtml = gzBuf.ToArray();
                File.WriteAllBytes(outPath + ".html", outHtml);
            }
        }

        private static void Gzip2Cpp(string inPath)
        {
            var inFile = Path.GetFileNameWithoutExtension(inPath);
            var outPath = Path.GetDirectoryName(inPath) + "\\" + Path.GetFileNameWithoutExtension(inFile);
            var gzBuf = File.ReadAllBytes(inPath);
            var cTxt = Bin2C.toText(gzBuf, 0, gzBuf.Length);
            cTxt = cTxt.Replace(@"//Total count", $"//GZip {inFile},size");

            if (Path.GetFileName(outPath).StartsWith(@"elop", StringComparison.CurrentCultureIgnoreCase))
            {
                cTxt = cTxt.Replace(@"const unsigned char bBinFile[]", $"#include \"elop.h\" \r\n\r\nconst uint8_t ELEGANT_HTML[{gzBuf.Length}] PROGMEM");
                if (File.Exists(outPath + ".h"))
                {
                    RmAnnotate rm = new();
                    var htxt = File.ReadAllText(outPath + ".h");
                    htxt = htxt.Replace(@" ELEGANT_HTML[", $" ELEGANT_HTML[{gzBuf.Length}]; //");
                    htxt = rm.Convert(htxt);
                    File.WriteAllText(outPath + ".h", htxt);
                }
            }
            else
            {
                cTxt = cTxt.Replace(@"const unsigned char bBinFile[]", $"#include \"{Path.GetFileName(outPath)}.h\" \r\n\r\nconst unsigned char {inFile.Replace(".", "_")}[{gzBuf.Length}]");
                if (File.Exists(outPath + ".h"))
                {
                    RmAnnotate rm = new();
                    var htxt = File.ReadAllText(outPath + ".h");
                    htxt = htxt.Replace($" {inFile.Replace(".", "_")}[", $" {inFile.Replace(".", "_")}[{gzBuf.Length}]; //");
                    htxt = rm.Convert(htxt);
                    File.WriteAllText(outPath + ".h", htxt);
                }
            }
            File.WriteAllText(outPath + ".cpp", cTxt);
        }

        private static void Bin2Gzip(string inPath)
        {
            var binBuf = File.ReadAllBytes(inPath);
            var gzBuf = Compress(binBuf);
            File.WriteAllBytes(inPath + ".gz", gzBuf);
        }

        private static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("file -> gz -> cpp -> html Convert.");
                Console.ReadKey();
                return;
            }
            var inPath = Path.GetFullPath(args[0]);
            if (inPath.ToLower().EndsWith(".cpp"))
                Cpp2Html(inPath);
            else if (inPath.ToLower().EndsWith(".c"))
                Cpp2Html(inPath);
            else if (inPath.ToLower().EndsWith(".gz"))
                Gzip2Cpp(inPath);
            else
                Bin2Gzip(inPath);
        }
    }
}
