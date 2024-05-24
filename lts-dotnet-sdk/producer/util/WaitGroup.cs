namespace HuaweiCloud.SDK.LTS.util
{
    public class WaitGroup
    {
        public object locker { set; get; }

        public int Count { set; get; }

        public void Add(int delta)
        {
            lock (locker)
            {
                Count += delta;
            }
        }
    }
}