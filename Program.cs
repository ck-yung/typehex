namespace typehex;
static class Program
{
    static int Main(string[] args)
    {
        try
        {
            return RunMain(args);
        }
        catch (Exception ee)
        {
            Console.Error.WriteLine(ee);
        }
        return 0;
    }

    static readonly string Syntax =
        "Syntax: typehex FILE NUMBER-OFFSET [NUMBER-SIZE]";

    static int RunMain(string[] args)
    {
        if (args.Contains("-?") ||
            args.Contains("-h") ||
            args.Contains("--help"))
        {
            Console.WriteLine(Syntax);
            return 0;
        }

        if (args.Length < 2)
        {
            Console.WriteLine(Syntax);
            return 0;            
        }

        var fileName = args[0];
        long fileSize;
        long offset;
        int length;

        #region Parsing Offset and Length
        string lengthText;
        if (args.Length == 2)
        {
            lengthText = "512";
        }
        else if (args.Length == 3)
        {
            lengthText = args[2];
        }
        else
        {
            Console.WriteLine(Syntax);
            return 0;
        }

        if (false == File.Exists(fileName))
        {
            Console.Error.WriteLine(
                $"{fileName} does not exist.");
            return 0;
        }

        if (false == TryParseInt64(args[1], out offset))
        {
            Console.Error.WriteLine(
                $"Offset {args[1]} is NOT a number.");
            return 0;
        }

        if (false == TryParseInt64(lengthText, out long i64length))
        {
            Console.Error.WriteLine(
                $"Length {args[2]} is NOT a number.");
            return 0;
        }
        //Console.WriteLine($"'{fileName}', offset={offset}, length={i64length}");

        if (i64length > 128 * 1024)
        {
            length = 128 * 1024;
        }
        else
        {
            length = (int) i64length;
        }

        fileSize = new FileInfo(fileName).Length;
        if (offset < 0)
        {
            long offsetTry = fileSize + offset;
            if (0 > offsetTry)
            {
                Console.Error.WriteLine(
                    $"""
                    The size of '{fileName}' is {fileSize}.
                    But offset as '{args[1]}' is over!
                    """);
                return 0;
            }
            offset = offsetTry;
        }

        if (length < 0)
        {
            long endOffset = fileSize + length;
            if (1 > endOffset)
            {
                Console.Error.WriteLine(
                    $"""
                    Size of file '{fileName}' is {fileSize}
                    but end offset from '{lengthText}' is {endOffset}!
                    """);
                return 0;
            }
            if (offset >= endOffset)
            {
                Console.Error.WriteLine(
                    $"""
                    Size of file '{fileName}' is {fileSize} and offset is {offset}
                    but end offset from '{lengthText}' is {endOffset}!
                    """);
                return 0;
            }
            length = (int) (endOffset - offset);
            if (length > 100 * 1024) length = 100 * 1024;
        }

        if (128 * 1024 < length)
        {
            Console.Error.WriteLine(
                $"The length as '{lengthText}', {length} is over 100k !");
            return 0;
        }
        else if (1 > length)
        {
            Console.Error.WriteLine(
                $"The length as '{lengthText}', {length} is NOT positive.");
            return 0;
        }

        SetupOffsetFormat(fileSize, offset + length);

        if (offset >= fileSize)
        {
            Console.Error.WriteLine(
                $"Offset '{args[1]}' as {offset} is over file size {fileSize} !");
            return 0;
        }
        #endregion

        PrintFilePart(fileName, offset, length);
        return 0;
    }

    static void PrintFilePart(string path, long offset, int length)
    {
        var buffer = new byte[128];
        int wantSize = length;
        int readSize = 0;
        int wantRead = 0;

        using var ins = File.OpenRead(path);

        ins.Seek(offset, SeekOrigin.Begin);
        int cntAddSpace = (int) (offset % 16);
        if (cntAddSpace != 0)
        {
            wantRead = 16 - cntAddSpace;
            if (wantRead > length) wantRead = length;
            readSize = ins.Read(buffer, 0, wantRead);
            wantSize -= readSize;
            if (offset > cntAddSpace)
            {
                offset -= cntAddSpace;
            }
            else
            {
                offset = 0;
            }
            PrintHexPreLine(buffer, offset, cntAddSpace, readSize);
            offset += 16;
        }

        while (true)
        {
            if (1 > wantSize) break;
            wantRead = wantSize;
            if (wantRead > buffer.Length)
            {
                wantRead = buffer.Length;
            }

            readSize = ins.Read(buffer, 0, wantRead);
            wantSize -= readSize;
            if (readSize < 1) break;
            for (int offsetThis = 0; offsetThis < readSize; offsetThis+=16)
            {
                if (readSize <= offsetThis) break;
                PrintHexLine(buffer, offset, offsetThis, readSize);
            }
            offset += readSize;
        }
    }

    static void PrintHexLine(byte[] buffer, long offset, long indexThis, int length)
    {
        int lengthMore = 16;
        long indexThis2 = indexThis;
        Console.Write(OffsetText(indexThis + offset));
        for (int ii = 0; ii < 4; ii += 1)
        {
            for (int jj = 0; jj < 3; jj += 1)
            {
                if (indexThis2 >= length) break;
                Console.Write($"{buffer[indexThis2]:x2}.");
                indexThis2 += 1;
                lengthMore -= 1;
            }
            if (indexThis2 >= length) break;
            Console.Write($"{buffer[indexThis2]:x2} ");
            indexThis2 += 1;
            lengthMore -= 1;
        }

        if (lengthMore < 16)
        {
            for (; lengthMore > 0; lengthMore -= 1)
            {
                Console.Write("   ");
            }
        }

        indexThis2 = indexThis;
        byte byteThe;
        char charThe;
        for (int ii = 0; ii < 4; ii += 1)
        {
            Console.Write($" ");
            for (int jj = 0; jj < 4; jj += 1)
            {
                if (indexThis2 >= length) break;
                byteThe = buffer[indexThis2];
                charThe = (31 < byteThe && 128 > byteThe) ? (char)byteThe : '.';
                Console.Write(charThe);
                indexThis2 += 1;
            }
        }
        Console.WriteLine();
    }

    static void PrintHexPreLine(byte[] buffer, long offset, int countOfSpace, int length)
    {
        Console.Write(OffsetText(offset));
        int ii = 0;

        for (ii = 0; ii < countOfSpace; ii+= 1)
        {
            Console.Write("   ");
        }

        for (ii = 0; ii < length; ii += 1)
        {
            Console.Write($"{buffer[ii]:x2}");
            if (0 == ((countOfSpace + ii) % 4)) Console.Write(' ');
            else Console.Write('.');
        }

        for (ii = countOfSpace + length; ii < 16; ii += 1)
        {
            Console.Write("   ");
        }

        for (ii = 0; ii < countOfSpace; ii += 1)
        {
            Console.Write(' ');
            if (0 == (ii % 4)) Console.Write(' ');
        }

        for (ii = 0; ii < length; ii += 1)
        {
            var byThe = buffer[ii];
            if (31 < byThe && byThe < 128) Console.Write((char)byThe);
            else Console.Write('.');
            if (0 == ((countOfSpace + ii) % 4)) Console.Write(' ');
        }
        Console.WriteLine();
    }

    static Func<long, string> OffsetText
    { get; set;} = (it) => $"{it:x4} ";

    static void SetupOffsetFormat(long max1, long max2)
    {
        long maxThe = max1;
        if (maxThe > max2) maxThe = max2;
        var tmp2 = $"{maxThe:x4}";
        if (5 > tmp2.Length) return;
        var fmtThe = $"{{0:x{tmp2.Length}}} ";
        OffsetText = (it) => string.Format(fmtThe, it);
    }

    static bool TryParseInt64(string text, out Int64 value)
    {
        var unit = 1;
        if (text.EndsWith('k'))
        {
            unit = 1024;
            text = text[..^1];
        }
        else if (text.EndsWith('m'))
        {
            unit = 1024 * 1024;
            text = text[..^1];
        }
        else if(text.EndsWith('g'))
        {
            unit = 1024 * 1024 * 1024;
            text = text[..^1];
        }

        if (long.TryParse(text, out long offsetTry))
        {
            value = unit * offsetTry;
            return true;
        }

        if (text.Length > 1 &&
        (text.StartsWith("x") || text.StartsWith("X")))
        {
            var text1 = text.Substring(1);
            long value1 = 0;
            var try1 = Int64.TryParse(text1, 
            System.Globalization.NumberStyles.HexNumber,
            System.Globalization.CultureInfo.InvariantCulture, out value1);
            value = value1;
            return try1;
        }
        else if (text.Length > 2 &&
        (text.StartsWith("0x") || text.StartsWith("0X")))
        {
            var text2 = text.Substring(2);
            long value2 = 0;
            var try2 = Int64.TryParse(text2,
            System.Globalization.NumberStyles.HexNumber,
            System.Globalization.CultureInfo.InvariantCulture, out value2);
            value = value2;
            return try2;
        }
        value = 0;
        return false;
    }
}
