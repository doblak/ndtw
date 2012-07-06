namespace NDtw.Preprocessing
{
    public interface IPreprocessor
    {
        double[] Preprocess(double[] data);
        string ToString();
    }
}