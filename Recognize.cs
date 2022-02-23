using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace digits;

public class Classifier
{
    private const int OrdainedLength = 784;
    private static readonly Record _defaultBest = new(0, new List<int>());

    internal static Prediction Predict<TClassifier>(Record input, TClassifier classifier, List<Record> trainingData) where TClassifier : IClassifier
    {
        int best_total = int.MaxValue;
        var best = _defaultBest;

        var inputList = input.Image;
        foreach (Record candidate in trainingData)
        {
            var candidateList = candidate.Image;
            int total = 0;
            for (int i = 0; i < OrdainedLength; i++)
            {
                int diff = classifier.Algorithm(inputList[i], candidateList[i]);
                total += diff;
            }

            if (total < best_total)
            {
                best_total = total;
                best = candidate;
            }
        }

        return new Prediction(input, best);
    }

    [DoesNotReturn]
    private static void ThrowInvalidInput() => throw new ArgumentException("Input is of an insufficient length");
}

public interface IClassifier
{
    string Name { get; }

    int Algorithm(int input, int test);

    Prediction Predict(Record input);
}

public readonly struct ManhattanClassifier : IClassifier
{
    private readonly List<Record> _trainingData;

    public ManhattanClassifier(List<Record> trainingData)
    {
        _trainingData = trainingData;
    }

    public string Name => "Manhattan Classifier";

    public Prediction Predict(Record input)
    {
        return Classifier.Predict(input, this, _trainingData);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int Algorithm(int input, int test)
    {
        return Math.Abs(input - test);
    }
}

public readonly struct EuclideanClassifier : IClassifier
{
    private readonly List<Record> _trainingData;

    public EuclideanClassifier(List<Record> trainingData)
    {
        _trainingData = trainingData;
    }

    public string Name => "Euclidean Classifier";

    public Prediction Predict(Record input)
    {
        return Classifier.Predict(input, this, _trainingData);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int Algorithm(int input, int test)
    {
        var diff = input - test;
        return diff * diff;
    }
}
