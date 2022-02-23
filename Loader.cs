using System;
using System.Buffers.Text;
using System.Text;
using System.Threading.Channels;

namespace digits;

public class FileLoader
{
    public static (List<Record>, List<Record>) GetData(string filename, int offset, int count)
    {
        using var fileStream = new FileStream(filename, FileMode.Open, FileAccess.Read);

        List<Record> records = new();
        var buffer = new byte[4096];

        var readHead = new Span<byte>();
        var writeHead = buffer.AsSpan();

        var needMore = true;
        var firstLine = true;
        while (needMore)
        {
            var bytesRead = fileStream.Read(writeHead);
            if (bytesRead <= 0)
            {
                // EOF
                break;
            }

            // Move the write head by the amount written.
            writeHead = buffer.AsSpan(readHead.Length + bytesRead);
            readHead = buffer.AsSpan(0, readHead.Length + bytesRead);

            var endOfLine = readHead.IndexOf((byte)'\n');
            if (endOfLine > 0)
            {
                // Parse the data.
                if (!firstLine)
                {
                    ParseLine(readHead[..endOfLine], records);
                }

                firstLine = false;

                // Copy remainder to start of buffer
                var remainder = readHead[(endOfLine + 1)..readHead.Length];
                remainder.CopyTo(buffer);
                readHead = buffer.AsSpan(0, remainder.Length);
                writeHead = buffer.AsSpan(remainder.Length);
            }

            // Grow the buffer
            if (endOfLine < 0 && readHead.Length == buffer.Length)
            {
                buffer = new byte[readHead.Length + 4096];
                readHead.CopyTo(buffer);
                readHead = buffer.AsSpan(0, readHead.Length);
                writeHead = buffer.AsSpan(readHead.Length);
            }
        }

        var (training, validation) = SplitDataSets(records, offset, count);
        return (training, validation);
    }

    private static void ParseLine(ReadOnlySpan<byte> span, List<Record> records)
    {
        // Skip the label
        int label = -1;
        var results = new List<int>();
        while (Utf8Parser.TryParse(span, out int value, out var consumed))
        {
            if (label == -1)
            {
                label = value;
            }
            else
            {
                results.Add(value);
            }

            if (span.Length > consumed)
            {
                span = span.Slice(consumed + 1);
            }
            else
            {
                break;
            }
        }

        records.Add(new Record(label, results));
    }

    private static (List<Record>, List<Record>) SplitDataSets(List<Record> data, int offset, int count)
    {
        var training = data.GetRange(0, offset);
        training.AddRange(data.GetRange(offset + count, (data.Count - offset - count)));
        var validation = data.GetRange(offset, count);
        return (training, validation);
    }

    public static List<List<Record>> ChunkData(List<Record> data, int chunks)
    {
        List<List<Record>> results = new();
        var chunk_size = data.Count / chunks;
        var remainder = data.Count % chunks;
        for (int i = 0; i < chunks; i++)
        {
            if (i != chunks - 1)
            {
                var chunk = data.GetRange(i * chunk_size, chunk_size);
                results.Add(chunk);
            }
            else
            {
                var chunk = data.GetRange(i * chunk_size, chunk_size + remainder);
                results.Add(chunk);
            }
        }
        return results;
    }
}
