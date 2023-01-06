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

        var tmp = args[1];
        var unit = 1;
        if (tmp.EndsWith('k'))
        {
            unit = 1024;
            tmp = tmp[..^1];
        }
        else if (tmp.EndsWith('m'))
        {
            unit = 1024 * 1024;
            tmp = tmp[..^1];
        }
        else if(tmp.EndsWith('g'))
        {
            unit = 1024 * 1024 * 1024;
            tmp = tmp[..^1];
        }

        long offsetTry;
        if (long.TryParse(tmp, out offsetTry))
        {
            offset = unit * offsetTry;
        }
        else if (tmp.StartsWith('x') || tmp.StartsWith('X'))
        {
            offset = Convert.ToInt64(tmp.Substring(1), 16);
        }
        else
        {
            Console.Error.WriteLine(
                $"{args[1]} is NOT a number.");
            return 0;
        }

        tmp = lengthText;
        unit = 1;
        if (tmp.EndsWith('k'))
        {
            unit = 1024;
            tmp = tmp[..^1];
        }

        if (int.TryParse(tmp, out length))
        {
            length *= unit;
        }
        else if (tmp.StartsWith('x') || tmp.StartsWith('X'))
        {
            length = Convert.ToInt32(tmp.Substring(1), 16);
        }
        else
        {
            Console.Error.WriteLine(
                $"Length {lengthText} is NOT a number, hex-digit, ends with 'k'.");
            return 0;
        }

        fileSize = new FileInfo(fileName).Length;
        if (offset < 0)
        {
            offsetTry = fileSize + offset;
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

        if (100 * 1024 < length)
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


        if (offset >= fileSize)
        {
            Console.Error.WriteLine(
                $"Offset '{args[1]}' as {offset} is over file size {fileSize} !");
            return 0;
        }
        #endregion

        //Console.WriteLine(
        //    $"""
        //    Size of file '{fileName}' is {fileSize}
        //    and offset is {offset} with length {length}
        //    """);

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
            //Console.WriteLine($"dbg: cntAdd={cntAddSpace}, that is, wantRead={wantRead}");
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
            //Console.Write($"dbg: offset={ins.Position},");
            readSize = ins.Read(buffer, 0, wantRead);
            //Console.WriteLine($"want={wantSize},real={readSize}");
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
        Console.Write($"{(indexThis + offset):x4} ");
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
        Console.Write($"{offset:x4} ");
        int indexThis = 0;
        int ndxBuf = 0;
        for (int ii=0; ii<4; ii+=1)
        {
            for (int jj=0;jj<3;jj+=1)
            {
                if (indexThis <countOfSpace)
                {
                    Console.Write("   ");
                }
                else
                {
                    Console.Write($"{buffer[ndxBuf]:x2}.");
                    ndxBuf += 1;
                }
                indexThis += 1;
            }

            if (indexThis < countOfSpace)
            {
                Console.Write("   ");
            }
            else
            {
                Console.Write($"{buffer[ndxBuf]:x2} ");
            }
            indexThis += 1;
        }

        indexThis = 0;
        ndxBuf = 0;
        for (int ii = 0; ii < 4; ii += 1)
        {
            Console.Write(' ');
            for (int jj = 0; jj < 4; jj += 1)
            {
                if (indexThis < countOfSpace)
                {
                    Console.Write(' ');
                }
                else
                {
                    if (31 < buffer[ndxBuf] && buffer[ndxBuf] < 128)
                    {
                        Console.Write((char)buffer[ndxBuf]);
                    }
                    else
                    {
                        Console.Write('.');
                    }
                    ndxBuf += 1;
                }
                indexThis += 1;
            }
        }
        Console.WriteLine();
    }
}
