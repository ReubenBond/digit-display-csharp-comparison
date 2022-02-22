using System.Security.Cryptography.X509Certificates;

namespace digits
{
    public readonly struct Record
    {
        public Record(int value, List<int> image)
        {
            Value = value;
            Image = image;
        }

        public int Value { get; init; }
        public List<int> Image { get; init; }
    }

    public readonly struct Prediction
    {
        public Prediction(Record actual, Record predicted)
        {
            Actual = actual;
            Predicted = predicted;
        }

        public Record Actual { get; init; }
        public Record Predicted { get; init; }
    }
}