using System.Drawing;

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

        var fileName = args[0];
        long offset;
        long offsetTry;
        int length;

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

        var fileSize = new FileInfo(fileName).Length;
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

        Console.WriteLine(
            $"""
            Size of file '{fileName}' is {fileSize}
            and offset is {offset} with length {length}
            """);
        return 0;
    }
}
