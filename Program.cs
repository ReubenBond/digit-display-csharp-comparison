using digits;
using digit_console;
using System.Threading.Channels;
using CommandLine;
using System.Text;


string BlankSpace = new string(' ', 46);
string ResultLine = new string('=', 115);

int offset = 0;
int count = 10;
string classifier_option = "";

CommandLine.Parser.Default.ParseArguments<Configuration>(args)
    .WithParsed(c =>
    {
        offset = c.Offset;
        count = c.Count;
        classifier_option = c.Classifier.ToLower() switch
        {
            "euclidean" => "euclidean",
            "manhattan" => "manhattan",
            _ => "euclidean",
        };
    }).WithNotParsed(c =>
    {
        Environment.Exit(0);
    });

List<Prediction> errors = new();

var (training, validation) = FileLoader.GetData("train.csv", offset, count);
Console.Clear();
Console.WriteLine("Data Load Complete...");

IClassifier classifier = classifier_option
    switch
{
    "euclidean" => new EuclideanClassifier(training),
    "manhattan" => new ManhattanClassifier(training),
    _ => new EuclideanClassifier(training),
};

var start = DateTime.Now;

var channel = Channel.CreateUnbounded<Prediction>(new UnboundedChannelOptions { SingleReader = true });
var listener = Listen(channel.Reader, errors);

var producer = Produce(channel.Writer, classifier, validation);
await producer;
await listener;

var elapsed = DateTime.Now - start;

PrintSummary(classifier.Name, offset, count, elapsed, errors.Count);
Console.WriteLine("Press any key to show errors...");
Console.ReadLine();

var sb = new StringBuilder();
foreach (var item in errors)
{
    DisplayImages(sb, item, true);
}

PrintSummary(classifier.Name, offset, count, elapsed, errors.Count);


static async Task Produce(
    ChannelWriter<Prediction> writer,
    IClassifier classifier,
    List<Record> validation)
{
    await Parallel.ForEachAsync(
        validation,
        (imageData, token) =>
        {
            var result = classifier.Predict(new(imageData.Value, imageData.Image));
            if (writer.TryWrite(result))
            {
                return default;
            }

            return writer.WriteAsync(result, token);
        });

    writer.Complete();
}

async Task Listen(ChannelReader<Prediction> reader, List<Prediction> log)
{
    StringBuilder result = new();
    while (await reader.WaitToReadAsync())
    {
        while (reader.TryRead(out var prediction))
        {
            DisplayImages(result, prediction, false);
            if (prediction.Actual.Value != prediction.Predicted.Value)
            {
                log.Add(prediction);
            }
        }
    }
}

void DisplayImages(StringBuilder result, Prediction prediction, bool scroll)
{
    result.Append("Actual: ");
    result.Append(prediction.Actual.Value);
    result.Append(' ');
    result.Append(BlankSpace);
    result.Append(" | Predicted: ");
    result.Append(prediction.Predicted.Value);
    result.AppendLine();
    Display.GetImagesAsString(result, prediction.Actual.Image, prediction.Predicted.Image);
    result.AppendLine();
    result.Append(ResultLine);

    if (!scroll)
    {
        Console.SetCursorPosition(0, 0);
    }

    foreach (var chunk in result.GetChunks())
    {
        Console.Write(chunk);
    }
    Console.WriteLine();
    result.Clear();
}

static void PrintSummary(string name, int offset, int count, TimeSpan elapsed, int total_errors)
{
    Console.WriteLine($"Using {name} -- Offset: {offset}   Count: {count}");
    Console.WriteLine($"Total time: {elapsed}");
    Console.WriteLine($"Total errors: {total_errors}");
}
